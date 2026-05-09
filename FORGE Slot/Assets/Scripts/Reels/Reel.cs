using System;
using System.Collections;
using UnityEngine;

namespace FORGE
{
    /// <summary>
    /// One physical reel. Owns its strip reference, the spin coroutine,
    /// and the final landed stop index.
    ///
    /// ReelDisplay reads LandedStop and VisualOffset to position symbols.
    /// GameManager calls Spin() and awaits the OnLanded callback.
    /// </summary>
    public class Reel : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────
        [SerializeField] private ReelStripData normalStrip;
        [SerializeField] private ReelStripData surgeStrip;

        [Header("Spin Timing")]
        [Tooltip("Seconds the reel spins before decelerating.")]
        [SerializeField] private float spinDuration = 0.6f;

        [Tooltip("Additional delay before this reel starts (stagger between reels).")]
        [SerializeField] private float startDelay = 0f;

        [Tooltip("Deceleration duration in seconds.")]
        [SerializeField] private float stopDuration = 0.25f;

        // ── State ─────────────────────────────────────────────────────
        private ReelStripData _activeStrip;
        private int _landedStopIndex = -1;
        private bool _isSpinning;

        /// <summary>Normalised 0–1 scroll offset, driven by the spin coroutine.
        /// ReelDisplay uses this to lerp symbol positions.</summary>
        public float VisualOffset { get; private set; }

        /// <summary>The stop index the reel landed on after the last spin.</summary>
        public int LandedStopIndex => _landedStopIndex;

        /// <summary>The symbol at the landed stop.</summary>
        public SymbolType LandedSymbol => _activeStrip.GetStop(_landedStopIndex);

        /// <summary>
        /// Returns the symbol at any stop index on the currently active strip.
        /// GameManager uses this to resolve symbols before animation plays.
        /// </summary>
        public SymbolType GetSymbolAt(int stopIndex) => _activeStrip.GetStop(stopIndex);

        public bool IsSpinning => _isSpinning;

        /// <summary>Number of stops on the currently active strip.</summary>
        public int StopCount => _activeStrip != null ? _activeStrip.StopCount : 22;

        // ── Events ────────────────────────────────────────────────────
        /// <summary>Fired when the reel has fully stopped and LandedSymbol is valid.</summary>
        public event Action<Reel> OnLanded;

        // ── Lifecycle ─────────────────────────────────────────────────
        private void Awake()
        {
            _activeStrip = normalStrip;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Switch to normal or surge strip.
        /// Safe to call between spins; has no effect while spinning.
        /// </summary>
        public void SetStrip(bool isSurge)
        {
            if (_isSpinning) return;
            _activeStrip = isSurge ? surgeStrip : normalStrip;
        }

        /// <summary>
        /// Begin a spin that will land on targetStop.
        /// The outcome (targetStop) is pre-determined by GameManager before
        /// any animation plays — the spin is purely presentational.
        /// </summary>
        public void Spin(int targetStop)
        {
            // Stop any in-progress spin coroutine before starting a new one.
            // This ensures Spin() always starts fresh even if called rapidly.
            StopAllCoroutines();
            StartCoroutine(SpinCoroutine(targetStop));
        }

        // ── Internal ─────────────────────────────────────────────────

        private IEnumerator SpinCoroutine(int targetStop)
        {
            _isSpinning = true;

            // Optional stagger delay
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            float elapsed = 0f;
            float startOffset = VisualOffset;

            // ── Spin phase: scroll continuously ──────────────────────
            while (elapsed < spinDuration)
            {
                elapsed    += Time.deltaTime;
                // Wrap offset 0–1 repeatedly to simulate continuous spin
                VisualOffset = (startOffset + elapsed * 4f) % 1f;
                yield return null;
            }

            // ── Deceleration phase: ease into target stop ─────────────
            float decElapsed = 0f;
            float fromOffset = VisualOffset;
            float toOffset = targetStop / (float)_activeStrip.StopCount;

            // Ensure we always travel forward (never backward)
            if (toOffset <= fromOffset)
                toOffset += 1f;

            while (decElapsed < stopDuration)
            {
                decElapsed   += Time.deltaTime;
                float t = decElapsed / stopDuration;
                float ease = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
                VisualOffset  = Mathf.Lerp(fromOffset, toOffset, ease) % 1f;
                yield return null;
            }

            VisualOffset     = toOffset % 1f;
            _landedStopIndex = targetStop;
            _isSpinning      = false;

            OnLanded?.Invoke(this);
        }
    }
}