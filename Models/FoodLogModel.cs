using System;
using System.Collections.Generic;

namespace ValensFit.Models
{
    /// <summary>
    /// Captures a user's food intake for a single day in Dhaka, Bangladesh,
    /// including specific details on cooking oil, preparation style, and portions.
    /// </summary>
    public class DailyFoodLogInput
    {
        // Optional biometrics for TDEE deficit/surplus comparison
        public string? Name { get; set; }
        public int? Age { get; set; } = 25;
        public string? BiologicalSex { get; set; } = "Male";
        public double? WeightKg { get; set; } = 70.0;
        public double? HeightCm { get; set; } = 172.0;
        public string? ActivityLevel { get; set; } = "ModeratelyActive";
        public string? Goal { get; set; } = "FatLoss"; // FatLoss, Maintenance, MuscleGain

        // Meal logging entries
        public List<LoggedMealSlot> Meals { get; set; } = new();

        // General daily additions
        public int CupsOfMilkTea { get; set; } = 0;
        public int SpoonsOfSugarPerCup { get; set; } = 1;
        public string? SnacksDescription { get; set; }
        public int AdditionalSnackCalories { get; set; } = 0;
    }

    public class LoggedMealSlot
    {
        public string MealName { get; set; } = "Lunch"; // Breakfast, Lunch, Dinner, Evening Snacks
        public List<LoggedFoodItem> Items { get; set; } = new();

        // Cooking & preparation details for this meal
        public string CookingOilType { get; set; } = "Mustard"; // Mustard, Soybean, Ghee, Olive, None
        public string OilAmount { get; set; } = "Medium"; // None, Low (1 tsp ~5ml), Medium (1 tbsp ~15ml), High (2+ tbsp ~30ml / Restaurant)
        public string CookingMethod { get; set; } = "Jhol"; // Steamed/Boiled, Jhol (Light Curry), Bhuna (Heavy Gravy), Bhorta (Mashed with Raw Oil), Fried
    }

    public class LoggedFoodItem
    {
        public string FoodKey { get; set; } = string.Empty; // e.g. rice_white, chicken_curry, roti, dal_masoor, egg_boiled
        public string CustomFoodName { get; set; } = string.Empty;
        public double Quantity { get; set; } = 1.0; // e.g. 1.5 bati, 3 rotis, 2 pieces
        public string PortionUnit { get; set; } = "bati"; // bati, piece, cup, serving, plate
        public int EstimatedCalories { get; set; } = 0;
        public double ProteinGrams { get; set; } = 0;
        public double CarbGrams { get; set; } = 0;
        public double FatGrams { get; set; } = 0;
    }

    /// <summary>
    /// Calculated output of daily calorie & macro intake for Dhaka food.
    /// </summary>
    public class FoodLogResultModel
    {
        public string UserName { get; set; } = "User";
        public double EstimatedTdee { get; set; } = 2200;
        public int TotalCaloriesConsumed { get; set; }
        public double TotalProteinGrams { get; set; }
        public double TotalCarbGrams { get; set; }
        public double TotalFatGrams { get; set; }

        // Breakdown analysis
        public int BaseFoodCalories { get; set; }
        public int CookingOilCalories { get; set; }
        public int BeverageAndSugarCalories { get; set; }
        public int SnackCalories { get; set; }

        // Comparison & Goal Impact
        public int CalorieDifferenceFromTdee { get; set; }
        public string EnergyBalanceVerdict { get; set; } = string.Empty;
        public string ProteinAdequacyVerdict { get; set; } = string.Empty;

        // Meal breakdown cards
        public List<MealLogSummary> MealSummaries { get; set; } = new();

        // Practical tips for Dhaka eating
        public List<string> ActionableInsights { get; set; } = new();
    }

    public class MealLogSummary
    {
        public string MealName { get; set; } = string.Empty;
        public int MealCalories { get; set; }
        public double ProteinGrams { get; set; }
        public double CarbGrams { get; set; }
        public double FatGrams { get; set; }
        public string CookingDetailsText { get; set; } = string.Empty;
        public List<string> ItemDescriptions { get; set; } = new();
    }
}
