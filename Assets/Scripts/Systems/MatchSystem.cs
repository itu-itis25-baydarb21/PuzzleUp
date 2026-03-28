using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    public class MatchResult
    {
        public HashSet<Vector2Int> matchedTiles = new HashSet<Vector2Int>();
        public Dictionary<Vector2Int, TileType> specialsToCreate = new Dictionary<Vector2Int, TileType>();
    }

    public class MatchSystem
    {
        private BoardModel board;

        public MatchSystem(BoardModel boardModel)
        {
            board = boardModel;
        }

        private bool IsBaseColor(TileType type)
        {
            return type >= TileType.Red && type <= TileType.Pink;
        }

        public MatchResult FindMatches(Vector2Int swapPosA, Vector2Int swapPosB)
        {
            MatchResult result = new MatchResult();
            List<HashSet<Vector2Int>> matchGroups = new List<HashSet<Vector2Int>>();

            //for finding horizontal matches
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width - 2; x++)
                {
                    TileType current = board.GetTile(x, y);
                    if (!IsBaseColor(current)) continue;

                    if (board.GetTile(x + 1, y) == current && board.GetTile(x + 2, y) == current)
                    {
                        HashSet<Vector2Int> group = new HashSet<Vector2Int> {
                            new Vector2Int(x, y), new Vector2Int(x + 1, y), new Vector2Int(x + 2, y)
                        };

                        // 3'ten fazla var mı diye sağa doğru kontrol et
                        int nextX = x + 3;
                        while (nextX < board.Width && board.GetTile(nextX, y) == current)
                        {
                            group.Add(new Vector2Int(nextX, y));
                            nextX++;
                        }
                        matchGroups.Add(group);
                        x = nextX - 1;
                    }
                }
            }

            //for finding vertical matches
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height - 2; y++)
                {
                    TileType current = board.GetTile(x, y);
                    if (!IsBaseColor(current)) continue;

                    if (board.GetTile(x, y + 1) == current && board.GetTile(x, y + 2) == current)
                    {
                        HashSet<Vector2Int> group = new HashSet<Vector2Int> {
                            new Vector2Int(x, y), new Vector2Int(x, y + 1), new Vector2Int(x, y + 2)
                        };

                        int nextY = y + 3;
                        while (nextY < board.Height && board.GetTile(x, nextY) == current)
                        {
                            group.Add(new Vector2Int(x, nextY));
                            nextY++;
                        }
                        matchGroups.Add(group);
                        y = nextY - 1;
                    }
                }
            }

            //merge the finalized groups
            bool merged;
            do
            {
                merged = false;
                for (int i = 0; i < matchGroups.Count; i++)
                {
                    for (int j = i + 1; j < matchGroups.Count; j++)
                    {
                        if (matchGroups[i].Overlaps(matchGroups[j]))
                        {
                            matchGroups[i].UnionWith(matchGroups[j]);
                            matchGroups.RemoveAt(j);
                            merged = true;
                            break;
                        }
                    }
                    if (merged) break;
                }
            } while (merged);

            //analyze the groups and determine the special groups (L and T)
            foreach (var group in matchGroups)
            {
                result.matchedTiles.UnionWith(group);

                int minX = group.Min(p => p.x);
                int maxX = group.Max(p => p.x);
                int minY = group.Min(p => p.y);
                int maxY = group.Max(p => p.y);

                int width = maxX - minX + 1;
                int height = maxY - minY + 1;
                int count = group.Count;

                TileType specialType = TileType.None;

                //merge rules
                if (count >= 5)
                {
                    if (width >= 5 || height >= 5) specialType = TileType.ColorBomb; // 5 union for color bomb
                    else specialType = TileType.TNT; // L or T union (in 3x3 area)
                }
                else if (count == 4)
                {
                    if (width == 4) specialType = TileType.RocketHorizontal;
                    else if (height == 4) specialType = TileType.RocketVertical;
                }

                if (specialType != TileType.None)
                {
                    /* We create the special tile where the player touched.
                     * If the match is in cascade situtation the special tile will create on most intersection point.
                     */
                    Vector2Int spawnPos = group.First();
                    if (group.Contains(swapPosA)) spawnPos = swapPosA;
                    else if (group.Contains(swapPosB)) spawnPos = swapPosB;
                    else
                    {
                        int maxNeighbors = -1;
                        foreach (var p in group)
                        {
                            int neighbors = 0;
                            if (group.Contains(new Vector2Int(p.x + 1, p.y))) neighbors++;
                            if (group.Contains(new Vector2Int(p.x - 1, p.y))) neighbors++;
                            if (group.Contains(new Vector2Int(p.x, p.y + 1))) neighbors++;
                            if (group.Contains(new Vector2Int(p.x, p.y - 1))) neighbors++;

                            if (neighbors > maxNeighbors)
                            {
                                maxNeighbors = neighbors;
                                spawnPos = p;
                            }
                        }
                    }
                    result.specialsToCreate[spawnPos] = specialType;
                }
            }
            return result;
        }
    }
}