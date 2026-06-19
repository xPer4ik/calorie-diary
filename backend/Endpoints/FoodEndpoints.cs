using CalorieDiary.Api.Data;
using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Models;
using CalorieDiary.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Endpoints;

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/foods")
            .WithTags("Foods")
            .RequireAuthorization();

        group.MapGet("", GetFoodsAsync)
            .WithName("GetFoods")
            .WithOpenApi();

        group.MapPost("", CreateFoodAsync)
            .WithName("CreateFood")
            .WithOpenApi();

        group.MapPut("/{id:int}", UpdateFoodAsync)
            .WithName("UpdateFood")
            .WithOpenApi();

        group.MapDelete("/{id:int}", DeleteFoodAsync)
            .WithName("DeleteFood")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetFoodsAsync(
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(FoodEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Foods request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var foodItems = await dbContext.FoodItems
            .AsNoTracking()
            .Where(food => food.UserId == null || food.UserId == userId.Value)
            .OrderBy(food => food.UserId.HasValue)
            .ThenBy(food => food.Name)
            .ToListAsync();
        var foods = foodItems.Select(ToResponse).ToList();

        logger.LogInformation("Returned {Count} foods for user {UserId}.", foods.Count, userId);

        return Results.Ok(foods);
    }

    private static async Task<IResult> CreateFoodAsync(
        CreateFoodRequest request,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(FoodEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Food create request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var errors = ValidateFood(request.Name, request.CaloriesPer100g, request.ProteinPer100g, request.FatPer100g, request.CarbsPer100g);

        if (errors.Count > 0)
        {
            logger.LogInformation("Food create rejected for user {UserId} because request is invalid.", userId);
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        var food = new FoodItem
        {
            UserId = userId.Value,
            Name = request.Name.Trim(),
            CaloriesPer100g = RoundMacro(request.CaloriesPer100g),
            ProteinPer100g = RoundMacro(request.ProteinPer100g),
            FatPer100g = RoundMacro(request.FatPer100g),
            CarbsPer100g = RoundMacro(request.CarbsPer100g)
        };

        dbContext.FoodItems.Add(food);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Food {FoodId} created for user {UserId}.", food.Id, userId);

        return Results.Created($"/api/foods/{food.Id}", ToResponse(food));
    }

    private static async Task<IResult> UpdateFoodAsync(
        int id,
        UpdateFoodRequest request,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(FoodEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Food update request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var errors = ValidateFood(request.Name, request.CaloriesPer100g, request.ProteinPer100g, request.FatPer100g, request.CarbsPer100g);

        if (errors.Count > 0)
        {
            logger.LogInformation("Food update rejected for user {UserId} because request is invalid.", userId);
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        var food = await dbContext.FoodItems.SingleOrDefaultAsync(food => food.Id == id);

        if (food is null)
        {
            return Results.NotFound(new ErrorResponse("Food item was not found."));
        }

        if (food.UserId != userId.Value)
        {
            logger.LogWarning("User {UserId} tried to update forbidden food {FoodId}.", userId, id);
            return Results.Forbid();
        }

        food.Name = request.Name.Trim();
        food.CaloriesPer100g = RoundMacro(request.CaloriesPer100g);
        food.ProteinPer100g = RoundMacro(request.ProteinPer100g);
        food.FatPer100g = RoundMacro(request.FatPer100g);
        food.CarbsPer100g = RoundMacro(request.CarbsPer100g);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Food {FoodId} updated by user {UserId}.", id, userId);

        return Results.Ok(ToResponse(food));
    }

    private static async Task<IResult> DeleteFoodAsync(
        int id,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(FoodEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Food delete request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var food = await dbContext.FoodItems.SingleOrDefaultAsync(food => food.Id == id);

        if (food is null)
        {
            return Results.NotFound(new ErrorResponse("Food item was not found."));
        }

        if (food.UserId != userId.Value)
        {
            logger.LogWarning("User {UserId} tried to delete forbidden food {FoodId}.", userId, id);
            return Results.Forbid();
        }

        dbContext.FoodItems.Remove(food);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Food {FoodId} deleted by user {UserId}.", id, userId);

        return Results.NoContent();
    }

    private static Dictionary<string, string[]> ValidateFood(
        string? name,
        decimal caloriesPer100g,
        decimal proteinPer100g,
        decimal fatPer100g,
        decimal carbsPer100g)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Food name is required."];
        }
        else if (name.Trim().Length > 150)
        {
            errors["name"] = ["Food name must be 150 characters or fewer."];
        }

        if (caloriesPer100g < 0)
        {
            errors["caloriesPer100g"] = ["Calories per 100g must be greater than or equal to 0."];
        }

        if (proteinPer100g < 0)
        {
            errors["proteinPer100g"] = ["Protein per 100g must be greater than or equal to 0."];
        }

        if (fatPer100g < 0)
        {
            errors["fatPer100g"] = ["Fat per 100g must be greater than or equal to 0."];
        }

        if (carbsPer100g < 0)
        {
            errors["carbsPer100g"] = ["Carbs per 100g must be greater than or equal to 0."];
        }

        return errors;
    }

    private static FoodItemResponse ToResponse(FoodItem food)
    {
        return new FoodItemResponse(
            food.Id,
            food.UserId,
            food.Name,
            food.CaloriesPer100g,
            food.ProteinPer100g,
            food.FatPer100g,
            food.CarbsPer100g,
            food.UserId is null);
    }

    private static decimal RoundMacro(decimal value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }

    private static IResult Unauthorized(string error)
    {
        return Results.Json(
            new ErrorResponse(error),
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
