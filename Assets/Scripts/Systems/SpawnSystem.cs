using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    public class SpawnSystem
    {
        private BoardModel board;
        private TileType[] availableTypes;

        public SpawnSystem(BoardModel boardModel, TileType[] types)
        {
            // Hooking up my board and the tile types allowed in this level
            board = boardModel;
            availableTypes = types;
        }

        public void SpawnTiles()
        {
            // I need to fill any empty spots left behind by gravity
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.GetTile(x, y) == TileType.None)
                    {
                        // Picking a random tile type from my available list
                        TileType randomType = availableTypes[Random.Range(0, availableTypes.Length)];
                        board.SetTile(x, y, randomType);
                    }
                }
            }
        }
    }
}