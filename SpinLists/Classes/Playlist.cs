using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpinCore.UI;
using SpinLists.Patches;
using SpinLists.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpinLists.Classes;

[method: JsonConstructor]
public class Playlist()
{
    private readonly uint _id = (uint)SpinListPanel.Playlists.Count;

    [JsonIgnore]
    internal List<string> MissingCharts = [];
    
    [JsonProperty(PropertyName = "entries")]
    public List<PlaylistEntry> Entries = [];
    
    [JsonProperty(PropertyName = "name")]
    public string Name = $"Playlist_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    [JsonProperty(PropertyName = "author", NullValueHandling = NullValueHandling.Ignore)]
    public string? Author;
    
    [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description;
    
    // sane default, doesn't matter
    internal string FilePath = $"{SpinListPanel.PlaylistsPath}\\Playlist_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json";
    
    private CustomGroup _rowEntry = null!;
    private CustomGroup _rowDisplay = null!;
    private CustomGroup _metadataGroup = null!;
    private CustomButton? _modifyPlaylistButton;
    internal CustomButton? ActivateButton;
    internal CustomButton? MissingButton;
    private CustomTextComponent? _playlistChartCount;
    
    private async Task SetArt(Texture2D? texture)
    {
        await Awaitable.MainThreadAsync();
        
        texture ??= GameSystemSingleton<TrackListSystem, TrackListSystemSettings>.Settings.fallbackAlbumArt;
            
        CustomImage artImage = UIHelper.CreateImage(_rowDisplay, "QueueEntryArt", texture);
        artImage.Transform.SetSiblingIndex(0);

        artImage.Transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(110, 110);

        artImage.Transform.GetComponent<LayoutElement>().preferredHeight = 100;
        artImage.Transform.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
    }

    private CustomTextComponent CreateMetadataRow(string objectName, string? extraText = null,
        FontStyles style = FontStyles.Normal, float fontSize = 30f, float alpha = 1f)
    {
        CustomTextComponent textComponent = UIHelper.CreateLabel(_metadataGroup, objectName, TranslationReference.Empty);
        textComponent.ExtraText = extraText ?? string.Empty;
        
        LayoutElement textComponentLayoutComponent = textComponent.Transform.GetComponent<LayoutElement>();
        textComponentLayoutComponent.preferredWidth = 350;
        
        CustomTextMeshProUGUI textComponentTextComponent = textComponent.Transform.GetComponent<CustomTextMeshProUGUI>();
        textComponentTextComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponentTextComponent.overflowMode = TextOverflowModes.Ellipsis;
        textComponentTextComponent.richText = false;
        textComponentTextComponent.fontSize = fontSize;
        textComponentTextComponent.fontStyle = style;
        textComponentTextComponent.alpha = alpha;

        return textComponent;
    }

    internal async Task CreatePlaylistRow(Texture2D? coverImage = null, bool forceToTop = false)
    {
        await Awaitable.MainThreadAsync();
        
        _rowEntry = UIHelper.CreateGroup(SpinListPanel.SidePanel!.PanelContentTransform, $"PlaylistRow_{_id}");
        _rowEntry.Transform.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 10, 20);
        
        _rowDisplay = UIHelper.CreateGroup(_rowEntry, "PlaylistRowDisplay", Axis.Horizontal);
        _rowDisplay.Transform.GetComponent<HorizontalLayoutGroup>().spacing = 10f;
        
        #region metadata
        _metadataGroup = UIHelper.CreateGroup(_rowDisplay, "PlaylistMetadata");
        VerticalLayoutGroup metadataLayoutGroupComponent = _metadataGroup.Transform.GetComponent<VerticalLayoutGroup>();
        metadataLayoutGroupComponent.spacing = 0;

        CreateMetadataRow("PlaylistTitle", Name);
        
        _playlistChartCount = CreateMetadataRow("PlaylistChartCount", string.Empty, FontStyles.Italic, 22.5f, 0.5f);
        UpdatePlaylistChartCountText();

        CreateMetadataRow("PlaylistAuthor", (string.IsNullOrEmpty(Author) ? string.Empty : $"by {Author}"), FontStyles.Normal, 22.5f);
        #endregion
        
        await SetArt(coverImage);
        
        #region buttons
        CustomGroup buttonGroup = UIHelper.CreateGroup(_rowEntry, "PlaylistButtons", Axis.Horizontal);
        
        ActivateButton = UIHelper.CreateButton(buttonGroup, "ActivatePlaylist", $"{Plugin.TRANSLATION_PREFIX}View", OnPlaylistSelected);
        ActivateButton.Transform.GetComponent<LayoutElement>().preferredWidth = 100;
        ActivateButton.Transform.GetComponent<XDNavigable>().forceExpanded = true;
        
        _modifyPlaylistButton = UIHelper.CreateButton(buttonGroup, "ModifyPlaylist", $"{Plugin.TRANSLATION_PREFIX}Add", OnPlaylistWantsToBeModified);
        _modifyPlaylistButton.Transform.GetComponent<LayoutElement>().preferredWidth = 100;
        UpdateModifyButtonText();
        #endregion
        
        #region missing button
        CustomGroup missingButtonGroup = UIHelper.CreateGroup(_rowEntry, "MissingButtonContainer", Axis.Horizontal);
        MissingButton = UIHelper.CreateButton(missingButtonGroup, "DownloadMissingCharts", $"{Plugin.TRANSLATION_PREFIX}DownloadMissing",
            async void () =>
            {
                try
                {
                    await Utils.BatchDownloadSpinShareCharts(MissingCharts);
                }
                catch (Exception)
                {
                    // do nothing
                }
            });
        MissingButton.GameObject.SetActive(MissingCharts.Any());
        #endregion

        if (forceToTop)
        {
            _rowEntry.Transform.SetSiblingIndex(SpinListPanel.ListHeader.Transform.GetSiblingIndex() + 1);
        }
    }

    internal void UpdateModifyButtonText()
    {
        if (_modifyPlaylistButton == null)
        {
            return;
        }
        
        string activeFileReference = Utils.GetFileReference(XDSelectionListMenu.Instance._previewTrackDataSetup.Item1);
        
        _modifyPlaylistButton.TextTranslationKey = Entries.Exists(x => x.FileReference == activeFileReference)
            ? $"{Plugin.TRANSLATION_PREFIX}Remove"
            : $"{Plugin.TRANSLATION_PREFIX}Add";
    }

    internal void OnPlaylistSelected()
    {
        if (ActivateButton?.TextTranslationKey == $"{Plugin.TRANSLATION_PREFIX}Back")
        {
            UpdatePlaylistViewingState.ViewingPlaylist = false;
            return;
        }
        
        Plugin.DebugMessage($"Selected playlist {Name}");
        
        if (Entries.Count - MissingCharts.Count == 0)
        {
            Plugin.Log.LogInfo("Playlist is empty");
            return;
        }
        
        MetadataHandle? selectedTrack = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1;
        
        XDSelectionListMenu.Instance.ClearSearch();
        PlayerSettingsData.Instance.FilterCustomTracks.ResetData();
        PlayerSettingsData.Instance.FilterMaximumDifficulty.ResetData();
        PlayerSettingsData.Instance.FilterMinimumDifficulty.ResetData();
        PlayerSettingsData.Instance.ShowOnlyFavouritesArcade.ResetData();
        
        UpdatePlaylistViewingState.ViewingPlaylist = true;
        SpinListPanel.SelectedPlaylist = this;
        
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
        
        List<MetadataHandle> sortedTracks = [];

        foreach (PlaylistEntry entry in Entries)
        {
            try
            {
                sortedTracks.Add(allTracks.First(x => Utils.GetFileReference(x) == entry.FileReference));
                Plugin.DebugMessage($"Found chart {entry.FileReference}");
            }
            catch (Exception e)
            {
                // allTracks.First will throw an InvalidOperationException if charts can't be found, let's not spam logs
                if (e is not InvalidOperationException)
                {
                    Plugin.Log.LogWarning(e);   
                }
            }
        }
        
        XDSelectionListMenu.Instance.state.trackSelectionList.items.Clear();
        Plugin.DebugMessage("Cleared selection list");
        foreach (MetadataHandle foundHandle in sortedTracks)
        {
            XDSelectionListMenu.Instance.state.trackSelectionList.items.Add(foundHandle);   
        }
        Plugin.DebugMessage($"trackSelectionList should have {XDSelectionListMenu.Instance.state.trackSelectionList.items.Count} items");
        XDSelectionListMenu.Instance.CreateListIfNeeded();
        
        XDSelectionListMenu.Instance.ScrollToTrack(XDSelectionListMenu.Instance.state.trackSelectionList.items.Contains(selectedTrack) ? selectedTrack : null
                                                   ?? (MetadataHandle)XDSelectionListMenu.Instance.state.trackSelectionList.items.First());
    }

    private void OnPlaylistWantsToBeModified()
    {
        string activeFileReference = Utils.GetFileReference(XDSelectionListMenu.Instance._previewTrackDataSetup.Item1);
        bool existsInPlaylist = Entries.Exists(x => x.FileReference == activeFileReference);

        if (existsInPlaylist)
        {
            RemoveFromPlaylist(activeFileReference);
        }
        else
        {
            AddToPlaylist(XDSelectionListMenu.Instance._previewTrackDataSetup.Item1);
        }

        UpdatePlaylistChartCountText();
        UpdateModifyButtonText();
    }

    internal void UpdatePlaylistChartCountText()
    {
        if (_playlistChartCount != null)
        {
            _playlistChartCount.ExtraText = $"{Entries.Count:N0} charts {(MissingCharts.Count > 0 ? $" ({MissingCharts.Count:N0} missing)" : "")}";
        }
        MissingButton?.GameObject.SetActive(MissingCharts.Any());
    }
    
    internal void UpdateMissingCharts()
    {
        TrackListSystem.AllTracksEnumerator allTracksEnumerator = (GameSystemSingleton<TrackListSystem, TrackListSystemSettings>.Instance.AllTracks with
        {
            sorterSettings = TrackSorterSettings.DefaultValues
        }).GetEnumerator();
        List<string> allFileReferences = [];
        for (int i = 0; i < allTracksEnumerator.GetTrackCount(); i++)
        {
            allFileReferences.Add(Utils.GetFileReference(allTracksEnumerator.Current));
            allTracksEnumerator.MoveNext();
        }

        MissingCharts = Entries.Where(entry => !allFileReferences.Contains(entry.FileReference))
            .Select(x => x.FileReference).ToList();
        
        MissingButton?.GameObject.SetActive(MissingCharts.Any());

        UpdatePlaylistChartCountText();
    }

    private void RemoveFromPlaylist(string fileReference)
    {
        Entries.RemoveAll(x => x.FileReference == fileReference);
        Save();

        // ReSharper disable once InvertIf
        if (UpdatePlaylistViewingState.ViewingPlaylist)
        {
            XDSelectionListMenu.Instance.state.trackSelectionList.items.Remove(XDSelectionListMenu.Instance._previewTrackDataSetup.Item1);
            if (XDSelectionListMenu.Instance.state.trackSelectionList.items.Count == 0)
            {
                UpdatePlaylistViewingState.ViewingPlaylist = false;
            }
        }
    }

    private void AddToPlaylist(MetadataHandle metadataHandle)
    {
        Entries.Add(new PlaylistEntry(metadataHandle));
        Save();
    }

    internal void Save()
    {
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    internal PlaylistEntry? GetPlaylistEntry(MetadataHandle metadataHandle)
    {
        string fileReference = Utils.GetFileReference(metadataHandle);
        
        try
        {
            return Entries.First(x => x.FileReference == fileReference);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal void Destroy()
    {
        Object.Destroy(_rowEntry.GameObject);
    }
}