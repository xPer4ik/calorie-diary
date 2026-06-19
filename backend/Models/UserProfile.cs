namespace CalorieDiary.Api.Models;

public class UserProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Gender { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal HeightCm { get; set; }

    public decimal WeightKg { get; set; }

    public string ActivityLevel { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public int DailyCaloriesTarget { get; set; }

    public decimal ProteinTarget { get; set; }

    public decimal FatTarget { get; set; }

    public decimal CarbsTarget { get; set; }

    public User? User { get; set; }
}
