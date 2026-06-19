namespace CalorieDiary.Api.Dtos;

public record FoodItemResponse(
    int Id,
    int? UserId,
    string Name,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal FatPer100g,
    decimal CarbsPer100g,
    bool IsSeed);

public record CreateFoodRequest(
    string Name,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal FatPer100g,
    decimal CarbsPer100g);

public record UpdateFoodRequest(
    string Name,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal FatPer100g,
    decimal CarbsPer100g);
