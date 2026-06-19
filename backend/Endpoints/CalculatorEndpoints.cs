using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Services;

namespace CalorieDiary.Api.Endpoints;

public static class CalculatorEndpoints
{
    public static IEndpointRouteBuilder MapCalculatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/calculator", Calculate)
            .WithName("CalculateCalories")
            .WithTags("Calculator")
            .AllowAnonymous()
            .WithOpenApi();

        return app;
    }

    private static IResult Calculate(
        CalorieCalculationRequest request,
        CalorieCalculatorService calculator,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(CalculatorEndpoints));
        var errors = calculator.Validate(request);

        if (errors.Count > 0)
        {
            logger.LogInformation("Calorie calculation rejected because request is invalid.");
            return Results.BadRequest(new ErrorResponse("Validation failed.", errors));
        }

        return Results.Ok(calculator.Calculate(request));
    }
}
