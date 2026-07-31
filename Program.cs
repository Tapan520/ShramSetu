using System.Text;
using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShramSetu.Data;
using ShramSetu.Hubs;
using ShramSetu.Jobs;
using ShramSetu.Services;

var builder = WebApplication.CreateBuilder(args);

// ?? Database ??????????????????????????????????????????????????????????????????
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

// ?? Identity ??????????????????????????????????????????????????????????????????
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// ?? JWT Authentication ????????????????????????????????????????????????????????
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "CHANGE_ME_USE_A_32+_CHAR_SECRET_IN_PRODUCTION";

builder.Services
    .AddAuthentication(options =>
    {
        // Keep cookie auth as default for Razor Pages
        options.DefaultScheme          = "MultiScheme";
        options.DefaultChallengeScheme = "MultiScheme";
    })
    .AddPolicyScheme("MultiScheme", "Cookie or JWT", options =>
    {
        // API routes ? JWT; everything else ? Identity cookies
        options.ForwardDefaultSelector = ctx =>
            ctx.Request.Path.StartsWithSegments("/api")
                ? JwtBearerDefaults.AuthenticationScheme
                : IdentityConstants.ApplicationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// ?? CORS (allow mobile apps to call the API) ??????????????????????????????????
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ?? MVC Controllers + Razor Pages + SignalR ???????????????????????????????????
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// ?? Swagger / OpenAPI (mobile team reference) ?????????????????????????????????
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ShramSetu API",
        Version     = "v1",
        Description = "REST API powering the ShramSetu mobile app. " +
                      "All /api/* endpoints use Bearer JWT authentication.",
        Contact     = new OpenApiContact { Name = "ShramSetu Admin" }
    });

    // Swagger UI "Authorize" button sends Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ?? Services ??????????????????????????????????????????????????????????????????
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IWorkerMatchingService, WorkerMatchingService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IJobAlertService, JobAlertService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IGstInvoiceService, GstInvoiceService>();
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IInAppNotificationService, InAppNotificationService>();

// Email: SMTP in production, console stub in dev
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:SmtpPassword"]))
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
else
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();

// Background jobs: Hangfire (in-memory for dev; swap to SQL in production)
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Register job classes for DI
builder.Services.AddScoped<JobAlertDispatchJob>();
builder.Services.AddScoped<CleanupJob>();
builder.Services.AddScoped<PayrollReminderJob>();
builder.Services.AddScoped<SlaBreachJob>();
builder.Services.AddScoped<WeeklyDigestJob>();

// Push: FCM when credential is configured, else no-op
if (!string.IsNullOrWhiteSpace(builder.Configuration["Firebase:CredentialPath"])
 || !string.IsNullOrWhiteSpace(builder.Configuration["Firebase:CredentialJson"]))
    builder.Services.AddScoped<IPushNotificationService, FcmPushNotificationService>();
else
    builder.Services.AddScoped<IPushNotificationService, NoOpPushNotificationService>();

// SMS/WhatsApp: Twilio in production, console stub in dev
if (!string.IsNullOrWhiteSpace(builder.Configuration["Twilio:AccountSid"]))
    builder.Services.AddScoped<INotificationService, TwilioNotificationService>();
else
    builder.Services.AddScoped<INotificationService, ConsoleNotificationService>();

// ?? Rate Limiting ????????????????????????????????????????????????????????????
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ?? Health Checks ?????????????????????????????????????????????????????????????
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// ?????????????????????????????????????????????????????????????????????????????
var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
    await DbSeeder.SeedAsync(scope.ServiceProvider);

// ?? Middleware pipeline ???????????????????????????????????????????????????????
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Swagger always available (restrict in production if needed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShramSetu API v1");
    c.RoutePrefix = "api/docs";  // available at /api/docs
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseIpRateLimiting();

app.UseCors("MobileApp");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire Dashboard (admin only in production)
app.MapHangfireDashboard("/admin/jobs", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthFilter()]
});

// Register recurring jobs
RecurringJobsSetup.RegisterAll();

// ?? Health endpoint ???????????????????????????????????????????????????????????
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status  = report.Status.ToString(),
            checks  = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
            duration = report.TotalDuration
        });
        await ctx.Response.WriteAsync(result);
    }
});

app.Run();
