using System.Collections.Generic;
using UnityEngine;
using Match3Engine.AI;
using Match3Engine.Core;
using Match3Engine.Simulation;

namespace Match3Engine.Players
{
    // What happened when a whole population of players met one level
    public class LevelReport
    {
        public int players;

        // Players who eventually beat the level, and those who left the game on it
        public int passed;
        public int quit;

        public int passedOnFirstAttempt;
        public int attemptsByPassers;

        public int purchases;
        public float revenue;

        public int failedAttempts;
        public int nearMisses;

        // One entry per won attempt, and one per failed attempt
        public List<int> movesLeftOnWin = new List<int>();
        public List<float> progressOnFail = new List<float>();

        // All from 0 to 1
        public float FirstAttemptPassRate => players > 0 ? (float)passedOnFirstAttempt / players : 0f;
        public float PassRate => players > 0 ? (float)passed / players : 0f;
        public float QuitRate => players > 0 ? (float)quit / players : 0f;

        // Share of failed attempts that were close enough to offer extra moves
        public float NearMissShare => failedAttempts > 0 ? (float)nearMisses / failedAttempts : 0f;

        public float AverageAttemptsToPass => passed > 0 ? (float)attemptsByPassers / passed : 0f;
        public float RevenuePer100Players => players > 0 ? revenue * 100f / players : 0f;

        public float AverageMovesLeftOnWin
        {
            get
            {
                if (movesLeftOnWin.Count == 0) return 0f;

                int total = 0;
                foreach (int moves in movesLeftOnWin) total += moves;
                return (float)total / movesLeftOnWin.Count;
            }
        }
    }

    public static class PlayerPopulation
    {
        // The same settings and seed always produce the same crowd
        public static List<PlayerProfile> Generate(PlayerModelSettings settings, int seed)
        {
            System.Random random = new System.Random(seed);
            List<PlayerProfile> players = new List<PlayerProfile>(settings.populationSize);

            for (int i = 0; i < settings.populationSize; i++)
            {
                // Three dice added together bunch up around the middle, like real ability does
                float bell = ((float)(random.NextDouble() + random.NextDouble() + random.NextDouble()) - 1.5f) / 0.5f;

                PlayerProfile player = new PlayerProfile();
                player.skill = Mathf.Clamp01(settings.skillAverage + settings.skillSpread * bell);
                player.patience = Mathf.Lerp(settings.patienceMin, settings.patienceMax, (float)random.NextDouble());

                double spenderRoll = random.NextDouble();
                if (spenderRoll < settings.shareOften) player.spender = SpenderType.Often;
                else if (spenderRoll < settings.shareOften + settings.shareSometimes) player.spender = SpenderType.Sometimes;
                else player.spender = SpenderType.Never;

                players.Add(player);
            }

            return players;
        }
    }

    // Turns "a bot won or lost" into money and quits: sends every player in a population
    // at a level and lets them retry, pay or give up according to the settings.
    public static class LevelAnalyzer
    {
        public static LevelReport Run(LevelData level, PlayerModelSettings settings, int seed = 1)
        {
            return Run(level, settings, PlayerPopulation.Generate(settings, seed), seed);
        }

        public static LevelReport Run(LevelData level, PlayerModelSettings settings, List<PlayerProfile> population, int seed = 1)
        {
            LevelReport report = new LevelReport();

            for (int i = 0; i < population.Count; i++)
            {
                // Seeds are spaced out so one player's attempts never reuse another player's boards
                SimulatePlayer(level, population[i], settings, seed + i * 1000, report);
            }

            return report;
        }

        // One player keeps trying the level until they pass it or lose patience
        public static void SimulatePlayer(LevelData level, PlayerProfile player, PlayerModelSettings settings, int playerSeed, LevelReport report)
        {
            System.Random decisions = new System.Random(playerSeed);
            float frustration = 0f;

            report.players++;

            for (int attempt = 1; attempt <= settings.maxAttempts; attempt++)
            {
                int attemptSeed = playerSeed + attempt;

                GameSimulation game = new GameSimulation(level, attemptSeed);
                IBot bot = BotFactory.Create(settings.strategy, player.skill, attemptSeed);

                BatchRunner.PlayToEnd(game, bot);

                // So close... a near miss is the one moment a player may pay to keep going
                int purchasesThisAttempt = 0;
                while (game.Progress.State == LevelState.Lost
                    && game.Progress.GoalProgress >= settings.nearMissProgress
                    && purchasesThisAttempt < settings.maxPurchasesPerAttempt
                    && decisions.NextDouble() < settings.BuyChance(player.spender))
                {
                    purchasesThisAttempt++;
                    report.purchases++;
                    report.revenue += settings.extraMovesPrice;

                    game.AddMoves(settings.extraMoves);
                    BatchRunner.PlayToEnd(game, bot);
                }

                if (game.Progress.State == LevelState.Won)
                {
                    report.passed++;
                    report.attemptsByPassers += attempt;
                    if (attempt == 1) report.passedOnFirstAttempt++;
                    report.movesLeftOnWin.Add(game.Progress.MovesLeft);
                    return;
                }

                float progress = game.Progress.GoalProgress;

                report.failedAttempts++;
                report.progressOnFail.Add(progress);
                if (progress >= settings.nearMissProgress) report.nearMisses++;

                // A bad loss hurts more than a close one
                frustration += settings.FrustrationForFail(progress);

                if (frustration > player.patience)
                {
                    report.quit++;
                    return;
                }
            }

            // Still stuck after every allowed attempt: nobody keeps going forever
            report.quit++;
        }
    }
}
