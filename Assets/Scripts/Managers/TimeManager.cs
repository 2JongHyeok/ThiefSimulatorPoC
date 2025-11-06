using System;
using System.Collections;
using UnityEngine;

namespace ThiefSimulator.Managers
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Tooltip("The starting hour of the game (0-23).")]
        [SerializeField] private int _startingHour = 8;

        private int _totalMinutes;

        public event Action<int, int> OnTimeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _totalMinutes = _startingHour * 60;
        }

        private void Start()
        {
            BroadcastTime();
        }

        public void AdvanceTime(int minutes)
        {
            if (minutes <= 0) return;
            _totalMinutes += minutes;
            BroadcastTime();
        }

        public void AdvanceTimeGradually(int totalMinutes, float intervalInSeconds, Action onComplete)
        {
            StartCoroutine(AdvanceTimeRoutine(totalMinutes, intervalInSeconds, onComplete));
        }

        private IEnumerator AdvanceTimeRoutine(int totalMinutes, float intervalInSeconds, Action onComplete)
        {
            for (int i = 0; i < totalMinutes; i++)
            {
                AdvanceTime(1);
                yield return new WaitForSeconds(intervalInSeconds);
            }

            onComplete?.Invoke();
        }

        private void BroadcastTime()
        {
            int currentHour = (_totalMinutes / 60) % 24;
            int currentMinute = _totalMinutes % 60;
            OnTimeChanged?.Invoke(currentHour, currentMinute);
        }

        public (int, int) GetCurrentTime()
        {
            int currentHour = (_totalMinutes / 60) % 24;
            int currentMinute = _totalMinutes % 60;
            return (currentHour, currentMinute);
        }
    }
}
