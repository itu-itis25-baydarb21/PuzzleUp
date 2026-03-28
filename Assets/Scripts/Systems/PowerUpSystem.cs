using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    // Calculates the Area of Effect (AoE) for my special tiles.
    public class PowerUpSystem
    {
        private BoardModel board;

        public PowerUpSystem(BoardModel boardModel)
        {
            board = boardModel;
        }

        public bool IsPowerUp(TileType type)
        {
            return type == TileType.RocketHorizontal || type == TileType.RocketVertical ||
                   type == TileType.TNT || type == TileType.ColorBomb;
        }

        // Returns a list of all grid coordinates that should be destroyed by this powerup
        public HashSet<Vector2Int> GetExplosionArea(Vector2Int pos, TileType type, TileType swappedColor = TileType.None)
        {
            HashSet<Vector2Int> area = new HashSet<Vector2Int>();

            // Always destroy the powerup itself
            area.Add(pos);

            if (type == TileType.RocketHorizontal)
            {
                // Destroy the entire row (all X coordinates for this Y)
                for (int x = 0; x < board.Width; x++)
                    area.Add(new Vector2Int(x, pos.y));
            }
            else if (type == TileType.RocketVertical)
            {
                // Destroy the entire column (all Y coordinates for this X)
                for (int y = 0; y < board.Height; y++)
                    area.Add(new Vector2Int(pos.x, y));
            }
            else if (type == TileType.TNT)
            {
                // Destroy a 3x3 area around the TNT
                for (int x = pos.x - 1; x <= pos.x + 1; x++)
                {
                    for (int y = pos.y - 1; y <= pos.y + 1; y++)
                    {
                        // Make sure we don't go outside the board limits
                        if (x >= 0 && x < board.Width && y >= 0 && y < board.Height)
                            area.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (type == TileType.ColorBomb && swappedColor != TileType.None)
            {
                // Destroy ALL tiles on the board that match the color we swapped with
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (board.GetTile(x, y) == swappedColor)
                            area.Add(new Vector2Int(x, y));
                    }
                }
            }

            return area;
        }
    }
}