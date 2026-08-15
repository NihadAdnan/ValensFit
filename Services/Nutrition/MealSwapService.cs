using ValensFit.Models;

namespace ValensFit.Services.Nutrition
{
    public class MealSwapService
    {
        private readonly FoodDatabase _foodDb;

        public MealSwapService(FoodDatabase foodDb)
        {
            _foodDb = foodDb;
        }

        public class SwapItemRequest
        {
            public string TargetFoodId { get; set; } = string.Empty;
            public string ReplacementFoodId { get; set; } = string.Empty;
            public double OriginalCalories { get; set; }
            public double OriginalProtein { get; set; }
            public string MealSlot { get; set; } = "Lunch";
        }

        public class SwapItemResponse
        {
            public bool Success { get; set; }
            public MealFoodItem? NewItem { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public SwapItemResponse SwapFoodItem(SwapItemRequest request)
        {
            var replacementFood = _foodDb.GetFoodById(request.ReplacementFoodId);
            if (replacementFood == null)
            {
                return new SwapItemResponse { Success = false, Message = "Replacement food not found in database." };
            }

            // Calculate portion size of replacement food to match original calories and protein
            double targetGrams = 100.0;

            if (replacementFood.Category == "Protein" && replacementFood.ProteinPer100g > 0)
            {
                // Match protein primarily
                targetGrams = (request.OriginalProtein / replacementFood.ProteinPer100g) * 100.0;
            }
            else if (replacementFood.CaloriesPer100g > 0)
            {
                // Match calories primarily
                targetGrams = (request.OriginalCalories / replacementFood.CaloriesPer100g) * 100.0;
            }

            // Practical rounding
            if (replacementFood.ServingUnit == "piece" && replacementFood.GramsPerServingUnit > 1)
            {
                double pieces = Math.Max(1, Math.Round(targetGrams / replacementFood.GramsPerServingUnit));
                targetGrams = pieces * replacementFood.GramsPerServingUnit;
            }
            else
            {
                targetGrams = Math.Round(targetGrams / 5.0) * 5.0;
            }

            double scale = targetGrams / 100.0;
            string displayQty = FormatDisplay(replacementFood, targetGrams);

            var newItem = new MealFoodItem
            {
                FoodId = replacementFood.Id,
                FoodName = replacementFood.Name,
                Category = replacementFood.Category,
                Grams = Math.Round(targetGrams, 0),
                DisplayQuantity = displayQty,
                Calories = Math.Round(replacementFood.CaloriesPer100g * scale, 0),
                Protein = Math.Round(replacementFood.ProteinPer100g * scale, 1),
                Carbs = Math.Round(replacementFood.CarbsPer100g * scale, 1),
                Fat = Math.Round(replacementFood.FatPer100g * scale, 1),
                PrepNotes = replacementFood.PrepNotes
            };

            return new SwapItemResponse
            {
                Success = true,
                NewItem = newItem,
                Message = $"Successfully calibrated portion for {replacementFood.Name}."
            };
        }

        private string FormatDisplay(FoodItem food, double grams)
        {
            if (food.ServingUnit == "piece" && food.GramsPerServingUnit > 1)
            {
                int pieces = (int)Math.Round(grams / food.GramsPerServingUnit);
                return $"{pieces} {(pieces > 1 ? "pieces" : "piece")} ({grams:F0}g)";
            }
            if (food.ServingUnit == "tsp")
            {
                double tsp = Math.Round(grams / 5.0, 1);
                return $"{tsp} tsp ({grams:F0}g)";
            }
            return $"{grams:F0}g";
        }
    }
}
