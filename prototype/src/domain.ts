export type Network = "ethereum" | "polygon";
export type PaymentStatus =
  | "requires_payment"
  | "detected"
  | "confirming"
  | "succeeded"
  | "expired"
  | "cancelled";
export type Scenario =
  | "detect"
  | "confirm"
  | "finalize"
  | "partial"
  | "excess"
  | "expire"
  | "late"
  | "wrong_token"
  | "duplicate"
  | "reorg"
  | "hold"
  | "timeout"
  | "reconcile_timeout";
export interface Journal {
  id: string;
  at: string;
  title: string;
  detail: string;
}
export interface Transfer {
  id: string;
  network: Network;
  hash: string;
  logIndex: number;
  contract: string;
  amount: string;
  credited: boolean;
  confirmations: number;
}
export interface Intent {
  id: string;
  tenant: string;
  merchant: string;
  reference: string;
  amount: string;
  received: string;
  network: Network;
  status: PaymentStatus;
  hold: boolean;
  flags: string[];
  provider: string;
  policy: string;
  routeEvidence: Route[];
  expiresAt: string;
  createdAt: string;
  transfers: Transfer[];
  events: Journal[];
  settlement: "pending" | "settled" | "mismatch";
  unknownOutcome: boolean;
}
export interface Provider {
  id: string;
  name: string;
  enabled: boolean;
  healthy: boolean;
  fee: string;
  latency: string;
  priority: number;
  limit: string;
}
export interface Route {
  provider: string;
  eligible: boolean;
  reasons: string[];
  fee: string;
  priority: number;
}
export interface HookEvent {
  id: string;
  tenant: string;
  intent: string;
  type: string;
  status: "delivered" | "failed";
  attempts: number;
  history: Journal[];
}
export interface Credential {
  id: string;
  tenant: string;
  label: string;
  revoked: boolean;
}
export interface State {
  version: 1;
  statementRefs: Record<string, string>;
  intents: Intent[];
  providers: Provider[];
  hooks: HookEvent[];
  audit: Journal[];
  keys: Credential[];
  idempotency: Record<string, { payload: string; id: string }>;
  endpoints: Record<string, string>;
}
export const TOKEN: Record<Network, string> = {
  ethereum: "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48",
  polygon: "0x3c499c542cEF5E3811e1192ce70d8cC03d5c3359",
};
export const CHAIN: Record<Network, string> = { ethereum: "1", polygon: "137" };
export const DEPOSIT = "0x7B91a12E2f5a8c0c4D09C3c4D84a0B31b946cE12";
export const now = () => new Date().toISOString();
export const uid = (prefix: string) =>
  `${prefix}_${crypto.randomUUID().slice(0, 8)}`;
export function entry(title: string, detail = ""): Journal {
  return { id: uid("evt"), at: now(), title, detail };
}
// Six-decimal token precision. All financial arithmetic uses integer base units.
export function units(value: string): bigint {
  if (!/^\d+(\.\d{1,6})?$/.test(value))
    throw new Error(
      "Enter a positive decimal with at most six decimal places.",
    );
  const [whole, fraction = ""] = value.split(".");
  return BigInt(whole) * 1000000n + BigInt(fraction.padEnd(6, "0"));
}
export function decimal(value: bigint): string {
  return `${value / 1000000n}.${String(value % 1000000n).padStart(6, "0")}`;
}
export function routes(state: State, amount: string, hold = false): Route[] {
  return state.providers
    .map((p) => {
      const reasons = [
        !p.enabled && "Emergency stop enabled",
        !p.healthy && "Provider unavailable",
        hold && "Mandatory screening unresolved",
        units(amount) > units(p.limit) && "Amount exceeds provider limit",
      ].filter(Boolean) as string[];
      return {
        provider: p.name,
        eligible: reasons.length === 0,
        reasons,
        fee: p.fee,
        priority: p.priority,
      };
    })
    .sort((a, b) => a.priority - b.priority);
}
export interface CreateInput {
  tenant: string;
  merchant: string;
  reference: string;
  amount: string;
  network: Network;
  key: string;
  minutes: string;
}
export function createIntent(
  state: State,
  input: CreateInput,
): { state: State; id: string; replay: boolean } {
  if (!input.key.trim() || !input.reference.trim())
    throw new Error("Reference and idempotency key are required.");
  if (units(input.amount) <= 0n || units(input.amount) > units("100000"))
    throw new Error(
      "Amount must be greater than zero and no more than USD 100,000.",
    );
  if (
    !/^\d+$/.test(input.minutes) ||
    +input.minutes < 1 ||
    +input.minutes > 1440
  )
    throw new Error("Expiry must be 1–1440 minutes.");
  if (
    input.merchant !==
    (input.tenant === "atlas" ? "Northstar Commerce" : "Harbor Markets")
  )
    throw new Error("Merchant is outside the active tenant.");
  const key = `${input.tenant}:${input.key}`;
  const payload = JSON.stringify({
    ...input,
    amount: decimal(units(input.amount)),
  });
  const previous = state.idempotency[key];
  if (previous) {
    if (previous.payload !== payload)
      throw new Error(
        "idempotency_conflict: this key was used with a different payload.",
      );
    return { state, id: previous.id, replay: true };
  }
  const evidence = routes(state, input.amount);
  const chosen = evidence.find((r) => r.eligible);
  if (!chosen)
    throw new Error(
      "no_eligible_route: restore an eligible provider before creating instructions.",
    );
  const id = uid("pi");
  const intent: Intent = {
    id,
    tenant: input.tenant,
    merchant: input.merchant,
    reference: input.reference,
    amount: decimal(units(input.amount)),
    received: "0.000000",
    network: input.network,
    status: "requires_payment",
    hold: false,
    flags: [],
    provider: chosen.provider,
    policy: "pol_2026_10_v3",
    routeEvidence: evidence,
    expiresAt: new Date(
      Date.now() + Number(input.minutes) * 60000,
    ).toISOString(),
    createdAt: now(),
    transfers: [],
    events: [
      entry(
        "Payment instructions issued",
        `${chosen.provider}; policy pol_2026_10_v3; exact token and chain verified`,
      ),
      entry("Intent created", `Idempotency key ${input.key}`),
    ],
    settlement: "pending",
    unknownOutcome: false,
  };
  return {
    state: {
      ...state,
      intents: [intent, ...state.intents],
      idempotency: { ...state.idempotency, [key]: { payload, id } },
      audit: [
        entry("Intent created", `${input.tenant} · ${id}`),
        ...state.audit,
      ],
    },
    id,
    replay: false,
  };
}
export function access(state: State, id: string, tenant: string): Intent {
  const intent = state.intents.find((i) => i.id === id && i.tenant === tenant);
  if (!intent)
    throw new Error(
      "authorization_denied: intent is outside the active tenant.",
    );
  return intent;
}
export function simulate(
  state: State,
  id: string,
  tenant: string,
  scenario: Scenario,
): State {
  const source = access(state, id, tenant);
  const p: Intent = structuredClone(source);
  const add = (title: string, detail = "") =>
    p.events.unshift(entry(title, detail));
  const flag = (name: string) => {
    if (!p.flags.includes(name)) p.flags.push(name);
  };
  if (scenario === "timeout") {
    p.unknownOutcome = true;
    flag("Unknown provider outcome");
    add(
      "Provider acknowledgement timed out",
      "No fallback permitted until original outcome is reconciled.",
    );
  } else if (scenario === "reconcile_timeout") {
    if (!p.unknownOutcome)
      throw new Error("There is no unknown outcome to reconcile.");
    p.unknownOutcome = false;
    p.flags = p.flags.filter((f) => f !== "Unknown provider outcome");
    add(
      "Original provider outcome reconciled",
      "Provider confirmed existing instructions; no second execution.",
    );
  } else if (scenario === "hold") {
    p.hold = true;
    add(
      "Compliance hold applied",
      "CASE-1042 · Mandatory screening unavailable; fail closed.",
    );
  } else if (scenario === "expire") {
    if (["succeeded", "cancelled"].includes(p.status))
      throw new Error("A terminal intent cannot expire.");
    p.status = "expired";
    add("Intent expired", "Monitoring remains active for late transfers.");
  } else if (scenario === "reorg") {
    if (!p.transfers.length)
      throw new Error("Detect a transfer before simulating a reorganization.");
    p.status = ["expired", "cancelled"].includes(p.status)
      ? p.status
      : "confirming";
    p.transfers.forEach((t) => (t.confirmations = 0));
    flag("Reorganization review");
    if (p.settlement === "settled") p.settlement = "mismatch";
    add(
      "Chain reorganization detected",
      "Provisional finality withdrawn; append-only correction; settlement requires review.",
    );
  } else if (scenario === "confirm" || scenario === "finalize") {
    if (!p.transfers.some((t) => t.credited))
      throw new Error("Detect a valid transfer first.");
    if (["expired", "cancelled"].includes(p.status))
      throw new Error(
        "Late or cancelled transfers require an acceptance review.",
      );
    if (p.hold || p.unknownOutcome)
      throw new Error(
        "Resolve the compliance hold or unknown execution outcome before advancing.",
      );
    if (p.flags.includes("Reorganization review"))
      throw new Error(
        "Reorganization requires independent chain review before finality can be restored.",
      );
    p.transfers.forEach((t) => {
      if (t.credited)
        t.confirmations =
          scenario === "finalize" ? (p.network === "ethereum" ? 12 : 64) : 3;
    });
    p.status =
      scenario === "finalize" && units(p.received) >= units(p.amount)
        ? "succeeded"
        : "confirming";
    add(
      p.status === "succeeded"
        ? "Payment finality confirmed"
        : "Confirmations updated",
      `${p.network === "ethereum" ? 12 : 64} confirmations required; payment and settlement remain separate.`,
    );
  } else {
    if (scenario === "duplicate") {
      if (!p.transfers.length) throw new Error("Detect a transfer first.");
      add(
        "Duplicate chain event ignored",
        `${p.transfers[0].hash}:0 · No additional financial credit.`,
      );
    } else {
      if (scenario === "late" && !["expired", "cancelled"].includes(p.status))
        throw new Error(
          "Expire or cancel the intent before recording a late transfer.",
        );
      const amount =
        scenario === "partial"
          ? decimal(units(p.amount) / 2n)
          : scenario === "excess"
            ? decimal(units(p.amount) + units("10"))
            : p.amount;
      const t: Transfer = {
        id: uid("tx"),
        network: p.network,
        hash: `0x${crypto.randomUUID().replaceAll("-", "").padEnd(64, "0")}`,
        logIndex: 0,
        contract:
          scenario === "wrong_token"
            ? "0x0000000000000000000000000000000000000001"
            : TOKEN[p.network],
        amount,
        credited: scenario !== "wrong_token",
        confirmations: 0,
      };
      p.transfers.push(t);
      if (t.credited) p.received = decimal(units(p.received) + units(amount));
      if (scenario === "wrong_token") flag("Wrong token · unallocated");
      if (["expired", "cancelled"].includes(p.status))
        flag("Late transfer · acceptance review");
      else if (t.credited && p.status !== "succeeded") p.status = "detected";
      if (t.credited && units(p.received) < units(p.amount))
        flag("Underpayment");
      if (units(p.received) > units(p.amount)) flag("Overpayment");
      if (units(p.received) >= units(p.amount))
        p.flags = p.flags.filter((f) => f !== "Underpayment");
      add(
        t.credited ? "Transfer detected" : "Unallocated transfer recorded",
        `${amount} USDC · ${scenario === "wrong_token" ? "Contract mismatch: never credited" : "Exact chain, contract and destination verified"}`,
      );
    }
  }
  const event: HookEvent = {
    id: uid("wh"),
    tenant,
    intent: id,
    type: `payment_intent.${p.status}`,
    status: "failed",
    attempts: 1,
    history: [
      entry(
        "Delivery failed",
        "HTTP 503 · Next retry in 30 seconds (sandbox clock paused)",
      ),
    ],
  };
  const emits =
    scenario !== "duplicate" &&
    scenario !== "timeout" &&
    scenario !== "reconcile_timeout";
  return {
    ...state,
    intents: state.intents.map((i) => (i.id === id ? p : i)),
    hooks: emits ? [event, ...state.hooks] : state.hooks,
    audit: [entry(`Sandbox: ${scenario}`, `${tenant} · ${id}`), ...state.audit],
  };
}
export function cancelIntent(state: State, id: string, tenant: string): State {
  const p = access(state, id, tenant);
  if (
    !["requires_payment"].includes(p.status) ||
    p.transfers.length ||
    p.unknownOutcome
  )
    throw new Error(
      "Only an unfunded intent with a known provider outcome can be cancelled.",
    );
  return {
    ...state,
    intents: state.intents.map((i) =>
      i.id === id
        ? {
            ...i,
            status: "cancelled",
            events: [
              entry(
                "Intent cancelled",
                "Blockchain monitoring continues; broadcast transfers cannot be reversed.",
              ),
              ...i.events,
            ],
          }
        : i,
    ),
    audit: [entry("Intent cancelled", id), ...state.audit],
  };
}
export function replayHook(state: State, id: string, tenant: string): State {
  const h = state.hooks.find((h) => h.id === id && h.tenant === tenant);
  if (!h) throw new Error("authorization_denied");
  return {
    ...state,
    hooks: state.hooks.map((e) =>
      e.id === id
        ? {
            ...e,
            status: "delivered",
            attempts: e.attempts + 1,
            history: [
              entry(
                "Replay delivered",
                "HTTP 200 · Stable event ID retained; consumer must deduplicate.",
              ),
              ...e.history,
            ],
          }
        : e,
    ),
    audit: [entry("Webhook replayed", id), ...state.audit],
  };
}
export function seed(): State {
  let s: State = {
    version: 1,
    statementRefs: {},
    intents: [],
    providers: [
      {
        id: "a",
        name: "Bridgeway",
        enabled: true,
        healthy: true,
        fee: "0.80",
        latency: "184 ms",
        priority: 1,
        limit: "100000",
      },
      {
        id: "b",
        name: "Orbit Payments",
        enabled: true,
        healthy: true,
        fee: "0.45",
        latency: "236 ms",
        priority: 2,
        limit: "50000",
      },
    ],
    hooks: [],
    audit: [
      entry(
        "Sandbox initialized",
        "All providers, transfers and screening results are simulated.",
      ),
    ],
    keys: [
      {
        id: "sk_test_atlas_••••7f2a",
        tenant: "atlas",
        label: "Development integration",
        revoked: false,
      },
    ],
    idempotency: {},
    endpoints: {
      atlas: "https://sandbox.northstar.example/webhooks",
      harbor: "https://sandbox.harbor.example/webhooks",
    },
  };
  const data: [string, string, Network, PaymentStatus][] = [
    ["8421", "24500", "polygon", "succeeded"],
    ["8420", "1280", "ethereum", "confirming"],
    ["8419", "5200", "polygon", "detected"],
    ["8418", "890", "polygon", "requires_payment"],
    ["8417", "3200", "ethereum", "expired"],
    ["8416", "18000", "polygon", "succeeded"],
    ["8415", "750", "ethereum", "succeeded"],
    ["8414", "4100", "polygon", "requires_payment"],
  ];
  for (const [ref, amount, network, status] of data) {
    const c = createIntent(s, {
      tenant: "atlas",
      merchant: "Northstar Commerce",
      reference: `ORD-${ref}`,
      amount,
      network,
      key: `seed-${ref}`,
      minutes: "15",
    });
    s = c.state;
    if (["succeeded", "confirming", "detected"].includes(status)) {
      s = simulate(s, c.id, "atlas", ref === "8419" ? "partial" : "detect");
      if (status !== "detected")
        s = simulate(
          s,
          c.id,
          "atlas",
          status === "succeeded" ? "finalize" : "confirm",
        );
    }
    if (status === "expired") {
      s = simulate(s, c.id, "atlas", "expire");
      s = simulate(s, c.id, "atlas", "late");
    }
    if (ref === "8421")
      s.intents = s.intents.map((i) =>
        i.id === c.id ? { ...i, settlement: "mismatch" } : i,
      );
    if (ref === "8416")
      s.intents = s.intents.map((i) =>
        i.id === c.id ? { ...i, settlement: "settled" } : i,
      );
  }
  s = createIntent(s, {
    tenant: "harbor",
    merchant: "Harbor Markets",
    reference: "HB-2041",
    amount: "9200",
    network: "polygon",
    key: "harbor-seed",
    minutes: "15",
  }).state;
  s.hooks = s.hooks.slice(0, 5);
  return s;
}
export function exportCsv(headers: string[], rows: string[][]): string {
  const safe = (v: string) =>
    `"${(/^[=+\-@\t\r]/.test(v) ? "'" : "") + v.replaceAll('"', '""')}"`;
  return [headers, ...rows].map((row) => row.map(safe).join(",")).join("\r\n");
}

/** Deliberately narrow sandbox statement format; quoted/multiline CSV is rejected. */
export function importStatement(
  state: State,
  tenant: string,
  csv: string,
): State {
  const lines = csv.trim().split(/\r?\n/);
  if (
    lines.shift() !== "intent_id,amount_usdc,provider_reference" ||
    !lines.length ||
    lines.length > 100
  )
    throw new Error(
      "Use intent_id,amount_usdc,provider_reference with 1–100 unquoted rows.",
    );
  const parsed = lines.map((line) => {
    const cells = line.split(",");
    if (cells.length !== 3 || cells.some((c) => !c.trim() || /["\n\r]/.test(c)))
      throw new Error("Each row needs exactly three unquoted values.");
    const [id, amount, reference] = cells.map((c) => c.trim());
    const intent = access(state, id, tenant);
    units(amount);
    if (intent.status !== "succeeded" || intent.hold || intent.flags.length)
      throw new Error(
        `${intent.reference}: resolve finality and payment exceptions before settlement.`,
      );
    return { id, amount: decimal(units(amount)), reference };
  });
  let next = structuredClone(state);
  for (const row of parsed) {
    const key = `${tenant}:${row.reference}`;
    const payload = JSON.stringify({ id: row.id, amount: row.amount });
    if (next.statementRefs[key]) {
      if (next.statementRefs[key] !== payload)
        throw new Error(
          "statement_conflict: provider reference has conflicting financial data.",
        );
      next.audit.unshift(
        entry("Duplicate statement ignored", `${tenant} · ${row.reference}`),
      );
      continue;
    }
    const intent = access(next, row.id, tenant);
    const matched = units(row.amount) === units(intent.received);
    next = {
      ...next,
      statementRefs: { ...next.statementRefs, [key]: payload },
      intents: next.intents.map((p) =>
        p.id === row.id
          ? {
              ...p,
              settlement: matched ? "settled" : "mismatch",
              events: [
                entry(
                  matched
                    ? "Provider statement matched"
                    : "Settlement mismatch detected",
                  `${row.reference} · statement ${row.amount} USDC; observed ${p.received} USDC · original evidence retained`,
                ),
                ...p.events,
              ],
            }
          : p,
      ),
      audit: [
        entry(
          "Provider statement imported",
          `${tenant} · ${row.reference} · ${matched ? "matched" : "mismatch"}`,
        ),
        ...next.audit,
      ],
    };
  }
  return next;
}
