using System.Collections.Generic;

namespace ValensFit.Models
{
    public class MealFoodItem
    {
        public string FoodId { get; set; } = string.Empty;
        public string FoodName { get; set; } = string.Empty;
        public string Category { get; set; } = "Protein"; // Protein, Carb, Vegetable, HealthyFat, Dairy
        public double Grams { get; set; }
        public string DisplayQuantity { get; set; } = string.Empty; // e.g. "180 g", "3 whole eggs (150 g)"
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public string PrepNotes { get; set; } = string.Empty;
    }

    public class MealPlanModel
    {
        public string SlotName { get; set; } = "Breakfast"; // Breakfast, Lunch, Dinner, Snack
        public string Title { get; set; } = "Morning Sustenance";
        public string TimingGuidance { get; set; } = "Within 1-2 hours of waking";
        public double TargetCalories { get; set; }
        public double TargetProtein { get; set; }
        public double ActualCalories { get; set; }
        public double ActualProtein { get; set; }
        public double ActualCarbs { get; set; }
        public double ActualFat { get; set; }
        public List<MealFoodItem> Items { get; set; } = new();
        public string CookingTip { get; set; } = string.Empty;
    }

    public class DayPlanModel
    {
        public int DayNumber { get; set; } = 1;
        public string DayName { get; set; } = "Day I - Primus";
        public string DayTheme { get; set; } = "High Energy & Sustained Focus";
        public List<MealPlanModel> Meals { get; set; } = new();
        public double TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalFat { get; set; }
    }

    public class WeeklyGroceryItem
    {
        public string FoodId { get; set; } = string.Empty;
        public string FoodName { get; set; } = string.Empty;
        public string Category { get; set; } = "Protein";
        public double TotalGrams { get; set; }
        public string DisplayWeeklyQuantity { get; set; } = string.Empty; // e.g. "2.4 kg" or "21 eggs"
        public decimal UnitPrice { get; set; }
        public string PriceUnit { get; set; } = "kg";
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = "BDT";
    }

    public class GroceryBudgetVerdictModel
    {
        public List<WeeklyGroceryItem> Items { get; set; } = new();
        public decimal EstimatedWeeklyCost { get; set; }
        public decimal EstimatedMonthlyCost { get; set; }
        public decimal? UserMonthlyBudget { get; set; }
        public string Currency { get; set; } = "BDT";
        public string Verdict { get; set; } = "fits"; // fits, tight, over_budget
        public string VerdictTitle { get; set; } = "VICTORY: FITS BUDGET";
        public string Notes { get; set; } = string.Empty;
        public List<string> SwapSuggestions { get; set; } = new();
        public string Source { get; set; } = "Rule-Based Grounded Engine"; // Ollama AI Grounded / Deterministic Market Fallback
    }

    public class PlanResultModel
    {
        public string FirstName { get; set; } = "Gladiator";
        public string Gender { get; set; } = "Male";
        public int Age { get; set; } = 25;
        public double HeightCm { get; set; } = 175;
        public double WeightKg { get; set; } = 70;
        public string ActivityLevel { get; set; } = "ModeratelyActive";
        public string Goal { get; set; } = "LoseFat";
        public string GoalDisplayName { get; set; } = "Fat Loss & Athletic Definition";

        public double Bmr { get; set; }
        public double Tdee { get; set; }
        public double TargetCalories { get; set; }
        public double CalorieAdjustment { get; set; }
        public double CalorieAdjustmentPercentage { get; set; }
        public string GoalAdjustmentDescription { get; set; } = string.Empty;
        public string? PaceWarning { get; set; }

        public bool IsUnder18 { get; set; } = false;
        public List<string> SafetyDisclaimers { get; set; } = new();

        public double ProteinGrams { get; set; }
        public double ProteinCalories { get; set; }
        public double ProteinPerKg { get; set; }

        public double FatGrams { get; set; }
        public double FatCalories { get; set; }
        public double FatPercentage { get; set; }

        public double CarbGrams { get; set; }
        public double CarbCalories { get; set; }

        public double WaterIntakeLitres { get; set; } = 3.2;
        public int WaterGlasses { get; set; } = 13;

        public List<DayPlanModel> Days { get; set; } = new();
        public double WeeklyAvgCalories { get; set; }
        public double WeeklyAvgProtein { get; set; }
        public double WeeklyAvgCarbs { get; set; }
        public double WeeklyAvgFat { get; set; }

        public GroceryBudgetVerdictModel GroceryBudget { get; set; } = new();
        public ExercisePlanModel ExercisePlan { get; set; } = new();
    }
}
