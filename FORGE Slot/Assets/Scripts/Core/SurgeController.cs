using UnityEngine;
using System;

namespace FORGE
{
    /// <summary>
    /// Owns the Wild Surge state machine.
    /// GameManager calls CheckSurgeTrigger() before each spin and
    /// ConsumeSpurgeSpin() after each surge spin resolves.
    /// </summary>
    public class SurgeController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        // ── State ─────────────────────────────────────────────────────
        private int _surgeSpinsRemaining;

        public bool IsSurge           => _surgeSpinsRemaining > 0;
        public int  SpinsRemaining    => _surgeSpinsRemaining;

        // ── Events ────────────────────────────────────────────────────
        /// <summary>Fired on the spin that first triggers a surge.</summary>
        public event Action OnSurgeTriggered;

        /// <summary>Fired when the final surge spin has been consumed.</summary>
        public event Action OnSurgeEnded;

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Called by GameManager at the start of each spin, before math resolves.
        /// Returns true if surge was triggered this spin (new trigger, not continuation).
        /// </summary>
        public bool CheckSurgeTrigger()
        {
            if (IsSurge) return false;   // already in surge, no new trigger

            if (UnityEngine.Random.value < config.surgeTriggerChance)
            {
                _surgeSpinsRemaining = config.surgeDurationSpins;
                OnSurgeTriggered?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called by GameManager after each spin resolves during a surge.
        /// Decrements the counter and fires OnSurgeEnded when the last spin is consumed.
        /// </summary>
        public void ConsumeSurgeSpin()
        {
            if (!IsSurge) return;

            _surgeSpinsRemaining--;

            if (_surgeSpinsRemaining == 0)
                OnSurgeEnded?.Invoke();
        }

        /// <summary>Hard reset — used when restarting a session.</summary>
        public void Reset()
        {
            _surgeSpinsRemaining = 0;
        }
    }
}
