namespace StableRail.Infrastructure.Configuration;

public sealed class StableRailOptions
{
    public const string SectionName = "StableRail";

    public AuthenticationOptions Authentication { get; set; } = new();

    public SimulationOptions Simulation { get; set; } = new();

    public ProviderHostOptions Providers { get; set; } = new();

    public FinalityOptions Finality { get; set; } = new();

    public QueueOptions Queue { get; set; } = new();

    public TelemetryOptions Telemetry { get; set; } = new();

    public DiagnosticsOptions Diagnostics { get; set; } = new();

    public OperationsOptions Operations { get; set; } = new();
}

public sealed class AuthenticationOptions
{
    public const string DevelopmentMode = "Development";
    public const string DisabledMode = "Disabled";
    public const string RequiredMode = "Required";

    public string Mode { get; set; } = "";
}

public sealed class SimulationOptions
{
    public bool Enabled { get; set; }
}

public sealed class ProviderHostOptions
{
    public bool UseFakeAdapters { get; set; }
}

public sealed class FinalityOptions
{
    public const string DevOnlyProfile = "dev-only";

    public string Profile { get; set; } = "";
}

public sealed class QueueOptions
{
    public string Name { get; set; } = "";

    public string DeadLetterName { get; set; } = "";

    public string Region { get; set; } = "";

    public string ServiceUrl { get; set; } = "";

    public string AccessKey { get; set; } = "";

    public string SecretKey { get; set; } = "";
}

public sealed class TelemetryOptions
{
    public string OtlpEndpoint { get; set; } = "";

    public bool ConsoleExporter { get; set; }
}

public sealed class DiagnosticsOptions
{
    public bool ExposeTrace { get; set; }
}

public sealed class OperationsOptions
{
    public string Origin { get; set; } = "http://localhost:5174";
}

public sealed record StableRailHostRole(string Name);
