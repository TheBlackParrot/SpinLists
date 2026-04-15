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

    private void RegisterConfigEntries()
    {
        SuggestedDifficultyMode = Config.Bind("General", nameof(SuggestedDifficultyMode), false,
            "When true, add the actively selected difficulty as a suggested difficulty in the playlist, and automatically switch to the suggested difficulty when selecting charts");
        PlaylistSortMethod = Config.Bind("General", nameof(PlaylistSortMethod), SpinLists.PlaylistSortMethod.CreationTime,
            "Order to sort playlists in the side panel");
    }
}

