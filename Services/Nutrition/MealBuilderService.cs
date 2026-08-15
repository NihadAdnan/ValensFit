using ValensFit.Models;

namespace ValensFit.Services.Nutrition
{
    public class MealBuilderService
    {
        private readonly FoodDatabase _foodDb;
        private readonly ILogger<MealBuilderService> _logger;

        public MealBuilderService(FoodDatabase foodDb, ILogger<MealBuilderService> logger)
        {
            _foodDb = foodDb;
            _logger = logger;
        }

        public List<DayPlanModel> BuildSevenDayPlan(
            UserInputModel input, 
            double dailyTargetKcal, 
            double dailyTargetProtein,
            double dailyTargetFat,
            double dailyTargetCarb)
        {
            var days = new List<DayPlanModel>();
            var dayRomanNumerals = new[] { "I", "II", "III", "IV", "V", "VI", "VII" };
            var dayThemes = new[]
            {
                "High Energy Kickoff & Metabolic Priming",
                "Strength & Lean Definition",
                "Endurance & Glycogen Replenishment",
                "Mid-Week Peak Focus & Athletic Drive",
                "Cellular Recovery & Sustained Power",
                "Fiber Cleanse & Digestive Balance",
                "Weekly Rejuvenation & Muscle Fortification"
            };

            // Meal split percentages
            double breakfastPct = 0.30;
            double lunchPct = 0.35;
            double dinnerPct = 0.35;

            bool isLightBreakfast = input.MealStructure?.Equals("LightBreakfast", StringComparison.OrdinalIgnoreCase) == true;

            if (isLightBreakfast)
            {
                // Light Breakfast (e.g. 2 eggs + tea/fruit ~ 15-18% of calories)
                breakfastPct = 0.18;
                lunchPct = 0.41;
                dinnerPct = 0.41;
            }
            else if (input.OfficeLunch)
            {
                // Portable outside lunch
                lunchPct = 0.25;
                breakfastPct = 0.375;
                dinnerPct = 0.375;
            }

            // Food pool rotation history to avoid back-to-back repetitive choices
            var usedProteinHistory = new Queue<string>();
            var usedVegHistory = new Queue<string>();

            // Distinct vegetable rotation list
            var signatureVeggies = new[]
            {
                "lau_bottle_gourd",
                "potol_pointed_gourd",
                "spinach_shaak",
                "cabbage_shredded",
                "shim_hyacinth_beans",
                "sweet_pumpkin_misti_kumra",
                "cucumber_tomato_salad"
            };

            for (int dayIdx = 0; dayIdx < 7; dayIdx++)
            {
                var dayPlan = new DayPlanModel
                {
                    DayNumber = dayIdx + 1,
                    DayName = $"Day {dayRomanNumerals[dayIdx]}",
                    DayTheme = dayThemes[dayIdx]
                };

                // 1. Breakfast
                double bTargetKcal = dailyTargetKcal * breakfastPct;
                double bTargetProtein = dailyTargetProtein * breakfastPct;
                var breakfast = isLightBreakfast 
                    ? BuildLightBreakfast(input, bTargetKcal, bTargetProtein, dayIdx)
                    : BuildMeal(input, "Breakfast", "Morning Breakfast", bTargetKcal, bTargetProtein, dayIdx, usedProteinHistory, usedVegHistory, null);
                dayPlan.Meals.Add(breakfast);

                // 2. Lunch
                double lTargetKcal = dailyTargetKcal * lunchPct;
                double lTargetProtein = dailyTargetProtein * lunchPct;
                string preferredLunchVeg = signatureVeggies[dayIdx % signatureVeggies.Length];
                
                string lunchTitle = input.OfficeLunch 
                    ? (!string.IsNullOrWhiteSpace(input.OfficeLunchDescription) ? $"Outside Lunch ({input.OfficeLunchDescription})" : "Office / Outside Lunch")
                    : "Midday Meal";

                var lunch = BuildMeal(input, "Lunch", lunchTitle, lTargetKcal, lTargetProtein, dayIdx, usedProteinHistory, usedVegHistory, preferredLunchVeg);
                if (input.OfficeLunch && !string.IsNullOrWhiteSpace(input.OfficeLunchDescription))
                {
                    lunch.CookingTip = $"Accounted for your customary outside lunch: {input.OfficeLunchDescription}. Remaining daily macros balanced across breakfast and dinner.";
                }
                dayPlan.Meals.Add(lunch);

                // 3. Dinner
                double dTargetKcal = dailyTargetKcal * dinnerPct;
                double dTargetProtein = dailyTargetProtein * dinnerPct;
                string preferredDinnerVeg = signatureVeggies[(dayIdx + 3) % signatureVeggies.Length];
                var dinner = BuildMeal(input, "Dinner", "Evening Dinner", dTargetKcal, dTargetProtein, dayIdx, usedProteinHistory, usedVegHistory, preferredDinnerVeg);
                dayPlan.Meals.Add(dinner);

                // Calculate Day Totals
                dayPlan.TotalCalories = Math.Round(dayPlan.Meals.Sum(m => m.ActualCalories), 0);
                dayPlan.TotalProtein = Math.Round(dayPlan.Meals.Sum(m => m.ActualProtein), 1);
                dayPlan.TotalCarbs = Math.Round(dayPlan.Meals.Sum(m => m.ActualCarbs), 1);
                dayPlan.TotalFat = Math.Round(dayPlan.Meals.Sum(m => m.ActualFat), 1);

                // Day-level validation & convergence check:
                double dayKcalDiff = dailyTargetKcal - dayPlan.TotalCalories;
                if (Math.Abs(dayKcalDiff) > (dailyTargetKcal * 0.03))
                {
                    AdjustMealToFit(dinner, dayKcalDiff);
                    dayPlan.TotalCalories = Math.Round(dayPlan.Meals.Sum(m => m.ActualCalories), 0);
                    dayPlan.TotalProtein = Math.Round(dayPlan.Meals.Sum(m => m.ActualProtein), 1);
                    dayPlan.TotalCarbs = Math.Round(dayPlan.Meals.Sum(m => m.ActualCarbs), 1);
                    dayPlan.TotalFat = Math.Round(dayPlan.Meals.Sum(m => m.ActualFat), 1);
                }

                days.Add(dayPlan);
            }

            return days;
        }

        private MealPlanModel BuildLightBreakfast(UserInputModel input, double targetKcal, double targetProtein, int dayIdx)
        {
            var meal = new MealPlanModel
            {
                SlotName = "Breakfast",
                Title = "Light Morning Breakfast",
                TargetCalories = Math.Round(targetKcal, 0),
                TargetProtein = Math.Round(targetProtein, 1),
                TimingGuidance = "7:30 AM - 9:00 AM (Light & Fast Digestion)"
            };

            var egg = _foodDb.GetFoodById("egg_whole") ?? _foodDb.GetAllFoods().First(f => f.Category == "Protein");
            var banana = _foodDb.GetFoodById("banana_fresh");

            // 2 whole boiled eggs (~100g -> ~143 kcal, 12.6g protein)
            AddMealItem(meal, egg, 100.0);

            // Optional 1 piece fruit (banana) or 1 whole wheat roti if target allows
            if (targetKcal > 250 && banana != null)
            {
                AddMealItem(meal, banana, 100.0);
            }
            else
            {
                var salad = _foodDb.GetFoodById("cucumber_tomato_salad");
                if (salad != null) AddMealItem(meal, salad, 100.0);
            }

            meal.ActualCalories = Math.Round(meal.Items.Sum(i => i.Calories), 0);
            meal.ActualProtein = Math.Round(meal.Items.Sum(i => i.Protein), 1);
            meal.ActualCarbs = Math.Round(meal.Items.Sum(i => i.Carbs), 1);
            meal.ActualFat = Math.Round(meal.Items.Sum(i => i.Fat), 1);

            meal.CookingTip = "Light breakfast mode: 2 boiled eggs + green tea or black coffee (sugar-free). Caloric budget saved for fuller lunch & dinner.";

            return meal;
        }

        private MealPlanModel BuildMeal(
            UserInputModel input,
            string slot,
            string title,
            double targetKcal,
            double targetProtein,
            int dayIdx,
            Queue<string> proteinHistory,
            Queue<string> vegHistory,
            string? preferredVegId)
        {
            var meal = new MealPlanModel
            {
                SlotName = slot,
                Title = title,
                TargetCalories = Math.Round(targetKcal, 0),
                TargetProtein = Math.Round(targetProtein, 1),
                TimingGuidance = slot switch
                {
                    "Breakfast" => "7:30 AM - 9:00 AM (Within 1-2 hours of waking)",
                    "Lunch" => "1:00 PM - 2:30 PM (Midday energy peak)",
                    "Dinner" => "7:30 PM - 9:00 PM (At least 2.5 hours prior to sleep)",
                    _ => "Flexible timing"
                }
            };

            // 1. Pick Protein filtered by user region & diet tags
            var proteinPool = _foodDb.FilterFoods(input.DietPreferences, input.CustomRestrictions, slot, "Protein", input.Country);
            if (proteinPool.Count == 0) proteinPool = _foodDb.FilterFoods(null, null, slot, "Protein", input.Country);
            if (proteinPool.Count == 0) proteinPool = _foodDb.GetAllFoods().Where(f => f.Category == "Protein").ToList();

            var candidateProtein = proteinPool.FirstOrDefault(p => !proteinHistory.Contains(p.Id)) ?? proteinPool[dayIdx % proteinPool.Count];
            proteinHistory.Enqueue(candidateProtein.Id);
            if (proteinHistory.Count > 3) proteinHistory.Dequeue();

            // 2. Pick Carb filtered by user region & diet tags
            var carbPool = _foodDb.FilterFoods(input.DietPreferences, input.CustomRestrictions, slot, "Carb", input.Country);
            if (carbPool.Count == 0) carbPool = _foodDb.FilterFoods(null, null, slot, "Carb", input.Country);
            if (carbPool.Count == 0) carbPool = _foodDb.GetAllFoods().Where(f => f.Category == "Carb").ToList();
            var candidateCarb = carbPool[dayIdx % carbPool.Count];

            // 3. Pick Veggie
            FoodItem? candidateVeg = null;
            if (!string.IsNullOrEmpty(preferredVegId))
            {
                candidateVeg = _foodDb.GetFoodById(preferredVegId);
            }
            if (candidateVeg == null)
            {
                var vegPool = _foodDb.FilterFoods(input.DietPreferences, input.CustomRestrictions, slot, "Vegetable", input.Country);
                if (vegPool.Count == 0) vegPool = _foodDb.FilterFoods(null, null, slot, "Vegetable", input.Country);
                candidateVeg = vegPool.FirstOrDefault(v => !vegHistory.Contains(v.Id)) ?? vegPool[dayIdx % vegPool.Count];
            }
            vegHistory.Enqueue(candidateVeg.Id);
            if (vegHistory.Count > 3) vegHistory.Dequeue();

            // 4. Healthy Fat / Oil (1-2 tsp strictly capped)
            var oilItem = _foodDb.GetFoodById("mustard_oil_or_olive_oil");

            // --- Iterative Greedy Portion Fitting Solver ---
            double pGrams = candidateProtein.DefaultGramPortion;
            double cGrams = candidateCarb.DefaultGramPortion;
            double vGrams = candidateVeg.DefaultGramPortion;
            double oilGrams = slot == "Breakfast" ? 0.0 : 5.0;

            // Step A: Fit protein grams
            if (candidateProtein.ProteinPer100g > 0)
            {
                double proteinNeeded = Math.Max(targetProtein - (candidateCarb.ProteinPer100g * (cGrams / 100.0)), 15.0);
                pGrams = (proteinNeeded / candidateProtein.ProteinPer100g) * 100.0;
                pGrams = Math.Clamp(pGrams, 40.0, 400.0);
            }

            pGrams = RoundPractical(candidateProtein, pGrams);

            // Step B: Fit carb grams to hit target calories
            double currentKcalWithoutCarb = (candidateProtein.CaloriesPer100g * (pGrams / 100.0)) +
                                           (candidateVeg.CaloriesPer100g * (vGrams / 100.0)) +
                                           (oilItem != null ? oilItem.CaloriesPer100g * (oilGrams / 100.0) : 0);

            double remainingKcal = targetKcal - currentKcalWithoutCarb;
            if (candidateCarb.CaloriesPer100g > 0 && remainingKcal > 0)
            {
                cGrams = (remainingKcal / candidateCarb.CaloriesPer100g) * 100.0;
                cGrams = Math.Clamp(cGrams, 20.0, 450.0);
            }

            cGrams = RoundPractical(candidateCarb, cGrams);
            vGrams = Math.Round(vGrams / 10.0) * 10.0;

            // Add Food Items to Meal
            AddMealItem(meal, candidateProtein, pGrams);
            AddMealItem(meal, candidateCarb, cGrams);
            AddMealItem(meal, candidateVeg, vGrams);
            if (oilGrams > 0 && oilItem != null)
            {
                AddMealItem(meal, oilItem, oilGrams);
            }

            // Calculate actuals
            meal.ActualCalories = Math.Round(meal.Items.Sum(i => i.Calories), 0);
            meal.ActualProtein = Math.Round(meal.Items.Sum(i => i.Protein), 1);
            meal.ActualCarbs = Math.Round(meal.Items.Sum(i => i.Carbs), 1);
            meal.ActualFat = Math.Round(meal.Items.Sum(i => i.Fat), 1);

            meal.CookingTip = $"{candidateProtein.PrepNotes} Pair with steamed {candidateVeg.Name} for optimal digestion and satiety.";

            return meal;
        }

        private void AdjustMealToFit(MealPlanModel meal, double kcalDiff)
        {
            var carbItem = meal.Items.FirstOrDefault(i => i.Category == "Carb");
            if (carbItem == null) return;

            var food = _foodDb.GetFoodById(carbItem.FoodId);
            if (food == null || food.CaloriesPer100g <= 0) return;

            double addGrams = (kcalDiff / food.CaloriesPer100g) * 100.0;
            double newGrams = Math.Clamp(carbItem.Grams + addGrams, 30.0, 450.0);
            newGrams = RoundPractical(food, newGrams);

            double scale = newGrams / 100.0;
            carbItem.Grams = newGrams;
            carbItem.DisplayQuantity = FormatDisplayQuantity(food, newGrams);
            carbItem.Calories = Math.Round(food.CaloriesPer100g * scale, 0);
            carbItem.Protein = Math.Round(food.ProteinPer100g * scale, 1);
            carbItem.Carbs = Math.Round(food.CarbsPer100g * scale, 1);
            carbItem.Fat = Math.Round(food.FatPer100g * scale, 1);

            meal.ActualCalories = Math.Round(meal.Items.Sum(i => i.Calories), 0);
            meal.ActualProtein = Math.Round(meal.Items.Sum(i => i.Protein), 1);
            meal.ActualCarbs = Math.Round(meal.Items.Sum(i => i.Carbs), 1);
            meal.ActualFat = Math.Round(meal.Items.Sum(i => i.Fat), 1);
        }

        private double RoundPractical(FoodItem food, double grams)
        {
            if (food.ServingUnit == "piece" && food.GramsPerServingUnit > 1)
            {
                double pieces = Math.Round(grams / food.GramsPerServingUnit);
                pieces = Math.Max(1, pieces);
                return pieces * food.GramsPerServingUnit;
            }
            return Math.Round(grams / 5.0) * 5.0; // round to nearest 5 grams
        }

        private void AddMealItem(MealPlanModel meal, FoodItem food, double grams)
        {
            double scale = grams / 100.0;
            string displayQty = FormatDisplayQuantity(food, grams);

            meal.Items.Add(new MealFoodItem
            {
                FoodId = food.Id,
                FoodName = food.Name,
                Category = food.Category,
                Grams = Math.Round(grams, 0),
                DisplayQuantity = displayQty,
                Calories = Math.Round(food.CaloriesPer100g * scale, 0),
                Protein = Math.Round(food.ProteinPer100g * scale, 1),
                Carbs = Math.Round(food.CarbsPer100g * scale, 1),
                Fat = Math.Round(food.FatPer100g * scale, 1),
                PrepNotes = food.PrepNotes
            });
        }

        private string FormatDisplayQuantity(FoodItem food, double grams)
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
