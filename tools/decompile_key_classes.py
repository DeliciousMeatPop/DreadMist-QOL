#!/usr/bin/env python3
"""Step 2 helper: decompile Assembly-CSharp.dll LOCALLY and bundle only the
classes the mod needs, so you can send them for analysis.

One-time setup (needs the .NET SDK from https://dotnet.microsoft.com/download):
    dotnet tool install -g ilspycmd
Then:
    python tools/inspect_install.py        (once, creates inspection/game_path.txt)
    python tools/decompile_key_classes.py [path\\to\\DreadMist]

Everything goes to ./inspection/ which is git-ignored. Never commit it.
"""
import shutil, subprocess, sys, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "inspection"
FULL = OUT / "decompiled_full"
SEL = OUT / "decompiled_selected"

# File names (type names) found in the game's own source-path strings.
WANTED = [
    "ConsoleCommands", "CommandButtonsUI",
    "PerksWindowUI", "PerkDisplayUI", "PerkUpgradeEntryUI", "PerkUpgradeListUI",
    "PerkScrollListUI", "PerkData", "PerkDefinition", "PerkSave",
    "DialogueWindowUI", "NPC_BehaviorDialogue", "NPC_BehaviorTrader", "NPC_BehaviorBuying",
    "TradingMenuInventoryUI", "InteractableShopCounterController",
    "ConditionDisplay", "ConditionIndicator", "Condition",
    "StatEffectInstance", "SentientStatEffectContainer", "StatModifiers",
    "StatEffectUI", "StatEffectScrollListUI", "StatEffectsGridUI", "EntityActiveStatsUI",
    "CharacterStatsWindowUI", "StatsElementUI", "DialogueActionSanity",
    "DialogueResponse", "DialogueNode", "DialogueTreeDefinition", "DialoguePackageDefinition",
    "DialogueBranch", "IntButtonTypedController", "AttributeData", "SkillData", "SentientLevelData",
    "EntitySentientPlayerData", "ResourceManager", "ItemDefinition", "ItemListDefinition",
    "PlayerManager", "PlayerController", "BaseWindowUI", "GameHUD", "Localizer", "StatEffectValue",
    "StatEffectDefinition", "NPC_Controller", "ItemData", "InfoDisplayUI",
    "ItemEquipmentData", "InventoryHumanoidData", "ItemContextMenuUI", "HoverToolTipUI",
]

FAMILY_WORDS = ("Interactable", "Forag", "Growable", "Bush", "Dig", "Shovel", "Fishing", "Bait",
                "NPC_Behavior", "NPC_Property", "WorldManager", "SaveGame", "GameSave", "SaveManager", "InteractionController", "ResourceNodeDefinition", "ResourceDefinition", "ToolType", "CharacterDefinition", "TraderDefinition", "Schedule")


def main():
    gp = Path(sys.argv[1]) if len(sys.argv) > 1 else None
    if gp is None:
        f = OUT / "game_path.txt"
        if not f.exists():
            sys.exit("Run tools/inspect_install.py first, or pass the DreadMist folder path.")
        gp = Path(f.read_text(encoding="utf-8").strip())
    dll = gp / "DreadMist_Data" / "Managed" / "Assembly-CSharp.dll"
    if not dll.exists():
        sys.exit(f"Not found: {dll}")
    tool = shutil.which("ilspycmd")
    if not tool:
        sys.exit("ilspycmd not found. Install the .NET SDK, then run: dotnet tool install -g ilspycmd\n"
                 "(restart the terminal afterwards so PATH updates)")
    FULL.mkdir(parents=True, exist_ok=True)
    print("Decompiling (can take a few minutes)...")
    subprocess.run([tool, "-p", "-o", str(FULL), str(dll)], check=True)

    SEL.mkdir(exist_ok=True)
    found, missing = [], []
    cs = list(FULL.rglob("*.cs"))
    for name in WANTED:
        hits = [p for p in cs if p.stem == name or p.stem.startswith(name + "`")]
        (found if hits else missing).append(name)
        for h in hits:
            shutil.copy2(h, SEL / h.name)
    # Whole families (names containing these words): foraging/digging/fishing/growth and NPC behaviour/shops.
    for p in cs:
        if any(w in p.stem for w in FAMILY_WORDS):
            shutil.copy2(p, SEL / p.name)
    # include every ConditionIndicator*/Condition* sibling: they define the red requirement text
    for p in cs:
        if p.stem.startswith(("ConditionIndicator", "Condition")) or p.stem.startswith("DialogueAction"):
            shutil.copy2(p, SEL / p.name)
    zpath = OUT / "decompiled_selected.zip"
    with zipfile.ZipFile(zpath, "w", zipfile.ZIP_DEFLATED) as z:
        for p in SEL.glob("*.cs"):
            z.write(p, p.name)
    print(f"Bundled {len(list(SEL.glob('*.cs')))} files -> {zpath}")
    if missing:
        print("Names not found (fine, may live in a different assembly):", ", ".join(missing))
    print("Please send decompiled_selected.zip (it contains game source code - share it only with me, never commit it).")


if __name__ == "__main__":
    main()
