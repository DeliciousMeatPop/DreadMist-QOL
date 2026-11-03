# Plan (status: nothing verified yet)
1. Inspect install -> runtime (Mono/IL2CPP), loader choice  [tools/inspect_install.py]
2. Extract console commands + IDs -> docs/COMMANDS.md
3. Command UI (cheats kept in a separate, optional module)
4. Perk points / costs
5. Dialogue filtering (Shift reveals)
6. Insanity + clothing/purchase guidance
Every finding is tagged VERIFIED (seen in current build) or ASSUMED.

## Findings from the user's file listing (VERIFIED: present in the listed install; contents NOT yet read)
- Runtime is **Mono** (`MonoBleedingEdge\`, `DreadMist_Data\Managed\Assembly-CSharp.dll`), not IL2CPP -> BepInEx 5 (Mono, x64) + Harmony is a viable, non-destructive loader. Not yet tested.
- Console is **QFSW Quantum Console** (`QFSW.QC*.dll`). Its commands are normally C# methods tagged `[Command]`, so a mod can likely enumerate/invoke them via the QC command table instead of typing text. ASSUMED until confirmed in the assembly.
- UI uses TextMeshPro + UnityEngine.UI; Unity Input System and legacy input both present; Unity Localization present (`Definitions\Localization`).
- No mod loader currently installed in the listing.
- Data is XML under `DreadMist_Data\StreamingAssets\Definitions\`: Items (1576 files), ItemLists (673, incl. `Development\_DevTest*`), Perks (189: Attribute/Skill/Race), StatEffects (110, incl. `Sanity`, 4 files), Dialogue (196: Trees/Packages/Statements), Interactable, Entities, Characters, GameStarts, Localization.
- Item IDs likely equal XML file names (e.g. `Res_Farm_Chilli1`, `Misc_DevOrb`). ASSUMED; confirm by reading the XML.
- Dialogue has `Packages\ToBeRemoved` and `Statements\Greetings\WIP`: the dialogue system is still in flux.

## Still needed
Contents of: Perks XML (cost/requirements), StatEffects\Sanity XML, a Dialogue Tree + a Cloth trader package, `_DevTest.xml`, and the command methods in Assembly-CSharp.dll (via ILSpy/dnSpy or `tools/inspect_install.py` output).
