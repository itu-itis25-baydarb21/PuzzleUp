using UnityEngine;

namespace Match3Engine.Studio
{
    // The rules of running the studio. These are meant to be tuned.
    [CreateAssetMenu(fileName = "Studio", menuName = "Puzzle Up/Studio Settings")]
    public class StudioSettings : ScriptableObject
    {
        [Header("Money")]
        [Min(0f)] public float startingMoney = 30f;

        [Tooltip("Paid every time a level is tested in the designer.")]
        [Min(0f)] public float testCost = 3f;

        [Tooltip("Paid to add a level to the game.")]
        [Min(0f)] public float publishCost = 10f;

        [Tooltip("Earned each time any player starts any level, on top of what players buy.")]
        [Min(0f)] public float adIncomePerLevelPlayed = 0.02f;

        [Header("Players")]
        [Tooltip("How many new players arrive in the first month.")]
        [Min(1)] public int startingCohort = 200;
        [Min(1)] public int minCohort = 50;
        [Min(1)] public int maxCohort = 600;

        [Tooltip("Next month's arrivals are this month's times a factor between these two, set by how many players stayed to the end.")]
        [Min(0f)] public float growthWhenEveryoneQuits = 0.6f;
        [Min(0f)] public float growthWhenEveryoneStays = 1.4f;

        [Header("Game")]
        [Min(1)] public int maxLevels = 15;

        // Word of mouth: a game that keeps its players brings in more of them next month
        public int NextCohort(int current, float retention)
        {
            float growth = Mathf.Lerp(growthWhenEveryoneQuits, growthWhenEveryoneStays, Mathf.Clamp01(retention));
            return Mathf.Clamp(Mathf.RoundToInt(current * growth), minCohort, maxCohort);
        }
    }
}
