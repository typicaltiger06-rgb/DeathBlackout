using HarmonyLib;
using NuclearOption.Networking;
using UnityEngine;


/// <summary>
/// Patches Pilot.ApplyDamage to detect when the local player's pilot dies
/// and trigger the blackout + killer text.
/// </summary>
[HarmonyPatch(typeof(Pilot), "ApplyDamage")]
internal static class Pilot_ApplyDamage_Patch
{
    /// <summary>
    /// Capture the pilot's alive state BEFORE the original method runs,
    /// so we can detect the alive → dead transition in the Postfix.
    /// </summary>
    private static void Prefix(Pilot __instance, out bool __state)
    {
        __state = __instance.dead;
    }

    /// <summary>
    /// After ApplyDamage runs, check if the pilot just died (was alive before,
    /// is dead now). If it's the local player, trigger blackout and resolve
    /// the killer's name from the aircraft's damage credit dictionary.
    /// </summary>
    private static void Postfix(Pilot __instance, bool __state)
    {
        bool wasAlive = !__state;
        bool isNowDead = __instance.dead;

        if (!wasAlive || !isNowDead)
            return;

        if (__instance.aircraft == null)
            return;

        if (!GameManager.IsLocalPlayer<Player>(__instance.aircraft.Player))
            return;

        // Resolve who killed us
        string killer = Plugin.ResolveKillerName(__instance.aircraft);

        // Trigger the blackout with the killer text
        Plugin.TriggerBlackout(killer);
    }
}
