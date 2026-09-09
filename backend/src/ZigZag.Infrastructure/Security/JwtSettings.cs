namespace ZigZag.Infrastructure.Security;

/// <summary>
/// Bound from the "Jwt" configuration section. Also read directly (same keys)
/// by ZigZag.API's Program.cs when configuring the JwtBearer authentication
/// handler, since that configuration happens before the DI container that
/// would resolve <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> is built.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Key { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
