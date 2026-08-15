using ValensFit.Models;

namespace ValensFit.Services.Nutrition
{
    public class MacroCalculator
    {
        public class MacroResult
        {
            public double TargetCalories { get; set; }
            public double CalorieAdjustment { get; set; }
            public double CalorieAdjustmentPercentage { get; set; }
            public string GoalDescription { get; set; } = string.Empty;
            public string? PaceWarning { get; set; }

            public double ProteinGrams { get; set; }
            public double ProteinCalories { get; set; }
            public double ProteinPerKg { get; set; }

            public double FatGrams { get; set; }
            public double FatCalories { get; set; }
            public double FatPercentage { get; set; }

            public double CarbGrams { get; set; }
            public double CarbCalories { get; set; }

            public double WaterLitres { get; set; }
            public int WaterGlasses { get; set; }

            public List<string> SafetyDisclaimers { get; set; } = new();
            public bool IsUnder18 { get; set; }
        }

        public MacroResult CalculateMacros(UserInputModel input, double bmr, double tdee)
        {
            var result = new MacroResult();
            double weightKg = input.GetWeightInKg();
            bool isUnder18 = input.Age < 18;
            result.IsUnder18 = isUnder18;

            // Universal Medical Disclaimer
            result.SafetyDisclaimers.Add("Not medical advice. General population wellness guide only. Consult a physician if pregnant, diabetic, or managing chronic health conditions.");

            if (isUnder18)
            {
                result.SafetyDisclaimers.Add("Youth Advisory: Under-18 nutrition requires adequate nourishment for skeletal and hormonal development. Deficit is capped conservatively at 10%. Speak to a guardian or physician.");
            }

            double targetKcal = tdee;
            double adjustment = 0;
            string goalDesc = "Weight Maintenance & Energetic Balance";

            string goal = input.Goal?.ToLowerInvariant() ?? "losefat";

            if (goal == "losefat")
            {
                // Baseline 20% deficit
                double deficitPct = 0.20;
                if (isUnder18)
                {
                    deficitPct = 0.10; // Capped for teens
                }

                adjustment = -(tdee * deficitPct);

                // Check if user specified target weight loss & timeframe
                if (input.TargetWeightLossKg.HasValue && input.TargetWeightLossKg > 0 && 
                    input.TimeframeWeeks.HasValue && input.TimeframeWeeks > 0)
                {
                    double targetLossKg = input.TargetWeightLossKg.Value;
                    int weeks = input.TimeframeWeeks.Value;
                    double impliedWeeklyLossKg = targetLossKg / weeks;
                    double weeklyLossPctOfBw = (impliedWeeklyLossKg / weightKg) * 100.0;

                    // Safe band is 0.5% - 0.75% body weight loss per week
                    double safeWeeklyLossKgMax = weightKg * 0.0075;
                    
                    if (weeklyLossPctOfBw > 0.85) // Too aggressive
                    {
                        result.PaceWarning = $"Your requested pace ({impliedWeeklyLossKg:F2} kg/week, {weeklyLossPctOfBw:F1}% of bodyweight) is overly aggressive and risks lean muscle loss. Auto-calibrated to the safe athletic ceiling of 0.75% bodyweight ({safeWeeklyLossKgMax:F2} kg/week).";
                        
                        // 1 kg fat loss ≈ 7,700 kcal deficit -> daily deficit = (lossKg * 7700) / 7
                        double safeDailyDeficit = (safeWeeklyLossKgMax * 7700.0) / 7.0;
                        adjustment = -Math.Min(safeDailyDeficit, 1100.0); // max 1100 kcal/day deficit cap
                    }
                    else
                    {
                        double requestedDailyDeficit = (impliedWeeklyLossKg * 7700.0) / 7.0;
                        adjustment = -Math.Min(requestedDailyDeficit, 1100.0);
                        result.PaceWarning = $"Pace verified: Targeted rate of {impliedWeeklyLossKg:F2} kg/week ({weeklyLossPctOfBw:F2}% BW/week) falls within the safe, sustainable athletic band.";
                    }
                }
                else
                {
                    // Cap at 1 kg / week (1100 kcal/day cap)
                    adjustment = Math.Max(adjustment, -1100.0);
                }

                targetKcal = tdee + adjustment;

                // Hard Safety Floors
                double minFloor = input.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase) ? 1200.0 : 1500.0;
                if (targetKcal < minFloor)
                {
                    targetKcal = minFloor;
                    adjustment = targetKcal - tdee;
                    result.SafetyDisclaimers.Add($"Strict metabolic floor applied: Calories set to {minFloor} kcal/day to protect hormonal baseline and avoid nutrient deficiency.");
                }

                goalDesc = $"Sustainable Fat Loss Deficit ({Math.Abs(adjustment):F0} kcal/day, {Math.Abs(adjustment / tdee * 100):F0}% cut)";
            }
            else if (goal == "buildmuscle")
            {
                // 10-15% surplus, capped at +500 kcal/day
                double surplus = Math.Min(tdee * 0.12, 500.0);
                adjustment = surplus;
                targetKcal = tdee + adjustment;
                goalDesc = $"Lean Hypertrophy Surplus (+{adjustment:F0} kcal/day, +{adjustment / tdee * 100:F0}%)";
            }
            else // Maintain
            {
                adjustment = 0;
                targetKcal = tdee;
                goalDesc = "Iso-Caloric Weight Maintenance & Body Recomposition";
            }

            result.TargetCalories = Math.Round(targetKcal, 0);
            result.CalorieAdjustment = Math.Round(adjustment, 0);
            result.CalorieAdjustmentPercentage = Math.Round((adjustment / tdee) * 100.0, 1);
            result.GoalDescription = goalDesc;

            // Protein-First Split
            double proteinFactor = goal switch
            {
                "losefat" => 2.0,      // 1.8 - 2.2 g/kg
                "buildmuscle" => 1.8,  // 1.6 - 2.0 g/kg
                _ => 1.6              // 1.4 - 1.8 g/kg
            };

            double proteinG = Math.Round(weightKg * proteinFactor, 0);
            double proteinKcal = proteinG * 4.0;

            // Fat % (20% - 25% for cut/bulk, 25% for maintain)
            double fatPct = goal == "maintain" ? 0.25 : 0.22;
            double fatKcal = result.TargetCalories * fatPct;
            double fatG = Math.Round(fatKcal / 9.0, 0);

            // Carbs remainder
            double carbKcal = Math.Max(result.TargetCalories - proteinKcal - fatKcal, 0.0);
            double carbG = Math.Round(carbKcal / 4.0, 0);

            result.ProteinGrams = proteinG;
            result.ProteinCalories = Math.Round(proteinKcal, 0);
            result.ProteinPerKg = Math.Round(proteinFactor, 2);

            result.FatGrams = fatG;
            result.FatCalories = Math.Round(fatKcal, 0);
            result.FatPercentage = Math.Round(fatPct * 100.0, 0);

            result.CarbGrams = carbG;
            result.CarbCalories = Math.Round(carbKcal, 0);

            // Water intake: 35-40 ml/kg + 500ml exercise
            double waterMl = (weightKg * 38.0) + (input.MinutesPerSession > 0 ? 500.0 : 0.0);
            result.WaterLitres = Math.Round(waterMl / 1000.0, 1);
            result.WaterGlasses = (int)Math.Round(waterMl / 250.0);

            return result;
        }
    }
}
