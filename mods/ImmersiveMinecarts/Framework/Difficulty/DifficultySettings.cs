namespace ImmersiveMinecarts.Framework.Difficulty;

/// <summary>A difficulty preset loaded from <c>assets/difficulties.json</c>.</summary>
internal sealed class DifficultySettings
{
    /// <summary>The label used in the config file (the key in <c>assets/difficulties.json</c>).</summary>
    public string Label { get; set; } = "";

    /// <summary>The translation key for the difficulty name.</summary>
    public string DisplayNameKey { get; set; } = "";

    /// <summary>The translation key for the difficulty description.</summary>
    public string DescriptionKey { get; set; } = "";

    /// <summary>The multiplier applied to each spot's unlock gold cost.</summary>
    public double UnlockGoldMultiplier { get; set; } = 1;

    /// <summary>The multiplier applied to each spot's required item counts.</summary>
    public double UnlockItemMultiplier { get; set; } = 1;

    /// <summary>The multiplier applied to each spot's per-ride price.</summary>
    public double RidePriceMultiplier { get; set; } = 1;

    /// <summary>Whether the spot's discovery condition must be met before it can be unlocked.</summary>
    public bool RequireDiscovery { get; set; } = true;
}