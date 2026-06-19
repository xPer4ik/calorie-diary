using CalorieDiary.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<FoodItem> FoodItems => Set<FoodItem>();

    public DbSet<MealEntry> MealEntries => Set<MealEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.Property(profile => profile.Gender).HasMaxLength(30).IsRequired();
            entity.Property(profile => profile.ActivityLevel).HasMaxLength(50).IsRequired();
            entity.Property(profile => profile.Goal).HasMaxLength(50).IsRequired();

            entity
                .HasOne(profile => profile.User)
                .WithOne(user => user.Profile)
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FoodItem>(entity =>
        {
            entity.Property(food => food.Name).HasMaxLength(150).IsRequired();

            entity
                .HasOne(food => food.User)
                .WithMany(user => user.FoodItems)
                .HasForeignKey(food => food.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MealEntry>(entity =>
        {
            entity.Property(meal => meal.MealType).HasMaxLength(50).IsRequired();
            entity.Property(meal => meal.FoodName).HasMaxLength(150).IsRequired();

            entity
                .HasOne(meal => meal.User)
                .WithMany(user => user.MealEntries)
                .HasForeignKey(meal => meal.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(meal => meal.FoodItem)
                .WithMany(food => food.MealEntries)
                .HasForeignKey(meal => meal.FoodItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
