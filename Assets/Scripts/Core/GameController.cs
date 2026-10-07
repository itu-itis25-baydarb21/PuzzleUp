using Match3Engine.Simulation;
using Match3Engine.View;
using UnityEngine;
using System.Collections;

namespace Match3Engine.Core
{
    [System.Serializable]
    public struct TileSpriteMapping
    {
        public TileType type;
        public Sprite sprite;
    }

    // Plays one level on screen. The simulation decides everything instantly;
    // my job here is to replay what it reports, one animated step at a time.
    public class GameController : MonoBehaviour
    {
        private GameSimulation simulation;

        // True from the moment a swap starts until the board has fully settled
        private bool isBusy;

        // Moves and goals as currently shown on screen. The simulation is always a full
        // turn ahead of the animation, so I keep my own copy and advance it step by step.
        public LevelProgress Progress { get; private set; }

        // The game itself. A bot reads this to choose its move.
        public GameSimulation Simulation => simulation;

        // True while a move is being animated. Swaps sent now are ignored.
        public bool IsBusy => isBusy;

        // The seed the current attempt was dealt from
        public int CurrentSeed { get; private set; }

        [Header("Level")]
        public LevelData level;

        [Tooltip("The same seed always deals the same tiles. 0 picks a new seed every time.")]
        public int seed;

        [Header("View References")]
        public BoardView boardView;
        public LevelHud levelHud;
        public TileSpriteMapping[] tileSprites;

        private void Start()
        {
            if (level == null)
            {
                Debug.LogError("GameController has no level assigned.", this);
                return;
            }

            BeginAttempt();

            boardView.InitializeBoard(simulation.Board);
            boardView.CenterAndScaleCamera(level.width, level.height);

            ShowSimulationState();
        }

        // A fresh game of the same level
        private void BeginAttempt()
        {
            CurrentSeed = seed != 0 ? seed : System.Environment.TickCount;
            simulation = new GameSimulation(level, CurrentSeed);
        }

        // Starts the level over. With seed 0 this deals a new board; with a fixed seed, the same one.
        public void RestartLevel()
        {
            if (simulation == null || isBusy) return;

            BeginAttempt();
            boardView.SetBoard(simulation.Board);
            ShowSimulationState();
        }

        // Jumps the screen straight to where the simulation is now, with no animation.
        // Used at the start, and after a game was finished off-screen.
        public void ShowSimulationState()
        {
            if (simulation == null || isBusy) return;

            Progress = simulation.Progress.Clone();
            boardView.SyncVisualsWithData(GetSpriteForType);
            levelHud.Refresh(Progress);
        }

        // Helper method to find the right image for my tile types
        private Sprite GetSpriteForType(TileType type)
        {
            foreach (var mapping in tileSprites)
            {
                if (mapping.type == type) return mapping.sprite;
            }
            return null;
        }


        public void ProcessPlayerSwap(Vector2Int posA, Vector2Int posB)
        {
            // No more moves once the level has been won or lost
            if (Progress == null || Progress.State != LevelState.Playing) return;

            // Ignoring swipes while the board is still animating, or ones that leave the board
            if (isBusy) return;
            if (!simulation.Board.IsValidPosition(posA.x, posA.y) || !simulation.Board.IsValidPosition(posB.x, posB.y)) return;

            // Starting my timeline routine so animations have time to play
            StartCoroutine(PlayTurnRoutine(posA, posB));
        }

        private IEnumerator PlayTurnRoutine(Vector2Int posA, Vector2Int posB)
        {
            isBusy = true;

            // The simulation resolves the whole move right now. Everything after this is animation.
            TurnResult turn = simulation.PlayMove(posA, posB);

            boardView.SwapVisuals(posA, posB);
            yield return new WaitForSeconds(0.25f);

            if (!turn.valid)
            {
                // The swap did nothing, so the tiles slide back and no move is spent
                boardView.SwapVisuals(posA, posB);
                yield return new WaitForSeconds(0.25f);

                isBusy = false;
                yield break;
            }

            Progress.UseMove();
            levelHud.Refresh(Progress);

            foreach (CascadeStep step in turn.steps)
            {
                yield return PlayStepRoutine(step);
            }

            if (turn.shuffled)
            {
                // No moves were left, so the simulation mixed the board
                boardView.SyncVisualsWithData(GetSpriteForType);
                yield return new WaitForSeconds(0.3f);
            }

            // The board has settled, so now I can show if the level is won or lost
            Progress.Evaluate();
            levelHud.Refresh(Progress);

            isBusy = false;
        }

        // Shows one cascade: tiles disappear, then the survivors and the new tiles fall together
        private IEnumerator PlayStepRoutine(CascadeStep step)
        {
            foreach (PlacedTile tile in step.destroyed)
            {
                Progress.Collect(tile.type);
            }
            levelHud.Refresh(Progress);

            boardView.ClearTiles(step.destroyed);
            boardView.PlaceTiles(step.specials, GetSpriteForType);
            yield return new WaitForSeconds(0.2f);

            boardView.DropTiles(step.falls);
            boardView.DropNewTiles(step.spawns, GetSpriteForType);

            // Waiting for every tile to land before I show the next cascade
            yield return new WaitUntil(() => !boardView.IsDropping);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
