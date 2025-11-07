using System.Collections;
using System.Collections.Generic;
using ThiefSimulator.Managers;
using ThiefSimulator.Player;
using UnityEngine;

namespace ThiefSimulator.Police
{
    [DefaultExecutionOrder(-150)]
    public class PoliceManager : MonoBehaviour
    {
        public static PoliceManager Instance { get; private set; }

        [SerializeField] private PlayerData _playerData;

        private readonly List<PoliceOfficerController> _officers = new();
        private int _lastProcessedMinute = -1;
        private Coroutine _subscriptionRoutine;
        private Vector2Int _lastDetectionTile = new Vector2Int(int.MinValue, int.MinValue);
        private int _lastDetectionMinute = -1;

        public PlayerData PlayerData => _playerData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_playerData == null)
            {
                _playerData = FindObjectOfType<PlayerData>();
            }
        }

        private void OnEnable()
        {
            TrySubscribeToTime();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
            }

            if (_subscriptionRoutine != null)
            {
                StopCoroutine(_subscriptionRoutine);
                _subscriptionRoutine = null;
            }
        }

        public void RegisterOfficer(PoliceOfficerController officer)
        {
            if (!_officers.Contains(officer))
            {
                _officers.Add(officer);
            }
        }

        public void UnregisterOfficer(PoliceOfficerController officer)
        {
            if (_officers.Contains(officer))
            {
                _officers.Remove(officer);
            }
        }

        public void ReportDetection(Vector2Int detectionTile)
        {
            if (TimeManager.Instance == null) { return; }
            int currentMinute = TimeManager.Instance.TotalMinutes;

            if (currentMinute == _lastDetectionMinute && detectionTile == _lastDetectionTile)
            {
                return; // Already broadcast this minute.
            }

            _lastDetectionMinute = currentMinute;
            _lastDetectionTile = detectionTile;

            foreach (PoliceOfficerController officer in _officers)
            {
                officer.OnPlayerDetected(detectionTile, currentMinute);
            }
        }

        public void NotifyPlayerCaught(PoliceOfficerController officer)
        {
            Debug.LogWarning($"[PoliceManager] Player was caught by {officer.name}. Triggering fail state.");
            // TODO: Hook up to a dedicated game-over / fail-state manager.
        }

        private void TrySubscribeToTime()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
                TimeManager.Instance.OnTimeChanged += HandleTimeChanged;
                _lastProcessedMinute = TimeManager.Instance.TotalMinutes;
            }
            else if (_subscriptionRoutine == null)
            {
                _subscriptionRoutine = StartCoroutine(WaitForTimeManager());
            }
        }

        private IEnumerator WaitForTimeManager()
        {
            while (TimeManager.Instance == null)
            {
                yield return null;
            }
            _subscriptionRoutine = null;
            TrySubscribeToTime();
        }

        private void HandleTimeChanged(int hour, int minute)
        {
            if (TimeManager.Instance == null) { return; }
            int totalMinutes = TimeManager.Instance.TotalMinutes;
            if (_lastProcessedMinute < 0)
            {
                _lastProcessedMinute = totalMinutes;
                return;
            }

            int delta = totalMinutes - _lastProcessedMinute;
            if (delta <= 0) { return; }

            for (int i = 1; i <= delta; i++)
            {
                int absoluteMinute = _lastProcessedMinute + i;
                foreach (PoliceOfficerController officer in _officers)
                {
                    officer.TickMinute(absoluteMinute);
                }
            }

            _lastProcessedMinute = totalMinutes;
        }
    }
}
