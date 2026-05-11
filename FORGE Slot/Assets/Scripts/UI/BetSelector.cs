using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FORGE
{
    /// <summary>
    /// Cycles the active bet through allowed values: 1, 3, 5.
    /// Locked while spinning. Polls CanSpin in Update so buttons
    /// re-enable as soon as the spin fully resolves.
    /// </summary>
    public class BetSelector : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text betLabel;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;

        private readonly int[] _betOptions = { 1, 3, 5 };
        private int _currentIndex = 0;

        private void Awake()
        {
            ApplyBet();
            RefreshDisplay();
        }

        // Poll every frame — cheap and guarantees buttons re-enable
        // the moment _isSpinning flips back to false
        private void Update()
        {
            bool canChange = gameManager.CanSpin;
            if (decreaseButton != null)
                decreaseButton.interactable = canChange && _currentIndex > 0;
            if (increaseButton != null)
                increaseButton.interactable = canChange && _currentIndex < _betOptions.Length - 1;
        }

        public void OnDecreaseClicked()
        {
            if (_currentIndex <= 0) return;
            _currentIndex--;
            ApplyBet();
            RefreshDisplay();
        }

        public void OnIncreaseClicked()
        {
            if (_currentIndex >= _betOptions.Length - 1) return;
            _currentIndex++;
            ApplyBet();
            RefreshDisplay();
        }

        private void ApplyBet()
        {
            gameManager.SetBetSize(_betOptions[_currentIndex]);
        }

        private void RefreshDisplay()
        {
            if (betLabel != null)
                betLabel.text = $"{_betOptions[_currentIndex]}";
        }
    }
}