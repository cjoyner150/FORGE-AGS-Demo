using UnityEngine;
using TMPro;

namespace FORGE
{
    public class CreditDisplay : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text    creditsLabel;
        [SerializeField] private TMP_Text    rtpLabel;
        [SerializeField] private TMP_Text    hitFreqLabel;
        [SerializeField] private TMP_Text    spinCountLabel;

        private void Awake()
        {
            gameManager.OnSessionUpdated += Refresh;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnSessionUpdated -= Refresh;
        }

        private void Refresh(SessionState s)
        {
            if (creditsLabel   != null) creditsLabel.text   = $"{s.Credits:F2}";
            if (rtpLabel       != null) rtpLabel.text       = $"RTP {s.RealizedRTP * 100f:F1}%";
            if (hitFreqLabel   != null) hitFreqLabel.text   = $"Hit {s.HitFrequency * 100f:F1}%";
            if (spinCountLabel != null) spinCountLabel.text = $"Spins {s.SpinCount}";
        }
    }
}
