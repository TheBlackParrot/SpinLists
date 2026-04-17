using System;
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
    private static CustomButton? _downloadLikedChartsButton;
    private static CustomButton? _downloadDislikedChartsButton;
    
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
        
        UIHelper.CreateSectionHeader(modGroup, "ModGroupHeader", $"{TRANSLATION_PREFIX}DownloadSettings", false);
        
        #region LockSpinSharePlaylists
        UIHelper.CreateSmallToggle(modGroup,
            nameof(LockSpinSharePlaylists), $"{TRANSLATION_PREFIX}{nameof(LockSpinSharePlaylists)}",
            LockSpinSharePlaylists.Value, value =>
            {
                LockSpinSharePlaylists.Value = value;
            });
        #endregion
        
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
        
        #region download buttons (row 1)
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
        
        #region download buttons (row 2)
        CustomGroup downloadPlaylistButtonsGroup2 = UIHelper.CreateGroup(modGroup, "DownloadPlaylistButtons");
        downloadPlaylistButtonsGroup2.LayoutDirection = Axis.Horizontal;
        _downloadLikedChartsButton = UIHelper.CreateButton(downloadPlaylistButtonsGroup2,
            "DownloadLikedChartsButton", $"{TRANSLATION_PREFIX}GetLikedCharts",
            async void () =>
            {
                try
                {
                    Utils.SetButtonAvailable(ref _downloadLikedChartsButton, false, $"{TRANSLATION_PREFIX}Downloading");
                    await DownloadSpinShareUsersReviews(true);
                    Utils.SetButtonAvailable(ref _downloadLikedChartsButton, true, $"{TRANSLATION_PREFIX}GetLikedCharts");
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        _downloadDislikedChartsButton = UIHelper.CreateButton(downloadPlaylistButtonsGroup2,
            "DownloadDislikedChartsButton", $"{TRANSLATION_PREFIX}GetDislikedCharts",
            async void () =>
            {
                try
                {
                    Utils.SetButtonAvailable(ref _downloadDislikedChartsButton, false, $"{TRANSLATION_PREFIX}Downloading");
                    await DownloadSpinShareUsersReviews(false);
                    Utils.SetButtonAvailable(ref _downloadDislikedChartsButton, true, $"{TRANSLATION_PREFIX}GetDislikedCharts");
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        
        #endregion
    }

    private static uint GetIdNumberFromString(string rawInput)
    {
        if (uint.TryParse(rawInput, out uint id))
        {
            return id;
        }
                        
        Uri uri = new(rawInput);
        if (uint.TryParse(uri.Segments[3].Replace("/", string.Empty), out id))
        {
            return id;
        }
        
        NotificationSystemGUI.AddMessage("Could not parse ID number from URL", 5f);
        throw new InvalidOperationException("Could not parse ID number from URL");
    }

    private static async Task DownloadSpinSharePlaylist()
    {
        uint playlistId;
        try
        {
            playlistId = GetIdNumberFromString(_playlistIdInputField!.InputField.text);
        }
        catch (Exception e)
        {
            NotificationSystemGUI.AddMessage("Could not parse ID number", 5f);
            Log.LogWarning(e);
            return;
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
        uint userId;
        try
        {
            userId = GetIdNumberFromString(_playlistIdInputField!.InputField.text);
        }
        catch (Exception e)
        {
            NotificationSystemGUI.AddMessage("Could not parse ID number", 5f);
            Log.LogWarning(e);
            return;
        }

        Playlist? playlist = await Utils.DownloadSpinShareUserChartsAsPlaylist(userId);
        if (playlist == null)
        {
            NotificationSystemGUI.AddMessage("User not found", 5f);
            return;
        }

        playlist.Save();
        NotificationSystemGUI.AddMessage($"Downloaded playlist <b>{playlist.Name}</b>!", 5f);
        await SpinListPanel.ReloadPlaylists();
    }

    private static async Task DownloadSpinShareUsersReviews(bool recommended)
    {
        uint userId;
        try
        {
            userId = GetIdNumberFromString(_playlistIdInputField!.InputField.text);
        }
        catch (Exception e)
        {
            NotificationSystemGUI.AddMessage("Could not parse ID number", 5f);
            Log.LogWarning(e);
            return;
        }

        Playlist? playlist = await Utils.DownloadSpinShareUserReviewsAsPlaylist(userId, recommended);
        if (playlist == null)
        {
            NotificationSystemGUI.AddMessage("User not found or has no reviews", 5f);
            return;
        }

        playlist.Save();
        NotificationSystemGUI.AddMessage($"Downloaded playlist <b>{playlist.Name}</b>!", 5f);
        await SpinListPanel.ReloadPlaylists();
    }
}