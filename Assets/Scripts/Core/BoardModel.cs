namespace Match3Engine.Core
{
    // The Data Grid represents the real game state, keeping it completely separated from the view layer
    public class BoardModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        // This 2D array holds the tile types and board state
        private TileType[,] grid;

        public BoardModel(int width, int height)
        {
            Width = width;
            Height = height;

            // Initialize the grid based on the given dimensions
            grid = new TileType[width, height];

            InitializeEmptyBoard();
        }

        // Fill the board with empty slots to start clean
        private void InitializeEmptyBoard()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    grid[x, y] = TileType.None;
                }
            }
        }

        // Helper to safely get a tile type from the grid
        public TileType GetTile(int x, int y)
        {
            if (IsValidPosition(x, y))
                return grid[x, y];

            return TileType.None;
        }

        // Helper to safely set a tile type on the grid
        public void SetTile(int x, int y, TileType type)
        {
            if (IsValidPosition(x, y))
                grid[x, y] = type;
        }

        // Checking bounds to prevent out of range exceptions during cascades or swaps
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }
}