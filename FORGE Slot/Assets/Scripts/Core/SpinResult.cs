namespace FORGE
{
    /// <summary>
    /// Immutable record of everything that happened on a single spin.
    /// Produced by PaylineEvaluator; consumed by GameManager, UI, and AudioManager.
    /// </summary>
    public class SpinResult
    {
        // ── Reel outcomes ─────────────────────────────────────────────
        public readonly SymbolType Reel1;
        public readonly SymbolType Reel2;
        public readonly SymbolType Reel3;

        // ── Math outcomes ─────────────────────────────────────────────
        /// <summary>Total payout as a multiple of bet. 0 = no win.</summary>
        public readonly float PayoutMultiplier;

        /// <summary>Wild multiplier applied (1 if no wilds in win).</summary>
        public readonly float WildMultiplier;

        /// <summary>Number of wilds that contributed to the win.</summary>
        public readonly int WildCount;

        /// <summary>The non-wild symbol that formed the match (null if 3-wild).</summary>
        public readonly SymbolType? MatchedSymbol;

        // ── State flags ───────────────────────────────────────────────
        public readonly bool IsWin;
        public readonly bool IsSurge;
        public readonly bool SurgeTriggered;   // true on the spin that first triggered surge
        public readonly int  SurgeSpinsRemaining;

        public SpinResult(
            SymbolType reel1, SymbolType reel2, SymbolType reel3,
            float payoutMultiplier, float wildMultiplier, int wildCount,
            SymbolType? matchedSymbol,
            bool isSurge, bool surgeTriggered, int surgeSpinsRemaining)
        {
            Reel1  = reel1;
            Reel2  = reel2;
            Reel3  = reel3;

            PayoutMultiplier    = payoutMultiplier;
            WildMultiplier      = wildMultiplier;
            WildCount           = wildCount;
            MatchedSymbol       = matchedSymbol;

            IsWin               = payoutMultiplier > 0f;
            IsSurge             = isSurge;
            SurgeTriggered      = surgeTriggered;
            SurgeSpinsRemaining = surgeSpinsRemaining;
        }

        public override string ToString() =>
            $"[{Reel1}|{Reel2}|{Reel3}] " +
            $"Payout:{PayoutMultiplier}x WildMult:{WildMultiplier}x Wilds:{WildCount} " +
            $"Surge:{IsSurge}({SurgeSpinsRemaining} left)";
    }
}
