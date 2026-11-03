# DreadMist-QOL

Quality-of-life mod for **DreadMist** (Steam app 2285190, single-player; not Dreadmyst).
It is a BepInEx plugin: **no original game file is changed**, and removal is one script.

Status: **0.1.0 - first working draft, not yet tested in the real game.** Included so far:
the optional **cheat panel** (buttons for the game's console commands) and the **dialogue filter**
(conversations show only choices you can do now; hold **Shift** to see all choices with their requirements;
setting `HideUnavailableChoices`, `RevealKey`). Perk-point display and sanity/trader-finder are next (findings in `docs/FINDINGS.md`).

Quality-of-life features and cheats are separate. The cheat panel is **off by default** (`EnableCheatPanel = false`).

## What you need
- Windows, DreadMist installed through Steam
- Python 3 (only for the optional inspection tools)
- The .NET SDK: https://dotnet.microsoft.com/download (big "Download" button, then reopen PowerShell)

No paths are hardcoded. The scripts find the game through Steam; if they can't, a folder picker opens.

## 1) Install BepInEx (manual, one time)
1. Download **BepInEx 5.4.x, x64** (file named like `BepInEx_win_x64_5.4.23.x.zip`) from https://github.com/BepInEx/BepInEx/releases. Use 5.4.x, not 6 / "bleeding edge".
2. Unzip **into the game folder** (right-click DreadMist in Steam > Manage > Browse local files) so `winhttp.dll` sits next to `DreadMist.exe`.
3. Start the game once, wait for the main menu, quit. This creates `BepInEx\core`, `BepInEx\config`, `BepInEx\LogOutput.log`.

## 2) Build and install the mod
Open PowerShell in this repo's folder:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1
```
```powershell
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```
(If the build fails, copy the red error text. Don't edit anything.)

## 3) Turn on the cheat panel (optional)
1. Start the game once so the config file is created, then quit.
2. Open `BepInEx\config\com.deliciousmeatpop.dreadmistqol.cfg` and set `EnableCheatPanel = true`.
   The hotkey is `ToggleCheatPanel = F8` (change it there, e.g. `LeftControl + F8`).
3. Load a save, press **F8**.

## Settings (BepInEx\config\com.deliciousmeatpop.dreadmistqol.cfg)
| Setting | Default | What it does |
|---|---|---|
| `Cheats.EnableCheatPanel` | false | Master switch for the cheat panel |
| `Hotkeys.ToggleCheatPanel` | F8 | Show/hide the panel (the X button also hides it, `_` minimizes it to a thin bar) |
| `Dialogue.HideUnavailableChoices` | true | Hide choices you can't do now; hold the reveal key to see all. Set false to turn the filter off completely |
| `Dialogue.RevealKey` | Shift | Shift, Control or Alt |
| `Dialogue.DebugLog` | true | Log which choices were hidden (send me `BepInEx\LogOutput.log` if the filter misbehaves) |
| `Tweaks.GoldWeighsNothing` | false | Coins weigh 0 |

## Testing checklist (please report what happens)
1. The game starts and `BepInEx\LogOutput.log` contains `DreadMist QOL 0.1.0 loaded`.
2. With `EnableCheatPanel = false`, F8 shows a "disabled" box and nothing else changes.
3. With it `true`, F8 opens/closes the panel (after loading a save).
4. Items tab: search "coin" or "orb", pick one, quantity 2, **Give item**. It shows up in the inventory.
5. Progress tab: pick Strength, Add experience 100.
6. Perks tab: pick Courage, pick Brave, **Give perk**.
7. World tab: Set time to noon.
8. Dialogue: talk to any NPC. Red/unavailable choices should be hidden; hold Shift and they appear with requirements; release and they vanish. Back/Exit must always be reachable.
9. Try typing in a text box: if the game also reacts to those keys (hotkeys), tell me which.
Send me `BepInEx\LogOutput.log` for any problem.

## Remove
```powershell
powershell -ExecutionPolicy Bypass -File scripts\uninstall.ps1
```
Add `-RemoveBepInEx` to also remove BepInEx (back to a stock game folder), `-RemoveConfig` to delete the settings file.

## Known risks (not yet verified in game)
- Clicks on the panel may also reach the game underneath (IMGUI doesn't block the game's own UI).
- Typing in text boxes may trigger the game's keyboard shortcuts.
- The plugin calls `QFSW.QC.ConsoleCommands` directly (verified from the game's code, untested at runtime).

## For developers / research
- `docs/COMMANDS.md`: every console command with arguments (verified from the game's code).
- `docs/FINDINGS.md`: how perk points, dialogue requirements, sanity and trading actually work.
- `tools/inspect_install.py`, `tools/decompile_key_classes.py`: local, read-only inspection helpers. Their output goes to `inspection/`, which is git-ignored. **Never commit game files or decompiled code.**
