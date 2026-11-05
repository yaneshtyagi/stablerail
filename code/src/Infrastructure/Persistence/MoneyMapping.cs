using System.Globalization;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StableRail.Infrastructure.Persistence;

internal sealed class BigIntegerConverter() : ValueConverter<BigInteger, string>(
    value => value.ToString(CultureInfo.InvariantCulture),
    value => BigInteger.Parse(value, CultureInfo.InvariantCulture));

internal sealed class NullableBigIntegerConverter() : ValueConverter<BigInteger?, string?>(
    value => value == null ? null : value.Value.ToString(CultureInfo.InvariantCulture),
    value => value == null ? null : BigInteger.Parse(value, CultureInfo.InvariantCulture));

internal static class MoneyMapping
{
    private static readonly BigIntegerConverter Converter = new();
    private static readonly NullableBigIntegerConverter NullableConverter = new();

    public static PropertyBuilder<BigInteger> MinorUnits(this PropertyBuilder<BigInteger> property) =>
        property.HasConversion(Converter).HasColumnType("text");

    public static PropertyBuilder<BigInteger?> MinorUnits(this PropertyBuilder<BigInteger?> property) =>
        property.HasConversion(NullableConverter).HasColumnType("text");
}
