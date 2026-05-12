using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FORGE
{
    public class SpinButton : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button button;
        [SerializeField] private Sprite activeButtonSprite;
        [SerializeField] private Sprite inactiveButtonSprite;

        private bool _gameOver;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnSpinClicked);

            gameManager.OnOutOfCredits += () => {
                _gameOver = true;
                if (button      != null) 
                { 
                    button.interactable = false; 
                    button.image.sprite = inactiveButtonSprite; 
                }
            };
        }

        // Poll CanSpin every frame — simple and reliable
        private void Update()
        {
            if (_gameOver) return;
            if (button != null)
            {
                button.interactable = gameManager.CanSpin;

                if (button.interactable)
                {
                    button.image.sprite = activeButtonSprite;
                }
                else
                {
                    button.image.sprite = inactiveButtonSprite;
                }
            }
        }

        private void OnSpinClicked()
        {
            gameManager.RequestSpin();
        }
    }
}