namespace FORGE
{
    /// <summary>
    /// Pure static math class. No MonoBehaviour, no Unity dependencies.
    /// Given three landed symbols and a config, returns a fully resolved SpinResult.
    ///
    /// Evaluation rules:
    ///   - Three matching non-wild symbols → base payout
    ///   - Any combination where non-wilds all match → base payout × wild multiplier
    ///   - Three wilds → bestSymbolValue × wildMult3 (the 3-wild jackpot case)
    ///   - Anything else → 0 (no win)
    /// </summary>
    public static class PaylineEvaluator
    {
        /// <summary>
        /// Resolve a single payline. Called with the landed symbols from the three reels
        /// and the current game state.
        /// </summary>
        public static (float payout, float wildMult, int wildCount, SymbolType? matched)
            Evaluate(SymbolType s1, SymbolType s2, SymbolType s3, GameConfig cfg, bool isSurge)
        {
            int wildCount = CountWilds(s1, s2, s3);

            // ── Three wilds ───────────────────────────────────────────
            if (wildCount == 3)
            {
                float wm3   = cfg.GetWildMultiplier(3, isSurge);
                float pay3w = cfg.BestSymbolValue * wm3;
                return (pay3w, wm3, 3, null);
            }

            // ── Collect non-wild symbols ──────────────────────────────
            SymbolType[] nonWilds = GetNonWilds(s1, s2, s3);

            // All non-wilds must be the same symbol for a win
            if (!AllSame(nonWilds))
                return (0f, 1f, 0, null);

            SymbolType matchedSym = nonWilds[0];
            float baseValue       = cfg.GetBaseValue(matchedSym);

            if (wildCount == 0)
                return (baseValue, 1f, 0, matchedSym);

            float wm  = cfg.GetWildMultiplier(wildCount, isSurge);
            float pay = baseValue * wm;
            return (pay, wm, wildCount, matchedSym);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static int CountWilds(SymbolType s1, SymbolType s2, SymbolType s3)
        {
            int n = 0;
            if (s1 == SymbolType.Wild) n++;
            if (s2 == SymbolType.Wild) n++;
            if (s3 == SymbolType.Wild) n++;
            return n;
        }

        private static SymbolType[] GetNonWilds(SymbolType s1, SymbolType s2, SymbolType s3)
        {
            var list = new System.Collections.Generic.List<SymbolType>(3);
            if (s1 != SymbolType.Wild) list.Add(s1);
            if (s2 != SymbolType.Wild) list.Add(s2);
            if (s3 != SymbolType.Wild) list.Add(s3);
            return list.ToArray();
        }

        private static bool AllSame(SymbolType[] syms)
        {
            if (syms.Length == 0) return false;
            for (int i = 1; i < syms.Length; i++)
                if (syms[i] != syms[0]) return false;
            return true;
        }
    }
}
