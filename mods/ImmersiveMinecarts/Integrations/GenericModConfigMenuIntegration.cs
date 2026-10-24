using System;
using System.Linq;

using ImmersiveMinecarts.Framework.Difficulty;
using ImmersiveMinecarts.Framework.Spots;

using StardewModdingAPI;

namespace ImmersiveMinecarts.Integrations;

/// <summary>Registers this mod's options in Generic Mod Config Menu when it's installed.</summary>
internal sealed class GenericModConfigMenuIntegration
{
    /*********
    ** Fields
    *********/
    private const string ModId = "spacechase0.GenericModConfigMenu";
    private const string PositionPageId = "positions";
    private const int MinTile = 0;
    private const int MaxTile = 500;

    private static readonly string[] ValidDirections = ["up", "down", "left", "right"];

    private readonly IModHelper Helper;
    private readonly IManifest Manifest;
    private readonly IMonitor Monitor;
    private readonly Func<ModConfig> GetConfig;
    private readonly Action Reset;
    private readonly Action SaveAndApply;
    private readonly MinecartSpotRepository Spots;
    private readonly DifficultyRepository Difficulties;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="helper">The mod helper.</param>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="monitor">The mod monitor.</param>
    /// <param name="getConfig">Get the current config.</param>
    /// <param name="reset">Reset the config to its defaults.</param>
    /// <param name="saveAndApply">Save and apply the current config.</param>
    /// <param name="spots">The spot repository.</param>
    /// <param name="difficulties">The difficulty repository.</param>
    public GenericModConfigMenuIntegration(IModHelper helper, IManifest manifest, IMonitor monitor, Func<ModConfig> getConfig, Action reset, Action saveAndApply, MinecartSpotRepository spots, DifficultyRepository difficulties)
    {
        this.Helper = helper;
        this.Manifest = manifest;
        this.Monitor = monitor;
        this.GetConfig = getConfig;
        this.Reset = reset;
        this.SaveAndApply = saveAndApply;
        this.Spots = spots;
        this.Difficulties = difficulties;
    }

    /// <summary>Register the config menu, if Generic Mod Config Menu is installed.</summary>
    public void Register()
    {
        IGenericModConfigMenuApi? api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModId);
        if (api is null)
        {
            this.Monitor.Log("Generic Mod Config Menu isn't installed; options can still be edited in config.json.", LogLevel.Trace);
            return;
        }

        api.Unregister(this.Manifest);
        api.Register(this.Manifest, this.Reset, this.SaveAndApply);

        // general options
        api.AddSectionTitle(this.Manifest, () => this.Translate("config.section.general"));
        api.AddTextOption(
            manifest: this.Manifest,
            getValue: () => this.GetConfig().Difficulty,
            setValue: value => this.GetConfig().Difficulty = value,
            name: () => this.Translate("config.difficulty"),
            tooltip: this.GetDifficultyTooltip,
            allowedValues: this.Difficulties.Labels.ToArray(),
            formatAllowedValue: this.FormatDifficulty
        );
        api.AddBoolOption(
            manifest: this.Manifest,
            getValue: () => this.GetConfig().ShowUnlockNotifications,
            setValue: value => this.GetConfig().ShowUnlockNotifications = value,
            name: () => this.Translate("config.show-notifications"),
            tooltip: () => this.Translate("config.show-notifications.tooltip")
        );
        api.AddBoolOption(
            manifest: this.Manifest,
            getValue: () => this.GetConfig().EnableDebugCommands,
            setValue: value => this.GetConfig().EnableDebugCommands = value,
            name: () => this.Translate("config.enable-debug"),
            tooltip: () => this.Translate("config.enable-debug.tooltip")
        );

        // position pages
        api.AddPageLink(this.Manifest, PositionPageId, () => this.Translate("config.page.positions"), () => this.Translate("config.page.positions.tooltip"));
        api.AddPage(this.Manifest, PositionPageId, () => this.Translate("config.page.positions"));

        MinecartSpot[] orderedSpots = this.Spots.Spots
            .OrderBy(spot => spot.Unlock.Gold)
            .ThenBy(spot => spot.Id)
            .ToArray();

        foreach (MinecartSpot spot in orderedSpots)
            api.AddPageLink(this.Manifest, GetSpotPageId(spot), () => this.GetSpotName(spot), () => this.Translate("config.page.spot.tooltip"));

        foreach (MinecartSpot spot in orderedSpots)
        {
            api.AddPage(this.Manifest, GetSpotPageId(spot), () => this.GetSpotName(spot));

            api.AddNumberOption(
                manifest: this.Manifest,
                getValue: () => this.GetX(spot),
                setValue: value => this.SetX(spot, value),
                name: () => this.Translate("config.spot.x"),
                tooltip: () => this.Translate("config.spot.x.tooltip"),
                min: MinTile,
                max: MaxTile
            );
            api.AddNumberOption(
                manifest: this.Manifest,
                getValue: () => this.GetY(spot),
                setValue: value => this.SetY(spot, value),
                name: () => this.Translate("config.spot.y"),
                tooltip: () => this.Translate("config.spot.y.tooltip"),
                min: MinTile,
                max: MaxTile
            );
            api.AddTextOption(
                manifest: this.Manifest,
                getValue: () => this.GetDirection(spot),
                setValue: value => this.SetDirection(spot, value),
                name: () => this.Translate("config.spot.direction"),
                tooltip: () => this.Translate("config.spot.direction.tooltip"),
                allowedValues: ValidDirections
            );
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the page ID for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private static string GetSpotPageId(MinecartSpot spot)
    {
        return $"spot-{spot.Id}";
    }

    /// <summary>Get a translated string.</summary>
    /// <param name="key">The translation key.</param>
    private string Translate(string key)
    {
        return this.Helper.Translation.Get(key).ToString();
    }

    /// <summary>Get the translated name for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private string GetSpotName(MinecartSpot spot)
    {
        return this.Translate(spot.DisplayNameKey);
    }

    /// <summary>Get the tooltip for the difficulty dropdown.</summary>
    private string GetDifficultyTooltip()
    {
        ModConfig config = this.GetConfig();
        if (this.Difficulties.TryGet(config.Difficulty, out DifficultySettings? settings) && !string.IsNullOrWhiteSpace(settings.DescriptionKey))
            return this.Translate(settings.DescriptionKey);

        return this.Translate("config.difficulty.tooltip");
    }

    /// <summary>Get the display text for a difficulty label.</summary>
    /// <param name="label">The difficulty label.</param>
    private string FormatDifficulty(string label)
    {
        return this.Difficulties.TryGet(label, out DifficultySettings? settings) && !string.IsNullOrWhiteSpace(settings.DisplayNameKey)
            ? this.Translate(settings.DisplayNameKey)
            : label;
    }

    /// <summary>Get the configured X tile for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private int GetX(MinecartSpot spot)
    {
        return this.GetExistingPosition(spot.Id)?.X ?? spot.TargetTile.X;
    }

    /// <summary>Set the configured X tile for a spot.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="value">The new X tile.</param>
    private void SetX(MinecartSpot spot, int value)
    {
        this.GetPosition(spot.Id).X = value;
    }

    /// <summary>Get the configured Y tile for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private int GetY(MinecartSpot spot)
    {
        return this.GetExistingPosition(spot.Id)?.Y ?? spot.TargetTile.Y;
    }

    /// <summary>Set the configured Y tile for a spot.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="value">The new Y tile.</param>
    private void SetY(MinecartSpot spot, int value)
    {
        this.GetPosition(spot.Id).Y = value;
    }

    /// <summary>Get the configured arrival direction for a spot.</summary>
    /// <param name="spot">The spot.</param>
    private string GetDirection(MinecartSpot spot)
    {
        return this.GetExistingPosition(spot.Id)?.Direction ?? spot.TargetDirection;
    }

    /// <summary>Set the configured arrival direction for a spot.</summary>
    /// <param name="spot">The spot.</param>
    /// <param name="value">The new direction.</param>
    private void SetDirection(MinecartSpot spot, string value)
    {
        this.GetPosition(spot.Id).Direction = value;
    }

    /// <summary>Get the config override for a spot, if one exists.</summary>
    /// <param name="spotId">The spot ID.</param>
    private SpotPosition? GetExistingPosition(string spotId)
    {
        return this.GetConfig().SpotPositions is { } positions && positions.TryGetValue(spotId, out SpotPosition? position)
            ? position
            : null;
    }

    /// <summary>Get or create the config override for a spot.</summary>
    /// <param name="spotId">The spot ID.</param>
    private SpotPosition GetPosition(string spotId)
    {
        ModConfig config = this.GetConfig();
        config.SpotPositions ??= [];

        if (!config.SpotPositions.TryGetValue(spotId, out SpotPosition? position))
        {
            position = new SpotPosition();
            config.SpotPositions[spotId] = position;
        }

        return position;
    }
}