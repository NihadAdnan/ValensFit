using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ValensFit.Models;
using ValensFit.Services;
using ValensFit.Services.Exercise;
using ValensFit.Services.Nutrition;

namespace ValensFit.Controllers
{
    public class PlanController : Controller
    {
        private readonly PlanCalculatorService _planCalculator;
        private readonly MealSwapService _mealSwapService;
        private readonly FoodDatabase _foodDb;
        private readonly ExercisePlanService _exerciseService;
        private readonly DailyCalorieCalculatorService _calorieCalculator;
        private readonly ILogger<PlanController> _logger;

        private const string PlanSessionKey = "ValensFit_ActivePlan";
        private const string DietSessionKey = "ValensFit_ActiveDietPlan";
        private const string WorkoutSessionKey = "ValensFit_ActiveWorkoutPlan";
        private const string TrackerSessionKey = "ValensFit_ActiveTrackerPlan";

        public PlanController(
            PlanCalculatorService planCalculator,
            MealSwapService mealSwapService,
            FoodDatabase foodDb,
            ExercisePlanService exerciseService,
            DailyCalorieCalculatorService calorieCalculator,
            ILogger<PlanController> logger)
        {
            _planCalculator = planCalculator;
            _mealSwapService = mealSwapService;
            _foodDb = foodDb;
            _exerciseService = exerciseService;
            _calorieCalculator = calorieCalculator;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        // =========================================================================
        // MODE 1: DIET & NUTRITION ARCHITECT
        // =========================================================================
        [HttpGet]
        public IActionResult Diet()
        {
            var defaultModel = new UserInputModel
            {
                PlanMode = "Diet",
                ExercisePreference = "NoExercise",
                Country = "Bangladesh",
                CityRegion = "Dhaka",
                Currency = "BDT",
                MonthlyBudget = 7000
            };
            return View("DietWizard", defaultModel);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateDiet([FromBody] UserInputModel? model)
        {
            var input = model ?? new UserInputModel();
            input.PlanMode = "Diet";
            input.Country = "Bangladesh";
            input.CityRegion = "Dhaka";
            input.Currency = "BDT";

            try
            {
                var plan = await _planCalculator.GenerateComprehensivePlanAsync(input);
                var json = JsonSerializer.Serialize(plan);
                HttpContext.Session.SetString(DietSessionKey, json);

                return Ok(new { success = true, redirectUrl = "/Plan/DietResult" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating diet plan");
                return StatusCode(500, new { success = false, message = "Calculation error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DietResult()
        {
            var json = HttpContext.Session.GetString(DietSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("Diet");
            }

            try
            {
                var plan = JsonSerializer.Deserialize<PlanResultModel>(json);
                if (plan == null) return RedirectToAction("Diet");
                return View("DietResult", plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize diet plan from session.");
                return RedirectToAction("Diet");
            }
        }

        // =========================================================================
        // MODE 2: WORKOUT & EXERCISE PLANNER
        // =========================================================================
        [HttpGet]
        public IActionResult Workout()
        {
            var defaultModel = new UserInputModel
            {
                PlanMode = "Workout",
                Country = "Bangladesh",
                CityRegion = "Dhaka",
                Currency = "BDT"
            };
            return View("WorkoutWizard", defaultModel);
        }

        [HttpPost]
        public IActionResult GenerateWorkout([FromBody] UserInputModel? model)
        {
            var input = model ?? new UserInputModel();
            input.PlanMode = "Workout";

            try
            {
                var exercisePlan = _exerciseService.GeneratePlan(input);
                var json = JsonSerializer.Serialize(new { Input = input, Workout = exercisePlan });
                HttpContext.Session.SetString(WorkoutSessionKey, json);

                return Ok(new { success = true, redirectUrl = "/Plan/WorkoutResult" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating workout plan");
                return StatusCode(500, new { success = false, message = "Calculation error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult WorkoutResult()
        {
            var json = HttpContext.Session.GetString(WorkoutSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("Workout");
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var input = JsonSerializer.Deserialize<UserInputModel>(doc.RootElement.GetProperty("input").GetRawText(), options);
                var workout = JsonSerializer.Deserialize<ExercisePlanModel>(doc.RootElement.GetProperty("workout").GetRawText(), options);

                ViewBag.UserInput = input;
                return View("WorkoutResult", workout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize workout plan from session.");
                return RedirectToAction("Workout");
            }
        }

        // =========================================================================
        // MODE 3: DAILY CALORIE TRACKER & FOOD LOGGER
        // =========================================================================
        [HttpGet]
        public IActionResult Tracker()
        {
            var defaultInput = new DailyFoodLogInput
            {
                Name = "Friend",
                Age = 25,
                BiologicalSex = "Male",
                WeightKg = 70.0,
                HeightCm = 172.0,
                ActivityLevel = "ModeratelyActive",
                Goal = "FatLoss"
            };
            return View("Tracker", defaultInput);
        }

        [HttpPost]
        public IActionResult CalculateCalories([FromBody] DailyFoodLogInput? input)
        {
            var logInput = input ?? new DailyFoodLogInput();

            try
            {
                var result = _calorieCalculator.CalculateDailyIntake(logInput);
                var json = JsonSerializer.Serialize(result);
                HttpContext.Session.SetString(TrackerSessionKey, json);

                return Ok(new { success = true, redirectUrl = "/Plan/TrackerResult", result = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating logged calories");
                return StatusCode(500, new { success = false, message = "Calculation error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TrackerResult()
        {
            var json = HttpContext.Session.GetString(TrackerSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("Tracker");
            }

            try
            {
                var result = JsonSerializer.Deserialize<FoodLogResultModel>(json);
                if (result == null) return RedirectToAction("Tracker");
                return View("TrackerResult", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize tracker result from session.");
                return RedirectToAction("Tracker");
            }
        }

        // =========================================================================
        // MODE 4: COMBINED TRANSFORMATION (DIET + WORKOUT)
        // =========================================================================
        [HttpGet]
        public IActionResult Combined()
        {
            var defaultModel = new UserInputModel
            {
                PlanMode = "Combined",
                Country = "Bangladesh",
                CityRegion = "Dhaka",
                Currency = "BDT",
                MonthlyBudget = 7000
            };
            return View("Wizard", defaultModel);
        }

        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] UserInputModel? model)
        {
            var input = model ?? new UserInputModel();
            input.Country = "Bangladesh";
            input.CityRegion = "Dhaka";
            input.Currency = "BDT";

            try
            {
                var plan = await _planCalculator.GenerateComprehensivePlanAsync(input);
                var json = JsonSerializer.Serialize(plan);
                HttpContext.Session.SetString(PlanSessionKey, json);

                return Ok(new { success = true, redirectUrl = "/Plan/Result", plan = plan });
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
                return RedirectToAction("Combined");
            }

            try
            {
                var plan = JsonSerializer.Deserialize<PlanResultModel>(json);
                if (plan == null) return RedirectToAction("Combined");
                return View("Result", plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize active plan from session.");
                return RedirectToAction("Combined");
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
                "dhaka_hypertrophy" => new UserInputModel
                {
                    FirstName = "Tanvir",
                    Gender = "Male",
                    Age = 24,
                    Height = 178,
                    HeightUnit = "cm",
                    Weight = 72,
                    WeightUnit = "kg",
                    ActivityLevel = "VeryActive",
                    Goal = "BuildMuscle",
                    MaximizeMuscleRetention = true,
                    Country = "Bangladesh",
                    CityRegion = "Dhaka",
                    MonthlyBudget = 9500,
                    Currency = "BDT",
                    DietPreferences = new List<string> { "Halal" },
                    ExercisePreference = "Gym",
                    MinutesPerSession = 60,
                    DaysPerWeek = 5,
                    ExperienceLevel = "Intermediate"
                },
                "home_fit" => new UserInputModel
                {
                    FirstName = "Nusrat",
                    Gender = "Female",
                    Age = 27,
                    Height = 160,
                    HeightUnit = "cm",
                    Weight = 63,
                    WeightUnit = "kg",
                    ActivityLevel = "LightlyActive",
                    Goal = "LoseFat",
                    Country = "Bangladesh",
                    CityRegion = "Dhaka",
                    MonthlyBudget = 6000,
                    Currency = "BDT",
                    DietPreferences = new List<string> { "Halal" },
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
