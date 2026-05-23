using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOption.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;


[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.aiden.deathblackout";
    public const string PluginName = "Death Blackout";
    public const string PluginVersion = "2.1.0";

    internal static ManualLogSource Log;

    private static Texture2D blackTex;
    private static float alpha;
    private static bool blackedOut;
    private static float blackoutStartTime;

    /// <summary>Public read-only access to current blackout alpha for other patches.</summary>
    internal static float CurrentAlpha => alpha;

    /// <summary>Public read-only access to whether blackout is active for other patches.</summary>
    internal static bool IsBlackedOut => blackedOut;

    // Killer text state
    internal static string killerText = "";
    private static GUIStyle killerStyle;
    private static bool styleInitialized;

    // Audio mute state
    private static float savedListenerVolume = -1f; // -1 = not saved / already restored

    // Config: Blackout
    internal static ConfigEntry<bool> instantBlack;
    internal static ConfigEntry<float> fadeInSpeed;
    internal static ConfigEntry<float> holdSeconds;
    internal static ConfigEntry<float> fadeOutSpeed;
    internal static ConfigEntry<KeyCode> dismissKey;

    // Config: Sound
    internal static ConfigEntry<bool> muteAllSound;

    // Config: Killer text
    internal static ConfigEntry<bool> showKillerText;
    internal static ConfigEntry<int> killerTextSize;
    internal static ConfigEntry<float> killerTextYOffset;

    private void Awake()
    {
        Log = Logger;

        // Blackout settings
        instantBlack = Config.Bind("Blackout", "InstantBlackout", true,
            "True: cut to black instantly on death. False: fade to black.");
        fadeInSpeed = Config.Bind("Blackout", "FadeInSpeed", 4f,
            new ConfigDescription("Seconds when fading TO black (only when InstantBlack = false).",
                new AcceptableValueRange<float>(0.1f, 20f), Array.Empty<object>()));
        holdSeconds = Config.Bind("Blackout", "HoldSeconds", 3f,
            new ConfigDescription("Seconds to stay fully black before auto-fading out. 0 = no auto-clear.",
                new AcceptableValueRange<float>(0f, 15f), Array.Empty<object>()));
        fadeOutSpeed = Config.Bind("Blackout", "FadeOutSpeed", 1f,
            new ConfigDescription("Seconds when fading FROM black. 0 = instant clear.",
                new AcceptableValueRange<float>(0f, 10f), Array.Empty<object>()));
        dismissKey = Config.Bind("Blackout", "DismissKey", KeyCode.End,
            "Hotkey to instantly clear the overlay if stuck.");

        // Sound settings — complete silence during death
        muteAllSound = Config.Bind("Sound", "MuteAllSound", true,
            "Mute ALL audio while the death blackout is active. You will hear absolutely nothing — no death music, no explosions, no cockpit sounds. Audio fades back in as the overlay fades out.");

        // Killer text settings
        showKillerText = Config.Bind("KillerText", "ShowKillerText", true,
            "Show white text on the blackout overlay indicating what killed you.");
        killerTextSize = Config.Bind("KillerText", "TextSize", 28,
            new ConfigDescription("Font size for the killer text.",
                new AcceptableValueRange<int>(12, 72), Array.Empty<object>()));
        killerTextYOffset = Config.Bind("KillerText", "YOffset", 0.35f,
            new ConfigDescription("Vertical position of the killer text on screen (0 = bottom, 1 = top).",
                new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));

        // Create the 1x1 black texture for the overlay
        blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();
        DontDestroyOnLoad(blackTex);

        new Harmony(PluginGuid).PatchAll();

        Log.LogInfo($"Death Blackout {PluginVersion} loaded.");
    }

    /// <summary>
    /// Called by Pilot_ApplyDamage_Patch when the local player's pilot dies.
    /// </summary>
    public static void TriggerBlackout(string killer = "")
    {
        if (blackedOut)
            return;

        blackedOut = true;
        blackoutStartTime = Time.unscaledTime;
        killerText = killer;
        styleInitialized = false;

        // Save the current AudioListener volume and mute everything
        if (muteAllSound.Value && savedListenerVolume < 0f)
        {
            savedListenerVolume = AudioListener.volume;
            AudioListener.volume = 0f;

            // Also stop any currently-playing music (e.g. the death sound
            // that Pilot.ApplyDamage just started via CrossFadeMusic).
            // This prevents it from bleeding through when volume fades back.
            try { MusicManager.i.StopMusic(); } catch { }

            Log.LogInfo($"Muted all audio (saved volume: {savedListenerVolume:F2}).");
        }

        Log.LogInfo(string.IsNullOrEmpty(killer)
            ? "Pilot down — triggering blackout."
            : $"Pilot down — triggering blackout. Killed by: {killer}");
    }

    /// <summary>
    /// Called by Pilot_OnInitialize_Patch when the pilot re-spawns.
    /// </summary>
    public static void ClearBlackout()
    {
        if (!blackedOut && alpha <= 0f)
            return;

        blackedOut = false;
        killerText = "";

        // Restore audio immediately on respawn
        RestoreVolume();

        Log.LogInfo("Clearing blackout.");
    }

    /// <summary>
    /// Restores the saved AudioListener volume. Safe to call multiple times.
    /// </summary>
    private static void RestoreVolume()
    {
        if (savedListenerVolume >= 0f)
        {
            AudioListener.volume = savedListenerVolume;
            Log.LogInfo($"Restored audio volume to {savedListenerVolume:F2}.");
            savedListenerVolume = -1f;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Dismiss key
        if (Input.GetKeyDown(dismissKey.Value) && (alpha > 0f || blackedOut))
        {
            blackedOut = false;
            alpha = 0f;
            killerText = "";
            RestoreVolume();
            Log.LogInfo($"Blackout dismissed via {dismissKey.Value} key.");
            return;
        }

        if (blackedOut)
        {
            alpha = instantBlack.Value
                ? 1f
                : Mathf.MoveTowards(alpha, 1f, fadeInSpeed.Value * dt);

            if (holdSeconds.Value > 0f && Time.unscaledTime - blackoutStartTime >= holdSeconds.Value)
            {
                blackedOut = false;
                Log.LogInfo($"Hold time elapsed ({holdSeconds.Value}s) — fading out.");
            }
        }
        else
        {
            if (alpha <= 0f)
            {
                killerText = "";
                return;
            }

            alpha = fadeOutSpeed.Value <= 0f
                ? 0f
                : Mathf.MoveTowards(alpha, 0f, fadeOutSpeed.Value * dt);

            if (alpha <= 0f)
            {
                killerText = "";
                // Overlay fully gone — restore volume
                RestoreVolume();
            }
        }

        // During fade-out, smoothly bring audio back proportionally with overlay alpha.
        // When alpha = 1 (fully black): volume = 0 (silent)
        // When alpha = 0 (overlay gone): volume = savedListenerVolume (full)
        if (muteAllSound.Value && savedListenerVolume >= 0f && alpha > 0f && !blackedOut)
        {
            // We're in the fade-out phase — fade volume back in
            AudioListener.volume = savedListenerVolume * (1f - alpha);
        }
    }

    private void OnGUI()
    {
        if (alpha <= 0f || blackTex == null)
            return;

        // Draw the black overlay
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), blackTex);
        GUI.color = prev;

        // Draw killer text
        if (showKillerText.Value && !string.IsNullOrEmpty(killerText) && alpha > 0.5f)
        {
            if (!styleInitialized || killerStyle == null)
            {
                killerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = killerTextSize.Value,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
                styleInitialized = true;
            }

            killerStyle.normal.textColor = new Color(1f, 1f, 1f, Mathf.Clamp01((alpha - 0.5f) * 2f));

            float yCenter = Screen.height * Mathf.Clamp(killerTextYOffset.Value, 0f, 1f);
            Vector2 size = killerStyle.CalcSize(new GUIContent(killerText));
            Rect rect = new Rect(
                (Screen.width - size.x) / 2f,
                yCenter - size.y / 2f,
                size.x,
                size.y);

            GUI.Label(rect, killerText, killerStyle);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: resolve the name of the unit that dealt the most
    // damage to the given aircraft's damageCredit dictionary.
    // ─────────────────────────────────────────────────────────────
    internal static string ResolveKillerName(Aircraft aircraft)
    {
        try
        {
            if (aircraft == null)
                return "";

            // Access the protected damageCredit field via reflection
            var dcField = AccessTools.Field(typeof(Unit), "damageCredit");
            if (dcField == null)
                return "";

            var damageCredit = dcField.GetValue(aircraft) as Dictionary<PersistentID, float>;
            if (damageCredit == null || damageCredit.Count == 0)
                return "";

            // Find the dealer with the highest damage
            PersistentID killerID = PersistentID.None;
            float highestDamage = 0f;

            foreach (var kvp in damageCredit)
            {
                if (kvp.Value > highestDamage)
                {
                    highestDamage = kvp.Value;
                    killerID = kvp.Key;
                }
            }

            if (!killerID.IsValid)
                return "";

            // Resolve the killer's PersistentUnit to get its display name
            PersistentUnit killerPU;
            if (!UnitRegistry.TryGetPersistentUnit(killerID, out killerPU) || killerPU == null)
                return "";

            string name = killerPU.unitName;
            if (string.IsNullOrEmpty(name))
                return "";

            // If the killer is a player-controlled unit, append the player name
            if (killerPU.player != null)
            {
                string playerName = killerPU.player.GetNameOrCensored();
                if (!string.IsNullOrEmpty(playerName))
                    return $"Killed by {name} ({playerName})";
            }

            return $"Killed by {name}";
        }
        catch (Exception e)
        {
            Log.LogWarning($"Failed to resolve killer name: {e.Message}");
            return "";
        }
    }
}
