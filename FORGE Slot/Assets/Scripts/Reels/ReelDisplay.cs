using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FORGE
{
    public class ReelDisplay : MonoBehaviour
    {
        [SerializeField] private Reel reel;
        [SerializeField] private SymbolSpriteLibrary spriteLibrary;

        [Tooltip("Height of one symbol slot in pixels. Must match slot RectTransform height.")]
        [SerializeField] private float symbolHeight = 120f;

        [Tooltip("5 slot RectTransforms ordered top to bottom.")]
        [SerializeField] private RectTransform[] slots = new RectTransform[5];

        private Image[] _images;
        private TMP_Text[] _labels;

        private const int SlotCount = 5;

        private void Awake()
        {
            _images = new Image[SlotCount];
            _labels = new TMP_Text[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null) continue;
                _images[i] = slots[i].GetComponent<Image>();
                _labels[i] = slots[i].GetComponentInChildren<TMP_Text>();
            }
        }

        private void LateUpdate()
        {
            if (reel == null) return;
            UpdateSlots(reel.VisualOffset);
        }

        private void UpdateSlots(float offset)
        {
            int stripLen = reel.StopCount;
            float poolH = SlotCount * symbolHeight;
            float scrollPx = offset * stripLen * symbolHeight;
            float scroll = scrollPx % poolH;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null) continue;

                // Base positions: slot 0 = +symbolHeight (bleed above)
                //                 slot 1 = 0             (top row)
                //                 slot 2 = -symbolHeight  (PAYLINE)
                //                 slot 3 = -2*symbolHeight
                //                 slot 4 = -3*symbolHeight (bleed below)
                float baseY = (1 - i) * symbolHeight;
                float y = baseY - scroll;

                // Wrap up when slot scrolls past bottom bleed
                if (y < -(SlotCount - 1) * symbolHeight)
                    y += poolH;

                slots[i].anchoredPosition = new Vector2(0f, y);

                // Strip index: how many stops scrolled + slot offset
                int stopIdx = Mod(Mathf.FloorToInt(scrollPx / symbolHeight) + i, stripLen);
                SetSlotVisual(i, stopIdx);
            }
        }

        private void SetSlotVisual(int slotIndex, int stopIndex)
        {
            SymbolType symbol = reel.GetSymbolAt(stopIndex);

            if (_images[slotIndex] != null)
            {
                Sprite spr = spriteLibrary != null ? spriteLibrary.GetSprite(symbol) : null;
                _images[slotIndex].sprite  = spr;
                _images[slotIndex].enabled = spr != null;
            }

            if (_labels[slotIndex] != null)
                _labels[slotIndex].text = SymbolSpriteLibrary.GetLabel(symbol);
        }

        private static int Mod(int a, int b) => ((a % b) + b) % b;
    }
}