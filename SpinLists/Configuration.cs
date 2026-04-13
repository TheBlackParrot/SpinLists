using BepInEx.Configuration;

namespace SpinLists;

public partial class Plugin
{
    internal static ConfigEntry<bool> SuggestedDifficultyMode = null!;

    private void RegisterConfigEntries()
    {
        SuggestedDifficultyMode = Config.Bind("General", nameof(SuggestedDifficultyMode), false,
            "When true, add the actively selected difficulty as a suggested difficulty in the playlist, and automatically switch to the suggested difficulty when selecting charts");
    }
}

