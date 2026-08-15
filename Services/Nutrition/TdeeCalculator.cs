namespace ValensFit.Services.Nutrition
{
    public class TdeeCalculator
    {
        public (double tdee, double activityMultiplier, double stepBonusKcal) CalculateTdee(
            double bmr, 
            string activityLevel, 
            int dailyStepsTarget = 8000)
        {
            double multiplier = activityLevel?.ToLowerInvariant() switch
            {
                "sedentary" => 1.2,
                "lightlyactive" => 1.375,
                "moderatelyactive" => 1.55,
                "veryactive" => 1.725,
                "extraactive" => 1.9,
                _ => 1.55
            };

            double baseTdee = bmr * multiplier;

            // Explicit walking bonus (avoiding double-counting)
            // Baseline 5,000 steps are considered included in sedentary/daily movement.
            // Extra steps beyond 5,000 add ~35 kcal per 1,000 steps (~150-300 kcal for 8k-12k steps).
            double stepBonus = 0.0;
            if (dailyStepsTarget > 5000)
            {
                int extraSteps = Math.Min(dailyStepsTarget - 5000, 15000);
                stepBonus = Math.Round((extraSteps / 1000.0) * 35.0, 0);
            }

            double totalTdee = Math.Round(baseTdee + stepBonus, 0);
            return (totalTdee, multiplier, stepBonus);
        }
    }
}
