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
                decimal itemWeeklyCost = Math.Round((decimal)(totalGrams / 100.0) * pricePer100g, 0);
                decimal itemMonthlyCost = Math.Round(itemWeeklyCost * 4.33m, 0);
                totalWeeklyCost += itemWeeklyCost;

                string displayWeeklyQty = FormatWeeklyQuantity(food, totalGrams);

                groceryItems.Add(new WeeklyGroceryItem
                {
                    FoodId = foodId,
                    FoodName = food?.Name ?? foodId,
                    Category = food?.Category ?? "Protein",
                    TotalGrams = Math.Round(totalGrams, 0),
                    DisplayWeeklyQuantity = displayWeeklyQty,
                    UnitPrice = pricePerUnit,
                    PriceUnit = unit,
                    TotalCost = itemWeeklyCost,
                    EstimatedMonthlyCost = itemMonthlyCost,
                    Currency = currency
                });
            }

            // Sort by Category then Total Cost descending
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

            decimal totalMonthlyCost = Math.Round(totalWeeklyCost * 4.33m, 0); // 4.33 weeks per month

            var verdictModel = new GroceryBudgetVerdictModel
            {
                Items = groceryItems,
                EstimatedWeeklyCost = totalWeeklyCost,
                EstimatedMonthlyCost = totalMonthlyCost,
                UserMonthlyBudget = input.MonthlyBudget,
                Currency = currency,
                Source = "Regional Grocery Price Index"
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
                model.VerdictTitle = "Estimated Market Cost";
                model.Notes = $"Estimated grocery expenditure is {model.Currency} {model.EstimatedWeeklyCost:N0} / week (approx. {model.Currency} {model.EstimatedMonthlyCost:N0} / month).";
                model.SwapSuggestions.Add("Buy eggs in wholesale crates (30-egg tray) and staple grains in 5kg bags to save 15-20%.");
                return;
            }

            decimal monthlyBudget = userBudget.Value;
            decimal estCost = model.EstimatedMonthlyCost;

            if (estCost <= monthlyBudget * 0.90m)
            {
                model.Verdict = "fits";
                model.VerdictTitle = "Within Budget";
                model.Notes = $"Your monthly budget of {model.Currency} {monthlyBudget:N0} comfortably covers estimated monthly grocery costs ({model.Currency} {estCost:N0}) with a buffer of {model.Currency} {(monthlyBudget - estCost):N0}.";
                model.SwapSuggestions.Add("Budget is well balanced. You can maintain this plan consistently without financial strain.");
            }
            else if (estCost <= monthlyBudget * 1.05m)
            {
                model.Verdict = "tight";
                model.VerdictTitle = "Tight Budget Fit";
                model.Notes = $"Estimated monthly expenditure ({model.Currency} {estCost:N0}) closely matches your budget of {model.Currency} {monthlyBudget:N0} (~{(estCost / monthlyBudget * 100):F0}% utilization).";
                model.SwapSuggestions.Add("Swap 1 chicken portion for farm eggs or masoor dal on 2 days each week to trim ~10-15% of protein cost.");
                model.SwapSuggestions.Add("Prioritize seasonal local greens (palong shaak, lau, bandhakopi) over off-season produce.");
            }
            else
            {
                model.Verdict = "over_budget";
                model.VerdictTitle = "Over Budget Target";
                model.Notes = $"Estimated monthly cost ({model.Currency} {estCost:N0}) exceeds your targeted budget of {model.Currency} {monthlyBudget:N0} by approximately {model.Currency} {(estCost - monthlyBudget):N0}.";
                model.SwapSuggestions.Add("Substitute some chicken breast meals with whole eggs, masoor dal, and plain sour curd (tok doi) to maintain high protein at lower cost.");
                model.SwapSuggestions.Add("Purchase whole bone-in chicken or fresh fish from wholesale wet markets rather than supermarket pre-cuts.");
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
