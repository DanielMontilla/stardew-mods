using System.Collections.Generic;

namespace ImmersiveMinecarts;

/// <summary>The mod configuration.</summary>
internal sealed class ModConfig
{
    /// <summary>The difficulty label from <c>assets/difficulties.json</c>.</summary>
    public string Difficulty { get; set; } = "Realistic";

    /// <summary>Whether to show a HUD message when a destination is unlocked.</summary>
    public bool ShowUnlockNotifications { get; set; } = true;

    /// <summary>Whether to register the <c>imm_*</c> debug commands. Disable before release.</summary>
    public bool EnableDebugCommands { get; set; } = true;

    /// <summary>Per-spot position overrides. Spots not listed use the position from <c>assets/minecart-spots.json</c>.</summary>
    public Dictionary<string, SpotPosition> SpotPositions { get; set; } = [];
}

/// <summary>Overrides where a minecart drops the player off.</summary>
internal sealed class SpotPosition
{
    /// <summary>The X tile, or <c>null</c> to keep the spot's default.</summary>
    public int? X { get; set; }

    /// <summary>The Y tile, or <c>null</c> to keep the spot's default.</summary>
    public int? Y { get; set; }

    /// <summary>The direction to face after arriving, or <c>null</c> to keep the spot's default.</summary>
    public string? Direction { get; set; }
}