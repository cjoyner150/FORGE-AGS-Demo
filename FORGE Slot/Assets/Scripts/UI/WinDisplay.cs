using UnityEngine;
using TMPro;

namespace FORGE
{
    public class WinDisplay : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject  winPanel;
        [SerializeField] private TMP_Text    winAmountLabel;
        [SerializeField] private TMP_Text    multiplierLabel;
        [SerializeField] private TMP_Text    symbolLabel;
        [SerializeField] private TMP_Text    surgeWinLabel;

        private void Awake()
        {
            gameManager.OnSpinResolved += ShowResult;
            if (winPanel != null) winPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnSpinResolved -= ShowResult;
        }

        private void ShowResult(SpinResult r)
        {
            if (winPanel == null) return;

            if (!r.IsWin)
            {
                winPanel.SetActive(false);
                return;
            }

            winPanel.SetActive(true);

            float dollarWin = r.PayoutMultiplier * gameManager.BetSize;

            if (winAmountLabel  != null)
                winAmountLabel.text = $"${dollarWin:F2}";

            if (multiplierLabel != null)
            {
                multiplierLabel.text = r.WildCount > 0
                    ? $"{r.PayoutMultiplier:F0}x (base x {r.WildMultiplier:F0}x wild)"
                    : $"{r.PayoutMultiplier:F0}x";
            }

            if (symbolLabel != null)
            {
                symbolLabel.text = r.WildCount == 3
                    ? "3x WILD"
                    : r.MatchedSymbol.HasValue
                        ? r.MatchedSymbol.Value.ToString().ToUpper()
                        : "";
            }

            if (surgeWinLabel != null)
                surgeWinLabel.gameObject.SetActive(r.IsSurge);
        }
    }
}
