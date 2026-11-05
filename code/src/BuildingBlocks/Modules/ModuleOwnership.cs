namespace StableRail.BuildingBlocks.Modules;

public interface IModule
{
    string Name { get; }
}

public sealed record ModuleRegistration(string Name) : IModule;

public sealed record ModuleOwnership(
    string Name,
    string Schema,
    string Responsibility,
    IReadOnlyList<string> Requirements);
