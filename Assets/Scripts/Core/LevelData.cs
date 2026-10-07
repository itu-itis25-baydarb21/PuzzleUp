using UnityEngine;

namespace Match3Engine.Core
{
    // One thing the level asks for: clear this many tiles of this colour
    [System.Serializable]
    public struct LevelGoal
    {
        public TileType type;
        [Min(1)] public int amount;
    }

    // An obstacle the level starts with. Only Crate and Hole are used here.
    [System.Serializable]
    public struct LevelObstacle
    {
        // (0, 0) is the bottom-left slot
        public Vector2Int position;
        public TileType type;
    }

    // Everything that defines a level. This is the data the designer edits.
    [CreateAssetMenu(fileName = "Level", menuName = "Puzzle Up/Level")]
    public class LevelData : ScriptableObject
    {
        [Header("Board")]
        [Range(5, 10)] public int width = 8;
        [Range(5, 10)] public int height = 8;
        public TileType[] availableTileTypes;

        [Tooltip("Crates and holes the board starts with. Every other slot gets a random tile.")]
        public LevelObstacle[] obstacles;

        [Header("Rules")]
        [Min(1)] public int moveLimit = 20;
        public LevelGoal[] goals;
    }
}
