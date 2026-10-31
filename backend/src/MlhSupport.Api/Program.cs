using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MlhSupport.Api.Web;
using MlhSupport.Application;
using MlhSupport.Infrastructure;
using MlhSupport.Infrastructure.Agents;
using MlhSupport.Infrastructure.Auth;
using MlhSupport.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < JwtOptions.MinKeyLength)
{
    throw new InvalidOperationException(
        $"Jwt:Key must be at least {JwtOptions.MinKeyLength} characters. Set it in secrets.json or the Jwt__Key environment variable.");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.Configure<AgentServiceOptions>(builder.Configuration.GetSection(AgentServiceOptions.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = ClaimNames.Name,
            RoleClaimType = ClaimNames.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.SetIsOriginAllowed(origin =>
            allowedOrigins.Contains(origin) ||
            (builder.Environment.IsDevelopment() && Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback))
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
