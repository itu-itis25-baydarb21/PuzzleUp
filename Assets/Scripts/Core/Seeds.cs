namespace Match3Engine.Core
{
    public static class Seeds
    {
        // Scrambles a seed before it goes into a random generator.
        //
        // System.Random gives related numbers for related seeds: seeds 1, 2, 3 start out almost
        // in step, and two generators made from the same seed are identical. My seeds are full of
        // such relatives (attempt 1, 2, 3 of one player; the board and the bot of one game), so
        // every generator is created from Mix(seed, salt) with its own salt instead.
        public static int Mix(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)seed * 0x9E3779B9u + (uint)salt * 0x85EBCA6Bu;
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return (int)x;
            }
        }
    }
}
