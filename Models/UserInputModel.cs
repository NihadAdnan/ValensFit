using System.ComponentModel.DataAnnotations;

namespace ValensFit.Models
{
    public class UserInputModel
    {
        [Required(ErrorMessage = "Please enter your first name.")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 30 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Name must contain letters only.")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = "Male"; // Male, Female

        [Range(13, 80, ErrorMessage = "Age must be between 13 and 80.")]
        public int Age { get; set; } = 25;

        public double Height { get; set; } = 175; // in cm or ft
        public string HeightUnit { get; set; } = "cm"; // "cm" or "ft"
        public double HeightInches { get; set; } = 0; // if ft, e.g. 5 ft 9 in

        public double Weight { get; set; } = 70; // in kg or lbs
        public string WeightUnit { get; set; } = "kg"; // "kg" or "lb"

        public string ActivityLevel { get; set; } = "ModeratelyActive"; 
        // Sedentary, LightlyActive, ModeratelyActive, VeryActive, ExtraActive

        public string Goal { get; set; } = "LoseFat"; 
        // LoseFat, Maintain, BuildMuscle

        public bool MaximizeMuscleRetention { get; set; } = true; // Higher protein (2.2-2.4 g/kg)

        public double? TargetWeightLossKg { get; set; }
        public int? TimeframeWeeks { get; set; }

        public string Country { get; set; } = "Bangladesh";
        public string? CityRegion { get; set; } = "Dhaka";

        public decimal? MonthlyBudget { get; set; } = 7000;
        public string Currency { get; set; } = "BDT";

        public List<string> DietPreferences { get; set; } = new();
        // Tags: "Halal", "Egg + Chicken Only", "Vegetarian", "Vegan", "No beef", "No pork", "No fish", "Lactose Free", "Gluten Free", "Nut Free"

        public string? CustomRestrictions { get; set; }

        // Meal Structure & Preferences
        public string MealStructure { get; set; } = "Standard"; 
        // "Standard" (30/35/35), "LightBreakfast" (e.g. 2 eggs + tea, 18/41/41), "TwoMeals" (Lunch + Dinner)

        public bool OfficeLunch { get; set; } = false;
        public string? OfficeLunchDescription { get; set; } // What user actually eats outside (e.g. "Rice + Chicken curry, 1 piece")

        // Exercise Preferences
        public string ExercisePreference { get; set; } = "Gym"; 
        // Gym, HomeBodyweight, WalkingOnly, NoExercise

        public int DailyStepsTarget { get; set; } = 10000; // Step target for walking / general NEAT

        public string HomeEquipment { get; set; } = "None"; // "None", "PullUpBar", "Bands", "Dumbbells"

        [Range(15, 120)]
        public int MinutesPerSession { get; set; } = 45;

        [Range(0, 7)]
        public int DaysPerWeek { get; set; } = 4;

        public string ExperienceLevel { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced

        // Helper calculations
        public double GetHeightInCm()
        {
            if (HeightUnit.Equals("ft", StringComparison.OrdinalIgnoreCase))
            {
                double totalInches = (Height * 12.0) + HeightInches;
                return Math.Round(totalInches * 2.54, 1);
            }
            return Math.Round(Height, 1);
        }

        public double GetWeightInKg()
        {
            if (WeightUnit.Equals("lb", StringComparison.OrdinalIgnoreCase) || WeightUnit.Equals("lbs", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Round(Weight * 0.45359237, 1);
            }
            return Math.Round(Weight, 1);
        }
    }
}
