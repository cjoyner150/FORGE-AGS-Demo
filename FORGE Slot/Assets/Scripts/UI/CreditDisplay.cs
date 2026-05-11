using System.Collections;
using UnityEngine;
using TMPro;

namespace FORGE
{
    public class CreditDisplay : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Labels")]
        [SerializeField] private TMP_Text creditsLabel;
        [SerializeField] private TMP_Text winAmountLabel;
        [SerializeField] private TMP_Text rtpLabel;
        [SerializeField] private TMP_Text hitFreqLabel;
        [SerializeField] private TMP_Text spinCountLabel;

        [Header("Count-up timing")]
        [Tooltip("Seconds to count up to a win payout.")]
        [SerializeField] private float winCountUpDuration = 0.6f;
        [Tooltip("Seconds to count down the bet deduction on spin click.")]
        [SerializeField] private float betCountDownDuration = 0.15f;
        [SerializeField] private float swellScale = 1.2f;

        private float _displayedCredits;
        private bool _initialised;
        private Coroutine _creditsCoroutine;
        private Coroutine _winCoroutine;

        private void Awake()
        {
            gameManager.OnSessionUpdated += OnSessionUpdated;
            gameManager.OnSpinResolved   += OnSpinResolved;
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.OnSessionUpdated -= OnSessionUpdated;
            gameManager.OnSpinResolved   -= OnSpinResolved;
        }

        public void ClearWin()
        {
            if (_winCoroutine != null) StopCoroutine(_winCoroutine);
            _winCoroutine = null;
            if (winAmountLabel != null)
            {
                winAmountLabel.text                 = "0";
                winAmountLabel.transform.localScale = Vector3.one;
            }
        }

        private void OnSessionUpdated(SessionState s)
        {
            // First call: initialise without animation
            if (!_initialised)
            {
                _displayedCredits = s.Credits;
                _initialised      = true;
                if (creditsLabel != null) creditsLabel.text = $"{s.Credits:F2}";
            }
            else
            {
                float target = s.Credits;
                bool isDeduct = target < _displayedCredits;
                bool isChanged = !Mathf.Approximately(target, _displayedCredits);
                float duration = isDeduct ? betCountDownDuration : winCountUpDuration;
                float swell = isDeduct ? 1f : swellScale;

                if (_creditsCoroutine != null) StopCoroutine(_creditsCoroutine);
                if (isChanged) _creditsCoroutine = StartCoroutine(CountUpCredits(target, duration, swell));
            }

            // Stats update immediately
            if (rtpLabel       != null) rtpLabel.text       = $"RTP {s.RealizedRTP * 100f:F1}%";
            if (hitFreqLabel   != null) hitFreqLabel.text   = $"Hit {s.HitFrequency * 100f:F1}%";
            if (spinCountLabel != null) spinCountLabel.text = $"Spins {s.SpinCount}";
        }

        private void OnSpinResolved(SpinResult r)
        {
            if (winAmountLabel == null) return;

            if (!r.IsWin)
            {
                winAmountLabel.text = "0";
                return;
            }

            // PayoutMultiplier is e.g. 14 for a 7x win with 2x wild.
            // BetSize is the raw credit bet (e.g. 1 credit).
            // Show credits won as a clean integer — all multipliers are whole numbers.
            float won = r.PayoutMultiplier * gameManager.BetSize;
            if (_winCoroutine != null) StopCoroutine(_winCoroutine);
            _winCoroutine = StartCoroutine(LabelCountUp.Run(
                winAmountLabel,
                0f, won,
                winCountUpDuration,
                "+{0:F0}",
                swellScale));
        }

        private IEnumerator CountUpCredits(float target, float duration, float swell)
        {
            if (creditsLabel == null) yield break;

            yield return StartCoroutine(LabelCountUp.Run(
                creditsLabel,
                _displayedCredits,
                target,
                duration,
                "{0:F2}",
                swell));

            _displayedCredits    = target;
            _creditsCoroutine    = null;
        }
    }
}