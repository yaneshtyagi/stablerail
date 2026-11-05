using System.Globalization;
using System.Numerics;
using StableRail.BuildingBlocks.Ledger;

namespace StableRail.BuildingBlocks.Money;

public readonly record struct AssetAmount
{
    public const int MaxScale = 18;
    public const int MaxMinorDigits = 38;

    private AssetAmount(BigInteger minorUnits, int scale, string originalText)
    {
        MinorUnits = minorUnits;
        Scale = scale;
        OriginalText = originalText;
    }

    public BigInteger MinorUnits { get; }
    public int Scale { get; }
    public string OriginalText { get; }
    public string CanonicalText => Format(MinorUnits, Scale);
    public bool IsZero => MinorUnits.IsZero;

    public static AssetAmount Zero(int scale) => FromMinor(BigInteger.Zero, scale);

    public static AssetAmount Parse(string text, int scale)
    {
        var minor = ParseMinor(text, scale);
        return new AssetAmount(minor, scale, text);
    }

    public static AssetAmount FromMinor(BigInteger minorUnits, int scale)
    {
        if (minorUnits.Sign < 0)
            throw new LedgerRuleException("precision", "Asset amounts cannot be negative.");
        EnsureScale(scale);
        EnsureDigits(minorUnits);
        return new AssetAmount(minorUnits, scale, Format(minorUnits, scale));
    }

    public AssetAmount Add(AssetAmount other)
    {
        SameScale(other);
        return FromMinor(MinorUnits + other.MinorUnits, Scale);
    }

    public AssetAmount Subtract(AssetAmount other)
    {
        SameScale(other);
        if (other.MinorUnits > MinorUnits)
            throw new LedgerRuleException("precision", "Asset subtraction would be negative.");
        return FromMinor(MinorUnits - other.MinorUnits, Scale);
    }

    public int CompareTo(AssetAmount other)
    {
        SameScale(other);
        return MinorUnits.CompareTo(other.MinorUnits);
    }

    public static string Format(BigInteger minorUnits, int scale)
    {
        if (minorUnits.Sign < 0)
            throw new LedgerRuleException("precision", "Asset amounts cannot be negative.");
        EnsureScale(scale);
        var digits = minorUnits.ToString(CultureInfo.InvariantCulture);
        if (scale == 0)
            return digits;
        digits = digits.PadLeft(scale + 1, '0');
        return string.Concat(digits.AsSpan(0, digits.Length - scale), ".", digits.AsSpan(digits.Length - scale));
    }

    private void SameScale(AssetAmount other)
    {
        if (other.Scale != Scale)
            throw new LedgerRuleException("scale_mismatch", "Amounts use different asset scales.");
    }

    private static BigInteger ParseMinor(string text, int scale)
    {
        EnsureScale(scale);
        if (string.IsNullOrEmpty(text) || text.Length > MaxMinorDigits + 1)
            throw new LedgerRuleException("precision", "Amount text is empty or too long.");

        var dot = text.IndexOf('.');
        if (text.IndexOf('.') != text.LastIndexOf('.'))
            throw new LedgerRuleException("precision", "Amount text is not a decimal string.");
        var whole = dot < 0 ? text : text[..dot];
        var fraction = dot < 0 ? "" : text[(dot + 1)..];
        if (dot == 0 || (dot >= 0 && fraction.Length == 0))
            throw new LedgerRuleException("precision", "Amount text is not a decimal string.");
        if (scale == 0 && dot >= 0)
            throw new LedgerRuleException("precision", "This asset does not allow a fractional amount.");
        if (fraction.Length > scale)
            throw new LedgerRuleException("precision", "Amount has more fractional digits than the asset scale.");
        if (!IsWhole(whole) || !IsDigits(fraction))
            throw new LedgerRuleException("precision", "Amount text is not a decimal string.");

        var minor = BigInteger.Parse(whole + fraction.PadRight(scale, '0'), CultureInfo.InvariantCulture);
        EnsureDigits(minor);
        return minor;
    }

    private static void EnsureScale(int scale)
    {
        if (scale < 0 || scale > MaxScale)
            throw new LedgerRuleException("precision", "Asset scale must be between 0 and 18.");
    }

    private static void EnsureDigits(BigInteger minorUnits)
    {
        var digits = minorUnits.IsZero
            ? 1
            : minorUnits.ToString(CultureInfo.InvariantCulture).Length;
        if (digits > MaxMinorDigits)
            throw new LedgerRuleException("precision", "Asset amount exceeds 38 minor-unit digits.");
    }

    private static bool IsWhole(string value) =>
        value.Length > 0 && (value == "0" || (value[0] != '0' && IsDigits(value)));

    private static bool IsDigits(string value)
    {
        foreach (var character in value)
        {
            if (character is < '0' or > '9')
                return false;
        }

        return true;
    }
}
