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
        [Header("Idle Auto Advance")]
        [Tooltip("Real-time seconds that must pass before an idle minute is added automatically.")]
        [SerializeField] private float _idleAdvanceIntervalSeconds = 5f;

        private float _idleTimer;

        public event Action<int, int> OnTimeChanged;

        public int TotalMinutes => _totalMinutes;
        public float IdleAdvanceIntervalSeconds => _idleAdvanceIntervalSeconds;
        public float IdleTimer => _idleTimer;
        public float RemainingIdleSeconds => _idleAdvanceIntervalSeconds <= 0f ? -1f : Mathf.Max(0f, _idleAdvanceIntervalSeconds - _idleTimer);

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

        private void Update()
        {
            HandleIdleAdvance();
        }

        public void AdvanceTime(int minutes)
        {
            if (minutes <= 0) return;
            _totalMinutes += minutes;
            BroadcastTime();
        }

        public void AdvanceTimeGradually(int totalMinutes, float intervalInSeconds, Action onComplete, bool resetIdleEachTick = false)
        {
            StartCoroutine(AdvanceTimeRoutine(totalMinutes, intervalInSeconds, onComplete, resetIdleEachTick));
        }

        public void ResetIdleTimer()
        {
            _idleTimer = 0f;
        }

        private IEnumerator AdvanceTimeRoutine(int totalMinutes, float intervalInSeconds, Action onComplete, bool resetIdleEachTick)
        {
            for (int i = 0; i < totalMinutes; i++)
            {
                if (resetIdleEachTick)
                {
                    ResetIdleTimer();
                }
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

        private void HandleIdleAdvance()
        {
            if (_idleAdvanceIntervalSeconds <= 0f) { return; }

            _idleTimer += Time.deltaTime;
            if (_idleTimer < _idleAdvanceIntervalSeconds) { return; }

            int minutesToAdd = Mathf.FloorToInt(_idleTimer / _idleAdvanceIntervalSeconds);
            _idleTimer -= minutesToAdd * _idleAdvanceIntervalSeconds;

            for (int i = 0; i < minutesToAdd; i++)
            {
                AdvanceTime(1);
            }
        }

        public (int, int) GetCurrentTime()
        {
            int currentHour = (_totalMinutes / 60) % 24;
            int currentMinute = _totalMinutes % 60;
            return (currentHour, currentMinute);
        }

        // Usage in Unity:
        // 1. Place TimeManager once in the scene and configure the idle interval if needed.
        // 2. Other systems can call AdvanceTime / AdvanceTimeGradually and ResetIdleTimer when the player acts.
    }
}
