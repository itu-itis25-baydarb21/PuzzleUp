using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Match3Engine.AI;
using Match3Engine.Core;

namespace Match3Engine.EditorTools
{
    // Puzzle Up > Run Bot Batch: plays a level many times with every kind of bot
    // and prints how each one did to the Console.
    public static class BotBatchMenu
    {
        private const int gamesPerBot = 1000;

        [MenuItem("Puzzle Up/Run Bot Batch")]
        private static void RunBotBatch()
        {
            LevelData level = FindLevel();
            if (level == null)
            {
                UnityEngine.Debug.LogWarning("Run Bot Batch: select a Level asset, or open a scene whose GameController has a level.");
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("Bot batch on '" + level.name + "' - " + gamesPerBot + " games per bot");
            report.AppendLine(level.width + "x" + level.height + ", " + level.availableTileTypes.Length + " colours, " + level.moveLimit + " moves");
            report.AppendLine();
            report.AppendLine(Row("Bot", "Pass rate", "Moves left", "Progress", "Time"));
            report.AppendLine(Row("", "", "(when won)", "(when lost)", ""));

            try
            {
                // Each strategy at full strength
                BotStrategy[] strategies = { BotStrategy.Random, BotStrategy.Greedy, BotStrategy.GoalAware, BotStrategy.LookAhead };
                float[] skills = { 0.25f, 0.5f, 0.75f };
                int rows = strategies.Length + skills.Length;
                int done = 0;

                foreach (BotStrategy strategy in strategies)
                {
                    ShowProgress(strategy.ToString(), done++, rows);
                    report.AppendLine(RunRow(level, strategy.ToString(), seed => BotFactory.Create(strategy, seed)));
                }

                // Then one strategy at lower skill, to see how much careless moves cost
                foreach (float skill in skills)
                {
                    string label = "GoalAware, skill " + skill.ToString("0.00", CultureInfo.InvariantCulture);
                    ShowProgress(label, done++, rows);
                    report.AppendLine(RunRow(level, label, seed => BotFactory.Create(BotStrategy.GoalAware, skill, seed)));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            UnityEngine.Debug.Log(report.ToString());
        }

        // The selected Level asset if there is one, otherwise the level the open scene plays
        private static LevelData FindLevel()
        {
            LevelData selected = Selection.activeObject as LevelData;
            if (selected != null) return selected;

            GameController controller = Object.FindFirstObjectByType<GameController>();
            return controller != null ? controller.level : null;
        }

        private static string RunRow(LevelData level, string label, System.Func<int, IBot> createBot)
        {
            Stopwatch timer = Stopwatch.StartNew();
            BatchResult result = BatchRunner.Run(level, createBot, gamesPerBot);
            timer.Stop();

            return Row(
                label,
                (result.PassRate * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%",
                result.AverageMovesLeft.ToString("0.0", CultureInfo.InvariantCulture),
                (result.AverageProgressWhenLost * 100f).ToString("0", CultureInfo.InvariantCulture) + "%",
                (timer.ElapsedMilliseconds / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + "s");
        }

        private static string Row(string bot, string passRate, string movesLeft, string progress, string time)
        {
            return bot.PadRight(24) + passRate.PadLeft(10) + movesLeft.PadLeft(13) + progress.PadLeft(13) + time.PadLeft(8);
        }

        private static void ShowProgress(string label, int done, int total)
        {
            EditorUtility.DisplayProgressBar("Run Bot Batch", label, (float)done / total);
        }
    }
}
