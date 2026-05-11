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
    /// Symbols are assigned when a slot crosses topY (enters from top).
    /// Between recycles, the symbol is stable.
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

        // Per-slot phase: the _scrollPx value at which this slot sits at topY.
        // Position = topY - (_scrollPx - phase) % poolH
        private float[] _phase;
        private int[] _slotStop;

        // Previous Y per slot -- used to detect when a slot crosses topY
        private float[] _prevY;

        private float _scrollPx;

        private bool _inDecel;
        private float _decelStartPx;
        private float _decelEndPx;
        private float _decelDuration;
        private float _decelElapsed;

        private float _topY;
        private float _botY;
        private float _poolH;

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

        private void OnReelLanded(Reel _)
        {
            // Force scrollPx to exact end -- OnLanded fires before LateUpdate
            if (_inDecel)
            {
                _scrollPx = _decelEndPx;
                _inDecel  = false;
            }
            SnapSymbols(reel.LandedStopIndex);
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
                float naturalEnd = _decelStartPx + displaySpinSpeed * _decelDuration / 3f;
                float targetBase = reel.TargetScrollPx;
                float candidate = targetBase
                    + Mathf.Round((naturalEnd - targetBase) / stripH) * stripH;
                while (candidate <= _decelStartPx + symbolHeight * 0.5f)
                    candidate += stripH;
                _decelEndPx = candidate;
            }

            float prevScrollPx = _scrollPx;

            if (_inDecel)
            {
                _decelElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_decelElapsed / _decelDuration);
                if (t >= 1f)
                {
                    _scrollPx = _decelEndPx;
                    _inDecel  = false;
                }
                else
                {
                    float h00 = 2*t*t*t - 3*t*t + 1;
                    float h10 = t*t*t - 2*t*t + t;
                    float h01 = -2*t*t*t + 3*t*t;
                    _scrollPx  = h00 * _decelStartPx
                                + h10 * displaySpinSpeed * _decelDuration
                                + h01 * _decelEndPx;
                    // Never go backward
                    if (_scrollPx < prevScrollPx) _scrollPx = prevScrollPx;
                }
            }
            else
            {
                _scrollPx += displaySpinSpeed * Time.deltaTime;
            }

            UpdateSlotSymbols(prevScrollPx);
            Render();
        }

        // Check if any slot crossed topY this frame and assign its new symbol
        private void UpdateSlotSymbols(float prevScrollPx)
        {
            int stripLen = reel.StopCount;
            float paylineY = -symbolHeight;

            for (int i = 0; i < SlotCount; i++)
            {
                float currY = GetSlotY(i);
                float prevSlotY = _prevY[i];
                _prevY[i] = currY;

                // Slot crossed topY when going from above to below
                // (i.e. it wrapped from near _topY back to _topY)
                // Detect: prev was close to topY from below, curr jumped to near topY from above
                // More reliably: prev < botY+threshold means it was about to recycle
                // A slot recycles when raw = (_scrollPx - phase) % poolH wraps from
                // near poolH back to near 0, causing Y to jump from near _botY to near _topY.
                // Detect by: prevY was near bottom AND currY is near top.
                bool justRecycled = prevSlotY < _botY + symbolHeight * 0.5f
                                 && currY     > _topY  - symbolHeight * 0.5f;
                if (!justRecycled) continue;

                // This slot just appeared at topY. What stop should it show?
                // Find the slot currently at paylineY (not this one) and offset from it.
                int paySlot = -1;
                float bestDist = float.MaxValue;
                for (int j = 0; j < SlotCount; j++)
                {
                    if (j == i) continue;
                    float d = Mathf.Abs(GetSlotY(j) - paylineY);
                    if (d < bestDist) { bestDist = d; paySlot = j; }
                }

                if (paySlot < 0) continue;

                int paylineStop = _slotStop[paySlot];
                int rowOffset = Mathf.RoundToInt((currY - paylineY) / symbolHeight);
                _slotStop[i]    = Mod(paylineStop + rowOffset, stripLen);
            }
        }

        private float GetSlotY(int i)
        {
            float raw = (_scrollPx - _phase[i]) % _poolH;
            if (raw < 0) raw += _poolH;
            return _topY - raw;
        }

        // After landing, assign correct symbols without moving slots
        private void SnapSymbols(int landedStop)
        {
            // VERSION: PhaseModel-v1 -- if you see this, the new file is active
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{name}] SnapSymbols v1 stop={landedStop} scrollPx={_scrollPx:F2}");
            for (int i = 0; i < SlotCount; i++)
                sb.AppendLine($"  slot{i} Y={GetSlotY(i):F2} stop={_slotStop[i]} dist={GetSlotY(i)+symbolHeight:F2}");
            Debug.Log(sb.ToString());

            int stripLen = reel.StopCount;
            float paylineY = -symbolHeight;

            // Find slot closest to payline
            int paySlot = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < SlotCount; i++)
            {
                float d = Mathf.Abs(GetSlotY(i) - paylineY);
                if (d < bestDist) { bestDist = d; paySlot = i; }
            }

            // Assign symbols by row offset from paySlot
            for (int i = 0; i < SlotCount; i++)
            {
                float y = GetSlotY(i);
                int rowOff = Mathf.RoundToInt((y - paylineY) / symbolHeight);
                _slotStop[i] = Mod(landedStop + rowOff, stripLen);
            }

            // Recompute phases from scratch -- never accumulate shift errors.
            // For each slot, phase is the _scrollPx value where that slot is at topY.
            // GetSlotY(i) = topY - (_scrollPx - phase[i]) % poolH
            // So: (_scrollPx - phase[i]) % poolH = topY - GetSlotY(i)
            // Therefore: phase[i] = _scrollPx - (topY - GetSlotY(i))  [mod poolH]
            for (int i = 0; i < SlotCount; i++)
            {
                float currentY = GetSlotY(i);
                float raw = _scrollPx - (_topY - currentY);
                _phase[i]      = ((raw % _poolH) + _poolH) % _poolH;
            }

            // Align _scrollPx to landed stop for next spin
            float stripTotalH = stripLen * symbolHeight;
            float targetBase = landedStop * symbolHeight;
            float aligned = targetBase
                + Mathf.Round((_scrollPx - targetBase) / stripTotalH) * stripTotalH;
            // Apply the same alignment to phases so positions don't move
            float shift = aligned - _scrollPx;
            _scrollPx     = aligned;
            for (int i = 0; i < SlotCount; i++)
                _phase[i] = ((_phase[i] + shift) % _poolH + _poolH) % _poolH;

            // Update prevY
            for (int i = 0; i < SlotCount; i++)
                _prevY[i] = GetSlotY(i);

            Render();
        }

        private void InitSlots(int landedStop)
        {
            int stripLen = reel.StopCount;
            // Initial phases so slot i starts at (1-i)*symbolHeight
            // GetSlotY(i) = topY - (_scrollPx - phase[i]) % poolH = (1-i)*symbolHeight
            // topY - (0 - phase[i]) % poolH = (1-i)*symbolHeight
            // (0 - phase[i]) % poolH = topY - (1-i)*symbolHeight = i*symbolHeight
            // (-phase[i]) % poolH = i*symbolHeight
            // phase[i] = -i*symbolHeight mod poolH = (poolH - i*symbolHeight) % poolH
            for (int i = 0; i < SlotCount; i++)
            {
                _phase[i]    = ((_poolH - i * symbolHeight) % _poolH + _poolH) % _poolH;
                _slotStop[i] = Mod(landedStop + (i - PaylineSlot), stripLen);
                _prevY[i]    = GetSlotY(i);
            }
            _scrollPx = 0f;
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