using System.ComponentModel.DataAnnotations;

namespace ValensFit.Models
{
    public class UserInputModel
    {
        [Required(ErrorMessage = "Please enter your first name.")]
        [StringLength(30, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 30 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Name must contain letters only.")]
        public string FirstName { get; set; } = "Athlete";

        [Required]
        public string Gender { get; set; } = "Male"; // Male, Female, Other

        [Range(13, 80, ErrorMessage = "Age must be between 13 and 80.")]
        public int Age { get; set; } = 25;

        public double Height { get; set; } = 175; // in cm or ft
        public string HeightUnit { get; set; } = "cm"; // "cm" or "ft"
        public double HeightInches { get; set; } = 0; // if ft, e.g. 5 ft 9 in

        public double Weight { get; set; } = 70; // in kg or lbs
        public string WeightUnit { get; set; } = "kg"; // "kg" or "lb"

        public string ActivityLevel { get; set; } = "ModeratelyActive"; 
        // Sedentary, LightlyActive, ModeratelyActive, VeryActive, ExtraActive

        public int DailyStepsTarget { get; set; } = 8000;

        public string Goal { get; set; } = "LoseFat"; 
        // LoseFat, Maintain, BuildMuscle

        public double? TargetWeightLossKg { get; set; }
        public int? TimeframeWeeks { get; set; }

        public string Country { get; set; } = "Bangladesh";
        public string? CityRegion { get; set; } = "Dhaka";

        public decimal? MonthlyBudget { get; set; } = 8000;
        public string Currency { get; set; } = "BDT";

        public List<string> DietPreferences { get; set; } = new();
        // Tags: "Vegetarian", "Vegan", "No beef", "No pork", "No fish", "Egg + Chicken Only", "Halal", "Lactose Free", "Gluten Free", "Nut Free"

        public string? CustomRestrictions { get; set; }

        public bool OfficeLunch { get; set; } = false; // Eaten outside / simplified lunch

        public string ExercisePreference { get; set; } = "Gym"; 
        // Gym, HomeBodyweight, WalkingOnly, NoExercise

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
                // Height is feet, HeightInches is inches
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
