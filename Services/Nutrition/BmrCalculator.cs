namespace ValensFit.Services.Nutrition
{
    public class BmrCalculator
    {
        /// <summary>
        /// Calculates Basal Metabolic Rate using the industry-standard Mifflin-St Jeor equation.
        /// </summary>
        public double CalculateBmr(string gender, double weightKg, double heightCm, int age)
        {
            // Clamp input boundaries for safety
            weightKg = Math.Clamp(weightKg, 30.0, 250.0);
            heightCm = Math.Clamp(heightCm, 120.0, 230.0);
            age = Math.Clamp(age, 13, 80);

            double baseMath = (10.0 * weightKg) + (6.25 * heightCm) - (5.0 * age);

            if (gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Round(baseMath + 5.0, 0);
            }
            else if (gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Round(baseMath - 161.0, 0);
            }
            else
            {
                // Average of Male (+5) and Female (-161) = -78
                return Math.Round(baseMath - 78.0, 0);
            }
        }
    }
}
