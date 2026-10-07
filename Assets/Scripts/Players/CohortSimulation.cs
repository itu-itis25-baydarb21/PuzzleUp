using System.Collections.Generic;
using Match3Engine.Core;

namespace Match3Engine.Players
{
    // What happened when a group of new players went through a whole game, level after level
    public class CohortReport
    {
        // Players who started level 1, and those who got through every level
        public int started;
        public int finished;

        // One report per level, in order. Each level only sees the players who survived the ones before.
        public List<LevelReport> levels = new List<LevelReport>();

        // Every extra-moves purchase across all levels
        public float Revenue
        {
            get
            {
                float total = 0f;
                foreach (LevelReport level in levels) total += level.revenue;
                return total;
            }
        }

        // How many level starts there were in total: one for every player on every level they reached
        public int LevelsPlayed
        {
            get
            {
                int total = 0;
                foreach (LevelReport level in levels) total += level.players;
                return total;
            }
        }

        // From 0 to 1: the share of players still in the game at the end
        public float Retention => started > 0 ? (float)finished / started : 0f;
    }

    // Sends a group of new players through a list of levels in order. A player who quits on one
    // level never sees the next, and frustration from a hard level follows them into the one after.
    public class CohortSimulation
    {
        public CohortReport Report { get; private set; } = new CohortReport();

        public int PlayerCount => population.Count;
        public int PlayersDone { get; private set; }
        public bool IsDone => PlayersDone >= population.Count;

        private IList<LevelData> levels;
        private PlayerModelSettings settings;
        private List<PlayerProfile> population;
        private int seed;

        public CohortSimulation(IList<LevelData> gameLevels, PlayerModelSettings modelSettings, int cohortSize, int cohortSeed)
        {
            levels = gameLevels;
            settings = modelSettings;
            seed = cohortSeed;
            population = PlayerPopulation.Generate(modelSettings, cohortSize, cohortSeed);

            for (int i = 0; i < levels.Count; i++) Report.levels.Add(new LevelReport());
        }

        // Takes one player through the game. Call until IsDone; this is what lets a long run
        // be spread over many frames.
        public void SimulateNextPlayer()
        {
            if (IsDone) return;

            PlayerProfile player = population[PlayersDone];
            float frustration = 0f;
            bool stillPlaying = true;

            Report.started++;

            for (int i = 0; i < levels.Count && stillPlaying; i++)
            {
                // Seeds are spaced so no two players, and no two levels, ever share a board
                int playerSeed = seed + PlayersDone * 10000 + i * 100;

                stillPlaying = LevelAnalyzer.SimulatePlayer(levels[i], player, settings, playerSeed, Report.levels[i], ref frustration);
            }

            if (stillPlaying) Report.finished++;

            PlayersDone++;
        }

        // The whole run in one call
        public static CohortReport Run(IList<LevelData> gameLevels, PlayerModelSettings modelSettings, int cohortSize, int cohortSeed)
        {
            CohortSimulation simulation = new CohortSimulation(gameLevels, modelSettings, cohortSize, cohortSeed);
            while (!simulation.IsDone) simulation.SimulateNextPlayer();
            return simulation.Report;
        }
    }
}
