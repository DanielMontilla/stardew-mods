---
title: Immersive Minecarts
description: Per-player minecart routes with staged payments in gold, ingots, and regional resources.
sidebar:
  order: 10
---

[Immersive Minecarts](/mods/immersive-minecarts/) adds minecart routes that each player unlocks separately.
Every route costs gold **and** resources, and the long routes are paid in stages over several in-game days.

The generated [mod page](/mods/immersive-minecarts/) always shows the current route data straight from
`assets/minecart-spots.json`. This guide explains how the system fits together; see
[Configuration](/mods/immersive-minecarts/configuration/) for settings, [Progression](/mods/immersive-minecarts/progression/)
for stage rules, and the [data reference](/guides/adding-minecart-spots/) for the JSON schema.

## The idea

- **Per player, not per save.** Two farmhands can be at different stages of the same route.
- **Gold plus resources.** Every stage asks for themed materials, usually
  [ingots](https://stardewvalleywiki.com/Copper_Bar), [iridium](https://stardewvalleywiki.com/Iridium_Bar),
  and [regional](https://stardewvalleywiki.com/Cactus_Fruit) [goods](https://stardewvalleywiki.com/Ginger).
- **Staged builds.** Pay a partial amount, wait for the crew, then pay the next stage. The final payment opens
  the route.
- **Discovery first.** Most routes also require the player to have visited the destination at least once. The
  condition uses [game state queries](https://stardewvalleywiki.com/Modding:Game_state_queries).

## Routes

The mod adds routes to [Farm Home](https://stardewvalleywiki.com/The_Farm),
[Mountain](https://stardewvalleywiki.com/The_Mountain),
[Secret Woods](https://stardewvalleywiki.com/Secret_Woods),
[Calico Desert](https://stardewvalleywiki.com/Calico_Desert), and
[Ginger Island](https://stardewvalleywiki.com/Ginger_Island).

The Mountain route replaces the Railroad route. It starts with a quest chain for
[Clint](https://stardewvalleywiki.com/Clint) and [Robin](https://stardewvalleywiki.com/Robin), then is
rebuilt in stages with [ingots](https://stardewvalleywiki.com/Iron_Bar) and
[iridium](https://stardewvalleywiki.com/Iridium_Bar).

:::tip
The exact gold, resources, stages, wait times, and ride fares are rendered on the
[generated mod page](/mods/immersive-minecarts/) directly from `assets/minecart-spots.json`. That page is the
source of truth; this guide explains the system.
:::

## Staged builds

The Mountain, Desert, and Ginger Island routes have three stages:

1. **Survey / quest.** A smaller payment, sometimes a favour for a local character.
2. **Build.** The expensive middle stage with ingots and regional materials.
3. **Test / install.** A final payment and a short wait before the route opens.

The Mountain route starts with a Clint → Robin quest chain, which replaces the old Railroad route.

## NPC conversations

Steps can start with people instead of a purchase screen. The Mountain route currently uses a
[Clint](https://stardewvalleywiki.com/Clint) → [Robin](https://stardewvalleywiki.com/Robin) chain:

1. Clint explains the problem and sends you to Robin.
2. Robin starts the survey and accepts the first stage's timber and ingots.
3. Later Mountain stages are paid normally.

This is built on vanilla [conversation topics](https://stardewvalleywiki.com/Modding:Dialogue) and
[trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions), so it is per player and works in
multiplayer. If you are missing materials, Robin repeats the turn-in instead of silently consuming the topic.

See the [data reference](/guides/adding-minecart-spots/#npc-flow) for the JSON fields.

:::note
Waiting is measured in in-game days, not real time. Progress is stored per farmer, so leaving multiplayer and
returning later does not reset it.
:::

## Status

The data, staged payment logic, difficulty scaling, and destination registration are implemented. The
in-game purchase dialogue for locked routes is still being built; until then, use the
[debug commands](/mods/immersive-minecarts/#testing-and-debug-commands).

## See also

- [Installation](/guides/installing/)
- [Destinations](/mods/immersive-minecarts/destinations/)
- [Progression](/mods/immersive-minecarts/progression/)
- [Configuration](/mods/immersive-minecarts/configuration/)
- [Data reference](/guides/adding-minecart-spots/)
- [SMAPI](https://smapi.io/) · [SMAPI mod build package](https://smapi.io/package)
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)
- [Integrated Minecarts](https://www.nexusmods.com/stardewvalley/mods/11881) — the inspiration for adding
  carts in more places.
