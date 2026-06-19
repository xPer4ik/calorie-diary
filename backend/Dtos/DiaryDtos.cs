namespace CalorieDiary.Api.Dtos;

public record CreateMealEntryRequest(
    string Date,
    string MealType,
    int FoodItemId,
    decimal Grams);

public record MealEntryResponse(
    int Id,
    int UserId,
    int? FoodItemId,
    string Date,
    string MealType,
    string FoodName,
    decimal Grams,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    DateTime CreatedAt);

public record DiaryDayResponse(
    string Date,
    IReadOnlyList<MealEntryResponse> Entries);

public record DiarySummaryResponse(
    string Date,
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbs,
    int? DailyCaloriesTarget,
    decimal? ProteinTarget,
    decimal? FatTarget,
    decimal? CarbsTarget,
    decimal? CaloriesRemaining,
    decimal? ProteinRemaining,
    decimal? FatRemaining,
    decimal? CarbsRemaining);
