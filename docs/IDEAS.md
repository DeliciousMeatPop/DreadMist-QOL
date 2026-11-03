# Requested ideas and their status

## Done / in progress
- Cheat panel: dark readable theme, X (close) and _ (minimize to a thin bar) buttons, Dev Orb button, generic Commands tab (every game command, auto-generated, with argument boxes).
- Gold weighs nothing: config `Tweaks.GoldWeighsNothing` (off by default). Patches `ItemData.Weight` for coins only.
- Dialogue filter reworked (hide without deactivating buttons; debug log per dialogue). Needs in-game confirmation.

## Needs more game code (next decompile bundle)
- **Foraging rules** (user idea): foraging a bush down keeps it at a minimum of 1 (stop by default; warn before the final, destructive forage), partial bushes slowly regrow, and a fully-used-but-previously-foraged bush at e.g. 10/10 grows to 10/12 after ~5 days if left alone; never-foraged bushes stay at default. Needs `Interactable*` resource/growable classes and the world clock. The decompile script now bundles those families.
- **Dig for bait anywhere with a shovel** (fishing bugs). Needs the digging/`Interactable` + fishing bait code.
- **Trader finder** (where do clothes sell). Needs `NPC_Behavior*`/`CharacterDefinition`.

## Big new content (not planned yet; changes gameplay)
- Combination-lock strongbox for coins only; a moneylending stall (usury, hired collectors). These are new game systems (new items, interactables, NPC jobs, saving data), far beyond a QoL patch, and would need save-game and balance work. Parked until the core features are verified; say if you want me to scope one as a separate optional mod.
