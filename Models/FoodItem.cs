using System.Collections.Generic;

namespace ValensFit.Models
{
    public class FoodItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Protein"; // Protein, Carb, Vegetable, HealthyFat, Dairy
        public string ServingUnit { get; set; } = "g"; // "g", "piece", "tsp", "tbsp", "scoop"
        public double GramsPerServingUnit { get; set; } = 1.0;
        public double DefaultGramPortion { get; set; } = 100.0;
        
        public double CaloriesPer100g { get; set; }
        public double ProteinPer100g { get; set; }
        public double CarbsPer100g { get; set; }
        public double FatPer100g { get; set; }
        public double FiberPer100g { get; set; }

        public List<string> AllowedMealSlots { get; set; } = new(); // "Breakfast", "Lunch", "Dinner", "Snack"
        public List<string> DietTags { get; set; } = new(); // "Halal", "Vegetarian", "Vegan", "EggChickenOnly", "DairyFree", "GlutenFree", "NutFree", "NoBeef", "NoPork", "NoFish"
        public List<string> Regions { get; set; } = new(); // "Global", "SouthAsia", "NorthAmerica", "Europe"

        public decimal DefaultPricePer100g { get; set; } = 0m;
        public string PriceUnit { get; set; } = "100g";
        public string PrepNotes { get; set; } = string.Empty;
    }
}
