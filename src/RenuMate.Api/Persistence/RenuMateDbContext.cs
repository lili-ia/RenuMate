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
    }
}