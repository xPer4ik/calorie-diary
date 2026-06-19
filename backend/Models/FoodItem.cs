namespace CalorieDiary.Api.Models;

public class FoodItem
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal CaloriesPer100g { get; set; }

    public decimal ProteinPer100g { get; set; }

    public decimal FatPer100g { get; set; }

    public decimal CarbsPer100g { get; set; }

    public User? User { get; set; }

    public List<MealEntry> MealEntries { get; set; } = [];
}
