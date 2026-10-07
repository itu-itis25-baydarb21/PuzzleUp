using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Systems;

namespace Match3Engine.Simulation
{
    // A tile at a slot, with the type it had or was given
    public struct PlacedTile
    {
        public Vector2Int position;
        public TileType type;

        public PlacedTile(Vector2Int pos, TileType tileType)
        {
            position = pos;
            type = tileType;
        }
    }

    // One round of destroy -> fall -> refill. A turn has one of these per cascade.
    public class CascadeStep
    {
        // What was destroyed, with the type each tile had
        public List<PlacedTile> destroyed = new List<PlacedTile>();

        // Power-ups created in place of a destroyed tile
        public List<PlacedTile> specials = new List<PlacedTile>();

        // Surviving tiles falling into the gaps, in the order they happened
        public List<TileMove> falls = new List<TileMove>();

        // New tiles and the slots they end up in, column by column from bottom to top
        public List<PlacedTile> spawns = new List<PlacedTile>();
    }

    // Everything that happened in one move. The simulation has already applied all of it;
    // the view replays it step by step so it can be animated.
    public class TurnResult
    {
        // False when the swap was rejected. Nothing changed and no move was spent.
        public bool valid;

        public List<CascadeStep> steps = new List<CascadeStep>();

        // True when the board ran out of moves afterwards and was shuffled
        public bool shuffled;
    }
}
