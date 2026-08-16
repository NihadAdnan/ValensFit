using System;
using System.Collections.Generic;
using System.Linq;
using ValensFit.Models;

namespace ValensFit.Services.Nutrition
{
    public class DailyCalorieCalculatorService
    {
        private readonly BmrCalculator _bmrCalculator;
        private readonly TdeeCalculator _tdeeCalculator;

        public DailyCalorieCalculatorService(BmrCalculator bmrCalculator, TdeeCalculator tdeeCalculator)
        {
            _bmrCalculator = bmrCalculator;
            _tdeeCalculator = tdeeCalculator;
        }

        public FoodLogResultModel CalculateDailyIntake(DailyFoodLogInput input)
        {
            var result = new FoodLogResultModel
            {
                UserName = string.IsNullOrWhiteSpace(input.Name) ? "Friend" : input.Name.Trim()
            };

            // 1. Calculate Baseline TDEE if biometrics provided
            if (input.Age.HasValue && input.WeightKg.HasValue && input.HeightCm.HasValue)
            {
                double bmr = _bmrCalculator.CalculateBmr(
                    input.BiologicalSex ?? "Male",
                    input.WeightKg.Value,
                    input.HeightCm.Value,
                    input.Age.Value
                );
                var tdeeResult = _tdeeCalculator.CalculateTdee(bmr, input.ActivityLevel ?? "ModeratelyActive", 8000);
                result.EstimatedTdee = tdeeResult.tdee;
            }

            int totalBaseCalories = 0;
            double totalProtein = 0;
            double totalCarbs = 0;
            double totalFat = 0;
            int totalOilCalories = 0;

            // 2. Process each logged meal slot
            foreach (var meal in input.Meals)
            {
                var summary = new MealLogSummary
                {
                    MealName = meal.MealName
                };

                int mealBaseKcal = 0;
                double mealP = 0;
                double mealC = 0;
                double mealF = 0;

                foreach (var item in meal.Items)
                {
                    var calculatedItem = EstimateItemMacros(item);
                    mealBaseKcal += calculatedItem.Calories;
                    mealP += calculatedItem.Protein;
                    mealC += calculatedItem.Carbs;
                    mealF += calculatedItem.Fat;

                    summary.ItemDescriptions.Add($"{calculatedItem.Name} ({calculatedItem.PortionText}) — {calculatedItem.Calories} kcal ({calculatedItem.Protein:F1}g P, {calculatedItem.Carbs:F1}g C, {calculatedItem.Fat:F1}g F)");
                }

                // Calculate oil and cooking method additions for this meal
                int oilKcal = CalculateOilCalories(meal.CookingOilType, meal.OilAmount, meal.CookingMethod);
                double oilFatGrams = oilKcal / 9.0;

                totalOilCalories += oilKcal;
                mealBaseKcal += oilKcal;
                mealF += oilFatGrams;

                summary.MealCalories = mealBaseKcal;
                summary.ProteinGrams = Math.Round(mealP, 1);
                summary.CarbGrams = Math.Round(mealC, 1);
                summary.FatGrams = Math.Round(mealF, 1);
                summary.CookingDetailsText = $"{meal.CookingMethod} style with {meal.OilAmount} {meal.CookingOilType} oil (+{oilKcal} kcal fat)";

                totalBaseCalories += (mealBaseKcal - oilKcal);
                totalProtein += mealP;
                totalCarbs += mealC;
                totalFat += mealF;

                result.MealSummaries.Add(summary);
            }

            // 3. Process Dudh Cha (Milk Tea) & Sugar
            int teaCalories = 0;
            double teaProtein = 0;
            double teaCarbs = 0;
            double teaFat = 0;

            if (input.CupsOfMilkTea > 0)
            {
                int cups = input.CupsOfMilkTea;
                int spoons = Math.Clamp(input.SpoonsOfSugarPerCup, 0, 5);
                int perCupKcal = 45 + (spoons * 20); // 45 kcal milk base + 20 kcal per spoon sugar
                teaCalories = cups * perCupKcal;
                teaProtein = cups * 1.8;
                teaCarbs = cups * (4.0 + (spoons * 5.0));
                teaFat = cups * 2.2;

                totalProtein += teaProtein;
                totalCarbs += teaCarbs;
                totalFat += teaFat;
            }

            // 4. Additional Snacks
            int snackCalories = input.AdditionalSnackCalories;
            if (!string.IsNullOrWhiteSpace(input.SnacksDescription) && snackCalories == 0)
            {
                snackCalories = EstimateSnackTextCalories(input.SnacksDescription);
                totalCarbs += (snackCalories * 0.55) / 4.0;
                totalFat += (snackCalories * 0.35) / 9.0;
                totalProtein += (snackCalories * 0.10) / 4.0;
            }

            // 5. Aggregate Totals
            result.BaseFoodCalories = totalBaseCalories;
            result.CookingOilCalories = totalOilCalories;
            result.BeverageAndSugarCalories = teaCalories;
            result.SnackCalories = snackCalories;

            result.TotalCaloriesConsumed = totalBaseCalories + totalOilCalories + teaCalories + snackCalories;
            result.TotalProteinGrams = Math.Round(totalProtein, 1);
            result.TotalCarbGrams = Math.Round(totalCarbs, 1);
            result.TotalFatGrams = Math.Round(totalFat, 1);

            result.CalorieDifferenceFromTdee = (int)(result.TotalCaloriesConsumed - result.EstimatedTdee);

            // 6. Generate Contextual Verdicts & Actionable Dhaka Insights
            GenerateVerdictsAndInsights(result, input);

            return result;
        }

        private (string Name, string PortionText, int Calories, double Protein, double Carbs, double Fat) EstimateItemMacros(LoggedFoodItem item)
        {
            string key = (item.FoodKey ?? string.Empty).ToLowerInvariant().Trim();
            string customName = string.IsNullOrWhiteSpace(item.CustomFoodName) ? key : item.CustomFoodName;
            double qty = item.Quantity > 0 ? item.Quantity : 1.0;

            // Authentic Dhaka Food Library
            if (key.Contains("rice_white") || key.Contains("bhaat") || key.Contains("rice"))
            {
                // 1 bati (~200g cooked) = 260 kcal, 5g P, 57g C, 0.5g F
                return (customName, $"{qty:0.#} bati", (int)(260 * qty), 5.0 * qty, 57.0 * qty, 0.5 * qty);
            }
            if (key.Contains("laal_chal") || key.Contains("brown_rice"))
            {
                return (customName, $"{qty:0.#} bati", (int)(245 * qty), 5.5 * qty, 52.0 * qty, 1.2 * qty);
            }
            if (key.Contains("roti") || key.Contains("chapati") || key.Contains("hand_roti"))
            {
                // 1 dry atta roti (~35g) = 70 kcal, 2.8g P, 14g C, 0.4g F
                return (customName, $"{qty:0.#} pcs", (int)(70 * qty), 2.8 * qty, 14.0 * qty, 0.4 * qty);
            }
            if (key.Contains("porota") || key.Contains("paratha"))
            {
                // 1 oil paratha = 220 kcal, 4g P, 28g C, 11g F
                return (customName, $"{qty:0.#} pcs", (int)(220 * qty), 4.0 * qty, 28.0 * qty, 11.0 * qty);
            }
            if (key.Contains("dal_masoor") || key.Contains("dal") || key.Contains("daal"))
            {
                // 1 bati/cup dal = 130 kcal, 8g P, 20g C, 2g F
                return (customName, $"{qty:0.#} bati", (int)(130 * qty), 8.0 * qty, 20.0 * qty, 2.0 * qty);
            }
            if (key.Contains("chicken") || key.Contains("murgi"))
            {
                // 1 medium curry cut piece (~80g) = 140 kcal, 22g P, 1g C, 5g F
                return (customName, $"{qty:0.#} pcs", (int)(140 * qty), 22.0 * qty, 1.0 * qty, 5.0 * qty);
            }
            if (key.Contains("beef") || key.Contains("goru") || key.Contains("meat"))
            {
                // 1 piece beef (~70g) = 175 kcal, 18g P, 0g C, 11g F
                return (customName, $"{qty:0.#} pcs", (int)(175 * qty), 18.0 * qty, 0.0 * qty, 11.0 * qty);
            }
            if (key.Contains("fish") || key.Contains("maach") || key.Contains("rui") || key.Contains("katla") || key.Contains("tilapia"))
            {
                // 1 piece fish (~80g) = 115 kcal, 17g P, 1g C, 4.5g F
                return (customName, $"{qty:0.#} pcs", (int)(115 * qty), 17.0 * qty, 1.0 * qty, 4.5 * qty);
            }
            if (key.Contains("egg_boiled") || key.Contains("dim_shiddho"))
            {
                // 1 boiled egg = 74 kcal, 6.3g P, 0.5g C, 5g F
                return (customName, $"{qty:0.#} eggs", (int)(74 * qty), 6.3 * qty, 0.5 * qty, 5.0 * qty);
            }
            if (key.Contains("egg_fried") || key.Contains("dim_mamla") || key.Contains("dim_poach"))
            {
                // 1 fried egg = 115 kcal, 6.3g P, 0.5g C, 9.5g F
                return (customName, $"{qty:0.#} eggs", (int)(115 * qty), 6.3 * qty, 0.5 * qty, 9.5 * qty);
            }
            if (key.Contains("aloo_bhorta") || key.Contains("alu_bhorta"))
            {
                return (customName, $"{qty:0.#} serving", (int)(120 * qty), 2.0 * qty, 22.0 * qty, 3.0 * qty);
            }
            if (key.Contains("begun_bhorta") || key.Contains("bhorta"))
            {
                return (customName, $"{qty:0.#} serving", (int)(85 * qty), 2.0 * qty, 10.0 * qty, 4.5 * qty);
            }
            if (key.Contains("shak") || key.Contains("shaak") || key.Contains("palong") || key.Contains("laal_shak"))
            {
                return (customName, $"{qty:0.#} serving", (int)(65 * qty), 3.0 * qty, 5.0 * qty, 4.0 * qty);
            }
            if (key.Contains("sabji") || key.Contains("vegetable") || key.Contains("torkari"))
            {
                return (customName, $"{qty:0.#} bati", (int)(90 * qty), 2.5 * qty, 13.0 * qty, 3.5 * qty);
            }
            if (key.Contains("tok_doi") || key.Contains("yogurt") || key.Contains("curd"))
            {
                return (customName, $"{qty:0.#} cup", (int)(90 * qty), 6.0 * qty, 8.0 * qty, 4.0 * qty);
            }
            if (key.Contains("banana") || key.Contains("kola"))
            {
                return (customName, $"{qty:0.#} pcs", (int)(105 * qty), 1.3 * qty, 27.0 * qty, 0.3 * qty);
            }
            if (key.Contains("singara") || key.Contains("shingara"))
            {
                return (customName, $"{qty:0.#} pcs", (int)(140 * qty), 2.5 * qty, 18.0 * qty, 7.0 * qty);
            }
            if (key.Contains("samucha") || key.Contains("samosa"))
            {
                return (customName, $"{qty:0.#} pcs", (int)(120 * qty), 3.0 * qty, 14.0 * qty, 6.0 * qty);
            }
            if (key.Contains("peyaju") || key.Contains("pakora"))
            {
                return (customName, $"{qty:0.#} pcs", (int)(55 * qty), 1.5 * qty, 6.0 * qty, 3.0 * qty);
            }

            // Fallback for custom text
            int fallbackKcal = (int)(150 * qty);
            return (customName, $"{qty:0.#} serving", fallbackKcal, 8.0 * qty, 18.0 * qty, 5.0 * qty);
        }

        private int CalculateOilCalories(string oilType, string amount, string method)
        {
            if (oilType.Equals("None", StringComparison.OrdinalIgnoreCase) || method.Equals("Steamed/Boiled", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            int baseOilKcal = amount.ToLowerInvariant() switch
            {
                "none" => 0,
                "low" => 45,       // ~1 tsp (5ml)
                "medium" => 135,   // ~1 tbsp (15ml)
                "high" => 270,     // ~2+ tbsp (30ml)
                _ => 90
            };

            // Method adjustment (Bhuna absorbed more oil into the thick masala gravy)
            int methodMultiplierKcal = method.ToLowerInvariant() switch
            {
                "bhuna" => 50,
                "fried" => 75,
                "bhorta" => 35, // raw mustard oil added
                "jhol" => 20,
                _ => 0
            };

            return baseOilKcal + methodMultiplierKcal;
        }

        private int EstimateSnackTextCalories(string text)
        {
            int score = 150;
            string lower = text.ToLowerInvariant();
            if (lower.Contains("singara") || lower.Contains("shingara")) score += 140;
            if (lower.Contains("samucha") || lower.Contains("samosa")) score += 120;
            if (lower.Contains("chanachur") || lower.Contains("fuchka") || lower.Contains("chotpoti")) score += 250;
            if (lower.Contains("biscuit") || lower.Contains("toast") || lower.Contains("cake")) score += 120;
            if (lower.Contains("sweet") || lower.Contains("mishti") || lower.Contains("rosogolla")) score += 220;
            if (lower.Contains("fruit") || lower.Contains("apple") || lower.Contains("banana")) score += 100;
            return score;
        }

        private void GenerateVerdictsAndInsights(FoodLogResultModel result, DailyFoodLogInput input)
        {
            // Caloric Verdict
            int diff = result.CalorieDifferenceFromTdee;
            if (diff > 350)
            {
                result.EnergyBalanceVerdict = $"Surplus of +{diff} kcal above your estimated daily maintenance ({result.EstimatedTdee:N0} kcal). (Weight Gain Range)";
            }
            else if (diff < -350)
            {
                result.EnergyBalanceVerdict = $"Deficit of {Math.Abs(diff)} kcal below your daily maintenance ({result.EstimatedTdee:N0} kcal). (Fat Loss Range)";
            }
            else
            {
                result.EnergyBalanceVerdict = $"Within maintenance equilibrium range (±{Math.Abs(diff)} kcal of {result.EstimatedTdee:N0} kcal).";
            }

            // Protein Adequacy
            double targetProtein = (input.WeightKg ?? 70.0) * 1.6;
            if (result.TotalProteinGrams >= targetProtein)
            {
                result.ProteinAdequacyVerdict = $"Optimal ({result.TotalProteinGrams}g reached vs. {targetProtein:F0}g target). Excellent muscle protection.";
            }
            else
            {
                double gap = Math.Round(targetProtein - result.TotalProteinGrams, 1);
                result.ProteinAdequacyVerdict = $"Sub-optimal ({result.TotalProteinGrams}g consumed). Consider adding {gap}g more protein (e.g. 2 boiled eggs or 1 extra piece of chicken/fish).";
            }

            // Actionable Insights for Dhaka Cooking
            if (result.CookingOilCalories > 300)
            {
                result.ActionableInsights.Add($"⚠️ Cooking oil contributed **{result.CookingOilCalories} kcal** ({result.CookingOilCalories / 9}g fat) today. Shifting from heavy Bhuna gravy to Jhol or Steamed dishes can instantly save ~200+ kcal daily.");
            }

            if (result.BeverageAndSugarCalories > 150)
            {
                result.ActionableInsights.Add($"☕ Milk tea with sugar contributed **{result.BeverageAndSugarCalories} kcal**. Reducing 1 spoon of sugar per cup saves ~60 kcal/day (~1.8k kcal/month).");
            }

            if (result.TotalCarbGrams > 280)
            {
                result.ActionableInsights.Add($"🍚 High carbohydrate intake ({result.TotalCarbGrams}g, largely from rice/roti). Substituting half a bati of rice with sabji or shak increases fullness with fewer calories.");
            }

            if (result.ActionableInsights.Count == 0)
            {
                result.ActionableInsights.Add("✅ Well-balanced daily intake for authentic Dhaka home cooking! Great portion and oil discipline.");
            }
        }
    }
}
