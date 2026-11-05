namespace StableRail.Infrastructure.Persistence;

public sealed class ModuleOwnershipRecord
{
    public string Name { get; set; } = "";

    public string SchemaName { get; set; } = "";

    public string Responsibility { get; set; } = "";

    public DateTimeOffset RecordedAt { get; set; }
}
