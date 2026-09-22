# Earthy Lawn Grass

[Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/52657)

Makes [Lawn Grass](https://www.nexusmods.com/stardewvalley/mods/47165) match your recolour pack.

Lawn Grass ships its own bright green lawn sprites. Recolour packs don't touch them, because those
sprites are the mod's own assets rather than vanilla ones, so the lawn stays neon while the rest of
the farm goes muted. This mod repaints the lawn at runtime using the grass tile from whichever
outdoor tilesheet is actually loaded — your recolour's, or vanilla's if you don't use one.

**It ships no art.** Nothing is copied from any recolour pack or from Lawn Grass; the pixels come
from the tilesheet already loaded in your game, so the lawn matches whatever you install, including
future updates to that pack.

It can also **keep a mown lawn mown**: a lawn tile cut down to bare ground only starts growing back
with a small daily chance (`LawnSproutChance`). Once it sprouts, it grows at the normal rate, so the
rest of your grass isn't slowed down.

## Install

1. Install [SMAPI](https://smapi.io) 4.0.0 or later.
2. Install [Lawn Grass](https://www.nexusmods.com/stardewvalley/mods/47165) (required).
3. Unzip this mod into `Stardew Valley/Mods`, or install the zip through Vortex.
4. Run the game once to generate `config.json`.

## Configuration

`config.json` is created on first run. If you have
[Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) installed (optional),
you can change every setting in-game instead: title screen cog, or "Mod Options" in the game menu.
Colour changes apply immediately, with no restart.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | `true` | Set to `false` to leave the lawn as Lawn Grass draws it. |
| `TileIndex` | `175` | Which tile of `Maps/<season>_outdoorsTileSheet` the lawn is painted with. `175` is the plain grass used across the farm map. `351` is the darker mown patch the map draws around the farmhouse. |
| `LawnSproutChance` | `0.01` | The daily chance that a mown lawn tile (height 0) starts growing again. Once it sprouts it grows at the usual rate, so a mown lawn stays mown without slowing the rest of your grass. `1` turns this off. For the usual rate after sprouting, leave Lawn Grass's own `GrowChance` at `1`. |

## How it works

When the game requests `aedenthorn.LawnGrass/lawn_<season>`, this mod edits it:

1. Load `Maps/<season>_outdoorsTileSheet` through the content pipeline, so any recolour pack has
   already been applied.
2. Read the 16x16 tile at `TileIndex`.
3. Keep Lawn Grass's own sprite shape (its alpha channel) and fill it with that tile, repeated.

It also listens for tilesheet invalidation, so the lawn refreshes when a recolour pack reloads.

If the tilesheet can't be loaded, or `TileIndex` falls outside it, the mod logs a warning and leaves
the lawn untouched rather than breaking it.

### Keeping a mown lawn mown

The game grows grass twice each night: `Grass.dayUpdate` on every tile, then the spreading pass
`GameLocation.growWeedGrass`, which also grows existing grass. A tile that starts the night at height 0
(bare lawn) gets one roll against `LawnSproutChance` in the first; if it fails, it's held at 0 through
both. Tiles at any other height are left alone. Only a lawn
tile can sit at height 0 (the game removes ordinary grass that reaches it), so nothing else is
affected. Lawn Grass's `GrowChance` slows every growth stage alike, which is why this is separate.

## Compatibility

- Stardew Valley 1.6.14+, SMAPI 4.0.0+, single player and multiplayer.
- Works with any recolour pack, or with vanilla art.
- `LawnSproutChance` patches `Grass.dayUpdate`. Another mod that also changes how bare lawn regrows
  may fight it; set `LawnSproutChance` to `1` to switch this part off.
- Don't run this alongside a Content Patcher pack that replaces the same lawn assets; both would
  patch `aedenthorn.LawnGrass/lawn_<season>` and the results would depend on load order.

## Building from source

Needs the .NET 6 SDK (or a newer SDK that can target `net6.0`).

```sh
dotnet build -c Release
```

[ModBuildConfig](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md)
finds the game folder automatically and writes a release zip to `dist/`. The project sets
`EnableModDeploy=false`, so it doesn't copy anything into your `Mods` folder while building.

## Updates

SMAPI checks for new versions through the `Nexus:52657` and `GitHub:dhanjit/EarthyLawnGrass` update keys
([Nexus page](https://www.nexusmods.com/stardewvalley/mods/52657); GitHub matches against this repo's release tags). To release: bump `Version` in `manifest.json` and the `.csproj`,
build, then create a GitHub release tagged with the version and attach the zip from `dist/`.

## License

MIT, see [LICENSE](LICENSE).

## Credits

- Lawn Grass by aedenthorn — required; this mod only repaints its lawn.
- Any recolour pack you use supplies the colours; none of its files are redistributed here.
