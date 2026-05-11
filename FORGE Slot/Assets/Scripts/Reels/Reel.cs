using System;
using System.Collections;
using UnityEngine;

namespace FORGE
{
    public class Reel : MonoBehaviour
    {
        [SerializeField] private ReelStripData normalStrip;
        [SerializeField] private ReelStripData surgeStrip;

        [Header("Spin Timing")]
        [Tooltip("Stagger delay before this reel starts spinning.")]
        [SerializeField] private float startDelay = 0f;
        [Tooltip("How long the reel spins at full speed before decelerating.")]
        [SerializeField] private float fullSpeedDuration = 0.6f;
        [Tooltip("How long the deceleration phase lasts.")]
        [SerializeField] private float decelDuration = 0.4f;
        [Tooltip("Must match ReelDisplay.symbolHeight.")]
        [SerializeField] private float symbolHeight = 120f;

        private ReelStripData _activeStrip;

        public float SpinProgress { get; private set; }
        public float DecelDuration => decelDuration;
        public bool HasTarget { get; private set; }
        public int TargetStopIndex { get; private set; }
        public int LandedStopIndex { get; private set; }
        public bool IsSpinning { get; private set; }
        public int StopCount => _activeStrip != null ? _activeStrip.StopCount : 22;

        public event Action<Reel> OnLanded;

        private void Awake()
        {
            _activeStrip = normalStrip;
            SpinProgress = 0f;
            HasTarget    = false;
        }

        public SymbolType GetSymbolAt(int stopIndex) => _activeStrip.GetStop(stopIndex);

        public void SetStrip(bool isSurge)
        {
            if (IsSpinning) return;
            _activeStrip = isSurge ? surgeStrip : normalStrip;
        }

        public void Spin(int targetStop)
        {
            StopAllCoroutines();

            // Set IsSpinning synchronously so ReelDisplay.LateUpdate sees it
            // on the same frame Spin() is called, before the coroutine runs.
            IsSpinning   = true;
            SpinProgress = 0f;
            HasTarget    = false;

            StartCoroutine(SpinCoroutine(targetStop));
        }

        /// <summary>
        /// Called by ReelDisplay when its Hermite decel curve reaches t=1.
        /// ReelDisplay owns decel timing. This fires OnLanded for all listeners
        /// (audio, UI, GameManager land counter, etc).
        /// </summary>
        public void NotifyDisplayLanded()
        {
            LandedStopIndex = TargetStopIndex;
            IsSpinning      = false;
            HasTarget       = false;
            OnLanded?.Invoke(this);
        }

        private IEnumerator SpinCoroutine(int targetStop)
        {
            // IsSpinning already true -- set synchronously in Spin()
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            // Full-speed phase
            float elapsed = 0f;
            while (elapsed < fullSpeedDuration)
            {
                elapsed      += Time.deltaTime;
                SpinProgress  = 0f;
                yield return null;
            }

            // Signal ReelDisplay to begin decel. ReelDisplay computes
            // _decelEndPx from TargetStopIndex and runs the Hermite curve.
            // When it finishes it calls NotifyDisplayLanded() -- not us.
            TargetStopIndex = targetStop;
            HasTarget       = true;

            // Wait for ReelDisplay to call NotifyDisplayLanded()
            while (IsSpinning)
                yield return null;
        }
    }
}