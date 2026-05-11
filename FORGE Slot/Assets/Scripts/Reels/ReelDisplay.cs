using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FORGE
{
    /// <summary>
    /// Slot positions are computed ABSOLUTELY from _scrollPx each frame.
    /// No delta accumulation -- eliminates all floating point drift.
    ///
    /// Each slot has a fixed phase offset. Position formula:
    ///   slotY[i] = topY - (_scrollPx - _phase[i]) % poolH
    ///
    /// Sign convention: higher Y = earlier (lower) stop number.
    /// Slots above the payline show stops before the landed stop.
    ///
    /// During full-speed: _paylineStop tracks the current payline stop,
    /// incrementing on each slot recycle.
    ///
    /// During decel: symbols are assigned every frame directly from
    /// TargetStopIndex using actual slot Y positions. This eliminates
    /// the landing pop -- by the time SnapSymbols fires, the symbols
    /// are already correct.
    ///
    /// ReelDisplay owns decel timing. When the Hermite curve finishes,
    /// it calls reel.NotifyDisplayLanded(), which sets reel state and
    /// fires Reel.OnLanded for all listeners (audio, UI, etc).
    /// </summary>
    public class ReelDisplay : MonoBehaviour
    {
        [SerializeField] private Reel reel;
        [SerializeField] private SymbolSpriteLibrary spriteLibrary;
        [SerializeField] private float symbolHeight = 120f;
        [SerializeField] private float displaySpinSpeed = 1800f;
        [SerializeField] private RectTransform[] slots = new RectTransform[5];

        private Image[] _images;
        private TMP_Text[] _labels;

        private const int SlotCount = 5;
        private const int PaylineSlot = 2;
        private const float SnapThreshold = 2f;

        private float[] _phase;
        private int[] _slotStop;
        private float[] _prevY;

        private int _paylineStop;

        private float _scrollPx;

        private bool _inDecel;
        private float _decelStartPx;
        private float _decelEndPx;
        private float _decelDuration;
        private float _decelElapsed;

        private float _topY;
        private float _botY;
        private float _poolH;

        // Debug
        private bool _firstSpinDone = false;
        private StreamWriter _log;

        private void Awake()
        {
            _images   = new Image[SlotCount];
            _labels   = new TMP_Text[SlotCount];
            _phase    = new float[SlotCount];
            _slotStop = new int[SlotCount];
            _prevY    = new float[SlotCount];

            _topY  =  symbolHeight;
            _botY  = -(SlotCount - 1) * symbolHeight;
            _poolH =  SlotCount * symbolHeight;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null) continue;
                _images[i] = slots[i].GetComponent<Image>();
                _labels[i] = slots[i].GetComponentInChildren<TMP_Text>();
            }

            string path = Path.Combine(Application.persistentDataPath, $"forge_reel_{name}.txt");
            _log = new StreamWriter(path, append: false);
            _log.AutoFlush = true;
            Debug.Log($"[{name}] Debug log: {path}");
        }

        private void OnDestroy()
        {
            _log?.Close();
        }

        private void Start()
        {
            InitSlots(0);
        }

        private void OnEnable()
        {
            if (reel != null) reel.OnLanded += OnReelLanded;
        }

        private void OnDisable()
        {
            if (reel != null) reel.OnLanded -= OnReelLanded;
        }

        private void OnReelLanded(Reel _)
        {
            if (!_firstSpinDone)
            {
                _log.WriteLine($"--- LANDED stop={reel.LandedStopIndex} scrollPx={_scrollPx:F1} _paylineStop={_paylineStop}");
                float paylineY = -symbolHeight;
                for (int i = 0; i < SlotCount; i++)
                    _log.WriteLine($"    slot{i} Y={GetSlotY(i):F1} stop={_slotStop[i]} distFromPayline={GetSlotY(i)-paylineY:F1}");
                _firstSpinDone = true;
            }
            SnapSymbols(reel.LandedStopIndex);
        }

        public void StartSpin()
        {
            _inDecel      = false;
            _decelElapsed = 0f;

            if (!_firstSpinDone)
            {
                _log.WriteLine($"=== STARTSPIN scrollPx={_scrollPx:F1} topY={_topY} botY={_botY} poolH={_poolH} _paylineStop={_paylineStop}");
                for (int i = 0; i < SlotCount; i++)
                    _log.WriteLine($"  slot{i} phase={_phase[i]:F1} Y={GetSlotY(i):F1} stop={_slotStop[i]}");
            }

            for (int i = 0; i < SlotCount; i++)
                _prevY[i] = GetSlotY(i);
        }

        private void LateUpdate()
        {
            if (reel == null || !reel.IsSpinning) return;

            if (reel.HasTarget && !_inDecel)
            {
                _inDecel       = true;
                _decelElapsed  = 0f;
                _decelDuration = reel.DecelDuration;
                _decelStartPx  = _scrollPx;

                float stripH = reel.StopCount * symbolHeight;
                float targetBase = reel.TargetStopIndex * symbolHeight;
                float candidate = targetBase
                    + Mathf.Ceil((_decelStartPx - targetBase) / stripH) * stripH;
                while (candidate < _decelStartPx + symbolHeight * 0.5f)
                    candidate += stripH;
                _decelEndPx = candidate;
            }

            float prevScrollPx = _scrollPx;

            if (_inDecel)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                _decelElapsed += dt;
                float t = Mathf.Clamp01(_decelElapsed / _decelDuration);

                float h00 = 2*t*t*t - 3*t*t + 1;
                float h10 = t*t*t - 2*t*t + t;
                float h01 = -2*t*t*t + 3*t*t;
                float hermitePos = h00 * _decelStartPx
                                 + h10 * displaySpinSpeed * _decelDuration
                                 + h01 * _decelEndPx;

                _scrollPx = Mathf.Max(hermitePos, prevScrollPx);

                // During decel, assign symbols every frame from the known
                // target stop using actual Y positions. No recycle tracking
                // needed -- the target is fixed and Y positions are exact.
                // This ensures symbols are already correct when SnapSymbols
                // fires at landing, producing no visible pop.
                AssignSymbolsFromStop(reel.TargetStopIndex);

                bool timerDone = _decelElapsed >= _decelDuration;
                bool positionDone = _scrollPx >= _decelEndPx - SnapThreshold;

                if (timerDone || positionDone)
                {
                    _scrollPx = _decelEndPx;
                    _inDecel  = false;
                    reel.NotifyDisplayLanded();
                    return;
                }

                Render();
            }
            else
            {
                _scrollPx += displaySpinSpeed * Time.deltaTime;

                // Full-speed: use recycle-based tracking
                UpdateSlotSymbols();
                Render();
            }
        }

        // Assigns stops to all slots from a known stop, using Y positions.
        // Used every frame during decel so symbols converge to the correct
        // state before landing.
        private void AssignSymbolsFromStop(int targetStop)
        {
            int stripLen = reel.StopCount;
            float paylineY = -symbolHeight;
            for (int i = 0; i < SlotCount; i++)
            {
                float y = GetSlotY(i);
                int rowOff = Mathf.RoundToInt((y - paylineY) / symbolHeight);
                _slotStop[i] = Mod(targetStop - rowOff, stripLen);
            }
        }

        private void UpdateSlotSymbols()
        {
            int stripLen = reel.StopCount;

            for (int i = 0; i < SlotCount; i++)
            {
                float currY = GetSlotY(i);
                float prevSlotY = _prevY[i];
                _prevY[i]       = currY;

                bool justRecycled = prevSlotY < _botY + symbolHeight * 0.5f
                                 && currY     > _topY  - symbolHeight * 0.5f;
                if (!justRecycled) continue;

                _paylineStop = Mod(_paylineStop + 1, stripLen);
                int newStop = Mod(_paylineStop - PaylineSlot, stripLen);

                if (!_firstSpinDone)
                    _log.WriteLine($"  RECYCLE slot{i} currY={currY:F1} _paylineStop={_paylineStop} newStop={newStop} scrollPx={_scrollPx:F1}");

                _slotStop[i] = newStop;
            }
        }

        private float GetSlotY(int i)
        {
            float raw = (_scrollPx - _phase[i]) % _poolH;
            if (raw < 0) raw += _poolH;
            return _topY - raw;
        }

        private void SnapSymbols(int landedStop)
        {
            int stripLen = reel.StopCount;
            float paylineY = -symbolHeight;

            for (int i = 0; i < SlotCount; i++)
            {
                float y = GetSlotY(i);
                int rowOff = Mathf.RoundToInt((y - paylineY) / symbolHeight);
                _slotStop[i] = Mod(landedStop - rowOff, stripLen);
            }

            _paylineStop = landedStop;

            // Recompute phases from current positions
            for (int i = 0; i < SlotCount; i++)
            {
                float currentY = GetSlotY(i);
                float raw = _scrollPx - (_topY - currentY);
                _phase[i]      = ((raw % _poolH) + _poolH) % _poolH;
            }

            // Align _scrollPx to landed stop
            float stripTotalH = stripLen * symbolHeight;
            float targetBase = landedStop * symbolHeight;
            float aligned = targetBase
                + Mathf.Round((_scrollPx - targetBase) / stripTotalH) * stripTotalH;
            float shift = aligned - _scrollPx;
            _scrollPx     = aligned;
            for (int i = 0; i < SlotCount; i++)
                _phase[i] = ((_phase[i] + shift) % _poolH + _poolH) % _poolH;

            for (int i = 0; i < SlotCount; i++)
                _prevY[i] = GetSlotY(i);

            Render();
        }

        private void InitSlots(int landedStop)
        {
            int stripLen = reel != null ? reel.StopCount : 22;
            float stripTotalH = stripLen * symbolHeight;

            _scrollPx    = stripTotalH * 10f;
            _paylineStop = landedStop;

            for (int i = 0; i < SlotCount; i++)
            {
                _phase[i]    = ((_poolH - i * symbolHeight) % _poolH + _poolH) % _poolH;
                _slotStop[i] = Mod(landedStop - (PaylineSlot - i), stripLen);
                _prevY[i]    = GetSlotY(i);
            }
            Render();
        }

        private void Render()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null) continue;
                slots[i].anchoredPosition = new Vector2(0f, GetSlotY(i));
                SetSlotVisual(i, _slotStop[i]);
            }
        }

        private void SetSlotVisual(int slotIndex, int stopIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            SymbolType sym = reel.GetSymbolAt(stopIndex);
            if (_images[slotIndex] != null)
            {
                Sprite spr = spriteLibrary != null ? spriteLibrary.GetSprite(sym) : null;
                _images[slotIndex].sprite  = spr;
                _images[slotIndex].enabled = spr != null;
            }
            if (_labels[slotIndex] != null)
                _labels[slotIndex].text = SymbolSpriteLibrary.GetLabel(sym);
        }

        private static int Mod(int a, int b) => ((a % b) + b) % b;
    }
}