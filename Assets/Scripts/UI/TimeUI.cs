using ThiefSimulator.Managers;
using TMPro;
using UnityEngine;

namespace ThiefSimulator.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TimeUI : MonoBehaviour
    {
        private TextMeshProUGUI _timeText;

        private void Awake()
        {
            _timeText = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogError("[TimeUI] TimeManager.Instance not found! Make sure a TimeManager is in the scene.");
                return;
            }

            // Subscribe to future time changes
            TimeManager.Instance.OnTimeChanged += Update_TimeText;

            // Set the initial time immediately
            var (hour, minute) = TimeManager.Instance.GetCurrentTime();
            Update_TimeText(hour, minute);
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent errors when the object is destroyed
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= Update_TimeText;
            }
        }

        private void Update_TimeText(int hour, int minute)
        {
            _timeText.text = $"{hour:D2}:{minute:D2}";
        }
    }
}
