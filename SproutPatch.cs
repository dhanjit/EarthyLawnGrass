using HarmonyLib;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace EarthyLawnGrass;

/// <summary>Keeps a mown lawn mown. A lawn tile at height 0 only starts growing with
/// <see cref="ModConfig.LawnSproutChance"/> per day; once it has sprouted, growth is left to the game and
/// to Lawn Grass. Only a lawn can sit at height 0 - the game removes ordinary grass that reaches it - so
/// the height alone identifies the tiles this applies to.</summary>
[HarmonyPatch(typeof(Grass), nameof(Grass.dayUpdate))]
internal static class SproutPatch
{
    internal static ModConfig Config = null!;

    /// <summary>Record the height before the game and Lawn Grass update it.</summary>
    [HarmonyPriority(Priority.First)]
    public static void Prefix(Grass __instance, out int __state)
    {
        __state = __instance.numberOfWeeds.Value;
    }

    /// <summary>Runs after Lawn Grass's own postfix, so this has the last word on a height-0 tile.</summary>
    [HarmonyAfter("aedenthorn.LawnGrass")]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(Grass __instance, int __state)
    {
        if (Config.LawnSproutChance >= 1f || __state != 0 || __instance.grassType.Value != Grass.springGrass)
            return;
        if (__instance.numberOfWeeds.Value > 0 && Game1.random.NextDouble() >= Config.LawnSproutChance)
            __instance.numberOfWeeds.Value = 0;
    }
}
