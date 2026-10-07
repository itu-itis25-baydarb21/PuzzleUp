namespace Match3Engine.Core
{
    public enum TileType
    {
        None,
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Pink,

        RocketHorizontal,
        RocketVertical,
        TNT,
        ColorBomb,

        // Obstacles. New values go at the end so saved levels and scenes keep their numbers.

        // Cannot be swapped or matched. Falls like a tile, and breaks when a match
        // is made next to it or an explosion reaches it.
        Crate,

        // A slot that is not part of the board. Never holds a tile; tiles fall straight past it.
        Hole
    }
}
