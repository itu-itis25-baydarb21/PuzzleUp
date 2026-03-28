using UnityEngine;
using Match3Engine.Core;

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

            // Spawning the initial visual grid based on my data model dimensions
            for (int x = 0; x < model.Width; x++)
            {
                for (int y = 0; y < model.Height; y++)
                {
                    Vector2 pos = new Vector2(x, y);
                    GameObject newTile = Instantiate(tilePrefab, pos, Quaternion.identity, boardParent);
                    tileViews[x, y] = newTile.GetComponent<TileView>();
                }
            }
        }

        // I'll call this to sync visuals after my command queue finishes processing
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