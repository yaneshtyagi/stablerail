import type { SystemStatus } from "./api";

export function developmentAuthenticationNotice(system: SystemStatus): string | null {
  if (system.authentication_mode !== "Development") return null;
  return "Development authentication is enabled. Production startup rejects this mode.";
}

export function simulationNotice(system: SystemStatus): string | null {
  if (!system.simulation_enabled && !system.fake_adapters_enabled && system.finality_profile !== "dev-only")
    return null;
  return "Simulation is enabled. Production startup rejects simulation, fake provider adapters, and the dev-only finality profile.";
}

export function paymentVolumeIsUnavailable(system: SystemStatus): boolean {
  return system.payments === "unavailable";
}
