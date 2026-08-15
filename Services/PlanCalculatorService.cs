using ValensFit.Models;
using ValensFit.Services.Exercise;
using ValensFit.Services.Grocery;
using ValensFit.Services.Nutrition;

namespace ValensFit.Services
{
    public class PlanCalculatorService
    {
        private readonly BmrCalculator _bmrCalculator;
        private readonly TdeeCalculator _tdeeCalculator;
        private readonly MacroCalculator _macroCalculator;
        private readonly MealBuilderService _mealBuilder;
        private readonly ExercisePlanService _exercisePlanService;
        private readonly GroceryPricingService _groceryPricingService;
        private readonly OllamaClient _ollamaClient;
        private readonly ILogger<PlanCalculatorService> _logger;

        public PlanCalculatorService(
            BmrCalculator bmrCalculator,
            TdeeCalculator tdeeCalculator,
            MacroCalculator macroCalculator,
            MealBuilderService mealBuilder,
            ExercisePlanService exercisePlanService,
            GroceryPricingService groceryPricingService,
            OllamaClient ollamaClient,
            ILogger<PlanCalculatorService> logger)
        {
            _bmrCalculator = bmrCalculator;
            _tdeeCalculator = tdeeCalculator;
            _macroCalculator = macroCalculator;
            _mealBuilder = mealBuilder;
            _exercisePlanService = exercisePlanService;
            _groceryPricingService = groceryPricingService;
            _ollamaClient = ollamaClient;
            _logger = logger;
        }

        public async Task<PlanResultModel> GenerateComprehensivePlanAsync(UserInputModel input, CancellationToken ct = default)
        {
            var plan = new PlanResultModel
            {
                FirstName = string.IsNullOrWhiteSpace(input.FirstName) ? "Gladiator" : input.FirstName.Trim(),
                Gender = input.Gender,
                Age = input.Age,
                HeightCm = input.GetHeightInCm(),
                WeightKg = input.GetWeightInKg(),
                ActivityLevel = input.ActivityLevel,
                Goal = input.Goal,
                GoalDisplayName = input.Goal?.ToLowerInvariant() switch
                {
                    "losefat" => "Fat Loss & Athletic Definition",
                    "buildmuscle" => "Hypertrophy & Muscle Fortification",
                    _ => "Body Recomposition & Lean Maintenance"
                }
            };

            // 1. Calculate BMR
            plan.Bmr = _bmrCalculator.CalculateBmr(input.Gender, plan.WeightKg, plan.HeightCm, plan.Age);

            // 2. Calculate TDEE
            var (tdee, activityMultiplier, stepBonus) = _tdeeCalculator.CalculateTdee(plan.Bmr, input.ActivityLevel, input.DailyStepsTarget);
            plan.Tdee = tdee;

            // 3. Calculate Macros & Target Calories
            var macroResult = _macroCalculator.CalculateMacros(input, plan.Bmr, plan.Tdee);
            plan.TargetCalories = macroResult.TargetCalories;
            plan.CalorieAdjustment = macroResult.CalorieAdjustment;
            plan.CalorieAdjustmentPercentage = macroResult.CalorieAdjustmentPercentage;
            plan.GoalAdjustmentDescription = macroResult.GoalDescription;
            plan.PaceWarning = macroResult.PaceWarning;
            plan.IsUnder18 = macroResult.IsUnder18;
            plan.SafetyDisclaimers = macroResult.SafetyDisclaimers;

            plan.ProteinGrams = macroResult.ProteinGrams;
            plan.ProteinCalories = macroResult.ProteinCalories;
            plan.ProteinPerKg = macroResult.ProteinPerKg;

            plan.FatGrams = macroResult.FatGrams;
            plan.FatCalories = macroResult.FatCalories;
            plan.FatPercentage = macroResult.FatPercentage;

            plan.CarbGrams = macroResult.CarbGrams;
            plan.CarbCalories = macroResult.CarbCalories;

            plan.WaterIntakeLitres = macroResult.WaterLitres;
            plan.WaterGlasses = macroResult.WaterGlasses;

            // 4. Build 7-Day Meal Plan
            plan.Days = _mealBuilder.BuildSevenDayPlan(input, plan.TargetCalories, plan.ProteinGrams, plan.FatGrams, plan.CarbGrams);

            plan.WeeklyAvgCalories = Math.Round(plan.Days.Average(d => d.TotalCalories), 0);
            plan.WeeklyAvgProtein = Math.Round(plan.Days.Average(d => d.TotalProtein), 1);
            plan.WeeklyAvgCarbs = Math.Round(plan.Days.Average(d => d.TotalCarbs), 1);
            plan.WeeklyAvgFat = Math.Round(plan.Days.Average(d => d.TotalFat), 1);

            // 5. Generate Exercise Plan
            plan.ExercisePlan = _exercisePlanService.GeneratePlan(input);

            // 6. Calculate Deterministic Grocery Plan & Grounded Pricing
            var deterministicGrocery = _groceryPricingService.CalculateDeterministicGroceryPlan(plan.Days, input);
            plan.GroceryBudget = deterministicGrocery;

            // 7. Ollama AI Budget Grounding (Progressive Enhancement)
            try
            {
                var aiVerdict = await _ollamaClient.EvaluateBudgetWithAiAsync(input, deterministicGrocery, ct);
                if (aiVerdict != null)
                {
                    plan.GroceryBudget.Source = "Ollama AI Grounded (Real Market Index)";
                    plan.GroceryBudget.Verdict = aiVerdict.Verdict?.ToLowerInvariant() ?? deterministicGrocery.Verdict;
                    plan.GroceryBudget.VerdictTitle = plan.GroceryBudget.Verdict switch
                    {
                        "fits" => "VICTORY: FITS COMFORTABLY",
                        "tight" => "ALERT: TIGHT BUDGET FIT",
                        "over_budget" => "EXCEEDS BUDGET ALLOCATION",
                        _ => deterministicGrocery.VerdictTitle
                    };

                    if (!string.IsNullOrWhiteSpace(aiVerdict.Notes))
                    {
                        plan.GroceryBudget.Notes = aiVerdict.Notes;
                    }

                    if (aiVerdict.SwapSuggestions != null && aiVerdict.SwapSuggestions.Count > 0)
                    {
                        plan.GroceryBudget.SwapSuggestions = aiVerdict.SwapSuggestions;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI budget enhancement step skipped. Deterministic budget verdict retained.");
            }

            return plan;
        }
    }
}
