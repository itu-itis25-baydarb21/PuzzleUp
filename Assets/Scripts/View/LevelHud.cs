using System.Text;
using TMPro;
using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.View
{
    // Shows the moves left, the goals and the final result of the level
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text goalsText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;

        private StringBuilder goalsBuilder = new StringBuilder();

        // My controller calls this whenever the moves, goals or state change
        public void Refresh(LevelProgress progress)
        {
            movesText.text = "Moves: " + progress.MovesLeft;

            goalsBuilder.Clear();
            foreach (LevelGoal goal in progress.Goals)
            {
                if (goalsBuilder.Length > 0) goalsBuilder.Append("   ");

                int remaining = progress.GetRemaining(goal.type);
                goalsBuilder.Append(goal.type).Append(": ");
                goalsBuilder.Append(remaining > 0 ? remaining.ToString() : "Done");
            }
            goalsText.text = goalsBuilder.ToString();

            resultPanel.SetActive(progress.State != LevelState.Playing);
            if (progress.State == LevelState.Won) resultText.text = "Level Complete!";
            else if (progress.State == LevelState.Lost) resultText.text = "Out of Moves";
        }
    }
}
