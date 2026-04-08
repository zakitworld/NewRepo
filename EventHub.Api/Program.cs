using EventHub.Api.Data;
using EventHub.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "eventhub_api.db");
builder.Services.AddDbContext<ApiDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

// ── JWT Auth ──────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "CHANGE_THIS_32_CHAR_SECRET_IN_PROD!";   // override via appsettings or env vars

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"] ?? "EventHub",
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "EventHubApp",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // Allow JWT from SignalR WebSocket query string
        o.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddSignalR();
builder.Services.AddScoped<EventHub.Api.Hubs.IVoteNotifier, EventHub.Api.Hubs.VoteNotifier>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ── Ensure DB created on startup ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<ApiDbContext>().Database.EnsureCreatedAsync();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<VoteHub>("/hubs/votes");

app.Run();
