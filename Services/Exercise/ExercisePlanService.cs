using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
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
            double weightKg = input.GetWeightInKg();
            int sessionMins = input.MinutesPerSession > 0 ? input.MinutesPerSession : 45;
            int daysPerWeek = input.DaysPerWeek > 0 ? input.DaysPerWeek : 4;
            int stepTarget = input.DailyStepsTarget > 0 ? input.DailyStepsTarget : 8000;

            var plan = new ExercisePlanModel
            {
                Preference = input.ExercisePreference,
                DaysPerWeek = daysPerWeek,
                MinutesPerSession = sessionMins,
                DailyStepTarget = stepTarget
            };

            // Calculate MET-based daily step energy burn (~0.04 kcal per step for 70kg baseline)
            plan.DailyStepsCalorieBurn = (int)Math.Round(stepTarget * 0.04 * (weightKg / 70.0));

            string prefKey = input.ExercisePreference?.ToLowerInvariant() ?? "gym";

            if (prefKey == "noexercise")
            {
                plan.ProgramTitle = "Nutritional Discipline & Metabolic Vitality Walking";
                plan.Rationale = "Your primary focus in this phase is strict nutritional precision. No strenuous resistance workouts are scheduled, preserving energy while maintaining metabolic circulation through daily baseline steps.";
                plan.WalkingRationale = $"Aim for {plan.DailyStepTarget:N0} daily steps to sustain insulin sensitivity, joint mobility, and non-exercise thermogenesis (NEAT).";
                plan.AvgCaloriesBurnedPerSession = 0;
                plan.WeeklyTotalCalorieBurn = plan.DailyStepsCalorieBurn * 7;

                plan.Principles.Add($"Target {plan.DailyStepTarget:N0} daily steps (~{plan.DailyStepsCalorieBurn} kcal/day burn)");
                plan.Principles.Add("Take a 10-15 minute walk after your largest carbohydrate meal");
                plan.Principles.Add("Prioritize 7-8 hours of restful sleep for cellular recovery");

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 1,
                    DayTitle = "Daily Baseline Activity",
                    Focus = "Metabolic Health & Digestion",
                    IsRestDay = true,
                    EstimatedMinutes = 20,
                    EstimatedCaloriesBurned = (int)(3.0 * weightKg * (20.0 / 60.0)),
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
                double walkingMet = 3.8;
                int walkingSessionBurn = (int)Math.Round(walkingMet * weightKg * (sessionMins / 60.0));

                plan.ProgramTitle = $"Structured Walking Protocol ({plan.DailyStepTarget:N0} Steps/Day)";
                plan.Rationale = $"Low-impact aerobic conditioning programmed for {sessionMins} mins/day across {daysPerWeek} days/week. Maximizes fat oxidation with near-zero joint stress.";
                plan.WalkingRationale = $"Hitting {plan.DailyStepTarget:N0} steps daily provides ~{plan.DailyStepsCalorieBurn} kcal daily expenditure, accelerating fat loss.";
                plan.AvgCaloriesBurnedPerSession = walkingSessionBurn;
                plan.WeeklyTotalCalorieBurn = (walkingSessionBurn * daysPerWeek) + (plan.DailyStepsCalorieBurn * 7);

                plan.Principles.Add($"Daily target: {plan.DailyStepTarget:N0} steps (brisk pace ~100-115 steps/min)");
                plan.Principles.Add($"Each structured session burns ~{walkingSessionBurn} kcal");
                plan.Principles.Add("Divide walking volume into morning pre-work and evening post-dinner sessions");

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 1,
                    DayTitle = "Session A: Morning Metabolic Stride",
                    Focus = "Fat Oxidation & Wakefulness",
                    IsRestDay = false,
                    EstimatedMinutes = sessionMins,
                    EstimatedCaloriesBurned = walkingSessionBurn,
                    Warmup = "2 min ankle rolls and calf raises",
                    Cooldown = "2 min standing hamstring and calf stretch",
                    Exercises = new List<ExerciseItem>
                    {
                        new() { Name = "Brisk Outdoor / Treadmill Walk", TargetMuscle = "Aerobic System / Lower Body", Sets = 1, Reps = $"{sessionMins} mins", RestSeconds = 0, FormCue = "Maintain upright posture, chest open, steady rhythmic arm swing" }
                    }
                });

                plan.Schedule.Add(new WorkoutDay
                {
                    DayNumber = 2,
                    DayTitle = "Session B: Post-Dinner Digestive Stride",
                    Focus = "Glucose Clearance & Digestion",
                    IsRestDay = false,
                    EstimatedMinutes = 20,
                    EstimatedCaloriesBurned = (int)Math.Round(3.5 * weightKg * (20.0 / 60.0)),
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
            double workoutMet = prefKey == "homebodyweight" ? 4.8 : 5.8;
            int estimatedSessionBurn = (int)Math.Round(workoutMet * weightKg * (sessionMins / 60.0));
            plan.AvgCaloriesBurnedPerSession = estimatedSessionBurn;
            plan.WeeklyTotalCalorieBurn = (estimatedSessionBurn * daysPerWeek) + (plan.DailyStepsCalorieBurn * 7);

            string categoryKey = prefKey == "homebodyweight" ? "homebodyweight" : "gym";
            if (_workoutData.TryGetValue(categoryKey, out var workoutObj))
            {
                plan.ProgramTitle = workoutObj.TryGetProperty("Title", out var t) ? t.GetString() ?? "" : "ValensFit Training Protocol";
                plan.Rationale = workoutObj.TryGetProperty("Rationale", out var r) ? r.GetString() ?? "" : "";
                
                string daysPropName = daysPerWeek >= 4 && workoutObj.TryGetProperty("Days4", out _) ? "Days4" : "Days3";
                if (workoutObj.TryGetProperty(daysPropName, out var daysArr))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var daysList = JsonSerializer.Deserialize<List<WorkoutDay>>(daysArr.GetRawText(), options);
                    if (daysList != null)
                    {
                        foreach (var day in daysList)
                        {
                            day.EstimatedMinutes = sessionMins;
                            day.EstimatedCaloriesBurned = day.IsRestDay ? 0 : estimatedSessionBurn;
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

            plan.Principles.Add($"Each {sessionMins}-min session expends ~{estimatedSessionBurn} kcal based on your {weightKg:F1}kg bodyweight");
            plan.Principles.Add("Apply Progressive Overload: strive to add 1 rep or slight resistance each week");
            plan.Principles.Add("Maintain strict eccentric tempo (2-3 seconds on lowering phase)");
            plan.Principles.Add("Consume 25-35g protein within 2 hours post-workout to support muscle protein synthesis");

            return plan;
        }
    }
}
