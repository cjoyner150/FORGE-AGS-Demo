using System;
using UnityEngine;

namespace FORGE
{
    /// <summary>
    /// Single source of truth for all game math parameters.
    /// Edit in the Inspector; no magic numbers in code.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "FORGE/Game Config")]
    public class GameConfig : ScriptableObject
    {
        // ── Denomination ──────────────────────────────────────────────
        [Header("Denomination")]
        [Tooltip("Credit value in dollars.")]
        public float creditValue = 1.00f;

        // ── Paytable ──────────────────────────────────────────────────
        [Header("Paytable (3-of-a-kind base multipliers)")]
        public float payScrap   = 2f;
        public float payShatter = 2f;
        public float payIngots  = 4f;
        public float payPlate   = 7f;
        public float payMolten  = 15f;

        /// <summary>Returns the base 3-of-a-kind payout for a non-wild symbol.</summary>
        public float GetBaseValue(SymbolType symbol)
        {
            return symbol switch
            {
                SymbolType.Scrap    => payScrap,
                SymbolType.Shatter  => payShatter,
                SymbolType.Ingots   => payIngots,
                SymbolType.Plate    => payPlate,
                SymbolType.Molten   => payMolten,
                _                   => 0f,
            };
        }

        /// <summary>The highest base payout — used for the 3-wild case.</summary>
        public float BestSymbolValue => Mathf.Max(payScrap, payShatter, payIngots, payPlate, payMolten);

        // ── Wild Multipliers ─────────────────────────────────────────
        [Header("Wild Multipliers — Normal State")]
        public float wildMult1Normal = 2f;
        public float wildMult2Normal = 4f;
        public float wildMult3Normal = 8f;

        [Header("Wild Multipliers — Surge State")]
        public float wildMult1Surge  = 3f;
        public float wildMult2Surge  = 6f;
        public float wildMult3Surge  = 12f;

        /// <summary>Returns the wild multiplier for a given wild count and game state.</summary>
        public float GetWildMultiplier(int wildCount, bool isSurge)
        {
            return wildCount switch
            {
                1 => isSurge ? wildMult1Surge : wildMult1Normal,
                2 => isSurge ? wildMult2Surge : wildMult2Normal,
                3 => isSurge ? wildMult3Surge : wildMult3Normal,
                _ => 1f,
            };
        }

        // ── Surge Parameters ─────────────────────────────────────────
        [Header("Wild Surge")]
        [Range(0f, 0.1f)]
        public float surgeTriggerChance = 0.01f;

        [Range(1, 10)]
        public int surgeDurationSpins = 3;
    }
}
