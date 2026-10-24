# Immersive Minecarts

A [Stardew Valley](https://stardewvalleywiki.com/Stardew_Valley) mod that adds expensive, per-player minecart
routes. Each route is paid for in gold **and** themed resources, and the big routes are built in stages over
several in-game days.

Built for [SMAPI](https://smapi.io/) 4.x. Optional in-game settings via
[Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).

> **Status:** the data, progression, NPC conversations, and destination logic work. The locked-destination
> purchase UI is still being built; use the [debug commands](#testing-and-debug-commands) until then.

## Routes

{{table:spots}}

Every route is unlocked per player, so two farmhands can be at different stages. The exact per-stage costs and
wait times are also on the [Destinations page](/mods/immersive-minecarts/destinations/).

### Farm Home

A cart that drops you at the main farmhouse door. It follows a moved farmhouse automatically, so it keeps
working on rearranged farms. See [The Farm](https://stardewvalleywiki.com/The_Farm).

### Mountain

Replaces the old Railroad route. The first stage is a quest chain for [Clint](https://stardewvalleywiki.com/Clint)
and [Robin](https://stardewvalleywiki.com/Robin): Clint explains the problem and sends you to Robin, then Robin
starts the survey and takes the supplies. Later stages rebuild the line with [ingots](https://stardewvalleywiki.com/Iron_Bar),
[iridium](https://stardewvalleywiki.com/Iridium_Bar), and a final test run.

### Secret Woods

A direct cart into the [Secret Woods](https://stardewvalleywiki.com/Secret_Woods), paid mostly in
[hardwood](https://stardewvalleywiki.com/Hardwood) and [gold bars](https://stardewvalleywiki.com/Gold_Bar).

### Calico Desert

A three-stage build across the [Calico Desert](https://stardewvalleywiki.com/Calico_Desert). The survey, track
layers, and final inspection each need [gold](https://stardewvalleywiki.com/Gold_Bar),
[iridium](https://stardewvalleywiki.com/Iridium_Bar), and [desert](https://stardewvalleywiki.com/Cactus_Fruit)
[goods](https://stardewvalleywiki.com/Coconut).

### Ginger Island

The most expensive route, built for [Ginger Island](https://stardewvalleywiki.com/Ginger_Island) with
[gold](https://stardewvalleywiki.com/Gold_Bar), [iridium](https://stardewvalleywiki.com/Iridium_Bar),
[cinder shards](https://stardewvalleywiki.com/Cinder_Shard), [dragon teeth](https://stardewvalleywiki.com/Dragon_Tooth),
[battery packs](https://stardewvalleywiki.com/Battery_Pack), [prismatic shards](https://stardewvalleywiki.com/Prismatic_Shard),
[ginger](https://stardewvalleywiki.com/Ginger), and [magma caps](https://stardewvalleywiki.com/Magma_Cap).

## Progression

- Progress is **per player**, not per save. Two farmhands can be at different stages of the same route.
- Each stage consumes its own gold and items immediately. Paying a partial stage never charges the whole route.
- After a stage is paid, the next one unlocks after its wait time (in in-game days).
- Completing the final stage grants the route to that player and makes it appear in the minecart menu.
- Some stages begin with NPC conversations. The current example is the Mountain chain: Clint sends you to
  Robin, and Robin accepts the first stage's supplies.

Unlocked routes are remembered with a per-farmer mail flag; multi-stage progress is stored in per-farmer
[mod data](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Data). Both are synced by the game.

Read the [Progression page](/mods/immersive-minecarts/progression/) for the stage table, wait rules, and NPC
flow details.

## Difficulty

{{table:difficulties}}

[Easy](/mods/immersive-minecarts/configuration/) halves gold and item costs and removes ride fares;
[Hard](/mods/immersive-minecarts/configuration/) doubles costs; Realistic is the baseline. Waiting days are
**not** scaled. See the [Configuration page](/mods/immersive-minecarts/configuration/) for presets and the full
option list.

## Configuration

{{table:config}}

`config.json` is created on first launch. You can also move each route's arrival tile and facing direction:

```json
"SpotPositions": {
    "Desert": { "X": 35, "Y": 40, "Direction": "left" }
}
```

Position overrides win over the automatic Farm Home farmhouse entry. The same values can be edited in game
through [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098). The full reference is on
the [Configuration page](/mods/immersive-minecarts/configuration/).

## Installing

See the [installation guide](/guides/installing/). In short: install [SMAPI](https://smapi.io/), then unzip
this mod into your `Mods` folder.

## Testing and debug commands

Enable `EnableDebugCommands` in `config.json`, then load a save.

| Command | Effect |
| --- | --- |
| `imm_list` | Show each route's next-stage cost, progress, and wait time |
| `imm_unlock <spotId>` | Pay the next stage normally |
| `imm_unlock <spotId> force` | Advance one stage ignoring cost, conditions, and wait |
| `imm_lock <spotId>` | Clear the unlock and all progress |
| `imm_difficulty <label>` | Switch difficulty and apply it immediately |
| `imm_give <spotId>` | Add the current stage's gold and items to your inventory (testing) |

## Data and modding

- `assets/minecart-spots.json` — routes, stages, and NPC flows; rendered by the Routes table above.
- `assets/difficulties.json` — difficulty presets and multipliers.
- `Framework/Spots/` — data model and loader.
- `Framework/Progression/` — per-player stage state and payment logic.
- `Framework/Npc/` — conversation topics, dialogue edits, and the custom `$action`.
- `Framework/Minecarts/` — writes routes into `Data/Minecarts`.

See the [data reference](/guides/adding-minecart-spots/) for field-by-field documentation and how to add a
route.

## See also

- [SMAPI](https://smapi.io/) · [SMAPI mod build package](https://smapi.io/package)
- [Stardew Valley Wiki](https://stardewvalleywiki.com/) · [Modding:Game state queries](https://stardewvalleywiki.com/Modding:Game_state_queries)
- [Modding:Dialogue](https://stardewvalleywiki.com/Modding:Dialogue) · [Modding:Trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions)
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) · [Integrated Minecarts](https://www.nexusmods.com/stardewvalley/mods/11881)
