using HarmonyLib;
using UnityEngine;

namespace PhoA_AP_client.patches;

[HarmonyPatch]
public class GameplayAdjustmentPatches
{
    private static bool _justRespawnedInWater;
    
    [HarmonyPatch(typeof(GaleInteracter), "ReturnToCheckpoint")]
    [HarmonyPrefix] // Patch to detect whether the player respawns in water
    private static void ReturnToCheckpointPrefix(GaleInteracter __instance)
    {
        Vector3 checkpointLocation = Traverse.Create(__instance).Field<Vector3>("_checkpoint_location").Value;
        RaycastHit2D hit =  Physics2D.Raycast(checkpointLocation, Vector2.zero, 1f, GL.mask_WATER);
        
        if (hit.collider == null) return;
        Traverse.Create(__instance).Field<bool>("_checkpoint_flush").Value = false;
        
        if (!Traverse.Create(__instance).Field<bool>("DEBUG_CAN_SWIM").Value)
            _justRespawnedInWater = true;
    }
    
    [HarmonyPatch(typeof(GaleLogicOne), "_STATE_Drowning")]
    [HarmonyPostfix] // Patch to prevent the player from infinitely drowning
    private static void STATEDrowningPostfix()
    {
        if (!_justRespawnedInWater) return;
        PT2.gale_script.SetGaleModeOnLevelLoad(GALE_MODE.SWIMMING);
        PT2.gale_script.SendGaleCommand(GALE_CMD.SET_GALE_MODE);
        _justRespawnedInWater = false;
    }
}