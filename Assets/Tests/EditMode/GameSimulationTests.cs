using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.Tests
{
    // These run a whole game with no scene and no view, which is exactly what the bots will do
    public class GameSimulationTests
    {
        private static readonly TileType[] allColours =
        {
            TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow, TileType.Purple, TileType.Pink
        };

        private List<LevelData> createdLevels = new List<LevelData>();

        [TearDown]
        public void DestroyLevels()
        {
            foreach (LevelData level in createdLevels) Object.DestroyImmediate(level);
            createdLevels.Clear();
        }

        private LevelData MakeLevel(int size, int colours, int moveLimit, int redGoal)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.width = size;
            level.height = size;
            level.availableTileTypes = new TileType[colours];
            System.Array.Copy(allColours, level.availableTileTypes, colours);
            level.moveLimit = moveLimit;
            level.goals = new[] { new LevelGoal { type = TileType.Red, amount = redGoal } };

            createdLevels.Add(level);
            return level;
        }

        // The whole observable state of a game as text, so two games can be compared
        private static string Describe(GameSimulation sim)
        {
            StringBuilder text = new StringBuilder();

            for (int y = 0; y < sim.Board.Height; y++)
                for (int x = 0; x < sim.Board.Width; x++)
                    text.Append((int)sim.Board.GetTile(x, y)).Append(',');

            text.Append(" moves=").Append(sim.Progress.MovesLeft);
            text.Append(" red=").Append(sim.Progress.GetRemaining(TileType.Red));
            text.Append(" state=").Append(sim.Progress.State);
            return text.ToString();
        }

        private static bool IsColour(TileType type)
        {
            return type >= TileType.Red && type <= TileType.Pink;
        }

        // An independent check for three in a row, not using the game's own match code
        private static bool HasLineOfThree(BoardModel board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    TileType type = board.GetTile(x, y);
                    if (!IsColour(type)) continue;

                    if (board.GetTile(x + 1, y) == type && board.GetTile(x + 2, y) == type) return true;
                    if (board.GetTile(x, y + 1) == type && board.GetTile(x, y + 2) == type) return true;
                }
            }
            return false;
        }

        private static bool HasEmptySlot(BoardModel board)
        {
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                    if (board.GetTile(x, y) == TileType.None) return true;
            return false;
        }

        // Plays the first valid move every turn and records the state after each one
        private static List<string> PlayToEnd(GameSimulation sim, int moveLimit)
        {
            List<string> history = new List<string> { Describe(sim) };

            int turns = 0;
            while (sim.Progress.State == LevelState.Playing)
            {
                List<Move> moves = sim.GetValidMoves();
                Assert.That(moves, Is.Not.Empty, "A game in progress must always have a move.");

                TurnResult turn = sim.PlayMove(moves[0].a, moves[0].b);
                Assert.That(turn.valid, Is.True, "A move the simulation listed as valid was rejected.");

                history.Add(Describe(sim));

                turns++;
                Assert.That(turns, Is.LessThanOrEqualTo(moveLimit), "The game ran past its move limit.");
            }

            return history;
        }

        [Test]
        public void SameSeed_PlaysOutIdentically()
        {
            LevelData level = MakeLevel(8, 5, 20, 30);

            List<string> first = PlayToEnd(new GameSimulation(level, 1234), level.moveLimit);
            List<string> second = PlayToEnd(new GameSimulation(level, 1234), level.moveLimit);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DifferentSeeds_DealDifferentBoards()
        {
            LevelData level = MakeLevel(8, 5, 20, 30);

            string first = Describe(new GameSimulation(level, 1));
            string second = Describe(new GameSimulation(level, 2));

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void StartingBoard_HasNoMatches_AndHasAMove()
        {
            for (int size = 5; size <= 10; size++)
            {
                for (int colours = 3; colours <= 6; colours++)
                {
                    LevelData level = MakeLevel(size, colours, 20, 30);

                    for (int seed = 1; seed <= 20; seed++)
                    {
                        GameSimulation sim = new GameSimulation(level, seed);
                        string where = "size " + size + ", " + colours + " colours, seed " + seed;

                        Assert.That(HasEmptySlot(sim.Board), Is.False, "Empty slot at start: " + where);
                        Assert.That(HasLineOfThree(sim.Board), Is.False, "Free match at start: " + where);
                        Assert.That(sim.GetValidMoves(), Is.Not.Empty, "No move at start: " + where);
                    }
                }
            }
        }

        [Test]
        public void Game_AlwaysEndsWonOrLost_AndStaysPlayable()
        {
            // Small boards with few colours run out of moves most often, so they exercise the shuffle
            for (int colours = 3; colours <= 5; colours++)
            {
                LevelData level = MakeLevel(5, colours, 30, 60);

                for (int seed = 1; seed <= 40; seed++)
                {
                    GameSimulation sim = new GameSimulation(level, seed);
                    string where = colours + " colours, seed " + seed;

                    while (sim.Progress.State == LevelState.Playing)
                    {
                        List<Move> moves = sim.GetValidMoves();
                        Assert.That(moves, Is.Not.Empty, "Stuck with no move: " + where);

                        sim.PlayMove(moves[0].a, moves[0].b);

                        Assert.That(HasEmptySlot(sim.Board), Is.False, "Empty slot after a move: " + where);
                        Assert.That(HasLineOfThree(sim.Board), Is.False, "Unresolved match after a move: " + where);
                    }

                    Assert.That(sim.Progress.State, Is.Not.EqualTo(LevelState.Playing));
                    Assert.That(sim.GetValidMoves(), Is.Empty, "A finished game still offers moves: " + where);
                }
            }
        }

        [Test]
        public void RejectedSwap_ChangesNothing_AndCostsNoMove()
        {
            LevelData level = MakeLevel(8, 5, 20, 30);
            GameSimulation sim = new GameSimulation(level, 77);
            string before = Describe(sim);

            // Find two neighbours whose swap makes no match
            Vector2Int a = Vector2Int.zero, b = Vector2Int.zero;
            bool found = false;
            for (int x = 0; x < level.width - 1 && !found; x++)
            {
                for (int y = 0; y < level.height && !found; y++)
                {
                    a = new Vector2Int(x, y);
                    b = new Vector2Int(x + 1, y);
                    found = !sim.IsValidMove(a, b);
                }
            }
            Assert.That(found, Is.True, "Test setup: every swap on this board is valid.");

            Assert.That(sim.PlayMove(a, b).valid, Is.False, "A swap that matches nothing was accepted.");
            Assert.That(sim.PlayMove(new Vector2Int(0, 0), new Vector2Int(2, 0)).valid, Is.False, "A swap between non-neighbours was accepted.");
            Assert.That(sim.PlayMove(new Vector2Int(0, 0), new Vector2Int(1, 1)).valid, Is.False, "A diagonal swap was accepted.");
            Assert.That(sim.PlayMove(new Vector2Int(0, 0), new Vector2Int(-1, 0)).valid, Is.False, "A swap off the board was accepted.");

            Assert.That(Describe(sim), Is.EqualTo(before));
        }

        [Test]
        public void ValidMove_SpendsExactlyOneMove()
        {
            LevelData level = MakeLevel(8, 5, 20, 1000);
            GameSimulation sim = new GameSimulation(level, 5);

            Move move = sim.GetValidMoves()[0];
            TurnResult turn = sim.PlayMove(move.a, move.b);

            Assert.That(turn.valid, Is.True);
            Assert.That(turn.steps, Is.Not.Empty);
            Assert.That(sim.Progress.MovesLeft, Is.EqualTo(19));
        }

        [Test]
        public void ReportedSteps_ReproduceTheBoard()
        {
            // The view only sees the steps. Replaying them on a copy must land on the real board.
            LevelData level = MakeLevel(7, 4, 30, 1000);

            for (int seed = 1; seed <= 20; seed++)
            {
                GameSimulation sim = new GameSimulation(level, seed);
                TileType[,] copy = CopyBoard(sim.Board);

                while (sim.Progress.State == LevelState.Playing)
                {
                    Move move = sim.GetValidMoves()[0];
                    TurnResult turn = sim.PlayMove(move.a, move.b);

                    TileType held = copy[move.a.x, move.a.y];
                    copy[move.a.x, move.a.y] = copy[move.b.x, move.b.y];
                    copy[move.b.x, move.b.y] = held;

                    foreach (CascadeStep step in turn.steps)
                    {
                        foreach (PlacedTile tile in step.destroyed)
                        {
                            Assert.That(copy[tile.position.x, tile.position.y], Is.EqualTo(tile.type), "Destroyed tile has the wrong type, seed " + seed);
                            copy[tile.position.x, tile.position.y] = TileType.None;
                        }

                        foreach (PlacedTile tile in step.specials)
                            copy[tile.position.x, tile.position.y] = tile.type;

                        foreach (TileMove fall in step.falls)
                        {
                            Assert.That(copy[fall.to.x, fall.to.y], Is.EqualTo(TileType.None), "A tile fell into an occupied slot, seed " + seed);
                            copy[fall.to.x, fall.to.y] = copy[fall.from.x, fall.from.y];
                            copy[fall.from.x, fall.from.y] = TileType.None;
                        }

                        foreach (PlacedTile tile in step.spawns)
                        {
                            Assert.That(copy[tile.position.x, tile.position.y], Is.EqualTo(TileType.None), "A tile spawned into an occupied slot, seed " + seed);
                            copy[tile.position.x, tile.position.y] = tile.type;
                        }
                    }

                    // A shuffle is shown by repainting the whole board, so the copy does the same
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
