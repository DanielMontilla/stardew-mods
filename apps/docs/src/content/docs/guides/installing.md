---
title: Installing mods
description: How to install SMAPI and these mods on Windows, macOS, or Linux.
---

1. Install [SMAPI](https://smapi.io/) for your platform and run the installer once. It sets up the
   launcher that loads mods.
2. Download the mod release and unzip it into the game's `Mods` folder:
   - Windows: `%APPDATA%\StardewValley\Mods`
   - macOS: `~/.local/share/StardewValley/Mods`
   - Linux: `~/.local/share/StardewValley/Mods`
3. Keep the mod folder intact — the folder containing `manifest.json` must sit directly inside
   `Mods`.
4. Launch the game through SMAPI and check the console window for load errors.

:::tip
When updating, replace the whole mod folder. Leaving old files behind can cause duplicate or stale
content to load.
:::

## Configuration

Mods that have settings create a `config.json` next to their `manifest.json` the first time they
run. Close the game before editing it, then restart to apply changes.

## Next steps

- [Immersive Minecarts overview](/guides/immersive-minecarts/)
- [Immersive Minecarts configuration](/mods/immersive-minecarts/configuration/)
- [Minecart data reference](/guides/adding-minecart-spots/)
- [Browse all mods](/mods/)
