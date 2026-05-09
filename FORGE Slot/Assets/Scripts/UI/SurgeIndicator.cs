using System.Collections;
using UnityEngine;
using TMPro;

namespace FORGE
{
    public class SurgeIndicator : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject  surgeActivePanel;
        [SerializeField] private TMP_Text    surgeSpinsLabel;
        [SerializeField] private GameObject  surgeTriggerFlash;

        private void Awake()
        {
            gameManager.OnSpinResolved += Refresh;
            if (surgeActivePanel  != null) surgeActivePanel.SetActive(false);
            if (surgeTriggerFlash != null) surgeTriggerFlash.SetActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnSpinResolved -= Refresh;
        }

        private void Refresh(SpinResult r)
        {
            bool active = r.IsSurge || r.SurgeSpinsRemaining > 0;

            if (surgeActivePanel != null)
                surgeActivePanel.SetActive(active);

            if (surgeSpinsLabel != null)
                surgeSpinsLabel.text = active ? $"{r.SurgeSpinsRemaining}" : "";

            if (surgeTriggerFlash != null && r.SurgeTriggered)
                StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            surgeTriggerFlash.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            surgeTriggerFlash.SetActive(false);
        }
    }
}
