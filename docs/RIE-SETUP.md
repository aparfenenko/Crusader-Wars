# Realms in Exile (The Dawnless Days) — mac setup guide

The focus playthrough for the mac port. Everything the pipeline needs is already in
this repository except the games themselves and their mods.

## 0. Prerequisites

- macOS arm64 with **native Steam versions of CK3 and Total War: ATTILA**.
- This repository cloned and built (`dotnet build CrusaderWars.sln`).
- The workspace pieces ship with the repo: `unit mappers/`, `data/schema_att.ron`,
  `data/battle files/`, `settings/schemas/`.
- rpfm (pack step): `tools/rpfm_cli` wraps the Windows rpfm_cli.exe v4.7.4 through
  CrossOver (`CX_BOTTLE` selects the bottle; use a GUI-created bottle that has the
  Windows system DLLs). Register it once:
  `dotnet run --project CrusaderWars.MacCli -- set-path rpfmCli "<repo>/tools/rpfm_cli"`

## 1. Attila mods (The Dawnless Days packs)

The RIE mapper (`unit mappers/OfficialCW_RealmsInExile_TheDawnlessDays/Mods.xml`)
requires these 10 packs in Attila. Subscribe/download them, then put the `.pack`
files into Attila's `data/` folder:

| Pack | Source |
|---|---|
| rom_exe_extended_elves.pack | Steam Workshop (id 2746623268) |
| war_for_the_mountains.pack | Steam Workshop (id 3115874646) |
| the_last_breath.pack | Steam Workshop (id 2780897519) |
| fiefs_of_gondor.pack | moddb.com/mods/fiefs-of-gondor |
| tdd_pack1_main_1.0.0.pack … tdd_pack6_weather_1.0.0.pack | nexusmods.com/totalwarattila/mods/1 |

Then enable them in `~/Library/Application Support/The Creative Assembly/Attila/scripts/user.script.txt`
and add the battle pack line (the player manages mods; the launcher never touches this file):

```
mod rom_exe_extended_elves.pack;
mod war_for_the_mountains.pack;
mod the_last_breath.pack;
mod fiefs_of_gondor.pack;
mod tdd_pack1_main_1.0.0.pack;
mod tdd_pack2_battles_1.0.0.pack;
mod tdd_pack3_campaign_1.0.0.pack;
mod tdd_pack4_models_1.0.0.pack;
mod tdd_pack5_buildings_1.0.0.pack;
mod tdd_pack6_weather_1.0.0.pack;
mod CrusaderConflicts.pack;
```

The `mod CrusaderConflicts.pack;` line is required — without it Attila will not load
the battle (the CLI/GUI warns about it before launching).

## 2. CK3 mods

Install into the CK3 mod folder (`~/Documents/Paradox Interactive/Crusader Kings III/mod/`):

1. **Realms In Exile** itself (Steam Workshop) — total conversion.
2. **Crusader Conflicts** base mod — copy `ck3_mods/crusader_conflicts` + `crusader_conflicts.mod`.
3. **RIE compatibility patch** — copy `ck3_mods/crusader_conflicts_realms_in_exile_compat_patch`
   + its `.mod` file.

Enable all three in the same playset. The compat patch writes the battle dump
(`Keyword:CRUSADERCONFLICTS` + `debug_log_scopes`) to `console_history.txt` when you
press the Crusader Conflicts button in-game.

## 3. Paths

Either the GUI (Path Setup → edit/Browse → Save paths) or the CLI:

```bash
dotnet run --project CrusaderWars.MacCli -- detect            # Steam autodetect first
dotnet run --project CrusaderWars.MacCli -- set-path ck3App "/path/to/Crusader Kings III.app"     # if GOG/custom
dotnet run --project CrusaderWars.MacCli -- set-path attilaApp "/path/to/Attila.app"
```

Verify everything:

```bash
dotnet run --project CrusaderWars.MacCli -- detect            # all OK / only expected MISSING
dotnet run --project CrusaderWars.MacCli -- check-mappers     # 9/9 valid (RIE: 10 packs)
```

## 4. Battle cycle

```bash
dotnet run --project CrusaderWars.MacCli -- run               # full single-process cycle
```

or the Avalonia app: `dotnet run --project CrusaderWars.Avalonia` → **Start Battle**
(Options and Path Setup from the main window). The cycle: watches `console_history.txt`
→ parses the battle dump → reads armies from `last_save.ck3` → generates the Attila
battle files and `CrusaderConflicts.pack` → suspends CK3 → launches Attila → waits for
the battle to end → writes casualties, character fates and war score back into
`battle_results.ck3`.

For step-by-step debugging: `parse-console`, `read-save`, `generate-battle`, `wait-battle`.

## 5. Logs and diagnostics

- `data/debug.log` — every step (paths, parsed units, mapper decisions);
- `data/battle.log` — battle header, pre-battle composition, post-battle casualty report;
- `data/save_file_data/*.txt` — extracted/edited gamestate fragments;
- the Avalonia app shows Info/Warn/Error live in its log panel.

## 6. RIE-specific caveats (known, non-blocking)

- The RIE mapper has no `terrains/` folder — terrain/weather selection falls back to
  defaults (the code handles a missing folder gracefully).
- Mapper folder names are `OfficialCW_*` while some code special-cases `OfficialCC_*`
  names; those switches fall to defaults (slightly less loc/era tuning).
- The `SelectedCustomMapper` option stays empty for official playthroughs — the tag
  (`RealmsInExile`) comes from the playthrough setting, matched via `tag.txt`.
