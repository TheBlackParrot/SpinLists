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

public class Playlist
{
    private readonly uint _id = (uint)SpinListPanel.Playlists.Count;

    [JsonIgnore] private List<string> _missingCharts = [];
    
    [JsonProperty(PropertyName = "entries")]
    public List<PlaylistEntry> Entries = [];
    
    [JsonProperty(PropertyName = "name")]
    public string Name = $"Playlist_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

    [JsonProperty(PropertyName = "author", NullValueHandling = NullValueHandling.Ignore)]
    public string? Author;
    
    [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description;
    
    [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
    // this is here purely to render SpinShare playlists invalid for this type, don't ever use it
    private int? Status { set => throw new NotSupportedException(); }
    
    [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url;
    
    // sane default, doesn't matter
    internal string FilePath = $"{SpinListPanel.PlaylistsPath}\\Playlist_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json";
    
    [JsonProperty(PropertyName = "locked")]
    public bool Locked
    {
        get;
        set
        {
            field = value;
            _modifyPlaylistButton?.GameObject.SetActive(!value);
        }
    }
    
    private CustomGroup _rowEntry = null!;
    private CustomGroup _rowDisplay = null!;
    private CustomGroup _metadataGroup = null!;
    private CustomButton? _modifyPlaylistButton;
    internal CustomButton? ActivateButton;
    private CustomButton? _missingButton;
    private CustomTextComponent? _playlistChartCount;
    private CustomButton? _updateButton;

    [JsonConstructor]
    public Playlist() { }

    public Playlist(SpinShareLib.Types.Playlist playlist)
    {
        Name = playlist.title;
        Author = playlist.user.username;
        Description = playlist.description;
        FilePath = $"{SpinListPanel.PlaylistsPath}\\{playlist.fileReference}.json";
        Url = $"https://spinsha.re/api/playlist/{playlist.id}";
        
        Entries = playlist.songs.Select(x => new PlaylistEntry(x)).ToList();
        Locked = true;
    }

    public Playlist(SpinShareLib.Types.UserDetail userDetail, SpinShareLib.Types.Song[] charts)
    {
        Name = $"All {userDetail.username} Charts";
        Author = nameof(SpinLists);
        Description = $"All of {userDetail.username}'s SpinShare charts as of {DateTimeOffset.UtcNow.Date.ToLongDateString()}";
        FilePath = $"{SpinListPanel.PlaylistsPath}\\user_{userDetail.id}.json";
        Url = $"https://spinsha.re/api/user/{userDetail.id}/charts";
        
        Entries = charts.Select(x => new PlaylistEntry(x)).ToList();
        Locked = true;
    }
    
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
        
        #region update button
        if (Url != null)
        {
            CustomGroup updateButtonGroup = UIHelper.CreateGroup(_rowEntry, "UpdateButtonContainer", Axis.Horizontal);
            _updateButton = UIHelper.CreateButton(updateButtonGroup, "UpdatePlaylistButton", $"{Plugin.TRANSLATION_PREFIX}UpdatePlaylist",
                async void () =>
                {
                    try
                    {
                        Utils.SetButtonAvailable(ref _updateButton, false, $"{Plugin.TRANSLATION_PREFIX}Updating");
                        await UpdatePlaylist();
                        Utils.SetButtonAvailable(ref _updateButton, true, $"{Plugin.TRANSLATION_PREFIX}UpdatePlaylist");
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError(e);
                        Utils.SetButtonAvailable(ref _updateButton, true, $"{Plugin.TRANSLATION_PREFIX}UpdatePlaylist");
                    }
                });
            _updateButton.Transform.GetComponent<LayoutElement>().preferredHeight = 50;
            _updateButton.Transform.Find("IconContainer/ButtonText").GetComponent<CustomTextMeshProUGUI>().fontSizeMax = 35 - (35 / 3f);
        }
        #endregion
        
        #region buttons
        CustomGroup buttonGroup = UIHelper.CreateGroup(_rowEntry, "PlaylistButtons", Axis.Horizontal);
        
        ActivateButton = UIHelper.CreateButton(buttonGroup, "ActivatePlaylist", $"{Plugin.TRANSLATION_PREFIX}View", OnPlaylistSelected);
        ActivateButton.Transform.GetComponent<LayoutElement>().preferredWidth = 100;
        ActivateButton.Transform.GetComponent<XDNavigable>().forceExpanded = true;
        
        _modifyPlaylistButton = UIHelper.CreateButton(buttonGroup, "ModifyPlaylist", $"{Plugin.TRANSLATION_PREFIX}Add", OnPlaylistWantsToBeModified);
        _modifyPlaylistButton.Transform.GetComponent<LayoutElement>().preferredWidth = 100;
        UpdateModifyButtonText();

        _modifyPlaylistButton.GameObject.SetActive(!Locked);
        #endregion
        
        #region missing button
        CustomGroup missingButtonGroup = UIHelper.CreateGroup(_rowEntry, "MissingButtonContainer", Axis.Horizontal);
        _missingButton = UIHelper.CreateButton(missingButtonGroup, "DownloadMissingCharts", $"{Plugin.TRANSLATION_PREFIX}DownloadMissing",
            async void () =>
            {
                try
                {
                    Utils.SetButtonAvailable(ref _missingButton, false, $"{Plugin.TRANSLATION_PREFIX}Downloading");
                    await Utils.BatchDownloadSpinShareCharts(_missingCharts);
                    Utils.SetButtonAvailable(ref _missingButton, true, $"{Plugin.TRANSLATION_PREFIX}DownloadMissing");
                }
                catch (Exception)
                {
                    // do nothing
                }
            });
        _missingButton.Transform.GetComponent<LayoutElement>().preferredHeight = 50;
        _missingButton.Transform.Find("IconContainer/ButtonText").GetComponent<CustomTextMeshProUGUI>().fontSizeMax = 35 - (35 / 3f);
        _missingButton.GameObject.SetActive(_missingCharts.Any());
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
    
    // ReSharper disable once MemberCanBePrivate.Global (it can't be made private, actually :nerd_face:)
    internal void OnPlaylistSelected(bool force)
    {
        if (ActivateButton?.TextTranslationKey == $"{Plugin.TRANSLATION_PREFIX}Back" && !force)
        {
            UpdatePlaylistViewingState.ViewingPlaylist = false;
            return;
        }
        
        Plugin.DebugMessage($"Selected playlist {Name}");
        
        if (Entries.Count - _missingCharts.Count == 0)
        {
            Plugin.Log.LogInfo("Playlist is empty");
            return;
        }
        
        MetadataHandle? selectedTrack = XDSelectionListMenu.Instance.CurrentPreviewTrack.Item1;

        if (!UpdatePlaylistViewingState.ViewingPlaylist && !force)
        {
            XDSelectionListMenu.Instance.ClearSearch();
            PlayerSettingsData.Instance.FilterCustomTracks.ResetData();
            PlayerSettingsData.Instance.FilterMaximumDifficulty.ResetData();
            PlayerSettingsData.Instance.FilterMinimumDifficulty.ResetData();
            PlayerSettingsData.Instance.ShowOnlyFavouritesArcade.ResetData();
        }

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
    
    // ActivateButton's action specifically wants an action with no parameters, so we have to do optional parameters this way. guh
    // ReSharper disable once MemberCanBePrivate.Global
    internal void OnPlaylistSelected() => OnPlaylistSelected(false);

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

    private void UpdatePlaylistChartCountText()
    {
        if (_playlistChartCount != null)
        {
            _playlistChartCount.ExtraText =
                $"{Entries.Count:N0} chart{(Entries.Count != 1 ? "s" : "")} {(_missingCharts.Count > 0 ? $" ({_missingCharts.Count:N0} missing)" : "")}";
        }
        _missingButton?.GameObject.SetActive(_missingCharts.Any());
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

        _missingCharts = Entries.Where(entry => !allFileReferences.Contains(entry.FileReference))
            .Select(x => x.FileReference).ToList();
        
        _missingButton?.GameObject.SetActive(_missingCharts.Any());

        UpdatePlaylistChartCountText();
    }

    private async Task UpdatePlaylist()
    {
        if (Url == null)
        {
            return;
        }
        
        Uri uri = new(Url);
        if (uri.Host == "spinsha.re")
        {
            Plugin.DebugMessage(string.Join(",", uri.Segments));
            
            if (uri.Segments.Contains("user/"))
            {
                string id = uri.Segments[3].Replace("/", string.Empty);
                SpinShareLib.Types.Content<SpinShareLib.Types.Song[]>? charts = await Plugin.SpinShare.getUserCharts(id);
                if (charts == null)
                {
                    Plugin.Log.LogWarning($"Could not update SpinShare playlist {Name}, obtained data was empty");
                    NotificationSystemGUI.AddMessage($"Could not update SpinShare playlist <b>{Name}</b> (obtained data was empty)");
                    return;
                }

                List<SpinShareLib.Types.Song> filtered = charts.data.ToList();
                if (Plugin.MinimumDifficultyThreshold.Value > 0)
                {
                    int threshold = (int)Plugin.MinimumDifficultyThreshold.Value;
                    filtered = filtered.Where(chart => ((chart.easyDifficulty == 0 ? null : chart.easyDifficulty) ?? int.MinValue) >= threshold
                                                       || ((chart.normalDifficulty == 0 ? null : chart.normalDifficulty) ?? int.MinValue) >= threshold
                                                       || ((chart.hardDifficulty == 0 ? null : chart.hardDifficulty) ?? int.MinValue) >= threshold
                                                       || ((chart.expertDifficulty == 0 ? null : chart.expertDifficulty) ?? int.MinValue) >= threshold
                                                       || ((chart.XDDifficulty == 0 ? null : chart.XDDifficulty) ?? int.MinValue) >= threshold).ToList();
                }
                if (Plugin.MaximumDifficultyThreshold.Value > 0)
                {
                    int threshold = (int)Plugin.MaximumDifficultyThreshold.Value;
                    filtered = filtered.Where(chart => ((chart.easyDifficulty == 0 ? null : chart.easyDifficulty) ?? int.MaxValue) <= threshold
                                                       || ((chart.normalDifficulty == 0 ? null : chart.normalDifficulty) ?? int.MaxValue) <= threshold
                                                       || ((chart.hardDifficulty == 0 ? null : chart.hardDifficulty) ?? int.MaxValue) <= threshold
                                                       || ((chart.expertDifficulty == 0 ? null : chart.expertDifficulty) ?? int.MaxValue) <= threshold
                                                       || ((chart.XDDifficulty == 0 ? null : chart.XDDifficulty) ?? int.MaxValue) <= threshold).ToList();
                }
        
                Entries = filtered.Select(x => new PlaylistEntry(x)).ToList();
                Locked = Plugin.LockSpinSharePlaylists.Value;
            }
            else if (uri.Segments.Contains("playlist/"))
            {
                string id = uri.Segments[3].Replace("/", string.Empty);
                SpinShareLib.Types.Content<SpinShareLib.Types.Playlist>? playlist = await Plugin.SpinShare.getPlaylist(id);
                if (playlist == null)
                {
                    Plugin.Log.LogWarning($"Could not update SpinShare playlist {Name}, obtained data was empty");
                    NotificationSystemGUI.AddMessage($"Could not update SpinShare playlist <b>{Name}</b> (obtained data was empty)");
                    return;
                }
                
                List<SpinShareLib.Types.SongDetail> filtered = playlist.data.songs.ToList();
                if (Plugin.AlsoApplyThresholdsToPlaylists.Value)
                {
                    if (Plugin.MinimumDifficultyThreshold.Value > 0)
                    {
                        int threshold = (int)Plugin.MinimumDifficultyThreshold.Value;
                        filtered = filtered.Where(chart => ((chart.easyDifficulty == 0 ? null : chart.easyDifficulty) ?? int.MinValue) >= threshold
                                                           || ((chart.normalDifficulty == 0 ? null : chart.normalDifficulty) ?? int.MinValue) >= threshold
                                                           || ((chart.hardDifficulty == 0 ? null : chart.hardDifficulty) ?? int.MinValue) >= threshold
                                                           || ((chart.expertDifficulty == 0 ? null : chart.expertDifficulty) ?? int.MinValue) >= threshold
                                                           || ((chart.XDDifficulty == 0 ? null : chart.XDDifficulty) ?? int.MinValue) >= threshold).ToList();
                    }
                    if (Plugin.MaximumDifficultyThreshold.Value > 0)
                    {
                        int threshold = (int)Plugin.MaximumDifficultyThreshold.Value;
                        filtered = filtered.Where(chart => ((chart.easyDifficulty == 0 ? null : chart.easyDifficulty) ?? int.MaxValue) <= threshold
                                                           || ((chart.normalDifficulty == 0 ? null : chart.normalDifficulty) ?? int.MaxValue) <= threshold
                                                           || ((chart.hardDifficulty == 0 ? null : chart.hardDifficulty) ?? int.MaxValue) <= threshold
                                                           || ((chart.expertDifficulty == 0 ? null : chart.expertDifficulty) ?? int.MaxValue) <= threshold
                                                           || ((chart.XDDifficulty == 0 ? null : chart.XDDifficulty) ?? int.MaxValue) <= threshold).ToList();
                    }
                }

                Name = playlist.data.title;
                Author = playlist.data.user.username;
                Description = playlist.data.description;
                Entries = filtered.Select(x => new PlaylistEntry(x)).ToList();
                Locked = Plugin.LockSpinSharePlaylists.Value;
            }
            else
            {
                Plugin.Log.LogWarning($"Could not update SpinShare playlist {Name} (invalid URL)");
                NotificationSystemGUI.AddMessage($"Could not update SpinShare playlist <b>{Name}</b> (invalid URL)");
                return;
            }

            UpdatePlaylistChartCountText();
            UpdateModifyButtonText();
            UpdateMissingCharts();
            Save();
            
            Plugin.Log.LogInfo($"Updated SpinShare playlist {Name}");
            NotificationSystemGUI.AddMessage($"Updated SpinShare playlist <b>{Name}</b>!");
            
            Playlist? previouslySelectedPlaylist = SpinListPanel.SelectedPlaylist;
            if (previouslySelectedPlaylist?.FilePath == FilePath)
            {
                OnPlaylistSelected(true);
            }
        }
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