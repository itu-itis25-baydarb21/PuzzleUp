using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Match3Engine.AI;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.Tests
{
    public class BotTests
    {
        private static readonly BotStrategy[] allStrategies =
        {
            BotStrategy.Random, BotStrategy.Greedy, BotStrategy.GoalAware, BotStrategy.LookAhead
        };

        private LevelData level;

        [SetUp]
        public void CreateLevel()
        {
            // Tuned so that careless play usually loses and careful play usually wins
            level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 8;
            level.height = 8;
            level.availableTileTypes = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow, TileType.Pink };
            level.moveLimit = 20;
            level.goals = new[]
            {
                new LevelGoal { type = TileType.Red, amount = 25 },
                new LevelGoal { type = TileType.Blue, amount = 25 }
            };
        }

        [TearDown]
        public void DestroyLevel()
        {
            Object.DestroyImmediate(level);
        }

        private static string Describe(GameSimulation sim)
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();

            for (int y = 0; y < sim.Board.Height; y++)
                for (int x = 0; x < sim.Board.Width; x++)
                    text.Append((int)sim.Board.GetTile(x, y)).Append(',');

            text.Append(sim.Progress.MovesLeft).Append('/').Append(sim.Progress.TotalRemaining).Append('/').Append(sim.Progress.State);
            return text.ToString();
        }

        [Test]
        public void EveryBot_OnlyPlaysValidMoves_AndFinishesTheGame()
        {
            foreach (BotStrategy strategy in allStrategies)
            {
                for (int seed = 1; seed <= 10; seed++)
                {
                    GameSimulation sim = new GameSimulation(level, seed);
                    IBot bot = BotFactory.Create(strategy, seed);

                    int turns = 0;
                    while (sim.Progress.State == LevelState.Playing)
                    {
                        List<Move> moves = sim.GetValidMoves();
                        Move move = bot.ChooseMove(sim, moves);

                        Assert.That(sim.PlayMove(move.a, move.b).valid, Is.True, strategy + " chose a swap that is not allowed, seed " + seed);
                        Assert.That(++turns, Is.LessThanOrEqualTo(level.moveLimit));
                    }
                }
            }
        }

        [Test]
        public void ChoosingAMove_DoesNotChangeTheGame()
        {
            // Bots preview swaps and play copies. None of that may leak into the real game.
            foreach (BotStrategy strategy in allStrategies)
            {
                GameSimulation sim = new GameSimulation(level, 42);
                string before = Describe(sim);

                BotFactory.Create(strategy, 42).ChooseMove(sim, sim.GetValidMoves());

                Assert.That(Describe(sim), Is.EqualTo(before), strategy + " changed the game while thinking.");
            }
        }

        [Test]
        public void Clone_IsIndependentOfTheOriginal()
        {
            GameSimulation sim = new GameSimulation(level, 7);
            string before = Describe(sim);

            GameSimulation copy = sim.Clone(99);
            Assert.That(Describe(copy), Is.EqualTo(before));

            Move move = copy.GetValidMoves()[0];
            copy.PlayMove(move.a, move.b);

            Assert.That(Describe(copy), Is.Not.EqualTo(before));
            Assert.That(Describe(sim), Is.EqualTo(before));
        }

        [Test]
        public void Preview_MatchesWhatTheMoveDestroysFirst()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                GameSimulation sim = new GameSimulation(level, seed);

                foreach (Move move in sim.GetValidMoves())
                {
                    MovePreview preview = sim.PreviewMove(move.a, move.b);
                    TurnResult turn = sim.Clone(seed).PlayMove(move.a, move.b);

                    Assert.That(preview.valid, Is.True);
                    Assert.That(preview.tilesDestroyed, Is.EqualTo(turn.steps[0].destroyed.Count));
                    Assert.That(preview.powerUpsCreated, Is.EqualTo(turn.steps[0].specials.Count));
                }
            }
        }

        [Test]
        public void SameBatch_GivesTheSameResult()
        {
            BatchResult first = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.LookAhead, 0.5f, seed), 30);
            BatchResult second = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.LookAhead, 0.5f, seed), 30);

            Assert.That(second.games, Is.EqualTo(first.games));
        }

        [Test]
        public void ThinkingBots_BeatTheRandomBot()
        {
            const int games = 300;

            float random = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.Random, seed), games).PassRate;
            float goalAware = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.GoalAware, seed), games).PassRate;
            float lookAhead = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.LookAhead, seed), games).PassRate;

            Assert.That(goalAware, Is.GreaterThan(random + 0.15f), "Goal-aware " + goalAware + " vs random " + random);
            Assert.That(lookAhead, Is.GreaterThan(random + 0.15f), "Look-ahead " + lookAhead + " vs random " + random);
        }

        [Test]
        public void HigherSkill_WinsMoreOften()
        {
            const int games = 400;

            float low = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.GoalAware, 0f, seed), games).PassRate;
            float mid = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.GoalAware, 0.5f, seed), games).PassRate;
            float high = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.GoalAware, 1f, seed), games).PassRate;

            Assert.That(mid, Is.GreaterThan(low + 0.05f), "Skill 0.5 " + mid + " vs skill 0 " + low);
            Assert.That(high, Is.GreaterThan(mid + 0.05f), "Skill 1 " + high + " vs skill 0.5 " + mid);
        }
    }
}
