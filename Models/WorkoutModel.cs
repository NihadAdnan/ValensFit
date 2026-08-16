using System.Collections.Generic;

namespace ValensFit.Models
{
    public class ExerciseItem
    {
        public string Name { get; set; } = string.Empty;
        public string TargetMuscle { get; set; } = string.Empty;
        public int Sets { get; set; } = 3;
        public string Reps { get; set; } = "8-12";
        public int RestSeconds { get; set; } = 90;
        public string FormCue { get; set; } = string.Empty;
        public string? Regression { get; set; } // Beginner alternative
        public string? Progression { get; set; } // Advanced progression
        public string Category { get; set; } = "Compound"; // Compound, Isolation, Bodyweight, Mobility
    }

    public class WorkoutDay
    {
        public int DayNumber { get; set; }
        public string DayTitle { get; set; } = string.Empty; // e.g. "Day 1: Upper Body Strength" or "Active Recovery & Mobility"
        public string Focus { get; set; } = "Full Body";
        public bool IsRestDay { get; set; } = false;
        public int EstimatedMinutes { get; set; } = 45;
        public int EstimatedCaloriesBurned { get; set; } = 280; // Calculated via MET * Weight * Duration
        public string Warmup { get; set; } = "5 min dynamic stretching, arm circles & light bodyweight squats";
        public List<ExerciseItem> Exercises { get; set; } = new();
        public string Cooldown { get; set; } = "5 min deep breathing & static stretching";
    }

    public class ExercisePlanModel
    {
        public string Preference { get; set; } = "Gym"; // Gym, HomeBodyweight, WalkingOnly, NoExercise
        public string ProgramTitle { get; set; } = "Hypertrophy 4-Day Strength Protocol";
        public string Rationale { get; set; } = string.Empty;
        public int DaysPerWeek { get; set; } = 4;
        public int MinutesPerSession { get; set; } = 45;
        public int DailyStepTarget { get; set; } = 8000;
        public string WalkingRationale { get; set; } = string.Empty;
        
        // Sports science calorie burn analytics
        public int AvgCaloriesBurnedPerSession { get; set; } = 320;
        public int WeeklyTotalCalorieBurn { get; set; } = 1280;
        public int DailyStepsCalorieBurn { get; set; } = 320; // ~40 kcal per 1,000 steps
        
        public List<WorkoutDay> Schedule { get; set; } = new();
        public List<string> Principles { get; set; } = new();
    }
}
