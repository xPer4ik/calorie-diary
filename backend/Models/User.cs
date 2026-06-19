namespace CalorieDiary.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile? Profile { get; set; }

    public List<FoodItem> FoodItems { get; set; } = [];

    public List<MealEntry> MealEntries { get; set; } = [];
}
