using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Players;

namespace Match3Engine.EditorTools
{
    // Puzzle Up > Run Player Model: sends a population of simulated players at each level
    // and prints what the level would earn and how many players it would lose.
    public static class PlayerModelMenu
    {
        private const string settingsPath = "Assets/Settings/PlayerModel.asset";

        [MenuItem("Puzzle Up/Run Player Model")]
        private static void RunPlayerModel()
        {
            List<LevelData> levels = FindLevels();
            if (levels.Count == 0)
            {
                Debug.LogWarning("Run Player Model: there are no Level assets in the project.");
                return;
            }

            // Using the project's settings asset if there is one, otherwise the built-in defaults
            PlayerModelSettings settings = AssetDatabase.LoadAssetAtPath<PlayerModelSettings>(settingsPath);
            bool usingDefaults = settings == null;
            if (usingDefaults) settings = ScriptableObject.CreateInstance<PlayerModelSettings>();

            StringBuilder report = new StringBuilder();
            report.AppendLine("Player model - " + settings.populationSize + " players per level"
                + (usingDefaults ? " (default settings; no asset at " + settingsPath + ")" : " (settings from " + settingsPath + ")"));
            report.AppendLine();
            report.AppendLine(Row("Level", "First try", "Attempts", "Revenue", "Quit", "Near miss"));
            report.AppendLine(Row("", "pass", "to pass", "per 100", "", "of fails"));

            try
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Run Player Model", levels[i].name, (float)i / levels.Count);

                    LevelReport result = LevelAnalyzer.Run(levels[i], settings);

                    report.AppendLine(Row(
                        levels[i].name,
                        Percent(result.FirstAttemptPassRate),
                        result.AverageAttemptsToPass.ToString("0.0", CultureInfo.InvariantCulture),
                        result.RevenuePer100Players.ToString("0.0", CultureInfo.InvariantCulture),
                        Percent(result.QuitRate),
                        Percent(result.NearMissShare)));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (usingDefaults) Object.DestroyImmediate(settings);
            }

            Debug.Log(report.ToString());
        }

        // The selected Level assets if any are selected, otherwise every Level in the project
        private static List<LevelData> FindLevels()
        {
            List<LevelData> levels = new List<LevelData>();

            foreach (Object selected in Selection.objects)
            {
                LevelData level = selected as LevelData;
                if (level != null) levels.Add(level);
            }

            if (levels.Count > 0) return levels;

            foreach (string guid in AssetDatabase.FindAssets("t:LevelData"))
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null) levels.Add(level);
            }

            levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return levels;
        }

        private static string Percent(float fraction)
        {
            return (fraction * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        private static string Row(string level, string firstTry, string attempts, string revenue, string quit, string nearMiss)
        {
            return level.PadRight(18) + firstTry.PadLeft(10) + attempts.PadLeft(10) + revenue.PadLeft(10) + quit.PadLeft(7) + nearMiss.PadLeft(11);
        }
    }
}
