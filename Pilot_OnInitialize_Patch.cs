using HarmonyLib;
using NuclearOption.Networking;
using UnityEngine;


/// <summary>
/// Clears the blackout overlay when the pilot re-initializes (respawn).
/// </summary>
[HarmonyPatch(typeof(Pilot), "Pilot_OnInitialize")]
internal static class Pilot_OnInitialize_Patch
{
    private static void Postfix(Pilot __instance)
    {
        if (__instance.player == null)
            return;

        if (!GameManager.IsLocalPlayer<Player>(__instance.player))
            return;

        Plugin.ClearBlackout();
    }
}
