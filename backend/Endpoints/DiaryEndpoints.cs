using System.Globalization;
using CalorieDiary.Api.Data;
using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Models;
using CalorieDiary.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Endpoints;

public static class DiaryEndpoints
{
    private static readonly HashSet<string> MealTypes = ["breakfast", "lunch", "dinner", "snack"];

    public static IEndpointRouteBuilder MapDiaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diary")
            .WithTags("Diary")
            .RequireAuthorization();

        group.MapGet("", GetDiaryAsync)
            .WithName("GetDiary")
            .WithOpenApi();

        group.MapPost("", CreateMealEntryAsync)
            .WithName("CreateMealEntry")
            .WithOpenApi();

        group.MapDelete("/{id:int}", DeleteMealEntryAsync)
            .WithName("DeleteMealEntry")
            .WithOpenApi();

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetDiarySummary")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetDiaryAsync(
        string? date,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(DiaryEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Diary request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        if (!TryParseDate(date, out var day))
        {
            return Results.BadRequest(new ErrorResponse("Validation failed.", new
            {
                date = new[] { "Date must use YYYY-MM-DD format." }
            }));
        }

        var entries = await dbContext.MealEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId.Value && entry.Date == day)
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync();

        logger.LogInformation("Returned {Count} diary entries for user {UserId} on {Date}.", entries.Count, userId, date);

        return Results.Ok(new DiaryDayResponse(FormatDate(day), entries.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> CreateMealEntryAsync(
        CreateMealEntryRequest request,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(DiaryEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Diary create request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var errors = ValidateCreateMealEntry(request);

        if (!TryParseDate(request.Date, out var day))
        {
            errors["date"] = ["Date must use YYYY-MM-DD format."];
        }

        if (errors.Count > 0)
        {
            logger.LogInformation("Diary create rejected for user {UserId} because request is invalid.", userId);
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        var food = await dbContext.FoodItems
            .AsNoTracking()
            .SingleOrDefaultAsync(food =>
                food.Id == request.FoodItemId &&
                (food.UserId == null || food.UserId == userId.Value));

        if (food is null)
        {
            logger.LogInformation("Diary create failed because food {FoodItemId} is unavailable for user {UserId}.", request.FoodItemId, userId);
            return Results.NotFound(new ErrorResponse("Food item was not found."));
        }

        var grams = RoundMacro(request.Grams);
        var entry = new MealEntry
        {
            UserId = userId.Value,
            FoodItemId = food.Id,
            Date = day,
            MealType = NormalizeMealType(request.MealType),
            FoodName = food.Name,
            Grams = grams,
            Calories = CalculateForGrams(food.CaloriesPer100g, grams),
            Protein = CalculateForGrams(food.ProteinPer100g, grams),
            Fat = CalculateForGrams(food.FatPer100g, grams),
            Carbs = CalculateForGrams(food.CarbsPer100g, grams),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.MealEntries.Add(entry);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Diary entry {EntryId} created for user {UserId}.", entry.Id, userId);

        return Results.Created($"/api/diary?date={FormatDate(day)}", ToResponse(entry));
    }

    private static async Task<IResult> DeleteMealEntryAsync(
        int id,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(DiaryEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Diary delete request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var entry = await dbContext.MealEntries.SingleOrDefaultAsync(entry => entry.Id == id);

        if (entry is null)
        {
            return Results.NotFound(new ErrorResponse("Diary entry was not found."));
        }

        if (entry.UserId != userId.Value)
        {
            logger.LogWarning("User {UserId} tried to delete forbidden diary entry {EntryId}.", userId, id);
            return Results.Forbid();
        }

        dbContext.MealEntries.Remove(entry);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Diary entry {EntryId} deleted by user {UserId}.", id, userId);

        return Results.NoContent();
    }

    private static async Task<IResult> GetSummaryAsync(
        string? date,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(DiaryEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Diary summary request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        if (!TryParseDate(date, out var day))
        {
            return Results.BadRequest(new ErrorResponse("Validation failed.", new
            {
                date = new[] { "Date must use YYYY-MM-DD format." }
            }));
        }

        var entries = await dbContext.MealEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId.Value && entry.Date == day)
            .ToListAsync();
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value);

        var calories = RoundMacro(entries.Sum(entry => entry.Calories));
        var protein = RoundMacro(entries.Sum(entry => entry.Protein));
        var fat = RoundMacro(entries.Sum(entry => entry.Fat));
        var carbs = RoundMacro(entries.Sum(entry => entry.Carbs));

        logger.LogInformation("Diary summary requested for user {UserId} on {Date}.", userId, date);

        return Results.Ok(new DiarySummaryResponse(
            Date: FormatDate(day),
            Calories: calories,
            Protein: protein,
            Fat: fat,
            Carbs: carbs,
            DailyCaloriesTarget: profile?.DailyCaloriesTarget,
            ProteinTarget: profile?.ProteinTarget,
            FatTarget: profile?.FatTarget,
            CarbsTarget: profile?.CarbsTarget,
            CaloriesRemaining: profile is null ? null : RoundMacro(profile.DailyCaloriesTarget - calories),
            ProteinRemaining: profile is null ? null : RoundMacro(profile.ProteinTarget - protein),
            FatRemaining: profile is null ? null : RoundMacro(profile.FatTarget - fat),
            CarbsRemaining: profile is null ? null : RoundMacro(profile.CarbsTarget - carbs)));
    }

    private static Dictionary<string, string[]> ValidateCreateMealEntry(CreateMealEntryRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var mealType = NormalizeMealType(request.MealType);

        if (request.FoodItemId <= 0)
        {
            errors["foodItemId"] = ["Food item id is required."];
        }

        if (request.Grams <= 0)
        {
            errors["grams"] = ["Grams must be greater than 0."];
        }

        if (!MealTypes.Contains(mealType))
        {
            errors["mealType"] = ["Meal type must be breakfast, lunch, dinner, or snack."];
        }

        return errors;
    }

    private static bool TryParseDate(string? date, out DateTime day)
    {
        day = default;

        if (string.IsNullOrWhiteSpace(date))
        {
            return false;
        }

        if (!DateOnly.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate))
        {
            return false;
        }

        day = parsedDate.ToDateTime(TimeOnly.MinValue);
        return true;
    }

    private static MealEntryResponse ToResponse(MealEntry entry)
    {
        return new MealEntryResponse(
            entry.Id,
            entry.UserId,
            entry.FoodItemId,
            FormatDate(entry.Date),
            entry.MealType,
            entry.FoodName,
            entry.Grams,
            entry.Calories,
            entry.Protein,
            entry.Fat,
            entry.Carbs,
            entry.CreatedAt);
    }

    private static decimal CalculateForGrams(decimal valuePer100g, decimal grams)
    {
        return RoundMacro(valuePer100g * grams / 100m);
    }

    private static decimal RoundMacro(decimal value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeMealType(string? mealType)
    {
        return mealType?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static IResult Unauthorized(string error)
    {
        return Results.Json(
            new ErrorResponse(error),
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
