using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Match3Engine.AI;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.Tests
{
    public class ObstacleTests
    {
        private const TileType R = TileType.Red;
        private const TileType B = TileType.Blue;
        private const TileType G = TileType.Green;
        private const TileType Y = TileType.Yellow;
        private const TileType C = TileType.Crate;
        private const TileType H = TileType.Hole;

        private List<LevelData> createdLevels = new List<LevelData>();

        [TearDown]
        public void DestroyLevels()
        {
            foreach (LevelData level in createdLevels) Object.DestroyImmediate(level);
            createdLevels.Clear();
        }

        // Rows are written top row first, the way the board looks on screen
        private static BoardModel MakeBoard(TileType[][] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            BoardModel board = new BoardModel(width, height);

            for (int row = 0; row < height; row++)
                for (int x = 0; x < width; x++)
                    board.SetTile(x, height - 1 - row, rows[row][x]);

            return board;
        }

        private LevelData MakeLevel(int size, int moveLimit, LevelGoal goal, params LevelObstacle[] obstacles)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.width = size;
            level.height = size;
            level.availableTileTypes = new[] { R, B, G, Y };
            level.moveLimit = moveLimit;
            level.goals = new[] { goal };
            level.obstacles = obstacles;

            createdLevels.Add(level);
            return level;
        }

        private static LevelObstacle Obstacle(int x, int y, TileType type)
        {
            return new LevelObstacle { position = new Vector2Int(x, y), type = type };
        }

        // A board with a hole in the middle, a missing corner and a block of crates
        private LevelData MakeObstacleLevel()
        {
            return MakeLevel(7, 25, new LevelGoal { type = C, amount = 6 },
                Obstacle(3, 3, H), Obstacle(0, 6, H), Obstacle(6, 0, H), Obstacle(3, 0, H),
                Obstacle(1, 1, C), Obstacle(2, 1, C), Obstacle(4, 1, C), Obstacle(5, 1, C),
                Obstacle(2, 4, C), Obstacle(4, 4, C));
        }

        [Test]
        public void Match_BreaksCratesNextToIt_ButNotFurtherAway()
        {
            BoardModel board = MakeBoard(new[]
            {
                new[] { G, C, Y, B, C },
                new[] { R, R, R, C, G },
                new[] { B, C, G, Y, B },
            });

            MatchResult result = new MatchSystem(board).FindMatches(new Vector2Int(-1, -1), new Vector2Int(-1, -1));

            // The three reds, the crate above, the crate below and the crate to the right
            Assert.That(result.matchedTiles, Is.EquivalentTo(new[]
            {
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                new Vector2Int(1, 2), new Vector2Int(1, 0), new Vector2Int(3, 1)
            }));
        }

        [Test]
        public void Crates_NeverMatchEachOther()
        {
            BoardModel board = MakeBoard(new[]
            {
                new[] { C, C, C, C },
                new[] { R, B, G, Y },
                new[] { C, B, G, Y },
                new[] { C, G, Y, R },
            });

            MatchResult result = new MatchSystem(board).FindMatches(new Vector2Int(-1, -1), new Vector2Int(-1, -1));

            Assert.That(result.matchedTiles, Is.Empty);
        }

        [Test]
        public void Gravity_DropsTilesAndCratesPastHoles()
        {
            // One column, bottom to top: empty, hole, red, empty, crate
            BoardModel board = new BoardModel(1, 5);
            board.SetTile(0, 1, H);
            board.SetTile(0, 2, R);
            board.SetTile(0, 4, C);

            GravitySystem gravity = new GravitySystem(board);
            gravity.ApplyGravity();

            Assert.That(board.GetTile(0, 0), Is.EqualTo(R), "The red tile should fall past the hole to the bottom.");
            Assert.That(board.GetTile(0, 1), Is.EqualTo(H), "The hole must not move.");
            Assert.That(board.GetTile(0, 2), Is.EqualTo(C), "The crate should fall like a tile.");
            Assert.That(board.GetTile(0, 3), Is.EqualTo(TileType.None));
            Assert.That(board.GetTile(0, 4), Is.EqualTo(TileType.None));
            Assert.That(gravity.LastMoves, Has.Count.EqualTo(2));
        }

        [Test]
        public void CratesAndHoles_CannotBeSwapped()
        {
            BoardModel board = MakeBoard(new[]
            {
                new[] { G, R, Y, B },
                new[] { R, C, R, R },
                new[] { R, H, R, R },
                new[] { B, R, B, G },
            });

            MatchSystem matches = new MatchSystem(board);
            MoveFinder finder = new MoveFinder(board, matches, new PowerUpSystem(board));

            // Moving the crate left would line up three reds, if crates could move
            Assert.That(finder.IsValidMove(new Vector2Int(1, 2), new Vector2Int(0, 2)), Is.False, "A crate was swapped.");

            // The same one row down, with a hole in the crate's place
            Assert.That(finder.IsValidMove(new Vector2Int(1, 1), new Vector2Int(0, 1)), Is.False, "A hole was swapped.");

            foreach (Move move in finder.FindValidMoves())
            {
                Assert.That(board.GetTile(move.a.x, move.a.y), Is.Not.EqualTo(C).And.Not.EqualTo(H));
                Assert.That(board.GetTile(move.b.x, move.b.y), Is.Not.EqualTo(C).And.Not.EqualTo(H));
            }
        }

        [Test]
        public void Explosion_BreaksCrates_AndSkipsHoles()
        {
            BoardModel board = MakeBoard(new[]
            {
                new[] { G, R, Y },
                new[] { C, H, B },
                new[] { G, C, Y },
            });

            // TNT in the middle column, top row: its 3x3 area covers the top two rows
            HashSet<Vector2Int> area = new PowerUpSystem(board).GetExplosionArea(new Vector2Int(1, 2), TileType.TNT);

            Assert.That(area, Has.Member(new Vector2Int(0, 1)), "The crate inside the blast should be destroyed.");
            Assert.That(area, Has.No.Member(new Vector2Int(1, 1)), "A hole can never be destroyed.");
            Assert.That(area, Has.No.Member(new Vector2Int(1, 0)), "The crate outside the blast should survive.");
        }

        [Test]
        public void Level_StartsWithItsObstaclesInPlace()
        {
            LevelData level = MakeObstacleLevel();

            for (int seed = 1; seed <= 20; seed++)
            {
                GameSimulation sim = new GameSimulation(level, seed);

                foreach (LevelObstacle obstacle in level.obstacles)
                {
                    Assert.That(sim.Board.GetTile(obstacle.position.x, obstacle.position.y), Is.EqualTo(obstacle.type), "seed " + seed);
                }

                Assert.That(sim.GetValidMoves(), Is.Not.Empty, "No move at start, seed " + seed);
            }
        }

        [Test]
        public void HolesNeverChange_AndTheBoardStaysFull_ThroughWholeGames()
        {
            LevelData level = MakeObstacleLevel();

            for (int seed = 1; seed <= 40; seed++)
            {
                GameSimulation sim = new GameSimulation(level, seed);
                IBot bot = BotFactory.Create(BotStrategy.Random, seed);

                while (sim.Progress.State == LevelState.Playing)
                {
                    List<Move> moves = sim.GetValidMoves();
                    Assert.That(moves, Is.Not.Empty, "Stuck with no move, seed " + seed);

                    Move move = bot.ChooseMove(sim, moves);
                    sim.PlayMove(move.a, move.b);

                    int holes = 0;
                    for (int x = 0; x < level.width; x++)
                    {
                        for (int y = 0; y < level.height; y++)
                        {
                            TileType type = sim.Board.GetTile(x, y);
                            Assert.That(type, Is.Not.EqualTo(TileType.None), "Empty slot left behind, seed " + seed);
                            if (type == H) holes++;
                        }
                    }

                    Assert.That(holes, Is.EqualTo(4), "A hole appeared or disappeared, seed " + seed);
                    foreach (LevelObstacle obstacle in level.obstacles)
                    {
                        if (obstacle.type != H) continue;
                        Assert.That(sim.Board.GetTile(obstacle.position.x, obstacle.position.y), Is.EqualTo(H), "A hole moved, seed " + seed);
                    }
                }
            }
        }

        [Test]
        public void BrokenCrates_CountTowardACrateGoal()
        {
            LevelData level = MakeObstacleLevel();

            // A thinking bot should be able to clear six crates in 25 moves most of the time
            BatchResult result = BatchRunner.Run(level, seed => BotFactory.Create(BotStrategy.GoalAware, seed), 100);

            Assert.That(result.PassRate, Is.GreaterThan(0.5f), "Crate goals are not being completed.");
        }

        [Test]
        public void ReportedSteps_ReproduceABoardWithObstacles()
        {
            // Same check as for plain boards: the view only sees the steps
            LevelData level = MakeObstacleLevel();

            for (int seed = 1; seed <= 20; seed++)
            {
                GameSimulation sim = new GameSimulation(level, seed);
                IBot bot = BotFactory.Create(BotStrategy.Random, seed);
                TileType[,] copy = CopyBoard(sim.Board);

                while (sim.Progress.State == LevelState.Playing)
                {
                    Move move = bot.ChooseMove(sim, sim.GetValidMoves());
                    TurnResult turn = sim.PlayMove(move.a, move.b);

                    TileType held = copy[move.a.x, move.a.y];
                    copy[move.a.x, move.a.y] = copy[move.b.x, move.b.y];
                    copy[move.b.x, move.b.y] = held;

                    foreach (CascadeStep step in turn.steps)
                    {
                        foreach (PlacedTile tile in step.destroyed)
                        {
                            Assert.That(tile.type, Is.Not.EqualTo(H), "A hole was reported destroyed, seed " + seed);
                            Assert.That(copy[tile.position.x, tile.position.y], Is.EqualTo(tile.type));
                            copy[tile.position.x, tile.position.y] = TileType.None;
                        }

                        foreach (PlacedTile tile in step.specials)
                            copy[tile.position.x, tile.position.y] = tile.type;

                        foreach (TileMove fall in step.falls)
                        {
                            Assert.That(copy[fall.to.x, fall.to.y], Is.EqualTo(TileType.None), "A tile fell into an occupied slot, seed " + seed);
                            Assert.That(copy[fall.from.x, fall.from.y], Is.Not.EqualTo(H), "A hole fell, seed " + seed);
                            copy[fall.to.x, fall.to.y] = copy[fall.from.x, fall.from.y];
                            copy[fall.from.x, fall.from.y] = TileType.None;
                        }

                        foreach (PlacedTile tile in step.spawns)
                        {
                            Assert.That(copy[tile.position.x, tile.position.y], Is.EqualTo(TileType.None), "A tile spawned into an occupied slot, seed " + seed);
                            Assert.That(tile.type, Is.Not.EqualTo(C), "A crate was spawned, seed " + seed);
                            copy[tile.position.x, tile.position.y] = tile.type;
                        }
                    }

                    if (turn.shuffled) copy = CopyBoard(sim.Board);

                    Assert.That(copy, Is.EqualTo(CopyBoard(sim.Board)), "Replayed board differs from the real one, seed " + seed);
                }
            }
        }

        private static TileType[,] CopyBoard(BoardModel board)
        {
            TileType[,] copy = new TileType[board.Width, board.Height];
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                    copy[x, y] = board.GetTile(x, y);
            return copy;
        }
    }
}
