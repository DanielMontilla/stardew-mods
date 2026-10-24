---
title: Secret Woods Minecart
description: The single-payment Secret Woods route.
order: 23
---

The Secret Woods route is a single delivery. Pay the gold, hand over the materials, and the minecart
opens immediately â€” there are no stages or wait times to track.

The values below follow the site's mode switch.

## Requirements

{{requirements:SecretWoods}}

## Configure the arrival point

The default drop-off is tile {{spot.SecretWoods.TargetTile.X}}, {{spot.SecretWoods.TargetTile.Y}}
facing {{spot.SecretWoods.TargetDirection}}. Override it per player in `config.json`:

```json
{
	"SpotPositions": {
		"SecretWoods": { "X": 46, "Y": 8, "Direction": "down" }
	}
}
```

Only the fields you set are overridden; everything else keeps the asset defaults. See
[Configuration](/mods/immersive-minecarts/configuration/) for the full option list.

## Unlocking it for testing

With `EnableDebugCommands` on (default `{{config.EnableDebugCommands}}`):

- `imm_unlock SecretWoods` pays the route normally.
- `imm_unlock SecretWoods force` unlocks it without requirements.
- `imm_lock SecretWoods` clears it.

## See also

- [Destinations](/mods/immersive-minecarts/destinations/)
- [Configuration](/mods/immersive-minecarts/configuration/)
- [Secret Woods](https://stardewvalleywiki.com/Secret_Woods)
