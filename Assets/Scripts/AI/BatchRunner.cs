using System.Collections.Generic;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.AI
{
    // How one game ended
    public struct GameResult
    {
        public bool won;
        public int movesLeft;

        // From 0 to 1; always 1 for a win
        public float goalProgress;
    }

    // The outcome of many games on one level
    public class BatchResult
    {
        public List<GameResult> games = new List<GameResult>();

        public int Games => games.Count;

        public int Wins
        {
            get
            {
                int wins = 0;
                foreach (GameResult game in games) if (game.won) wins++;
                return wins;
            }
        }

        // From 0 to 1
        public float PassRate => Games > 0 ? (float)Wins / Games : 0f;

        // Over the games that were won. A high number means the level has moves to spare.
        public float AverageMovesLeft
        {
            get
            {
                int wins = 0, moves = 0;
                foreach (GameResult game in games)
                {
                    if (!game.won) continue;
                    wins++;
                    moves += game.movesLeft;
                }
                return wins > 0 ? (float)moves / wins : 0f;
            }
        }

        // Over the games that were lost, from 0 to 1. A high number means the losses were close.
        public float AverageProgressWhenLost
        {
            get
            {
                int losses = 0;
                float progress = 0f;
                foreach (GameResult game in games)
                {
                    if (game.won) continue;
                    losses++;
                    progress += game.goalProgress;
                }
                return losses > 0 ? progress / losses : 0f;
            }
        }
    }

    public static class BatchRunner
    {
        // Lets the bot play a game that is already set up until it is won or lost
        public static void PlayToEnd(GameSimulation simulation, IBot bot)
        {
            while (simulation.Progress.State == LevelState.Playing)
            {
                List<Move> moves = simulation.GetValidMoves();
                if (moves.Count == 0) break;

                Move move = bot.ChooseMove(simulation, moves);
                simulation.PlayMove(move.a, move.b);
            }
        }

        // Plays one whole game with no visuals
        public static GameResult PlayGame(LevelData level, IBot bot, int seed)
        {
            GameSimulation simulation = new GameSimulation(level, seed);
            PlayToEnd(simulation, bot);

            return new GameResult
            {
                won = simulation.Progress.State == LevelState.Won,
                movesLeft = simulation.Progress.MovesLeft,
                goalProgress = simulation.Progress.GoalProgress
            };
        }

        // Plays the level many times. Game number i uses seed firstSeed + i for both the board
        // and the bot, so the same call always gives the same result.
        public static BatchResult Run(LevelData level, System.Func<int, IBot> createBot, int games, int firstSeed = 1)
        {
            BatchResult result = new BatchResult();

            for (int i = 0; i < games; i++)
            {
                int seed = firstSeed + i;
                result.games.Add(PlayGame(level, createBot(seed), seed));
            }

            return result;
        }
    }
}
