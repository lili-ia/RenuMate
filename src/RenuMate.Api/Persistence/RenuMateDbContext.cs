using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Api.Entities;
using RenuMate.Api.Enums;

namespace RenuMate.Api.Persistence;

public class RenuMateDbContext : DbContext
{
    public RenuMateDbContext(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public RenuMateDbContext(DbContextOptions<RenuMateDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }
    
    public virtual DbSet<User> Users { get; set; }
    
    public virtual DbSet<Subscription> Subscriptions { get; set; }
    
    public virtual DbSet<ReminderRule> ReminderRules { get; set; }
    
    public virtual DbSet<ReminderOccurrence> ReminderOccurrences { get; set; }
    
    public virtual DbSet<PendingEmail> PendingEmails { get; set; }
    
    public virtual DbSet<Tag> Tags { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var domainEvents = ChangeTracker
            .Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();
        
        ChangeTracker
            .Entries<BaseEntity>()
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());

        var orphansToDelete = ChangeTracker.Entries<ReminderOccurrence>()
            .Where(e => e.State == EntityState.Modified && 
                        e.Property(x => x.ReminderRuleId).IsModified && 
                        e.Entity.ReminderRuleId == null && 
                        !e.Entity.IsSent);

        foreach (var entry in orphansToDelete)
        {
            entry.State = EntityState.Deleted;
        }
        
        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
            
        return result;
    }
    
    private readonly IMediator _mediator;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(e => e.Id)
                .ValueGeneratedNever();
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasIndex(e => e.Auth0Id)
                .IsUnique();
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Auth0Id)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            
            entity.Property(e => e.Id)
                .ValueGeneratedNever();
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.UserId, e.Name })
                .IsUnique();
            
            entity.HasIndex(e => new { e.UserId, e.StartDate });
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Plan)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(SubscriptionPlan.Monthly);
            
            entity.Property(r => r.IsMuted)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.Currency)
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasDefaultValue(Currency.USD);
            
            modelBuilder.Entity<Subscription>()
                .Property(s => s.Note)
                .HasMaxLength(500)
                .IsUnicode(true);
            
            entity.HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ReminderRule>(entity =>
        {
            entity.ToTable("ReminderRules");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .ValueGeneratedNever();
            
            entity.Property(r => r.DaysBeforeRenewal)
                .IsRequired()
                .HasDefaultValue(1);
            
            entity.Property(r => r.NotifyTimeUtc)
                .IsRequired()
                .HasDefaultValue(new TimeSpan(9, 0, 0));

            entity.HasIndex(r => r.SubscriptionId);
            
            entity.HasIndex(r => new { r.SubscriptionId, r.NotifyTimeUtc, r.DaysBeforeRenewal })
                .IsUnique();

            entity.HasOne(r => r.Subscription)
                .WithMany(s => s.Reminders) 
                .HasForeignKey(r => r.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(r => r.ReminderOccurrences)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ReminderOccurrence>(entity =>
        {
            entity.ToTable("ReminderOccurrences");

            entity.HasKey(o => o.Id);
            
            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(o => o.ScheduledAt)
                .IsRequired();

            entity.HasIndex(o => new { o.ReminderRuleId, o.ScheduledAt });
            
            entity.HasIndex(o => new { o.IsSent, o.ScheduledAt });
            
            entity.Property(o => o.IsSent)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(o => o.SentAt)
                .IsRequired(false);

            entity.HasOne(o => o.ReminderRule)
                .WithMany(r => r.ReminderOccurrences) 
                .HasForeignKey(o => o.ReminderRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<PendingEmail>(entity =>
        {
            entity.ToTable("PendingEmails");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.To)
                .IsRequired();
            
            entity.Property(e => e.Subject)
                .IsRequired();
            
            entity.Property(e => e.Body)
                .IsRequired();

            entity.Property(e => e.RetryCount);
            
            entity.Property(e => e.MaxRetries);

            entity.HasIndex(e => new { e.IsSent, e.RetryCount });
        });
        
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");

            entity.HasKey(e => e.Id);

            entity.Property(t => t.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(t => t.Color)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(t => t.IsSystem)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(t => t.UserId)
                .IsRequired(false);

            entity.Metadata
                .FindNavigation(nameof(Tag.Subscriptions))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(t => t.Subscriptions)
                .WithMany(s => s.Tags)
                .UsingEntity(j => j.ToTable("SubscriptionTags")); 
            
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.IsSystem);
            
            // for custom tags
            entity.HasIndex(t => new { t.UserId, t.Name })
                .IsUnique()
                .HasFilter("\"UserId\" IS NOT NULL"); 

            // for system tags
            entity.HasIndex(t => t.Name)
                .IsUnique()
                .HasFilter("\"IsSystem\" = true");

            modelBuilder.Entity<Tag>().HasData(
                Tag.CreateSystem(_streamingId, "Streaming", "#E91E63", SystemDataTimestamp),
                Tag.CreateSystem(_gamingId, "Gaming", "#9C27B0", SystemDataTimestamp),
                Tag.CreateSystem(_newsId, "News & Media", "#673AB7", SystemDataTimestamp),
                Tag.CreateSystem(_workId, "Work", "#2196F3", SystemDataTimestamp),
                Tag.CreateSystem(_educationId, "Education", "#03A9F4", SystemDataTimestamp),
                Tag.CreateSystem(_financeId, "Finance", "#FF9800", SystemDataTimestamp),
                Tag.CreateSystem(_sportId, "Sport", "#4CAF50", SystemDataTimestamp),
                Tag.CreateSystem(_healthId, "Health", "#8BC34A", SystemDataTimestamp),
                Tag.CreateSystem(_foodId, "Food & Drinks", "#FF5722", SystemDataTimestamp),
                Tag.CreateSystem(_telephonyId, "Telephony", "#00BCD4", SystemDataTimestamp),
                Tag.CreateSystem(_cloudId, "Cloud Storage", "#607D8B", SystemDataTimestamp),
                Tag.CreateSystem(_securityId, "Security", "#3F51B5", SystemDataTimestamp),
                Tag.CreateSystem(_householdId, "Household", "#795548", SystemDataTimestamp),
                Tag.CreateSystem(_shoppingId, "Shopping", "#FFC107", SystemDataTimestamp),
                Tag.CreateSystem(_lifestyleId, "Lifestyle", "#009688", SystemDataTimestamp),
                Tag.CreateSystem(_petsId, "Pets", "#F44336", SystemDataTimestamp)
            );
        });
        
    }

    private readonly Guid _streamingId = Guid.Parse("4AA91298-37A1-46E8-B1F8-5B31882C6C7E");
    private readonly Guid _gamingId = Guid.Parse("E9E68348-DCD2-4904-B1B1-91A1883C720C");
    private readonly Guid _newsId = Guid.Parse("B2F40FAA-F537-4020-9792-B79874E747EC");
    private readonly Guid _workId = Guid.Parse("D545838B-28B2-4FBC-9191-35F9BF968C5F");
    private readonly Guid _educationId = Guid.Parse("7F6B33D8-D0AD-4F28-9FF1-059CC7E1BDE7");
    private readonly Guid _financeId = Guid.Parse("20B1723D-B723-4F54-9D7F-B72ACCF8ECAE");
    private readonly Guid _sportId = Guid.Parse("10E9D371-E94F-4F81-B739-94A3038803DF"); 
    private readonly Guid _healthId = Guid.Parse("F3B512F7-A07A-45B8-B9F1-604CB574AE27");
    private readonly Guid _foodId = Guid.Parse("9EB8C6C5-FB75-48CB-8886-BFFA682DE635");
    private readonly Guid _telephonyId = Guid.Parse("670F7A67-07A5-4941-940F-E29150DA1037");
    private readonly Guid _cloudId = Guid.Parse("538AC968-6AB4-4341-9777-DEC6F9EB4FB4");
    private readonly Guid _securityId = Guid.Parse("97F23909-EDFF-4401-AE5F-0747587D136C");
    private readonly Guid _householdId = Guid.Parse("A40355C0-87E9-4558-ABE7-22EA6C4DA41D");
    private readonly Guid _shoppingId = Guid.Parse("62E4982E-4875-49F6-8FAB-2634CD26489E");
    private readonly Guid _lifestyleId = Guid.Parse("C30588EB-C063-410F-985C-2C9FF063D2FC");
    private readonly Guid _petsId = Guid.Parse("DE3E6465-7993-4A55-8AB7-792B107EBE17");
    private static readonly DateTime SystemDataTimestamp = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
}