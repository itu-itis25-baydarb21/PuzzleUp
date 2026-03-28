using Match3Engine.Commands;
using Match3Engine.Systems;
using Match3Engine.View;
using System.Collections.Generic;
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

    public class GameController : MonoBehaviour
    {
        private BoardModel boardModel;
        private CommandSystem commandSystem;
        private MatchSystem matchSystem;
        private GravitySystem gravitySystem;
        private SpawnSystem spawnSystem;
        private PowerUpSystem powerUpSystem; 

        [Header("Board Settings")]
        public int boardWidth = 8;
        public int boardHeight = 8;
        public TileType[] availableTileTypes;

        [Header("View References")]
        public BoardView boardView;
        public TileSpriteMapping[] tileSprites;

        private void Start()
        {
            boardModel = new BoardModel(boardWidth, boardHeight);

            commandSystem = new CommandSystem();
            matchSystem = new MatchSystem(boardModel);
            gravitySystem = new GravitySystem(boardModel);
            spawnSystem = new SpawnSystem(boardModel, availableTileTypes);
            powerUpSystem = new PowerUpSystem(boardModel); 

            boardView.InitializeBoard(boardModel);

            boardView.CenterAndScaleCamera(boardWidth, boardHeight);

            spawnSystem.SpawnTiles();
            boardView.SyncVisualsWithData(GetSpriteForType);
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
            // Starting my timeline routine so animations have time to play
            StartCoroutine(SwapAndProcessRoutine(posA, posB));
        }

        private IEnumerator SwapAndProcessRoutine(Vector2Int posA, Vector2Int posB)
        {
            boardView.SwapVisuals(posA, posB);

            ICommand swapCmd = new SwapCommand(boardModel, posA, posB);
            commandSystem.EnqueueCommand(swapCmd);
            commandSystem.ProcessNextCommand();

            yield return new WaitForSeconds(0.25f);

            TileType typeAtA = boardModel.GetTile(posA.x, posA.y);
            TileType typeAtB = boardModel.GetTile(posB.x, posB.y);

            bool isPowerUpA = powerUpSystem.IsPowerUp(typeAtA);
            bool isPowerUpB = powerUpSystem.IsPowerUp(typeAtB);

            if (isPowerUpA || isPowerUpB)
            {
                HashSet<Vector2Int> explosionArea = new HashSet<Vector2Int>();

                if (isPowerUpA) explosionArea.UnionWith(powerUpSystem.GetExplosionArea(posA, typeAtA, typeAtB));
                if (isPowerUpB) explosionArea.UnionWith(powerUpSystem.GetExplosionArea(posB, typeAtB, typeAtA));

                MatchResult powerUpResult = new MatchResult();
                powerUpResult.matchedTiles = explosionArea;

                ICommand destroyCmd = new DestroyCommand(boardModel, powerUpResult);
                commandSystem.EnqueueCommand(destroyCmd);
                commandSystem.ProcessNextCommand();
                boardView.SyncVisualsWithData(GetSpriteForType);
                yield return new WaitForSeconds(0.2f);

                ICommand gravityCmd = new GravityCommand(gravitySystem);
                commandSystem.EnqueueCommand(gravityCmd);
                commandSystem.ProcessNextCommand();
                boardView.SyncVisualsWithData(GetSpriteForType);
                yield return new WaitForSeconds(0.2f);

                ICommand spawnCmd = new SpawnCommand(spawnSystem);
                commandSystem.EnqueueCommand(spawnCmd);
                commandSystem.ProcessNextCommand();
                boardView.SyncVisualsWithData(GetSpriteForType);
                yield return new WaitForSeconds(0.3f);

                StartCoroutine(RunGameplayPipelineRoutine(new Vector2Int(-1, -1), new Vector2Int(-1, -1)));

                yield break; 
            }

            MatchResult matchResult = matchSystem.FindMatches(posA, posB);

            if (matchResult.matchedTiles.Count == 0)
            {
                boardView.SwapVisuals(posA, posB);

                ICommand revertCmd = new SwapCommand(boardModel, posA, posB);
                commandSystem.EnqueueCommand(revertCmd);
                commandSystem.ProcessNextCommand();

                yield break;
            }

            // Passing the exact touch coordinates into my pipeline so it knows where to spawn specials!
            StartCoroutine(RunGameplayPipelineRoutine(posA, posB));
        }

        private IEnumerator RunGameplayPipelineRoutine(Vector2Int initialSwapA, Vector2Int initialSwapB)
        {
            bool cascadeHappened = true;

            // Tracking the current swap positions. 
            Vector2Int currentSwapA = initialSwapA;
            Vector2Int currentSwapB = initialSwapB;

            while (cascadeHappened)
            {
                cascadeHappened = false;

                // Using the actual coordinates for the first match, and (-1,-1) for cascades
                MatchResult matchResult = matchSystem.FindMatches(currentSwapA, currentSwapB);

                if (matchResult.matchedTiles.Count > 0)
                {
                    cascadeHappened = true;

                    ICommand destroyCmd = new DestroyCommand(boardModel, matchResult);
                    commandSystem.EnqueueCommand(destroyCmd);
                    commandSystem.ProcessNextCommand();
                    boardView.SyncVisualsWithData(GetSpriteForType);
                    yield return new WaitForSeconds(0.2f);

                    ICommand gravityCmd = new GravityCommand(gravitySystem);
                    commandSystem.EnqueueCommand(gravityCmd);
                    commandSystem.ProcessNextCommand();
                    boardView.SyncVisualsWithData(GetSpriteForType);
                    yield return new WaitForSeconds(0.2f);

                    ICommand spawnCmd = new SpawnCommand(spawnSystem);
                    commandSystem.EnqueueCommand(spawnCmd);
                    commandSystem.ProcessNextCommand();
                    boardView.SyncVisualsWithData(GetSpriteForType);
                    yield return new WaitForSeconds(0.3f);

                    // Resetting the swap coordinates for any chain reactions (cascades)
                    currentSwapA = new Vector2Int(-1, -1);
                    currentSwapB = new Vector2Int(-1, -1);
                }
            }
        }
    }
}