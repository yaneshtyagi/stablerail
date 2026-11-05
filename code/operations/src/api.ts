export interface HealthReport {
  status: string;
  checks: { name: string; status: string }[];
}

export interface QueueStatus {
  product: string;
  name: string;
  dead_letter_name: string;
  region: string;
  type: string;
  emulator: string | null;
}

export interface SystemStatus {
  environment: string;
  authentication_mode: string;
  simulation_enabled: boolean;
  fake_adapters_enabled: boolean;
  finality_profile: string;
  payments: "unavailable";
  queue: QueueStatus;
}

export interface ModuleOwnership {
  name: string;
  schema: string;
  responsibility: string;
  requirements: string[];
  recorded_at: string;
}

export interface ApiErrorBody {
  error: {
    code: string;
    message: string;
    category: string;
    retryable: boolean;
    request_id: string;
  };
}

export function apiBase(): string {
  const configured = import.meta.env.VITE_API_BASE_URL;
  if (configured === "") return "";
  return (configured ?? "http://localhost:5080").replace(/\/$/, "");
}

export function requestUrl(base: string, path: string): string {
  return `${base}${path}`;
}

export function createRequestId(): string {
  return `req_${crypto.randomUUID().replaceAll("-", "")}`;
}

export interface ControlPlaneSnapshot {
  live: HealthReport;
  ready: HealthReport;
  system: SystemStatus | null;
  modules: ModuleOwnership[] | null;
  systemError: string | null;
  moduleError: string | null;
  requestId: string;
}

export async function loadControlPlane(base = apiBase()): Promise<ControlPlaneSnapshot> {
  const requestId = createRequestId();
  const headers = { "X-Request-Id": requestId };
  const [live, ready, systemResponse, moduleResponse] = await Promise.all([
    readJson<HealthReport>(requestUrl(base, "/health/live"), headers),
    readJson<HealthReport>(requestUrl(base, "/health/ready"), headers),
    fetch(requestUrl(base, "/v1/system"), { headers, cache: "no-store" }),
    fetch(requestUrl(base, "/v1/modules"), { headers, cache: "no-store" }),
  ]);
  const system = await readOptional<SystemStatus>(systemResponse);
  const modules = await readOptional<ModuleOwnership[]>(moduleResponse);
  return {
    live,
    ready,
    system: system.value,
    modules: modules.value,
    systemError: system.error,
    moduleError: modules.error,
    requestId,
  };
}

async function readJson<T>(url: string, headers: HeadersInit): Promise<T> {
  const response = await fetch(url, { headers, cache: "no-store" });
  if (!response.ok) throw new Error(`${response.status} from ${url}`);
  return (await response.json()) as T;
}

async function readOptional<T>(response: Response): Promise<{ value: T | null; error: string | null }> {
  if (response.ok) return { value: (await response.json()) as T, error: null };
  const body = (await response.json().catch(() => null)) as ApiErrorBody | null;
  return { value: null, error: body?.error.message ?? `${response.status} ${response.statusText}` };
}
