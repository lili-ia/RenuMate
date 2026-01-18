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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RenuMate.Api.Converters;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");

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
            
        options.Events = new JwtBearerEvents
        {
            OnForbidden = async context =>
            {
                var problem = new ProblemDetails
                {
                    Title = "Email not verified",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "You must verify your email address before accessing this resource.",
                    Instance = context.Request.Path
                };
                await context.Response.WriteAsJsonAsync(problem);
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedEmailOnly", policy => 
    {
        policy.RequireAssertion(context => 
        {
            var emailVerifiedClaim = context.User.FindFirst("http://renumate.online/email_verified");

            return emailVerifiedClaim != null && 
                   emailVerifiedClaim.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
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
builder.Services.AddLogging();
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection("EmailSender"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddTransient<IReminderService, ReminderService>();
builder.Services.AddTransient<ISubscriptionService, SubscriptionService>();
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

var retries = 10;
while (retries > 0)
{
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrated successfully.");
        break;
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07")
    {
        logger.LogWarning("Table already exists: {Message}", ex.Message);
        break;
    }
    catch (Exception ex)
    {
        retries--;
        logger.LogWarning(ex, "Database not ready, retrying... {Retries} attempts left.", retries);
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
}
catch (PostgreSqlDistributedLockException ex)
{
    Console.WriteLine("Could not acquire lock, skipping job update.");
}

app.Run();