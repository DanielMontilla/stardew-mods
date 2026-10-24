using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ImmersiveMinecarts.Framework.Difficulty;

using Microsoft.Xna.Framework;

using StardewModdingAPI;

namespace ImmersiveMinecarts.Framework.Spots;

/// <summary>Loads the minecart spots defined in <c>assets/minecart-spots.json</c> and applies the active difficulty and position overrides.</summary>
internal sealed class MinecartSpotRepository
{
    /*********
    ** Fields
    *********/
    private const string AssetPath = "assets/minecart-spots.json";

    private static readonly HashSet<string> ValidDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "up",
        "down",
        "left",
        "right"
    };

    private readonly IModHelper Helper;
    private readonly IMonitor Monitor;
    private readonly List<MinecartSpot> BaseSpots = [];
    private readonly Dictionary<string, MinecartSpot> SpotsById = new(StringComparer.OrdinalIgnoreCase);


    /*********
    ** Accessors
    *********/
    /// <summary>The spots after applying the active difficulty and position overrides.</summary>
    public IReadOnlyCollection<MinecartSpot> Spots => this.SpotsById.Values;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="helper">The mod helper.</param>
    /// <param name="monitor">The mod monitor.</param>
    public MinecartSpotRepository(IModHelper helper, IMonitor monitor)
    {
        this.Helper = helper;
        this.Monitor = monitor;
    }

    /// <summary>Load or reload the base spot definitions.</summary>
    public void Load()
    {
        this.BaseSpots.Clear();
        this.SpotsById.Clear();

        MinecartSpotFile? file;
        try
        {
            file = this.Helper.ModContent.Load<MinecartSpotFile>(AssetPath);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to load {AssetPath}: {ex}", LogLevel.Error);
            return;
        }

        HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (MinecartSpot spot in file?.Spots ?? [])
        {
            string? error = Validate(spot);
            if (error is not null)
            {
                this.Monitor.Log($"Ignored invalid minecart spot '{spot.Id}': {error}", LogLevel.Warn);
                continue;
            }

            if (!seenIds.Add(spot.Id))
            {
                this.Monitor.Log($"Ignored duplicate minecart spot ID '{spot.Id}'.", LogLevel.Warn);
                continue;
            }

            this.BaseSpots.Add(spot);
        }

        this.Monitor.Log($"Loaded {this.BaseSpots.Count} minecart spot(s).", LogLevel.Trace);
    }

    /// <summary>Build the effective spots by applying a difficulty and the player's position overrides.</summary>
    /// <param name="difficulty">The active difficulty preset.</param>
    /// <param name="config">The mod configuration.</param>
    public void Apply(DifficultySettings difficulty, ModConfig config)
    {
        this.SpotsById.Clear();

        foreach (MinecartSpot baseSpot in this.BaseSpots)
        {
            MinecartSpot spot = baseSpot.Clone();

            spot.Unlock.Gold = ScaleGold(spot.Unlock.Gold, difficulty.UnlockGoldMultiplier);
            spot.RidePrice = ScaleGold(spot.RidePrice, difficulty.RidePriceMultiplier);
            ScaleItems(spot.Unlock.Items, difficulty.UnlockItemMultiplier);

            foreach (UnlockStep step in spot.Unlock.Steps)
            {
                step.Gold = ScaleGold(step.Gold, difficulty.UnlockGoldMultiplier);
                ScaleItems(step.Items, difficulty.UnlockItemMultiplier);
            }

            if (!difficulty.RequireDiscovery)
                spot.Unlock.Condition = null;

            if (config.SpotPositions is not null && config.SpotPositions.TryGetValue(spot.Id, out SpotPosition? position))
                this.ApplyPosition(spot, position);

            this.SpotsById[spot.Id] = spot;
        }

        this.Monitor.Log($"Applied difficulty '{difficulty.Label}' to {this.SpotsById.Count} spot(s).", LogLevel.Trace);
    }

    /// <summary>Get a spot by ID.</summary>
    /// <param name="id">The spot ID.</param>
    /// <param name="spot">The matching spot, if found.</param>
    public bool TryGet(string id, [NotNullWhen(true)] out MinecartSpot? spot)
    {
        return this.SpotsById.TryGetValue(id, out spot);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Apply a configured position override to a spot.</summary>
    /// <param name="spot">The spot to update.</param>
    /// <param name="position">The configured position.</param>
    private void ApplyPosition(MinecartSpot spot, SpotPosition position)
    {
        if (position.X.HasValue || position.Y.HasValue)
        {
            spot.TargetTile = new Point(position.X ?? spot.TargetTile.X, position.Y ?? spot.TargetTile.Y);
            spot.UseMainFarmHouseEntry = false;
        }

        if (string.IsNullOrWhiteSpace(position.Direction))
            return;

        if (ValidDirections.Contains(position.Direction))
            spot.TargetDirection = position.Direction;
        else
            this.Monitor.Log($"Ignored invalid direction '{position.Direction}' for spot '{spot.Id}'.", LogLevel.Warn);
    }

    /// <summary>Scale a gold value and round it to the nearest integer.</summary>
    /// <param name="value">The base value.</param>
    /// <param name="multiplier">The multiplier to apply.</param>
    private static int ScaleGold(int value, double multiplier)
    {
        return Math.Max(0, (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero));
    }

    /// <summary>Scale item counts in place.</summary>
    /// <param name="items">The items to scale.</param>
    /// <param name="multiplier">The multiplier to apply.</param>
    private static void ScaleItems(List<ItemRequirement> items, double multiplier)
    {
        foreach (ItemRequirement item in items)
            item.Count = ScaleCount(item.Count, multiplier);
    }

    /// <summary>Scale an item count and round up, with a minimum of one.</summary>
    /// <param name="value">The base count.</param>
    /// <param name="multiplier">The multiplier to apply.</param>
    private static int ScaleCount(int value, double multiplier)
    {
        return Math.Max(1, (int)Math.Ceiling(value * multiplier));
    }

    /// <summary>Get a validation error for a spot, if it's invalid.</summary>
    /// <param name="spot">The spot to validate.</param>
    private static string? Validate(MinecartSpot spot)
    {
        if (string.IsNullOrWhiteSpace(spot.Id))
            return "the ID is empty";
        if (string.IsNullOrWhiteSpace(spot.Network))
            return "the network ID is empty";
        if (string.IsNullOrWhiteSpace(spot.DisplayNameKey))
            return "the display name key is empty";
        if (string.IsNullOrWhiteSpace(spot.TargetLocation))
            return "the target location is empty";
        if (spot.UseMainFarmHouseEntry && !string.Equals(spot.TargetLocation, "Farm", StringComparison.OrdinalIgnoreCase))
            return "UseMainFarmHouseEntry requires the target location to be Farm";
        if (!ValidDirections.Contains(spot.TargetDirection))
            return $"the target direction '{spot.TargetDirection}' isn't one of up, down, left, or right";
        if (spot.Unlock.Gold < 0)
            return "the gold cost can't be negative";
        if (spot.RidePrice < 0)
            return "the ride price can't be negative";

        if (ValidateItems(spot.Unlock.Items) is { } itemError)
            return itemError;

        HashSet<string> stepIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (UnlockStep step in spot.Unlock.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
                return "a step has an empty ID";
            if (!stepIds.Add(step.Id))
                return $"the step ID '{step.Id}' is duplicated";
            if (step.Gold < 0)
                return $"the step '{step.Id}' has a negative gold cost";
            if (step.WaitDays < 0)
                return $"the step '{step.Id}' has a negative wait time";
            if (ValidateItems(step.Items) is { } stepItemError)
                return $"the step '{step.Id}' has an invalid item: {stepItemError}";

            foreach (NpcFlowStep flow in step.NpcFlow)
            {
                if (string.IsNullOrWhiteSpace(flow.Npc))
                    return $"the step '{step.Id}' has an NPC flow entry with an empty NPC name";
                if (string.IsNullOrWhiteSpace(flow.Topic))
                    return $"the step '{step.Id}' has an NPC flow entry with an empty topic";
                if (string.IsNullOrWhiteSpace(flow.DialogueKey))
                    return $"the step '{step.Id}' has an NPC flow entry with an empty dialogue key";
            }
        }

        return null;
    }

    /// <summary>Get a validation error for a set of item requirements, if any.</summary>
    /// <param name="items">The items to validate.</param>
    private static string? ValidateItems(IReadOnlyList<ItemRequirement> items)
    {
        foreach (ItemRequirement item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemId))
                return "an item requirement has an empty item ID";
            if (item.Count < 1)
                return $"the item requirement '{item.ItemId}' has a count below 1";
        }

        return null;
    }
}