using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace StableRail.BuildingBlocks.Telemetry;

public static class StableRailTelemetry
{
    public const string ActivitySourceName = "StableRail";
    public const string MeterName = "StableRail";

    public static readonly ActivitySource Source = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);
    public static readonly Counter<long> QueuePresenceChecks =
        Meter.CreateCounter<long>("stablerail.queue.presence.checks");
}

public static class CorrelationProbe
{
    public const string RequestIdTag = "stablerail.request_id";
    public const string IntentTag = "stablerail.intent_id";
    public const string RouteTag = "stablerail.route_id";
    public const string ProviderTag = "stablerail.provider_id";
    public const string ChainTag = "stablerail.chain_id";
    public const string ReconciliationTag = "stablerail.reconciliation_id";

    public static readonly string[] Operations =
    [
        "payments.intent",
        "providers.route",
        "providers.execution",
        "monitoring.chain",
        "finance.reconciliation",
    ];

    public static CorrelationInspection Inspect(string requestId, IReadOnlyDictionary<string, string?> tags)
    {
        using var activity = StableRailTelemetry.Source.StartActivity("stablerail.correlation", ActivityKind.Internal);
        var traceId = activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? "";
        activity?.SetTag(RequestIdTag, requestId);
        Apply(activity, tags);

        foreach (var operation in Operations)
        {
            using var child = StableRailTelemetry.Source.StartActivity(operation, ActivityKind.Internal);
            child?.SetTag(RequestIdTag, requestId);
            Apply(child, tags);
        }

        return new CorrelationInspection(requestId, traceId, Operations);
    }

    private static void Apply(Activity? activity, IReadOnlyDictionary<string, string?> tags)
    {
        if (activity is null)
            return;

        foreach (var (key, value) in tags)
        {
            if (IsCorrelationValue(value))
                activity.SetTag(key, value);
        }
    }

    public static bool IsCorrelationValue(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 128
        && value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
}

public sealed record CorrelationInspection(
    string RequestId,
    string TraceId,
    IReadOnlyList<string> Operations);
