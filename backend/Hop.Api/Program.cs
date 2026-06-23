using System.Text;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Middleware;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;

EnvFileLoader.LoadFromParentDirectories(".env");

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
var allowCredentials = builder.Configuration.GetValue("Cors:AllowCredentials", builder.Configuration.GetValue("CORS_ALLOW_CREDENTIALS", false));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
        if (allowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ILeaveAttachmentStorageService, LeaveAttachmentStorageService>();
builder.Services.AddScoped<ILeavePdfService, LeavePdfService>();
builder.Services.AddScoped<IFileScanningService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var scannerProvider = configuration["FileScan:Provider"] ?? configuration["FILE_SCAN_PROVIDER"] ?? "Placeholder";
    return string.Equals(scannerProvider, "ClamAV", StringComparison.OrdinalIgnoreCase)
        ? ActivatorUtilities.CreateInstance<ClamAvFileScanningService>(provider)
        : ActivatorUtilities.CreateInstance<PlaceholderFileScanningService>(provider);
});
builder.Services.AddScoped<ILeaveCalendarService, LeaveCalendarService>();
builder.Services.AddScoped<ILeaveBalanceValidationService, LeaveBalanceValidationService>();
builder.Services.AddScoped<ILeaveValidationService, LeaveValidationService>();
builder.Services.AddScoped<IApprovalChainService, ApprovalChainService>();
builder.Services.AddScoped<IApprovalEscalationService, ApprovalEscalationService>();
builder.Services.AddScoped<IPendingApprovalNotificationService, PendingApprovalNotificationService>();
builder.Services.AddScoped<ILeaveNotificationEventPublisher, LeaveNotificationEventPublisher>();
builder.Services.AddScoped<ILeaveRequestAccessService, LeaveRequestAccessService>();
builder.Services.AddScoped<ILeaveRequestNumberService, LeaveRequestNumberService>();
builder.Services.AddScoped<IAuditRetentionService, AuditRetentionService>();
builder.Services.AddSingleton<ILoginRateLimiter, InMemoryLoginRateLimiter>();
builder.Services.AddHttpClient<ILineMessagingService, LineMessagingService>();
builder.Services.AddHostedService<LineRetryWorker>();
builder.Services.AddHostedService<ApprovalEscalationWorker>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt__Key must be configured and contain at least 32 characters.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks();

var app = builder.Build();

var seedOnStartup = app.Configuration.GetValue<bool?>("Database:SeedOnStartup")
    ?? app.Configuration.GetValue<bool?>("DATABASE_SEED_ON_STARTUP")
    ?? (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("CI"));
if (seedOnStartup)
{
    await DevelopmentDataSeeder.SeedAsync(app.Services, app.Logger);
}
else
{
    app.Logger.LogInformation("Database startup seeding is disabled. Run EF Core migrations and bootstrap production data explicitly.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseMiddleware<PermissionDeniedAuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/healthz");
app.MapGet("/api", () => ApiResponse<string>.Ok("Hospital Operations Portal API is running."));

app.Run();

public partial class Program;
