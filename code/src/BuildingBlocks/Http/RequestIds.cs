namespace StableRail.BuildingBlocks.Http;

public static class RequestIds
{
    public const string HeaderName = "X-Request-Id";
    public const string TraceHeaderName = "X-Trace-Id";
    public const string ItemKey = "StableRail.RequestId";

    public static bool IsAcceptable(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length is >= 8 and <= 128
        && value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    public static string Create() => $"req_{Guid.CreateVersion7():N}";
}
