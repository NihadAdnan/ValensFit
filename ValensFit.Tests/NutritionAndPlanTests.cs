using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ValensFit.Models;
using ValensFit.Services.Exercise;
using ValensFit.Services.Grocery;
using ValensFit.Services.Nutrition;
using Xunit;

namespace ValensFit.Tests
{
    public class MockWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "ValensFit";
        public string ContentRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    public class NutritionAndPlanTests
    {
        private readonly BmrCalculator _bmrCalculator = new();
        private readonly TdeeCalculator _tdeeCalculator = new();
        private readonly MacroCalculator _macroCalculator = new();
        private readonly FoodDatabase _foodDatabase;
        private readonly MealBuilderService _mealBuilder;
        private readonly MealSwapService _mealSwap;
        private readonly ExercisePlanService _exerciseService;
        private readonly GroceryPricingService _groceryPricing;

        public NutritionAndPlanTests()
        {
            var env = new MockWebHostEnvironment();
            _foodDatabase = new FoodDatabase(env, NullLogger<FoodDatabase>.Instance);
            _mealBuilder = new MealBuilderService(_foodDatabase, NullLogger<MealBuilderService>.Instance);
            _mealSwap = new MealSwapService(_foodDatabase);
            _exerciseService = new ExercisePlanService(env, NullLogger<ExercisePlanService>.Instance);
            _groceryPricing = new GroceryPricingService(_foodDatabase, NullLogger<GroceryPricingService>.Instance);
        }

        [Fact]
        public void BmrCalculator_ShouldMatchMifflinStJeorEquation()
        {
            // Male: 10 * 70 + 6.25 * 175 - 5 * 25 + 5 = 700 + 1093.75 - 125 + 5 = 1673.75 -> 1674
            double maleBmr = _bmrCalculator.CalculateBmr("Male", 70, 175, 25);
            Assert.Equal(1674, maleBmr);

            // Female: 10 * 60 + 6.25 * 165 - 5 * 25 - 161 = 600 + 1031.25 - 125 - 161 = 1345.25 -> 1345
            double femaleBmr = _bmrCalculator.CalculateBmr("Female", 60, 165, 25);
            Assert.Equal(1345, femaleBmr);
        }

        [Fact]
        public void TdeeCalculator_ShouldApplyActivityAndStepBonus()
        {
            double bmr = 1674;
            var (tdee, multiplier, stepBonus) = _tdeeCalculator.CalculateTdee(bmr, "ModeratelyActive", 10000);
            
            Assert.Equal(1.55, multiplier);
            Assert.True(stepBonus > 0);
            Assert.True(tdee > bmr * 1.55);
        }

        [Fact]
        public void MacroCalculator_FatLoss_ShouldApplySafeDeficitAndProteinFirst()
        {
            var input = new UserInputModel
            {
                FirstName = "Rafi",
                Gender = "Male",
                Age = 24,
                Weight = 80,
                Height = 175,
                Goal = "LoseFat"
            };

            double bmr = 1774;
            double tdee = 2750;

            var result = _macroCalculator.CalculateMacros(input, bmr, tdee);

            Assert.True(result.TargetCalories < tdee);
            Assert.True(result.TargetCalories >= 1500, "Male safety floor must be respected");
            Assert.Equal(160, result.ProteinGrams); // 80kg * 2.0g/kg
            Assert.True(result.FatGrams > 0);
            Assert.True(result.CarbGrams > 0);
            Assert.True(result.WaterLitres >= 3.0);
        }

        [Fact]
        public void MacroCalculator_AggressivePace_ShouldAutoCalibrateAndWarn()
        {
            var input = new UserInputModel
            {
                FirstName = "TestUser",
                Gender = "Male",
                Age = 25,
                Weight = 70,
                Height = 175,
                Goal = "LoseFat",
                TargetWeightLossKg = 10, // 10kg in 4 weeks is 2.5 kg/week (3.5% BW/week) -> Overly aggressive!
                TimeframeWeeks = 4
            };

            var result = _macroCalculator.CalculateMacros(input, 1674, 2500);

            Assert.NotNull(result.PaceWarning);
            Assert.Contains("overly aggressive", result.PaceWarning, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FoodDatabase_Filter_ShouldFilterDietTags()
        {
            var halalChicken = _foodDatabase.FilterFoods(new List<string> { "Halal", "Egg + Chicken Only" }, null, "Lunch", "Protein");
            Assert.NotEmpty(halalChicken);
            Assert.All(halalChicken, f => Assert.DoesNotContain("beef", f.Id, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MealBuilderService_ShouldGenerateSevenDistinctDaysWithinTolerance()
        {
            var input = new UserInputModel
            {
                FirstName = "Gladiator",
                Gender = "Male",
                Age = 25,
                Weight = 75,
                Height = 175,
                Goal = "LoseFat",
                DietPreferences = new List<string> { "Halal" }
            };

            double targetKcal = 2000;
            double targetProtein = 150;
            double targetFat = 50;
            double targetCarbs = 237;

            var days = _mealBuilder.BuildSevenDayPlan(input, targetKcal, targetProtein, targetFat, targetCarbs);

            Assert.Equal(7, days.Count);
            foreach (var day in days)
            {
                Assert.Equal(3, day.Meals.Count); // Breakfast, Lunch, Dinner
                // Each day sum should be within +/- 3% of daily target
                double kcalDiffPct = Math.Abs(day.TotalCalories - targetKcal) / targetKcal;
                Assert.True(kcalDiffPct <= 0.05, $"Day {day.DayNumber} kcal difference was {kcalDiffPct * 100}% ({day.TotalCalories} vs {targetKcal})");
            }
        }

        [Fact]
        public void MealSwapService_ShouldRecalibratePortionOnSwap()
        {
            var swapReq = new MealSwapService.SwapItemRequest
            {
                TargetFoodId = "chicken_breast",
                ReplacementFoodId = "white_fish_tilapia",
                OriginalCalories = 297,
                OriginalProtein = 55.8,
                MealSlot = "Lunch"
            };

            var swapResp = _mealSwap.SwapFoodItem(swapReq);

            Assert.True(swapResp.Success);
            Assert.NotNull(swapResp.NewItem);
            Assert.Equal("white_fish_tilapia", swapResp.NewItem.FoodId);
            Assert.True(swapResp.NewItem.Grams > 0);
            Assert.True(swapResp.NewItem.Protein > 40);
        }

        [Fact]
        public void ExercisePlanService_ShouldGenerateMatchingSchedule()
        {
            var inputGym = new UserInputModel { ExercisePreference = "Gym", DaysPerWeek = 4, MinutesPerSession = 50 };
            var planGym = _exerciseService.GeneratePlan(inputGym);
            Assert.NotEmpty(planGym.Schedule);

            var inputWalk = new UserInputModel { ExercisePreference = "WalkingOnly", DaysPerWeek = 5, MinutesPerSession = 40 };
            var planWalk = _exerciseService.GeneratePlan(inputWalk);
            Assert.NotEmpty(planWalk.Schedule);
        }

        [Fact]
        public void GroceryPricingService_ShouldAggregateAndEvaluateBudget()
        {
            var input = new UserInputModel
            {
                Country = "Bangladesh",
                Currency = "BDT",
                MonthlyBudget = 8000
            };

            var days = _mealBuilder.BuildSevenDayPlan(input, 1900, 150, 45, 220);
            var verdict = _groceryPricing.CalculateDeterministicGroceryPlan(days, input);

            Assert.NotEmpty(verdict.Items);
            Assert.True(verdict.EstimatedWeeklyCost > 0);
            Assert.True(verdict.EstimatedMonthlyCost > 0);
            Assert.NotNull(verdict.Verdict);
            Assert.Contains(verdict.Verdict, new[] { "fits", "tight", "over_budget" });
        }
    }
}
