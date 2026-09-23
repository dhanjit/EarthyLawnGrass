using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace EarthyLawnGrass;

/// <summary>Keeps a mown lawn mown. Each night a lawn tile at height 0 gets one roll against
/// <see cref="ModConfig.LawnSproutChance"/>; if it fails, the tile stays at 0 through everything that grows
/// grass overnight. Once it has sprouted, growth is left to the game and to Lawn Grass. Only a lawn can sit
/// at height 0 - the game removes ordinary grass that reaches it - so the height alone identifies the tiles.
///
/// The game grows grass in two places each night, in this order: <see cref="Grass.dayUpdate"/> for every
/// tile, then <see cref="GameLocation.growWeedGrass"/> (the spreading pass, which also grows existing
/// grass). Both are covered; the roll happens once, in the first.</summary>
[HarmonyPatch(typeof(Grass), nameof(Grass.dayUpdate))]
internal static class SproutPatch
{
    internal static ModConfig Config = null!;

    /// <summary>Tiles that failed tonight's roll and must stay at 0 until morning.</summary>
    internal static readonly HashSet<Grass> HeldTonight = new(ReferenceEqualityComparer.Instance);

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
        if (Game1.random.NextDouble() < Config.LawnSproutChance)
            return;
        __instance.numberOfWeeds.Value = 0;
        HeldTonight.Add(__instance);
    }
}

/// <summary>The spreading pass runs after every tile's <see cref="Grass.dayUpdate"/>; undo any growth it gave a held tile.</summary>
[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.growWeedGrass))]
internal static class SpreadGrowthPatch
{
    [HarmonyPriority(Priority.Last)]
    public static void Postfix()
    {
        foreach (Grass grass in SproutPatch.HeldTonight)
        {
            if (grass.numberOfWeeds.Value > 0)
                grass.numberOfWeeds.Value = 0;
        }
    }
}
