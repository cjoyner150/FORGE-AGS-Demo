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
        private int _landedStopIndex = 0;
        private bool _isSpinning;

        // 0 = full speed, 1 = fully stopped.
        // ReelDisplay reads this to scale down its scroll speed.
        public float SpinProgress { get; private set; }
        public float DecelDuration => decelDuration;

        // The exact _displayScrollPx value ReelDisplay should arrive at
        // by the end of decel. Set before decel begins so ReelDisplay
        // can lerp toward it, eliminating the snap.
        public float TargetScrollPx { get; private set; }
        public bool HasTarget { get; private set; }

        public int LandedStopIndex => _landedStopIndex;
        public bool IsSpinning => _isSpinning;
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
            if (_isSpinning) return;
            _activeStrip = isSurge ? surgeStrip : normalStrip;
        }

        public void Spin(int targetStop)
        {
            StopAllCoroutines();
            StartCoroutine(SpinCoroutine(targetStop));
        }

        private IEnumerator SpinCoroutine(int targetStop)
        {
            _isSpinning  = true;
            SpinProgress = 0f;
            HasTarget    = false;

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

            // Signal ReelDisplay to compute its target scroll position now,
            // before decel begins, so it has the full decel window to arrive.
            // ReelDisplay.ComputeTargetScroll() is called via the property setter.
            TargetScrollPx = targetStop * symbolHeight;
            HasTarget      = true;

            // Deceleration phase
            elapsed = 0f;
            while (elapsed < decelDuration)
            {
                elapsed      += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / decelDuration);
                SpinProgress  = t * t;  // ease-in: slows toward stop
                yield return null;
            }

            SpinProgress     = 0f;
            _landedStopIndex = targetStop;
            _isSpinning      = false;
            HasTarget        = false;

            OnLanded?.Invoke(this);
        }
    }
}