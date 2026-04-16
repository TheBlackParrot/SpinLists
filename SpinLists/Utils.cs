using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SpinCore.UI;
using SpinLists.UI;
using SpinShareLib.Types;
using UnityEngine;
using UnityEngine.Networking;
using Playlist = SpinLists.Classes.Playlist;

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
    
    internal static void SetButtonAvailable(ref CustomButton? customButton, bool available, string? translationKey = null)
    {
        if (customButton == null)
        {
            return;
        }
        
        XDNavigableButton button = customButton.GameObject.GetComponent<XDNavigableButton>();
        CanvasGroup canvasGroup = customButton.GameObject.GetComponent<CanvasGroup>();
        button.interactable = available;
        canvasGroup.alpha = (available ? 1f : 0.5f);

        if (translationKey != null)
        {
            customButton.TextTranslationKey = translationKey;
        }
    }

    internal static async Task ResetTrackSelectionList()
    {
        MetadataHandle? selectedTrack = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1;
        
        TrackListSystem.AllTracksEnumerator allTracksEnumerator = (GameSystemSingleton<TrackListSystem, TrackListSystemSettings>.Instance.AllTracks with
        {
            sorterSettings = TrackSorterSettings.DefaultValues
        }).GetEnumerator();
        List<MetadataHandle> allTracks = [];
        for (int i = 0; i < allTracksEnumerator.GetTrackCount(); i++)
        {
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
        
        foreach (Playlist playlist in SpinListPanel.Playlists)
        {
            if (playlist.ActivateButton != null)
            {
                playlist.ActivateButton.TextTranslationKey = $"{Plugin.TRANSLATION_PREFIX}View";
            }
        }

        await Task.Delay(100);
        PlayerSettingsData.Instance.PreferredSortModeArcade.Value = 1;
        await Task.Delay(100);
        PlayerSettingsData.Instance.PreferredSortModeArcade.Value = 0;
    }

    private static async Task DownloadSpinShareChart(string fileReference)
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
        
        //int successfulDownloads = 0;
        //NotificationSystemGUI.AddMessage($"Downloading {fileReferences.Count} charts...", 5f);
        
        foreach (string fileReference in fileReferences)
        {
            await Plugin.SpinShare.downloadSongAndUnzip(fileReference, CustomAssetLoadingHelper.CUSTOM_DATA_PATH);
            
            /*if (!await Plugin.SpinShare.downloadSongAndUnzip(fileReference, CustomAssetLoadingHelper.CUSTOM_DATA_PATH))
            {
                continue;
            }

            successfulDownloads++;
            if (successfulDownloads > 0 && successfulDownloads % 3 == 0)
            {
                NotificationSystemGUI.AddMessage($"Successfully downloaded {successfulDownloads} of {fileReferences.Count} chart{(fileReferences.Count > 1 ? "s" : "")}...", 2f);
            }*/
        }
        
        //NotificationSystemGUI.AddMessage($"Successfully downloaded {successfulDownloads} of {fileReferences.Count} chart{(fileReferences.Count > 1 ? "s" : "")}!", 5f);
        NotificationSystemGUI.AddMessage("Finished downloading missing charts!", 5f);
    }

    internal static Texture2D FixPlaylistCoverImage(Texture2D texture)
    {
        // https://christianjmills.com/posts/crop-images-on-gpu-tutorial/
        int size;
        int[] coords;

        if (texture.width > texture.height)
        {
            size = texture.height;
            coords = [(int)((texture.width - texture.height) / 2f), 0];
        }
        else
        {
            size = texture.width;
            coords = [0, (int)((texture.height - texture.width) / 2f)];
        }

        RenderTexture cropped = RenderTexture.GetTemporary(size, size);

        Graphics.CopyTexture(texture, 0, 0, coords[0], coords[1], size, size, cropped, 0, 0, 0, 0);
        
        RenderTexture sizer = RenderTexture.GetTemporary(256, 256);
        Graphics.Blit(cropped, sizer);
            
        RenderTexture.ReleaseTemporary(cropped);
            
        Texture2D finalCover = new(sizer.width, sizer.height);
        finalCover.ReadPixels(new Rect(0, 0, sizer.width, sizer.height), 0, 0);
        finalCover.Apply();
        
        RenderTexture.ReleaseTemporary(sizer);
        
        return finalCover;
    }

    internal static async Task<Playlist?> DownloadSpinSharePlaylist(uint id)
    {
        Content<SpinShareLib.Types.Playlist>? playlistData = await Plugin.SpinShare.getPlaylist(id.ToString());
        if (playlistData == null)
        {
            return null;
        }

        if (playlistData.status is < 200 or >= 300)
        {
            return null;
        }
        
        // web requests to file:// are just easier and i'm all about easy
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(playlistData.data.cover);
        UnityWebRequestAsyncOperation response = request.SendWebRequest();

        bool isCoverFetched = false;
        Texture2D? texture = null;
        
        response.completed += async _ =>
        {
            await Awaitable.MainThreadAsync();
            
            try
            {
                texture = DownloadHandlerTexture.GetContent(request);
            }
            catch (Exception e)
            {
                if (e is not (InvalidOperationException or HttpRequestException))
                {
                    throw;
                }
            }

            isCoverFetched = true;
        };

        while (!isCoverFetched)
        {
            await Awaitable.EndOfFrameAsync();
        }

        if (texture == null)
        {
            return new Playlist(playlistData.data);
        }
        
        Texture2D finalCover = FixPlaylistCoverImage(texture);
        File.WriteAllBytes($"{SpinListPanel.PlaylistsPath}\\{playlistData.data.fileReference}.jpg", finalCover.EncodeToJPG());

        return new Playlist(playlistData.data);
    }
    
    internal static async Task<Playlist?> DownloadSpinShareUserChartsAsPlaylist(uint id)
    {
        Content<UserDetail>? userDetail = await Plugin.SpinShare.getUserDetail(id.ToString());
        if (userDetail == null)
        {
            return null;
        }
        
        Content<Song[]> userCharts = await Plugin.SpinShare.getUserCharts(id.ToString());
        if (userCharts.data.Length == 0)
        {
            return null;
        }
        
        HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            $"{nameof(SpinLists)}/{MyPluginInfo.PLUGIN_VERSION} (https://github.com/TheBlackParrot/SpinLists)");
        HttpResponseMessage responseMessage =
            await httpClient.GetAsync(userDetail.data.avatar);
        responseMessage.EnsureSuccessStatusCode();
        
        File.WriteAllBytes($"{SpinListPanel.PlaylistsPath}\\user_{userDetail.data.id}.{new Uri(userDetail.data.avatar).Segments.Last().Split('.').Last()}",
            await responseMessage.Content.ReadAsByteArrayAsync());
        
        return userDetail.status is < 200 or >= 300 ? null : new Playlist(userDetail.data, userCharts.data);
    }
}