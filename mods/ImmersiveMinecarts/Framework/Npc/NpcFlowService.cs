using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ImmersiveMinecarts.Framework.Progression;
using ImmersiveMinecarts.Framework.Spots;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace ImmersiveMinecarts.Framework.Npc;

/// <summary>Runs optional NPC conversation chains before a step can be paid.</summary>
/// <remarks>
/// Conversation topics are added to <see cref="Farmer.activeDialogueEvents"/> and matched against keys in
/// <c>Characters/Dialogue/&lt;npc&gt;</c>. The dialogue keys end in a custom <c>$action</c> which calls
/// <see cref="HandleAction"/> to record the conversation and move to the next NPC.
/// </remarks>
internal sealed class NpcFlowService
{
    /*********
    ** Fields
    *********/
    private const string ActionName = "DanielMontilla.ImmersiveMinecarts.NpcFlow";
    private const int TopicDurationDays = 14;

    private readonly IModHelper Helper;
    private readonly IMonitor Monitor;
    private readonly ModConfig Config;
    private readonly MinecartSpotRepository Spots;
    private readonly ProgressionService Progression;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="helper">The mod helper.</param>
    /// <param name="monitor">The mod monitor.</param>
    /// <param name="config">The mod configuration.</param>
    /// <param name="spots">The spot repository.</param>
    /// <param name="progression">The progression service.</param>
    public NpcFlowService(IModHelper helper, IMonitor monitor, ModConfig config, MinecartSpotRepository spots, ProgressionService progression)
    {
        this.Helper = helper;
        this.Monitor = monitor;
        this.Config = config;
        this.Spots = spots;
        this.Progression = progression;
    }

    /// <summary>Start listening for asset requests and day changes.</summary>
    public void Enable()
    {
        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        this.Helper.Events.Player.Warped += this.OnWarped;
    }

    /// <summary>Register the custom <c>$action</c> used by NPC flow dialogues.</summary>
    public void RegisterAction()
    {
        TriggerActionManager.RegisterAction(ActionName, this.HandleAction);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Add dialogue entries for every configured NPC flow.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        foreach (MinecartSpot spot in this.Spots.Spots)
        {
            foreach (UnlockStep step in spot.Unlock.Steps)
            {
                for (int i = 0; i < step.NpcFlow.Count; i++)
                {
                    NpcFlowStep flow = step.NpcFlow[i];
                    if (!e.NameWithoutLocale.IsEquivalentTo($"Characters/Dialogue/{flow.Npc}"))
                        continue;

                    int index = i;
                    e.Edit(asset =>
                    {
                        IDictionary<string, string> dialogue = asset.AsDictionary<string, string>().Data;
                        dialogue[flow.Topic] = this.BuildDialogueText(spot, step, index, flow);
                    });
                }
            }
        }
    }

    /// <summary>Make sure the next required NPC has an active conversation topic.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.EnsureTopics();
    }

    /// <summary>Re-add topics after their duration expires.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.EnsureTopics();
    }

    /// <summary>Add topics as soon as the player reaches a spot they just discovered.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        this.EnsureTopics();
    }

    /// <summary>Add the next missing conversation topic for each in-progress step.</summary>
    private void EnsureTopics()
    {
        if (!Context.IsWorldReady)
            return;

        Farmer player = Game1.player;

        foreach (MinecartSpot spot in this.Spots.Spots)
        {
            SpotProgress progress = ProgressionService.GetProgress(spot, player);
            if (progress.IsUnlocked || progress.CurrentStep is null)
                continue;

            UnlockStep step = progress.CurrentStep;
            if (step.NpcFlow.Count == 0)
                continue;

            if (!MeetsCondition(spot.Unlock.Condition) || !MeetsCondition(step.Condition))
                continue;

            int nextIndex = GetNextIndex(spot, step, player);
            if (nextIndex >= step.NpcFlow.Count)
                continue;

            NpcFlowStep flow = step.NpcFlow[nextIndex];
            bool wasActive = player.activeDialogueEvents.ContainsKey(flow.Topic);
            player.activeDialogueEvents[flow.Topic] = TopicDurationDays;

            if (!wasActive)
            {
                this.Monitor.Log($"Added NPC flow topic '{flow.Topic}' for {flow.Npc} ({spot.Id}/{step.Id}).", LogLevel.Trace);
                this.ShowHud("npc.flow.start", new { npc = flow.Npc, spot = this.GetSpotName(spot) });
            }
        }
    }

    /// <summary>Handle the custom <c>$action</c> emitted by an NPC flow dialogue.</summary>
    /// <param name="args">The action arguments.</param>
    /// <param name="context">The trigger action context.</param>
    /// <param name="error">An error phrase, if the action fails.</param>
    private bool HandleAction(string[] args, TriggerActionContext context, out string? error)
    {
        error = null;

        if (!Context.IsWorldReady)
        {
            error = "no save is loaded";
            return false;
        }

        if (args.Length < 4 || !int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
        {
            error = $"usage: {ActionName} <spotId> <stepId> <index>";
            return false;
        }

        string spotId = args[1];
        string stepId = args[2];

        if (!this.Spots.TryGet(spotId, out MinecartSpot? spot))
        {
            error = $"unknown minecart spot '{spotId}'";
            return false;
        }

        SpotProgress progress = ProgressionService.GetProgress(spot, Game1.player);
        if (progress.IsUnlocked || progress.CurrentStep is null || !string.Equals(progress.CurrentStep.Id, stepId, StringComparison.OrdinalIgnoreCase))
        {
            error = $"step '{stepId}' of spot '{spotId}' isn't current";
            return false;
        }

        UnlockStep step = progress.CurrentStep;
        if (index < 0 || index >= step.NpcFlow.Count)
        {
            error = $"NPC flow index {index} is out of range for step '{stepId}'";
            return false;
        }

        Farmer player = Game1.player;
        NpcFlowStep flow = step.NpcFlow[index];

        player.mailReceived.Add(ProgressionService.GetFlowFlag(spotId, stepId, index));
        this.Monitor.Log($"NPC flow '{spotId}/{stepId}' talked to {flow.Npc} (index {index}).", LogLevel.Trace);

        if (index + 1 < step.NpcFlow.Count)
        {
            NpcFlowStep next = step.NpcFlow[index + 1];
            player.activeDialogueEvents[next.Topic] = TopicDurationDays;
            this.ShowHud("npc.flow.next", new { npc = next.Npc, spot = this.GetSpotName(spot) });
            return true;
        }

        if (this.Progression.TryAdvance(spot, player, out string? failure))
        {
            ClearFlowFlags(spot, step, player);
            foreach (NpcFlowStep entry in step.NpcFlow)
                player.activeDialogueEvents.Remove(entry.Topic);

            return true;
        }

        // The player wasn't ready to pay yet. Keep the topic so Robin can repeat the conversation.
        player.activeDialogueEvents[flow.Topic] = TopicDurationDays;
        player.mailReceived.Remove(flow.Npc + "_" + flow.Topic);

        if (!string.IsNullOrWhiteSpace(failure))
        {
            Game1.addHUDMessage(new HUDMessage(failure));
            this.Monitor.Log($"NPC flow for '{spotId}' didn't complete: {failure}", LogLevel.Trace);
        }

        return true;
    }

    /// <summary>Build the dialogue text for an NPC flow entry.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="step">The step.</param>
    /// <param name="index">The NPC's index in the chain.</param>
    /// <param name="flow">The NPC flow entry.</param>
    private string BuildDialogueText(MinecartSpot spot, UnlockStep step, int index, NpcFlowStep flow)
    {
        string text = this.Helper.Translation.Get(flow.DialogueKey).ToString();
        string action = string.Join(' ', ActionName, spot.Id, step.Id, index.ToString(CultureInfo.InvariantCulture));

        // The action line has no text, so it runs as soon as the dialogue opens.
        return $"$action {action}#{text}";
    }

    /// <summary>Get the index of the first NPC the player hasn't talked to yet.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="step">The current step.</param>
    /// <param name="player">The player.</param>
    private static int GetNextIndex(MinecartSpot spot, UnlockStep step, Farmer player)
    {
        for (int i = 0; i < step.NpcFlow.Count; i++)
        {
            if (!player.mailReceived.Contains(ProgressionService.GetFlowFlag(spot.Id, step.Id, i)))
                return i;
        }

        // All conversations happened, but the step still isn't paid (the last NPC's turn-in failed).
        // Keep the last NPC available so the player can try again after gathering the materials.
        return step.NpcFlow.Count - 1;
    }

    /// <summary>Clear the conversation flags for a completed step.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="step">The completed step.</param>
    /// <param name="player">The player.</param>
    private static void ClearFlowFlags(MinecartSpot spot, UnlockStep step, Farmer player)
    {
        for (int i = 0; i < step.NpcFlow.Count; i++)
            player.mailReceived.Remove(ProgressionService.GetFlowFlag(spot.Id, step.Id, i));
    }

    /// <summary>Get whether a player meets a game-state condition.</summary>
    /// <param name="condition">The condition, or <c>null</c> for none.</param>
    private static bool MeetsCondition(string? condition)
    {
        return string.IsNullOrWhiteSpace(condition)
            || GameStateQuery.CheckConditions(condition, Game1.currentLocation, Game1.player);
    }

    /// <summary>Get the translated display name for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private string GetSpotName(MinecartSpot spot)
    {
        return this.Helper.Translation.Get(spot.DisplayNameKey).ToString();
    }

    /// <summary>Show a translated HUD message when notifications are enabled.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="tokens">The translation tokens.</param>
    private void ShowHud(string key, object tokens)
    {
        if (!this.Config.ShowUnlockNotifications)
            return;

        Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get(key, tokens).ToString()));
    }
}