using Microsoft.EntityFrameworkCore;
using RenuMate.Entities;
using RenuMate.Enums;

namespace RenuMate.Persistence;

public class RenuMateDbContext : DbContext
{
    public RenuMateDbContext()
    {
        
    }
    
    public RenuMateDbContext(DbContextOptions<RenuMateDbContext> options)
        : base(options)
    {
        
    }
    
    public virtual DbSet<User> Users { get; set; }
    
    public virtual DbSet<Subscription> Subscriptions { get; set; }
    
    public virtual DbSet<Reminder> Reminders { get; set; }

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

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(SubscriptionType.Monthly);
            
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
        
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.ToTable("Reminders");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(r => r.DaysBeforeRenewal)
                .IsRequired()
                .HasDefaultValue(1);
            
            entity.Property(r => r.NotifyTime)
                .IsRequired()
                .HasDefaultValue(new TimeSpan(9, 0, 0));

            entity.Property(r => r.IsMuted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.HasOne(r => r.Subscription)
                .WithMany(s => s.Reminders) 
                .HasForeignKey(r => r.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}