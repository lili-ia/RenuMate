using MediatR;
using Microsoft.EntityFrameworkCore;
using RenuMate.Entities;
using RenuMate.Enums;

namespace RenuMate.Persistence;

public class RenuMateDbContext : DbContext
{
    private readonly IMediator _mediator;
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

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
            
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired();
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(r => r.UserId);
            
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
            
            entity.Property(r => r.DaysBeforeRenewal)
                .IsRequired()
                .HasDefaultValue(1);
            
            entity.Property(r => r.NotifyTime)
                .IsRequired()
                .HasDefaultValue(new TimeSpan(9, 0, 0));

            entity.HasOne(r => r.Subscription)
                .WithMany(s => s.Reminders) 
                .HasForeignKey(r => r.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReminderOccurrence>(entity =>
        {
            entity.ToTable("ReminderOccurrences");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.ScheduledAt)
                .IsRequired();

            entity.Property(o => o.IsSent)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(o => o.SentAt)
                .IsRequired(false);

            entity.HasOne(o => o.ReminderRule)
                .WithMany(r => r.ReminderOccurrences) 
                .HasForeignKey(o => o.ReminderRuleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => o.ScheduledAt);
            entity.HasIndex(o => new { o.ReminderRuleId, o.IsSent });
        });
    }
}