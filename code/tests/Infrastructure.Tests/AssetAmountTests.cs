using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;

namespace StableRail.Infrastructure.Tests;

public sealed class AssetAmountTests
{
    [Fact]
    public void Decimal_strings_round_trip_at_the_asset_scale()
    {
        var amount = AssetAmount.Parse("250.10", 6);

        Assert.Equal("250.10", amount.OriginalText);
        Assert.Equal("250.100000", amount.CanonicalText);
        Assert.Equal(250100000, amount.MinorUnits);
    }

    [Fact]
    public void Addition_uses_exact_minor_units()
    {
        var sum = AssetAmount.Parse("0.10", 2).Add(AssetAmount.Parse("0.20", 2));

        Assert.Equal(30, sum.MinorUnits);
        Assert.Equal("0.30", sum.CanonicalText);
    }

    [Theory]
    [InlineData("0.001", 2)]
    [InlineData("1.0", 0)]
    [InlineData("1e-3", 6)]
    [InlineData("-1.00", 2)]
    [InlineData("01.00", 2)]
    [InlineData("", 2)]
    public void Precision_boundaries_reject_text_the_asset_cannot_represent(string text, int scale)
    {
        var exception = Assert.Throws<LedgerRuleException>(() => AssetAmount.Parse(text, scale));
        Assert.Equal("precision", exception.Code);
    }

    [Fact]
    public void Scale_and_digit_boundaries_are_exact()
    {
        var smallest = AssetAmount.Parse("0.000000000000000001", 18);
        Assert.Equal(1, smallest.MinorUnits);

        var largest = AssetAmount.Parse(new string('9', 20) + "." + new string('9', 18), 18);
        Assert.Equal(38, largest.MinorUnits.ToString().Length);

        var overflow = Assert.Throws<LedgerRuleException>(() => AssetAmount.Parse(new string('9', 39), 0));
        Assert.Equal("precision", overflow.Code);
        var scale = Assert.Throws<LedgerRuleException>(() => AssetAmount.Parse("1", 19));
        Assert.Equal("precision", scale.Code);
    }

    [Fact]
    public void Different_scales_do_not_add()
    {
        var exception = Assert.Throws<LedgerRuleException>(() =>
            AssetAmount.Parse("250.00", 2).Add(AssetAmount.Parse("250.000000", 6)));

        Assert.Equal("scale_mismatch", exception.Code);
    }

    [Fact]
    public void Money_source_does_not_use_binary_floating_point()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "StableRail.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root!.FullName, "src/BuildingBlocks/Money/AssetAmount.cs"));
        Assert.DoesNotContain("double", source, StringComparison.Ordinal);
        Assert.DoesNotContain("float", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Decimal", source, StringComparison.Ordinal);
    }
}
