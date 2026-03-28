using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Systems;

namespace Match3Engine.Commands
{
    public class DestroyCommand : ICommand
    {
        private BoardModel board;
        private MatchResult matchResult;

        public DestroyCommand(BoardModel boardModel, MatchResult result)
        {
            board = boardModel;
            matchResult = result;
        }

        public void Execute()
        {
            foreach (Vector2Int pos in matchResult.matchedTiles)
            {
                board.SetTile(pos.x, pos.y, TileType.None);
            }

            foreach (var kvp in matchResult.specialsToCreate)
            {
                board.SetTile(kvp.Key.x, kvp.Key.y, kvp.Value);
            }
        }
    }
}