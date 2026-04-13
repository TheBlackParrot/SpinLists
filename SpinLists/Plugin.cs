using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpinCore.Translation;
using SpinLists.UI;

namespace SpinLists;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("srxd.raoul1808.spincore", "1.1.2")]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    internal const string TRANSLATION_PREFIX = $"{nameof(SpinLists)}_";
    private static readonly Harmony HarmonyInstance = new(MyPluginInfo.PLUGIN_GUID);

    private static readonly Dictionary<string, string> TranslationReferences = new()
    {
        {$"{TRANSLATION_PREFIX}{nameof(SpinLists)}", "SpinLists"},
        {$"{TRANSLATION_PREFIX}View", "View"},
        {$"{TRANSLATION_PREFIX}Add", "Add Chart"},
        {$"{TRANSLATION_PREFIX}Remove", "Remove Chart"},
        {$"{TRANSLATION_PREFIX}{nameof(SuggestedDifficultyMode)}", "Use suggested difficulties"},
        {$"{TRANSLATION_PREFIX}Playlists", "Playlists"}
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
    }

    private void OnEnable()
    {
        HarmonyInstance.PatchAll();
        SpinListPanel.CreateSpinListPanel();
    }
    public void OnDisable()
    {
        HarmonyInstance.UnpatchSelf();
    }
}