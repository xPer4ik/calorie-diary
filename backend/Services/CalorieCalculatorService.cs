using CalorieDiary.Api.Dtos;

namespace CalorieDiary.Api.Services;

public class CalorieCalculatorService(ILogger<CalorieCalculatorService> logger)
{
    private static readonly Dictionary<string, decimal> ActivityCoefficients = new()
    {
        ["low"] = 1.2m,
        ["light"] = 1.375m,
        ["moderate"] = 1.55m,
        ["high"] = 1.725m
    };

    private static readonly HashSet<string> Genders = ["male", "female"];
    private static readonly HashSet<string> Goals = ["lose", "maintain", "gain"];

    public CalorieCalculationResponse Calculate(CalorieCalculationRequest request)
    {
        var gender = Normalize(request.Gender);
        var activityLevel = Normalize(request.ActivityLevel);
        var goal = Normalize(request.Goal);

        var bmr = gender == "male"
            ? 10m * request.WeightKg + 6.25m * request.HeightCm - 5m * request.Age + 5m
            : 10m * request.WeightKg + 6.25m * request.HeightCm - 5m * request.Age - 161m;

        var tdee = bmr * ActivityCoefficients[activityLevel];
        var targetCalories = goal switch
        {
            "lose" => tdee * 0.85m,
            "gain" => tdee * 1.10m,
            _ => tdee
        };

        var dailyCaloriesTarget = (int)Math.Round(targetCalories, MidpointRounding.AwayFromZero);
        var proteinTarget = Math.Round(request.WeightKg * 1.8m, 1, MidpointRounding.AwayFromZero);
        var fatTarget = Math.Round(dailyCaloriesTarget * 0.25m / 9m, 1, MidpointRounding.AwayFromZero);
        var carbsTarget = Math.Round(
            Math.Max(0, dailyCaloriesTarget - proteinTarget * 4m - fatTarget * 9m) / 4m,
            1,
            MidpointRounding.AwayFromZero);

        logger.LogInformation(
            "Calculated calorie target {DailyCaloriesTarget} for goal {Goal} and activity {ActivityLevel}.",
            dailyCaloriesTarget,
            goal,
            activityLevel);

        return new CalorieCalculationResponse(
            Bmr: Math.Round(bmr, 1, MidpointRounding.AwayFromZero),
            Tdee: Math.Round(tdee, 1, MidpointRounding.AwayFromZero),
            DailyCaloriesTarget: dailyCaloriesTarget,
            ProteinTarget: proteinTarget,
            FatTarget: fatTarget,
            CarbsTarget: carbsTarget);
    }

    public Dictionary<string, string[]> Validate(CalorieCalculationRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var gender = Normalize(request.Gender);
        var activityLevel = Normalize(request.ActivityLevel);
        var goal = Normalize(request.Goal);

        if (!Genders.Contains(gender))
        {
            errors["gender"] = ["Gender must be male or female."];
        }

        if (request.Age is < 10 or > 100)
        {
            errors["age"] = ["Age must be between 10 and 100."];
        }

        if (request.HeightCm is < 100 or > 230)
        {
            errors["heightCm"] = ["Height must be between 100 and 230 cm."];
        }

        if (request.WeightKg is < 30 or > 250)
        {
            errors["weightKg"] = ["Weight must be between 30 and 250 kg."];
        }

        if (!ActivityCoefficients.ContainsKey(activityLevel))
        {
            errors["activityLevel"] = ["Activity level must be low, light, moderate, or high."];
        }

        if (!Goals.Contains(goal))
        {
            errors["goal"] = ["Goal must be lose, maintain, or gain."];
        }

        return errors;
    }

    public static CalorieCalculationRequest NormalizeRequest(CalorieCalculationRequest request)
    {
        return request with
        {
            Gender = Normalize(request.Gender),
            ActivityLevel = Normalize(request.ActivityLevel),
            Goal = Normalize(request.Goal)
        };
    }

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
