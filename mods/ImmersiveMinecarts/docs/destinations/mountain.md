---
title: Mountain Minecart
description: The Mountain route, its Clint and Robin conversation chain, and the three build stages.
order: 22
---

The Mountain route starts with people instead of a payment screen. Clint explains the problem and
sends you to Robin; once she has the timber and ingots, the survey is complete and the build stages
begin. See [Progression](/mods/immersive-minecarts/progression/) for how the conversation chain
works.

The values below follow the site's mode switch.

## Requirements

{{requirements:Mountain}}

## Configure the arrival point

The default drop-off is tile {{spot.Mountain.TargetTile.X}}, {{spot.Mountain.TargetTile.Y}} facing
{{spot.Mountain.TargetDirection}}. Override it per player in `config.json`:

```json
{
	"SpotPositions": {
		"Mountain": { "X": 53, "Y": 10, "Direction": "down" }
	}
}
```

Only the fields you set are overridden; everything else keeps the asset defaults. See
[Configuration](/mods/immersive-minecarts/configuration/) for the full option list.

## Unlocking it for testing

With `EnableDebugCommands` on (default `{{config.EnableDebugCommands}}`):

- `imm_unlock Mountain` pays the next stage normally.
- `imm_unlock Mountain force` advances one stage without requirements.
- `imm_lock Mountain` clears the route and its progress.

## See also

- [Destinations](/mods/immersive-minecarts/destinations/)
- [Progression](/mods/immersive-minecarts/progression/)
- [Configuration](/mods/immersive-minecarts/configuration/)
- [The Mountain](https://stardewvalleywiki.com/The_Mountain)
