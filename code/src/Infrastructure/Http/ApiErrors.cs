using System.Text.Json;

namespace StableRail.Infrastructure.Http;

public static class StableRailJson
{
    public static readonly JsonSerializerOptions Serializer = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}

public static class ApiErrors
{
    public static object Body(string code, string message, string category, bool retryable, string requestId) =>
        new
        {
            error = new
            {
                code,
                message,
                category,
                retryable,
                request_id = requestId,
                details = new { },
            },
        };
}
