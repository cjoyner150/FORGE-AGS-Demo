using UnityEngine;

namespace FORGE
{
    /// <summary>
    /// Maps each SymbolType to a Sprite.
    /// Create one asset: Assets/FORGE/Data/SymbolSpriteLibrary.asset
    /// Assign your placeholder images here — swap for final art later
    /// without touching any code.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolSpriteLibrary", menuName = "FORGE/Symbol Sprite Library")]
    public class SymbolSpriteLibrary : ScriptableObject
    {
        [Header("Assign one sprite per symbol — placeholders are fine")]
        public Sprite scrap;
        public Sprite shatter;
        public Sprite ingots;
        public Sprite plate;
        public Sprite molten;
        public Sprite wild;

        public Sprite GetSprite(SymbolType symbol)
        {
            return symbol switch
            {
                SymbolType.Scrap    => scrap,
                SymbolType.Shatter  => shatter,
                SymbolType.Ingots   => ingots,
                SymbolType.Plate    => plate,
                SymbolType.Molten   => molten,
                SymbolType.Wild     => wild,
                _                   => null,
            };
        }

        /// <summary>Short display name used as fallback when sprite is null.</summary>
        public static string GetLabel(SymbolType symbol)
        {
            return symbol switch
            {
                SymbolType.Scrap    => "SCR",
                SymbolType.Shatter  => "SHT",
                SymbolType.Ingots   => "ING",
                SymbolType.Plate    => "PLT",
                SymbolType.Molten   => "MLT",
                SymbolType.Wild     => "WLD",
                _                   => "???",
            };
        }
    }
}
