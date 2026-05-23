# Changelog

All notable changes to Death Blackout are documented here.

## \[2.1.0] — 2026-05-22

### Added

* **Mute-on-death** (`MuteAllSound`, on by default): silences all audio while the blackout is active, fading back in as the overlay clears. Contributed by **Meteez**.
* **Killer text** (`ShowKillerText`, off by default): optional white text naming the unit (and player, if applicable) that killed you. Configurable size and vertical position. Contributed by **Meteez**.
* Music crossfades are now blocked during the mute so no death music bleeds through on audio restore.

### Changed

* `ShowKillerText` ships **off by default** to preserve the pure "never saw it coming" blackout; opt in via config if you want kill info.

### Notes

* Killer text only appears when an enemy unit dealt the killing blow. Deaths with no attacker (e.g. terrain impact) show a clean text-free blackout.

## \[1.0.0] — 2026-05-21

### Added

* Initial release: instant black overlay on local pilot death, with instant-cut or fade options, auto-clear on respawn.
* Auto-clear after a configurable `HoldSeconds`, so you're never stuck on a black screen if respawn isn't available.
* `FadeOutSpeed` for a smooth fade-back instead of an instant cut.
* Emergency `DismissKey` (default `End`) to instantly clear the overlay.
* Slider ranges on numeric config values for nicer Configuration Manager UI.



