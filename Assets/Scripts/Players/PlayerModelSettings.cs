using UnityEngine;
using Match3Engine.AI;

namespace Match3Engine.Players
{
    public enum SpenderType
    {
        Never,
        Sometimes,
        Often
    }

    // One simulated player
    [System.Serializable]
    public struct PlayerProfile
    {
        // From 0 to 1: how often they pick a good move instead of a random one
        public float skill;

        // How much frustration they can take before they leave the game
        public float patience;

        public SpenderType spender;
    }

    // Every number that decides how simulated players behave. These are meant to be tuned.
    [CreateAssetMenu(fileName = "PlayerModel", menuName = "Puzzle Up/Player Model Settings")]
    public class PlayerModelSettings : ScriptableObject
    {
        [Header("Population")]
        [Min(1)] public int populationSize = 300;

        [Tooltip("Skill of a typical player, from 0 to 1.")]
        [Range(0f, 1f)] public float skillAverage = 0.5f;

        [Tooltip("How far skill usually strays from the average. About two thirds of players fall within this distance.")]
        [Range(0f, 0.5f)] public float skillSpread = 0.2f;

        [Tooltip("Patience is picked evenly between these two.")]
        public float patienceMin = 2f;
        public float patienceMax = 5f;

        [Tooltip("Share of players who sometimes pay. The rest, after 'often', never pay.")]
        [Range(0f, 1f)] public float shareSometimes = 0.25f;

        [Tooltip("Share of players who often pay.")]
        [Range(0f, 1f)] public float shareOften = 0.1f;

        [Header("Playing")]
        [Tooltip("What players do on the turns they are paying attention.")]
        public BotStrategy strategy = BotStrategy.GoalAware;

        [Tooltip("A player who has not passed or quit by now is counted as having quit.")]
        [Min(1)] public int maxAttempts = 30;

        [Header("Buying extra moves")]
        [Tooltip("A lost game with at least this much of the goal done is a near miss, and may be bought out.")]
        [Range(0f, 1f)] public float nearMissProgress = 0.85f;

        [Min(1)] public int extraMoves = 5;
        [Min(0f)] public float extraMovesPrice = 1f;

        [Tooltip("Chance that each kind of spender buys extra moves on a near miss.")]
        [Range(0f, 1f)] public float buyChanceSometimes = 0.3f;
        [Range(0f, 1f)] public float buyChanceOften = 0.8f;

        [Tooltip("How many times extra moves can be bought in a single attempt.")]
        [Min(0)] public int maxPurchasesPerAttempt = 1;

        [Header("Frustration")]
        [Tooltip("Added for every failed attempt, however close it was.")]
        [Min(0f)] public float frustrationPerFail = 0.5f;

        [Tooltip("Added on top, scaled by how much of the goal was left. A loss with nothing done adds all of it.")]
        [Min(0f)] public float frustrationForBadLoss = 1f;

        public float BuyChance(SpenderType spender)
        {
            switch (spender)
            {
                case SpenderType.Sometimes: return buyChanceSometimes;
                case SpenderType.Often: return buyChanceOften;
                default: return 0f;
            }
        }

        public float FrustrationForFail(float goalProgress)
        {
            return frustrationPerFail + frustrationForBadLoss * (1f - goalProgress);
        }
    }
}
