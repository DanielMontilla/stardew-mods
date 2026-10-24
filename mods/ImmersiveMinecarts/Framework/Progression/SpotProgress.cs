using ImmersiveMinecarts.Framework.Spots;

namespace ImmersiveMinecarts.Framework.Progression;

/// <summary>A player's progress toward unlocking a spot.</summary>
internal sealed class SpotProgress
{
    /// <summary>Whether the spot is fully unlocked.</summary>
    public bool IsUnlocked { get; init; }

    /// <summary>The number of completed steps.</summary>
    public int CompletedSteps { get; init; }

    /// <summary>The total number of steps (zero for single-step spots).</summary>
    public int TotalSteps { get; init; }

    /// <summary>The number of days until the next step becomes available.</summary>
    public int DaysRemaining { get; init; }

    /// <summary>The step waiting to be paid, if any.</summary>
    public UnlockStep? CurrentStep { get; init; }
}