using System.Collections.Generic;

using ImmersiveMinecarts.Framework.Progression;
using ImmersiveMinecarts.Framework.Spots;

using Microsoft.Xna.Framework;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.GameData.Minecarts;

namespace ImmersiveMinecarts.Framework.Minecarts;

/// <summary>Adds this mod's spots to <c>Data/Minecarts</c>.</summary>
internal sealed class MinecartDataEditor
{
    /*********
    ** Fields
    *********/
    private readonly IModHelper Helper;
    private readonly MinecartSpotRepository Spots;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="helper">The mod helper.</param>
    /// <param name="spots">The spot definitions.</param>
    public MinecartDataEditor(IModHelper helper, MinecartSpotRepository spots)
    {
        this.Helper = helper;
        this.Spots = spots;
    }

    /// <summary>Start listening for asset requests.</summary>
    public void Enable()
    {
        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Add the mod's destinations when <c>Data/Minecarts</c> is loaded.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Minecarts"))
            return;

        e.Edit(asset =>
        {
            IDictionary<string, MinecartNetworkData> data = asset.AsDictionary<string, MinecartNetworkData>().Data;

            foreach (MinecartSpot spot in this.Spots.Spots)
            {
                if (!data.TryGetValue(spot.Network, out MinecartNetworkData? network) || network is null)
                {
                    network = new MinecartNetworkData { Destinations = [] };
                    data[spot.Network] = network;
                }

                network.Destinations ??= [];
                network.Destinations.RemoveAll(destination => destination?.Id == spot.Id);

                Point targetTile = spot.TargetTile;
                if (spot.UseMainFarmHouseEntry && Context.IsWorldReady)
                    targetTile = Game1.getFarm().GetMainFarmHouseEntry();

                network.Destinations.Add(new MinecartDestinationData
                {
                    Id = spot.Id,
                    DisplayName = this.Helper.Translation.Get(spot.DisplayNameKey).ToString(),
                    Condition = $"PLAYER_HAS_MAIL Current {ProgressionService.GetUnlockMailId(spot.Id)}",
                    Price = spot.RidePrice,
                    TargetLocation = spot.TargetLocation,
                    TargetTile = targetTile,
                    TargetDirection = spot.TargetDirection
                });
            }
        });
    }
}