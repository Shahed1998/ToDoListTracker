using Microsoft.EntityFrameworkCore;
using ToDoListTracker.Models;

namespace ToDoListTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TimeBoxEntry> TimeBoxEntries { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<SubEntry> SubEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeBoxEntry>()
            .Property(e => e.Status)
            .HasConversion<string>();

        // Date only ever represents a calendar date (see DataType.Date on the model), so map it to
        // Postgres's `date` type - avoids Npgsql's Kind=Utc requirement for `timestamp with time zone`.
        modelBuilder.Entity<TimeBoxEntry>()
            .Property(e => e.Date)
            .HasColumnType("date");

        // Date is the primary filter for both the daily view and the monthly report loop.
        modelBuilder.Entity<TimeBoxEntry>()
            .HasIndex(e => e.Date);

        modelBuilder.Entity<Category>()
            .Property(c => c.Type)
            .HasConversion<string>();

        modelBuilder.Entity<TimeBoxEntry>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Entries)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SubEntry>()
            .HasOne(s => s.TimeBoxEntry)
            .WithMany(e => e.SubEntries)
            .HasForeignKey(s => s.TimeBoxEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed starter categories so the app is usable immediately.
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Study", Type = CategoryType.Positive, ColorHex = "#4f46e5" },
            new Category { Id = 2, Name = "Work", Type = CategoryType.Positive, ColorHex = "#0891b2" },
            new Category { Id = 3, Name = "Personal Time", Type = CategoryType.Positive, ColorHex = "#059669" },
            new Category { Id = 4, Name = "Exercise", Type = CategoryType.Positive, ColorHex = "#d97706" },
            new Category { Id = 5, Name = "SCEcommerz", Type = CategoryType.Positive, ColorHex = "#7c3aed" },
            new Category { Id = 6, Name = "Procrastination", Type = CategoryType.Negative, ColorHex = "#dc2626" },
            new Category { Id = 7, Name = "Porn", Type = CategoryType.Negative, ColorHex = "#991b1b" },
            new Category { Id = 8, Name = "Oversleeping", Type = CategoryType.Negative, ColorHex = "#b91c1c" }
        );
    }
}
