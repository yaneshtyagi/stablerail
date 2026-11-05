namespace StableRail.BuildingBlocks.Runtime;

/// <summary>
/// Engineering pins checked on 5 November 2026. They are not a capacity or restore approval.
/// </summary>
public static class RuntimePins
{
    public const string TargetFramework = "net10.0";
    public const string PostgresImage = "postgres:18.6";
    public const string RedisImage = "redis:8.10.3";
    public const string Node = "26.13.0";

    public const int LocalPostgresPort = 54329;
    public const int LocalRedisPort = 63799;
    public const int ApiPort = 5080;
    public const int WorkerPort = 5081;
    public const int OperationsPort = 5174;
}
