using UnityEngine;

namespace FORGE
{
    /// <summary>
    /// One reel strip — an ordered array of 22 symbols.
    /// Create two assets: NormalStrip and SurgeStrip.
    /// SurgeController swaps the reference on each Reel at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "ReelStrip", menuName = "FORGE/Reel Strip")]
    public class ReelStripData : ScriptableObject
    {
        [Tooltip("Exactly 22 symbols in reel order.")]
        public SymbolType[] stops = new SymbolType[22];

        /// <summary>Returns the symbol at a given stop index, wrapping safely.</summary>
        public SymbolType GetStop(int index)
        {
            return stops[((index % stops.Length) + stops.Length) % stops.Length];
        }

        public int StopCount => stops.Length;

        /// <summary>
        /// Utility: counts how many times a symbol appears on this strip.
        /// Used by the editor and simulator for validation.
        /// </summary>
        public int CountOf(SymbolType symbol)
        {
            int count = 0;
            foreach (var s in stops)
                if (s == symbol) count++;
            return count;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (stops.Length != 22)
                Debug.LogWarning($"[ReelStrip] {name}: strip has {stops.Length} stops, expected 22.", this);
        }
#endif
    }
}
