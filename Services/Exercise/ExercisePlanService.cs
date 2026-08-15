using System.Text.Json;
using ValensFit.Models;

namespace ValensFit.Services.Exercise
{
    public class ExercisePlanService
    {
        private readonly Dictionary<string, JsonElement> _workoutData = new();
        private readonly ILogger<ExercisePlanService> _logger;

        public ExercisePlanService(IWebHostEnvironment env, ILogger<ExercisePlanService> logger)
        {
            _logger = logger;
            LoadWorkouts(env);
        }

        private void LoadWorkouts(IWebHostEnvironment env)
        {
            try
            {
                var path = Path.Combine(env.ContentRootPath, "Data", "workouts.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        _workoutData[prop.Name.ToLowerInvariant()] = prop.Value.Clone();
                    }
                    _logger.LogInformation("Loaded workout templates for: {Keys}", string.Join(", ", _workoutData.Keys));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load workouts.json");
            }
        }

        public ExercisePlanModel GeneratePlan(UserInputModel input)
        {
            var plan = new ExercisePlanModel
            {
                Preference = input.ExercisePreference,
                DaysPerWeek = input.DaysPerWeek,
                MinutesPerSession = input.MinutesPerSession,
                DailyStepTarget = input.DailyStepsTarget > 0 ? input.DailyStepsTarget : 8000
            };

            string prefKey = input.ExercisePreference?.ToLowerInvariant() ?? "gym";

            if (prefKey == "noexercise")
            {
                plan.ProgramTitle = "Nutritional Focus & Daily Vitality Stride";
                plan.Rationale = "Your current phase focuses 100% on nutritional adherence and energetic balance. Calisthenic or weight training is paused, while maintaining baseline metabolic circulation through incidental daily movement.";
                plan.WalkingRationale = "Walking + dietary protein alone cannot build new contractile tissue, but 6,000-8,000 daily steps protect insulin sensitivity and non-exercise thermogenesis without inducing muscular fatigue.";
                plan.Principles.Add("Aim for 6,000 - 8,000 daily steps spaced throughout the day");
                plan.Principles.Add("Take a 5-minute walking stroll after your highest carbohydrate meal");
                plan.Principles.Add("Prioritize 7-8 hours of uninterrupted sleep for metabolic recovery");

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 1,
                    DayTitle = "Daily Movement Standard",
                    Focus = "Metabolic Health & Digestion",
                    IsRestDay = true,
                    EstimatedMinutes = 20,
                    Warmup = "Gentle head and shoulder rolls",
                    Cooldown = "Adequate hydration (target 3+ liters)",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = "Incidental Daily Walking (6,000 - 8,000 steps)", TargetMuscle = "Cardiovascular & Metabolic System", Sets = 1, Reps = "Throughout day", RestSeconds = 0, FormCue = "Take stairs when possible; take phone calls while pacing" }
                    }
                });
                return plan;
            }

            if (prefKey == "walkingonly")
            {
                plan.ProgramTitle = "Centurion Stride & Metabolic Conditioning";
                plan.Rationale = $"Low-impact aerobic conditioning programmed for {input.MinutesPerSession} minutes/day across {input.DaysPerWeek} days/week. Optimizes fat oxidation while keeping joint impact at zero.";
                plan.WalkingRationale = "Daily brisk walking triggers sustained lipolysis, lowers systemic cortisol, and maintains high non-exercise activity thermogenesis (NEAT).";
                plan.Principles.Add($"Hit your target of {plan.DailyStepTarget:N0} steps daily");
                plan.Principles.Add("Maintain a cadence of 100-115 steps/min for cardiovascular benefit");
                plan.Principles.Add("Divide walking volume into morning pre-work and evening post-dinner sessions");

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 1,
                    DayTitle = "Session A: Morning Fasted / Pre-Work Stride",
                    Focus = "Metabolic Awakening",
                    IsRestDay = false,
                    EstimatedMinutes = Math.Min(input.MinutesPerSession, 45),
                    Warmup = "2 min ankle rolls and calf raises",
                    Cooldown = "2 min standing calf and hamstring stretch",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = "Brisk Outdoor Walk", TargetMuscle = "Aerobic System / Legs", Sets = 1, Reps = $"{Math.Min(input.MinutesPerSession, 30)} mins", RestSeconds = 0, FormCue = "Maintain upright posture, chest proud, swing arms rhythmically" }
                    }
                });

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 2,
                    DayTitle = "Session B: Post-Prandial Glucose Clearance Walk",
                    Focus = "Digestion & Recovery",
                    IsRestDay = false,
                    EstimatedMinutes = 20,
                    Warmup = "Light continuous walking",
                    Cooldown = "Deep diaphragmatic breathing",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = "Post-Dinner Leisure Walk", TargetMuscle = "Insulin Sensitivity", Sets = 1, Reps = "20 mins", RestSeconds = 0, FormCue = "Relaxed nose breathing, steady cadence" }
                    }
                });

                return plan;
            }

            // Gym or HomeBodyweight
            string categoryKey = prefKey == "homebodyweight" ? "homebodyweight" : "gym";
            if (_workoutData.TryGetValue(categoryKey, out var workoutObj))
            {
                plan.ProgramTitle = workoutObj.TryGetProperty("Title", out var t) ? t.GetString() ?? "" : "ValensFit Training Protocol";
                plan.Rationale = workoutObj.TryGetProperty("Rationale", out var r) ? r.GetString() ?? "" : "";
                
                string daysPropName = input.DaysPerWeek >= 4 && workoutObj.TryGetProperty("Days4", out _) ? "Days4" : "Days3";
                if (workoutObj.TryGetProperty(daysPropName, out var daysArr))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var daysList = JsonSerializer.Deserialize<List<WorkoutDay>>(daysArr.GetRawText(), options);
                    if (daysList != null)
                    {
                        // Scale reps / sets based on user experience & time
                        foreach (var day in daysList)
                        {
                            day.EstimatedMinutes = input.MinutesPerSession;
                            foreach (var ex in day.Exercises)
                            {
                                if (input.ExperienceLevel.Equals("Beginner", StringComparison.OrdinalIgnoreCase))
                                {
                                    ex.Sets = Math.Max(2, ex.Sets - 1);
                                }
                                else if (input.ExperienceLevel.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
                                {
                                    ex.Sets = ex.Sets + 1;
                                }
                            }
                        }
                        plan.Schedule = daysList;
                    }
                }
            }

            plan.Principles.Add("Apply Progressive Overload: strive to add 1 rep or small weight increase each week");
            plan.Principles.Add("Maintain strict eccentric control (2-3 seconds on lowering phase)");
            plan.Principles.Add($"Consume 25-35g protein within 2 hours post-workout");

            return plan;
        }
    }
}
