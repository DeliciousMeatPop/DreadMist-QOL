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

## Update 2: second decompile bundle (VERIFIED in current build)
### Perk points (explains the "24" confusion, probably)
- Each attribute/skill has its own `perkPointsUnlocked` (total earned) and `perkPointsCurrent` (unspent). One point is earned per **5 levels** (`min(level,100)/5`, so max 20 per attribute/skill). Spending calls `TakePerkPoint()`.
- The Perks window label (`PerksWindowUI.perkCountText`) shows only `PerkPointsCurrent`, unlabelled. The code deliberately discards the attribute `Level`.
- CORRECTION (user, in-game): the "24" IS the perk-point number shown in the Perks window; the level is separate. A skill at level 4 with 0 points cannot buy perks, which matches the code (`level/5` = 0 points until level 5).
- OPEN: the code can only grant `min(level,100)/5` = **max 20** points per attribute/skill (`SkillData` uses the same formula), so 24 can't come from levelling alone. Possible causes: points stored in an older/edited save (`PerkPointsCurrent` is saved and loaded as-is), a different source I haven't seen, or the label not refreshing after a purchase. Needs in-game data (which attribute/skill, its level, and the log line the plugin will print).
- In the Character Levels window (`StatsElementUI`) the big number is the level and the *name turns yellow* when points are unspent.
- Cost per perk level is always **1 point**. A perk level also has `RequireLevel` (attribute/skill level), shown in red only in the hover tooltip.
- Side effect: `PerkUpgradeEntryUI.SetPerkData()` (never-bought perk) enables Upgrade whenever points > 0 without checking level-1's `RequireLevel`. Not confirmed whether `UpgradePerk` rejects it; to test in game.

### Dialogue
- `DialoguePackageDefinition.GetTrees`, `DialogueNode.GetDialogueResponses/GetDialogueSubTreeDefinitions` already filter by `ConditionsGroup.Evaluate`. Choices shown are therefore *selectable*.
- The red text is **display only**: `DialogueResponse.ConditionDisplay` has `ConditionIndicator`s (Skill, Coin, SocialClass, ...). Each indicator wraps itself in `<color=red>` when its own check fails and its `DisplayMode` (Always/Positive/Negative) allows showing it. The text is passed to `IntButtonTypedController.SetText(conditionText, responseText)` by `DialogueWindowUI.GoToDialogueRoot` / `SetCurrentDialogueNode`.
- Clicking a red choice is allowed; the outcome is decided later by `DialogueBranch.Evaluate` (a failed check falls to another branch). So a filter that hides buttons whose condition text contains `<color=red>` is a pure presentation change and can't unlock anything. Back/Exit have no conditions in the game code, so they're never hidden. Plan: Harmony postfix on the two private builders, hide/show with Shift.

### Sanity (what actually drains it)
- Max = `100 + Courage level + SanityMax modifiers` (min 10).
- Passive drain every tick = `0.00347222 x SanityDrainModifier x GameplayConfig.SanityDrainMultiplier`, applied whenever the character is not using furniture that restores sanity. `SanityDrainModifier = max(1 - min(Courage,100) x 0.005 + statModifiers.SanityDrain, 0)`: **Courage lowers drain** by up to 50%.
- Other sources: stat effects with `SanityChange` (x0.25 per tick, restore if positive, `TakeSanity` if negative), dialogue actions (`ChangeSanity`), item sanity values, and furniture (`FurnitureSanityRestore`).
- The game already has `ActiveValuesDisplay` (a per-hour breakdown incl. `Sanity: +/-`, red when negative). It lumps everything into one number without listing causes. The plugin can add the per-source breakdown (passive drain, Courage reduction, active effects) from the same fields.

### Trading / clothing
- Selling to a trader uses `TradingMenuInventoryUI.CanTradeItem`: items with `Value < 1` or rejected by the trader's `ItemFilter` all show a generic **"Item Invalid"**; stolen items give "I do not deal with stolen items!". `DialogueActionTrading` only opens trading if the NPC has a trading container and is near it (`ConditionTrade`).
- Clothing items carry `Respectability` (shirt: 15) and `SocialClass` conditions exist on dialogue. Trader/clothing messages never name the reason; guidance should be added at "Item Invalid" (why: filter vs zero value) and on `NearTradingContainer`. Exact "buying clothes" failure not reproduced yet; needs in-game screenshots/steps from the user.

### Misc
- Keys: `Definition.Key` = XML file name without extension (`ResourceManager` loader), so item/perk IDs are the file names (`Misc_DevOrb`, `Perk_Brave`).
- `_DevTest.xml` is just a bulk raw-material list, **not** what `GiveTestItems` gives (that is list `Starting_Player_Testing`, file not seen yet).
- `ResourceManager.ItemDefinitions` exposes every loaded item at runtime, so the item picker can use real localized names without parsing XML.

## Update 3: clothing is a *discoverability* problem (user, in-game)
- User: buying clothes isn't failing; they can't find anyone selling them in town.
- Data names two cloth-related traders: `TraderClothing_Nara` (character `NPC_NaraSouster`, likely the clothes seller) and `TraderCloth_Jaquette` (`NPC_Jaquette`). USER-CONFIRMED: Jaquette sells cloth (material) only, not clothes, matching filter `Shop_Cloth`. So Nara is the only candidate clothes seller; her location/schedule is unknown. shop lists `Shop_Store_Clothing`, `Shop_ClothesSimple`, `Shop_Market_Cloth`; item filters `Shop_Clothing`, `Shop_Cloth`. A `Tailor_Francois` package sits in `ToBeRemoved`.
- Trading only opens if the NPC `CanTrade` AND is `NearTradingContainer` (their counter/stall). NPCs also have sleep/schedule behaviours, so a seller may be away from their stall at some times. ASSUMED; to be confirmed with the character XML.
- Idea: a "who trades here" helper listing loaded NPCs that can trade, what they sell (their item filter / shop list), and distance/direction. Needs `NPC_Controller`, `CharacterDefinition` (Trader section) decompiled.

## Dialogue filter semantics (user-defined, implemented in src/DreadMistQOL/DialogueFilter.cs)
- Default: show only choices the player can do now. Hold Shift: show every choice with its requirements.
- 'Can do now' = the game's requirement text for that choice has no `<color=red>` (the game marks each unmet requirement red). Limitation: an indicator the developer configured to hide itself when unmet (DisplayMode Positive) leaves no red, so such a choice still counts as available.
- Safety: if every choice would be hidden, all are shown so a conversation can never dead-end.
