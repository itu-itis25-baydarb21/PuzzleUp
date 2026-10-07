using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.View
{
    // My view grid that visualizes the data grid.
    public class BoardView : MonoBehaviour
    {
        [Header("References")]
        public GameObject tilePrefab;
        public Transform boardParent;

        // This is my visual grid mirroring the data grid structure
        private TileView[,] tileViews;
        private BoardModel dataModel;

        public void InitializeBoard(BoardModel model)
        {
            dataModel = model;
            tileViews = new TileView[model.Width, model.Height];

            CreateBoardMask(model.Width, model.Height);

            // Spawning the initial visual grid based on my data model dimensions
            for (int x = 0; x < model.Width; x++)
            {
                for (int y = 0; y < model.Height; y++)
                {
                    Vector2 pos = new Vector2(x, y);
                    GameObject newTile = Instantiate(tilePrefab, pos, Quaternion.identity, boardParent);
                    tileViews[x, y] = newTile.GetComponent<TileView>();

                    // New tiles wait above the board, so they must stay hidden until they fall inside it
                    tileViews[x, y].spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
        }

        // A mask the exact size of the board, so tiles are only drawn while they are inside it
        private void CreateBoardMask(int width, int height)
        {
            Texture2D maskTexture = Texture2D.whiteTexture;
            Sprite maskSprite = Sprite.Create(
                maskTexture,
                new Rect(0, 0, maskTexture.width, maskTexture.height),
                new Vector2(0.5f, 0.5f),
                maskTexture.width);

            GameObject maskObject = new GameObject("BoardMask");
            maskObject.transform.SetParent(boardParent, false);
            maskObject.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, 0f);
            maskObject.transform.localScale = new Vector3(width, height, 1f);
            maskObject.AddComponent<SpriteMask>().sprite = maskSprite;
        }

        // True while any tile is still falling
        public bool IsDropping
        {
            get
            {
                foreach (TileView view in tileViews)
                {
                    if (view.IsDropping) return true;
                }
                return false;
            }
        }

        // Replaying the gravity pass: each surviving tile falls into the empty slot below it
        public void DropTiles(List<TileMove> moves)
        {
            foreach (TileMove move in moves)
            {
                TileView fallingView = tileViews[move.from.x, move.from.y];
                TileView emptyView = tileViews[move.to.x, move.to.y];

                tileViews[move.to.x, move.to.y] = fallingView;
                tileViews[move.from.x, move.from.y] = emptyView;

                // The empty view has no sprite, so it can jump to its new slot unnoticed
                emptyView.SnapToPosition(new Vector2(move.from.x, move.from.y));
                fallingView.DropToPosition(new Vector2(move.to.x, move.to.y));
            }
        }

        // Destroyed tiles just lose their sprite. Their views stay in the grid to be reused.
        public void ClearTiles(List<PlacedTile> destroyedTiles)
        {
            foreach (PlacedTile destroyed in destroyedTiles)
            {
                tileViews[destroyed.position.x, destroyed.position.y].UpdateVisuals(TileType.None, null);
            }
        }

        // Showing a tile that appears in place, like a power-up created by a match
        public void PlaceTiles(List<PlacedTile> placedTiles, System.Func<TileType, Sprite> getSpriteFunc)
        {
            foreach (PlacedTile placed in placedTiles)
            {
                tileViews[placed.position.x, placed.position.y].UpdateVisuals(placed.type, getSpriteFunc(placed.type));
            }
        }

        // Newly spawned tiles start stacked above the board and fall into their slots
        public void DropNewTiles(List<PlacedTile> spawnedTiles, System.Func<TileType, Sprite> getSpriteFunc)
        {
            int[] stackedInColumn = new int[dataModel.Width];

            foreach (PlacedTile spawned in spawnedTiles)
            {
                Vector2Int slot = spawned.position;
                TileView view = tileViews[slot.x, slot.y];

                view.UpdateVisuals(spawned.type, getSpriteFunc(spawned.type));
                view.SnapToPosition(new Vector2(slot.x, dataModel.Height + stackedInColumn[slot.x]));
                view.DropToPosition(new Vector2(slot.x, slot.y));

                stackedInColumn[slot.x]++;
            }
        }

        // Full repaint straight from the data grid. Only correct while nothing is animating.
        public void SyncVisualsWithData(System.Func<TileType, Sprite> getSpriteFunc)
        {
            for (int x = 0; x < dataModel.Width; x++)
            {
                for (int y = 0; y < dataModel.Height; y++)
                {
                    TileType currentType = dataModel.GetTile(x, y);
                    Sprite targetSprite = getSpriteFunc(currentType);

                    // Updating each visual tile to match my data grid
                    tileViews[x, y].UpdateVisuals(currentType, targetSprite);
                    tileViews[x, y].MoveToPosition(new Vector2(x, y));
                }
            }
        }
        public void CenterAndScaleCamera(int width, int height)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;


            float centerX = (width - 1) / 2f;
            float centerY = (height - 1) / 2f;

            mainCam.transform.position = new Vector3(centerX, centerY, -10f);

            float padding = 2f; 
            float screenAspect = (float)Screen.width / Screen.height;

            float requiredSizeX = (width + padding) / (2f * screenAspect);
            float requiredSizeY = (height + padding) / 2f;

            mainCam.orthographicSize = Mathf.Max(requiredSizeX, requiredSizeY);
        }

        public void SwapVisuals(Vector2Int posA, Vector2Int posB)
        {
            // Swapping the references in my visual grid so they physically cross paths
            TileView viewA = tileViews[posA.x, posA.y];
            TileView viewB = tileViews[posB.x, posB.y];

            tileViews[posA.x, posA.y] = viewB;
            tileViews[posB.x, posB.y] = viewA;

            // Telling them to move to their new grid slots
            tileViews[posA.x, posA.y].MoveToPosition(new Vector2(posA.x, posA.y));
            tileViews[posB.x, posB.y].MoveToPosition(new Vector2(posB.x, posB.y));
        }
    }
}