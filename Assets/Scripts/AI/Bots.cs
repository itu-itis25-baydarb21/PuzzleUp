using System.Collections.Generic;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.AI
{
    // A simulated player. Given the game and the swaps that are allowed, it picks one.
    public interface IBot
    {
        Move ChooseMove(GameSimulation simulation, List<Move> validMoves);
    }

    public enum BotStrategy
    {
        Random,
        Greedy,
        GoalAware,
        LookAhead
    }

    // Plays any allowed swap. This is my floor: a player who is not thinking at all.
    public class RandomBot : IBot
    {
        private System.Random random;

        public RandomBot(int seed)
        {
            random = new System.Random(seed);
        }

        public Move ChooseMove(GameSimulation simulation, List<Move> validMoves)
        {
            return validMoves[random.Next(validMoves.Count)];
        }
    }

    // Shared by the bots that give every move a score and take the highest
    public abstract class ScoringBot : IBot
    {
        protected System.Random random;

        protected ScoringBot(int seed)
        {
            random = new System.Random(seed);
        }

        protected abstract float Score(GameSimulation simulation, Move move);

        public virtual Move ChooseMove(GameSimulation simulation, List<Move> validMoves)
        {
            Move best = validMoves[0];
            float bestScore = float.MinValue;
            int tied = 0;

            foreach (Move move in validMoves)
            {
                float score = Score(simulation, move);

                if (score > bestScore)
                {
                    best = move;
                    bestScore = score;
                    tied = 1;
                }
                else if (score == bestScore)
                {
                    // Equal moves get an equal chance, so I do not always favour one corner
                    tied++;
                    if (random.Next(tied) == 0) best = move;
                }
            }

            return best;
        }
    }

    // Takes the swap that destroys the most tiles right now, whatever their colour
    public class GreedyBot : ScoringBot
    {
        public GreedyBot(int seed) : base(seed) { }

        protected override float Score(GameSimulation simulation, Move move)
        {
            return simulation.PreviewMove(move.a, move.b).tilesDestroyed;
        }
    }

    // Takes the swap that does the most for the level's goals, and likes making power-ups
    public class GoalAwareBot : ScoringBot
    {
        public GoalAwareBot(int seed) : base(seed) { }

        protected override float Score(GameSimulation simulation, Move move)
        {
            return Evaluate(simulation.PreviewMove(move.a, move.b));
        }

        // A goal tile is worth far more than any other tile; a new power-up sits in between
        public static float Evaluate(MovePreview preview)
        {
            return preview.goalTilesDestroyed * 10f + preview.powerUpsCreated * 5f + preview.tilesDestroyed;
        }
    }

    // Tries its most promising swaps on a copy of the game, lets everything fall,
    // and also looks at the best follow-up swap before deciding
    public class LookAheadBot : ScoringBot
    {
        // Playing a copy is expensive, so only the best few candidates get that treatment
        private const int candidatesToTry = 6;
        private const float winBonus = 1000f;

        private List<Move> candidates = new List<Move>();
        private List<float> candidateScores = new List<float>();

        public LookAheadBot(int seed) : base(seed) { }

        public override Move ChooseMove(GameSimulation simulation, List<Move> validMoves)
        {
            // Shortlist by the quick goal-aware score, best first
            candidates.Clear();
            candidateScores.Clear();

            foreach (Move move in validMoves)
            {
                float quickScore = GoalAwareBot.Evaluate(simulation.PreviewMove(move.a, move.b));

                int index = 0;
                while (index < candidateScores.Count && candidateScores[index] >= quickScore) index++;

                if (index < candidatesToTry)
                {
                    candidates.Insert(index, move);
                    candidateScores.Insert(index, quickScore);

                    if (candidates.Count > candidatesToTry)
                    {
                        candidates.RemoveAt(candidatesToTry);
                        candidateScores.RemoveAt(candidatesToTry);
                    }
                }
            }

            return base.ChooseMove(simulation, candidates);
        }

        protected override float Score(GameSimulation simulation, Move move)
        {
            // The copy spawns its own random tiles, so this is a guess at the future, not a peek
            GameSimulation copy = simulation.Clone(random.Next());

            int neededBefore = copy.Progress.TotalRemaining;
            copy.PlayMove(move.a, move.b);
            float score = (neededBefore - copy.Progress.TotalRemaining) * 10f;

            if (copy.Progress.State == LevelState.Won) return score + winBonus;

            float bestFollowUp = 0f;
            foreach (Move next in copy.GetValidMoves())
            {
                float followUp = GoalAwareBot.Evaluate(copy.PreviewMove(next.a, next.b));
                if (followUp > bestFollowUp) bestFollowUp = followUp;
            }

            return score + bestFollowUp;
        }
    }

    // A player of a given skill: thinks with that probability, and otherwise just plays something.
    // Skill 0 is the random bot, skill 1 is the strategy at full strength.
    public class SkillBot : IBot
    {
        private IBot thinkingBot;
        private IBot carelessBot;
        private float skill;
        private System.Random random;

        public SkillBot(IBot strategy, float skillLevel, int seed)
        {
            thinkingBot = strategy;
            carelessBot = new RandomBot(seed + 1);
            skill = skillLevel;
            random = new System.Random(seed);
        }

        public Move ChooseMove(GameSimulation simulation, List<Move> validMoves)
        {
            IBot chosen = random.NextDouble() < skill ? thinkingBot : carelessBot;
            return chosen.ChooseMove(simulation, validMoves);
        }
    }

    public static class BotFactory
    {
        public static IBot Create(BotStrategy strategy, int seed)
        {
            switch (strategy)
            {
                case BotStrategy.Greedy: return new GreedyBot(seed);
                case BotStrategy.GoalAware: return new GoalAwareBot(seed);
                case BotStrategy.LookAhead: return new LookAheadBot(seed);
                default: return new RandomBot(seed);
            }
        }

        public static IBot Create(BotStrategy strategy, float skill, int seed)
        {
            return new SkillBot(Create(strategy, seed), skill, seed);
        }
    }
}
