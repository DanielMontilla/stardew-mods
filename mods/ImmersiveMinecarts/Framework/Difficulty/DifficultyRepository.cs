using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using StardewModdingAPI;

namespace ImmersiveMinecarts.Framework.Difficulty;

/// <summary>Loads the difficulty presets which are the source of truth for unlock costs.</summary>
internal sealed class DifficultyRepository
{
    /*********
    ** Fields
    *********/
    private const string AssetPath = "assets/difficulties.json";
    private const string DefaultLabel = "Realistic";

    private readonly IModHelper Helper;
    private readonly IMonitor Monitor;
    private readonly Dictionary<string, DifficultySettings> Presets = new(StringComparer.OrdinalIgnoreCase);


    /*********
    ** Accessors
    *********/
    /// <summary>The available difficulty labels, in display order.</summary>
    public IReadOnlyList<string> Labels => this.Presets.Keys.OrderBy(label => label, StringComparer.OrdinalIgnoreCase).ToList();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="helper">The mod helper.</param>
    /// <param name="monitor">The mod monitor.</param>
    public DifficultyRepository(IModHelper helper, IMonitor monitor)
    {
        this.Helper = helper;
        this.Monitor = monitor;
    }

    /// <summary>Load or reload the difficulty presets.</summary>
    public void Load()
    {
        this.Presets.Clear();

        try
        {
            Dictionary<string, DifficultySettings>? presets = this.Helper.ModContent.Load<Dictionary<string, DifficultySettings>>(AssetPath);
            if (presets is not null)
            {
                foreach ((string label, DifficultySettings settings) in presets)
                {
                    if (settings is null)
                    {
                        this.Monitor.Log($"Ignored difficulty '{label}' because its configuration is null.", LogLevel.Warn);
                        continue;
                    }

                    if (Validate(label, settings) is { } error)
                    {
                        this.Monitor.Log($"Ignored invalid difficulty '{label}': {error}", LogLevel.Warn);
                        continue;
                    }

                    settings.Label = label;
                    if (string.IsNullOrWhiteSpace(settings.DisplayNameKey))
                        settings.DisplayNameKey = label;

                    this.Presets[label] = settings;
                }
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to load {AssetPath}: {ex}", LogLevel.Error);
        }

        if (this.Presets.Count == 0)
        {
            this.Monitor.Log("No difficulty presets were loaded; using a built-in Realistic preset.", LogLevel.Warn);
            this.Presets[DefaultLabel] = new DifficultySettings
            {
                Label = DefaultLabel,
                DisplayNameKey = "difficulty.realistic.name",
                DescriptionKey = "difficulty.realistic.description"
            };
        }
    }

    /// <summary>Get the canonical label for a configured difficulty.</summary>
    /// <param name="label">The configured label, if any.</param>
    public string ResolveLabel(string? label)
    {
        if (!string.IsNullOrWhiteSpace(label))
        {
            foreach (string key in this.Presets.Keys)
            {
                if (string.Equals(key, label, StringComparison.OrdinalIgnoreCase))
                    return key;
            }
        }

        if (this.Presets.ContainsKey(DefaultLabel))
            return this.Presets.Keys.First(key => string.Equals(key, DefaultLabel, StringComparison.OrdinalIgnoreCase));

        return this.Presets.Keys.FirstOrDefault() ?? DefaultLabel;
    }

    /// <summary>Get a difficulty preset by label, falling back to the default if needed.</summary>
    /// <param name="label">The configured label, if any.</param>
    public DifficultySettings Get(string? label)
    {
        string resolved = this.ResolveLabel(label);

        return this.Presets.TryGetValue(resolved, out DifficultySettings? settings)
            ? settings
            : new DifficultySettings { Label = resolved, DisplayNameKey = resolved };
    }

    /// <summary>Get a difficulty preset by label.</summary>
    /// <param name="label">The difficulty label.</param>
    /// <param name="settings">The matching preset, if found.</param>
    public bool TryGet(string label, [NotNullWhen(true)] out DifficultySettings? settings)
    {
        return this.Presets.TryGetValue(label, out settings);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a validation error for a preset, if it's invalid.</summary>
    /// <param name="label">The preset label.</param>
    /// <param name="settings">The preset to validate.</param>
    private static string? Validate(string label, DifficultySettings settings)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "the label is empty";
        if (settings.UnlockGoldMultiplier < 0)
            return "UnlockGoldMultiplier can't be negative";
        if (settings.UnlockItemMultiplier < 0)
            return "UnlockItemMultiplier can't be negative";
        if (settings.RidePriceMultiplier < 0)
            return "RidePriceMultiplier can't be negative";

        return null;
    }
}