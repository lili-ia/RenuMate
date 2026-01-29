using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Auth0.ManagementApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RenuMate.Api.Extensions;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services;
using RenuMate.Api.Services.Contracts;
using RenuMate.Api.Services.Email;
using IEmailSender = RenuMate.Api.Services.Contracts.IEmailSender;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using RenuMate.Api.Converters;
using Serilog;
using Serilog.Events;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IManagementApiClient>(_ => 
    new ManagementApiClient(string.Empty, new Uri($"https://{configuration["Auth0:Domain"]}/api/v2")));

builder.Services.AddScoped<IAuth0Service, Auth0Service>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];

        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://{builder.Configuration["Auth0:Domain"]}/",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth0:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedEmailOnly", policy => 
        policy.Requirements.Add(new EmailVerifiedRequirement()));
    
    options.AddPolicy("ActiveUserOnly", policy => 
        policy.Requirements.Add(new ActiveUserRequirement()));
});

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationResultHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://localhost:4173", 
                "http://localhost",      
                "http://renumate.online",
                "https://renumate.online"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("GlobalPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: key => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IAuthorizationHandler, VerifiedEmailOnlyAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ActiveUserOnlyAuthorizationHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));

builder.Services.AddDbContext<RenuMateDbContext>(options =>
{
    options.UseNpgsql(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null));
});

builder.Services.AddOpenApi();


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

using var log = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "log-.txt", 
        rollingInterval: RollingInterval.Day, 
        restrictedToMinimumLevel: LogEventLevel.Warning)
    .CreateLogger();

Log.Logger = log;

builder.Services.AddSerilog();
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.Configure<EmailSenderOptions>(
    builder.Configuration.GetSection("EmailSender"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddTransient<IReminderService, ReminderService>();
builder.Services.AddTransient<ISubscriptionService, SubscriptionService>();
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IPendingEmailService, PendingEmailService>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

var retries = 10;
while (retries > 0)
{
    try
    {
        db.Database.Migrate();
        Log.Information("Database migrated successfully.");
        break;
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07")
    {
        Log.Warning("Table already exists: {Message}", ex.Message);
        break;
    }
    catch (Exception ex)
    {
        retries--;
        Log.Warning(ex, "Database not ready, retrying... {Retries} attempts left.", retries);
        Thread.Sleep(5000);
    }
}

app.UseCors("AllowFrontend");
app.UseExceptionHandler();

app.UseRateLimiter(new RateLimiterOptions
{
    OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Try again later.", token);
    }
});
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEndpoints();
app.MapHangfireDashboard();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

try
{
    RecurringJob.AddOrUpdate<ISubscriptionService>(
        "process-subscription-renewals",
        service => service.ProcessSubscriptionRenewalAsync(CancellationToken.None),
        "0 2 * * *");
    
    RecurringJob.AddOrUpdate<IReminderService>(
        "process-reminders",
        service => service.ProcessDueRemindersAsync(CancellationToken.None),
        Cron.Minutely);
    
    RecurringJob.AddOrUpdate<IPendingEmailService>(
        "process-pending-emails",
        service => service.ProcessPendingEmailsAsync(CancellationToken.None),
        "*/10 * * * *");
}
catch (PostgreSqlDistributedLockException ex)
{
    Console.WriteLine("Could not acquire lock, skipping job update.");
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


public partial class Program { }