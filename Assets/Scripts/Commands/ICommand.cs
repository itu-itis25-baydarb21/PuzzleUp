namespace Match3Engine.Commands
{
    // My base interface for all gameplay actions to keep things deterministic.
    // Every action on the board will implement this.
    public interface ICommand
    {
        void Execute();
    }
}