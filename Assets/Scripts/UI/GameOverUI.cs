using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThiefSimulator.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RestartScene);
            }
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RestartScene);
            }
        }

        public void Show()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        private void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
