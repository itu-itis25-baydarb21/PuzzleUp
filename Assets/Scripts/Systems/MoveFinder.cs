using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    // A swap between two neighbouring slots
    public struct Move
    {
        public Vector2Int a;
        public Vector2Int b;

        public Move(Vector2Int posA, Vector2Int posB)
        {
            a = posA;
            b = posB;
        }
    }

    // Decides which swaps are allowed. The game and the bots both ask this, so they always agree.
    public class MoveFinder
    {
        private BoardModel board;
        private MatchSystem matchSystem;
        private PowerUpSystem powerUpSystem;

        public MoveFinder(BoardModel boardModel, MatchSystem matches, PowerUpSystem powerUps)
        {
            board = boardModel;
            matchSystem = matches;
            powerUpSystem = powerUps;
        }

        // A swap is valid when the two slots are neighbours and swapping them either
        // triggers a power-up or puts one of the two tiles into a line of 3
        public bool IsValidMove(Vector2Int a, Vector2Int b)
        {
            if (!board.IsValidPosition(a.x, a.y) || !board.IsValidPosition(b.x, b.y)) return false;
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) return false;

            TileType typeA = board.GetTile(a.x, a.y);
            TileType typeB = board.GetTile(b.x, b.y);

            if (typeA == TileType.None || typeB == TileType.None) return false;
            if (powerUpSystem.IsPowerUp(typeA) || powerUpSystem.IsPowerUp(typeB)) return true;
            if (typeA == typeB) return false;

            // Trying the swap on my data grid, checking, then putting it back
            board.SetTile(a.x, a.y, typeB);
            board.SetTile(b.x, b.y, typeA);

            bool createsMatch = matchSystem.HasMatchAt(a) || matchSystem.HasMatchAt(b);

            board.SetTile(a.x, a.y, typeA);
            board.SetTile(b.x, b.y, typeB);

            return createsMatch;
        }

        // Every valid swap on the board, each listed once
        public List<Move> FindValidMoves()
        {
            List<Move> moves = new List<Move>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Vector2Int right = new Vector2Int(x + 1, y);
                    Vector2Int up = new Vector2Int(x, y + 1);

                    if (IsValidMove(pos, right)) moves.Add(new Move(pos, right));
                    if (IsValidMove(pos, up)) moves.Add(new Move(pos, up));
                }
            }

            return moves;
        }

        public bool HasValidMove()
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    if (IsValidMove(pos, new Vector2Int(x + 1, y))) return true;
                    if (IsValidMove(pos, new Vector2Int(x, y + 1))) return true;
                }
            }

            return false;
        }
    }
}
