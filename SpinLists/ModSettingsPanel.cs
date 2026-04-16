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
    private static CustomButton? _downloadPlaylistButton;
    private static CustomButton? _downloadUsersChartsButton;
    
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
        
        UIHelper.CreateButton(modGroup, "OpenRepositoryButton", $"{TRANSLATION_PREFIX}GitHubButtonText", () =>
        {
            Application.OpenURL($"https://github.com/TheBlackParrot/{nameof(SpinLists)}/releases/latest");
        });
        
        UIHelper.CreateSectionHeader(modGroup, "ModGroupHeader", $"{TRANSLATION_PREFIX}ThresholdSettings", false);
        
        #region threshold settings
        UIHelper.CreateSmallToggle(modGroup,
            nameof(AlsoApplyThresholdsToPlaylists), $"{TRANSLATION_PREFIX}{nameof(AlsoApplyThresholdsToPlaylists)}",
            AlsoApplyThresholdsToPlaylists.Value, value =>
            {
                AlsoApplyThresholdsToPlaylists.Value = value;
            });
        
        CustomGroup minimumDifficultyThresholdGroup = UIHelper.CreateGroup(modGroup, "MinimumDifficultyThresholdGroup");
        UIHelper.CreateSmallMultiChoiceButton(minimumDifficultyThresholdGroup, nameof(MinimumDifficultyThreshold),
            $"{TRANSLATION_PREFIX}{nameof(MinimumDifficultyThreshold)}",
            (int)MinimumDifficultyThreshold.Value, (value) =>
            {
                MinimumDifficultyThreshold.Value = (uint)value;
            },
            () => new IntRange(0, 101),
            v => v.ToString());
        
        CustomGroup maximumDifficultyThresholdGroup = UIHelper.CreateGroup(modGroup, "MaximumDifficultyThresholdGroup");
        UIHelper.CreateSmallMultiChoiceButton(maximumDifficultyThresholdGroup, nameof(MaximumDifficultyThreshold),
            $"{TRANSLATION_PREFIX}{nameof(MaximumDifficultyThreshold)}",
            (int)MaximumDifficultyThreshold.Value, (value) =>
            {
                MaximumDifficultyThreshold.Value = (uint)value;
            },
            () => new IntRange(0, 101),
            v => v.ToString());
        
        UIHelper.CreateLabel(modGroup, "DifficultyThresholdExplainer", $"{TRANSLATION_PREFIX}DifficultyThresholdExplainer");
        #endregion
        
        UIHelper.CreateSectionHeader(modGroup, "ModGroupHeader", $"{TRANSLATION_PREFIX}PlaylistDownloading", false);
        
        #region download buttons
        UIHelper.CreateLabel(modGroup, "InputFieldExplainer", $"{TRANSLATION_PREFIX}InputFieldExplainer");

        _playlistIdInputField = UIHelper.CreateInputField(modGroup, "PlaylistIDInputField", (_, _) => { });
        
        CustomGroup downloadPlaylistButtonsGroup = UIHelper.CreateGroup(modGroup, "DownloadPlaylistButtons");
        downloadPlaylistButtonsGroup.LayoutDirection = Axis.Horizontal;
        _downloadPlaylistButton = UIHelper.CreateButton(downloadPlaylistButtonsGroup,
            "DownloadPlaylistButton", $"{TRANSLATION_PREFIX}GetPlaylist",
            async void () =>
            {
                try
                {
                    Utils.SetButtonAvailable(ref _downloadPlaylistButton, false, $"{TRANSLATION_PREFIX}Downloading");
                    await DownloadSpinSharePlaylist();
                    Utils.SetButtonAvailable(ref _downloadPlaylistButton, true, $"{TRANSLATION_PREFIX}GetPlaylist");
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        _downloadUsersChartsButton = UIHelper.CreateButton(downloadPlaylistButtonsGroup,
            "DownloadUsersChartsButton", $"{TRANSLATION_PREFIX}GetUsersCharts",
            async void () =>
            {
                try
                {
                    Utils.SetButtonAvailable(ref _downloadUsersChartsButton, false, $"{TRANSLATION_PREFIX}Downloading");
                    await DownloadSpinShareUsersCharts();
                    Utils.SetButtonAvailable(ref _downloadUsersChartsButton, true, $"{TRANSLATION_PREFIX}GetUsersCharts");
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        #endregion
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