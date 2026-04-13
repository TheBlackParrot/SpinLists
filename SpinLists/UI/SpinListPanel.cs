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

namespace SpinLists.UI;

internal static class SpinListPanel
{
    internal static readonly List<Playlist> Playlists = [];
    internal static CustomSidePanel SidePanel = null!;
    internal static readonly string PlaylistsPath = Path.GetFullPath($"{Directory.GetParent(AssetBundleSystem.CUSTOM_DATA_PATH)}\\SpinLists");
    internal static CustomGroup DisplayGroup = null!;

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
        Plugin.Log.LogInfo($"Created Playlists folder: {PlaylistsPath}");
    }

    private static void OnSidePanelLoaded(Transform panelTransform)
    {
        CreatePlaylistsFolder();
        
        DisplayGroup = UIHelper.CreateGroup(panelTransform, "PlaylistEntryDisplay", Axis.Horizontal);

        UIHelper.CreateSmallToggle(panelTransform,
            nameof(Plugin.SuggestedDifficultyMode),
            $"{Plugin.TRANSLATION_PREFIX}{nameof(Plugin.SuggestedDifficultyMode)}",
            Plugin.SuggestedDifficultyMode.Value, value =>
            {
                Plugin.SuggestedDifficultyMode.Value = value;
            });

        UIHelper.CreateSectionHeader(panelTransform, "SpinListHeader", $"{Plugin.TRANSLATION_PREFIX}Playlists", false);

        Task.Run(async () =>
        {
            foreach (string playlistFile in Directory.GetFiles(PlaylistsPath, "*.json"))
            {
                try
                {
                    string playlistData = new UTF8Encoding(false).GetString(File.ReadAllBytes(playlistFile));
                
                    Playlist playlist = JsonConvert.DeserializeObject<Playlist>(playlistData) ?? throw new InvalidOperationException();
                    playlist.FilePath = playlistFile;
                    Playlists.Add(playlist);

                    string playlistFileNoExtension = Path.GetFileNameWithoutExtension(playlistFile);

                    string? coverPath = null;
                    Plugin.Log.LogInfo($"Trying cover path {PlaylistsPath}\\{playlistFileNoExtension}.[png/jpg]");
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
                                    await playlist.CreatePlaylistRow();
                                }
                            }
                            
                            await playlist.CreatePlaylistRow(texture);
                            
                            done = true;
                        };

                        while (!done)
                        {
                            await Awaitable.EndOfFrameAsync();
                        }
                    }
                    else
                    {
                        await playlist.CreatePlaylistRow();
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Failed to load playlist file: {playlistFile}");
                    Plugin.Log.LogWarning(e);
                }
            }
        });
        
        Plugin.Log.LogInfo("Side panel loaded");
    }
}