using System.Globalization;
using System.Numerics;
using StableRail.BuildingBlocks.Ledger;

namespace StableRail.BuildingBlocks.Money;

public readonly record struct SignedAmount
{
    public SignedAmount(BigInteger minorUnits, int scale)
    {
        if (scale < 0 || scale > AssetAmount.MaxScale)
            throw new LedgerRuleException("precision", "Asset scale must be between 0 and 18.");
        var absolute = minorUnits.Sign < 0 ? -minorUnits : minorUnits;
        var digits = absolute.IsZero ? 1 : absolute.ToString(CultureInfo.InvariantCulture).Length;
        if (digits > AssetAmount.MaxMinorDigits)
            throw new LedgerRuleException("precision", "Asset amount exceeds 38 minor-unit digits.");
        MinorUnits = minorUnits;
        Scale = scale;
    }

    public BigInteger MinorUnits { get; }
    public int Scale { get; }

    public string Text
    {
        get
        {
            var absolute = MinorUnits.Sign < 0 ? -MinorUnits : MinorUnits;
            var formatted = AssetAmount.Format(absolute, Scale);
            return MinorUnits.Sign < 0 ? "-" + formatted : formatted;
        }
    }

    public static SignedAmount Credit(AssetAmount amount) => new(amount.MinorUnits, amount.Scale);

    public SignedAmount Negate() => new(-MinorUnits, Scale);

    public AssetAmount Absolute()
    {
        var absolute = MinorUnits.Sign < 0 ? -MinorUnits : MinorUnits;
        return AssetAmount.FromMinor(absolute, Scale);
    }
}
