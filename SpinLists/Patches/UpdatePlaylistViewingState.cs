using System.Collections.Generic;
using HarmonyLib;

namespace SpinLists.Patches;

[HarmonyPatch]
internal static class UpdatePlaylistViewingState
{
    internal static bool ViewingPlaylist;
    private static readonly List<string> ResetDataValidKeys = [];
    
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.ClearSearch))]
    [HarmonyPostfix]
    internal static void XDSelectionListMenu_Patch()
    {
        ViewingPlaylist = false;
    }

    /*[HarmonyPatch(typeof(IDataValue), nameof(IDataValue.ResetData))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    internal static void IPlayerValue_Patch(IDataValue __instance)
    {
        if (ResetDataValidKeys.Count == 0)
        {
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterCustomTracks.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterMaximumDifficulty.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterMinimumDifficulty.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.ShowOnlyFavouritesArcade.Key);
        }

        if (ResetDataValidKeys.Contains(__instance.Key))
        {
            ViewingPlaylist = false;
        }
    }*/
}