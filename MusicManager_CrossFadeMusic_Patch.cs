using HarmonyLib;
using UnityEngine;


/// <summary>
/// Patches MusicManager.CrossFadeMusic to block ALL music crossfades
/// when the death blackout is active and muteAllSound is enabled.
///
/// This works together with the AudioListener.volume muting in Plugin.cs.
/// While AudioListener.volume = 0 already ensures nothing is heard,
/// this patch prevents the music system from starting new tracks or
/// crossfading during death — so no stale music is queued that could
/// become audible when volume is restored.
///
/// In the base game, Pilot.ApplyDamage calls:
///   MusicManager.i.CrossFadeMusic(GameAssets.i.deathSound, 2f, 0f, false, true, true);
/// This prefix intercepts that call and any other crossfade during blackout.
/// </summary>
[HarmonyPatch(typeof(MusicManager), "CrossFadeMusic")]
internal static class MusicManager_CrossFadeMusic_Patch
{
    /// <summary>
    /// Returns false to skip the original CrossFadeMusic if:
    ///   - The muteAllSound config is enabled
    ///   - A blackout is currently active or was just triggered
    /// This blocks ALL music crossfades, not just the death sound.
    /// </summary>
    private static bool Prefix()
    {
        if (!Plugin.muteAllSound.Value)
            return true; // config disabled, allow all music

        // No blackout state at all — allow everything
        if (!Plugin.IsBlackedOut && Plugin.CurrentAlpha <= 0f)
            return true;

        // Blackout is active — block all music crossfades
        Plugin.Log.LogInfo("Blocked music crossfade during death blackout.");
        return false;
    }
}
