using System.Collections.Generic;
using HarmonyLib;
using SpinLists.Classes;
using SpinLists.UI;

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
            
            foreach (Playlist playlist in SpinListPanel.Playlists)
            {
                if (playlist.ActivateButton != null)
                {
                    playlist.ActivateButton.TextTranslationKey = $"{Plugin.TRANSLATION_PREFIX}View";
                }
            }
            
            TrackListSystem.AllTracksEnumerator allTracksEnumerator = GameSystemSingleton<TrackListSystem, TrackListSystemSettings>.Instance.AllTracks.GetEnumerator();
            allTracksEnumerator.sorterSettings = TrackSorterSettings.DefaultValues;
            List<MetadataHandle> allTracks = [];
            for (int i = 0; i < allTracksEnumerator.GetTrackCount(); i++)
            {
                allTracks.Add(allTracksEnumerator.Current);
                allTracksEnumerator.MoveNext();
            }
            Plugin.Log.LogInfo($"{allTracks.Count} charts are present");
            
            XDSelectionListMenu.Instance.state.trackSelectionList.items.Clear();
            Plugin.Log.LogInfo("Cleared selection list");
            foreach (MetadataHandle foundHandle in allTracks)
            {
                XDSelectionListMenu.Instance.state.trackSelectionList.items.Add(foundHandle);   
            }
            Plugin.Log.LogInfo($"trackSelectionList should have {XDSelectionListMenu.Instance.state.trackSelectionList.items.Count} items");
            XDSelectionListMenu.Instance.CreateListIfNeeded();
        }
    }
    
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.ClearSearch))]
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.OnSearchChange))]
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.SearchString), MethodType.Setter)]
    [HarmonyPostfix]
    internal static void OnSearchChangePatch()
    {
        Plugin.Log.LogInfo("should change");
        ViewingPlaylist = false;
    }

    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.OnStartupInitialise))]
    [HarmonyPostfix]
    internal static void OnStartupInitialisePatch()
    {
        XDSelectionListMenu.Instance.searchInputField.OnValueChanged += (_, _) =>
        {
            Plugin.Log.LogInfo("should change");
            ViewingPlaylist = false;
        };
        
        XDSelectionListMenu.Instance.searchInputField.tmpInputField.onValueChanged.AddListener((_) =>
        {
            Plugin.Log.LogInfo("should change");
            ViewingPlaylist = false;
        });
    }
    
    private static readonly List<string> ResetDataValidKeys = [];
    
    [HarmonyPatch(typeof(IntValueDefaults), nameof(IntValueDefaults.ResetData))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    internal static void ResetData_Patch(IntValueDefaults __instance)
    {
        if (ResetDataValidKeys.Count == 0)
        {
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterCustomTracks.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterMaximumDifficulty.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.FilterMinimumDifficulty.Key);
            ResetDataValidKeys.Add(PlayerSettingsData.Instance.ShowOnlyFavouritesArcade.Key);
        }

        if (!ResetDataValidKeys.Contains(__instance.Key))
        {
            return;
        }
        
        Plugin.Log.LogInfo("should change");
        ViewingPlaylist = false;
    }
}