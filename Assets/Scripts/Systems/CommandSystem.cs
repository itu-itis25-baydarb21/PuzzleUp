using System.Collections.Generic;
using Match3Engine.Commands;

namespace Match3Engine.Systems
{
    // I'm using a queue here to process all my board events sequentially.
    // This makes sure animations and logic don't trip over each other.
    public class CommandSystem
    {
        private Queue<ICommand> commandQueue;

        public CommandSystem()
        {
            // Initializing my empty queue.
            commandQueue = new Queue<ICommand>();
        }

        // Adding a new action to my queue.
        public void EnqueueCommand(ICommand command)
        {
            commandQueue.Enqueue(command);
        }

        // Processing the next action if I have any pending.
        public void ProcessNextCommand()
        {
            if (commandQueue.Count > 0)
            {
                ICommand nextCommand = commandQueue.Dequeue();
                nextCommand.Execute();
            }
        }

        // Quick helper to check if my queue has finished running.
        public bool HasPendingCommands()
        {
            return commandQueue.Count > 0;
        }

        // I might need to clear the queue if the level restarts or ends.
        public void ClearQueue()
        {
            commandQueue.Clear();
        }
    }
}