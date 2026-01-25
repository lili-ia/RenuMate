using System.Net.Http.Headers;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RenuMate.Api.Enums;
using RenuMate.Api.Middleware;
using RenuMate.Api.Persistence;
using RenuMate.Api.Services.Contracts;
using Respawn;
using Testcontainers.PostgreSql;

namespace RenuMate.Api.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16")
        .Build();
    
    private Respawner _respawner = default!;
    private string _connectionString = default!;
    public Mock<IAuth0Service> Auth0ServiceMock { get; } = new();

    public Mock<ICurrencyService> CurrencyServiceMock { get; } = new();
    
    public Mock<ITokenService> TokenServiceMock { get; } = new();

    public Mock<IEmailSender> EmailSenderMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<RenuMateDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }
            
            services.AddDbContext<RenuMateDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
            
            services.AddHttpContextAccessor();
            
            var authHandlersDescriptors = services
                .Where(d => d.ServiceType == typeof(IAuthorizationHandler));
            
            services.RemoveAll<IAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, VerifiedEmailOnlyAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, ActiveUserOnlyAuthorizationHandler>();
            
            var authDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationService));
            
            if (authDescriptor is not null)
            {
                services.Remove(authDescriptor);
            }
            
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy("VerifiedEmailOnly", policy => 
                    policy.Requirements.Add(new EmailVerifiedRequirement()));
    
                options.AddPolicy("ActiveUserOnly", policy => 
                    policy.Requirements.Add(new ActiveUserRequirement()));
            });

            var auth0ServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuth0Service));
            
            if (auth0ServiceDescriptor is not null)
            {
                services.Remove(auth0ServiceDescriptor);
            }
            
            services.AddSingleton(Auth0ServiceMock.Object);
            
            var currencyDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICurrencyService));

            if (currencyDescriptor is not null)
            {
                services.Remove(currencyDescriptor);
            }
            
            CurrencyServiceMock.Setup(x => x.GetRateForDesiredCurrency(
                    It.IsAny<Currency>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Currency, decimal>
                {
                    { Currency.EUR, 1m },
                    { Currency.USD, 1.1m },
                    { Currency.GBP, 0.85m }
                });
            
            services.AddSingleton(CurrencyServiceMock.Object);
            
            var tokenDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITokenService));

            if (tokenDescriptor is not null)
            {
                services.Remove(tokenDescriptor);
            }
            
            services.AddSingleton(TokenServiceMock.Object);
            
            services.AddSingleton<IValidator<string>>(new InlineValidator<string>());
            
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailSender));

            if (emailDescriptor is not null)
            {
                services.Remove(emailDescriptor);
            }
            
            services.AddSingleton(EmailSenderMock.Object);
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _connectionString = _dbContainer.GetConnectionString();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        await db.Database.MigrateAsync();

        await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }
    
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
    
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
    
    public HttpClient GetAuthenticatedClient(
        string sub = "auth0|test-user-id", 
        string email = "test@example.com",
        string name = "Test User",
        string userId = "",
        bool emailVerified = true,
        bool isActive = true)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        client.DefaultRequestHeaders.Add("X-Test-Name", name);
        client.DefaultRequestHeaders.Add("X-Test-EmailVerified", emailVerified.ToString().ToLower());
        client.DefaultRequestHeaders.Add("X-Test-IsActive", isActive.ToString().ToLower());
    
        if (!string.IsNullOrEmpty(userId))
        {
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        }
    
        return client;
    }
    
    public async Task CreateExampleUserAsync(
        Guid userId, 
        string? email = "", 
        string? auth0Id = "", 
        string? name = "Name",
        bool? verified = true,
        bool? isActive = true)
    {
        if (string.IsNullOrEmpty(email))
        {
            email = Guid.NewGuid().ToString();
        }
        
        if (string.IsNullOrEmpty(auth0Id))
        {
            auth0Id = Guid.NewGuid().ToString();
        }
        
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users" ("Id", "Email", "Auth0Id", "Name", "EmailConfirmed", "IsActive", "IsMetadataSynced", "CreatedAt")
            VALUES ({userId}, {email}, {auth0Id}, {name}, {verified}, {isActive}, {true}, {DateTime.UtcNow});
        """);
    }
    
    public async Task CreateExampleSubscriptionAsync(
        Guid subId,
        Guid userId,
        DateTime startDate,
        string name = "",
        SubscriptionPlan plan = SubscriptionPlan.Monthly,
        decimal cost = 15m,
        Currency currency = Currency.EUR,
        bool isMuted = false, 
        int? customPeriodInDays = null,
        int? trialPeriodInDays = null,
        string? note = null,
        string? cancelLink = null,
        string? picLink = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        var now = DateTime.UtcNow;

        if (string.IsNullOrEmpty(name))
        {
            name = Guid.NewGuid().ToString();
        }
        
        var renewalDate = plan switch
        {
            SubscriptionPlan.Trial => startDate.AddDays(trialPeriodInDays ?? 7),
            SubscriptionPlan.Custom => startDate.AddDays(customPeriodInDays ?? 30),
            SubscriptionPlan.Annual => startDate.AddYears(1),
            SubscriptionPlan.Quarterly => startDate.AddMonths(3),
            _ => startDate.AddMonths(1) 
        };

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Subscriptions" 
               ("Id", "UserId", "Name", "Plan", "Cost", "Currency", "StartDate", "RenewalDate", "CustomPeriodInDays", 
                "TrialPeriodInDays", "Note", "CancelLink", "PicLink", "CreatedAt", "IsMuted")
            VALUES 
               ({subId}, {userId}, {name}, {plan}, {cost}, {currency}, {startDate}, {renewalDate}, {customPeriodInDays},
                {trialPeriodInDays}, {note}, {cancelLink}, {picLink}, {now}, {isMuted});
        """);
    }
    
    public async Task CreateExampleReminderRuleAsync(
        Guid reminderRuleId,
        Guid? reminderOccurrenceId,
        Guid subscriptionId,
        int daysBeforeRenewal = 3,
        TimeSpan? notifyTimeUtc = null,
        bool createOccurrence = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();

        notifyTimeUtc ??= TimeSpan.FromHours(9);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ReminderRules"
               ("Id", "SubscriptionId", "DaysBeforeRenewal", "NotifyTimeUtc", "CreatedAt")
            VALUES
               ({reminderRuleId}, {subscriptionId}, {daysBeforeRenewal}, {notifyTimeUtc}, {DateTime.UtcNow});
        """);

        if (!createOccurrence)
        {
            return;
        }

        var scheduledAt = DateTime.UtcNow
            .Date
            .AddDays(10 - daysBeforeRenewal)
            .Add(notifyTimeUtc.Value);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ReminderOccurrences"
               ("Id", "ReminderRuleId", "ScheduledAt", "IsSent", "CreatedAt")
            VALUES
               ({reminderOccurrenceId}, {reminderRuleId}, {scheduledAt}, {false}, {DateTime.UtcNow});
        """);
    }

    public async Task CreateExampleReminderOccurrenceAsync(
        Guid ruleId,
        Guid occurrenceId,
        bool isSent = false)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RenuMateDbContext>();
        
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ReminderOccurrences"
              ("Id", "ReminderRuleId", "ScheduledAt", "IsSent", "CreatedAt")
            VALUES
              ({occurrenceId}, {ruleId}, {DateTime.UtcNow.AddDays(25)}, {isSent}, {DateTime.UtcNow});
        """);
    }
}