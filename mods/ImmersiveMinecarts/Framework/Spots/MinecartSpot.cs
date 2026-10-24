using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

namespace ImmersiveMinecarts.Framework.Spots;

/// <summary>The root of <c>assets/minecart-spots.json</c>.</summary>
internal sealed class MinecartSpotFile
{
    /// <summary>The spots defined by this mod.</summary>
    public List<MinecartSpot> Spots { get; set; } = [];
}

/// <summary>A minecart destination and the requirements to unlock it.</summary>
internal sealed class MinecartSpot
{
    /// <summary>A unique ID for this spot, used in save data and destination conditions.</summary>
    public string Id { get; set; } = "";

    /// <summary>The network ID to add this spot to (see <c>Data/Minecarts</c>).</summary>
    public string Network { get; set; } = "Default";

    /// <summary>The translation key for the name shown in the minecart menu.</summary>
    public string DisplayNameKey { get; set; } = "";

    /// <summary>The location to warp to.</summary>
    public string TargetLocation { get; set; } = "";

    /// <summary>The tile within <see cref="TargetLocation"/> to warp to.</summary>
    public Point TargetTile { get; set; }

    /// <summary>The direction the player faces after arriving.</summary>
    public string TargetDirection { get; set; } = "down";

    /// <summary>An optional gold price charged for each ride, separate from the unlock cost.</summary>
    public int RidePrice { get; set; }

    /// <summary>Whether to use the farmhouse door tile instead of <see cref="TargetTile"/>. Requires <see cref="TargetLocation"/> to be <c>Farm</c>.</summary>
    public bool UseMainFarmHouseEntry { get; set; }

    /// <summary>The requirements to unlock this spot for a player.</summary>
    public UnlockRequirement Unlock { get; set; } = new();

    /// <summary>Create a deep copy of this spot.</summary>
    public MinecartSpot Clone()
    {
        return new MinecartSpot
        {
            Id = this.Id,
            Network = this.Network,
            DisplayNameKey = this.DisplayNameKey,
            TargetLocation = this.TargetLocation,
            TargetTile = this.TargetTile,
            UseMainFarmHouseEntry = this.UseMainFarmHouseEntry,
            TargetDirection = this.TargetDirection,
            RidePrice = this.RidePrice,
            Unlock = new UnlockRequirement
            {
                Gold = this.Unlock.Gold,
                Condition = this.Unlock.Condition,
                Items = this.Unlock.Items
                    .Select(item => new ItemRequirement { ItemId = item.ItemId, Count = item.Count })
                    .ToList(),
                Steps = this.Unlock.Steps
                    .Select(step => new UnlockStep
                    {
                        Id = step.Id,
                        NameKey = step.NameKey,
                        DescriptionKey = step.DescriptionKey,
                        Gold = step.Gold,
                        Condition = step.Condition,
                        WaitDays = step.WaitDays,
                        Items = step.Items
                            .Select(item => new ItemRequirement { ItemId = item.ItemId, Count = item.Count })
                            .ToList(),
                        NpcFlow = step.NpcFlow
                            .Select(flow => new NpcFlowStep
                            {
                                Npc = flow.Npc,
                                Topic = flow.Topic,
                                DialogueKey = flow.DialogueKey
                            })
                            .ToList()
                    })
                    .ToList()
            }
        };
    }
}

/// <summary>The requirements to unlock a minecart spot.</summary>
internal sealed class UnlockRequirement
{
    /// <summary>The gold cost, or zero for a free unlock.</summary>
    public int Gold { get; set; }

    /// <summary>An optional game state query the player must match (e.g. having visited the area).</summary>
    public string? Condition { get; set; }

    /// <summary>The items the player must hand over, or an empty list for none.</summary>
    public List<ItemRequirement> Items { get; set; } = [];

    /// <summary>The ordered steps to complete. When non-empty, the top-level gold and item costs are ignored.</summary>
    public List<UnlockStep> Steps { get; set; } = [];
}

/// <summary>One payment stage in a multi-step unlock.</summary>
internal sealed class UnlockStep
{
    /// <summary>A unique ID for this step within the spot.</summary>
    public string Id { get; set; } = "";

    /// <summary>The translation key for the step name.</summary>
    public string NameKey { get; set; } = "";

    /// <summary>The translation key for the step description.</summary>
    public string? DescriptionKey { get; set; }

    /// <summary>The gold cost for this step, or zero for none.</summary>
    public int Gold { get; set; }

    /// <summary>An optional game state query the player must match for this step.</summary>
    public string? Condition { get; set; }

    /// <summary>The items the player must hand over for this step, or an empty list for none.</summary>
    public List<ItemRequirement> Items { get; set; } = [];

    /// <summary>The number of days to wait after completing the previous step before this step is available.</summary>
    public int WaitDays { get; set; }

    /// <summary>Optional NPC conversations that must happen before this step can be paid, in order.</summary>
    public List<NpcFlowStep> NpcFlow { get; set; } = [];
}

/// <summary>One conversation in an NPC-driven step.</summary>
internal sealed class NpcFlowStep
{
    /// <summary>The NPC's internal name, like <c>Clint</c> or <c>Robin</c>.</summary>
    public string Npc { get; set; } = "";

    /// <summary>The conversation topic ID added to the player's active dialogue events.</summary>
    public string Topic { get; set; } = "";

    /// <summary>The translation key for the dialogue shown by this NPC.</summary>
    public string DialogueKey { get; set; } = "";
}

/// <summary>An item and count required to unlock a spot.</summary>
internal sealed class ItemRequirement
{
    /// <summary>The qualified item ID (like <c>(O)90</c>).</summary>
    public string ItemId { get; set; } = "";

    /// <summary>The number of items required.</summary>
    public int Count { get; set; } = 1;
}