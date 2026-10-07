using System.Collections.Generic;
using Match3Engine.Players;

namespace Match3Engine.Studio
{
    // How one published level did last month
    [System.Serializable]
    public class LevelStats
    {
        public int players;
        public int passed;
        public int quit;
        public float revenue;
    }

    // Everything about the studio that is saved between sessions. Plain data plus its rules.
    [System.Serializable]
    public class StudioState
    {
        public float money;
        public int month = 1;

        // New players arriving this month
        public int cohortSize;

        // The published levels in order, each stored as JSON
        public List<string> levels = new List<string>();

        // Last month's result. Levels published since then have no entry yet.
        public bool hasRun;
        public List<LevelStats> lastStats = new List<LevelStats>();
        public int lastStarted;
        public int lastFinished;
        public float lastSales;
        public float lastAdIncome;

        public static StudioState CreateNew(StudioSettings rules)
        {
            StudioState state = new StudioState();
            state.money = rules.startingMoney;
            state.cohortSize = rules.startingCohort;
            return state;
        }

        public bool TrySpend(float cost)
        {
            if (money < cost) return false;

            money -= cost;
            return true;
        }

        // Why a level cannot be published right now, or null if it can
        public string PublishBlocker(StudioSettings rules)
        {
            if (levels.Count >= rules.maxLevels) return "Your game already has " + rules.maxLevels + " levels.";
            if (money < rules.publishCost) return "Not enough money to publish.";
            return null;
        }

        public bool Publish(string levelJson, StudioSettings rules)
        {
            if (PublishBlocker(rules) != null) return false;

            money -= rules.publishCost;
            levels.Add(levelJson);
            return true;
        }

        // Books a month that has been played: income, last month's numbers, and next month's arrivals
        public void ApplyMonth(CohortReport report, StudioSettings rules)
        {
            lastStats.Clear();
            foreach (LevelReport level in report.levels)
            {
                lastStats.Add(new LevelStats
                {
                    players = level.players,
                    passed = level.passed,
                    quit = level.quit,
                    revenue = level.revenue
                });
            }

            hasRun = true;
            lastStarted = report.started;
            lastFinished = report.finished;
            lastSales = report.Revenue;
            lastAdIncome = report.LevelsPlayed * rules.adIncomePerLevelPlayed;

            money += lastSales + lastAdIncome;
            cohortSize = rules.NextCohort(cohortSize, report.Retention);
            month++;
        }
    }
}
