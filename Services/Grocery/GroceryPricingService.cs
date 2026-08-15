using ValensFit.Models;
using ValensFit.Services.Nutrition;

namespace ValensFit.Services.Grocery
{
    public class GroceryPricingService
    {
        private readonly FoodDatabase _foodDb;
        private readonly ILogger<GroceryPricingService> _logger;

        public GroceryPricingService(FoodDatabase foodDb, ILogger<GroceryPricingService> logger)
        {
            _foodDb = foodDb;
            _logger = logger;
        }

        public GroceryBudgetVerdictModel CalculateDeterministicGroceryPlan(
            List<DayPlanModel> days, 
            UserInputModel input)
        {
            var currency = !string.IsNullOrWhiteSpace(input.Currency) ? input.Currency.Trim().ToUpperInvariant() : "BDT";
            var weeklyTotals = new Dictionary<string, double>(); // FoodId -> total grams across 7 days

            foreach (var day in days)
            {
                foreach (var meal in day.Meals)
                {
                    foreach (var item in meal.Items)
                    {
                        if (!weeklyTotals.ContainsKey(item.FoodId))
                        {
                            weeklyTotals[item.FoodId] = 0;
                        }
                        weeklyTotals[item.FoodId] += item.Grams;
                    }
                }
            }

            var groceryItems = new List<WeeklyGroceryItem>();
            decimal totalWeeklyCost = 0m;

            foreach (var kvp in weeklyTotals)
            {
                var foodId = kvp.Key;
                var totalGrams = kvp.Value;
                var food = _foodDb.GetFoodById(foodId);
                var (pricePer100g, note, unit, pricePerUnit) = _foodDb.GetPriceEstimate(foodId, currency);

                // Calculate item cost
                decimal itemCost = Math.Round((decimal)(totalGrams / 100.0) * pricePer100g, 0);
                totalWeeklyCost += itemCost;

                string displayWeeklyQty = FormatWeeklyQuantity(food, totalGrams);

                groceryItems.Add(new WeeklyGroceryItem
                {
                    FoodId = foodId,
                    FoodName = food?.Name ?? foodId,
                    Category = food?.Category ?? "Pantry",
                    TotalGrams = Math.Round(totalGrams, 0),
                    DisplayWeeklyQuantity = displayWeeklyQty,
                    UnitPrice = pricePerUnit,
                    PriceUnit = unit,
                    TotalCost = itemCost,
                    Currency = currency
                });
            }

            // Sort by Category (Protein, Carb, Veggie, HealthyFat) then Total Cost descending
            var categoryOrder = new Dictionary<string, int>
            {
                { "Protein", 1 },
                { "Dairy", 2 },
                { "Carb", 3 },
                { "Vegetable", 4 },
                { "HealthyFat", 5 },
                { "Fruit", 6 }
            };

            groceryItems = groceryItems
                .OrderBy(g => categoryOrder.TryGetValue(g.Category, out var o) ? o : 99)
                .ThenByDescending(g => g.TotalCost)
                .ToList();

            decimal totalMonthlyCost = Math.Round(totalWeeklyCost * 4.33m, 0); // 4.33 weeks in a month

            var verdictModel = new GroceryBudgetVerdictModel
            {
                Items = groceryItems,
                EstimatedWeeklyCost = totalWeeklyCost,
                EstimatedMonthlyCost = totalMonthlyCost,
                UserMonthlyBudget = input.MonthlyBudget,
                Currency = currency,
                Source = "Deterministic Local Market Price Index"
            };

            // Rule-based budget evaluation
            EvaluateBudgetVerdict(verdictModel, input.MonthlyBudget);

            return verdictModel;
        }

        private void EvaluateBudgetVerdict(GroceryBudgetVerdictModel model, decimal? userBudget)
        {
            if (!userBudget.HasValue || userBudget.Value <= 0)
            {
                model.Verdict = "fits";
                model.VerdictTitle = "ESTIMATED MARKET COST";
                model.Notes = $"Weekly grocery expenditure is estimated at {model.Currency} {model.EstimatedWeeklyCost:N0} (~{model.Currency} {model.EstimatedMonthlyCost:N0}/month).";
                model.SwapSuggestions.Add("Buy staple grains (rice/oats) and whole eggs in bulk wholesale trays to save 15-20%.");
                return;
            }

            decimal monthlyBudget = userBudget.Value;
            decimal estCost = model.EstimatedMonthlyCost;

            if (estCost <= monthlyBudget * 0.90m)
            {
                model.Verdict = "fits";
                model.VerdictTitle = "VICTORY: FITS COMFORTABLY";
                model.Notes = $"Your monthly budget of {model.Currency} {monthlyBudget:N0} comfortably covers the estimated monthly food expenditure of {model.Currency} {estCost:N0} with a surplus buffer of {model.Currency} {(monthlyBudget - estCost):N0}.";
                model.SwapSuggestions.Add("You can occasionally upgrade protein sources to lean beef cuts or salmon while remaining within your allocation.");
            }
            else if (estCost <= monthlyBudget * 1.05m)
            {
                model.Verdict = "tight";
                model.VerdictTitle = "ALERT: TIGHT BUDGET FIT";
                model.Notes = $"Your monthly estimate of {model.Currency} {estCost:N0} closely tracks your declared budget of {model.Currency} {monthlyBudget:N0} (approx {(estCost / monthlyBudget * 100):F0}% utilization).";
                model.SwapSuggestions.Add("Substitute 1 whole-egg portion for chicken breast on alternating days to trim 10-15% of protein cost.");
                model.SwapSuggestions.Add("Purchase seasonal local greens (lau, spinach, cabbage) over premium imported vegetables.");
            }
            else
            {
                model.Verdict = "over_budget";
                model.VerdictTitle = "EXCEEDS BUDGET ALLOCATION";
                model.Notes = $"Estimated monthly expenditure ({model.Currency} {estCost:N0}) exceeds your targeted budget of {model.Currency} {monthlyBudget:N0} by approximately {model.Currency} {(estCost - monthlyBudget):N0}.";
                model.SwapSuggestions.Add("Increase the proportion of eggs, masoor dal (red lentils), and tok doi/curd to reach high protein at 40% lower cost.");
                model.SwapSuggestions.Add("Buy local chicken breast in whole kilo portions from wholesale wet markets rather than supermarket pre-cuts.");
            }
        }

        private string FormatWeeklyQuantity(FoodItem? food, double totalGrams)
        {
            if (food == null) return $"{totalGrams:F0} g";

            if (food.ServingUnit == "piece" && food.GramsPerServingUnit > 1)
            {
                int pieces = (int)Math.Round(totalGrams / food.GramsPerServingUnit);
                if (food.Id.Contains("egg"))
                {
                    return $"{pieces} eggs (~{totalGrams / 1000.0:F1} kg)";
                }
                return $"{pieces} pieces (~{totalGrams / 1000.0:F1} kg)";
            }

            if (totalGrams >= 1000.0)
            {
                return $"{totalGrams / 1000.0:F2} kg";
            }
            return $"{totalGrams:F0} g";
        }
    }
}
