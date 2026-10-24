---
title: Minecart data reference
description: Field-by-field reference for minecart-spots.json and difficulties.json, plus how to add a route.
sidebar:
  order: 12
---

The mod is data-driven. Routes live in
[`assets/minecart-spots.json`](/mods/immersive-minecarts/), difficulty multipliers live in
`assets/difficulties.json`, and the generated [mod page](/mods/immersive-minecarts/) renders both as tables.

This page is for anyone editing those files. For gameplay, start with
[Immersive Minecarts](/guides/immersive-minecarts/); for settings, see
[configuration](/mods/immersive-minecarts/configuration/).

## How the pieces connect

1. `MinecartSpotRepository` loads and validates the JSON.
2. `DifficultyRepository` loads the presets and scales gold, item counts, and ride fares.
3. `MinecartDataEditor` writes each effective spot into
   [`Data/Minecarts`](https://stardewvalleywiki.com/Modding:Minecarts) through the
   [SMAPI content API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content).
4. Each destination gets the condition
   `PLAYER_HAS_MAIL Current ImmersiveMinecarts.Unlocked.<spotId>`, so only the current player's unlocked routes
   appear. Conditions use [game state queries](https://stardewvalleywiki.com/Modding:Game_state_queries).
5. `ProgressionService` stores staged progress in the farmer's
   [mod data](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Data) and grants the final mail flag.

## Route schema

```json
{
    "Id": "Desert",
    "Network": "Default",
    "DisplayNameKey": "spot.desert.name",
    "TargetLocation": "Desert",
    "TargetTile": { "X": 32, "Y": 43 },
    "TargetDirection": "down",
    "RidePrice": 250,
    "Unlock": {
        "Condition": "PLAYER_VISITED_LOCATION Current Desert",
        "Steps": []
    }
}
```

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | string | Unique route ID. Used in save data, mail flags, and config overrides |
| `Network` | string | Network ID in [`Data/Minecarts`](https://stardewvalleywiki.com/Modding:Minecarts); usually `Default` |
| `DisplayNameKey` | string | [Translation key](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation) in `i18n/default.json` |
| `TargetLocation` | string | Internal location name, like `Desert` or `IslandWest` |
| `TargetTile` | `{ X, Y }` | Arrival tile; see [tile coordinates](https://stardewvalleywiki.com/Modding:Modder_Guide/Game_Fundamentals#Tiles) |
| `TargetDirection` | string | `up`, `down`, `left`, or `right` |
| `RidePrice` | int | Per-ride gold, separate from the unlock cost |
| `UseMainFarmHouseEntry` | bool | Optional. Uses the farmhouse door tile; requires `TargetLocation` = `Farm` |
| `Unlock` | object | Costs and conditions; see below |

### Unlock object

| Field | Type | Notes |
| --- | --- | --- |
| `Condition` | string? | Optional [game state query](https://stardewvalleywiki.com/Modding:Game_state_queries) |
| `Gold` | int | Single-step gold cost; ignored when `Steps` is non-empty |
| `Items` | array | Single-step item costs; ignored when `Steps` is non-empty |
| `Steps` | array | Staged payments; see below |

### Step object

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | string | Unique within the route; used in progress data |
| `NameKey` | string | Translation key for the step name |
| `DescriptionKey` | string? | Optional translation key for the step description |
| `Gold` | int | Gold paid for this stage |
| `Condition` | string? | Optional extra [game state query](https://stardewvalleywiki.com/Modding:Game_state_queries) |
| `Items` | array | Items handed over for this stage |
| `WaitDays` | int | Days to wait **before** this stage, counted from the previous stage |
| `NpcFlow` | array | Optional NPC conversations that must happen before the stage can be paid |

`WaitDays` on the first stage is ignored. Waiting is measured in in-game days with `Game1.Date.TotalDays`, so
it survives saving and reloading.

## NPC flow

A step can require conversations before its payment. The Mountain first stage uses this to send the player from
[Clint](https://stardewvalleywiki.com/Clint) to [Robin](https://stardewvalleywiki.com/Robin):

```json
"NpcFlow": [
    {
        "Npc": "Clint",
        "Topic": "ImmersiveMinecarts.Mountain.Hermit.Clint",
        "DialogueKey": "dialogue.mountain.hermit.clint"
    },
    {
        "Npc": "Robin",
        "Topic": "ImmersiveMinecarts.Mountain.Hermit.Robin",
        "DialogueKey": "dialogue.mountain.hermit.robin"
    }
]
```

| Field | Type | Notes |
| --- | --- | --- |
| `Npc` | string | Internal NPC name, like `Clint` or `Robin` |
| `Topic` | string | Unique conversation topic ID added to the player's active dialogue events |
| `DialogueKey` | string | [Translation key](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation) for the NPC's line |

How it works:

1. The mod adds a [conversation topic](https://stardewvalleywiki.com/Modding:Dialogue) to the player for the
   next NPC in the chain.
2. A matching key is added to `Characters/Dialogue/<npc>` with the translated text and a custom
   [`$action`](https://stardewvalleywiki.com/Modding:Dialogue).
3. That action records the conversation and either adds the next NPC's topic or tries to pay the stage.
4. If the final NPC can't complete the payment (missing items, gold, or condition), the topic is refreshed and
   the conversation can be repeated.

Topics last 14 days and are refreshed each morning while they are still the current step. Flow progress and
seen markers are per player. The code lives in `Framework/Npc/NpcFlowService.cs`.

### Item requirement

```json
{ "ItemId": "(O)337", "Count": 25 }
```

`ItemId` is a [qualified item ID](https://stardewvalleywiki.com/Modding:Items). `Count` must be at least 1.

## Item IDs used by this mod

| Item | ID | Wiki |
| --- | --- | --- |
| Copper Bar | `(O)334` | [Copper Bar](https://stardewvalleywiki.com/Copper_Bar) |
| Iron Bar | `(O)335` | [Iron Bar](https://stardewvalleywiki.com/Iron_Bar) |
| Gold Bar | `(O)336` | [Gold Bar](https://stardewvalleywiki.com/Gold_Bar) |
| Iridium Bar | `(O)337` | [Iridium Bar](https://stardewvalleywiki.com/Iridium_Bar) |
| Prismatic Shard | `(O)74` | [Prismatic Shard](https://stardewvalleywiki.com/Prismatic_Shard) |
| Hardwood | `(O)709` | [Hardwood](https://stardewvalleywiki.com/Hardwood) |
| Battery Pack | `(O)787` | [Battery Pack](https://stardewvalleywiki.com/Battery_Pack) |
| Ginger | `(O)829` | [Ginger](https://stardewvalleywiki.com/Ginger) |
| Cinder Shard | `(O)848` | [Cinder Shard](https://stardewvalleywiki.com/Cinder_Shard) |
| Magma Cap | `(O)851` | [Magma Cap](https://stardewvalleywiki.com/Magma_Cap) |
| Dragon Tooth | `(O)852` | [Dragon Tooth](https://stardewvalleywiki.com/Dragon_Tooth) |
| Cactus Fruit | `(O)90` | [Cactus Fruit](https://stardewvalleywiki.com/Cactus_Fruit) |
| Coconut | `(O)88` | [Coconut](https://stardewvalleywiki.com/Coconut) |

## Difficulty scaling

`assets/difficulties.json` is a dictionary keyed by difficulty label:

```json
"Hard": {
    "DisplayNameKey": "difficulty.hard.name",
    "DescriptionKey": "difficulty.hard.description",
    "UnlockGoldMultiplier": 2,
    "UnlockItemMultiplier": 2,
    "RidePriceMultiplier": 1.5,
    "RequireDiscovery": true
}
```

- Gold is multiplied and rounded to the nearest integer.
- Item counts are multiplied and rounded up, with a minimum of 1.
- Ride prices are multiplied and rounded to the nearest integer.
- `RequireDiscovery: false` clears the top-level `Unlock.Condition`, but leaves per-step conditions alone.
- Waiting days are never scaled.

## Adding a route

1. Add a route to `assets/minecart-spots.json` with a unique `Id`.
2. Add a `spot.<id>.name` key to `i18n/default.json`.
3. Add step keys if the route is staged.
4. Build the mod (`dotnet build mods/ImmersiveMinecarts`) and restart the game.
5. Check the generated [mod page](/mods/immersive-minecarts/) and the in-game
   [debug commands](/mods/immersive-minecarts/configuration/#debug-commands).

Example staged route:

```json
{
    "Id": "Example",
    "Network": "Default",
    "DisplayNameKey": "spot.example.name",
    "TargetLocation": "Town",
    "TargetTile": { "X": 45, "Y": 9 },
    "TargetDirection": "down",
    "RidePrice": 100,
    "Unlock": {
        "Condition": "PLAYER_VISITED_LOCATION Current Town",
        "Steps": [
            {
                "Id": "survey",
                "NameKey": "step.example.survey.name",
                "Gold": 10000,
                "WaitDays": 0,
                "Items": [{ "ItemId": "(O)335", "Count": 10 }]
            },
            {
                "Id": "build",
                "NameKey": "step.example.build.name",
                "Gold": 40000,
                "WaitDays": 3,
                "Items": [{ "ItemId": "(O)336", "Count": 10 }]
            }
        ]
    }
}
```

The mod validates unique IDs, directions, non-negative costs, wait times, and item counts. Invalid entries are
logged and skipped instead of crashing the game.

## Related code

| File | Responsibility |
| --- | --- |
| `Framework/Spots/MinecartSpot.cs` | Route and step models |
| `Framework/Spots/MinecartSpotRepository.cs` | Loads, validates, scales, and applies position overrides |
| `Framework/Difficulty/DifficultyRepository.cs` | Loads difficulty presets and resolves unknown labels |
| `Framework/Progression/ProgressionService.cs` | Per-player staged payments and mail flags |
| `Framework/Npc/NpcFlowService.cs` | Conversation topics, dialogue asset edits, and the custom `$action` |
| `Framework/Minecarts/MinecartDataEditor.cs` | Writes routes into `Data/Minecarts` |

## See also

- [Immersive Minecarts overview](/guides/immersive-minecarts/)
- [Destinations](/mods/immersive-minecarts/destinations/)
- [Progression](/mods/immersive-minecarts/progression/)
- [Configuration](/mods/immersive-minecarts/configuration/)
- [SMAPI content API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content)
- [SMAPI data API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Data)
- [Game state queries](https://stardewvalleywiki.com/Modding:Game_state_queries)
- [Dialogue](https://stardewvalleywiki.com/Modding:Dialogue)
- [Trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions)
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) — useful for map edits and asset packs
- [Integrated Minecarts](https://www.nexusmods.com/stardewvalley/mods/11881) — destination data and map examples
