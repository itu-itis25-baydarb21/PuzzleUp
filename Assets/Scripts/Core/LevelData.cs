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

    // Everything that defines a level. This is the data the designer edits.
    [CreateAssetMenu(fileName = "Level", menuName = "Puzzle Up/Level")]
    public class LevelData : ScriptableObject
    {
        [Header("Board")]
        [Range(5, 10)] public int width = 8;
        [Range(5, 10)] public int height = 8;
        public TileType[] availableTileTypes;

        [Header("Rules")]
        [Min(1)] public int moveLimit = 20;
        public LevelGoal[] goals;
    }
}
