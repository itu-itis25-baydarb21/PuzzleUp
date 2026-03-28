using Match3Engine.Systems;

namespace Match3Engine.Commands
{
    public class GravityCommand : ICommand
    {
        private GravitySystem gravitySystem;

        public GravityCommand(GravitySystem system)
        {
            gravitySystem = system;
        }

        public void Execute()
        {
            // Applying the downward pull on all tiles in my grid
            gravitySystem.ApplyGravity();
        }
    }
}