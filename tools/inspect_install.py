#!/usr/bin/env python3
"""DreadMist-QOL installation inspector (step 1 of the plan).

Read-only. Finds the game, identifies the Unity runtime (Mono vs IL2CPP),
detects mod loaders, lists files, and pulls candidate console-command
strings and StreamingAssets definition names into ./inspection/ (git-ignored).

Usage:  python tools/inspect_install.py [path\\to\\DreadMist]
No path given -> tries Steam library detection, then asks you.
"""
import os, re, sys, json, platform
from pathlib import Path

APP_ID = "2285190"
OUT = Path(__file__).resolve().parent.parent / "inspection"


def steam_roots():
    roots = []
    if platform.system() == "Windows":
        try:
            import winreg
            for hive, key in ((winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Valve\Steam"),
                              (winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam")):
                try:
                    with winreg.OpenKey(hive, key) as k:
                        for name in ("InstallPath", "SteamPath"):
                            try:
                                roots.append(Path(winreg.QueryValueEx(k, name)[0]))
                            except OSError:
                                pass
                except OSError:
                    pass
        except ImportError:
            pass
    else:
        h = Path.home()
        roots += [h / ".steam/steam", h / ".local/share/Steam"]
    return roots


def find_game():
    for root in steam_roots():
        libs = [root]
        vdf = root / "steamapps" / "libraryfolders.vdf"
        if vdf.exists():
            libs += [Path(p.replace("\\\\", "\\")) for p in
                     re.findall(r'"path"\s+"([^"]+)"', vdf.read_text(errors="ignore"))]
        for lib in libs:
            manifest = lib / "steamapps" / f"appmanifest_{APP_ID}.acf"
            if manifest.exists():
                d = re.search(r'"installdir"\s+"([^"]+)"', manifest.read_text(errors="ignore"))
                if d and (lib / "steamapps" / "common" / d.group(1)).is_dir():
                    return lib / "steamapps" / "common" / d.group(1)
    return None


def strings(data, minlen=4):
    for m in re.finditer(rb"[\x20-\x7e]{%d,}" % minlen, data):
        yield m.group().decode()
    for m in re.finditer(rb"(?:[\x20-\x7e]\x00){%d,}" % minlen, data):
        yield m.group().decode("utf-16le")


def main():
    game = Path(sys.argv[1]) if len(sys.argv) > 1 else find_game()
    if not game:
        game = Path(input("Game folder not found automatically. Paste the DreadMist folder path: ").strip('" '))
    if not game.is_dir():
        sys.exit(f"Not a folder: {game}")
    OUT.mkdir(exist_ok=True)
    report = {"game_dir_name": game.name}

    exes = [p.name for p in game.glob("*.exe")]
    data_dirs = [p for p in game.glob("*_Data") if p.is_dir()]
    data = data_dirs[0] if data_dirs else None
    report["exes"] = exes
    report["data_dir"] = data.name if data else None
    report["arch64"] = (game / "UnityPlayer.dll").exists()
    mono = bool(data and (data / "Managed" / "Assembly-CSharp.dll").exists())
    il2cpp = (game / "GameAssembly.dll").exists()
    report["runtime"] = "IL2CPP" if il2cpp else "Mono" if mono else "unknown"
    report["mod_loaders"] = [n for n in ("BepInEx", "MelonLoader", "winhttp.dll", "doorstop_config.ini", "version.dll")
                             if (game / n).exists()]
    report["has_streamingassets"] = bool(data and (data / "StreamingAssets").exists())

    # full file listing (names + sizes only)
    with open(OUT / "file_listing.txt", "w", encoding="utf-8") as f:
        for p in sorted(game.rglob("*")):
            if p.is_file():
                f.write(f"{p.relative_to(game)}\t{p.stat().st_size}\n")

    # StreamingAssets/Definitions overview
    if data and (data / "StreamingAssets").exists():
        defs = {}
        for p in (data / "StreamingAssets").rglob("*"):
            if p.is_file():
                defs.setdefault(str(p.parent.relative_to(data / "StreamingAssets")), []).append(p.name)
        (OUT / "definitions_index.json").write_text(json.dumps(defs, indent=1))
        report["definition_folders"] = {k: len(v) for k, v in defs.items()}

    # candidate command strings from managed assembly (Mono only)
    if mono:
        blob = (data / "Managed" / "Assembly-CSharp.dll").read_bytes()
        seen = set()
        pat = re.compile(r"(Give|Add|Remove|Set|Revive|Spawn|Teleport|Kill|Heal|Unlock|Console|Cheat|Debug|Command)", re.I)
        for s in strings(blob):
            if pat.search(s) and len(s) < 80:
                seen.add(s)
        (OUT / "command_candidates.txt").write_text("\n".join(sorted(seen)), encoding="utf-8")
        report["command_candidate_count"] = len(seen)
    elif il2cpp:
        report["note"] = ("IL2CPP: Assembly-CSharp.dll is not present. Needs Il2CppDumper "
                          "(GameAssembly.dll + global-metadata.dat) for method/string extraction.")

    (OUT / "report.json").write_text(json.dumps(report, indent=1))
    print(json.dumps(report, indent=1))
    print(f"\nWrote results to: {OUT}")
    print("Please share report.json, file_listing.txt, definitions_index.json and command_candidates.txt.")


if __name__ == "__main__":
    main()
