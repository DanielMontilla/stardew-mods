using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ImmersiveMinecarts.Framework.Spots;

using StardewModdingAPI;

using StardewValley;

namespace ImmersiveMinecarts.Framework.Progression;

/// <summary>Tracks and applies each player's minecart unlocks.</summary>
/// <remarks>
/// Unlocks are stored as mail flags on the player, and multi-step progress is stored in the farmer's mod data.
/// Both are per-player, saved with the farmer, and synced by the game, which lets <c>Data/Minecarts</c>
/// conditions check the final state with <c>PLAYER_HAS_MAIL Current ...</c>.
/// </remarks>
internal sealed class ProgressionService
{
    /*********
    ** Fields
    *********/
    private const string MailIdPrefix = "ImmersiveMinecarts.Unlocked.";
    private const string ProgressKeyPrefix = "ImmersiveMinecarts.Progress.";

    private readonly IModHelper Helper;
    private readonly IMonitor Monitor;
    private readonly ModConfig Config;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="helper">The mod helper.</param>
    /// <param name="monitor">The mod monitor.</param>
    /// <param name="config">The mod configuration.</param>
    public ProgressionService(IModHelper helper, IMonitor monitor, ModConfig config)
    {
        this.Helper = helper;
        this.Monitor = monitor;
        this.Config = config;
    }

    /// <summary>Get the mail flag ID which marks a spot as unlocked.</summary>
    /// <param name="spotId">The spot ID.</param>
    public static string GetUnlockMailId(string spotId)
    {
        return MailIdPrefix + spotId;
    }

    /// <summary>Get the mail flag ID which marks one NPC in a step's flow as talked to.</summary>
    /// <param name="spotId">The spot ID.</param>
    /// <param name="stepId">The step ID.</param>
    /// <param name="index">The NPC index in the flow.</param>
    public static string GetFlowFlag(string spotId, string stepId, int index)
    {
        return $"ImmersiveMinecarts.Flow.{spotId}.{stepId}.{index}";
    }

    /// <summary>Get whether a player has fully unlocked a spot.</summary>
    /// <param name="spot">The spot to check.</param>
    /// <param name="player">The player to check.</param>
    public static bool IsUnlocked(MinecartSpot spot, Farmer player)
    {
        return player.mailReceived.Contains(GetUnlockMailId(spot.Id));
    }

    /// <summary>Get a player's progress toward unlocking a spot.</summary>
    /// <param name="spot">The spot to check.</param>
    /// <param name="player">The player to check.</param>
    public static SpotProgress GetProgress(MinecartSpot spot, Farmer player)
    {
        int totalSteps = spot.Unlock.Steps.Count;
        bool unlocked = IsUnlocked(spot, player);
        int completedSteps = 0;
        int readyDay = 0;

        if (!unlocked
            && totalSteps > 0
            && player.modData.TryGetValue(GetProgressKey(spot.Id), out string? raw)
            && TryParseProgress(raw, out int savedCompleted, out int savedReadyDay))
        {
            completedSteps = Math.Clamp(savedCompleted, 0, totalSteps);
            readyDay = savedReadyDay;
        }

        if (unlocked)
            completedSteps = totalSteps;

        UnlockStep? currentStep = !unlocked && completedSteps < totalSteps ? spot.Unlock.Steps[completedSteps] : null;
        int daysRemaining = currentStep is null ? 0 : Math.Max(0, readyDay - Game1.Date.TotalDays);

        return new SpotProgress
        {
            IsUnlocked = unlocked,
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
            DaysRemaining = daysRemaining,
            CurrentStep = currentStep
        };
    }

    /// <summary>Pay the next step toward unlocking a spot. For single-step spots, this performs the whole unlock.</summary>
    /// <param name="spot">The spot to advance.</param>
    /// <param name="player">The player paying.</param>
    /// <param name="failure">A translated message explaining why it failed, if applicable.</param>
    /// <returns>Returns whether the step was paid.</returns>
    public bool TryAdvance(MinecartSpot spot, Farmer player, out string? failure)
    {
        failure = null;

        if (IsUnlocked(spot, player))
            return true;

        if (spot.Unlock.Steps.Count == 0)
            return this.TryUnlockSingle(spot, player, out failure);

        SpotProgress progress = GetProgress(spot, player);
        string spotName = this.GetSpotName(spot);

        if (progress.CompletedSteps >= progress.TotalSteps)
        {
            // Progress is complete but the final flag is missing (e.g. a manually edited save).
            this.Unlock(spot, player);
            return true;
        }

        if (progress.DaysRemaining > 0)
        {
            failure = this.Helper.Translation.Get("unlock.step-wait", new { spot = spotName, days = progress.DaysRemaining }).ToString();
            return false;
        }

        UnlockStep step = progress.CurrentStep!;

        if (!MeetsCondition(spot.Unlock.Condition, player) || !MeetsCondition(step.Condition, player))
        {
            failure = this.Helper.Translation.Get("unlock.requirement-not-met", new { spot = spotName }).ToString();
            return false;
        }

        if (player.Money < step.Gold)
        {
            failure = this.Helper.Translation.Get("unlock.not-enough-gold", new { spot = spotName, gold = step.Gold }).ToString();
            return false;
        }

        if (!HasItems(player, step.Items))
        {
            failure = this.Helper.Translation.Get("unlock.missing-items", new { spot = spotName }).ToString();
            return false;
        }

        player.Money -= step.Gold;
        TakeItems(player, step.Items);

        int completedSteps = progress.CompletedSteps + 1;
        bool finished = completedSteps >= spot.Unlock.Steps.Count;
        int waitDays = finished ? 0 : spot.Unlock.Steps[completedSteps].WaitDays;
        int readyDay = finished ? 0 : Game1.Date.TotalDays + waitDays;

        SaveProgress(spot, player, completedSteps, readyDay);

        if (finished)
        {
            this.Unlock(spot, player);
        }
        else if (waitDays > 0)
        {
            this.ShowHud("unlock.step-complete", new
            {
                spot = spotName,
                step = completedSteps,
                total = spot.Unlock.Steps.Count,
                days = waitDays
            });
        }
        else
        {
            this.ShowHud("unlock.step-complete-now", new
            {
                spot = spotName,
                step = completedSteps,
                total = spot.Unlock.Steps.Count
            });
        }

        return true;
    }

    /// <summary>Advance a spot by one step, ignoring costs, conditions, and delays.</summary>
    /// <param name="spot">The spot to advance.</param>
    /// <param name="player">The player to advance it for.</param>
    public void ForceAdvance(MinecartSpot spot, Farmer player)
    {
        if (spot.Unlock.Steps.Count == 0)
        {
            this.Unlock(spot, player);
            return;
        }

        SpotProgress progress = GetProgress(spot, player);
        int completedSteps = Math.Min(progress.TotalSteps, progress.CompletedSteps + 1);

        if (completedSteps >= progress.TotalSteps)
            this.Unlock(spot, player);
        else
            SaveProgress(spot, player, completedSteps, 0);
    }

    /// <summary>Mark a spot as unlocked for a player without checking requirements.</summary>
    /// <param name="spot">The spot to unlock.</param>
    /// <param name="player">The player unlocking it.</param>
    public void Unlock(MinecartSpot spot, Farmer player)
    {
        if (spot.Unlock.Steps.Count > 0)
            SaveProgress(spot, player, spot.Unlock.Steps.Count, 0);

        ClearNpcFlow(spot, player);

        if (!player.mailReceived.Add(GetUnlockMailId(spot.Id)))
            return;

        this.Monitor.Log($"Unlocked minecart spot '{spot.Id}' for {player.Name}.", LogLevel.Trace);
        this.ShowHud("unlock.success", new { spot = this.GetSpotName(spot) });
    }

    /// <summary>Mark a spot as locked for a player and clear its progress.</summary>
    /// <param name="spot">The spot to lock.</param>
    /// <param name="player">The player locking it.</param>
    public void Lock(MinecartSpot spot, Farmer player)
    {
        player.mailReceived.Remove(GetUnlockMailId(spot.Id));
        player.modData.Remove(GetProgressKey(spot.Id));
        ClearNpcFlow(spot, player);

        this.Monitor.Log($"Locked minecart spot '{spot.Id}' for {player.Name}.", LogLevel.Trace);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Clear all NPC-flow flags, topics, and seen markers for a spot.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="player">The player.</param>
    private static void ClearNpcFlow(MinecartSpot spot, Farmer player)
    {
        foreach (UnlockStep step in spot.Unlock.Steps)
        {
            for (int i = 0; i < step.NpcFlow.Count; i++)
            {
                NpcFlowStep flow = step.NpcFlow[i];
                player.mailReceived.Remove(GetFlowFlag(spot.Id, step.Id, i));
                player.mailReceived.Remove(flow.Npc + "_" + flow.Topic);
                player.activeDialogueEvents.Remove(flow.Topic);
            }
        }
    }

    /// <summary>Unlock a single-step spot after checking its requirements.</summary>
    /// <param name="spot">The spot to unlock.</param>
    /// <param name="player">The player unlocking it.</param>
    /// <param name="failure">A translated message explaining why it failed, if applicable.</param>
    private bool TryUnlockSingle(MinecartSpot spot, Farmer player, out string? failure)
    {
        failure = null;
        string spotName = this.GetSpotName(spot);

        if (!MeetsCondition(spot.Unlock.Condition, player))
        {
            failure = this.Helper.Translation.Get("unlock.requirement-not-met", new { spot = spotName }).ToString();
            return false;
        }

        if (player.Money < spot.Unlock.Gold)
        {
            failure = this.Helper.Translation.Get("unlock.not-enough-gold", new { spot = spotName, gold = spot.Unlock.Gold }).ToString();
            return false;
        }

        if (!HasItems(player, spot.Unlock.Items))
        {
            failure = this.Helper.Translation.Get("unlock.missing-items", new { spot = spotName }).ToString();
            return false;
        }

        player.Money -= spot.Unlock.Gold;
        TakeItems(player, spot.Unlock.Items);
        this.Unlock(spot, player);

        return true;
    }

    /// <summary>Get the translated display name for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private string GetSpotName(MinecartSpot spot)
    {
        return this.Helper.Translation.Get(spot.DisplayNameKey).ToString();
    }

    /// <summary>Get the mod-data key which stores a spot's progress.</summary>
    /// <param name="spotId">The spot ID.</param>
    private static string GetProgressKey(string spotId)
    {
        return ProgressKeyPrefix + spotId;
    }

    /// <summary>Save a player's progress toward a spot.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="player">The player.</param>
    /// <param name="completedSteps">The number of completed steps.</param>
    /// <param name="readyDay">The absolute day when the next step is available.</param>
    private static void SaveProgress(MinecartSpot spot, Farmer player, int completedSteps, int readyDay)
    {
        player.modData[GetProgressKey(spot.Id)] =
            completedSteps.ToString(CultureInfo.InvariantCulture) + "|" + readyDay.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Parse a stored progress value.</summary>
    /// <param name="raw">The stored value.</param>
    /// <param name="completedSteps">The number of completed steps.</param>
    /// <param name="readyDay">The absolute day when the next step is available.</param>
    private static bool TryParseProgress(string raw, out int completedSteps, out int readyDay)
    {
        completedSteps = 0;
        readyDay = 0;

        string[] parts = raw.Split('|');
        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out completedSteps)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out readyDay);
    }

    /// <summary>Show a translated HUD message when notifications are enabled.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="tokens">The translation tokens.</param>
    private void ShowHud(string key, object tokens)
    {
        if (!this.Config.ShowUnlockNotifications)
            return;

        string message = this.Helper.Translation.Get(key, tokens).ToString();
        Game1.addHUDMessage(new HUDMessage(message));
    }

    /// <summary>Get whether a player meets a game-state condition.</summary>
    /// <param name="condition">The condition, or <c>null</c> for none.</param>
    /// <param name="player">The player to check.</param>
    private static bool MeetsCondition(string? condition, Farmer player)
    {
        return string.IsNullOrWhiteSpace(condition)
            || GameStateQuery.CheckConditions(condition, Game1.currentLocation, player);
    }

    /// <summary>Get whether a player has all required items.</summary>
    /// <param name="player">The player to check.</param>
    /// <param name="requirements">The required items.</param>
    private static bool HasItems(Farmer player, IReadOnlyCollection<ItemRequirement> requirements)
    {
        return requirements.All(requirement => CountItem(player, requirement.ItemId) >= requirement.Count);
    }

    /// <summary>Count how many of an item a player has.</summary>
    /// <param name="player">The player to check.</param>
    /// <param name="itemId">The qualified item ID.</param>
    private static int CountItem(Farmer player, string itemId)
    {
        return player.Items
            .Where(item => item is not null && item.QualifiedItemId == itemId)
            .Sum(item => item!.Stack);
    }

    /// <summary>Remove the required items from a player's inventory.</summary>
    /// <param name="player">The player to update.</param>
    /// <param name="requirements">The items to remove.</param>
    private static void TakeItems(Farmer player, IReadOnlyCollection<ItemRequirement> requirements)
    {
        foreach (ItemRequirement requirement in requirements)
        {
            int remaining = requirement.Count;

            for (int i = 0; i < player.Items.Count && remaining > 0; i++)
            {
                Item? item = player.Items[i];
                if (item is null || item.QualifiedItemId != requirement.ItemId)
                    continue;

                int taken = Math.Min(item.Stack, remaining);
                item.Stack -= taken;
                remaining -= taken;

                if (item.Stack <= 0)
                    player.removeItemFromInventory(item);
            }
        }
    }
}