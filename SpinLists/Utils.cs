using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpinLists.UI;

namespace SpinLists;

internal abstract class Utils
{
    internal static string GetFileReference(MetadataHandle? metadataHandle)
    {
        if (metadataHandle == null)
        {
            return string.Empty;
        }

        if (!metadataHandle.IsLoaded())
        {
            // i... don't think this is an issue? only ever getting 1 hit here out of 1375
            Plugin.Log.LogWarning("MetadataHandle is not loaded");
            metadataHandle.LoadIfNeeded();
            return string.Empty;
        }

        string? reference = metadataHandle.UniqueName;
        if (string.IsNullOrEmpty(reference))
        {
            return reference;
        }

        if (reference.LastIndexOf('_') != -1)
        {
            reference = reference.Remove(metadataHandle.UniqueName.LastIndexOf('_')).Replace("CUSTOM_", string.Empty);
        }

        return reference;
    }

    internal static void ResetTrackSelectionList()
    {
        MetadataHandle? selectedTrack = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1;
        
        TrackListSystem.AllTracksEnumerator allTracksEnumerator = (GameSystemSingleton<TrackListSystem, TrackListSystemSettings>.Instance.AllTracks with
        {
            sorterSettings = TrackSorterSettings.DefaultValues
        }).GetEnumerator();
        List<MetadataHandle> allTracks = [];
        for (int i = 0; i < allTracksEnumerator.GetTrackCount(); i++)
        {
            if (allTracksEnumerator.Current == null)
            {
                continue;
            }
            
            allTracks.Add(allTracksEnumerator.Current);
            allTracksEnumerator.MoveNext();
        }
        Plugin.DebugMessage($"{allTracks.Count} charts are present");
            
        XDSelectionListMenu.Instance.state.trackSelectionList.items.Clear();
        Plugin.DebugMessage("Cleared selection list");
        foreach (MetadataHandle foundHandle in allTracks)
        {
            XDSelectionListMenu.Instance.state.trackSelectionList.items.Add(foundHandle);   
        }
        Plugin.DebugMessage($"trackSelectionList should have {XDSelectionListMenu.Instance.state.trackSelectionList.items.Count} items");
        XDSelectionListMenu.Instance.CreateListIfNeeded();

        if (selectedTrack != null)
        {
            XDSelectionListMenu.Instance.ScrollToTrack(selectedTrack);
        }
        else
        {
            XDSelectionListMenu.Instance.ScrollToItem(XDSelectionListMenu.Instance.state.trackSelectionList.items.First());
        }

        SpinListPanel.SelectedPlaylist = null;
    }

    internal static async Task DownloadSpinShareChart(string fileReference)
    {
        if (await Plugin.SpinShare.downloadSongAndUnzip(fileReference, CustomAssetLoadingHelper.CUSTOM_DATA_PATH))
        {
            NotificationSystemGUI.AddMessage($"Successfully downloaded chart {fileReference}!", 5f);
        }
        //XDSelectionListMenu.Instance.FireRapidTrackDataChange();
    }

    internal static async Task BatchDownloadSpinShareCharts(List<string> fileReferences)
    {
        if (fileReferences.Count == 1)
        {
            await DownloadSpinShareChart(fileReferences[0]);
            return;
        }
        
        int successfulDownloads = 0;
        NotificationSystemGUI.AddMessage($"Downloading {fileReferences.Count} charts...", 5f);
        
        foreach (string fileReference in fileReferences)
        {
            if (!await Plugin.SpinShare.downloadSongAndUnzip(fileReference, CustomAssetLoadingHelper.CUSTOM_DATA_PATH))
            {
                continue;
            }

            successfulDownloads++;
            if (successfulDownloads > 0 && successfulDownloads % 3 == 0)
            {
                NotificationSystemGUI.AddMessage($"Successfully downloaded {successfulDownloads} of {fileReferences.Count} chart{(fileReferences.Count > 1 ? "s" : "")}...", 2f);
            }
        }
        
        NotificationSystemGUI.AddMessage($"Successfully downloaded {successfulDownloads} of {fileReferences.Count} chart{(fileReferences.Count > 1 ? "s" : "")}!", 5f);
    }
}