using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace FORGE
{
    public class Reel : MonoBehaviour
    {
        [SerializeField] private ReelStripData normalStrip;
        [SerializeField] private ReelStripData surgeStrip;

        [Header("Spin Timing")]
        [Tooltip("How long the deceleration phase lasts.")]
        [SerializeField] private float decelDuration = 0.4f;
        [Tooltip("Must match ReelDisplay.symbolHeight.")]
        [SerializeField] private float symbolHeight = 120f;

        private ReelStripData _activeStrip;

        public float DecelDuration => decelDuration;
        public bool HasTarget { get; private set; }
        public int TargetStopIndex { get; private set; }
        public int LandedStopIndex { get; private set; }
        public bool IsSpinning { get; private set; }
        public bool IsScrolling { get; private set; }
        public int StopCount => _activeStrip != null ? _activeStrip.StopCount : 22;

        public event Action<Reel> OnLanded;

        private void Awake()
        {
            _activeStrip = normalStrip;
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
            IsSpinning      = true;
            IsScrolling     = true;
            HasTarget       = false;
            TargetStopIndex = targetStop;
            ForgeLog.Write($"[{name}] Spin() targetStop={targetStop} Time={Time.time:F3}");
            StartCoroutine(SpinCoroutine());
        }

        public void BeginDecel()
        {
            ForgeLog.Write($"[{name}] BeginDecel called. IsSpinning={IsSpinning} HasTarget={HasTarget} Time={Time.time:F3}");
            if (!IsSpinning || HasTarget) return;
            HasTarget = true;
            ForgeLog.Write($"[{name}] HasTarget set to true.");
        }

        public void NotifyDisplayLanded()
        {
            ForgeLog.Write($"[{name}] NotifyDisplayLanded. stop={TargetStopIndex} Time={Time.time:F3}");
            LandedStopIndex = TargetStopIndex;
            IsSpinning      = false;
            IsScrolling     = false;
            HasTarget       = false;
            OnLanded?.Invoke(this);
        }

        private IEnumerator SpinCoroutine()
        {
            while (IsSpinning)
                yield return null;
        }
    }
}