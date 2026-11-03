# DreadMist QOL

A quality-of-life mod for **DreadMist** (the single-player sandbox RPG by Michael Shoots, Steam app 2285190 - not Dreadmyst).

It makes the game easier to read and understand **without changing its normal rules**. Every feature has its own on/off switch, and the optional cheat panel is **off by default**.

> **Unofficial fan mod.** Not affiliated with or endorsed by the game's developer. It contains no game files; it only adds one small plugin file next to your game. Early release (v0.2.0): please report problems, see "Feedback" below.

## What it does

| Feature | What you get |
|---|---|
| **Cleaner dialogue** | Conversations only show the choices you can do right now. **Hold Shift** to see every choice together with its requirements. Presentation only: nothing is bypassed. |
| **Clear perk points** | The perk window says "Perk points: X unspent / Y earned". Hovering a perk shows its cost (1 point) and exactly what is missing (level needed, no points, when the next point comes). |
| **Sanity details** | The stats window gets a "Sanity details" section: your stage and where the next one starts, what your maximum is made of, the constant drain and how Courage reduces it, and each active effect raising or lowering sanity. Information only; the mechanic is unchanged. |
| **Foraging protection** | Bushes, flowers and similar foraged plants stop at 1 harvest left (the last one destroys the plant). A hover note explains it; **hold Control** while interacting to harvest the last one anyway. Trees are not affected. |
| **Plant regrowth** | A partly-harvested plant you leave alone regains 1 harvest every 2 in-game days. |
| **Gold weighs nothing** *(optional, off)* | Coins stop counting toward your carry weight. |
| **Cheat panel** *(optional, off)* | A clickable panel for the game's own console commands: give items (searchable names + quantity), coins, XP and levels, perks, revive, time/weather, and a tab listing **every** console command with argument boxes. Has minimize and close buttons. Uses exactly the commands you could type with `~`. |

Everything is explained further down, including how to change the settings.

## Install (about 5 minutes, Windows)

You need **BepInEx 5** (a free, widely used mod loader) once, then this mod.

**1. Install BepInEx**
1. Download **BepInEx 5.4.x, x64** (a file like `BepInEx_win_x64_5.4.23.x.zip`) from <https://github.com/BepInEx/BepInEx/releases>. Use the 5.4.x version, not 6 or "bleeding edge".
2. Open the game folder: in Steam right-click DreadMist, **Manage > Browse local files**. It is the folder containing `DreadMist.exe`.
3. Extract the zip **into that folder**, so `winhttp.dll` sits next to `DreadMist.exe`.
4. Start the game once, wait for the main menu, then quit. This creates the `BepInEx\config` and `BepInEx\plugins` folders.

**2. Install DreadMist QOL**
1. Download `DreadMistQOL-v0.2.0.zip` from the **Releases** page of this repository.
2. Extract it **into the game folder** too (say yes to merging the `BepInEx` folder). The result must be `BepInEx\plugins\DreadMistQOL\DreadMistQOL.dll`.
3. Start the game. Open `BepInEx\LogOutput.log`: it should contain `DreadMist QOL 0.2.0 loaded`.

**Uninstall:** delete the `BepInEx\plugins\DreadMistQOL` folder. To remove BepInEx as well, delete the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini` and `.doorstop_version` from the game folder. Your saves are never touched.

## Using it

- **Dialogue:** talk to anyone. Unavailable choices are hidden; hold **Shift** to show them with their requirements. If every choice would be hidden, all are shown, so you can never get stuck.
- **Perks:** open a skill's perk window and hover a perk.
- **Sanity:** open the character stats window and scroll to "Sanity details".
- **Foraging:** hover a bush: the hover text shows "Harvests: remaining/limit", plus a note when it is protected or regrowing.
- **Cheat panel** (if enabled): press **F8** to open. **X** closes it, **_** shrinks it to a thin bar that stays on screen. Pressing F8 again also closes it.

## Settings

Start the game once, then edit `BepInEx\config\com.deliciousmeatpop.dreadmistqol.cfg` with Notepad. Restart the game after changing it.

| Setting | Default | Meaning |
|---|---|---|
| `Cheats` > `EnableCheatPanel` | `false` | Turns the cheat panel on |
| `Hotkeys` > `ToggleCheatPanel` | `F8` | Hotkey, e.g. `LeftControl + F8` |
| `Dialogue` > `HideUnavailableChoices` | `true` | `false` turns the dialogue filter off completely |
| `Dialogue` > `RevealKey` | `Shift` | `Shift`, `Control` or `Alt` |
| `Dialogue` > `DebugLog` | `true` | Writes what was hidden to the log (helps bug reports) |
| `Perks` > `ClearPointDisplay` | `true` | Perk point label and perk tooltips |
| `Sanity` > `ShowBreakdown` | `true` | "Sanity details" in the stats window |
| `Foraging` > `ProtectLastHarvest` | `true` | Stop at 1 harvest left |
| `Foraging` > `OverrideKey` | `Control` | Key to harvest the protected last one |
| `Foraging` > `RegrowDays` | `2` | Days per regrown harvest. `0` turns regrowth off |
| `Tweaks` > `GoldWeighsNothing` | `false` | Coins weigh 0 |

## Good to know

- **Perk points are per skill.** Each attribute and skill has its own pool: 1 point for every 5 levels (max 20 each), and every perk level costs 1 point. A skill at level 4 has 0 points, so nothing can be bought yet.
- **"Unavailable" in dialogue** means the game itself marked a requirement red. Choices whose requirements the developer chose to hide can't be detected and stay visible.
- **Game updates** can rename internal parts of the game. Each feature installs separately, so one breaking should not stop the others; errors are written to the log.
- Tested on Windows with the Steam version of the game. **Steam Deck / Linux / Proton is untested.**
- The cheat panel clicks may also reach the game underneath it, and typing in its text boxes may trigger game hotkeys. Minimize it while playing.

## Feedback (this is an early release!)

Bug reports and ideas are very welcome: open an **Issue** on this repository (the form tells you what to include). The most useful thing to attach is **`BepInEx\LogOutput.log`** from the game folder. Please mention your difficulty/start and whether you changed any settings.

Planned: time-of-day changes within the same day, a "who sells what" trader finder, digging for bait, and bushes that grow beyond their default size when cared for.

## Credits and license

Made by DeliciousMeatPop. Released under the MIT license (see `LICENSE`). Built on [BepInEx](https://github.com/BepInEx/BepInEx) and Harmony. DreadMist belongs to its developer; this project contains none of its files.
