using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    public class SpawnSystem
    {
        private BoardModel board;
        private TileType[] availableTypes;

        // A seeded generator, so the same seed always produces the same tiles
        private System.Random random;

        // The slots I filled in my last spawn pass, column by column from bottom to top
        public List<Vector2Int> LastSpawned { get; private set; } = new List<Vector2Int>();

        private List<TileType> candidates = new List<TileType>();

        public SpawnSystem(BoardModel boardModel, TileType[] types, System.Random seededRandom)
        {
            // Hooking up my board and the tile types allowed in this level
            board = boardModel;
            availableTypes = types;
            random = seededRandom;
        }

        public void SpawnTiles()
        {
            LastSpawned.Clear();

            // I need to fill any empty spots left behind by gravity
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.GetTile(x, y) == TileType.None)
                    {
                        // Picking a random tile type from my available list
                        TileType randomType = availableTypes[random.Next(availableTypes.Length)];
                        board.SetTile(x, y, randomType);
                        LastSpawned.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        // Deals random tiles over the whole board, never placing a third tile in a row.
        // Used for the starting board so the level does not begin with free matches.
        // Crates and holes already on the board are left where they are.
        public void FillWithoutMatches()
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    TileType current = board.GetTile(x, y);
                    if (current == TileType.Crate || current == TileType.Hole) continue;

                    candidates.Clear();

                    foreach (TileType type in availableTypes)
                    {
                        bool completesRow = board.GetTile(x - 1, y) == type && board.GetTile(x - 2, y) == type;
                        bool completesColumn = board.GetTile(x, y - 1) == type && board.GetTile(x, y - 2) == type;

                        if (!completesRow && !completesColumn) candidates.Add(type);
                    }

                    // With only one or two colours there may be no safe choice, so I take any
                    TileType chosen = candidates.Count > 0
                        ? candidates[random.Next(candidates.Count)]
                        : availableTypes[random.Next(availableTypes.Length)];

                    board.SetTile(x, y, chosen);
                }
            }
        }
    }
}
