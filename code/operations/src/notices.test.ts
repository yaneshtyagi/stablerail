import { describe, expect, it } from "vitest";
import type { SystemStatus } from "./api";
import { apiBase, requestUrl } from "./api";
import { developmentAuthenticationNotice, paymentVolumeIsUnavailable, simulationNotice } from "./notices";

const system: SystemStatus = {
  environment: "Development",
  authentication_mode: "Development",
  simulation_enabled: true,
  fake_adapters_enabled: true,
  finality_profile: "dev-only",
  payments: "unavailable",
  queue: {
    product: "AmazonSqs",
    name: "stablerail-control-plane",
    dead_letter_name: "stablerail-control-plane-dlq",
    region: "us-east-1",
    type: "Standard",
    emulator: "ElasticMQ",
  },
};

describe("operations status", () => {
  it("points local requests at the control-plane API", () => {
    expect(requestUrl(apiBase(), "/health/ready")).toBe("http://localhost:5080/health/ready");
  });

  it("explains development authentication and simulation without showing payment volume", () => {
    expect(developmentAuthenticationNotice(system)).toMatch(/Development authentication/);
    expect(simulationNotice(system)).toMatch(/Simulation is enabled/);
    expect(paymentVolumeIsUnavailable(system)).toBe(true);
    expect(JSON.stringify(system)).not.toMatch(/100,000|USD/);
  });

  it("hides the development notices when production rules are in force", () => {
    const production: SystemStatus = {
      ...system,
      environment: "Production",
      authentication_mode: "Required",
      simulation_enabled: false,
      fake_adapters_enabled: false,
      finality_profile: "",
      queue: { ...system.queue, emulator: null },
    };
    expect(developmentAuthenticationNotice(production)).toBeNull();
    expect(simulationNotice(production)).toBeNull();
  });
});
