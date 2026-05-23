# Death Blackout — Nuclear Option mod

Your screen cuts to black the instant your pilot dies. A dramatic death effect that enhances immersion.
---

## Features

* **Instant blackout on pilot death** — hard cut to black the moment you die. Optionally a smooth fade instead.
* **Auto-clear** — the overlay fades back on respawn, or after a configurable hold time so you're never stuck.
* **Emergency dismiss key** — instantly clear the overlay (default: `End`) just in case.
* **Mute-on-death** *(optional)* — silence all audio while blacked out; fades back in as the screen clears.
* **Killer text** *(optional, off by default)* — white text naming the unit (and player, if applicable) that killed you.
* **Fully configurable** via [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) — tweak everything live with F1.

\---

## Installation

1. Install [BepInEx 5 (Mono, x64)](https://thunderstore.io/c/nuclear-option/p/BepInEx/BepInExPack/) into your Nuclear Option folder.
2. Download the latest `DeathBlackout.dll` from the [Releases page](https://github.com/typicaltiger06-rgb/DeathBlackout/releases/latest).
3. Drop it into `<Nuclear Option>/BepInEx/plugins/`.
4. Launch the game. You should see `Death Blackout 2.1.0 loaded.` in the BepInEx console.

Optional but recommended: install [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to tweak settings live in-game with F1. Otherwise, edit `BepInEx/config/com.aiden.deathblackout.cfg` after first launch.

\---

## Configuration

### Blackout

|Key|Default|Effect|
|-|-|-|
|`InstantBlackout`|`true`|Hard cut to black on death. `false` = fade.|
|`FadeInSpeed`|`4`|Fade-to-black speed (only used when `InstantBlackout = false`).|
|`HoldSeconds`|`3`|Seconds to stay fully black before auto-fading out. `0` = stay until respawn.|
|`FadeOutSpeed`|`1`|Fade-from-black speed. `0` = instant clear.|
|`DismissKey`|`End`|Hotkey to instantly clear the overlay if stuck.|

### Sound

|Key|Default|Effect|
|-|-|-|
|`MuteAllSound`|`true`|Mute all audio while blacked out. Fades back in as overlay clears.|

### KillerText

|Key|Default|Effect|
|-|-|-|
|`ShowKillerText`|`false`|Show text naming what killed you. Off by default to preserve the pure blackout.|
|`TextSize`|`28`|Font size for the killer text.|
|`YOffset`|`0.35`|Vertical position of the text (`0` = bottom, `1` = top).|

Note: killer text only appears when an enemy unit dealt the killing damage. Deaths with no attacker (terrain impact, etc.) show a clean text-free blackout.

\---

## How it works

A [BepInEx 5](https://docs.bepinex.dev/) plugin using Harmony patches on the game's `Pilot` class.

* **`Pilot.ApplyDamage`** — when pilot hitpoints drop below zero the game sets `pilot.dead = true`. The patch detects that alive→dead transition for the local player and triggers the blackout. It uses the same `GameManager.IsLocalPlayer` check the game's own death code uses, so it only fires for *you*, never for AI or other players.
* **`Pilot\_OnInitialize`** — fires when a fresh pilot spawns; clears the blackout on respawn.
* **`MusicManager.CrossFadeMusic`** — blocked while muted so no music bleeds through when audio is restored.

The overlay is a black texture drawn in `OnGUI` (on top of the HUD). Optional killer text is resolved from the aircraft's damage-credit data via the game's own name-resolution (respecting its censoring rules).

\---

## Credits

* **Typickle** — creator, original blackout mod.
* **Meteez** — contributed the mute-on-death and killer-text features (v2.1.0).

Thanks to the Nuclear Option modding community for the feedback and ideas.

\---

## License

MIT — see [LICENSE](LICENSE). Do whatever you want with it; attribution appreciated.

