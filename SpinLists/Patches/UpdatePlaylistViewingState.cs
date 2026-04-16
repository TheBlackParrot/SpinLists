using System.Collections.Generic;
using HarmonyLib;

namespace SpinLists.Patches;

[HarmonyPatch]
internal static class UpdatePlaylistViewingState
{
    internal static bool ViewingPlaylist
    {
        get;
        set
        {
            bool oldValue = field;
            field = value;
            
            if (value || oldValue == value)
            {
                return;
            }
            
            _ = Utils.ResetTrackSelectionList();
        }
    }
    
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.ClearSearch))]
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.OnSearchChange))]
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.SearchString), MethodType.Setter)]
    [HarmonyPostfix]
    internal static void OnSearchChangePatch()
    {
        ViewingPlaylist = false;
    }

    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.OnStartupInitialise))]
    [HarmonyPostfix]
    internal static void OnStartupInitialisePatch()
    {
        XDSelectionListMenu.Instance.searchInputField.OnValueChanged += (_, _) =>
        {
            ViewingPlaylist = false;
        };
        
        XDSelectionListMenu.Instance.searchInputField.tmpInputField.onValueChanged.AddListener((_) =>
        {
            ViewingPlaylist = false;
        });
    }
    
    private static readonly List<string> ResetDataValidKeys = [];

    [HarmonyPatch(typeof(IntValueDefaults), nameof(IntValueDefaults.ResetData))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    internal static void ResetData_Patch(IntValueDefaults __instance)
    {
        Plugin.DebugMessage(__instance.Key);
        
        if (ResetDataValidKeys.Count == 0)
        {
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterLockedTracks.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterCustomTracks.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterMaximumDifficulty.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterMinimumDifficulty.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.ShowOnlyFavouritesArcade.Key);
        }

        if (!ResetDataValidKeys.Contains(__instance.Key))
        {
            return;
        }

        ViewingPlaylist = false;
    }
}