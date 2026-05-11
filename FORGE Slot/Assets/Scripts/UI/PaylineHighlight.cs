using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FORGE
{
    /// <summary>
    /// A horizontal line across the center (payline) row.
    ///
    /// Setup:
    ///   Create a UI Image stretched to the full width of your reel area,
    ///   height ~4px, positioned at the vertical center of the middle row.
    ///   Set its color to a subtle accent (e.g. white at 40% alpha normally).
    ///   Assign this component to that GameObject.
    ///
    /// Behaviour:
    ///   - Always visible at base opacity (marks the payline)
    ///   - Pulses brighter on win, then fades back to base
    /// </summary>
    public class PaylineHighlight : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image       line;

        [Header("Appearance")]
        [SerializeField] private Color baseColor  = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color winColor   = new Color(1f, 0.85f, 0.3f, 1f);

        [Header("Timing")]
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float fadeDuration  = 0.8f;

        private void Awake()
        {
            if (line != null) line.color = baseColor;
            gameManager.OnSpinResolved += OnSpinResolved;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnSpinResolved -= OnSpinResolved;
        }

        private void OnSpinResolved(SpinResult r)
        {
            if (!r.IsWin) return;
            StopAllCoroutines();
            StartCoroutine(PulseCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            // Flash to win color
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / flashDuration;
                if (line != null) line.color = Color.Lerp(baseColor, winColor, t);
                yield return null;
            }
            if (line != null) line.color = winColor;

            // Fade back to base
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / fadeDuration;
                if (line != null) line.color = Color.Lerp(winColor, baseColor, t);
                yield return null;
            }
            if (line != null) line.color = baseColor;
        }
    }
}
