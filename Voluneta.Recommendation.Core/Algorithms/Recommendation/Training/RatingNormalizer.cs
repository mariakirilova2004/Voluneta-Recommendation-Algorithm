namespace Voluneta.Recommendation.Core.Algorithms.Recommendation.Training
{
    public static class RatingNormalizer
    {
        public static double Normalize(int rating)
        {
            if (rating < 1 || rating > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 10.");
            }

            return (rating - 1.0) / 9.0;
        }

        public static double Denormalize(double value)
        {
            if (!double.IsFinite(value) || value < 0 || value > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 1.");
            }

            return 1 + value * 9;
        }
    }
}
