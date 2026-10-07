using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Commands;
using Match3Engine.Core;
using Match3Engine.Systems;

namespace Match3Engine.Simulation
{
    // One attempt at a level, with no visuals at all. A whole move resolves instantly here,
    // so bots can play thousands of games, and the same seed always plays out the same way.
    public class GameSimulation
    {
        public BoardModel Board { get; private set; }
        public LevelProgress Progress { get; private set; }

        private CommandSystem commandSystem;
        private MatchSystem matchSystem;
        private GravitySystem gravitySystem;
        private SpawnSystem spawnSystem;
        private PowerUpSystem powerUpSystem;
        private MoveFinder moveFinder;
        private System.Random random;
        private LevelData level;

        // Scratch space for PreviewMove, one counter per tile type
        private int[] destroyedPerType = new int[System.Enum.GetValues(typeof(TileType)).Length];

        private const int maxShuffleAttempts = 50;
        private static readonly Vector2Int noSwap = new Vector2Int(-1, -1);

        public GameSimulation(LevelData levelData, int seed)
        {
            Board = new BoardModel(levelData.width, levelData.height);
            Progress = new LevelProgress(levelData.moveLimit, levelData.goals);

            BuildSystems(levelData, seed);
            FillStartingBoard();
        }

        // Used by Clone: takes over a board and progress that already exist
        private GameSimulation(LevelData levelData, int seed, BoardModel board, LevelProgress progress)
        {
            Board = board;
            Progress = progress;

            BuildSystems(levelData, seed);
        }

        private void BuildSystems(LevelData levelData, int seed)
        {
            level = levelData;
            random = new System.Random(seed);

            commandSystem = new CommandSystem();
            matchSystem = new MatchSystem(Board);
            gravitySystem = new GravitySystem(Board);
            spawnSystem = new SpawnSystem(Board, level.availableTileTypes, random);
            powerUpSystem = new PowerUpSystem(Board);
            moveFinder = new MoveFinder(Board, matchSystem, powerUpSystem);
        }

        // An independent copy of this game in its current state. The copy gets its own seed, so the
        // tiles it spawns are a guess at the future, not a peek at what this game will really deal.
        public GameSimulation Clone(int seed)
        {
            BoardModel boardCopy = new BoardModel(Board.Width, Board.Height);

            for (int x = 0; x < Board.Width; x++)
                for (int y = 0; y < Board.Height; y++)
                    boardCopy.SetTile(x, y, Board.GetTile(x, y));

            return new GameSimulation(level, seed, boardCopy, Progress.Clone());
        }

        // What a swap would destroy straight away, without playing it. Cascades and new tiles
        // are not included, because a player cannot see those coming either.
        public MovePreview PreviewMove(Vector2Int posA, Vector2Int posB)
        {
            MovePreview preview = new MovePreview();

            if (!IsValidMove(posA, posB)) return preview;

            TileType typeA = Board.GetTile(posA.x, posA.y);
            TileType typeB = Board.GetTile(posB.x, posB.y);

            // Trying the swap on my data grid, looking, then putting it back
            Board.SetTile(posA.x, posA.y, typeB);
            Board.SetTile(posB.x, posB.y, typeA);

            MatchResult toDestroy = FindDestruction(posA, posB);

            foreach (Vector2Int pos in toDestroy.matchedTiles)
            {
                destroyedPerType[(int)Board.GetTile(pos.x, pos.y)]++;
            }

            for (int type = 0; type < destroyedPerType.Length; type++)
            {
                // Only tiles a goal still needs count as goal progress
                preview.goalTilesDestroyed += Mathf.Min(destroyedPerType[type], Progress.GetRemaining((TileType)type));
                destroyedPerType[type] = 0;
            }

            preview.valid = true;
            preview.tilesDestroyed = toDestroy.matchedTiles.Count;
            preview.powerUpsCreated = toDestroy.specialsToCreate.Count;

            Board.SetTile(posA.x, posA.y, typeA);
            Board.SetTile(posB.x, posB.y, typeB);

            return preview;
        }

        // What gets destroyed first once the two tiles have been swapped on the board
        private MatchResult FindDestruction(Vector2Int posA, Vector2Int posB)
        {
            TileType typeAtA = Board.GetTile(posA.x, posA.y);
            TileType typeAtB = Board.GetTile(posB.x, posB.y);

            bool isPowerUpA = powerUpSystem.IsPowerUp(typeAtA);
            bool isPowerUpB = powerUpSystem.IsPowerUp(typeAtB);

            if (isPowerUpA || isPowerUpB)
            {
                MatchResult explosion = new MatchResult();

                if (isPowerUpA) explosion.matchedTiles.UnionWith(powerUpSystem.GetExplosionArea(posA, typeAtA, typeAtB));
                if (isPowerUpB) explosion.matchedTiles.UnionWith(powerUpSystem.GetExplosionArea(posB, typeAtB, typeAtA));

                return explosion;
            }

            // Passing the swap coordinates so specials spawn where the player touched
            return matchSystem.FindMatches(posA, posB);
        }

        public bool IsValidMove(Vector2Int a, Vector2Int b)
        {
            return Progress.State == LevelState.Playing && moveFinder.IsValidMove(a, b);
        }

        public List<Move> GetValidMoves()
        {
            if (Progress.State != LevelState.Playing) return new List<Move>();
            return moveFinder.FindValidMoves();
        }

        // Plays one swap all the way through: every cascade, the shuffle if needed, win or lose
        public TurnResult PlayMove(Vector2Int posA, Vector2Int posB)
        {
            TurnResult turn = new TurnResult();

            if (!IsValidMove(posA, posB)) return turn;

            turn.valid = true;
            Progress.UseMove();

            Run(new SwapCommand(Board, posA, posB));

            MatchResult toDestroy = FindDestruction(posA, posB);

            // Keep resolving until a full pass finds nothing left to match
            while (toDestroy.matchedTiles.Count > 0)
            {
                turn.steps.Add(ResolveStep(toDestroy));
                toDestroy = matchSystem.FindMatches(noSwap, noSwap);
            }

            Progress.Evaluate();

            if (Progress.State == LevelState.Playing && !moveFinder.HasValidMove())
            {
                Shuffle();
                turn.shuffled = true;
            }

            return turn;
        }

        private CascadeStep ResolveStep(MatchResult result)
        {
            CascadeStep step = new CascadeStep();

            // Recording what is about to be destroyed, before the types are wiped
            foreach (Vector2Int pos in result.matchedTiles)
            {
                TileType type = Board.GetTile(pos.x, pos.y);
                step.destroyed.Add(new PlacedTile(pos, type));
                Progress.Collect(type);
            }

            foreach (var special in result.specialsToCreate)
            {
                step.specials.Add(new PlacedTile(special.Key, special.Value));
            }

            Run(new DestroyCommand(Board, result));

            Run(new GravityCommand(gravitySystem));
            step.falls.AddRange(gravitySystem.LastMoves);

            Run(new SpawnCommand(spawnSystem));
            foreach (Vector2Int pos in spawnSystem.LastSpawned)
            {
                step.spawns.Add(new PlacedTile(pos, Board.GetTile(pos.x, pos.y)));
            }

            return step;
        }

        private void Run(ICommand command)
        {
            commandSystem.EnqueueCommand(command);
            commandSystem.ProcessNextCommand();
        }

        // A fresh board with no matches on it and at least one move to make
        private void FillStartingBoard()
        {
            for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
            {
                spawnSystem.FillWithoutMatches();
                if (moveFinder.HasValidMove()) return;
            }
        }

        // Mixes the tiles already on the board until there is a move again
        private void Shuffle()
        {
            List<TileType> tiles = new List<TileType>();

            for (int x = 0; x < Board.Width; x++)
                for (int y = 0; y < Board.Height; y++)
                    tiles.Add(Board.GetTile(x, y));

            for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
            {
                for (int i = tiles.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    TileType swapped = tiles[i];
                    tiles[i] = tiles[j];
                    tiles[j] = swapped;
                }

                int index = 0;
                for (int x = 0; x < Board.Width; x++)
                    for (int y = 0; y < Board.Height; y++)
                        Board.SetTile(x, y, tiles[index++]);

                bool hasMatches = matchSystem.FindMatches(noSwap, noSwap).matchedTiles.Count > 0;
                if (!hasMatches && moveFinder.HasValidMove()) return;
            }

            // These tiles cannot be arranged into a playable board, so I deal a new one
            FillStartingBoard();
        }
    }
}
