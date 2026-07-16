using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using PhoA_AP_client.util.DataClasses;
using UnityEngine;

namespace PhoA_AP_client.patches;

[HarmonyPatch]
internal sealed class SaveWarpPatches
{
    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _savepointData = new();
    private static string _currentRegionCmd;
    private static string _currentLocationCmd;

    [HarmonyPatch(typeof(DirectorLogic), "Awake")]
    [HarmonyPostfix] // Patch to add save warp option to menu
    private static void DirectorLogicAwakePostfix(DirectorLogic __instance)
    {
        LoadSavepointData();

        string[] original = Traverse.Create(__instance).Field<string[]>("_options_arr_main").Value;
        string[] expanded = new string[original.Length + 1];

        expanded[0] = original[0];
        expanded[1] = "__OPT_WARP";
        Array.Copy(original, 1, expanded, 2, original.Length - 1);

        Traverse.Create(__instance).Field<string[]>("_options_arr_main").Value = expanded;
    }

    [HarmonyPatch(typeof(DB), "_LoadTranslateMap")]
    [HarmonyPostfix] // Patch to add entries to DB.TRANSLATE_map
    private static void LoadTranslateMapPostfix(DirectorLogic __instance)
    {
        DB.TRANSLATE_map["__OPT_WARP"] = "Warp to Savepoint";
        DB.TRANSLATE_map["__OPT_WARP_return"] = "Go Back";
        DB.TRANSLATE_map["__OPT_WARP_select_region"] = "Select Region";
        DB.TRANSLATE_map["__OPT_WARP_select_location"] = "Select Location";
        DB.TRANSLATE_map["__OPT_WARP_select_savepoint"] = "Select Savepoint";

        foreach (var kvpRegions in _savepointData)
        {
            DB.TRANSLATE_map[$"__OPT_WARP_REGION_{kvpRegions.Key.Replace(' ', '_').ToLower()}"] = kvpRegions.Key;
            DB.TRANSLATE_map[$"__OPT_WARP_REGION_{kvpRegions.Key.Replace(' ', '_').ToLower()}_return"] = "Go Back";

            foreach (var kvpLocations in kvpRegions.Value)
            {
                DB.TRANSLATE_map[$"__OPT_WARP_LOCATION_{kvpLocations.Key.Replace(' ', '_').ToLower()}"] =
                    kvpLocations.Key;

                foreach (var kvpSavepoints in kvpLocations.Value)
                    DB.TRANSLATE_map[$"__OPT_WARP_SAVE_{kvpSavepoints.Key.Replace(' ', '_').ToLower()}"] =
                        kvpSavepoints.Key;
            }
        }
    }

    [HarmonyPatch(typeof(DirectorLogic), "_OptionsExecuteCmd")]
    [HarmonyPrefix] // Patch to add actions to new menu options
    private static bool OptionsExecuteCmdPrefix(DirectorLogic __instance, string cmd_option)
    {
        PhoaAPClient.Logger.LogDebug(cmd_option);
        if (cmd_option is "__OPT_WARP" or "__OPT_WARP_return")
        {
            string[] regionArray = _savepointData.Keys
                .Select(r => $"__OPT_WARP_REGION_{r.Replace(' ', '_').ToLower()}")
                .ToArray();
            string[] optionsArray = new string[regionArray.Length + 3];
            optionsArray[0] = "__OPT_WARP_select_region";
            Array.Copy(regionArray, 0, optionsArray, 1, regionArray.Length);
            optionsArray[optionsArray.Length - 2] = optionsArray[optionsArray.Length - 1] = "__OPT_CONTROL_return";

            foreach (var thing in optionsArray)
                PhoaAPClient.Logger.LogDebug(thing);
            InsertMenuData(__instance, optionsArray);

            int soundId = cmd_option.EndsWith("_return") ? 123 : 122;
            PT2.sound_g.PlayGlobalCommonSfx(soundId, 1f, 1.25f, 1);
            return false;
        }

        if (cmd_option.StartsWith("__OPT_WARP_REGION_"))
        {
            _currentRegionCmd = cmd_option.Replace("_return", "");
            string[] locationArray = _savepointData[DB.TRANSLATE_map[_currentRegionCmd]].Keys
                .Select(l => $"__OPT_WARP_LOCATION_{l.Replace(' ', '_').ToLower()}")
                .ToArray();
            string[] optionsArray = new string[locationArray.Length + 3];
            optionsArray[0] = "__OPT_WARP_select_location";
            Array.Copy(locationArray, 0, optionsArray, 1, locationArray.Length);
            optionsArray[optionsArray.Length - 2] = optionsArray[optionsArray.Length - 1] = "__OPT_WARP_return";

            InsertMenuData(__instance, optionsArray);

            int soundId = cmd_option.EndsWith("_return") ? 123 : 122;
            PT2.sound_g.PlayGlobalCommonSfx(soundId, 1f, 1.25f, 1);
            return false;
        }

        if (cmd_option.StartsWith("__OPT_WARP_LOCATION_"))
        {
            _currentLocationCmd = cmd_option.Replace("_return", "");
            string[] savepointArray =
                _savepointData[DB.TRANSLATE_map[_currentRegionCmd]][DB.TRANSLATE_map[_currentLocationCmd]].Keys
                    .Select(s => $"__OPT_WARP_SAVE_{s.Replace(' ', '_').ToLower()}")
                    .ToArray();
            string[] optionsArray = new string[savepointArray.Length + 3];
            optionsArray[0] = "__OPT_WARP_select_savepoint";
            Array.Copy(savepointArray, 0, optionsArray, 1, savepointArray.Length);
            optionsArray[optionsArray.Length - 2] = optionsArray[optionsArray.Length - 1] =
                $"{_currentRegionCmd}_return";

            InsertMenuData(__instance, optionsArray);

            PT2.sound_g.PlayGlobalCommonSfx(122, 1f, 1.25f, 1);
            return false;
        }

        if (cmd_option.StartsWith("__OPT_WARP_SAVE_"))
        {
            string levelNameToWarp = _savepointData[DB.TRANSLATE_map[_currentRegionCmd]]
                [DB.TRANSLATE_map[_currentLocationCmd]]
                [DB.TRANSLATE_map[cmd_option]];
            PT2.tv_hud.AppearPauseScreen(false, false);
            PT2.sound_g.PauseAllSounds(false);
            PT2.game_paused = false;
            Time.timeScale = 1f;
            PT2.sound_g.PlayGlobalCommonSfx(126, 1f, 1f, 1);
            PT2.LoadLevel(levelNameToWarp, 9000, Vector3.zero, false, 0.0f);
        }

        return true;
    }

    private static void InsertMenuData(DirectorLogic __instance, string[] optionsArray)
    {
        Traverse.Create(__instance).Field<string[]>("_curr_options_arr").Value = optionsArray;
        Traverse.Create(__instance).Field<int>("_curr_options_index").Value = 1;
        Traverse.Create(__instance).Field<int>("_curr_options_top_index").Value = 1;
        Traverse.Create(__instance).Method("_Options_RenderText").GetValue();
    }

    private static void LoadSavepointData()
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("PhoA_AP_client.assets.data.savepointdata.json");
        using StreamReader reader =
            new StreamReader(stream ?? throw new InvalidOperationException("savepoint data not found"));

        string json = reader.ReadToEnd();
        List<Savepoint> savepoints = JsonConvert.DeserializeObject<List<Savepoint>>(json);

        _savepointData = savepoints
            .GroupBy(savepoint => savepoint.Region)
            .ToDictionary(
                region => region.Key,
                region => region
                    .GroupBy(savepoint => savepoint.Location)
                    .ToDictionary(
                        location => location.Key,
                        location => location.ToDictionary(savepoint => savepoint.name,
                            savepoint => savepoint.levelName)
                    )
            );
    }
}