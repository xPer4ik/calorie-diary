using CalorieDiary.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CalorieDiary.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", (ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(HealthEndpoints));
            logger.LogInformation("Health check requested.");

            return Results.Ok(new
            {
                status = "ok",
                app = "Calorie Diary API"
            });
        })
        .WithName("GetHealth")
        .WithTags("Health")
        .WithOpenApi();

        app.MapGet("/api/health/db", async Task<IResult> (
            AppDbContext dbContext,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(HealthEndpoints));
            logger.LogInformation("Database health check requested.");

            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync();
                var productsCount = await dbContext.FoodItems.CountAsync();

                return Results.Ok(new
                {
                    status = canConnect ? "ok" : "error",
                    database = canConnect ? "connected" : "unavailable",
                    productsCount
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database health check failed.");

                return Results.Problem(
                    title: "Database health check failed",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetDatabaseHealth")
        .WithTags("Health")
        .WithOpenApi();

        return app;
    }
}
