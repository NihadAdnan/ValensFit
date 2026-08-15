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
                DailyStepTarget = input.DailyStepsTarget > 0 ? input.DailyStepsTarget : 10000
            };

            string prefKey = input.ExercisePreference?.ToLowerInvariant() ?? "gym";

            if (prefKey == "noexercise")
            {
                plan.ProgramTitle = "Nutritional Focus & Daily Vitality Walking";
                plan.Rationale = "Your primary focus in this phase is strict nutritional precision. No strenuous resistance workouts are scheduled, preserving energy while maintaining metabolic circulation through daily baseline steps.";
                plan.WalkingRationale = $"Aim for {plan.DailyStepTarget:N0} daily steps to sustain insulin sensitivity, joint mobility, and non-exercise thermogenesis (NEAT).";
                plan.Principles.Add($"Target {plan.DailyStepTarget:N0} daily steps split throughout the day");
                plan.Principles.Add("Take a 10-15 minute walk after your largest carbohydrate meal");
                plan.Principles.Add("Prioritize 7-8 hours of restful sleep for cellular recovery");

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 1,
                    DayTitle = "Daily Baseline Activity",
                    Focus = "Metabolic Health & Digestion",
                    IsRestDay = true,
                    EstimatedMinutes = 20,
                    Warmup = "Gentle joint mobility & neck/shoulder rolls",
                    Cooldown = "Adequate water hydration",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = $"Daily Walking Target ({plan.DailyStepTarget:N0} steps)", TargetMuscle = "Cardiovascular & Metabolic System", Sets = 1, Reps = "Throughout day", RestSeconds = 0, FormCue = "Take stairs, walk while on phone calls, maintain upright posture" }
                    }
                });
                return plan;
            }

            if (prefKey == "walkingonly")
            {
                plan.ProgramTitle = $"Structured Walking Protocol ({plan.DailyStepTarget:N0} Steps/Day)";
                plan.Rationale = $"Low-impact aerobic conditioning programmed for {input.MinutesPerSession} minutes/day across {input.DaysPerWeek} days/week. Maximizes fat oxidation while causing near-zero joint wear.";
                plan.WalkingRationale = $"Hitting {plan.DailyStepTarget:N0} steps daily provides steady energy expenditure and accelerates fat loss.";
                plan.Principles.Add($"Daily target: {plan.DailyStepTarget:N0} steps (brisk pace ~100-115 steps/min)");
                plan.Principles.Add("Divide walking volume into morning pre-work and evening post-dinner sessions");
                plan.Principles.Add("Track weekly consistency to ensure positive metabolic momentum");

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 1,
                    DayTitle = "Session A: Morning Metabolic Stride",
                    Focus = "Fat Oxidation & Wakefulness",
                    IsRestDay = false,
                    EstimatedMinutes = Math.Min(input.MinutesPerSession, 45),
                    Warmup = "2 min ankle rolls and calf raises",
                    Cooldown = "2 min standing hamstring and calf stretch",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = "Brisk Outdoor / Treadmill Walk", TargetMuscle = "Aerobic System / Legs", Sets = 1, Reps = $"{Math.Min(input.MinutesPerSession, 30)} mins", RestSeconds = 0, FormCue = "Maintain upright posture, chest open, steady rhythmic arm swing" }
                    }
                });

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 2,
                    DayTitle = "Session B: Post-Dinner Digestive Stride",
                    Focus = "Glucose Clearance & Digestion",
                    IsRestDay = false,
                    EstimatedMinutes = 20,
                    Warmup = "Light continuous walking",
                    Cooldown = "Deep diaphragmatic breathing",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = "Evening Leisure Stride", TargetMuscle = "Insulin Sensitivity", Sets = 1, Reps = "20 mins", RestSeconds = 0, FormCue = "Relaxed nose breathing, steady cadence" }
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

            plan.Principles.Add("Apply Progressive Overload: strive to add 1 rep or slight resistance each week");
            plan.Principles.Add("Maintain strict eccentric tempo (2-3 seconds on lowering phase)");
            plan.Principles.Add($"Consume 25-35g protein within 2 hours post-workout to support muscle recovery");

            return plan;
        }
    }
}
