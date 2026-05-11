using System;
using System.Collections;
using UnityEngine;

namespace FORGE
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private SurgeController surge;
        [SerializeField] private Reel[] reels = new Reel[3];
        [SerializeField] private WinDisplay winDisplay;
        [SerializeField] private CreditDisplay creditDisplay;

        [Header("Session")]
        [SerializeField] private float startingCredits = 100f;
        [SerializeField] private float betSize = 1f;

        private SessionState _session;
        private bool _isSpinning;
        private int _landsReceived;
        private bool _winSequenceComplete;

        public event Action<SpinResult> OnSpinResolved;
        public event Action<SessionState> OnSessionUpdated;
        public event Action OnOutOfCredits;

        public SessionState Session => _session;
        public bool CanSpin => !_isSpinning && _session != null && _session.Credits >= betSize;
        public float BetSize => betSize;

        /// <summary>Called by BetSelector to change the active bet size.</summary>
        public void SetBetSize(float size)
        {
            if (_isSpinning) return;
            betSize = size;
        }

        private void Awake()
        {
            // Validate Inspector references before anything else
            if (config == null) Debug.LogError("[GameManager] config is not assigned!", this);
            if (surge  == null) Debug.LogError("[GameManager] surge is not assigned!", this);
            for (int i = 0; i < reels.Length; i++)
                if (reels[i] == null) Debug.LogError($"[GameManager] reels[{i}] is not assigned!", this);

            _session = new SessionState(startingCredits);

            foreach (var reel in reels)
                if (reel != null)
                    reel.OnLanded += HandleReelLanded;

            if (winDisplay != null)
                winDisplay.OnSequenceComplete += () => _winSequenceComplete = true;
            else
                Debug.LogWarning("[GameManager] winDisplay not assigned — win sequence will be skipped.");
        }

        private void Start()
        {
            Debug.Log("[GameManager] Start — session initialised, credits: " + _session.Credits);
            OnSessionUpdated?.Invoke(_session);
        }

        public void RequestSpin()
        {
            Debug.Log($"[GameManager] RequestSpin called. CanSpin={CanSpin} _isSpinning={_isSpinning} credits={_session?.Credits}");
            if (!CanSpin) return;
            StartCoroutine(SpinSequence());
        }

        public void RestartSession()
        {
            if (_isSpinning) return;
            _session.Reset(startingCredits);
            surge.Reset();
            OnSessionUpdated?.Invoke(_session);
        }

        private IEnumerator SpinSequence()
        {
            Debug.Log("[GameManager] SpinSequence — START");
            _isSpinning    = true;
            _landsReceived = 0;

            // Clear any previous win display immediately
            if (winDisplay     != null) winDisplay.HideImmediate();
            if (creditDisplay  != null) creditDisplay.ClearWin();

            // Deduct bet right away — credits drop the moment the player spins
            Debug.Log($"[GameManager] betSize={betSize} startingCredits={startingCredits}");
            _session.DeductBet(betSize);
            OnSessionUpdated?.Invoke(_session);

            // Step 1: surge check
            bool surgeTriggered = surge.CheckSurgeTrigger();
            bool isSurge = surge.IsSurge;
            Debug.Log($"[GameManager] isSurge={isSurge} surgeTriggered={surgeTriggered}");

            // Step 2: swap strips
            foreach (var reel in reels)
                reel.SetStrip(isSurge);

            // Step 3: roll stops
            int stop1 = UnityEngine.Random.Range(0, reels[0].StopCount);
            int stop2 = UnityEngine.Random.Range(0, reels[1].StopCount);
            int stop3 = UnityEngine.Random.Range(0, reels[2].StopCount);
            Debug.Log($"[GameManager] Stops rolled: {stop1}, {stop2}, {stop3}");

            // Step 4: resolve symbols
            SymbolType s1 = reels[0].GetSymbolAt(stop1);
            SymbolType s2 = reels[1].GetSymbolAt(stop2);
            SymbolType s3 = reels[2].GetSymbolAt(stop3);
            Debug.Log($"[GameManager] Symbols: {s1}, {s2}, {s3}");

            // Step 5: evaluate
            var (payout, wildMult, wildCount, matched) =
                PaylineEvaluator.Evaluate(s1, s2, s3, config, isSurge);
            Debug.Log($"[GameManager] s1={s1} s2={s2} s3={s3} | payout={payout} wildMult={wildMult} wildCount={wildCount} matched={matched}");
            Debug.Log($"[GameManager] config values — Scrap={config.payScrap} Shatter={config.payShatter} Ingots={config.payIngots} Plate={config.payPlate} Molten={config.payMolten}");
            Debug.Log($"[GameManager] config wild mults normal — wm1={config.wildMult1Normal} wm2={config.wildMult2Normal} wm3={config.wildMult3Normal}");

            // Step 6: spin reels
            Debug.Log("[GameManager] Calling Spin on all reels...");
            reels[0].Spin(stop1);
            reels[1].Spin(stop2);
            reels[2].Spin(stop3);
            Debug.Log("[GameManager] Spin called — waiting for OnLanded...");

            // Step 7: wait with timeout
            float waited = 0f;
            const float timeout = 5f;
            while (_landsReceived < reels.Length && waited < timeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (waited >= timeout)
                Debug.LogWarning($"[GameManager] TIMEOUT after {timeout}s. Lands received: {_landsReceived}/{reels.Length}");
            else
                Debug.Log($"[GameManager] All reels landed after {waited:F2}s");

            // Step 8: surge bookkeeping
            if (isSurge) surge.ConsumeSurgeSpin();

            // Step 9: build result
            var result = new SpinResult(
                s1, s2, s3,
                payout, wildMult, wildCount, matched,
                isSurge, surgeTriggered, surge.SpinsRemaining);

            _session.RecordPayout(betSize, payout, surgeTriggered);

            // Step 10: fire events (WinDisplay listens to OnSpinResolved and starts its sequence)
            Debug.Log("[GameManager] Firing OnSpinResolved and OnSessionUpdated");
            _winSequenceComplete = false;
            OnSpinResolved?.Invoke(result);
            OnSessionUpdated?.Invoke(_session);

            // Step 11: wait for win presentation to finish before unlocking
            if (winDisplay != null)
            {
                float winWait = 0f;
                while (!_winSequenceComplete && winWait < 5f)
                {
                    winWait += Time.deltaTime;
                    yield return null;
                }
            }

            if (_session.Credits < betSize)
                OnOutOfCredits?.Invoke();

            _isSpinning = false;
            Debug.Log($"[GameManager] SpinSequence COMPLETE — credits={_session.Credits} betSize={betSize} CanSpin={CanSpin}");
        }

        private void HandleReelLanded(Reel reel)
        {
            _landsReceived++;
            Debug.Log($"[GameManager] Reel landed: {reel.name} — total landed: {_landsReceived}/{reels.Length}");
        }
    }
}