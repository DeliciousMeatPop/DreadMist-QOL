# DreadMist-QOL

Quality-of-life mod for **DreadMist** (Steam app 2285190, single-player; not Dreadmyst).

Status: investigation stage. Nothing is built yet. See `docs/PLAN.md`.

## Step 1 - inspect your install
```
python tools/inspect_install.py
```
Output goes to `inspection/` (git-ignored). Never commit game files.

## Step 2 - decompile only the relevant classes (local, never committed)
Needs the .NET SDK, then once: `dotnet tool install -g ilspycmd`
```
python tools/decompile_key_classes.py
```
Send `inspection/decompiled_selected.zip`. Also send (copy from `DreadMist_Data\StreamingAssets\`): `Mods\TestMod\ModInfo.xml`,
`Definitions\Perks\Attribute\Courage\Perk_Brave.xml`, the 3 files in `Definitions\StatEffects\Sanity`,
`Definitions\ItemLists\Development\_DevTest.xml`, `Definitions\Dialogue\Trade\*`, and `Definitions\Dialogue\Statements\Barks\Trader\B_TraderCloth.xml`.
