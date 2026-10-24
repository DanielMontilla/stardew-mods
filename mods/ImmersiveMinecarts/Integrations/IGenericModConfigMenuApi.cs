using System;

using StardewModdingAPI;

namespace ImmersiveMinecarts.Integrations;

/// <summary>The subset of the Generic Mod Config Menu API used by this mod.</summary>
public interface IGenericModConfigMenuApi
{
    /// <summary>Register a mod whose config can be edited through the UI.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="reset">Reset the mod's config to its default values.</param>
    /// <param name="save">Save the mod's current config.</param>
    /// <param name="titleScreenOnly">Whether the options can only be edited from the title screen.</param>
    void Register(IManifest manifest, Action reset, Action save, bool titleScreenOnly = false);

    /// <summary>Add a section title at the current position in the form.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="text">The title text.</param>
    /// <param name="tooltip">The tooltip text, or <c>null</c> for none.</param>
    void AddSectionTitle(IManifest manifest, Func<string> text, Func<string>? tooltip = null);

    /// <summary>Add a boolean option at the current position in the form.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="getValue">Get the current value.</param>
    /// <param name="setValue">Set a new value.</param>
    /// <param name="name">The label text.</param>
    /// <param name="tooltip">The tooltip text, or <c>null</c> for none.</param>
    /// <param name="fieldId">The unique field ID, or <c>null</c> to auto-generate one.</param>
    void AddBoolOption(IManifest manifest, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

    /// <summary>Add an integer option at the current position in the form.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="getValue">Get the current value.</param>
    /// <param name="setValue">Set a new value.</param>
    /// <param name="name">The label text.</param>
    /// <param name="tooltip">The tooltip text, or <c>null</c> for none.</param>
    /// <param name="min">The minimum allowed value, or <c>null</c> for none.</param>
    /// <param name="max">The maximum allowed value, or <c>null</c> for none.</param>
    /// <param name="interval">The interval between values, or <c>null</c> for the default.</param>
    /// <param name="formatValue">Get the display text for a value, or <c>null</c> to show the number.</param>
    /// <param name="fieldId">The unique field ID, or <c>null</c> to auto-generate one.</param>
    void AddNumberOption(IManifest manifest, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

    /// <summary>Add a string option at the current position in the form.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="getValue">Get the current value.</param>
    /// <param name="setValue">Set a new value.</param>
    /// <param name="name">The label text.</param>
    /// <param name="tooltip">The tooltip text, or <c>null</c> for none.</param>
    /// <param name="allowedValues">The values that can be selected, or <c>null</c> for any value.</param>
    /// <param name="formatAllowedValue">Get the display text for a value, or <c>null</c> to show it as-is.</param>
    /// <param name="fieldId">The unique field ID, or <c>null</c> to auto-generate one.</param>
    void AddTextOption(IManifest manifest, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);

    /// <summary>Start a new page, or switch to it if it already exists.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="pageId">The unique page ID.</param>
    /// <param name="pageTitle">The page title, or <c>null</c> to use the page ID.</param>
    void AddPage(IManifest manifest, string pageId, Func<string>? pageTitle = null);

    /// <summary>Add a link to a page at the current position in the form.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="pageId">The page ID to open.</param>
    /// <param name="text">The link text.</param>
    /// <param name="tooltip">The tooltip text, or <c>null</c> for none.</param>
    void AddPageLink(IManifest manifest, string pageId, Func<string> text, Func<string>? tooltip = null);

    /// <summary>Remove a mod from the config UI and delete its options.</summary>
    /// <param name="manifest">The mod's manifest.</param>
    void Unregister(IManifest manifest);
}