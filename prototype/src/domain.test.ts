import { describe, expect, it } from "vitest";
import {
  access,
  cancelIntent,
  createIntent,
  importStatement,
  decimal,
  exportCsv,
  replayHook,
  routes,
  seed,
  simulate,
  units,
  type State,
} from "./domain";
const input = {
  tenant: "atlas",
  merchant: "Northstar Commerce",
  reference: "TEST-100",
  amount: "250.00",
  network: "polygon" as const,
  key: "test-idempotency",
  minutes: "15",
};
function fresh() {
  return createIntent(seed(), input);
}
describe("payment safety invariants", () => {
  it("returns the same logical intent for the same key and normalized payload", () => {
    const a = fresh();
    const b = createIntent(a.state, { ...input, amount: "250.000000" });
    expect(b.id).toBe(a.id);
    expect(b.replay).toBe(true);
    expect(b.state.intents.length).toBe(a.state.intents.length);
  });
  it("rejects conflicting reuse of an idempotency key", () => {
    const a = fresh();
    expect(() => createIntent(a.state, { ...input, amount: "251" })).toThrow(
      "idempotency_conflict",
    );
  });
  it("falls back before issuing instructions when the primary provider is unavailable", () => {
    const s = seed();
    s.providers[0].healthy = false;
    const c = createIntent(s, input);
    expect(access(c.state, c.id, "atlas").provider).toBe("Orbit Payments");
  });
  it("fails closed when both provider routes are unavailable", () => {
    const s = seed();
    s.providers.forEach((p) => (p.enabled = false));
    expect(() => createIntent(s, input)).toThrow("no_eligible_route");
  });
  it("blocks cancellation/finality after an ambiguous timeout and preserves original provider", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = simulate(state, id, "atlas", "timeout");
    expect(() => cancelIntent(state, id, "atlas")).toThrow();
    state = simulate(state, id, "atlas", "detect");
    expect(() => simulate(state, id, "atlas", "finalize")).toThrow("unknown");
    state = simulate(state, id, "atlas", "reconcile_timeout");
    expect(access(state, id, "atlas").provider).toBe("Bridgeway");
    expect(access(state, id, "atlas").transfers).toHaveLength(1);
  });
  it("duplicate events and serialized recovery do not recognize financial value twice", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = simulate(state, id, "atlas", "detect");
    state = JSON.parse(JSON.stringify(state)) as State;
    state = simulate(state, id, "atlas", "duplicate");
    expect(access(state, id, "atlas").received).toBe("250.000000");
    expect(access(state, id, "atlas").transfers).toHaveLength(1);
  });
  it("records late transfers on expired intents without automatically accepting them", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = simulate(state, id, "atlas", "expire");
    state = simulate(state, id, "atlas", "late");
    const p = access(state, id, "atlas");
    expect(p.status).toBe("expired");
    expect(p.transfers).toHaveLength(1);
    expect(p.flags).toContain("Late transfer · acceptance review");
    expect(() => simulate(state, id, "atlas", "finalize")).toThrow("review");
  });
  it("never credits a wrong contract", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = simulate(state, id, "atlas", "wrong_token");
    const p = access(state, id, "atlas");
    expect(p.received).toBe("0.000000");
    expect(p.transfers[0].credited).toBe(false);
    expect(p.status).toBe("requires_payment");
  });
  it("reorganizations withdraw finality and preserve prior evidence", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = simulate(state, id, "atlas", "detect");
    state = simulate(state, id, "atlas", "finalize");
    const original = access(state, id, "atlas").events;
    state = simulate(state, id, "atlas", "reorg");
    const p = access(state, id, "atlas");
    expect(p.status).toBe("confirming");
    expect(p.events).toEqual(expect.arrayContaining(original));
    expect(() => simulate(state, id, "atlas", "finalize")).toThrow(
      "independent chain review",
    );
  });
  it("replays the same webhook ID with a new attempt", () => {
    const s = seed();
    const h = s.hooks[0];
    const next = replayHook(s, h.id, h.tenant);
    const result = next.hooks.find((x) => x.id === h.id)!;
    expect(result.attempts).toBe(h.attempts + 1);
    expect(result.status).toBe("delivered");
    expect(result.history).toHaveLength(2);
  });
  it("mandatory screening excludes even the cheapest provider", () => {
    expect(routes(seed(), "250", true).every((r) => !r.eligible)).toBe(true);
  });
  it("rejects cross-tenant reads and mutations", () => {
    const { state, id } = fresh();
    expect(() => access(state, id, "harbor")).toThrow("authorization_denied");
    expect(() => simulate(state, id, "harbor", "detect")).toThrow(
      "authorization_denied",
    );
  });
  it("retains historical policy and inputs after provider controls change", () => {
    const { state, id } = fresh();
    const before = structuredClone(access(state, id, "atlas").routeEvidence);
    state.providers[0].enabled = false;
    expect(access(state, id, "atlas").routeEvidence).toEqual(before);
    expect(access(state, id, "atlas").policy).toBe("pol_2026_10_v3");
  });
  it("keeps compliance, payment exceptions and settlement separate", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = simulate(state, id, "atlas", "partial");
    state = simulate(state, id, "atlas", "hold");
    const p = access(state, id, "atlas");
    expect(p.status).toBe("detected");
    expect(p.hold).toBe(true);
    expect(p.flags).toContain("Underpayment");
    expect(p.settlement).toBe("pending");
    expect(() => simulate(state, id, "atlas", "finalize")).toThrow("hold");
  });
  it("does exact six-decimal arithmetic and validates input", () => {
    expect(decimal(units("0.1") + units("0.2"))).toBe("0.300000");
    expect(() => units("0.0000001")).toThrow();
    expect(() => createIntent(seed(), { ...input, amount: "0" })).toThrow();
    expect(() => createIntent(seed(), { ...input, minutes: "0" })).toThrow();
  });
  it("cancels only eligible intents and still records unexpected transfers", () => {
    const initial = fresh();
    const id = initial.id;
    let state = initial.state;
    state = cancelIntent(state, id, "atlas");
    state = simulate(state, id, "atlas", "late");
    expect(access(state, id, "atlas").status).toBe("cancelled");
    expect(access(state, id, "atlas").transfers).toHaveLength(1);
  });
  it("neutralizes spreadsheet formulas and quotes CSV fields", () => {
    expect(exportCsv(["Reference"], [['=HYPERLINK("bad")']])).toContain("'=");
    expect(exportCsv(["Reference"], [["A,B"]])).toContain('"A,B"');
  });
});

describe("provider statement reconciliation", () => {
  it("detects a mismatch and retains evidence when corrected", () => {
    const { state, id } = fresh();
    const detected = simulate(state, id, "atlas", "detect");
    const finalized = simulate(detected, id, "atlas", "finalize");
    const mismatch = importStatement(
      finalized,
      "atlas",
      `intent_id,amount_usdc,provider_reference\n${id},249.00,STMT-1`,
    );
    expect(access(mismatch, id, "atlas").settlement).toBe("mismatch");
    const corrected = importStatement(
      mismatch,
      "atlas",
      `intent_id,amount_usdc,provider_reference\n${id},250.00,STMT-2`,
    );
    expect(access(corrected, id, "atlas").settlement).toBe("settled");
    expect(
      access(corrected, id, "atlas").events.some(
        (e) => e.title === "Settlement mismatch detected",
      ),
    ).toBe(true);
  });
  it("deduplicates provider references and rejects conflicting data atomically", () => {
    const { state, id } = fresh();
    const finalized = simulate(
      simulate(state, id, "atlas", "detect"),
      id,
      "atlas",
      "finalize",
    );
    const csv = `intent_id,amount_usdc,provider_reference\n${id},250.00,STMT-1`;
    const first = importStatement(finalized, "atlas", csv);
    const again = importStatement(first, "atlas", csv);
    expect(access(again, id, "atlas").events).toHaveLength(
      access(first, id, "atlas").events.length,
    );
    expect(() =>
      importStatement(first, "atlas", csv.replace("250.00", "249.00")),
    ).toThrow("statement_conflict");
    expect(() => importStatement(first, "harbor", csv)).toThrow(
      "authorization_denied",
    );
  });
});
