using Match3Engine.Systems;

namespace Match3Engine.Commands
{
    public class SpawnCommand : ICommand
    {
        private SpawnSystem spawnSystem;

        public SpawnCommand(SpawnSystem system)
        {
            // Passing my spawn system reference
            spawnSystem = system;
        }

        public void Execute()
        {
            // Filling the board with brand new tiles
            spawnSystem.SpawnTiles();
        }
    }
}