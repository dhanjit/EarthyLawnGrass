using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace EarthyLawnGrass;

internal sealed class ModConfig
{
    /// <summary>Whether to retexture the lawn at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The tile index in <c>Maps/&lt;season&gt;_outdoorsTileSheet</c> to take the lawn colour from.
    /// 175 is the plain grass the farm map uses; 351 is the darker mown patch by the farmhouse.</summary>
    public int TileIndex { get; set; } = 175;

    /// <summary>The daily chance that a mown lawn tile (height 0) starts growing again. Once it has
    /// sprouted it grows at the normal rate. 1 turns this off and leaves growth to Lawn Grass alone.</summary>
    public float LawnSproutChance { get; set; } = 0.01f;
}

/// <summary>Retextures aedenthorn's Lawn Grass with the grass tile from whichever outdoor tilesheet is
/// loaded, so the lawn matches the player's recolour pack (or vanilla) without shipping any art.</summary>
public class ModEntry : Mod
{
    private const string AssetPrefix = "aedenthorn.LawnGrass/lawn_";
    private const int TileSize = 16;
    private static readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

    private ModConfig Config = null!;

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();
        SproutPatch.Config = this.Config;
        new Harmony(this.ModManifest.UniqueID).PatchAll();
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.DayStarted += (_, _) => SproutPatch.HeldTonight.Clear();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => SproutPatch.HeldTonight.Clear();
    }

    /// <summary>Add the settings to Generic Mod Config Menu, if it's installed.</summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var menu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (menu == null)
            return;

        menu.Register(
            mod: this.ModManifest,
            reset: () =>
            {
                this.Config = new ModConfig();
                SproutPatch.Config = this.Config;
            },
            save: () =>
            {
                this.Config.LawnSproutChance = Math.Clamp(this.Config.LawnSproutChance, 0f, 1f);
                this.Helper.WriteConfig(this.Config);
                this.RepaintLawn();
            }
        );

        menu.AddSectionTitle(this.ModManifest, () => "Lawn colour");
        menu.AddBoolOption(
            mod: this.ModManifest,
            getValue: () => this.Config.Enabled,
            setValue: value => this.Config.Enabled = value,
            name: () => "Repaint the lawn",
            tooltip: () => "Paint Lawn Grass with the grass tile from your recolour pack (or vanilla). Off leaves the lawn as Lawn Grass draws it."
        );
        menu.AddNumberOption(
            mod: this.ModManifest,
            getValue: () => this.Config.TileIndex,
            setValue: value => this.Config.TileIndex = value,
            name: () => "Tile index",
            tooltip: () => "Which tile of the outdoor tilesheet the lawn is painted with. 175 is the plain farm grass; 351 is the darker mown patch by the farmhouse.",
            min: 0
        );

        menu.AddSectionTitle(this.ModManifest, () => "Mown lawn");
        menu.AddNumberOption(
            mod: this.ModManifest,
            getValue: () => this.Config.LawnSproutChance,
            setValue: value => this.Config.LawnSproutChance = value,
            name: () => "Sprout chance per day (0-1)",
            tooltip: () => "The daily chance (0 to 1) that a mown lawn tile starts growing again. 0.01 = 1%, 0.001 = 0.1%, 0 = never. Once it sprouts it grows at the usual rate. 1 turns this off."
        );
    }

    /// <summary>Reload the lawn sprites so a changed colour setting shows up without restarting.</summary>
    private void RepaintLawn()
    {
        foreach (string season in Seasons)
            this.Helper.GameContent.InvalidateCache(AssetPrefix + season);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!this.Config.Enabled)
            return;

        string name = e.NameWithoutLocale.Name;
        if (!name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase))
            return;

        string season = name[AssetPrefix.Length..].ToLowerInvariant();
        if (Array.IndexOf(Seasons, season) < 0)
            return;

        e.Edit(asset => this.Retexture(asset, season), AssetEditPriority.Late);
    }

    /// <summary>Re-run our edit when the source tilesheets change, e.g. a recolour pack updates.</summary>
    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        bool sheetChanged = false;
        foreach (IAssetName asset in e.NamesWithoutLocale)
        {
            if (asset.Name.Contains("_outdoorsTileSheet", StringComparison.OrdinalIgnoreCase))
            {
                sheetChanged = true;
                break;
            }
        }
        if (!sheetChanged)
            return;

        this.RepaintLawn();
    }

    /// <summary>Fill the mod's lawn sprite (which supplies the shape) with the map's grass tile (which supplies the pixels).</summary>
    private void Retexture(IAssetData asset, string season)
    {
        IAssetDataForImage image = asset.AsImage();
        Texture2D lawn = image.Data;

        GraphicsDevice? device = Game1.graphics?.GraphicsDevice;
        if (device == null)
            return;

        Texture2D sheet;
        try
        {
            sheet = this.Helper.GameContent.Load<Texture2D>($"Maps/{season}_outdoorsTileSheet");
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Couldn't load the {season} tilesheet, leaving the lawn as-is: {ex.Message}", LogLevel.Warn);
            return;
        }

        int columns = sheet.Width / TileSize;
        int tileX = this.Config.TileIndex % columns * TileSize;
        int tileY = this.Config.TileIndex / columns * TileSize;
        if (columns <= 0 || tileX + TileSize > sheet.Width || tileY + TileSize > sheet.Height)
        {
            this.Monitor.Log($"Tile index {this.Config.TileIndex} is outside the {season} tilesheet ({sheet.Width}x{sheet.Height}); leaving the lawn as-is.", LogLevel.Warn);
            return;
        }

        Color[] grass = new Color[TileSize * TileSize];
        sheet.GetData(0, new Rectangle(tileX, tileY, TileSize, TileSize), grass, 0, grass.Length);

        Color[] shape = new Color[lawn.Width * lawn.Height];
        lawn.GetData(shape);

        Color[] output = new Color[shape.Length];
        for (int y = 0; y < lawn.Height; y++)
        {
            for (int x = 0; x < lawn.Width; x++)
            {
                int i = (y * lawn.Width) + x;
                byte alpha = shape[i].A;
                if (alpha == 0)
                    continue;

                Color source = grass[(y % TileSize * TileSize) + (x % TileSize)];
                float scale = alpha / 255f;   // textures are premultiplied
                output[i] = new Color(
                    (byte)(source.R * scale),
                    (byte)(source.G * scale),
                    (byte)(source.B * scale),
                    alpha
                );
            }
        }

        Texture2D patched = new(device, lawn.Width, lawn.Height);
        patched.SetData(output);
        image.PatchImage(patched);
    }
}
