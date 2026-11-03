# Requested ideas and their status

## Done / in progress
- Cheat panel: dark readable theme, X (close) and _ (minimize to a thin bar) buttons, Dev Orb button, generic Commands tab (every game command, auto-generated, with argument boxes).
- Gold weighs nothing: config `Tweaks.GoldWeighsNothing` (off by default). Patches `ItemData.Weight` for coins only.
- Dialogue filter reworked (hide without deactivating buttons; debug log per dialogue). Needs in-game confirmation.

## Foraging (built, needs in-game testing)
- Verified from game code: bushes use `InteractableResourceDestructibleController` (harvests used vs `HarvestLimit`; limit reached = destroyed; the game already respawns destroyed ones after `RespawnTime`, default 7 days).
- Built: stop-at-1 protection with warning + override key; slow regrowth of partly used plants. Applies to plants whose skill is Foraging and that aren't trees (assumed to cover bushes, flowers, mushrooms; to be confirmed with the log).
- NOT built: growing a plant's maximum above its default (your 10/12 idea). It needs a per-plant record that survives save/load and the game's clock units; needs `WorldManager` and the save system classes.

## Needs more game code (next decompile bundle)
- **Dig for bait anywhere with a shovel** (fishing bugs). Needs the digging/`Interactable` + fishing bait code.
- **Trader finder** (where do clothes sell). Needs `NPC_Behavior*`/`CharacterDefinition`.

## Big new content (not planned yet; changes gameplay)
- Combination-lock strongbox for coins only; a moneylending stall (usury, hired collectors). These are new game systems (new items, interactables, NPC jobs, saving data), far beyond a QoL patch, and would need save-game and balance work. Parked until the core features are verified; say if you want me to scope one as a separate optional mod.
