using System.Collections.Generic;

namespace FORGE
{
    /// <summary>
    /// Plain C# class — no Unity dependencies.
    /// Tracks all mutable session variables.
    /// GameManager owns the single instance and mutates it.
    /// </summary>
    public class SessionState
    {
        public float Credits { get; private set; }
        public int SpinCount { get; private set; }
        public float TotalWagered { get; private set; }
        public float TotalPaid { get; private set; }
        public int WinCount { get; private set; }
        public int SurgeCount { get; private set; }
        public int CurrentDrySpell { get; private set; }
        public int LongestDrySpell { get; private set; }

        public float RealizedRTP =>
            TotalWagered > 0f ? TotalPaid / TotalWagered : 0f;

        public float HitFrequency =>
            SpinCount > 0 ? WinCount / (float)SpinCount : 0f;

        public SessionState(float startingCredits)
        {
            Credits = startingCredits;
        }

        // ── Mutations — called only by GameManager ────────────────────

        public void RecordSpin(float betSize, float payoutMultiplier, bool surgeTriggered)
        {
            float payout  = betSize * payoutMultiplier;

            Credits      -= betSize;
            Credits      += payout;
            TotalWagered += betSize;
            TotalPaid    += payout;
            SpinCount++;

            bool isWin = payoutMultiplier > 0f;
            if (isWin)
            {
                WinCount++;
                LongestDrySpell = System.Math.Max(LongestDrySpell, CurrentDrySpell);
                CurrentDrySpell = 0;
            }
            else
            {
                CurrentDrySpell++;
            }

            if (surgeTriggered)
                SurgeCount++;
        }

        public void Reset(float startingCredits)
        {
            Credits         = startingCredits;
            SpinCount       = 0;
            TotalWagered    = 0f;
            TotalPaid       = 0f;
            WinCount        = 0;
            SurgeCount      = 0;
            CurrentDrySpell = 0;
            LongestDrySpell = 0;
        }
    }
}
