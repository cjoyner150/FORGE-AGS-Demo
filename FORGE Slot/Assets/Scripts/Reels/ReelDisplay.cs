using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FORGE
{
    /// <summary>
    /// Phase-based absolute position model for a UI reel column.
    ///
    /// Slot position formula:
    ///   slotY[i] = topY - (_scrollPx - _phase[i]) % poolH
    ///
    /// Sign convention: higher Y = earlier (lower) stop number.
    ///
    /// Full-speed: _scrollPx advances by displaySpinSpeed * dt.
    /// Symbols assigned via recycle detection (_paylineStop tracking).
    ///
    /// Decel: at the moment decel begins, _scrollPx is snapped to the
    /// nearest stop boundary (at most symbolHeight/2, invisible at speed).
    /// _decelEndPx is then _decelStartPx + stopsToTravel * symbolHeight,
    /// which is guaranteed pixel-aligned AND symbol-consistent with the
    /// recycle tracker. Symbols only update when stopsFromEnd changes.
    ///
    /// ReelDisplay calls reel.NotifyDisplayLanded() when done.
    /// </summary>
    public class ReelDisplay : MonoBehaviour
    {
        [SerializeField] private Reel reel;
        [SerializeField] private SymbolSpriteLibrary spriteLibrary;
        [SerializeField] private float symbolHeight = 120f;
        [SerializeField] private float displaySpinSpeed = 1800f;
        [SerializeField] private RectTransform[] slots = new RectTransform[5];

        [Header("Decel")]
        [Tooltip("Ease-out position curve. X=normalised time (0-1), Y=normalised position (0-1). " +
                 "Start steep (slope ~1 to match full speed), flatten to 0 at end.")]
        [SerializeField]
        private AnimationCurve decelCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));

        private Image[] _images;
        private TMP_Text[] _labels;

        private const int SlotCount = 5;
        private const int PaylineSlot = 2;

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
        private int _lastStopsFromEnd;

        private float _topY;
        private float _botY;
        private float _poolH;

        // -------------------------------------------------------------------------
        #region Unity Lifecycle
        // -------------------------------------------------------------------------

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

        #endregion

        // -------------------------------------------------------------------------
        #region Public API
        // -------------------------------------------------------------------------

        public void StartSpin()
        {
            _inDecel      = false;
            _decelElapsed = 0f;

            for (int i = 0; i < SlotCount; i++)
                _prevY[i] = GetSlotY(i);
        }

        #endregion

        // -------------------------------------------------------------------------
        #region LateUpdate
        // -------------------------------------------------------------------------

        private void LateUpdate()
        {
            if (reel == null || !reel.IsScrolling) return;

            if (reel.HasTarget && !_inDecel)
                BeginDecel();

            if (_inDecel)
                UpdateDecel();
            else
                UpdateFullSpeed();
        }

        private void BeginDecel()
        {
            _inDecel       = true;
            _decelElapsed  = 0f;
            _decelDuration = reel.DecelDuration;

            // Snap _scrollPx to the nearest stop boundary before starting decel.
            // At full speed this is at most symbolHeight/2 = invisible.
            // This guarantees _decelStartPx is pixel-aligned so adding whole
            // symbol heights gives a pixel-aligned _decelEndPx.
            float snapped = Mathf.Round(_scrollPx / symbolHeight) * symbolHeight;
            _scrollPx     = snapped;
            _decelStartPx = snapped;

            // Also update _paylineStop to match the snapped position so
            // symbol tracking stays consistent after the snap.
            // The snap moves us by at most half a symbol -- check if we
            // crossed a stop boundary and adjust _paylineStop accordingly.
            // Simplest approach: recompute from the snapped scroll position.
            // Since _decelStartPx is now stop-aligned, _paylineStop should
            // reflect whatever stop is at the payline right now.
            // We trust the existing _paylineStop value since the snap is
            // less than half a symbol and can't cross a full boundary.

            int stripLen = reel.StopCount;

            // How many stops from current payline to target?
            int stopsToTravel = Mod(reel.TargetStopIndex - _paylineStop, stripLen);

            // Ensure at least half a revolution so the reel visibly spins.
            if (stopsToTravel < stripLen / 2)
                stopsToTravel += stripLen;

            // Both pixel-aligned (snapped start + whole symbols) AND
            // symbol-consistent (matches _paylineStop tracking).
            _decelEndPx       = _decelStartPx + stopsToTravel * symbolHeight;
            _lastStopsFromEnd = stopsToTravel;
        }

        private void UpdateDecel()
        {
            _decelElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_decelElapsed / _decelDuration);
            float curveT = decelCurve.Evaluate(t);

            _scrollPx = Mathf.Lerp(_decelStartPx, _decelEndPx, curveT);

            // Only reassign symbols when stopsFromEnd crosses an integer boundary.
            // Symbols change exactly once per stop -- no oscillation.
            int stopsFromEnd = Mathf.RoundToInt((_decelEndPx - _scrollPx) / symbolHeight);
            if (stopsFromEnd != _lastStopsFromEnd)
            {
                _lastStopsFromEnd = stopsFromEnd;
                AssignSymbolsFromScroll();
            }

            Render();

            if (_decelElapsed >= _decelDuration)
            {
                _scrollPx = _decelEndPx;
                _inDecel  = false;
                reel.NotifyDisplayLanded();
            }
        }

        private void UpdateFullSpeed()
        {
            _scrollPx += displaySpinSpeed * Time.deltaTime;
            UpdateSlotSymbols();
            Render();
        }

        #endregion

        // -------------------------------------------------------------------------
        #region Symbol Assignment
        // -------------------------------------------------------------------------

        private void AssignSymbolsFromScroll()
        {
            int stripLen = reel.StopCount;
            float pxFromEnd = _decelEndPx - _scrollPx;
            int stopsFromEnd = Mathf.RoundToInt(pxFromEnd / symbolHeight);
            int paylineStop = Mod(reel.TargetStopIndex + stopsFromEnd, stripLen);
            float paylineY = -symbolHeight;

            for (int i = 0; i < SlotCount; i++)
            {
                float slotY = GetSlotY(i);
                int poolOff = Mathf.RoundToInt((slotY - paylineY) / symbolHeight);
                _slotStop[i]  = Mod(paylineStop - poolOff, stripLen);
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
                _slotStop[i] = Mod(_paylineStop - PaylineSlot, stripLen);
            }
        }

        #endregion

        // -------------------------------------------------------------------------
        #region Positioning
        // -------------------------------------------------------------------------

        private float GetSlotY(int i)
        {
            float raw = (_scrollPx - _phase[i]) % _poolH;
            if (raw < 0) raw += _poolH;
            return _topY - raw;
        }

        #endregion

        // -------------------------------------------------------------------------
        #region Landing and Snap
        // -------------------------------------------------------------------------

        private void OnReelLanded(Reel _)
        {
            SnapSymbols(reel.LandedStopIndex);
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

            for (int i = 0; i < SlotCount; i++)
            {
                float currentY = GetSlotY(i);
                float raw = _scrollPx - (_topY - currentY);
                _phase[i]      = ((raw % _poolH) + _poolH) % _poolH;
            }

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

        #endregion

        // -------------------------------------------------------------------------
        #region Init and Render
        // -------------------------------------------------------------------------

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
            else if (_labels[slotIndex] != null && !_images[slotIndex].enabled)
                _labels[slotIndex].text = SymbolSpriteLibrary.GetLabel(sym);
        }

        #endregion

        // -------------------------------------------------------------------------
        #region Utilities
        // -------------------------------------------------------------------------

        private static int Mod(int a, int b) => ((a % b) + b) % b;

        #endregion
    }
}