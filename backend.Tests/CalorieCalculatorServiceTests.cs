using CalorieDiary.Api.Dtos;
using CalorieDiary.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.Tests;

public class CalorieCalculatorServiceTests
{
    private readonly CalorieCalculatorService _calculator = new(
        NullLogger<CalorieCalculatorService>.Instance);

    [Theory]
    [InlineData("male", 1780.0)]
    [InlineData("female", 1614.0)]
    public void Calculate_UsesMifflinStJeorFormulaForGender(
        string gender,
        double expectedBmr)
    {
        var result = _calculator.Calculate(Request(gender: gender));

        Assert.Equal((decimal)expectedBmr, result.Bmr);
    }

    [Theory]
    [InlineData("lose", 2345)]
    [InlineData("maintain", 2759)]
    [InlineData("gain", 3035)]
    public void Calculate_AppliesGoalToDailyCaloriesTarget(
        string goal,
        int expectedTarget)
    {
        var result = _calculator.Calculate(Request(goal: goal));

        Assert.Equal(expectedTarget, result.DailyCaloriesTarget);
    }

    [Theory]
    [InlineData("low", 2136.0)]
    [InlineData("light", 2447.5)]
    [InlineData("moderate", 2759.0)]
    [InlineData("high", 3070.5)]
    public void Calculate_AppliesActivityCoefficient(
        string activityLevel,
        double expectedTdee)
    {
        var result = _calculator.Calculate(Request(activityLevel: activityLevel));

        Assert.Equal((decimal)expectedTdee, result.Tdee);
    }

    private static CalorieCalculationRequest Request(
        string gender = "male",
        string activityLevel = "moderate",
        string goal = "maintain")
    {
        return new CalorieCalculationRequest(
            Gender: gender,
            Age: 30,
            HeightCm: 180,
            WeightKg: 80,
            ActivityLevel: activityLevel,
            Goal: goal);
    }
}
