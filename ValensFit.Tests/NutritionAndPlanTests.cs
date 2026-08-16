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
        private readonly DailyCalorieCalculatorService _calorieCalculator;

        public NutritionAndPlanTests()
        {
            var env = new MockWebHostEnvironment();
            _foodDatabase = new FoodDatabase(env, NullLogger<FoodDatabase>.Instance);
            _mealBuilder = new MealBuilderService(_foodDatabase, NullLogger<MealBuilderService>.Instance);
            _mealSwap = new MealSwapService(_foodDatabase);
            _exerciseService = new ExercisePlanService(env, NullLogger<ExercisePlanService>.Instance);
            _groceryPricing = new GroceryPricingService(_foodDatabase, NullLogger<GroceryPricingService>.Instance);
            _calorieCalculator = new DailyCalorieCalculatorService(_bmrCalculator, _tdeeCalculator);
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
        public void MacroCalculator_MuscleRetention_ShouldAllocateHighProtein()
        {
            var input = new UserInputModel
            {
                FirstName = "Rafi",
                Gender = "Male",
                Age = 24,
                Weight = 80,
                Height = 175,
                Goal = "LoseFat",
                MaximizeMuscleRetention = true
            };

            double bmr = 1774;
            double tdee = 2750;

            var result = _macroCalculator.CalculateMacros(input, bmr, tdee);

            Assert.True(result.TargetCalories < tdee);
            Assert.True(result.TargetCalories >= 1500, "Male safety floor must be respected");
            Assert.Equal(176, result.ProteinGrams); // 80kg * 2.2g/kg = 176g
            Assert.True(result.FatGrams > 0);
            Assert.True(result.CarbGrams > 0);
            Assert.True(result.WaterLitres >= 3.0);
        }

        [Fact]
        public void FoodDatabase_BangladeshRegion_ShouldFilterRealisticStaples()
        {
            var bangladeshFoods = _foodDatabase.FilterFoods(new List<string> { "Halal" }, null, "Breakfast", null, "Bangladesh");
            Assert.NotEmpty(bangladeshFoods);
            Assert.Contains(bangladeshFoods, f => f.Id == "egg_whole" || f.Id == "whole_wheat_roti");
            Assert.DoesNotContain(bangladeshFoods, f => f.Id == "rolled_oats");
        }

        [Fact]
        public void ExercisePlanService_METEquations_ShouldCalculateAccurateCalorieBurn()
        {
            var input = new UserInputModel
            {
                ExercisePreference = "Gym",
                Weight = 75,
                MinutesPerSession = 60,
                DaysPerWeek = 4,
                DailyStepsTarget = 10000
            };

            var plan = _exerciseService.GeneratePlan(input);

            // 75kg * 5.8 MET * 1 hr = ~435 kcal per session
            Assert.True(plan.AvgCaloriesBurnedPerSession >= 400);
            Assert.True(plan.WeeklyTotalCalorieBurn > plan.AvgCaloriesBurnedPerSession * 4);
            Assert.True(plan.DailyStepsCalorieBurn > 0);
        }

        [Fact]
        public void DailyCalorieCalculatorService_ShouldAccountForOilAndTeaSugar()
        {
            var logInput = new DailyFoodLogInput
            {
                Name = "Tanvir",
                WeightKg = 72,
                HeightCm = 174,
                Age = 25,
                BiologicalSex = "Male",
                CupsOfMilkTea = 2,
                SpoonsOfSugarPerCup = 2,
                Meals = new List<LoggedMealSlot>
                {
                    new LoggedMealSlot
                    {
                        MealName = "Breakfast",
                        CookingOilType = "Mustard",
                        OilAmount = "Low",
                        CookingMethod = "Jhol",
                        Items = new List<LoggedFoodItem>
                        {
                            new LoggedFoodItem { FoodKey = "roti", Quantity = 2, PortionUnit = "pcs" },
                            new LoggedFoodItem { FoodKey = "egg_boiled", Quantity = 2, PortionUnit = "eggs" }
                        }
                    },
                    new LoggedMealSlot
                    {
                        MealName = "Lunch",
                        CookingOilType = "Mustard",
                        OilAmount = "High",
                        CookingMethod = "Bhuna",
                        Items = new List<LoggedFoodItem>
                        {
                            new LoggedFoodItem { FoodKey = "rice_white", Quantity = 1, PortionUnit = "bati" },
                            new LoggedFoodItem { FoodKey = "chicken", Quantity = 1, PortionUnit = "pcs" },
                            new LoggedFoodItem { FoodKey = "dal_masoor", Quantity = 1, PortionUnit = "bati" }
                        }
                    }
                }
            };

            var result = _calorieCalculator.CalculateDailyIntake(logInput);

            Assert.True(result.TotalCaloriesConsumed > 1000);
            Assert.True(result.CookingOilCalories > 200, "High Bhuna + Jhol oil must add significant fat calories");
            Assert.True(result.BeverageAndSugarCalories >= 160, "2 cups of milk tea with 2 spoons sugar must add ~170 kcal");
            Assert.True(result.TotalProteinGrams > 45);
            Assert.NotEmpty(result.ActionableInsights);
        }

        [Fact]
        public void GroceryPricingService_ShouldIncludeWeeklyAndMonthlyCost()
        {
            var input = new UserInputModel
            {
                Country = "Bangladesh",
                Currency = "BDT",
                MonthlyBudget = 7000
            };

            var days = _mealBuilder.BuildSevenDayPlan(input, 1850, 160, 45, 200);
            var verdict = _groceryPricing.CalculateDeterministicGroceryPlan(days, input);

            Assert.NotEmpty(verdict.Items);
            Assert.True(verdict.EstimatedWeeklyCost > 0);
            Assert.True(verdict.EstimatedMonthlyCost > 0);
            Assert.All(verdict.Items, item => Assert.True(item.EstimatedMonthlyCost > 0));
        }
    }
}
