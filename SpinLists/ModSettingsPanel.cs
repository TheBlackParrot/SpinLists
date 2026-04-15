using System;
using System.Linq;
using System.Threading.Tasks;
using SpinCore.UI;
using SpinLists.Classes;
using SpinLists.UI;
using UnityEngine;

namespace SpinLists;

public partial class Plugin
{
    private static CustomInputField? _playlistIdInputField;
    
    private static void CreateModPage()
    {
        CustomPage rootModPage = UIHelper.CreateCustomPage("ModSettings");
        rootModPage.OnPageLoad += RootModPageOnPageLoad;
        
        UIHelper.RegisterMenuInModSettingsRoot($"{TRANSLATION_PREFIX}{nameof(SpinLists)}", rootModPage);
    }

    private static void RootModPageOnPageLoad(Transform rootModPageTransform)
    {
        CustomGroup modGroup = UIHelper.CreateGroup(rootModPageTransform, nameof(SpinLists));
        UIHelper.CreateSectionHeader(modGroup, "ModGroupHeader", $"{TRANSLATION_PREFIX}{nameof(SpinLists)}", false);

        _playlistIdInputField = UIHelper.CreateInputField(modGroup, "PlaylistIDInputField", (_, _) => { });
        
        CustomGroup downloadPlaylistButtonsGroup = UIHelper.CreateGroup(modGroup, "DownloadPlaylistButtons");
        downloadPlaylistButtonsGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateButton(downloadPlaylistButtonsGroup, "DownloadPlaylistButton", $"{TRANSLATION_PREFIX}GetPlaylist",
            async void () =>
            {
                try
                {
                    await DownloadSpinSharePlaylist();
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        UIHelper.CreateButton(downloadPlaylistButtonsGroup, "DownloadUsersChartsButton", $"{TRANSLATION_PREFIX}GetUsersCharts",
            async void () =>
            {
                try
                {
                    await DownloadSpinShareUsersCharts();
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
    }

    private static async Task DownloadSpinSharePlaylist()
    {
        string rawInput = _playlistIdInputField!.InputField.text;

        if (!uint.TryParse(rawInput, out uint playlistId))
        {
            if (!rawInput.Contains("playlist"))
            {
                NotificationSystemGUI.AddMessage("Invalid playlist URL", 5f);
                return;
            }
                        
            Uri uri = new(rawInput);
            if (!uint.TryParse(uri.Segments.Last(), out playlistId))
            {
                NotificationSystemGUI.AddMessage("Could not determine playlist ID number", 5f);
                return;
            }
        }

        Playlist? playlist = await Utils.DownloadSpinSharePlaylist(playlistId);
        if (playlist == null)
        {
            NotificationSystemGUI.AddMessage("Playlist not found", 5f);
            return;
        }

        playlist.Save();
        NotificationSystemGUI.AddMessage($"Downloaded playlist <b>{playlist.Name}</b>!", 5f);
        await SpinListPanel.ReloadPlaylists();
    }

    private static async Task DownloadSpinShareUsersCharts()
    {
        string rawInput = _playlistIdInputField!.InputField.text;

        if (!uint.TryParse(rawInput, out uint userId))
        {
            if (!rawInput.Contains("user"))
            {
                NotificationSystemGUI.AddMessage("Invalid user URL", 5f);
                return;
            }
                        
            Uri uri = new(rawInput);
            if (!uint.TryParse(uri.Segments.Last(), out userId))
            {
                NotificationSystemGUI.AddMessage("Could not determine user ID number", 5f);
                return;
            }
        }

        Playlist? playlist = await Utils.DownloadSpinShareUserChartsAsPlaylist(userId);
        if (playlist == null)
        {
            NotificationSystemGUI.AddMessage("Playlist not found", 5f);
            return;
        }

        playlist.Save();
        NotificationSystemGUI.AddMessage($"Downloaded playlist <b>{playlist.Name}</b>!", 5f);
        await SpinListPanel.ReloadPlaylists();
    }
}