using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.Commands
{
    public class SwapCommand : ICommand
    {
        private BoardModel board;
        private Vector2Int posA;
        private Vector2Int posB;

        public SwapCommand(BoardModel boardModel, Vector2Int a, Vector2Int b)
        {
            // Storing my board and the positions I want to swap
            board = boardModel;
            posA = a;
            posB = b;
        }

        public void Execute()
        {
            // Getting the current tile types
            TileType typeA = board.GetTile(posA.x, posA.y);
            TileType typeB = board.GetTile(posB.x, posB.y);

            // Executing the swap on my data grid
            board.SetTile(posA.x, posA.y, typeB);
            board.SetTile(posB.x, posB.y, typeA);
        }
    }
}