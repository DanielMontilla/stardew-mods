---
title: Progression
description: How staged payments, wait times, and NPC conversation chains work.
order: 15
---

[Immersive Minecarts](/mods/immersive-minecarts/) unlocks routes per player. This page covers the rules behind
the [destinations](/mods/immersive-minecarts/destinations/) and the NPC chains.

## Per player

Progress belongs to the farmer, not the save slot. Two farmhands can be on different stages of the same route,
and joining or leaving multiplayer does not reset anything.

- Unlocked routes use a mail flag with the {{const.MailIdPrefix}} prefix.
- Partial stages use the farmer's [mod data](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Data) under
  the {{const.ProgressKeyPrefix}} prefix.
- Both are synced by the game, so the host and clients agree on who has unlocked what.

## Staged payments

Long routes are split into ordered stages. Each stage:

1. Checks the route's [game state query](https://stardewvalleywiki.com/Modding:Game_state_queries) and the
   stage's own query, if any.
2. Takes that stage's [gold](https://stardewvalleywiki.com/Gold) and items from the player.
3. Saves the number of completed stages.
4. Waits for the next stage's delay before accepting another payment.

The final stage grants the route and makes it appear in the minecart menu. A partial payment never charges the
whole route up front.

```text
Survey → wait 3 days → Lay tracks → wait 5 days → Final inspection → unlocked
```

Waiting is measured in in-game days. The exact costs and delays are on the
[Destinations page](/mods/immersive-minecarts/destinations/).

## NPC conversation chains

A stage can start with people instead of a payment screen. The Mountain first stage uses a
[Clint](https://stardewvalleywiki.com/Clint) → [Robin](https://stardewvalleywiki.com/Robin) chain:

1. Clint explains the problem and sends you to Robin.
2. Robin starts the survey.
3. If you have the timber and ingots, talking to her completes the stage and starts the build timer.
4. If you are missing materials, she repeats the turn-in later instead of losing the conversation.

This is built on vanilla [conversation topics](https://stardewvalleywiki.com/Modding:Dialogue) and
[trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions):

- Each NPC gets a topic in the player's active dialogue events.
- The mod adds a matching key to `Characters/Dialogue/<npc>` with a custom `$action`.
- The action records the conversation and either adds the next NPC or tries to pay the stage.

Topics last 14 days and are refreshed each morning while they are still the current stage. The data fields are
documented on the [data reference](/guides/adding-minecart-spots/#npc-flow).

## Resetting progress

For testing, `imm_lock <spotId>` clears the route, its staged progress, its NPC conversation flags, and the
seen markers. `imm_unlock <spotId> force` advances exactly one stage, and `imm_give <spotId>` adds the current
stage's gold and items to your inventory. See
[debug commands](/mods/immersive-minecarts/#testing-and-debug-commands).

## See also

- [Destinations](/mods/immersive-minecarts/destinations/)
- [Configuration](/mods/immersive-minecarts/configuration/)
- [Data reference](/guides/adding-minecart-spots/)
- [Game state queries](https://stardewvalleywiki.com/Modding:Game_state_queries)
- [Dialogue](https://stardewvalleywiki.com/Modding:Dialogue)
- [Trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions)
