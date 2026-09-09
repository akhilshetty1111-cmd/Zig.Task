namespace ZigZag.Infrastructure.Persistence.Mapping;

/// <summary>
/// Converts a <c>timestamptz</c> column's value to the <see cref="DateTimeOffset"/>
/// Domain/Application types use.
/// </summary>
/// <remarks>
/// Repository row DTOs use <see cref="DateTime"/>, not <see cref="DateTimeOffset"/>,
/// for every <c>timestamptz</c> column - Npgsql returns those as <c>DateTime</c>,
/// and Dapper's record materialization requires an exact constructor-parameter
/// type match. A row DTO with a <c>DateTimeOffset</c> property fails at
/// construction with "no parameterless constructor or matching signature",
/// confirmed by actually running a query against it. <see cref="ToUtcOffset"/>
/// converts explicitly at the mapping boundary (row DTO -&gt; Domain/Application
/// type) rather than relying on the implicit <c>DateTime</c>-&gt;<c>DateTimeOffset</c>
/// conversion, which silently treats an <see cref="DateTimeKind.Unspecified"/>
/// value as local time - the wrong behavior for a UTC timestamp whose Kind
/// Npgsql does not always mark as <see cref="DateTimeKind.Utc"/>.
/// </remarks>
public static class DbDateTimeMapper
{
    public static DateTimeOffset ToUtcOffset(DateTime timestamptzValue)
        => new(DateTime.SpecifyKind(timestamptzValue, DateTimeKind.Utc));
}
