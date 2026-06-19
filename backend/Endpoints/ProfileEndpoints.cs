using CalorieDiary.Api.Data;
using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Models;
using CalorieDiary.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("", GetProfileAsync)
            .WithName("GetProfile")
            .WithOpenApi();

        group.MapPut("", SaveProfileAsync)
            .WithName("SaveProfile")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetProfileAsync(
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        CalorieCalculatorService calculator,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(ProfileEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Profile request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value);

        if (profile is null)
        {
            logger.LogInformation("Profile for user {UserId} was not found.", userId);
            return Results.NotFound(new ErrorResponse("Profile is not created yet."));
        }

        logger.LogInformation("Profile requested for user {UserId}.", userId);

        var result = calculator.Calculate(ToCalculationRequest(profile));

        return Results.Ok(ToResponse(profile, result));
    }

    private static async Task<IResult> SaveProfileAsync(
        CalorieCalculationRequest request,
        CurrentUserService currentUserService,
        AppDbContext dbContext,
        CalorieCalculatorService calculator,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(ProfileEndpoints));
        var userId = currentUserService.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("Profile save request did not contain a valid user id claim.");
            return Unauthorized("User id claim is missing or invalid.");
        }

        var errors = calculator.Validate(request);

        if (errors.Count > 0)
        {
            logger.LogInformation("Profile save rejected for user {UserId} because request is invalid.", userId);
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        var userExists = await dbContext.Users.AnyAsync(user => user.Id == userId.Value);

        if (!userExists)
        {
            logger.LogWarning("Profile save failed because user {UserId} was not found.", userId);
            return Results.NotFound(new ErrorResponse("User from token was not found."));
        }

        var normalizedRequest = CalorieCalculatorService.NormalizeRequest(request);
        var result = calculator.Calculate(normalizedRequest);
        var profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value);

        if (profile is null)
        {
            profile = new UserProfile { UserId = userId.Value };
            dbContext.UserProfiles.Add(profile);
        }

        profile.Gender = normalizedRequest.Gender;
        profile.Age = normalizedRequest.Age;
        profile.HeightCm = normalizedRequest.HeightCm;
        profile.WeightKg = normalizedRequest.WeightKg;
        profile.ActivityLevel = normalizedRequest.ActivityLevel;
        profile.Goal = normalizedRequest.Goal;
        profile.DailyCaloriesTarget = result.DailyCaloriesTarget;
        profile.ProteinTarget = result.ProteinTarget;
        profile.FatTarget = result.FatTarget;
        profile.CarbsTarget = result.CarbsTarget;

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Profile saved for user {UserId}.", userId);

        return Results.Ok(ToResponse(profile, result));
    }

    private static UserProfileResponse ToResponse(
        UserProfile profile,
        CalorieCalculationResponse result)
    {
        return new UserProfileResponse(
            profile.Id,
            profile.UserId,
            profile.Gender,
            profile.Age,
            profile.HeightCm,
            profile.WeightKg,
            profile.ActivityLevel,
            profile.Goal,
            result.Bmr,
            result.Tdee,
            profile.DailyCaloriesTarget,
            profile.ProteinTarget,
            profile.FatTarget,
            profile.CarbsTarget);
    }

    private static CalorieCalculationRequest ToCalculationRequest(UserProfile profile)
    {
        return new CalorieCalculationRequest(
            profile.Gender,
            profile.Age,
            profile.HeightCm,
            profile.WeightKg,
            profile.ActivityLevel,
            profile.Goal);
    }

    private static IResult Unauthorized(string error)
    {
        return Results.Json(
            new ErrorResponse(error),
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
