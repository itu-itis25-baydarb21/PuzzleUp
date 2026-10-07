namespace Match3Engine.Simulation
{
    // What a swap would do the moment it is made, before anything falls
    public struct MovePreview
    {
        // False when the swap is not allowed; the other fields are then all zero
        public bool valid;

        // Every tile the swap destroys straight away
        public int tilesDestroyed;

        // How many of those a goal still needs
        public int goalTilesDestroyed;

        // Power-ups the match would leave behind
        public int powerUpsCreated;
    }
}
