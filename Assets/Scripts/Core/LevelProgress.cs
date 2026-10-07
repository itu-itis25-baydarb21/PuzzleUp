using System.Collections.Generic;

namespace Match3Engine.Core
{
    public enum LevelState
    {
        Playing,
        Won,
        Lost
    }

    // Tracks moves and goals for one attempt at a level. Pure data, like my BoardModel.
    public class LevelProgress
    {
        public int MovesLeft { get; private set; }
        public LevelState State { get; private set; } = LevelState.Playing;

        // The goals in the order the level lists them
        public IReadOnlyList<LevelGoal> Goals => goals;

        private List<LevelGoal> goals = new List<LevelGoal>();
        private Dictionary<TileType, int> remaining = new Dictionary<TileType, int>();

        public LevelProgress(int moveLimit, IEnumerable<LevelGoal> levelGoals)
        {
            MovesLeft = moveLimit;

            if (levelGoals == null) return;

            foreach (LevelGoal goal in levelGoals)
            {
                // Two goals for the same colour just add up
                if (remaining.ContainsKey(goal.type))
                {
                    remaining[goal.type] += goal.amount;
                }
                else
                {
                    remaining[goal.type] = goal.amount;
                    goals.Add(goal);
                }
            }
        }

        // How many tiles of this colour are still needed
        public int GetRemaining(TileType type)
        {
            return remaining.TryGetValue(type, out int amount) ? amount : 0;
        }

        public bool AllGoalsComplete
        {
            get
            {
                foreach (int amount in remaining.Values)
                {
                    if (amount > 0) return false;
                }
                return true;
            }
        }

        public void UseMove()
        {
            if (MovesLeft > 0) MovesLeft--;
        }

        // Call once for every tile that gets destroyed
        public void Collect(TileType type)
        {
            if (remaining.TryGetValue(type, out int amount) && amount > 0)
                remaining[type] = amount - 1;
        }

        // Call once the board has settled after a move to decide if the level is over
        public void Evaluate()
        {
            if (State != LevelState.Playing) return;

            if (AllGoalsComplete) State = LevelState.Won;
            else if (MovesLeft <= 0) State = LevelState.Lost;
        }
    }
}
