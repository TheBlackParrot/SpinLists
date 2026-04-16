using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpinCore.UI;
using SpinLists.Classes;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using JsonCommentHandling = System.Text.Json.JsonCommentHandling;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace SpinLists.UI;

internal static class SpinListPanel
{
    internal static readonly List<Playlist> Playlists = [];
    internal static CustomSidePanel? SidePanel;
    internal static readonly string PlaylistsPath = Path.GetFullPath($"{Directory.GetParent(AssetBundleSystem.CUSTOM_DATA_PATH)}\\SpinLists");
    internal static CustomGroup DisplayGroup = null!;
    internal static CustomSectionHeader ListHeader = null!;
    private static CustomButton? _reloadPlaylistsButton;

    internal static Playlist? SelectedPlaylist
    {
        get;
        set
        {
            if (field?.ActivateButton != null)
            {
                field.ActivateButton.TextTranslationKey = $"{Plugin.TRANSLATION_PREFIX}View";
            }
            if (value?.ActivateButton != null)
            {
                value.ActivateButton.TextTranslationKey = $"{Plugin.TRANSLATION_PREFIX}Back";
            }

            field = value;
        }
    }

    internal static void CreateSpinListPanel()
    {
        Task.Run(async () =>
        {
            await Awaitable.MainThreadAsync();

            Sprite? sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "Playlist");
            
            SidePanel = UIHelper.CreateSidePanel(nameof(SpinListPanel), $"{Plugin.TRANSLATION_PREFIX}{nameof(SpinLists)}", sprite);
            SidePanel.OnSidePanelLoaded += OnSidePanelLoaded;
        });
    }

    private static void CreatePlaylistsFolder()
    {
        if (Directory.Exists(PlaylistsPath))
        {
            return;
        }
        
        Directory.CreateDirectory(PlaylistsPath);
        Plugin.DebugMessage($"Created Playlists folder: {PlaylistsPath}");
    }

    private static async Task FinalizePlaylist(Playlist playlist, Playlist? previouslySelectedPlaylist = null, Texture2D? texture = null)
    {
        playlist.UpdateMissingCharts();
        
        await playlist.CreatePlaylistRow(texture);
        if (previouslySelectedPlaylist?.FilePath == playlist.FilePath)
        {
            playlist.OnPlaylistSelected();
        }
    }

    internal static async Task ReloadPlaylists()
    {
        if (SidePanel == null)
        {
            return;
        }
        
        Utils.SetButtonAvailable(ref _reloadPlaylistsButton, false, $"{Plugin.TRANSLATION_PREFIX}Loading");
        
        Playlist? previouslySelectedPlaylist = SelectedPlaylist;
        foreach (Playlist playlist in Playlists)
        {
            playlist.Destroy();
        }
        Playlists.Clear();

        Dictionary<string, string> forceCoverFetches = [];
        
        foreach (string playlistFile in Directory.GetFiles(PlaylistsPath, "*.json"))
        {
            try
            {
                string playlistData = new UTF8Encoding(false).GetString(File.ReadAllBytes(playlistFile));
                
                Playlist? playlist = JsonConvert.DeserializeObject<Playlist>(playlistData);
                if (playlist == null)
                {
                    throw new InvalidOperationException();
                }
                
                playlist.FilePath = playlistFile;
                Playlists.Add(playlist);
            }
            catch (Exception e)
            {
                if (e.InnerException is NotSupportedException)
                {
                    // may be a SpinShare-formatted playlist, try that next
                    Plugin.Log.LogWarning($"Attempting conversion of SpinShare-formatted playlist {Path.GetFileNameWithoutExtension(playlistFile)}");
                    
                    try
                    {
                        string playlistData = new UTF8Encoding(false).GetString(File.ReadAllBytes(playlistFile));
                        
                        // handling SpinShare playlists similarly to how SpinShareLib handles them. Newtonsoft's gets cranky
                        JsonSerializerOptions options = new()
                        {
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true,
                            IncludeFields = true,
                            Converters = { new SpinShareLib.DateTimeParse() }
                        };
                        SpinShareLib.Types.Content<SpinShareLib.Types.Playlist>? spinPlaylist = 
                            System.Text.Json.JsonSerializer.Deserialize<SpinShareLib.Types.Content<SpinShareLib.Types.Playlist>>(playlistData, options);

                        if (spinPlaylist == null)
                        {
                            throw new NullReferenceException();
                        }

                        Playlist playlist = new(spinPlaylist.data)
                        {
                            FilePath = playlistFile
                        };
                        Playlists.Add(playlist);
                        playlist.Save();
                        
                        forceCoverFetches.Add(playlistFile, spinPlaylist.data.cover);
                        
                        Plugin.Log.LogWarning($"Converted SpinShare-formatted playlist {Path.GetFileNameWithoutExtension(playlistFile)}");
                    }
                    catch (Exception e2)
                    {
                        Plugin.Log.LogWarning($"Failed to load playlist file (as SpinShare-formatted playlist): {playlistFile}");
                        Plugin.Log.LogWarning(e2);
                    }
                }
                else
                {
                    Plugin.Log.LogWarning($"Failed to load playlist file: {playlistFile}");
                    Plugin.Log.LogWarning(e);
                }
            }
        }
        
        Playlists.Sort((x, y) =>
            {
                return Plugin.PlaylistSortMethod.Value switch
                {
                    PlaylistSortMethod.Name => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase),
                    PlaylistSortMethod.Author => string.Compare(x.Author, y.Author, StringComparison.OrdinalIgnoreCase),
                    PlaylistSortMethod.CreationTime => File.GetCreationTime(y.FilePath).CompareTo(File.GetCreationTime(x.FilePath)),
                    PlaylistSortMethod.ModificationTime => File.GetLastWriteTime(y.FilePath).CompareTo(File.GetLastWriteTime(x.FilePath)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        );

        // fml i wish i had tuples in 472. ugly af
        foreach (KeyValuePair<string, string> keyValuePair in forceCoverFetches)
        {
            string filename = keyValuePair.Key;
            string coverURL = keyValuePair.Value;
            string playlistFileNoExtension = Path.GetFileNameWithoutExtension(filename);
            
            if (File.Exists(Path.Combine(PlaylistsPath, $"{playlistFileNoExtension}.jpg"))
                || File.Exists(Path.Combine(PlaylistsPath, $"{playlistFileNoExtension}.png")))
            {
                continue;
            }
            
            Plugin.Log.LogInfo($"Getting playlist cover image for {playlistFileNoExtension}");
            
            try
            {
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(coverURL);
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
                    throw new NullReferenceException();
                }
                
                Texture2D finalCover = Utils.FixPlaylistCoverImage(texture);
                File.WriteAllBytes(Path.Combine(PlaylistsPath, $"{playlistFileNoExtension}.jpg"), finalCover.EncodeToJPG());
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to get SpinShare cover {coverURL}");
                Plugin.Log.LogWarning(e);
            }
        }

        foreach (Playlist playlist in Playlists) {
            try {
                string playlistFileNoExtension = Path.GetFileNameWithoutExtension(playlist.FilePath);

                string? coverPath = null;
                Plugin.DebugMessage($"Trying cover path {PlaylistsPath}\\{playlistFileNoExtension}.[png/jpg]");
                if (File.Exists($"{PlaylistsPath}\\{playlistFileNoExtension}.png"))
                {
                    coverPath = $"file://{PlaylistsPath}\\{playlistFileNoExtension}.png";
                }
                else if (File.Exists($"{PlaylistsPath}\\{playlistFileNoExtension}.jpg"))
                {
                    coverPath = $"file://{PlaylistsPath}\\{playlistFileNoExtension}.jpg";
                }

                if (coverPath != null)
                {
                    await Awaitable.MainThreadAsync();
                        
                    // web requests to file:// are just easier and i'm all about easy
                    UnityWebRequest request = UnityWebRequestTexture.GetTexture(coverPath);
                    UnityWebRequestAsyncOperation response = request.SendWebRequest();

                    bool done = false;
                    response.completed += async _ =>
                    {
                        await Awaitable.MainThreadAsync();

                        Texture2D? texture = null;
                        try
                        {
                            texture = DownloadHandlerTexture.GetContent(request);
                        }
                        catch (Exception e)
                        {
                            if (e is not (InvalidOperationException or HttpRequestException))
                            {
                                await FinalizePlaylist(playlist, previouslySelectedPlaylist);
                            }
                        }
                            
                        await FinalizePlaylist(playlist, previouslySelectedPlaylist, texture);
                            
                        done = true;
                    };

                    while (!done)
                    {
                        await Awaitable.EndOfFrameAsync();
                    }
                }
                else
                {
                    await FinalizePlaylist(playlist, previouslySelectedPlaylist);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to load playlist file: {playlist.FilePath}");
                Plugin.Log.LogWarning(e);
            }
        }
        
        Utils.SetButtonAvailable(ref _reloadPlaylistsButton, true, $"{Plugin.TRANSLATION_PREFIX}ReloadPlaylists");
    }

    private static void OnSidePanelLoaded(Transform panelTransform)
    {
        CreatePlaylistsFolder();
        
        DisplayGroup = UIHelper.CreateGroup(panelTransform, "PlaylistEntryDisplay", Axis.Horizontal);

        UIHelper.CreateSmallMultiChoiceButton(panelTransform,
            nameof(Plugin.PlaylistSortMethod),
            $"{Plugin.TRANSLATION_PREFIX}{nameof(PlaylistSortMethod)}",
            Plugin.PlaylistSortMethod.Value, value =>
            {
                Plugin.PlaylistSortMethod.Value = value;
            });
        UIHelper.CreateSmallToggle(panelTransform,
            nameof(Plugin.SuggestedDifficultyMode),
            $"{Plugin.TRANSLATION_PREFIX}{nameof(Plugin.SuggestedDifficultyMode)}",
            Plugin.SuggestedDifficultyMode.Value, value =>
            {
                Plugin.SuggestedDifficultyMode.Value = value;
            });
        
        _reloadPlaylistsButton = UIHelper.CreateButton(panelTransform, "ReloadPlaylistsButton",
            $"{Plugin.TRANSLATION_PREFIX}ReloadPlaylists", async void () =>
        {
            try
            {
                await ReloadPlaylists();
            }
            catch (Exception)
            {
                // ignore, exceptions are handled in ReloadPlaylists
            }
        });
        
        UIHelper.CreateSectionHeader(panelTransform, "SpinListHeader", $"{Plugin.TRANSLATION_PREFIX}CreateNew", false);

        CustomGroup createNewPlaylistGroup = UIHelper.CreateGroup(panelTransform, "CreateNewPlaylistGroup", Axis.Horizontal);
        CustomGroup createNewPlaylistNameFieldGroup = UIHelper.CreateGroup(createNewPlaylistGroup, "CreateNewPlaylistNameFieldGroup", Axis.Horizontal);
        CustomInputField createNewPlaylistNameField =
            UIHelper.CreateInputField(createNewPlaylistNameFieldGroup, "CreateNewPlaylistNameField", (_, _) => { });
        CustomButton createButton = UIHelper.CreateButton(createNewPlaylistGroup, "CreateNewPlaylistButton", $"{Plugin.TRANSLATION_PREFIX}Create",
            async void () =>
            {
                try
                {
                    Playlist playlist = new()
                    {
                        Name = createNewPlaylistNameField.InputField.text,
                        Author = PlayerServiceManager.Instance.GetDisplayName()
                    };
                    Playlists.Add(playlist);
                    playlist.Save();
                    
                    await playlist.CreatePlaylistRow(null, true);
                    
                    NotificationSystemGUI.AddMessage($"Created playlist <b>{playlist.Name}</b> <i>({Path.GetFileName(playlist.FilePath)})</i>", 5f);
                    createNewPlaylistNameField.InputField.text = "";
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError(e);
                }
            });
        LayoutElement createButtonLayoutElement = createButton.Transform.GetComponent<LayoutElement>();
        createButtonLayoutElement.preferredWidth = 0;
        createButtonLayoutElement.preferredHeight = 60;

        ListHeader = UIHelper.CreateSectionHeader(panelTransform, "SpinListHeader", $"{Plugin.TRANSLATION_PREFIX}Playlists", false);

        _ = ReloadPlaylists();
        
        Plugin.DebugMessage("Side panel loaded");
    }
}