import { useCallback, useEffect, useState } from "react";
import {
  Alert,
  AppShell,
  Badge,
  Button,
  Card,
  CardBody,
  DataTable,
  DescriptionList,
  EmptyState,
  PageHeader,
  useTheme,
  useToast,
  type Column,
  type NavGroup,
} from "@yaneshtyagi/design-system";
import stableRailLogo from "./assets/stablerail-logo.svg";
import stableRailLogoDark from "./assets/stablerail-logo-dark.svg";
import { loadControlPlane, type ControlPlaneSnapshot, type ModuleOwnership } from "./api";
import { developmentAuthenticationNotice, paymentVolumeIsUnavailable, simulationNotice } from "./notices";

type Section = "runtime" | "modules";

const moduleColumns: Column<ModuleOwnership>[] = [
  { id: "name", header: "Module", cell: (row) => row.name, rowHeader: true },
  { id: "schema", header: "Schema", cell: (row) => row.schema },
  { id: "responsibility", header: "Owns", cell: (row) => row.responsibility },
  { id: "requirements", header: "Requirements", cell: (row) => row.requirements.join(", ") },
];

export function App() {
  const theme = useTheme();
  const toast = useToast();
  const [section, setSection] = useState<Section>(readSection);
  const [snapshot, setSnapshot] = useState<ControlPlaneSnapshot | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSnapshot(await loadControlPlane());
    } catch (cause) {
      setSnapshot(null);
      setError(cause instanceof Error ? cause.message : "The control plane could not be reached.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const onHash = () => setSection(readSection());
    window.addEventListener("hashchange", onHash);
    void reload();
    return () => window.removeEventListener("hashchange", onHash);
  }, [reload]);

  const groups: NavGroup[] = [
    {
      caption: "Control plane",
      items: [
        { id: "runtime", label: "Runtime", icon: "layout-dashboard", href: "#/runtime", current: section === "runtime" },
        { id: "modules", label: "Modules", icon: "file-text", href: "#/modules", current: section === "modules" },
      ],
    },
  ];

  return (
    <AppShell
      sidebar={{
        productName: "StableRail",
        logo: (
          <img
            className="stablerail-logo"
            src={theme.theme === "dark" ? stableRailLogoDark : stableRailLogo}
            alt="StableRail"
            width={160}
            height={25}
          />
        ),
        product: "Operations",
        version: "0.1",
        collapsedMark: "SR",
        groups,
        user: {
          name: "Not signed in",
          role: snapshot?.system?.authentication_mode ?? "Operator session",
        },
      }}
      topbar={{
        onHelp: () =>
          toast.show({
            title: "Control plane",
            body: "This console reads the API, worker dependencies and module ownership. It does not show payment volume.",
          }),
        user: (
          <Button variant="ghost" icon="eye" iconOnly aria-label="Toggle theme" onClick={theme.toggleTheme} />
        ),
      }}
    >
      {section === "modules" ? (
        <Modules snapshot={snapshot} loading={loading} error={error} onRetry={() => void reload()} />
      ) : (
        <Runtime snapshot={snapshot} loading={loading} error={error} onRetry={() => void reload()} />
      )}
    </AppShell>
  );
}

function Runtime({
  snapshot,
  loading,
  error,
  onRetry,
}: {
  snapshot: ControlPlaneSnapshot | null;
  loading: boolean;
  error: string | null;
  onRetry: () => void;
}) {
  const system = snapshot?.system ?? null;
  const authentication = system ? developmentAuthenticationNotice(system) : null;
  const simulation = system ? simulationNotice(system) : null;
  return (
    <div className="stack">
      <PageHeader
        title="Runtime"
        description="Dependency health for the control plane. Payment volume is not served."
        actions={
          <Button variant="secondary" onClick={onRetry} loading={loading}>
            Refresh
          </Button>
        }
      />
      {error ? (
        <Alert tone="danger" title="Control plane unavailable" actions={<Button onClick={onRetry}>Retry</Button>}>
          {error}
        </Alert>
      ) : null}
      {authentication ? <Alert tone="warning" title="Development authentication">{authentication}</Alert> : null}
      {simulation ? <Alert tone="warning" title="Simulation">{simulation}</Alert> : null}
      {system && paymentVolumeIsUnavailable(system) ? (
        <Alert tone="info" title="Payments are not available">
          This build does not serve payment, routing, finance or webhook operations, and it does not display fixture volume.
        </Alert>
      ) : null}
      <Card>
        <CardBody>
          {snapshot ? (
            <DescriptionList
              items={[
                { label: "Environment", value: system?.environment },
                { label: "Authentication", value: system?.authentication_mode },
                { label: "Live", value: <Badge tone={toneFor(snapshot.live.status)}>{snapshot.live.status}</Badge> },
                { label: "Ready", value: <Badge tone={toneFor(snapshot.ready.status)}>{snapshot.ready.status}</Badge> },
                { label: "PostgreSQL", value: checkStatus(snapshot, "postgresql") },
                { label: "Redis", value: checkStatus(snapshot, "redis") },
                { label: "Queue", value: checkStatus(snapshot, "queue") },
                { label: "Queue product", value: system ? `${system.queue.product} ${system.queue.type}` : undefined },
                { label: "Queue name", value: system?.queue.name, mono: true },
                { label: "Dead letter", value: system?.queue.dead_letter_name, mono: true },
                { label: "Emulator", value: system?.queue.emulator },
                { label: "Request", value: snapshot.requestId, mono: true },
              ]}
            />
          ) : (
            <EmptyState title={loading ? "Reading the control plane" : "No status yet"} />
          )}
        </CardBody>
      </Card>
    </div>
  );
}

function Modules({
  snapshot,
  loading,
  error,
  onRetry,
}: {
  snapshot: ControlPlaneSnapshot | null;
  loading: boolean;
  error: string | null;
  onRetry: () => void;
}) {
  return (
    <div className="stack">
      <PageHeader
        title="Modules"
        description="Ownership stored by the platform migration."
        actions={
          <Button variant="secondary" onClick={onRetry} loading={loading}>
            Refresh
          </Button>
        }
      />
      {error ? (
        <Alert tone="danger" title="Control plane unavailable">
          {error}
        </Alert>
      ) : null}
      {snapshot?.moduleError ? (
        <Alert tone="warning" title="Ownership is not available">
          {snapshot.moduleError}
        </Alert>
      ) : null}
      <DataTable
        aria-label="Module ownership"
        columns={moduleColumns}
        rows={snapshot?.modules ?? []}
        rowKey={(row) => row.name}
        loading={loading}
        empty={<EmptyState title="No modules returned" />}
      />
    </div>
  );
}

function checkStatus(snapshot: ControlPlaneSnapshot, name: string): string {
  return snapshot.ready.checks.find((check) => check.name === name)?.status ?? "Unknown";
}

function toneFor(status: string): "success" | "danger" | "warning" {
  if (status === "Healthy") return "success";
  if (status === "Degraded") return "warning";
  return "danger";
}

function readSection(): Section {
  return window.location.hash === "#/modules" ? "modules" : "runtime";
}
