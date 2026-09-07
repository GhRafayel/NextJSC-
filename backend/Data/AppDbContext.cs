using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<User> Users => Set<User>();
    public DbSet<Translations> Translations => Set<Translations>();
    public DbSet<Session> Sessions => Set<Session>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       modelBuilder.Entity<User>(e =>
       {
           e.HasIndex( u => u.Email).IsUnique();
           e.HasIndex( u => new { u.Provider, u.ProviderId }).IsUnique();
           e.Property( u => u.Role).HasConversion<string>();
       });

       modelBuilder.Entity<Session> ( e =>
       {
            e.HasIndex(s => s.RefreshToken).IsUnique();
            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
       });

       modelBuilder.Entity<Translations> (e =>
       {
           e.HasIndex(t => t.Key).IsUnique();
           e.Property(t => t.Values).HasColumnType("jsonb");
       });
    }
    public override int SaveChanges()
    {
        TouchTimestamps();
        return base.SaveChanges();

    }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        TouchTimestamps();
        return base.SaveChangesAsync(ct);
    }

    private void TouchTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}