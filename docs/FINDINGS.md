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
