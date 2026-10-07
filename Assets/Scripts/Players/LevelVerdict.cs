namespace Match3Engine.Players
{
    public enum LevelRating
    {
        TooEasy,
        Flat,
        SweetSpot,
        Costly,
        TooHard
    }

    // Sums a report up in one line for the designer. The thresholds are a first guess.
    public static class LevelVerdict
    {
        // At or above this share of players quitting, nothing else about the level matters
        public const float tooHardQuitRate = 0.4f;

        // A level that still earns but loses this many players is paying for its revenue
        public const float costlyQuitRate = 0.2f;

        // Revenue per 100 players that counts as the level doing its job
        public const float goodRevenue = 3f;

        // Below this revenue, with nearly everyone passing first try, the level is a free pass
        public const float noRevenue = 1f;
        public const float easyFirstTryRate = 0.8f;

        public static LevelRating Rate(LevelReport report)
        {
            if (report.QuitRate >= tooHardQuitRate) return LevelRating.TooHard;

            if (report.RevenuePer100Players < noRevenue && report.FirstAttemptPassRate >= easyFirstTryRate)
                return LevelRating.TooEasy;

            if (report.QuitRate >= costlyQuitRate) return LevelRating.Costly;
            if (report.RevenuePer100Players >= goodRevenue) return LevelRating.SweetSpot;

            return LevelRating.Flat;
        }

        public static string Describe(LevelRating rating)
        {
            switch (rating)
            {
                case LevelRating.TooEasy: return "Too easy. Players walk through without spending.";
                case LevelRating.SweetSpot: return "Sweet spot. Close calls are selling extra moves.";
                case LevelRating.Costly: return "It earns, but it is costing you a lot of players.";
                case LevelRating.TooHard: return "Too hard. Players are giving up and leaving.";
                default: return "Flat. Few close calls, so little spending.";
            }
        }
    }
}
