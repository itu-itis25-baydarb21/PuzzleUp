using TMPro;
using UnityEngine;

namespace Match3Engine.Designer
{
    // A number with a minus and a plus button
    public class Stepper : MonoBehaviour
    {
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private UnityEngine.UI.Button minusButton;
        [SerializeField] private UnityEngine.UI.Button plusButton;

        public int min = 0;
        public int max = 10;
        public int step = 1;

        public int Value { get; private set; }

        // Raised only when the player changes the value with the buttons
        public event System.Action<int> Changed;

        private void Awake()
        {
            minusButton.onClick.AddListener(() => ChangeBy(-step));
            plusButton.onClick.AddListener(() => ChangeBy(step));
        }

        private void ChangeBy(int amount)
        {
            int newValue = Mathf.Clamp(Value + amount, min, max);
            if (newValue == Value) return;

            Value = newValue;
            Show();

            if (Changed != null) Changed(Value);
        }

        // Sets the value from code, without raising Changed
        public void SetValue(int value)
        {
            Value = Mathf.Clamp(value, min, max);
            Show();
        }

        // Changes the allowed range and pulls the value back inside it if needed
        public void SetRange(int newMin, int newMax)
        {
            min = newMin;
            max = Mathf.Max(newMin, newMax);
            SetValue(Value);
        }

        private void Show()
        {
            valueText.text = Value.ToString();

            // A button that would do nothing is greyed out
            minusButton.interactable = Value > min;
            plusButton.interactable = Value < max;
        }
    }
}
