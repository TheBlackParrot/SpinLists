using System;
using System.Threading.Tasks;
using HarmonyLib;
using SpinLists.Classes;
using SpinLists.UI;
using UnityEngine;
using XDMenuPlay.TrackMenus;

namespace SpinLists.Patches;

[HarmonyPatch]
internal static class TrackDataChangePatches
{
    private static bool _armed;

    [HarmonyPatch(typeof(CustomAssetLoadingHelper), nameof(CustomAssetLoadingHelper.FullRefresh))]
    [HarmonyPatch(typeof(CustomAssetLoadingHelper), nameof(CustomAssetLoadingHelper.SRTrackBundle_Changed))]
    [HarmonyPostfix]
    internal static void ArmingPatch()
    {
        Plugin.DebugMessage("should be armed");
        _armed = true;
    }
    
    [HarmonyPatch(typeof(TrackList), nameof(TrackList.UpdateHandles))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    internal static void UpdateItemHandlers_Patch()
    {
        if (!_armed)
        {
            return;
        }
        
        _armed = false;
        Plugin.DebugMessage("should no longer be armed");
        
        Plugin.DebugMessage("File change callback");

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250); // figure out a better way to track this fml
                await Awaitable.MainThreadAsync();
                
                foreach (Playlist playlist in SpinListPanel.Playlists)
                {
                    playlist.UpdateMissingCharts();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        });
    }
    
    [HarmonyPatch(typeof(CustomAssetLoadingHelper), nameof(CustomAssetLoadingHelper.DeleteFileWithName))]
    [HarmonyPostfix]
    internal static void DeletePatch()
    {
        foreach (Playlist playlist in SpinListPanel.Playlists)
        {
            playlist.UpdateMissingCharts();
        }
    }
}