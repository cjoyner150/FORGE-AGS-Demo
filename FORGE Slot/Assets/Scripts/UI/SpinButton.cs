using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FORGE
{
    public class SpinButton : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonLabel;

        private bool _gameOver;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnSpinClicked);

            gameManager.OnOutOfCredits += () => {
                _gameOver = true;
                if (button      != null) button.interactable = false;
                if (buttonLabel != null) buttonLabel.text    = "GAME OVER";
            };
        }

        // Poll CanSpin every frame — simple and reliable
        private void Update()
        {
            if (_gameOver) return;
            if (button != null)
                button.interactable = gameManager.CanSpin;
        }

        private void OnSpinClicked()
        {
            gameManager.RequestSpin();
        }
    }
}