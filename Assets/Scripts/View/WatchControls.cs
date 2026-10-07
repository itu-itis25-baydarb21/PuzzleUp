using System.Globalization;
using TMPro;
using UnityEngine;
using Match3Engine.AI;

namespace Match3Engine.View
{
    // The buttons for watching a bot play: speed, skip to the end, and play again
    public class WatchControls : MonoBehaviour
    {
        [SerializeField] private BotPlayer botPlayer;

        [Header("Speed")]
        [SerializeField] private UnityEngine.UI.Button[] speedButtons;
        [SerializeField] private float[] speeds;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f);

        [Header("Other")]
        [SerializeField] private UnityEngine.UI.Button skipButton;
        [SerializeField] private UnityEngine.UI.Button againButton;
        [SerializeField] private TMP_Text botLabel;

        private void Awake()
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                // Each button needs its own copy of the speed for the click handler
                float speed = speeds[i];
                speedButtons[i].onClick.AddListener(() => SelectSpeed(speed));
            }

            skipButton.onClick.AddListener(botPlayer.Skip);
            againButton.onClick.AddListener(botPlayer.PlayAgain);
        }

        private void Start()
        {
            Refresh();
        }

        // Call after the bot or its speed was changed from somewhere else
        public void Refresh()
        {
            botLabel.text = "Bot: " + botPlayer.strategy + ", skill " + botPlayer.skill.ToString("0.00", CultureInfo.InvariantCulture);
            HighlightSpeed(botPlayer.Speed);
        }

        private void SelectSpeed(float speed)
        {
            botPlayer.SetSpeed(speed);
            HighlightSpeed(speed);
        }

        private void HighlightSpeed(float currentSpeed)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                bool selected = Mathf.Approximately(speeds[i], currentSpeed);
                speedButtons[i].image.color = selected ? selectedColor : normalColor;
            }
        }
    }
}
