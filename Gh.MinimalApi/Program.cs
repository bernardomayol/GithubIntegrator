using Microsoft.OpenApi.Models; // OpenApiInfo
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI nativo + Swagger UI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minimal API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Usa el header Authorization con el formato: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});
});

// JWT simple para pruebas
var jwtKey = builder.Configuration["Jwt:Key"] ?? "EstaEsUnaClaveSuperSecretaDeAlMenos32Chars!!";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("fixed", options =>
    {
        options.Window = TimeSpan.FromMinutes(5);
        options.PermitLimit = 100;
    });
});

// HttpClient con Resilience oficial (.NET)
builder.Services.AddHttpClient("github", c =>
{
    c.BaseAddress = new Uri("https://api.github.com/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("GithubIntegrator/1.0");
}).AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Token de prueba
app.MapPost("/auth/token", (string Username) =>
{
    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var token = handler.CreateJwtSecurityToken(subject: null, signingCredentials: creds, expires: DateTime.UtcNow.AddHours(8));
    return Results.Ok(new { access_token = handler.WriteToken(token) });
});

// Endpoints de ejemplo
app.MapGet("/api/v1/github/search", async (string q, int page, int pageSize, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("github");
    var resp = await client.GetAsync($"search/repositories?q={Uri.EscapeDataString(q)}&page={page}&per_page={pageSize}");
    return Results.Content(await resp.Content.ReadAsStringAsync(), "application/json");
}).RequireAuthorization();

app.MapGet("/", () => Results.Ok(new { message = "OK" }));

app.Run();

public partial class Program { }
