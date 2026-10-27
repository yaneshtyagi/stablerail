import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  Alert,
  AppShell,
  Badge,
  BarChart,
  Button,
  Card,
  CardBody,
  CardHeader,
  ChartCard,
  ConfirmDialog,
  DataTable,
  DescriptionList,
  EmptyState,
  FormFooter,
  FormGrid,
  FormLayout,
  FormSection,
  KpiRow,
  LinkButton,
  Money,
  MoneyField,
  PageHeader,
  RadioGroup,
  SearchInput,
  SelectField,
  Stat,
  StatusBadge,
  Switch,
  Tabs,
  TextField,
  TextareaField,
  Timeline,
  useTheme,
  useToast,
  formatDateTime,
  type Column,
  type NavGroup,
  type Status,
  type SortState,
} from "@yaneshtyagi/design-system";
import {
  cancelIntent,
  CHAIN,
  createIntent,
  importStatement,
  DEPOSIT,
  entry,
  exportCsv,
  replayHook,
  routes,
  seed,
  simulate,
  TOKEN,
  units,
  decimal,
  uid,
  type HookEvent,
  type Intent,
  type Network,
  type Scenario,
  type State,
} from "./domain";
const STORAGE = "stablerail.sandbox.v3";
const displayTime = (iso: string) =>
  formatDateTime(iso, {
    timeZone: "Asia/Kolkata",
    withYear: true,
    zoneLabel: "IST",
  });
const navTo = (path: string) => {
  window.location.hash = path;
};
const statusMap: Record<Intent["status"], Status> = {
  requires_payment: "Initiated",
  detected: "Processing",
  confirming: "Processing",
  succeeded: "Paid",
  expired: "Overdue",
  cancelled: "Archived",
};
function PaymentState({ payment }: { payment: Intent }) {
  return (
    <span
      className="bv-stack"
      style={{ gap: "var(--space-1)", alignItems: "flex-start" }}
    >
      <StatusBadge status={statusMap[payment.status]} />
      <span className="bv-mono bv-body-sm">{payment.status}</span>
    </span>
  );
}
function Amount({ value }: { value: string }) {
  return <Money amount={value} currency="USD" currencyDisplay="code" />;
}
function download(name: string, csv: string) {
  const url = URL.createObjectURL(
    new Blob([csv], { type: "text/csv;charset=utf-8" }),
  );
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  a.click();
  URL.revokeObjectURL(url);
}
function load(): State {
  try {
    const data = localStorage.getItem(STORAGE);
    if (data) {
      const s = JSON.parse(data) as State;
      if (
        s.version === 1 &&
        Array.isArray(s.intents) &&
        Array.isArray(s.providers)
      )
        return s;
    }
  } catch {
    /* A corrupt or unavailable browser store starts a fresh sandbox. */
  }
  return seed();
}
export function App() {
  const [state, setState] = useState<State>(load);
  const [tenant, setTenant] = useState("atlas");
  const [path, setPath] = useState(location.hash.slice(1) || "/overview");
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState("all");
  const [tableState, setTableState] = useState("ready");
  const [sort, setSort] = useState<SortState | null>(null);
  const [confirmation, setConfirmation] = useState<{
    title: string;
    body: ReactNode;
    label: string;
    danger?: boolean;
    action: () => void;
  } | null>(null);
  const toast = useToast();
  const { toggleTheme } = useTheme();
  useEffect(() => {
    const fn = () => {
      setPath(location.hash.slice(1) || "/overview");
      setFilter("all");
      setTableState("ready");
    };
    window.addEventListener("hashchange", fn);
    return () => window.removeEventListener("hashchange", fn);
  }, []);
  useEffect(() => {
    try {
      localStorage.setItem(STORAGE, JSON.stringify(state));
    } catch {
      toast.show({
        tone: "danger",
        title: "Browser storage unavailable",
        body: "Changes are available until this page reloads.",
      });
    }
  }, [state, toast]);
  const mutate = (fn: (s: State) => State, message: string) => {
    try {
      setState(fn(state));
      toast.show({ tone: "success", title: message });
    } catch (e) {
      toast.show({
        tone: "danger",
        title: "Action blocked",
        body: (e as Error).message,
      });
    }
  };
  const confirm = (
    title: string,
    body: ReactNode,
    label: string,
    action: () => void,
    danger = false,
  ) => setConfirmation({ title, body, label, action, danger });
  const payments = state.intents.filter((p) => p.tenant === tenant);
  const merchant = tenant === "atlas" ? "Northstar Commerce" : "Harbor Markets";
  const section = path.split("/")[1] || "overview";
  const selected = payments.find((p) => p.id === path.split("/")[2]);
  useEffect(() => {
    const id = path.split("/")[2];
    if (section === "payments" && id && id !== "new" && !selected) {
      setState((previous) => ({
        ...previous,
        audit: [
          entry(
            "Cross-tenant or missing record access denied",
            `${tenant} · ${id}`,
          ),
          ...previous.audit,
        ],
      }));
    }
  }, [path, tenant, section, selected]);
  const groups: NavGroup[] = [
    {
      caption: "Workspace",
      items: [
        {
          id: "overview",
          label: "Overview",
          icon: "layout-dashboard",
          href: "#/overview",
        },
        {
          id: "payments",
          label: "Payments",
          icon: "wallet",
          href: "#/payments",
        },
      ],
    },
    {
      caption: "Operations",
      items: [
        {
          id: "routing",
          label: "Routing policies",
          icon: "sliders-horizontal",
          href: "#/routing",
        },
        {
          id: "providers",
          label: "Providers",
          icon: "building-2",
          href: "#/providers",
        },
        { id: "webhooks", label: "Webhooks", icon: "send", href: "#/webhooks" },
      ],
    },
    {
      caption: "Finance",
      items: [
        {
          id: "reconciliation",
          label: "Reconciliation",
          icon: "circle-check",
          href: "#/reconciliation",
          count: payments.filter(
            (p) => p.flags.length || p.settlement === "mismatch",
          ).length,
        },
        {
          id: "settlements",
          label: "Settlements",
          icon: "receipt",
          href: "#/settlements",
        },
      ],
    },
    {
      caption: "Administration",
      items: [
        {
          id: "administration",
          label: "Merchants & access",
          icon: "users",
          href: "#/administration",
        },
        { id: "audit", label: "Audit trail", icon: "history", href: "#/audit" },
      ],
    },
  ].map((g) => ({
    ...g,
    items: g.items.map((i) => ({ ...i, current: i.id === section })),
  })) as NavGroup[];
  const filtered = payments.filter(
    (p) =>
      `${p.id} ${p.reference} ${p.merchant} ${p.provider} ${p.network}`
        .toLowerCase()
        .includes(query.toLowerCase()) &&
      (filter === "all" || p.status === filter),
  );
  if (sort)
    filtered.sort((a, b) => {
      const key = sort.id as "reference" | "amount";
      const result =
        key === "amount"
          ? units(a.amount) > units(b.amount)
            ? 1
            : units(a.amount) < units(b.amount)
              ? -1
              : 0
          : a.reference.localeCompare(b.reference);
      return sort.direction === "ascending" ? result : -result;
    });
  const paymentColumns = useMemo<Column<Intent>[]>(
    () => [
      {
        id: "reference",
        header: "Payment / reference",
        rowHeader: true,
        sortable: true,
        cell: (p) => (
          <span
            className="bv-stack"
            style={{ gap: "var(--space-1)", alignItems: "flex-start" }}
          >
            <LinkButton variant="link" href={`#/payments/${p.id}`}>
              {p.reference}
            </LinkButton>
            <span className="bv-mono bv-body-sm">{p.id}</span>
          </span>
        ),
      },
      {
        id: "amount",
        header: "Requested",
        numeric: true,
        sortable: true,
        cell: (p) => <Amount value={p.amount} />,
      },
      {
        id: "network",
        header: "Asset / network",
        cell: (p) => (
          <span
            className="bv-stack"
            style={{ gap: "var(--space-1)", alignItems: "flex-start" }}
          >
            <span>USDC</span>
            <span className="bv-body-sm page-note">
              {p.network === "polygon" ? "Polygon · 137" : "Ethereum · 1"}
            </span>
          </span>
        ),
      },
      { id: "provider", header: "Provider", cell: (p) => p.provider },
      {
        id: "status",
        header: "Payment state",
        cell: (p) => <PaymentState payment={p} />,
      },
      {
        id: "exception",
        header: "Review",
        cell: (p) =>
          p.hold ? (
            <StatusBadge status="On hold" />
          ) : p.flags.length ? (
            <StatusBadge status="In review" />
          ) : (
            <span className="page-note">No exceptions</span>
          ),
      },
    ],
    [],
  );
  const exportPayments = () =>
    download(
      "stablerail-payments.csv",
      exportCsv(
        [
          "Intent",
          "Reference",
          "Amount USD",
          "Received USDC",
          "Network",
          "Provider",
          "Payment state",
          "Compliance",
          "Settlement",
          "Exceptions",
        ],
        filtered.map((p) => [
          p.id,
          p.reference,
          p.amount,
          p.received,
          p.network,
          p.provider,
          p.status,
          p.hold ? "hold" : "clear",
          p.settlement,
          p.flags.join("; "),
        ]),
      ),
    );
  const tableTools = (
    <>
      <SearchInput
        aria-label="Search payments"
        placeholder="Search reference, ID, provider…"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
      />
      <SelectField
        label="Payment state"
        value={filter}
        onChange={(e) => setFilter(e.target.value)}
      >
        <option value="all">All states</option>
        {Object.keys(statusMap).map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </SelectField>
      <Button variant="secondary" icon="download" onClick={exportPayments}>
        Export {filtered.length} rows
      </Button>
    </>
  );
  const table = (rows = filtered) => (
    <DataTable
      density="comfortable"
      aria-label="Payment intents"
      columns={paymentColumns}
      rows={tableState === "empty" ? [] : rows}
      rowKey={(p) => p.id}
      sort={sort}
      onSortChange={setSort}
      noun="payments"
      toolbar={tableTools}
      loading={tableState === "loading"}
      empty={
        <EmptyState
          icon="search"
          title="No payments found"
          actions={
            <Button
              onClick={() => {
                setQuery("");
                setFilter("all");
                setTableState("ready");
              }}
            >
              Reset filters
            </Button>
          }
        >
          Try another reference or clear your filters.
        </EmptyState>
      }
      error={
        tableState === "error" ? (
          <Alert
            tone="danger"
            title="Payments could not be loaded"
            actions={
              <Button onClick={() => setTableState("ready")}>
                Retry loading
              </Button>
            }
          >
            Simulated service error. Your payment records are preserved.
          </Alert>
        ) : undefined
      }
    />
  );
  const header = (title: string, description: string, actions?: ReactNode) => (
    <PageHeader
      breadcrumbs={[
        {
          label: tenant === "atlas" ? "Atlas PSP" : "Harbor PSP",
          href: "#/overview",
        },
        { label: title },
      ]}
      title={title}
      description={description}
      actions={actions}
    />
  );
  const newButton = (
    <LinkButton variant="primary" icon="plus" href="#/payments/new">
      Create payment
    </LinkButton>
  );
  let content: ReactNode;
  if (path === "/payments/new")
    content = (
      <CreatePage
        state={state}
        tenant={tenant}
        merchant={merchant}
        onCreated={(s, id, replay) => {
          setState(s);
          navTo(`/payments/${id}`);
          toast.show({
            tone: "success",
            title: replay
              ? "Existing intent returned"
              : "Payment intent created",
          });
        }}
      />
    );
  else if (section === "payments" && path.split("/")[2])
    content = selected ? (
      <PaymentDetail
        payment={selected}
        state={state}
        onScenario={(scenario) =>
          mutate(
            (s) => simulate(s, selected.id, tenant, scenario),
            "Sandbox event recorded",
          )
        }
        onCancel={() =>
          confirm(
            "Cancel payment intent?",
            <>
              Cancel {selected.reference} for <Amount value={selected.amount} />
              . Broadcast transfers cannot be reversed; monitoring continues.
            </>,
            "Cancel payment intent",
            () =>
              mutate(
                (s) => cancelIntent(s, selected.id, tenant),
                "Payment intent cancelled",
              ),
            true,
          )
        }
      />
    ) : (
      <>
        {header(
          "Payment unavailable",
          "This record does not exist in the active tenant.",
        )}
        <EmptyState
          title="Access denied"
          icon="lock"
          actions={
            <LinkButton href="#/payments">Return to payments</LinkButton>
          }
        >
          Switch back to the owning tenant to inspect this payment.
        </EmptyState>
      </>
    );
  else if (section === "overview") {
    const accepted = decimal(
      payments
        .filter((p) => p.status === "succeeded")
        .reduce((sum, p) => sum + units(p.amount), 0n),
    );
    const exceptions = payments.filter(
      (p) => p.flags.length || p.hold || p.settlement === "mismatch",
    );
    content = (
      <>
        {header(
          "Payment operations",
          "A clear view of your collections, routes and settlement exceptions.",
          newButton,
        )}
        <KpiRow columns={4}>
          <Stat
            label="Accepted volume"
            value={<Money amount={accepted} currency="USD" compact />}
          />
          <Stat label="Payment intents" value={String(payments.length)} />
          <Stat label="Needs review" value={String(exceptions.length)} />
          <Stat
            label="Available providers"
            value={`${state.providers.filter((p) => p.enabled && p.healthy).length} / 2`}
          />
        </KpiRow>
        <Alert
          tone="warning"
          title={`${exceptions.length} records need an operator review`}
          actions={
            <LinkButton href="#/reconciliation" variant="secondary">
              Review exceptions
            </LinkButton>
          }
        >
          Investigate partial funding, late transfers and settlement differences
          before closing the books.
        </Alert>
        <div className="dashboard-grid">
          <ChartCard
            title="Collection mix"
            unit="Requested USD · current sandbox"
            summary="Requested payment volume by blockchain. This is seeded demonstration data."
            legend={[{ label: "USDC", series: 0 }]}
            table={
              <DataTable
                aria-label="Collection mix data"
                rows={(["polygon", "ethereum"] as Network[]).map((n) => ({
                  network: n,
                  amount: decimal(
                    payments
                      .filter((p) => p.network === n)
                      .reduce((s, p) => s + units(p.amount), 0n),
                  ),
                }))}
                rowKey={(r) => r.network}
                columns={[
                  { id: "chain", header: "Network", cell: (r) => r.network },
                  {
                    id: "amount",
                    header: "Requested USD",
                    numeric: true,
                    cell: (r) => <Amount value={r.amount} />,
                  },
                ]}
              />
            }
          >
            <BarChart
              categories={["Polygon", "Ethereum"]}
              series={[
                {
                  name: "USDC",
                  values: (["polygon", "ethereum"] as Network[]).map((n) =>
                    Number(
                      decimal(
                        payments
                          .filter((p) => p.network === n)
                          .reduce((s, p) => s + units(p.amount), 0n),
                      ),
                    ),
                  ),
                },
              ]}
            />
          </ChartCard>
          <Card>
            <CardHeader
              title="Provider readiness"
              sub="Sandbox adapters · policy v3"
            />
            <CardBody>
              <div className="bv-stack">
                {state.providers.map((p) => (
                  <DescriptionList
                    key={p.id}
                    columns={1}
                    items={[
                      {
                        label: p.name,
                        value: (
                          <StatusBadge
                            status={
                              p.enabled && p.healthy ? "Active" : "Inactive"
                            }
                          />
                        ),
                      },
                      { label: "Response latency", value: p.latency },
                    ]}
                  />
                ))}
                <LinkButton href="#/providers" variant="secondary">
                  Manage providers
                </LinkButton>
              </div>
            </CardBody>
          </Card>
        </div>
        <CardHeader
          title="Recent payments"
          sub="Payment acceptance and settlement are tracked independently"
          actions={
            <LinkButton href="#/payments" variant="link">
              View all payments
            </LinkButton>
          }
        />
        {table(filtered.slice(0, 5))}
      </>
    );
  } else if (section === "payments")
    content = (
      <>
        {header(
          "Payments",
          "Search payment intents and follow each transfer from detection to finality.",
          newButton,
        )}
        {table()}
        <div className="bv-cluster">
          <SelectField
            label="Sandbox list state"
            value={tableState}
            onChange={(e) => setTableState(e.target.value)}
          >
            {["ready", "loading", "empty", "error"].map((s) => (
              <option key={s}>{s}</option>
            ))}
          </SelectField>
          <p className="bv-body-sm page-note">
            Preview loading, empty and recovery states without changing records.
          </p>
        </div>
      </>
    );
  else if (section === "routing") content = <RoutingPage state={state} />;
  else if (section === "providers")
    content = (
      <>
        {header(
          "Providers",
          "Two simulated adapters with overlapping USDC coverage on Ethereum and Polygon.",
        )}
        <Alert title="Safe fallback boundary">
          Changes affect future instructions. Issued deposit addresses never
          change. Unknown execution outcomes block a second execution.
        </Alert>
        <DataTable
          aria-label="Execution providers"
          rows={state.providers}
          rowKey={(p) => p.id}
          columns={[
            {
              id: "name",
              header: "Provider",
              rowHeader: true,
              cell: (p) => p.name,
            },
            {
              id: "coverage",
              header: "Capabilities",
              cell: () => "USDC · chain 1 / 137",
            },
            {
              id: "status",
              header: "Operability",
              cell: (p) => (
                <StatusBadge
                  status={p.enabled && p.healthy ? "Active" : "Inactive"}
                />
              ),
            },
            {
              id: "fee",
              header: "Estimated fee",
              numeric: true,
              cell: (p) => <Amount value={p.fee} />,
            },
            { id: "latency", header: "Latency", cell: (p) => p.latency },
            {
              id: "control",
              header: "Controls",
              cell: (p) => (
                <div className="bv-stack">
                  <Switch
                    label={`Available: ${p.name}`}
                    checked={p.healthy}
                    onChange={(e) =>
                      mutate(
                        (s) => ({
                          ...s,
                          providers: s.providers.map((x) =>
                            x.id === p.id
                              ? { ...x, healthy: e.target.checked }
                              : x,
                          ),
                          audit: [
                            entry(
                              "Provider availability changed",
                              `${tenant} · ${p.name}`,
                            ),
                            ...s.audit,
                          ],
                        }),
                        "Provider health updated",
                      )
                    }
                  />
                  <Button
                    variant="secondary"
                    onClick={() =>
                      confirm(
                        p.enabled ? "Stop provider?" : "Restore provider?",
                        `${p.name}: ${p.enabled ? "future routes will be blocked. Existing transfers remain monitored." : "future eligible routes will be enabled."}`,
                        p.enabled ? "Stop provider" : "Restore provider",
                        () =>
                          mutate(
                            (s) => ({
                              ...s,
                              providers: s.providers.map((x) =>
                                x.id === p.id
                                  ? { ...x, enabled: !x.enabled }
                                  : x,
                              ),
                              audit: [
                                entry(
                                  p.enabled
                                    ? "Emergency provider stop"
                                    : "Provider restored",
                                  `${tenant} · ${p.name}`,
                                ),
                                ...s.audit,
                              ],
                            }),
                            "Provider policy updated",
                          ),
                        p.enabled,
                      )
                    }
                  >
                    {p.enabled ? "Emergency stop" : "Restore provider"}
                  </Button>
                </div>
              ),
            },
          ]}
          empty="No providers configured."
        />
      </>
    );
  else if (section === "reconciliation" || section === "settlements") {
    const reconciliation = section === "reconciliation";
    const rows = payments.filter((p) =>
      reconciliation
        ? p.flags.length || p.hold || p.settlement === "mismatch"
        : p.status === "succeeded",
    );
    const exportRows = () =>
      download(
        `stablerail-${section}.csv`,
        exportCsv(
          [
            "Intent",
            "Merchant",
            "Provider",
            "Network",
            "Requested USD",
            "Received USDC",
            "Settlement",
            "Exceptions",
          ],
          rows.map((p) => [
            p.id,
            p.merchant,
            p.provider,
            p.network,
            p.amount,
            p.received,
            p.settlement,
            p.flags.join("; "),
          ]),
        ),
      );
    content = (
      <>
        {header(
          reconciliation ? "Reconciliation" : "Settlements",
          reconciliation
            ? "Trace every mismatch back to its payment and original financial evidence."
            : "Accepted payments awaiting or completing external provider settlement.",
          <Button variant="primary" icon="download" onClick={exportRows}>
            Export {rows.length} rows
          </Button>,
        )}
        <Alert
          title={
            reconciliation
              ? "Corrections preserve original evidence"
              : "Acceptance is separate from settlement"
          }
        >
          {reconciliation
            ? "Review a payment before recording a compensating correction. Original records and routing decisions remain inspectable."
            : "No customer funds are held by StableRail. Statement outcomes below are simulated provider evidence."}
        </Alert>
        <StatementImport
          onImport={(csv) =>
            confirm(
              "Import sandbox statement?",
              "Validate every row against the active tenant and append matching or mismatch evidence. Original financial events are retained.",
              "Import statement",
              () =>
                mutate(
                  (s) => importStatement(s, tenant, csv),
                  "Statement imported",
                ),
            )
          }
          exampleId={
            payments.find(
              (p) => p.status === "succeeded" && !p.flags.length && !p.hold,
            )?.id || "pi_example"
          }
        />
        <DataTable
          aria-label={
            reconciliation ? "Reconciliation exceptions" : "Settlement records"
          }
          rows={rows}
          rowKey={(p) => p.id}
          empty={
            <EmptyState title="No records to review" icon="circle-check">
              New exceptions and settlements appear as payments progress.
            </EmptyState>
          }
          columns={[
            {
              id: "reference",
              header: "Payment",
              rowHeader: true,
              cell: (p) => (
                <LinkButton href={`#/payments/${p.id}`} variant="link">
                  {p.reference}
                </LinkButton>
              ),
            },
            {
              id: "expected",
              header: "Requested USD",
              numeric: true,
              cell: (p) => <Amount value={p.amount} />,
            },
            {
              id: "received",
              header: "Received USDC",
              numeric: true,
              cell: (p) => <span className="bv-num">{p.received}</span>,
            },
            {
              id: "settlement",
              header: "Settlement",
              cell: (p) => (
                <StatusBadge
                  status={
                    p.settlement === "settled"
                      ? "Settled"
                      : p.settlement === "mismatch"
                        ? "In review"
                        : "Processing"
                  }
                />
              ),
            },
            {
              id: "issue",
              header: "Evidence",
              cell: (p) =>
                p.settlement === "mismatch"
                  ? "Statement amount mismatch"
                  : p.flags.join(" · ") || "Awaiting provider statement",
            },
            {
              id: "action",
              header: "Action",
              cell: (p) => (
                <Button
                  variant="secondary"
                  onClick={() =>
                    confirm(
                      "Record provider statement?",
                      <>
                        Record sandbox statement for {p.reference},{" "}
                        <Amount value={p.amount} />.{" "}
                        {p.settlement === "mismatch"
                          ? "Append a correction matching the expected amount; preserve the original mismatch."
                          : "Mark external settlement complete only after the simulated statement matches."}
                      </>,
                      "Record matched statement",
                      () =>
                        mutate((s) => {
                          if (
                            p.status !== "succeeded" ||
                            p.hold ||
                            p.flags.length
                          )
                            throw new Error(
                              "Payment finality and exception review are required before settlement.",
                            );
                          return {
                            ...s,
                            intents: s.intents.map((i) =>
                              i.id === p.id
                                ? {
                                    ...i,
                                    settlement: "settled",
                                    events: [
                                      entry(
                                        "Matched provider statement recorded",
                                        `Original state ${i.settlement}; compensating evidence appended.`,
                                      ),
                                      ...i.events,
                                    ],
                                  }
                                : i,
                            ),
                            audit: [
                              entry(
                                "Settlement statement matched",
                                `${tenant} · ${p.id}`,
                              ),
                              ...s.audit,
                            ],
                          };
                        }, "Statement matched"),
                    )
                  }
                >
                  Record statement
                </Button>
              ),
            },
          ]}
        />
      </>
    );
  } else if (section === "webhooks")
    content = (
      <WebhookPage
        key={tenant}
        state={state}
        tenant={tenant}
        onSave={(endpoint) =>
          mutate(
            (s) => ({
              ...s,
              endpoints: { ...s.endpoints, [tenant]: endpoint },
              audit: [
                entry("Sandbox endpoint updated", `${tenant} · ${endpoint}`),
                ...s.audit,
              ],
            }),
            "Endpoint saved",
          )
        }
        onReplay={(h) =>
          confirm(
            "Replay webhook event?",
            `Replay ${h.id} to the sandbox endpoint. The stable event ID is retained; the receiver must deduplicate. No network request is sent.`,
            "Replay webhook",
            () =>
              mutate(
                (s) => replayHook(s, h.id, tenant),
                "Sandbox replay delivered",
              ),
          )
        }
      />
    );
  else if (section === "administration")
    content = (
      <>
        {header(
          "Merchants & access",
          "Environment-scoped configuration for your active tenant.",
        )}
        <Card>
          <CardHeader title={merchant} sub="Sandbox merchant · non-custodial" />
          <CardBody>
            <DescriptionList
              items={[
                { label: "Tenant", value: tenant, mono: true },
                { label: "KYB", value: <StatusBadge status="Approved" /> },
                { label: "Assets", value: "USDC (6 decimals)" },
                { label: "Networks", value: "Ethereum (1), Polygon (137)" },
                {
                  label: "Transaction limit",
                  value: <Amount value="100000" />,
                },
                {
                  label: "Policy version",
                  value: "pol_2026_10_v3",
                  mono: true,
                },
              ]}
            />
          </CardBody>
        </Card>
        <Card>
          <CardHeader
            title="API credentials"
            sub="Illustrative identifiers only; these cannot authenticate requests"
            actions={
              <Button
                variant="primary"
                onClick={() =>
                  mutate(
                    (s) => ({
                      ...s,
                      keys: [
                        {
                          id: `sk_test_${tenant}_••••${uid("key").slice(-4)}`,
                          tenant,
                          label: "Sandbox integration",
                          revoked: false,
                        },
                        ...s.keys,
                      ],
                      audit: [
                        entry("Sandbox credential created", tenant),
                        ...s.audit,
                      ],
                    }),
                    "Sandbox credential created",
                  )
                }
              >
                Create sandbox key
              </Button>
            }
          />
          <CardBody>
            <DataTable
              aria-label="API credentials"
              rows={state.keys.filter((k) => k.tenant === tenant)}
              rowKey={(k) => k.id}
              empty="No credentials. Create a sandbox key."
              columns={[
                {
                  id: "key",
                  header: "Masked identifier",
                  rowHeader: true,
                  cell: (k) => <span className="bv-mono">{k.id}</span>,
                },
                {
                  id: "scope",
                  header: "Scope",
                  cell: () => "Sandbox · payment intents:write",
                },
                {
                  id: "status",
                  header: "State",
                  cell: (k) => (
                    <StatusBadge status={k.revoked ? "Inactive" : "Active"} />
                  ),
                },
                {
                  id: "action",
                  header: "Action",
                  cell: (k) =>
                    !k.revoked && (
                      <Button
                        onClick={() =>
                          confirm(
                            "Revoke sandbox key?",
                            `${k.id} will become inactive. This prototype has no authentication server.`,
                            "Revoke sandbox key",
                            () =>
                              mutate(
                                (s) => ({
                                  ...s,
                                  keys: s.keys.map((x) =>
                                    x.id === k.id ? { ...x, revoked: true } : x,
                                  ),
                                  audit: [
                                    entry(
                                      "Sandbox credential revoked",
                                      `${tenant} · ${k.id}`,
                                    ),
                                    ...s.audit,
                                  ],
                                }),
                                "Sandbox key revoked",
                              ),
                            true,
                          )
                        }
                      >
                        Revoke key
                      </Button>
                    ),
                },
              ]}
            />
          </CardBody>
        </Card>
      </>
    );
  else if (section === "audit")
    content = (
      <>
        {header(
          "Audit trail",
          "Append-only sandbox history. Production requires server-side immutable storage.",
        )}
        <Card>
          <CardBody>
            <Timeline
              events={state.audit
                .filter(
                  (e) =>
                    e.detail.includes(tenant) ||
                    e.title === "Sandbox initialized",
                )
                .map((e) => ({
                  id: e.id,
                  title: e.title,
                  meta: e.detail,
                  time: `${displayTime(e.at)}`,
                  isoTime: e.at,
                }))}
            />
          </CardBody>
        </Card>
      </>
    );
  else
    content = (
      <>
        {header("Page unavailable", "Choose an area from the navigation.")}
        <EmptyState
          title="Page not found"
          actions={
            <LinkButton href="#/overview">Return to overview</LinkButton>
          }
        />
      </>
    );
  return (
    <AppShell
      sidebar={{
        productName: "StableRail",
        product: "Gateway",
        version: "0.1",
        collapsedMark: "SR",
        groups,
        user: { name: "Alex Morgan", role: "Sandbox administrator" },
      }}
      topbar={{
        searchPlaceholder: "Find payment references…",
        onSearch: (q) => {
          setQuery(q);
          if (section !== "payments") navTo("/payments");
        },
        onHelp: () =>
          toast.show({
            title: "Sandbox guide",
            body: "Create an intent, simulate transfers, inspect routing evidence, replay webhooks, then match a settlement statement.",
          }),
        notificationCount: payments.filter((p) => p.flags.length).length,
        onNotifications: () => navTo("/reconciliation"),
        user: (
          <Button
            variant="ghost"
            icon="eye"
            iconOnly
            aria-label="Toggle theme"
            onClick={toggleTheme}
          />
        ),
      }}
    >
      <div className="bv-cluster">
        <Badge tone="discovery" icon="info">
          Sandbox
        </Badge>
        <span className="bv-body-sm page-note">
          Simulated funds · no live execution
        </span>
        <SelectField
          label="Active tenant"
          value={tenant}
          onChange={(e) => {
            setTenant(e.target.value);
            setQuery("");
            setFilter("all");
          }}
        >
          <option value="atlas">Atlas PSP</option>
          <option value="harbor">Harbor PSP</option>
        </SelectField>
      </div>
      {content}
      <p className="bv-body-sm page-note">
        StableRail sandbox · USDC on Ethereum & Polygon · All activity is stored
        in this browser.
      </p>
      <ConfirmDialog
        open={!!confirmation}
        title={confirmation?.title || ""}
        confirmLabel={confirmation?.label || "Confirm action"}
        tone={confirmation?.danger ? "danger" : "primary"}
        onClose={() => setConfirmation(null)}
        onConfirm={() => {
          confirmation?.action();
          setConfirmation(null);
        }}
      >
        {confirmation?.body}
      </ConfirmDialog>
    </AppShell>
  );
}
function CreatePage({
  state,
  tenant,
  merchant,
  onCreated,
}: {
  state: State;
  tenant: string;
  merchant: string;
  onCreated: (state: State, id: string, replay: boolean) => void;
}) {
  const [reference, setReference] = useState("");
  const [amount, setAmount] = useState("250.00");
  const [network, setNetwork] = useState<Network>("polygon");
  const [key, setKey] = useState(() => uid("order"));
  const [minutes, setMinutes] = useState("15");
  const [error, setError] = useState("");
  const [confirming, setConfirming] = useState(false);
  const validate = () => {
    try {
      createIntent(state, {
        tenant,
        merchant,
        reference,
        amount,
        network,
        key,
        minutes,
      });
      setError("");
      return true;
    } catch (e) {
      setError((e as Error).message);
      return false;
    }
  };
  return (
    <>
      <PageHeader
        breadcrumbs={[
          { label: "Payments", href: "#/payments" },
          { label: "Create payment" },
        ]}
        title="Create payment"
        description="Issue exact deposit instructions after mandatory route checks."
      />
      <Card className="form-width">
        <CardBody>
          <FormLayout
            noValidate
            onSubmit={(e) => {
              e.preventDefault();
              if (validate()) setConfirming(true);
            }}
          >
            {error && (
              <Alert tone="danger" title="Payment intent could not be created">
                {error}
              </Alert>
            )}
            <FormSection title="Payment details">
              <FormGrid>
                <TextField
                  label="Merchant reference"
                  required
                  value={reference}
                  onChange={(e) => setReference(e.target.value)}
                  onBlur={() => {
                    if (reference.trim() === "")
                      setError("Enter a merchant reference.");
                  }}
                  placeholder="ORD-98431"
                />
                <MoneyField
                  label="Requested amount"
                  currency="USD"
                  required
                  value={amount}
                  onValueChange={setAmount}
                  onBlur={() => {
                    try {
                      units(amount);
                    } catch (e) {
                      setError((e as Error).message);
                    }
                  }}
                  help="Sandbox fixed quote: 1 USD = 1 USDC. Maximum USD 100,000."
                />
                <TextField
                  label="Idempotency key"
                  required
                  mono
                  value={key}
                  onChange={(e) => setKey(e.target.value)}
                  help="Reuse the same key and payload to retrieve the original intent."
                />
                <TextField
                  label="Expires in minutes"
                  required
                  inputMode="numeric"
                  value={minutes}
                  onChange={(e) => setMinutes(e.target.value)}
                  help="Between 1 and 1,440 minutes."
                />
              </FormGrid>
              <RadioGroup
                legend="Payment network"
                name="network"
                value={network}
                onValueChange={(v) => setNetwork(v as Network)}
                options={[
                  { value: "polygon", label: "Polygon · chain 137" },
                  { value: "ethereum", label: "Ethereum · chain 1" },
                ]}
              />
            </FormSection>
            <FormSection title="Acceptance policy">
              <DescriptionList
                items={[
                  { label: "Asset", value: "Native USDC · 6 decimals" },
                  {
                    label: "Finality",
                    value:
                      network === "ethereum"
                        ? "12 confirmations"
                        : "64 confirmations",
                  },
                  {
                    label: "Token contract",
                    value: TOKEN[network],
                    mono: true,
                    wide: true,
                  },
                  {
                    label: "Tolerance",
                    value: "0%; partial funding requires review",
                  },
                  {
                    label: "Compliance",
                    value: "Mandatory checks fail closed",
                  },
                ]}
              />
            </FormSection>
            <FormFooter>
              <LinkButton variant="ghost" href="#/payments">
                Cancel
              </LinkButton>
              <Button type="submit" variant="primary">
                Create payment intent
              </Button>
            </FormFooter>
          </FormLayout>
        </CardBody>
      </Card>
      <ConfirmDialog
        open={confirming}
        title="Issue payment instructions?"
        confirmLabel="Issue instructions"
        onClose={() => setConfirming(false)}
        onConfirm={() => {
          try {
            const result = createIntent(state, {
              tenant,
              merchant,
              reference,
              amount,
              network,
              key,
              minutes,
            });
            setConfirming(false);
            onCreated(result.state, result.id, result.replay);
          } catch (e) {
            setConfirming(false);
            setError((e as Error).message);
          }
        }}
      >
        Create {reference} for <Amount value={amount || "0"} /> payable in USDC
        on {network}. Instructions are simulated; no transfer is submitted.
      </ConfirmDialog>
    </>
  );
}
function PaymentDetail({
  payment: p,
  state,
  onScenario,
  onCancel,
}: {
  payment: Intent;
  state: State;
  onScenario: (s: Scenario) => void;
  onCancel: () => void;
}) {
  const [tab, setTab] = useState("overview");
  const [scenario, setScenario] = useState<Scenario>("detect");
  const scenarioOptions: Record<Scenario, string> = {
    detect: "Detect valid transfer",
    confirm: "Advance confirmations",
    finalize: "Reach chain finality",
    partial: "Record underpayment",
    excess: "Record overpayment",
    expire: "Expire intent",
    late: "Record late transfer",
    wrong_token: "Record wrong token",
    duplicate: "Replay duplicate event",
    reorg: "Simulate chain reorganization",
    hold: "Apply compliance hold",
    timeout: "Simulate provider timeout",
    reconcile_timeout: "Reconcile original outcome",
  };
  return (
    <>
      <PageHeader
        breadcrumbs={[
          { label: "Payments", href: "#/payments" },
          { label: p.reference },
        ]}
        title={p.reference}
        description={`${p.merchant} · ${p.id}`}
        meta={<PaymentState payment={p} />}
        actions={
          <Button variant="secondary" onClick={onCancel}>
            Cancel intent
          </Button>
        }
      />
      {(p.hold || p.unknownOutcome || p.flags.length > 0) && (
        <Alert
          tone="warning"
          title={
            p.hold
              ? "Compliance hold · execution blocked"
              : p.unknownOutcome
                ? "Unknown provider outcome · fallback blocked"
                : "Payment requires review"
          }
        >
          {p.hold
            ? "CASE-1042: mandatory screening is unavailable. Escalate to compliance; the sandbox cannot release holds."
            : p.unknownOutcome
              ? "Reconcile the original provider acknowledgement before attempting another execution."
              : p.flags.join(" · ")}
        </Alert>
      )}
      <KpiRow columns={3}>
        <Stat label="Requested" value={<Amount value={p.amount} />} />
        <Stat
          label="Received USDC"
          value={<span className="bv-num">{p.received}</span>}
        />
        <Stat
          label="Settlement"
          value={
            <StatusBadge
              status={
                p.settlement === "settled"
                  ? "Settled"
                  : p.settlement === "mismatch"
                    ? "In review"
                    : "Processing"
              }
            />
          }
        />
      </KpiRow>
      <Tabs
        aria-label="Payment sections"
        idPrefix="payment"
        value={tab}
        onValueChange={setTab}
        items={[
          { id: "overview", label: "Overview" },
          { id: "transfers", label: "Transfers", count: p.transfers.length },
          { id: "routing", label: "Routing evidence" },
          { id: "activity", label: "Event timeline" },
        ]}
      />
      {tab === "overview" && (
        <div className="record-grid">
          <div className="bv-stack">
            <Card>
              <CardHeader
                title="Payment instructions"
                sub="Fixed sandbox quote · 1 USD = 1 USDC"
              />
              <CardBody>
                <DescriptionList
                  items={[
                    {
                      label: "Deposit address",
                      value: <span className="break-id">{DEPOSIT}</span>,
                      mono: true,
                      wide: true,
                    },
                    {
                      label: "Network",
                      value: `${p.network} · chain ${CHAIN[p.network]}`,
                    },
                    { label: "Asset", value: "USDC · 6 decimals" },
                    {
                      label: "Token contract",
                      value: (
                        <span className="break-id">{TOKEN[p.network]}</span>
                      ),
                      mono: true,
                      wide: true,
                    },
                    { label: "Expires", value: `${displayTime(p.expiresAt)}` },
                    {
                      label: "Confirmation policy",
                      value:
                        p.network === "ethereum"
                          ? "12 confirmations"
                          : "64 confirmations",
                    },
                  ]}
                />
              </CardBody>
            </Card>
            <Card>
              <CardHeader
                title="Sandbox event simulator"
                sub="Exercise the happy path and exception recovery without moving funds"
              />
              <CardBody>
                <div className="bv-stack">
                  <SelectField
                    label="Scenario"
                    value={scenario}
                    onChange={(e) => setScenario(e.target.value as Scenario)}
                  >
                    {Object.entries(scenarioOptions).map(([value, label]) => (
                      <option value={value} key={value}>
                        {label}
                      </option>
                    ))}
                  </SelectField>
                  <Button
                    variant="primary"
                    onClick={() => onScenario(scenario)}
                  >
                    Run scenario
                  </Button>
                  <p className="bv-body-sm page-note">
                    Happy path: detect → advance confirmations → reach chain
                    finality. Expired or cancelled intents retain late-transfer
                    evidence.
                  </p>
                </div>
              </CardBody>
            </Card>
          </div>
          <div className="bv-stack">
            <Card>
              <CardHeader title="Operational context" />
              <CardBody>
                <DescriptionList
                  columns={1}
                  items={[
                    { label: "Provider", value: p.provider },
                    {
                      label: "Compliance",
                      value: (
                        <StatusBadge status={p.hold ? "On hold" : "Approved"} />
                      ),
                    },
                    { label: "Policy version", value: p.policy, mono: true },
                    { label: "Created", value: `${displayTime(p.createdAt)}` },
                    {
                      label: "Exception flags",
                      value: p.flags.join(" · ") || "None",
                    },
                    {
                      label: "Submission outcome",
                      value: p.unknownOutcome
                        ? "Unknown · execution locked"
                        : "Known",
                    },
                  ]}
                />
              </CardBody>
            </Card>
            <Card>
              <CardHeader
                title="Latest events"
                actions={
                  <Button variant="ghost" onClick={() => setTab("activity")}>
                    View timeline
                  </Button>
                }
              />
              <CardBody>
                <Timeline
                  events={p.events.slice(0, 3).map((e) => ({
                    id: e.id,
                    title: e.title,
                    meta: e.detail,
                    time: `${displayTime(e.at)}`,
                  }))}
                />
              </CardBody>
            </Card>
          </div>
        </div>
      )}
      {tab === "transfers" && (
        <DataTable
          aria-label="Blockchain transfers"
          rows={p.transfers}
          rowKey={(t) => t.id}
          empty={
            <EmptyState title="No transfers detected" icon="wallet">
              Use the simulator to observe a valid or unexpected transfer.
            </EmptyState>
          }
          columns={[
            {
              id: "hash",
              header: "Transaction / event",
              rowHeader: true,
              cell: (t) => (
                <span className="bv-mono break-id">
                  {t.hash}:{t.logIndex}
                </span>
              ),
            },
            {
              id: "amount",
              header: "USDC units",
              numeric: true,
              cell: (t) => <span className="bv-mono">{t.amount}</span>,
            },
            {
              id: "contract",
              header: "Token contract",
              cell: (t) => (
                <span className="bv-mono break-id">{t.contract}</span>
              ),
            },
            {
              id: "confirmation",
              header: "Confirmations",
              numeric: true,
              cell: (t) => t.confirmations,
            },
            {
              id: "credit",
              header: "Recognition",
              cell: (t) => (
                <StatusBadge status={t.credited ? "Approved" : "In review"} />
              ),
            },
          ]}
        />
      )}
      {tab === "routing" && (
        <>
          <Alert title="Historical decision · immutable snapshot">
            Selected {p.provider} using {p.policy} for{" "}
            <Amount value={p.amount} /> on chain {CHAIN[p.network]}. Current
            provider changes do not rewrite this evidence.
          </Alert>
          <RouteTable rows={p.routeEvidence} />
          <Card>
            <CardHeader
              title="Current eligibility recheck"
              sub="For inspection only. Cannot redirect an issued or broadcast transfer."
            />
            <CardBody>
              <RouteTable rows={routes(state, p.amount, p.hold)} />
            </CardBody>
          </Card>
        </>
      )}
      {tab === "activity" && (
        <Card>
          <CardHeader
            title="Complete event timeline"
            sub="Newest first · original observations are preserved"
          />
          <CardBody>
            <Timeline
              events={p.events.map((e) => ({
                id: e.id,
                title: e.title,
                meta: e.detail,
                time: `${displayTime(e.at)}`,
                isoTime: e.at,
              }))}
            />
          </CardBody>
        </Card>
      )}
    </>
  );
}
function RouteTable({ rows }: { rows: ReturnType<typeof routes> }) {
  return (
    <DataTable
      aria-label="Route eligibility"
      rows={rows}
      rowKey={(r) => r.provider}
      empty="No candidate providers."
      columns={[
        {
          id: "provider",
          header: "Provider",
          rowHeader: true,
          cell: (r) => r.provider,
        },
        {
          id: "eligibility",
          header: "Mandatory gates",
          cell: (r) => (
            <StatusBadge status={r.eligible ? "Approved" : "Failed"} />
          ),
        },
        {
          id: "reason",
          header: "Decision rationale",
          cell: (r) =>
            r.eligible
              ? "Asset, chain, limits, screening and operability pass"
              : r.reasons.join(" · "),
        },
        {
          id: "fee",
          header: "Estimated fee USD",
          numeric: true,
          cell: (r) => <Amount value={r.fee} />,
        },
        {
          id: "rank",
          header: "Policy priority",
          numeric: true,
          cell: (r) => r.priority,
        },
      ]}
    />
  );
}
function RoutingPage({ state }: { state: State }) {
  const [amount, setAmount] = useState("250.00");
  const [hold, setHold] = useState(false);
  const [result, setResult] = useState(() => routes(state, "250.00"));
  const [error, setError] = useState("");
  return (
    <>
      <PageHeader
        title="Routing policies"
        description="Mandatory eligibility first. Deterministic priority second."
        meta={<Badge>pol_2026_10_v3</Badge>}
      />
      <Card>
        <CardHeader
          title="Evaluate a route"
          sub="USDC on Ethereum / Polygon · same overlapping provider capabilities"
        />
        <CardBody>
          <FormLayout
            onSubmit={(e) => {
              e.preventDefault();
              try {
                if (units(amount) <= 0n)
                  throw new Error("Enter an amount greater than zero.");
                setResult(routes(state, amount, hold));
                setError("");
              } catch (e) {
                setError((e as Error).message);
              }
            }}
          >
            <FormGrid>
              <MoneyField
                currency="USD"
                label="Payment amount"
                value={amount}
                onValueChange={setAmount}
                error={error}
              />
              <RadioGroup
                legend="Mandatory screening"
                name="screening"
                value={hold ? "unavailable" : "clear"}
                onValueChange={(v) => setHold(v === "unavailable")}
                options={[
                  { label: "Clear", value: "clear" },
                  { label: "Unavailable / blocked", value: "unavailable" },
                ]}
              />
            </FormGrid>
            <FormFooter>
              <Button type="submit" variant="primary">
                Evaluate routes
              </Button>
            </FormFooter>
          </FormLayout>
        </CardBody>
      </Card>
      <Alert
        title={
          result.some((r) => r.eligible)
            ? `Selected: ${result.find((r) => r.eligible)?.provider}`
            : "No eligible route · fail closed"
        }
      >
        The cheapest provider cannot override compliance or hard constraints.
        Simulation does not execute a payment.
      </Alert>
      <RouteTable rows={result} />
      <Card>
        <CardHeader title="Policy controls" />
        <CardBody>
          <DescriptionList
            items={[
              {
                label: "Hard gates",
                value:
                  "Approved USDC contract, chain, jurisdiction, screening, limits, liquidity and provider operability",
                wide: true,
              },
              {
                label: "Ranking",
                value: "Bridgeway first; Orbit Payments second",
              },
              {
                label: "Fallback",
                value:
                  "Only before instructions or after known outcome reconciliation",
              },
              { label: "Version", value: "pol_2026_10_v3", mono: true },
              {
                label: "Scope",
                value: "Sandbox · approved demonstration corridor",
              },
            ]}
          />
        </CardBody>
      </Card>
    </>
  );
}
function WebhookPage({
  state,
  tenant,
  onSave,
  onReplay,
}: {
  state: State;
  tenant: string;
  onSave: (endpoint: string) => void;
  onReplay: (h: HookEvent) => void;
}) {
  const [endpoint, setEndpoint] = useState(state.endpoints[tenant]);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const hooks = state.hooks.filter((h) => h.tenant === tenant);
  return (
    <>
      <PageHeader
        title="Webhooks"
        description="Inspect delivery attempts and replay stable event envelopes."
      />
      <Card>
        <CardHeader
          title="Sandbox endpoint"
          sub="No HTTP requests are sent from this prototype"
        />
        <CardBody>
          <FormLayout
            onSubmit={(e) => {
              e.preventDefault();
              try {
                const url = new URL(endpoint);
                if (url.protocol !== "https:") throw new Error();
                setError("");
                onSave(endpoint);
              } catch {
                setError("Enter a valid HTTPS endpoint.");
              }
            }}
          >
            <TextField
              label="Endpoint URL"
              value={endpoint}
              onChange={(e) => setEndpoint(e.target.value)}
              error={error}
              required
            />
            <FormFooter>
              <Button variant="primary" type="submit">
                Save endpoint
              </Button>
            </FormFooter>
          </FormLayout>
          <DescriptionList
            items={[
              {
                label: "Delivery model",
                value: "At least once · stable event IDs",
              },
              {
                label: "Signature contract",
                value:
                  "HMAC-SHA256 · timestamp + event envelope (server implementation pending)",
              },
              {
                label: "Retry policy",
                value: "30s, 2m, 10m, then dead-letter (simulated)",
              },
              { label: "Signing secret", value: "Not generated in browser" },
            ]}
          />
        </CardBody>
      </Card>
      <DataTable
        aria-label="Webhook deliveries"
        rows={hooks}
        rowKey={(h) => h.id}
        empty={
          <EmptyState title="No webhook events" icon="send">
            Simulate a payment event to generate a delivery.
          </EmptyState>
        }
        columns={[
          {
            id: "event",
            header: "Stable event ID",
            rowHeader: true,
            cell: (h) => (
              <Button variant="link" onClick={() => setSelected(h.id)}>
                {h.id}
              </Button>
            ),
          },
          {
            id: "type",
            header: "Type",
            cell: (h) => <span className="bv-mono">{h.type}</span>,
          },
          {
            id: "status",
            header: "Delivery",
            cell: (h) => (
              <StatusBadge
                status={h.status === "delivered" ? "Approved" : "Failed"}
              />
            ),
          },
          {
            id: "attempts",
            header: "Attempts",
            numeric: true,
            cell: (h) => h.attempts,
          },
          {
            id: "action",
            header: "Recovery",
            cell: (h) => (
              <Button variant="secondary" onClick={() => onReplay(h)}>
                Replay event
              </Button>
            ),
          },
        ]}
      />
      {selected && (
        <Card>
          <CardHeader title="Delivery attempt history" sub={selected} />
          <CardBody>
            <Timeline
              events={(hooks.find((h) => h.id === selected)?.history || []).map(
                (e) => ({
                  id: e.id,
                  title: e.title,
                  meta: e.detail,
                  time: `${displayTime(e.at)}`,
                }),
              )}
            />
          </CardBody>
        </Card>
      )}
    </>
  );
}

function StatementImport({
  onImport,
  exampleId,
}: {
  onImport: (csv: string) => void;
  exampleId: string;
}) {
  const [csv, setCsv] = useState("");
  return (
    <Card>
      <CardHeader
        title="Import provider statement"
        sub="Sandbox CSV · up to 100 rows · no quoted fields"
      />
      <CardBody>
        <FormLayout
          onSubmit={(e) => {
            e.preventDefault();
            onImport(csv);
          }}
        >
          <TextareaField
            label="Statement CSV"
            value={csv}
            onChange={(e) => setCsv(e.target.value)}
            placeholder={`intent_id,amount_usdc,provider_reference\n${exampleId},250.000000,STMT-001`}
            help="Paste exact intent IDs, six-decimal USDC amounts and unique provider references. A conflicting reference is rejected atomically."
          />
          <FormFooter>
            <Button variant="secondary" type="submit">
              Import statement
            </Button>
          </FormFooter>
        </FormLayout>
      </CardBody>
    </Card>
  );
}
