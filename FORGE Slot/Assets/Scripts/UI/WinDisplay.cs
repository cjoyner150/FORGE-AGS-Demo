using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace FORGE
{
    public class WinDisplay : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Panel")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private TMP_Text multiplierLabel;
        [SerializeField] private TMP_Text symbolLabel;
        [SerializeField] private TMP_Text surgeWinLabel;

        [Header("Timing")]
        [SerializeField] private float countUpDuration = 0.6f;

        [Header("Swell")]
        [SerializeField] private float swellScale = 1.3f;

        public event Action OnSequenceComplete;

        private void Awake()
        {
            gameManager.OnSpinResolved += StartWinSequence;
            if (winPanel != null) winPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnSpinResolved -= StartWinSequence;
        }

        public void HideImmediate()
        {
            StopAllCoroutines();
            if (multiplierLabel != null) multiplierLabel.transform.localScale = Vector3.one;
            if (winPanel        != null) winPanel.SetActive(false);
        }

        private void StartWinSequence(SpinResult r)
        {
            StopAllCoroutines();
            StartCoroutine(WinSequenceCoroutine(r));
        }

        private IEnumerator WinSequenceCoroutine(SpinResult r)
        {
            if (!r.IsWin)
            {
                if (winPanel != null) winPanel.SetActive(false);
                OnSequenceComplete?.Invoke();
                yield break;
            }

            if (winPanel != null) winPanel.SetActive(true);

            if (symbolLabel != null)
            {
                symbolLabel.text = r.WildCount == 3
                    ? "3\u00d7 WILD"
                    : r.MatchedSymbol.HasValue
                        ? r.MatchedSymbol.Value.ToString().ToUpper()
                        : "";
            }

            if (surgeWinLabel != null)
                surgeWinLabel.gameObject.SetActive(r.IsSurge);

            // Count multiplier up with swell
            yield return StartCoroutine(LabelCountUp.Run(
                multiplierLabel,
                0f, r.PayoutMultiplier,
                countUpDuration,
                "{0:F1}\u00d7",
                swellScale));

            // Snap to clean integer at end
            if (multiplierLabel != null)
                multiplierLabel.text = $"{r.PayoutMultiplier:F0}\u00d7";

            // Count-up done — unlock spin. Panel stays visible.
            OnSequenceComplete?.Invoke();
        }
    }
}