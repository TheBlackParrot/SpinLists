using System.Collections.Generic;

namespace SpinLists;

internal abstract class Utils
{
    internal static string GetFileReference(MetadataHandle? metadataHandle)
    {
        if (metadataHandle == null)
        {
            Plugin.Log.LogWarning("MetadataHandle is null");
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