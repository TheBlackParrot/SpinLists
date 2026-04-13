using HarmonyLib;
using SpinLists.Classes;
using SpinLists.UI;
using UnityEngine;
using XDMenuPlay.TrackMenus;

namespace SpinLists.Patches;

[HarmonyPatch]
internal class CheckSelectionListPatches
{
    private static string _lastUniqueName = string.Empty;

    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.UpdatePreviewHandle))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void ForceDifficultyPatch(XDSelectionListMenu __instance)
    {
        // ReSharper disable once InvertIf (readability)
        if (UpdatePlaylistViewingState.ViewingPlaylist
            && SpinListPanel.SelectedPlaylist != null
            && Plugin.SuggestedDifficultyMode.Value)
        {
            PlaylistEntry? playlistEntry =
                SpinListPanel.SelectedPlaylist.GetPlaylistEntry(__instance._previewTrackDataSetup.Item1);
            
            if (playlistEntry?.SuggestedDifficulty == null)
            {
                return;
            }
            
            TrackData.DifficultyType difficultyType =
                __instance._previewTrackDataSetup.Item1.GetClosestDifficulty(playlistEntry.SuggestedDifficulty.Value);

            DifficultySelectMultiOptionHandler? selector = Object.FindFirstObjectByType<DifficultySelectMultiOptionHandler>(FindObjectsInactive.Exclude);
            if (selector?._setValueGuard ?? true)
            {
                return;
            }
            
            int closestIndex = DifficultySelectMultiOptionHandler.GetClosestIndex(selector._availableDifficultyTypes, difficultyType);
            if (selector.multiChoice.TargetIndex == closestIndex)
            {
                return;
            }
            
            Plugin.Log.LogInfo($"Should switch to {playlistEntry.SuggestedDifficulty.Value}");
            selector.multiChoice.TargetIndex = closestIndex;
        }
    }
    
    [HarmonyPatch(typeof(XDSelectionListMenu), nameof(XDSelectionListMenu.UpdatePreviewHandle))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void UpdateModifyButtonTextPatch(XDSelectionListMenu __instance)
    {
        if (__instance._previewTrackDataSetup.Item1 == null)
        {
            return;
        }
        if (_lastUniqueName == __instance._previewTrackDataSetup.Item1.UniqueName)
        {
            return;
        }
        
        _lastUniqueName = __instance._previewTrackDataSetup.Item1.UniqueName;
        foreach (Playlist playlist in SpinListPanel.Playlists)
        {
            playlist.UpdateModifyButtonText();   
        }
    }
}