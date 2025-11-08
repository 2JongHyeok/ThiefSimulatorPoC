using ThiefSimulator.Player;
using UnityEngine;

namespace ThiefSimulator.CameraSystems
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Follow Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        [Header("Smoothing")]
        [SerializeField, Range(0f, 20f)] private float _followSpeed = 10f;

        private void Awake()
        {
            if (_target == null)
            {
                PlayerData player = FindObjectOfType<PlayerData>();
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) { return; }

            Vector3 desiredPosition = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * _followSpeed);
        }

        // Usage in Unity:
        // 1. Attach this script to the Main Camera.
        // 2. Assign the player's transform to Target (auto-detected if left empty).
        // 3. Adjust Offset and Follow Speed to taste.
    }
}
