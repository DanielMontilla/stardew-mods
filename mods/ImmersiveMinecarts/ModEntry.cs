using System;
using System.Collections.Generic;
using System.Linq;

using ImmersiveMinecarts.Framework.Difficulty;
using ImmersiveMinecarts.Framework.Minecarts;
using ImmersiveMinecarts.Framework.Npc;
using ImmersiveMinecarts.Framework.Progression;
using ImmersiveMinecarts.Framework.Spots;
using ImmersiveMinecarts.Integrations;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;

namespace ImmersiveMinecarts;

/// <summary>The mod entry point.</summary>
public sealed class ModEntry : Mod
{
    /*********
    ** Fields
    *********/
    private ModConfig Config = null!;
    private MinecartSpotRepository SpotRepository = null!;
    private DifficultyRepository Difficulties = null!;
    private ProgressionService Progression = null!;
    private NpcFlowService NpcFlow = null!;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();
        this.Config.SpotPositions ??= [];

        this.Difficulties = new DifficultyRepository(helper, this.Monitor);
        this.Difficulties.Load();

        this.SpotRepository = new MinecartSpotRepository(helper, this.Monitor);
        this.SpotRepository.Load();

        string difficultyLabel = this.Difficulties.ResolveLabel(this.Config.Difficulty);
        if (!string.Equals(difficultyLabel, this.Config.Difficulty, StringComparison.Ordinal))
            this.Config.Difficulty = difficultyLabel;

        helper.WriteConfig(this.Config);

        this.ApplyConfig();

        this.Progression = new ProgressionService(helper, this.Monitor, this.Config);

        this.NpcFlow = new NpcFlowService(helper, this.Monitor, this.Config, this.SpotRepository, this.Progression);
        this.NpcFlow.Enable();

        new MinecartDataEditor(helper, this.SpotRepository).Enable();

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.World.BuildingListChanged += this.OnBuildingListChanged;

        if (this.Config.EnableDebugCommands)
            this.RegisterDebugCommands(helper);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Register the config menu once all mods are loaded.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.NpcFlow.RegisterAction();

        new GenericModConfigMenuIntegration(
            helper: this.Helper,
            manifest: this.ModManifest,
            monitor: this.Monitor,
            getConfig: () => this.Config,
            reset: this.ResetConfig,
            saveAndApply: this.SaveConfig,
            spots: this.SpotRepository,
            difficulties: this.Difficulties
        ).Register();
    }

    /// <summary>Refresh destinations that depend on save data.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (this.SpotRepository.Spots.Any(spot => spot.UseMainFarmHouseEntry))
            this.Helper.GameContent.InvalidateCache("Data/Minecarts");
    }

    /// <summary>Refresh the Farm Home destination when the farmhouse moves.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnBuildingListChanged(object? sender, BuildingListChangedEventArgs e)
    {
        if (e.Location is Farm && this.SpotRepository.Spots.Any(spot => spot.UseMainFarmHouseEntry))
            this.Helper.GameContent.InvalidateCache("Data/Minecarts");
    }

    /// <summary>Rebuild the effective spots and refresh <c>Data/Minecarts</c>.</summary>
    private void ApplyConfig()
    {
        DifficultySettings difficulty = this.Difficulties.Get(this.Config.Difficulty);
        this.SpotRepository.Apply(difficulty, this.Config);
        this.Helper.GameContent.InvalidateCache("Data/Minecarts");
    }

    /// <summary>Reset the config to its default values and apply it.</summary>
    private void ResetConfig()
    {
        ModConfig defaults = new();

        this.Config.Difficulty = this.Difficulties.ResolveLabel(defaults.Difficulty);
        this.Config.ShowUnlockNotifications = defaults.ShowUnlockNotifications;
        this.Config.EnableDebugCommands = defaults.EnableDebugCommands;
        this.Config.SpotPositions ??= [];
        this.Config.SpotPositions.Clear();

        this.Helper.WriteConfig(this.Config);
        this.ApplyConfig();
    }

    /// <summary>Save the current config and apply it.</summary>
    private void SaveConfig()
    {
        this.Helper.WriteConfig(this.Config);
        this.ApplyConfig();
    }

    /// <summary>Register the <c>imm_*</c> console commands used during development.</summary>
    /// <param name="helper">The mod helper.</param>
    private void RegisterDebugCommands(IModHelper helper)
    {
        helper.ConsoleCommands.Add(
            name: "imm_list",
            documentation: "List the minecart spots this mod knows about and whether the current player has unlocked them.\n\nUsage: imm_list",
            callback: this.HandleList
        );

        helper.ConsoleCommands.Add(
            name: "imm_unlock",
            documentation: "Unlock a minecart spot for the current player. Add 'force' to skip the requirements.\n\nUsage: imm_unlock <spot-id> [force]",
            callback: this.HandleUnlock
        );

        helper.ConsoleCommands.Add(
            name: "imm_lock",
            documentation: "Lock a minecart spot for the current player.\n\nUsage: imm_lock <spot-id>",
            callback: this.HandleLock
        );

        helper.ConsoleCommands.Add(
            name: "imm_difficulty",
            documentation: "Switch the active difficulty and apply it immediately.\n\nUsage: imm_difficulty <label>",
            callback: this.HandleDifficulty
        );

        helper.ConsoleCommands.Add(
            name: "imm_give",
            documentation: "Add the current stage's gold and items to your inventory (for testing).\n\nUsage: imm_give <spot-id>",
            callback: this.HandleGive
        );
    }

    /// <summary>Handle the <c>imm_list</c> command.</summary>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    private void HandleList(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log($"Difficulty: {this.Config.Difficulty}", LogLevel.Info);

        foreach (MinecartSpot spot in this.SpotRepository.Spots.OrderBy(spot => spot.Unlock.Gold).ThenBy(spot => spot.Id))
        {
            SpotProgress progress = ProgressionService.GetProgress(spot, Game1.player);
            string state = progress.IsUnlocked
                ? "unlocked"
                : progress.TotalSteps > 0
                    ? $"step {progress.CompletedSteps}/{progress.TotalSteps}" + (progress.DaysRemaining > 0 ? $" (ready in {progress.DaysRemaining}d)" : "")
                    : "locked";
            int gold = progress.CurrentStep?.Gold ?? spot.Unlock.Gold;

            this.Monitor.Log($"{spot.Id} | {spot.TargetLocation} ({spot.TargetTile.X},{spot.TargetTile.Y}) | {gold}g | {state}", LogLevel.Info);
        }
    }

    /// <summary>Handle the <c>imm_unlock</c> command.</summary>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    private void HandleUnlock(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }

        if (args.Length < 1 || !this.SpotRepository.TryGet(args[0], out MinecartSpot? spot))
        {
            this.Monitor.Log($"Usage: {command} <spot-id> [force]. Use 'imm_list' to see the spot IDs.", LogLevel.Warn);
            return;
        }

        if (args.Length > 1 && args[1].Equals("force", StringComparison.OrdinalIgnoreCase))
        {
            this.Progression.ForceAdvance(spot, Game1.player);
            this.Monitor.Log($"Force-advanced '{spot.Id}'.", LogLevel.Info);
            return;
        }

        if (this.Progression.TryAdvance(spot, Game1.player, out string? failure))
            this.Monitor.Log($"Advanced '{spot.Id}'.", LogLevel.Info);
        else
            this.Monitor.Log($"Couldn't advance '{spot.Id}': {failure}", LogLevel.Warn);
    }

    /// <summary>Handle the <c>imm_lock</c> command.</summary>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    private void HandleLock(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }

        if (args.Length < 1 || !this.SpotRepository.TryGet(args[0], out MinecartSpot? spot))
        {
            this.Monitor.Log($"Usage: {command} <spot-id>. Use 'imm_list' to see the spot IDs.", LogLevel.Warn);
            return;
        }

        this.Progression.Lock(spot, Game1.player);
        this.Monitor.Log($"Locked '{spot.Id}'.", LogLevel.Info);
    }

    /// <summary>Handle the <c>imm_difficulty</c> command.</summary>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    private void HandleDifficulty(string command, string[] args)
    {
        if (args.Length < 1)
        {
            this.Monitor.Log($"Usage: {command} <label>. Available: {string.Join(", ", this.Difficulties.Labels)}", LogLevel.Warn);
            return;
        }

        if (!this.Difficulties.TryGet(args[0], out DifficultySettings? _))
        {
            this.Monitor.Log($"Unknown difficulty '{args[0]}'. Available: {string.Join(", ", this.Difficulties.Labels)}", LogLevel.Warn);
            return;
        }

        this.Config.Difficulty = this.Difficulties.ResolveLabel(args[0]);
        this.Helper.WriteConfig(this.Config);
        this.ApplyConfig();

        this.Monitor.Log($"Difficulty set to '{this.Config.Difficulty}'.", LogLevel.Info);
    }

    /// <summary>Handle the <c>imm_give</c> command.</summary>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    private void HandleGive(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }

        if (args.Length < 1 || !this.SpotRepository.TryGet(args[0], out MinecartSpot? spot))
        {
            this.Monitor.Log($"Usage: {command} <spot-id>. Use 'imm_list' to see the spot IDs.", LogLevel.Warn);
            return;
        }

        SpotProgress progress = ProgressionService.GetProgress(spot, Game1.player);
        int gold = progress.CurrentStep?.Gold ?? spot.Unlock.Gold;
        List<ItemRequirement> items = progress.CurrentStep?.Items ?? spot.Unlock.Items;

        if (gold > 0)
            Game1.player.Money += gold;

        foreach (ItemRequirement requirement in items)
            Game1.player.addItemToInventory(ItemRegistry.Create(requirement.ItemId, requirement.Count));

        this.Monitor.Log($"Gave {gold}g and {items.Count} item stack(s) for '{spot.Id}'.", LogLevel.Info);
    }
}