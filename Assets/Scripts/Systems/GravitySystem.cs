using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    // A single tile falling from one slot to another in the same column
    public struct TileMove
    {
        public Vector2Int from;
        public Vector2Int to;

        public TileMove(Vector2Int fromPos, Vector2Int toPos)
        {
            from = fromPos;
            to = toPos;
        }
    }

    public class GravitySystem
    {
        private BoardModel board;

        // Every fall from my last gravity pass, in the order it happened, so the view can replay it
        public List<TileMove> LastMoves { get; private set; } = new List<TileMove>();

        public GravitySystem(BoardModel boardModel)
        {
            // I need the board reference to move tiles downwards
            board = boardModel;
        }

        public void ApplyGravity()
        {
            LastMoves.Clear();

            // I'm going through each column from bottom to top
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.GetTile(x, y) == TileType.None)
                    {
                        // Found an empty spot, let's find the first tile above it to pull down
                        for (int aboveY = y + 1; aboveY < board.Height; aboveY++)
                        {
                            TileType tileAbove = board.GetTile(x, aboveY);

                            // A hole is not part of the board, so I look straight past it
                            if (tileAbove == TileType.Hole) continue;

                            if (tileAbove != TileType.None)
                            {
                                // Move the tile down and clear its old spot
                                board.SetTile(x, y, tileAbove);
                                board.SetTile(x, aboveY, TileType.None);
                                LastMoves.Add(new TileMove(new Vector2Int(x, aboveY), new Vector2Int(x, y)));
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
