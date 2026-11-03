# Findings (from the user's inspector output; nothing run in-game yet)

Legend: **VERIFIED** = present in the user's file listing / string dump of the current Steam build. **ASSUMED** = inferred, needs confirming.

## Runtime
- VERIFIED: 64-bit Unity, Mono scripting backend (`report.json`: runtime=Mono, arch64=true, no mod loaders installed).
- ASSUMED: BepInEx 5.4.x (x64, Mono) + HarmonyX works. Plan: ship a plugin DLL + install/uninstall scripts. Nothing in the game folder is overwritten except adding BepInEx files.
- VERIFIED: game ships its own `StreamingAssets\Mods\TestMod\ModInfo.xml` (21 bytes). A native XML mod system exists; useful for data, not for UI code. Contents not yet seen.

## Console (QFSW Quantum Console)
- VERIFIED: type `ConsoleCommands` (source `Assets\Scripts\ConsoleCommands.cs`, `QFSW.QC` namespace), `CommandAttribute` referenced, `QuantumConsole` + `quantumConsolePrefab`. Separate game UI class `CommandButtonsUI`/`commandPanelUI` exists (unknown purpose).
- Command-like names found in the string table (NOT yet confirmed as `[Command]` methods; signatures unknown):

| Name | Seen in dev replies | Notes |
|---|---|---|
| GiveCoin, AddCoin, RemoveCoin | GiveCoin | which are real commands vs internal methods is unknown |
| GiveItem, GiveItems, GiveTestItems | GiveItem, GiveTestItems | |
| GivePerk | no | error string "No Character to give perk" |
| AddPerkPoint | no | |
| Revive, ReviveAll | ReviveAll | |
| AddExperience, AddExperienceAll, AddExperienceAttribute, AddExperienceAttributeAll, AddExperienceSkill, AddExperienceSkillAll, AddExperienceSoft, AddExperienceNoSoft, AddSkillExperience | AddExperienceAttribute | skill variants exist |

- No Teleport/Spawn/God/Time commands appeared in the string search (search was filtered to Give|Add|Remove|Set|Revive|Spawn|Teleport|Kill|Heal|Unlock|Console|Cheat|Debug|Command prefixes, so others may exist).

## Perks
- VERIFIED: field/property `perkPointsUnlocked` / `PerkPointsUnlocked`, method `AddPerkPoint`, classes `PerksWindowUI`, `PerkDisplayUI`, `PerkUpgradeEntryUI`, `PerkUpgradeListUI`, `PerkScrollListUI`, `PerkData`, `PerkDefinition`, `PerkSave`.
- The "24 stays 24" display is probably *unlocked/total* points rather than *remaining*. ASSUMED. Needs decompiled `PerksWindowUI`/`PerkData`.
- Perk XML: `Definitions\Perks\{Attribute,Skill,Race}\...` (Attribute: Agility, Courage, Endurance, Knowledge, Personality, Strength, Vitality; ~50 skill folders). IDs likely = file names such as `Perk_Brave`.

## Dialogue / requirements (red text)
- VERIFIED: `DialogueWindowUI`, `NPC_BehaviorDialogue`, many `Condition*` and `ConditionIndicator*` classes (Skill, Attribute, Coin, Item, Relation, Reputation, Sex, SocialClass, TimeOfDay, Hygiene, Intoxication...). The ConditionIndicator family is the likely source of the red requirement text, so presentation filtering can hook there/in DialogueWindowUI. ASSUMED.
- Dialogue content: `Definitions\Dialogue\{Trees,Packages,Statements,Trade}`; `Packages\ToBeRemoved` and `Statements\Greetings\WIP` mean content is mid-refactor.

## Trading / clothing
- VERIFIED classes: `NPC_BehaviorTrader`, `NPC_BehaviorBuying`, `TradingMenuInventoryUI`, `InteractableShopCounterController`, `ItemEquipmentData`, `ItemStolenData`; XML `ItemLists\Shop`, `ItemFilters\{Buyer,Shop}`, `Items\Equipment\Wearable\Clothes\*`, `Dialogue\Barks\Trader\B_TraderCloth.xml`.
- Cause of the clothing confusion not determined yet.

## Insanity
- VERIFIED: `StatEffects\Sanity` (3 XML), `SanityMultAdd`, `DialogueActionSanity`, `StatEffectInstance`, `SentientStatEffectContainer`, `StatEffectUI`, `EntityActiveStatsUI`, `StatModifiers`, `CharacterStatsWindowUI`.
- Other StatEffect folders that may feed it: Hunger, Hygiene, Light, Decor, Intoxication, Injuries, Temperature, Thirst, Fullness, Health. ASSUMED relationship.

## Next data needed
Run `tools/decompile_key_classes.py` and send `inspection/decompiled_selected.zip`; also the XML files listed in the README.

## Update: decompiled code + XML samples (VERIFIED in current build)
- `Mods\TestMod\ModInfo.xml` is an empty `<ModInfo></ModInfo>`: the game's native mod hook is a stub, so we use BepInEx.
- Perk XML (`Perk_Brave`): `Type` (attribute/skill), `IconKey`, `DisplayName`, `Levels[]`; each level has `RequireLevel` (the attribute/skill level needed) and stat effects. Brave: 10 levels needing attribute level 0,10,20...90. Perk key is likely the file name (`Perk_Brave`); not yet confirmed.
- Perk cost: the UI's upgrade click calls `UpgradePerk` then `TakePerkPoint()` once, so each perk level costs **1 point from that attribute/skill's own pool** (`AttributeData.PerkPointsCurrent`). The window label shows only that single number, unlabelled. A never-bought perk's button is enabled whenever points > 0 (the tooltip shows the level requirement in red).
- Sanity tiers (`SentientStatEffectContainer`): sanity > 50 none; 25-50 **Confused**; 10-25 **Irrational**; < 10 **Deranged**.

| Effect | Relations | Skill XP | Crafting quality | Crafting speed |
|---|---|---|---|---|
| Confused | -10 | -0.25 | -0.10 | - |
| Irrational | -25 | -0.50 | -0.25 | -0.10 |
| Deranged | -50 | -0.75 | -0.50 | -0.25 (pulses every 1.0) |

- What *drains* sanity is not yet known (needs `EntitySentientPlayerData`; `StatModifiers` has `SanityDrain`/`SanityChange`, dialogue actions call `ChangeSanity`).
