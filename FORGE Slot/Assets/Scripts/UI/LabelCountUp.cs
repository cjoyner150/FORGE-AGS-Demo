using System.Collections;
using TMPro;
using UnityEngine;

namespace FORGE
{
    /// <summary>
    /// Reusable utility: animates a TMP_Text label counting from one value
    /// to another with an ease-out curve, plus a scale swell on the text.
    ///
    /// Usage:
    ///   StartCoroutine(LabelCountUp.Run(label, fromValue, toValue, duration, format));
    ///
    /// The swell: label punches up to swellScale then eases back to 1.0
    /// over the first half of the count-up, giving a satisfying pop.
    /// </summary>
    public static class LabelCountUp
    {
        /// <summary>
        /// Animate a label from fromVal to toVal over duration seconds.
        /// format: e.g. "{0:F2}" for credits, "{0:F0}×" for multiplier.
        /// swellScale: peak scale during punch (1.3 = 30% bigger).
        /// </summary>
        public static IEnumerator Run(
            TMP_Text label,
            float    fromVal,
            float    toVal,
            float    duration,
            string   format      = "{0:F2}",
            float    swellScale  = 1.25f)
        {
            if (label == null) yield break;

            float elapsed  = 0f;
            float halfDur  = duration * 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);

                // Ease-out cubic for the value
                float eased   = 1f - Mathf.Pow(1f - t, 3f);
                float current = Mathf.Lerp(fromVal, toVal, eased);

                // Scale swell: punch up in first half, ease back in second half
                float scalePeak = Mathf.InverseLerp(0f, halfDur, Mathf.Min(elapsed, halfDur));
                float scaleDown = Mathf.InverseLerp(halfDur, duration, Mathf.Max(elapsed, halfDur));
                float punch     = Mathf.Lerp(1f, swellScale, EaseOut(scalePeak));
                float release   = Mathf.Lerp(swellScale, 1f, EaseOut(scaleDown));
                float scale     = elapsed < halfDur ? punch : release;

                label.text                     = string.Format(format, current);
                label.transform.localScale     = Vector3.one * scale;

                yield return null;
            }

            // Snap to exact final value and reset scale
            label.text                     = string.Format(format, toVal);
            label.transform.localScale     = Vector3.one;
        }

        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
