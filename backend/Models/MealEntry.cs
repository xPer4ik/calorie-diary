namespace CalorieDiary.Api.Models;

public class MealEntry
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? FoodItemId { get; set; }

    public DateTime Date { get; set; }

    public string MealType { get; set; } = string.Empty;

    public string FoodName { get; set; } = string.Empty;

    public decimal Grams { get; set; }

    public decimal Calories { get; set; }

    public decimal Protein { get; set; }

    public decimal Fat { get; set; }

    public decimal Carbs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public FoodItem? FoodItem { get; set; }
}
