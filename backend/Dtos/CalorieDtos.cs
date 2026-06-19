namespace CalorieDiary.Api.Dtos;

public record CalorieCalculationRequest(
    string Gender,
    int Age,
    decimal HeightCm,
    decimal WeightKg,
    string ActivityLevel,
    string Goal);

public record CalorieCalculationResponse(
    decimal Bmr,
    decimal Tdee,
    int DailyCaloriesTarget,
    decimal ProteinTarget,
    decimal FatTarget,
    decimal CarbsTarget);

public record UserProfileResponse(
    int Id,
    int UserId,
    string Gender,
    int Age,
    decimal HeightCm,
    decimal WeightKg,
    string ActivityLevel,
    string Goal,
    decimal Bmr,
    decimal Tdee,
    int DailyCaloriesTarget,
    decimal ProteinTarget,
    decimal FatTarget,
    decimal CarbsTarget);
