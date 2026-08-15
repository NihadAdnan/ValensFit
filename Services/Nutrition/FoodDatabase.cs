using System.Text.Json;
using ValensFit.Models;

namespace ValensFit.Services.Nutrition
{
    public class FoodDatabase
    {
        private readonly List<FoodItem> _foods = new();
        private readonly Dictionary<string, JsonElement> _prices = new();
        private readonly ILogger<FoodDatabase> _logger;

        public FoodDatabase(IWebHostEnvironment env, ILogger<FoodDatabase> logger)
        {
            _logger = logger;
            LoadFoods(env);
            LoadPrices(env);
        }

        private void LoadFoods(IWebHostEnvironment env)
        {
            try
            {
                var foodsPath = Path.Combine(env.ContentRootPath, "Data", "foods.json");
                if (File.Exists(foodsPath))
                {
                    var json = File.ReadAllText(foodsPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var list = JsonSerializer.Deserialize<List<FoodItem>>(json, options);
                    if (list != null && list.Count > 0)
                    {
                        _foods.AddRange(list);
                        _logger.LogInformation("Loaded {Count} food items from foods.json", _foods.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load foods.json");
            }
        }

        private void LoadPrices(IWebHostEnvironment env)
        {
            try
            {
                var pricesPath = Path.Combine(env.ContentRootPath, "Data", "prices.json");
                if (File.Exists(pricesPath))
                {
                    var json = File.ReadAllText(pricesPath);
                    using var doc = JsonDocument.Parse(json);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        _prices[prop.Name.ToUpperInvariant()] = prop.Value.Clone();
                    }
                    _logger.LogInformation("Loaded regional prices for currencies: {Currencies}", string.Join(", ", _prices.Keys));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load prices.json");
            }
        }

        public IReadOnlyList<FoodItem> GetAllFoods() => _foods;

        public FoodItem? GetFoodById(string id)
        {
            return _foods.FirstOrDefault(f => f.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public List<FoodItem> FilterFoods(
            List<string>? dietTags,
            string? customExclusions,
            string mealSlot,
            string? category = null)
        {
            var query = _foods.Where(f => f.AllowedMealSlots.Contains(mealSlot, StringComparer.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (dietTags != null && dietTags.Count > 0)
            {
                foreach (var tag in dietTags)
                {
                    var cleanTag = tag.Trim().ToLowerInvariant();
                    if (cleanTag == "vegetarian")
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("Vegetarian", StringComparison.OrdinalIgnoreCase)));
                    }
                    else if (cleanTag == "vegan")
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("Vegan", StringComparison.OrdinalIgnoreCase)));
                    }
                    else if (cleanTag.Contains("no beef") || cleanTag == "nobeef")
                    {
                        query = query.Where(f => !f.Id.Contains("beef", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (cleanTag.Contains("no pork") || cleanTag == "nopork")
                    {
                        query = query.Where(f => !f.Id.Contains("pork", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (cleanTag.Contains("no fish") || cleanTag == "nofish")
                    {
                        query = query.Where(f => !f.Id.Contains("fish", StringComparison.OrdinalIgnoreCase) && !f.Id.Contains("tuna", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (cleanTag.Contains("egg") && cleanTag.Contains("chicken"))
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("EggChickenOnly", StringComparison.OrdinalIgnoreCase)) ||
                                                 f.Category == "Carb" || f.Category == "Vegetable" || f.Category == "HealthyFat");
                    }
                    else if (cleanTag == "halal")
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("Halal", StringComparison.OrdinalIgnoreCase)));
                    }
                    else if (cleanTag.Contains("lactose") || cleanTag.Contains("dairy"))
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("DairyFree", StringComparison.OrdinalIgnoreCase)));
                    }
                    else if (cleanTag.Contains("gluten"))
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("GlutenFree", StringComparison.OrdinalIgnoreCase)));
                    }
                    else if (cleanTag.Contains("nut"))
                    {
                        query = query.Where(f => f.DietTags.Any(t => t.Equals("NutFree", StringComparison.OrdinalIgnoreCase)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(customExclusions))
            {
                var keywords = customExclusions.ToLowerInvariant()
                    .Split(new[] { ',', ';', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var kw in keywords)
                {
                    if (kw.Length < 3) continue;
                    query = query.Where(f => !f.Name.ToLowerInvariant().Contains(kw) && !f.Id.ToLowerInvariant().Contains(kw));
                }
            }

            var result = query.ToList();
            // Fallback safety if filter is too aggressive: return all items of category
            if (result.Count == 0 && !string.IsNullOrWhiteSpace(category))
            {
                return _foods.Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return result;
        }

        public (decimal pricePer100g, string note, string unit, decimal pricePerUnit) GetPriceEstimate(string foodId, string currency)
        {
            var currKey = (currency ?? "BDT").Trim().ToUpperInvariant();
            if (!_prices.ContainsKey(currKey))
            {
                currKey = _prices.ContainsKey("USD") ? "USD" : _prices.Keys.FirstOrDefault() ?? "BDT";
            }

            if (_prices.TryGetValue(currKey, out var currObj))
            {
                if (currObj.TryGetProperty("Prices", out var pricesMap) && pricesMap.TryGetProperty(foodId, out var itemPrice))
                {
                    decimal pricePer100g = itemPrice.TryGetProperty("PricePer100g", out var p100) ? p100.GetDecimal() : 0m;
                    string note = itemPrice.TryGetProperty("Note", out var n) ? n.GetString() ?? "" : "";
                    string unit = itemPrice.TryGetProperty("Unit", out var u) ? u.GetString() ?? "100g" : "100g";
                    decimal pricePerUnit = itemPrice.TryGetProperty("PricePerUnit", out var pu) ? pu.GetDecimal() : pricePer100g;

                    return (pricePer100g, note, unit, pricePerUnit);
                }
            }

            var food = GetFoodById(foodId);
            return (food?.DefaultPricePer100g ?? 10m, "Standard estimate", "100g", food?.DefaultPricePer100g ?? 10m);
        }
    }
}
