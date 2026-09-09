namespace ZigZag.Infrastructure.Persistence.Mapping;

/// <summary>
/// Converts between a C# enum and the SCREAMING_SNAKE_CASE string PostgreSQL
/// stores, so <c>TaskItemStatus.InProgress</c> round-trips as "IN_PROGRESS".
/// </summary>
/// <remarks>
/// <para>
/// This exists as an explicit static mapper - called by repository code at the
/// query boundary - rather than as a Dapper <see cref="Dapper.SqlMapper.ITypeHandler"/>.
/// A <c>SqlMapper.TypeHandler&lt;TEnum&gt;</c> was tried first and does not work:
/// Dapper hardcodes its own enum handling for both column deserialization and
/// query parameters (it converts an enum parameter to its underlying integer by
/// default and calls <see cref="Enum.Parse(Type, string, bool)"/> directly when
/// reading a string column into an enum property), and that built-in path runs
/// instead of any registered <c>ITypeHandler</c> - registration was confirmed
/// present in Dapper's internal handler dictionary and still bypassed.
/// </para>
/// <para>
/// Repositories therefore select enum columns as <c>text</c> into a plain
/// <c>string</c> property on the row DTO and call <see cref="Parse{TEnum}"/>
/// when mapping to the Domain type, and pass <see cref="ToDbValue{TEnum}"/> as
/// the query parameter rather than the enum itself. See docs/architecture.md
/// for the full write-up.
/// </para>
/// </remarks>
public static class DbEnumMapper
{
    /// <summary>Converts an enum member to the string PostgreSQL expects, e.g. <c>InProgress</c> -&gt; <c>"IN_PROGRESS"</c>.</summary>
    public static string ToDbValue<TEnum>(TEnum value) where TEnum : struct, Enum
        => ScreamingSnakeCaseConverter.ToScreamingSnakeCase(value.ToString());

    /// <summary>Converts a database string back to the matching enum member, e.g. <c>"IN_PROGRESS"</c> -&gt; <c>InProgress</c>.</summary>
    /// <exception cref="InvalidCastException">
    /// The value does not match any member of <typeparamref name="TEnum"/> - this means the
    /// database CHECK constraint and the C# enum have drifted apart.
    /// </exception>
    public static TEnum Parse<TEnum>(string dbValue) where TEnum : struct, Enum
    {
        var normalized = dbValue.Replace("_", string.Empty);

        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<TEnum>(name);
            }
        }

        throw new InvalidCastException(
            $"Database value '{dbValue}' does not map to any member of {typeof(TEnum).Name}. " +
            "This means the database CHECK constraint and the C# enum have drifted apart.");
    }
}
