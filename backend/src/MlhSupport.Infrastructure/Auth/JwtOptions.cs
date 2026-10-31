namespace MlhSupport.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32;

    public string Issuer { get; set; } = "classly-api";
    public string Audience { get; set; } = "classly-web";
    public string Key { get; set; } = string.Empty;
    public int LifetimeHours { get; set; } = 12;
}
