# Console commands (VERIFIED from the decompiled `QFSW.QC.ConsoleCommands` class of the current Steam build)

All are Quantum Console `[Command]` methods, so the mod can call them directly. Most act on the **first selected player character** and need a loaded game.
Attribute/skill names are case-insensitive. Missing/unknown names log a red "not found" message in the console.

## Items, money, perks
| Command | Arguments | What it does |
|---|---|---|
| `GiveCoin` | `int count = 1000` | Adds a coin stack to the first selected character |
| `GiveItem` | `string itemDefinitionKey, int count = 1` | Adds an item by definition key. Key format still to be confirmed from XML (likely file name e.g. `Misc_DevOrb`) |
| `GiveTestItems` | none | Gives everything in item list `Starting_Player_Testing` |
| `GivePerk` | `string perkKey` | Upgrades that perk one level via LevelData. Does **not** spend perk points (the UI button does) |
| `UnlockAllRecipes` / `UnlockAllConstruction` | none | Unlock recipes / construction |

## Experience and levels (names listed below)
| Command | Arguments |
|---|---|
| `SetLevel` | `string attributeSkill, float value` |
| `AddExperience` | `string attributeSkill, float value` |
| `AddExperienceAttribute` | `string attribute, float value` (7 attributes only) |
| `AddExperienceSkill` | `string skill, float value` |
| `AddExperienceAll` / `AddExperienceAttributeAll` / `AddExperienceSkillAll` | `float value = 100` |

Attributes (7): STRENGTH AGILITY ENDURANCE VITALITY KNOWLEDGE COURAGE PERSONALITY
Skills (accepted by SetLevel/AddExperience): MELEECOMBAT BLOCKING MARKSMANSHIP HANDTOHAND DODGING WEAPONBLADE WEAPONAXE WEAPONBLUNT WEAPONPOLEARM WEAPONBOW WEAPONCROSSBOW WEAPONTHROWN ARMORLIGHT ARMORMEDIUM ARMORHEAVY ARCANECONTROL SMITHING WEAPONSMITHING ARMORER JEWELSMITHING TAILORING BOWYERY ALCHEMY COOKING BREWING WOODWORKING TANNING ENCHANTING RUNICENGINEERING SNEAKING LOCKPICKING STEALING TAKEDOWNS MINING LOGGING FARMING FISHING ATHLETICS SWIMMING MEDICAL BUTCHERING SPEECHCRAFT BARTERING READING GAMBLING HERBALISM NECROMANCY CONSTRUCTION MUSICIAN FORAGING TINKERING CHYMISTRY
(`AddExperienceSkill` has its own list; it matches the above for the first entries seen, full parity not yet diffed.)
Note: perk XML folder is spelled `Echanting` but the command name is `ENCHANTING`.

## Character state
| Command | Arguments |
|---|---|
| `ReviveAll` | none (PlayerManager.ReviveAll) |
| `Unalive` | none (kills first selected character) |
| `RestoreAllStats` | none |
| `ClearStatusEffects` | none |
| `SetHygiene` | `float value = 100` |
| `SetFame` / `AddFame` | `float value` (0 / +10 default) |
| `SetHonor` / `AddHonor` | `float value` (0 / +10 default) |
| `AddInjuryBleeding`, `...Light`, `...Heavy`, `...Severe`, `AddInjuryBrokenBone`, `AddInjuryDislocatedJoint`, `AddInjuryConcussion` | none |

## World
| Command | Arguments |
|---|---|
| `SkipDay`, `SkipWeek`, `SetTimeNoon`, `SetTimeMidNight` | none |
| `SetTimeOfDayPercentage` | `float 0..1` (clamped) |
| `SetWeatherNormal` / `Clear` / `Cloudy` / `Rain` | none |
| `Teleport` | none (moves party to the camera pivot) |
| `FreeCamera` | none (toggle) |
| `InstaGrow` | none |

## Not found
There is no `AddPerkPoint` command (the method exists in game code only), no sanity command, and no `Spawn` command.
Original forum examples (`GiveCoin 4000`, `ReviveAll`, `GiveItem Misc_DevOrb`, `GiveTestItems`, `AddExperienceAttribute Strength`) all match real commands.

## Still unknown
Valid item keys (`ResourceManager.GetItemDefinition`), perk keys, and whether `Starting_Player_Testing` exists in this build (command logs "Item list not found" if not; the `ItemLists\Development` folder in the listing has 39 files).
