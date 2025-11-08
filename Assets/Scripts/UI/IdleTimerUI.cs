using ThiefSimulator.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ThiefSimulator.UI
{
    public class IdleTimerUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Slider _slider;
        [SerializeField] private GameObject _sliderRoot;

        private void Update()
        {
            if (TimeManager.Instance == null)
            {
                SetSliderState(false, 0f);
                return;
            }

            float interval = TimeManager.Instance.IdleAdvanceIntervalSeconds;
            if (interval <= 0f)
            {
                SetSliderState(false, 0f);
                return;
            }

            float remainingSeconds = Mathf.Clamp(TimeManager.Instance.RemainingIdleSeconds, 0f, interval);
            float normalized = interval > 0.01f ? remainingSeconds / interval : 0f;
            SetSliderState(true, normalized);
        }

        private void SetSliderState(bool active, float value)
        {
            if (_sliderRoot != null)
            {
                _sliderRoot.SetActive(active);
            }

            if (_slider != null)
            {
                _slider.value = Mathf.Clamp01(value);
            }
        }

        // Usage in Unity:
        // 1. Create a Slider (0-1 range) and assign it to _slider; optionally assign a parent GameObject to _sliderRoot.
        // 2. Attach this script to any always-active UI controller.
        // 3. Slider will fill proportionally to the remaining idle time before auto-advance.
    }
}
