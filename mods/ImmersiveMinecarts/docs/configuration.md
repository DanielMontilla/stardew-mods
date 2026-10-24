---
title: Configuration
description: Every option with its default value, read from ModConfig.cs.
order: 10
---

The mod writes `config.json` next to its `manifest.json` the first time it runs. These are the
defaults:

{{table:config}}

## Difficulty

`Difficulty` picks a preset from `assets/difficulties.json`. New installs start on
**{{config.Difficulty}}**. The mode switch in the header (and mobile menu) changes the costs shown
across the site and remembers your choice between visits.

{{table:difficulties}}

Waiting days are never scaled. See [Progression](/mods/immersive-minecarts/progression/) for how stages and
waits work.

## Moving carts

`SpotPositions` overrides where a route drops the player off:

```json
"SpotPositions": {
    "Desert": { "X": 35, "Y": 40, "Direction": "left" }
}
```

| Field | Meaning |
| --- | --- |
| `X` | Arrival tile X in the route's target location |
| `Y` | Arrival tile Y in the route's target location |
| `Direction` | `up`, `down`, `left`, or `right` |

Unset fields keep the values from `assets/minecart-spots.json`. An override also wins over the automatic Farm
Home farmhouse entry, which normally follows a moved farmhouse. In game, these are the
**Minecart positions** pages in [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).

## Multiplayer

Difficulty and positions are local install settings, so each player's client can use a different preset.
Progress is still per player. See [Progression](/mods/immersive-minecarts/progression/).

## See also

- [Progression](/mods/immersive-minecarts/progression/)
- [Destinations](/mods/immersive-minecarts/destinations/)
- [SMAPI config API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Config)
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)
- [Data reference](/guides/adding-minecart-spots/)
