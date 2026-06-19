using CalorieDiary.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        var seedItems = new[]
        {
            new FoodItem
            {
                Name = "Овсянка",
                CaloriesPer100g = 379,
                ProteinPer100g = 13.2m,
                FatPer100g = 6.5m,
                CarbsPer100g = 67.7m
            },
            new FoodItem
            {
                Name = "Куриная грудка",
                CaloriesPer100g = 165,
                ProteinPer100g = 31,
                FatPer100g = 3.6m,
                CarbsPer100g = 0
            },
            new FoodItem
            {
                Name = "Рис",
                CaloriesPer100g = 130,
                ProteinPer100g = 2.7m,
                FatPer100g = 0.3m,
                CarbsPer100g = 28.2m
            },
            new FoodItem
            {
                Name = "Яйцо",
                CaloriesPer100g = 155,
                ProteinPer100g = 13,
                FatPer100g = 11,
                CarbsPer100g = 1.1m
            },
            new FoodItem
            {
                Name = "Яблоко",
                CaloriesPer100g = 52,
                ProteinPer100g = 0.3m,
                FatPer100g = 0.2m,
                CarbsPer100g = 13.8m
            },
            new FoodItem
            {
                Name = "Творог",
                CaloriesPer100g = 121,
                ProteinPer100g = 17,
                FatPer100g = 5,
                CarbsPer100g = 1.8m
            }
        };

        var existingNames = await dbContext.FoodItems
            .Where(item => item.UserId == null)
            .Select(item => item.Name)
            .ToListAsync();

        var newItems = seedItems
            .Where(item => !existingNames.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (newItems.Count == 0)
        {
            logger.LogInformation("Seed food items already exist.");
            return;
        }

        dbContext.FoodItems.AddRange(newItems);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} food items.", newItems.Count);
    }
}
