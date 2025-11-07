using TMPro;
using ThiefSimulator.Managers;
using UnityEngine;

namespace ThiefSimulator.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class IdleTimerUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private string _activeFormat = "Auto +1m in {0:0.0}s";
        [SerializeField] private string _disabledText = "Auto advance disabled";
        [SerializeField] private string _missingTimeManagerText = "No TimeManager";

        private TextMeshProUGUI _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (_label == null)
            {
                return;
            }

            if (TimeManager.Instance == null)
            {
                _label.text = _missingTimeManagerText;
                return;
            }

            float interval = TimeManager.Instance.IdleAdvanceIntervalSeconds;
            if (interval <= 0f)
            {
                _label.text = _disabledText;
                return;
            }

            float remainingSeconds = Mathf.Clamp(TimeManager.Instance.RemainingIdleSeconds, 0f, interval);
            _label.text = string.Format(_activeFormat, remainingSeconds);
        }

        // Usage in Unity:
        // 1. Add this script to a TextMeshProUGUI object in the HUD.
        // 2. Optionally tweak the text format strings for your desired UI copy.
        // 3. Ensure a TimeManager exists in the scene so the label can query the idle countdown.
    }
}
