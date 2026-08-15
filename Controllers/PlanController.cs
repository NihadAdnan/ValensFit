using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ValensFit.Models;
using ValensFit.Services;
using ValensFit.Services.Nutrition;

namespace ValensFit.Controllers
{
    public class PlanController : Controller
    {
        private readonly PlanCalculatorService _planCalculator;
        private readonly MealSwapService _mealSwapService;
        private readonly FoodDatabase _foodDb;
        private readonly ILogger<PlanController> _logger;

        private const string PlanSessionKey = "ValensFit_ActivePlan";

        public PlanController(
            PlanCalculatorService planCalculator,
            MealSwapService mealSwapService,
            FoodDatabase foodDb,
            ILogger<PlanController> logger)
        {
            _planCalculator = planCalculator;
            _mealSwapService = mealSwapService;
            _foodDb = foodDb;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var defaultModel = new UserInputModel();
            return View("Wizard", defaultModel);
        }

        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] UserInputModel? model)
        {
            var input = model ?? new UserInputModel();

            // Default or clamp key fields safely
            if (string.IsNullOrWhiteSpace(input.FirstName))
            {
                input.FirstName = "Friend";
            }
            if (input.Age < 13 || input.Age > 80)
            {
                input.Age = Math.Clamp(input.Age, 13, 80);
            }

            try
            {
                var plan = await _planCalculator.GenerateComprehensivePlanAsync(input);

                // Store in session for result retrieval
                var json = JsonSerializer.Serialize(plan);
                HttpContext.Session.SetString(PlanSessionKey, json);

                return Ok(new { success = true, plan = plan });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating plan for user {Name}", input.FirstName);
                return StatusCode(500, new { success = false, message = "Calculation error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Result()
        {
            var json = HttpContext.Session.GetString(PlanSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("Index");
            }

            try
            {
                var plan = JsonSerializer.Deserialize<PlanResultModel>(json);
                if (plan == null) return RedirectToAction("Index");
                return View("Result", plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize active plan from session.");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult SwapItem([FromBody] MealSwapService.SwapItemRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ReplacementFoodId))
            {
                return BadRequest(new { success = false, message = "Invalid swap request." });
            }

            var response = _mealSwapService.SwapFoodItem(request);
            return Ok(response);
        }

        [HttpGet]
        public IActionResult GetFoodOptions(string? category, string? mealSlot = "Lunch")
        {
            var foods = _foodDb.GetAllFoods();
            if (!string.IsNullOrWhiteSpace(category))
            {
                foods = foods.Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(foods.Select(f => new
            {
                f.Id,
                f.Name,
                f.Category,
                f.CaloriesPer100g,
                f.ProteinPer100g,
                f.CarbsPer100g,
                f.FatPer100g,
                f.ServingUnit
            }));
        }

        [HttpGet]
        public IActionResult Preset(string type)
        {
            UserInputModel preset = type?.ToLowerInvariant() switch
            {
                "student_cut" => new UserInputModel
                {
                    FirstName = "Rafi",
                    Gender = "Male",
                    Age = 22,
                    Height = 173,
                    HeightUnit = "cm",
                    Weight = 78,
                    WeightUnit = "kg",
                    ActivityLevel = "ModeratelyActive",
                    Goal = "LoseFat",
                    MaximizeMuscleRetention = true,
                    TargetWeightLossKg = 6,
                    TimeframeWeeks = 10,
                    Country = "Bangladesh",
                    CityRegion = "Dhaka",
                    MonthlyBudget = 7000,
                    Currency = "BDT",
                    DietPreferences = new List<string> { "Halal", "Egg + Chicken Only" },
                    ExercisePreference = "Gym",
                    MinutesPerSession = 50,
                    DaysPerWeek = 4,
                    ExperienceLevel = "Beginner",
                    OfficeLunch = true,
                    OfficeLunchDescription = "Rice with chicken curry (1 piece)"
                },
                "hypertrophy" => new UserInputModel
                {
                    FirstName = "Marcus",
                    Gender = "Male",
                    Age = 26,
                    Height = 182,
                    HeightUnit = "cm",
                    Weight = 75,
                    WeightUnit = "kg",
                    ActivityLevel = "VeryActive",
                    Goal = "BuildMuscle",
                    MaximizeMuscleRetention = true,
                    Country = "United States",
                    CityRegion = "Chicago",
                    MonthlyBudget = 350,
                    Currency = "USD",
                    DietPreferences = new List<string> { "High Protein" },
                    ExercisePreference = "Gym",
                    MinutesPerSession = 60,
                    DaysPerWeek = 5,
                    ExperienceLevel = "Intermediate"
                },
                "home_fit" => new UserInputModel
                {
                    FirstName = "Elena",
                    Gender = "Female",
                    Age = 29,
                    Height = 165,
                    HeightUnit = "cm",
                    Weight = 62,
                    WeightUnit = "kg",
                    ActivityLevel = "LightlyActive",
                    Goal = "Maintain",
                    Country = "United Kingdom",
                    CityRegion = "London",
                    MonthlyBudget = 200,
                    Currency = "GBP",
                    DietPreferences = new List<string> { "Vegetarian" },
                    ExercisePreference = "HomeBodyweight",
                    MinutesPerSession = 40,
                    DaysPerWeek = 3,
                    ExperienceLevel = "Beginner"
                },
                _ => new UserInputModel()
            };

            return Json(preset);
        }
    }
}
