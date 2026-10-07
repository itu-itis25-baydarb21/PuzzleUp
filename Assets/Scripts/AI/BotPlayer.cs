using System.Collections.Generic;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Simulation;
using Match3Engine.Systems;

namespace Match3Engine.AI
{
    // Plays the level on screen in place of a human. It waits for the board to settle,
    // thinks for a moment, then sends its swap to the controller like a swipe would.
    public class BotPlayer : MonoBehaviour
    {
        public GameController gameController;

        [Header("Who is playing")]
        public BotStrategy strategy = BotStrategy.GoalAware;
        [Range(0f, 1f)] public float skill = 0.5f;

        [Header("Pacing")]
        [Tooltip("Pause before each move, in seconds, so the game can be followed.")]
        [Min(0f)] public float thinkTime = 0.4f;

        // How fast the game is being shown. 1 is normal speed.
        public float Speed { get; private set; } = 1f;

        private IBot bot;
        private int botSeed;
        private float waited;
        private bool skipRequested;

        private void Update()
        {
            GameSimulation simulation = gameController.Simulation;
            if (simulation == null) return;

            // Never touch the game while the last move is still being animated
            if (gameController.IsBusy)
            {
                waited = 0f;
                return;
            }

            if (simulation.Progress.State != LevelState.Playing) return;

            // A new attempt gets a new bot. Seeding it from the board's seed means the same
            // seed always shows the same game, and matches that seed's game in a batch run.
            if (bot == null || botSeed != gameController.CurrentSeed)
            {
                botSeed = gameController.CurrentSeed;
                bot = BotFactory.Create(strategy, skill, botSeed);
            }

            if (skipRequested)
            {
                FinishOffScreen(simulation);
                return;
            }

            waited += Time.deltaTime;
            if (waited < thinkTime) return;
            waited = 0f;

            List<Move> moves = simulation.GetValidMoves();
            if (moves.Count == 0) return;

            Move move = bot.ChooseMove(simulation, moves);
            gameController.ProcessPlayerSwap(move.a, move.b);
        }

        // Plays the rest of the game instantly, then shows where it ended
        private void FinishOffScreen(GameSimulation simulation)
        {
            skipRequested = false;

            while (simulation.Progress.State == LevelState.Playing)
            {
                List<Move> moves = simulation.GetValidMoves();
                if (moves.Count == 0) break;

                Move move = bot.ChooseMove(simulation, moves);
                simulation.PlayMove(move.a, move.b);
            }

            gameController.ShowSimulationState();
        }

        public void SetSpeed(float speed)
        {
            Speed = speed;
            Time.timeScale = speed;
        }

        // Takes effect as soon as the move on screen has finished
        public void Skip()
        {
            GameSimulation simulation = gameController.Simulation;
            if (simulation == null || simulation.Progress.State != LevelState.Playing) return;

            skipRequested = true;
        }

        public void PlayAgain()
        {
            if (gameController.IsBusy) return;

            skipRequested = false;
            waited = 0f;
            bot = null;
            gameController.RestartLevel();
        }

        // Changes who is playing. Takes effect from the next move.
        public void Configure(BotStrategy newStrategy, float newSkill)
        {
            strategy = newStrategy;
            skill = newSkill;
            bot = null;
            skipRequested = false;
            waited = 0f;
        }

        private void OnEnable()
        {
            Time.timeScale = Speed;
        }

        private void OnDisable()
        {
            // Speed is a global setting, so I put it back when I stop playing
            Time.timeScale = 1f;
        }
    }
}
