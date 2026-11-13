# The Last Known Signal  (Unity 6000.3.28f1, URP)

NASA Space Apps Cairo 2026 - Space Mission Design Game. Plan -> Run -> Debrief in every mission.

## Run / build
- Open this folder in Unity 6000.3.28f1 (Unity Hub). Scene: `Assets/Scenes/Main.unity` (empty - the game bootstraps itself from `Game.cs`).
- Menu: `TLKS > Setup Project` (URP asset, quality, build settings) and `TLKS > Build Windows`.
- Prebuilt: `Builds/TheLastKnownSignal/TheLastKnownSignal.exe`

## Controls
Mouse + Space/Enter for dialogue. M3 flight / evasion: WASD or arrows, Shift boost, Space opens the channel.
M4 landing: Space main engine, WASD thrusters. Walking: WASD, mouse, Shift. ESC twice = main menu. F11 fullscreen.
Menu has **Judge Mode** (unlocks Mission Design Mode and chapter select immediately).

## QA flags (command line)
`-chapter N` (0 prologue, 1-4 missions, 5 finale, 8 design mode), `-auto` (autoplays dialogue, planners, minigames), `-shots <dir> <seconds>` (screenshots).

## NASA data (shipped offline in `Assets/Resources/NasaData`)
- `h_10/301/499/599.txt` - JPL Horizons vectors (Sun, Moon, Mars, Jupiter relative to Earth, 2026-11-13) -> distances + light delay (distance / c).
- `exoplanets.csv` - NASA Exoplanet Archive pscomppars rows; Kepler-442 b is used (radius, insolation, Teq, period, mass, distance).
- `dsn.xml` - DSN Now snapshot (eyes.nasa.gov/dsn); the game also tries a live refresh and falls back to the snapshot.
- `parts.csv` - engine / module masses and thrusts. **Re-verify each against its cited NASA page before submission.**
- Textures: NASA Visible Earth imagery (from the provided model pack). Credit NASA on screen and in the description.

## Science vs fiction
Real: thrust/mass, rocket equation, redundancy math, light delay, Horizons positions, Exoplanet Archive values.
Fiction: year 2590, the nuclear ship, travel time, characters, relay network. Game-balance: reliabilities 0.63/0.90, masses, 45,000 kg launch limit, route delta-v and day counts.

## Structure
`Assets/Scripts`: Game.cs (core/menu), Planner.cs (Mission Planner + run sequence + DSN panel), Risk.cs (risk model), NasaData.cs, World.cs (celestial bodies, ships), UIKit.cs, Util.cs (materials, FX, procedural audio, post-processing), Stages1-3.cs (Prologue, M1-M4, Finale, Design Mode).
`Assets/Resources/Shaders`: custom URP shaders (Earth day/night, clouds, atmosphere, sun, sky, lit with shadows, additive).
