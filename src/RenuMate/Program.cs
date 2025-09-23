using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RenuMate.Extensions;
using RenuMate.Middleware;
using RenuMate.Persistence;
using RenuMate.Services;
using RenuMate.Services.Contracts;
using RenuMate.Services.Email;
using IEmailSender = RenuMate.Services.Contracts.IEmailSender;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");

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

builder.Services.AddAuthentication();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmailConfirmed", policy => 
        policy.RequireAssertion(context => context.User.HasClaim("emailconfirmed", "true")));
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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]))
        };

        options.MapInboundClaims = false;
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddLogging();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddTransient<IPasswordHasher, PasswordHasher>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddTransient<IReminderService, ReminderService>();
builder.Services.AddTransient<ISubscriptionService, SubscriptionService>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();

var app = builder.Build();

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

app.UseHttpsRedirection();
app.MapEndpoints();
app.MapHangfireDashboard();

RecurringJob.AddOrUpdate<ISubscriptionService>(
    "process-subscription-renewals",
    service => service.ProcessSubscriptionRenewalAsync(CancellationToken.None), 
    "0 2 * * *");

RecurringJob.AddOrUpdate<IReminderService>(
    "process-reminders",
    service => service.ProcessDueRemindersAsync(),
    Cron.Minutely);

var now = DateTime.UtcNow;
Console.WriteLine(now);

app.Run();