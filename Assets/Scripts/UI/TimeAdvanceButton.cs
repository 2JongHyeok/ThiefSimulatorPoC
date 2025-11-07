using UnityEngine;
using UnityEngine.UI;
using ThiefSimulator.Managers;

namespace ThiefSimulator.UI
{
    [RequireComponent(typeof(Button))]
    public class TimeAdvanceButton : MonoBehaviour
    {
        [SerializeField, Tooltip("Minutes to add every time the button is clicked.")] private int _minutesPerClick = 1;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(AdvanceTime);
        }

        private void OnDestroy()
        {
            if (TryGetComponent<Button>(out var button))
            {
                button.onClick.RemoveListener(AdvanceTime);
            }
        }

        private void AdvanceTime()
        {
            if (TimeManager.Instance == null)
            {
                Debug.LogWarning("[TimeAdvanceButton] TimeManager not available.");
                return;
            }

            if (_minutesPerClick <= 0)
            {
                Debug.LogWarning("[TimeAdvanceButton] Minutes per click must be positive.");
                return;
            }

            TimeManager.Instance.AdvanceTime(_minutesPerClick);
        }
    }
}
