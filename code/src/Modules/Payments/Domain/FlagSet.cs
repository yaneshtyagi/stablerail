using StableRail.BuildingBlocks.Ledger;

namespace StableRail.Modules.Payments.Domain;

public readonly record struct FlagSet(string Encoded)
{
    public static FlagSet Empty { get; } = new("");

    public static FlagSet From(IEnumerable<ExceptionFlag> flags)
    {
        var names = flags
            .Distinct()
            .Select(flag => flag.ToString())
            .OrderBy(name => name, StringComparer.Ordinal);
        return new FlagSet(string.Join(',', names));
    }

    public IReadOnlyList<ExceptionFlag> Items =>
        string.IsNullOrEmpty(Encoded)
            ? []
            : Encoded.Split(',').Select(Enum.Parse<ExceptionFlag>).ToArray();

    public bool Contains(ExceptionFlag flag) => Items.Contains(flag);

    public FlagSet Add(ExceptionFlag flag) => From(Items.Append(flag));

    public FlagSet ExceptDerived() =>
        From(Items.Where(flag => flag is not ExceptionFlag.underpayment and not ExceptionFlag.overpayment));
}
