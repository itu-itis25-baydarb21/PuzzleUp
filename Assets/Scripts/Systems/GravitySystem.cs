using Match3Engine.Core;

namespace Match3Engine.Systems
{
    public class GravitySystem
    {
        private BoardModel board;

        public GravitySystem(BoardModel boardModel)
        {
            // I need the board reference to move tiles downwards
            board = boardModel;
        }

        public void ApplyGravity()
        {
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
                            if (tileAbove != TileType.None)
                            {
                                // Move the tile down and clear its old spot
                                board.SetTile(x, y, tileAbove);
                                board.SetTile(x, aboveY, TileType.None);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}