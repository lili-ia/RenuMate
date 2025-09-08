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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
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
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Currency)
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
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(r => new { r.SubscriptionId, r.ReminderDate });

            entity.Property(r => r.IsSent)
                .IsRequired()
                .HasDefaultValue(false);

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