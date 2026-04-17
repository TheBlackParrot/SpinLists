using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using SpinCore.Translation;
using SpinLists.Classes;
using SpinLists.UI;
using SpinShareLib;
using UnityEngine;

namespace SpinLists;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("srxd.raoul1808.spincore", "1.1.2")]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    internal const string TRANSLATION_PREFIX = $"{nameof(SpinLists)}_";
    private static readonly Harmony HarmonyInstance = new(MyPluginInfo.PLUGIN_GUID);
    internal static readonly SSAPI SpinShare = new();

    private static readonly Dictionary<string, string> TranslationReferences = new()
    {
        {$"{TRANSLATION_PREFIX}{nameof(SpinLists)}", "SpinLists"},
        {$"{TRANSLATION_PREFIX}View", "View"},
        {$"{TRANSLATION_PREFIX}Back", "Back"},
        {$"{TRANSLATION_PREFIX}Add", "Add Chart"},
        {$"{TRANSLATION_PREFIX}Remove", "Remove Chart"},
        {$"{TRANSLATION_PREFIX}{nameof(SuggestedDifficultyMode)}", "Use suggested difficulties"},
        {$"{TRANSLATION_PREFIX}Playlists", "Playlists"},
        {$"{TRANSLATION_PREFIX}ReloadPlaylists", "Reload Playlists"},
        {$"{TRANSLATION_PREFIX}DownloadMissing", "Download Missing Charts"},
        {$"{TRANSLATION_PREFIX}CreateNew", "Create New Playlist"},
        {$"{TRANSLATION_PREFIX}Create", "Create"},
        {$"{TRANSLATION_PREFIX}GetPlaylist", "Get Playlist"},
        {$"{TRANSLATION_PREFIX}GetUsersCharts", "Get User's Charts"},
        {$"{TRANSLATION_PREFIX}Downloading", "Downloading..."},
        {$"{TRANSLATION_PREFIX}GitHubButtonText", $"{nameof(SpinLists)} Releases (GitHub)"},
        {$"{TRANSLATION_PREFIX}PlaylistDownloading", "Playlist Downloading"},
        {$"{TRANSLATION_PREFIX}InputFieldExplainer", "Enter a URL or numeric ID into the text field below:"},
        {$"{TRANSLATION_PREFIX}{nameof(PlaylistSortMethod)}", "Sort method"},
        {$"{TRANSLATION_PREFIX}Loading", "Loading..."},
        {$"{TRANSLATION_PREFIX}UpdatePlaylist", "Update Playlist"},
        {$"{TRANSLATION_PREFIX}Updating", "Updating..."},
        {$"{TRANSLATION_PREFIX}{nameof(MinimumDifficultyThreshold)}", "Minimum difficulty"},
        {$"{TRANSLATION_PREFIX}{nameof(MaximumDifficultyThreshold)}", "Maximum difficulty"},
        {$"{TRANSLATION_PREFIX}DifficultyThresholdExplainer", "(Set to 0 to disable difficulty thresholds)"},
        {$"{TRANSLATION_PREFIX}{nameof(AlsoApplyThresholdsToPlaylists)}", "Apply threshold settings to SpinShare playlists"},
        {$"{TRANSLATION_PREFIX}ThresholdSettings", "Difficulty Filtering Settings"},
        {$"{TRANSLATION_PREFIX}DownloadSettings", "Download Settings"},
        {$"{TRANSLATION_PREFIX}{nameof(LockSpinSharePlaylists)}", "Lock SpinShare playlists"}
    };

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo("Plugin loaded");

        // trying a slightly cleaner approach for translation keys
        foreach (KeyValuePair<string, string> entry in TranslationReferences)
        {
            TranslationHelper.AddTranslation(entry.Key, entry.Value);   
        }
        
        RegisterConfigEntries();
        CreateModPage();
    }

    private void OnEnable()
    {
        HarmonyInstance.PatchAll();
        MainCamera.OnCurrentCameraChanged += CheckForUpdates;
        
        SpinListPanel.CreateSpinListPanel();
    }
    public void OnDisable()
    {
        HarmonyInstance.UnpatchSelf();
    }
    
    private static void CheckForUpdates(Camera _)
    {
        MainCamera.OnCurrentCameraChanged -= CheckForUpdates;
        
        Task.Run(async () =>
        {
            try
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("User-Agent",
                    $"{nameof(SpinLists)}/{MyPluginInfo.PLUGIN_VERSION} (https://github.com/TheBlackParrot/SpinLists)");
                HttpResponseMessage responseMessage =
                    await httpClient.GetAsync("https://api.github.com/repos/TheBlackParrot/SpinLists/releases/latest");
                responseMessage.EnsureSuccessStatusCode();
                string json = await responseMessage.Content.ReadAsStringAsync();

                ReleaseVersion? releaseVersion = JsonConvert.DeserializeObject<ReleaseVersion>(json);
                if (releaseVersion == null)
                {
                    Log.LogInfo("Could not get newest release version information");
                    return;
                }

                if (releaseVersion.Version == null)
                {
                    Log.LogInfo("Could not get newest release version information");
                    return;
                }

                if (releaseVersion.IsPreRelease)
                {
                    Log.LogInfo("Newest release version is a pre-release");
                    return;
                }

                Version currentVersion = new(MyPluginInfo.PLUGIN_VERSION);
                Version latestVersion = new(releaseVersion.Version);
#if DEBUG
                // just so we can see the notifications
                if (currentVersion != latestVersion)
#else
            if (currentVersion < latestVersion)
#endif
                {
                    Log.LogMessage(
                        $"{nameof(SpinLists)} is out of date! (using v{currentVersion}, latest is v{latestVersion})");

                    await Awaitable.MainThreadAsync();
                    NotificationSystemGUI.AddMessage(
                        $"<b>{nameof(SpinLists)}</b> has an update available! <alpha=#AA>(v{currentVersion} <alpha=#77>-> <alpha=#AA>v{latestVersion})\n<alpha=#FF><size=67%>See the shortcut button in the Mod Settings page to grab the latest update.",
                        15f);
                }
                else
                {
                    Log.LogMessage($"{nameof(SpinLists)} is up to date!");
                }
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        });
    }

    public static void DebugMessage(string _)
    {
#if DEBUG
        Log.LogInfo(_);
#endif
    }
}