using BepInEx.Configuration;

namespace SpinLists;

public enum PlaylistSortMethod
{
    Name = 0,
    Author = 1,
    CreationTime = 2,
    ModificationTime = 3
}

public partial class Plugin
{
    internal static ConfigEntry<bool> SuggestedDifficultyMode = null!;
    internal static ConfigEntry<PlaylistSortMethod> PlaylistSortMethod = null!;
    
    internal static ConfigEntry<uint> MinimumDifficultyThreshold = null!;
    internal static ConfigEntry<uint> MaximumDifficultyThreshold = null!;
    internal static ConfigEntry<bool> AlsoApplyThresholdsToPlaylists = null!;

    private void RegisterConfigEntries()
    {
        SuggestedDifficultyMode = Config.Bind("General", nameof(SuggestedDifficultyMode), false,
            "When true, add the actively selected difficulty as a suggested difficulty in the playlist, and automatically switch to the suggested difficulty when selecting charts");
        PlaylistSortMethod = Config.Bind("General", nameof(PlaylistSortMethod), SpinLists.PlaylistSortMethod.CreationTime,
            "Order to sort playlists in the side panel");
        
        AlsoApplyThresholdsToPlaylists = Config.Bind("General", nameof(AlsoApplyThresholdsToPlaylists), false,
            "Also apply threshold settings to literal playlists");
        MinimumDifficultyThreshold = Config.Bind("Thresholds", nameof(MinimumDifficultyThreshold), (uint)0,
            "Ignore charts in generated/converted playlists that only contain difficulties rated below this number (0 to disable)");
        MaximumDifficultyThreshold = Config.Bind("Thresholds", nameof(MaximumDifficultyThreshold), (uint)0,
            "Ignore charts in generated/converted playlists that only contain difficulties rated above this number (0 to disable)");
    }
}

