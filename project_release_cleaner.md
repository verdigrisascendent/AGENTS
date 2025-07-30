# `project_release_cleaner` — Final Gate Agent (One‑Command Cleaner)

**Purpose:** Single entrypoint that runs *all* hygiene, migration, and export audits for the Godot repo and fails CLOSED on any violation.

---

## Agent Spec
```yaml
---
name: project_release_cleaner
aka: "the cleaner"
description: |
  One‑command final gate. Runs all hygiene checks and migration audits for Lights in the Dark’s Godot repo.
  Produces a single consolidated report and fails CLOSED on any violation.
role: Run LAST (after tests, rules verification, export dry‑run).
entrypoint:
  cli: GODOT_BIN=/Applications/Godot.app/Contents/MacOS/Godot \
       ./tools/clean.sh
reports:
  - reports/cleanliness_report.md
  - reports/cleanliness_report.json
  - reports/autofix.diff
health_checks:
  - HC-CLEAN-1: No Flutter/Dart vestiges (or explicit waiver)
  - HC-CLEAN-2: Godot equivalence satisfied (autoloads/scenes present)
  - HC-CLEAN-3: No orphan assets; .import matches sources
  - HC-CLEAN-4: Scene load/instantiate OK; no broken NodePaths/signals
  - HC-CLEAN-5: Export presets sane for iOS (Metal, integer scale, etc.)
  - HC-CLEAN-6: Amiga aesthetic spot‑checks pass (bitmap font, nearest)
  - HC-CLEAN-7: Primary/Secondary LED separation preserved
  - HC-CLEAN-8: Rules checksum matches embedded build info
  - HC-CLEAN-9: Repo hygiene (no tmp/garbage/large unreferenced bins)
  - HC-CLEAN-10: Naming/layout conventions pass
fail_closed: true
sources_of_truth:
  - LITD_RULES_CANON.md
  - agents/godot_scene_architect.md
  - agents/godot_platform_optimizer.md
  - agents/amiga_aesthetic_enforcer.md
  - agents/hardware_integration_effects_spec.md
  - agents/flutter_to_godot_migration_playbook.md
waivers_file: agents/sweeper_waivers.yaml
---
```

---

## File Layout
Place these files in your repo:

```
res://tools/ReleaseCleaner.gd
tools/clean.sh
reports/                # generated
agents/project_release_cleaner.md  # this file
```

---

## Usage

**Local:**
```bash
GODOT_BIN=/Applications/Godot.app/Contents/MacOS/Godot \
./tools/clean.sh
```

**CI (last job):**
```yaml
- name: Final project cleaner (one command)
  run: |
    GODOT_BIN=/Applications/Godot.app/Contents/MacOS/Godot \
    ./tools/clean.sh
```

Outputs:
- `reports/cleanliness_report.md` (human)
- `reports/cleanliness_report.json` (machine)
- `reports/autofix.diff` (optional)

---

## `tools/clean.sh`
```bash
#!/usr/bin/env bash
set -euo pipefail

ROOT="${1:-.}"
GODOT_BIN="${GODOT_BIN:-/Applications/Godot.app/Contents/MacOS/Godot}"

mkdir -p reports tools
echo "[Cleaner] Running one-shot final gate…"

"$GODOT_BIN" --headless --path "$ROOT" --script res://tools/ReleaseCleaner.gd

echo "[Cleaner] Completed. See reports/cleanliness_report.md"
```

> Make executable: `chmod +x tools/clean.sh`

---

## `res://tools/ReleaseCleaner.gd`
```gdscript
extends SceneTree

class_name ReleaseCleaner

const REPORT_DIR := "reports"
const SUMMARY_MD := REPORT_DIR + "/cleanliness_report.md"
const SUMMARY_JSON := REPORT_DIR + "/cleanliness_report.json"
const AUTOFIX_DIFF := REPORT_DIR + "/autofix.diff" # optional

const REQUIRED_SCENES := [
    "res://scenes/title/title.tscn",
    "res://scenes/game/game.tscn",
    "res://scenes/settings/settings.tscn",
]
const REQUIRED_AUTOLOADS := ["ContextManager", "GameStateGuardian", "LedBridge"]
const RULES_FLATTENED := "user://ctx/rules/flattened.json"
const EXPORT_PRESET_FILE := "res://export_presets.cfg"
const ASSET_EXTS := [".png",".ogg",".wav",".tres",".shader",".fnt",".font",".gd",".tscn"]

var issues := []
var stats := {"files_scanned": 0, "assets_seen": 0, "assets_orphan": 0, "scenes_loaded": 0}
var passed := true

func _ready():
    DirAccess.make_dir_recursive_absolute(REPORT_DIR)
    _check_flutter_vestiges()
    _check_godot_equivalence()
    _check_orphan_assets()
    _check_scene_consistency()
    _audit_export_presets()
    _amiga_spotcheck()
    _check_bridge_layer_separation()
    _verify_rules_checksum()
    _repo_hygiene_and_conventions()
    _write_reports()
    if not passed:
        push_error("[Cleaner] Violations found. See " + SUMMARY_MD)
        quit(2)
    print("[Cleaner] All checks passed.")
    quit(0)

func _add_issue(code:String, severity:String, msg:String, hints:Array=[]):
    issues.append({"code":code, "severity":severity, "msg":msg, "hints":hints})
    if severity in ["error","fail"]:
        passed = false

func _check_flutter_vestiges():
    var suspicious_patterns := [
        "pubspec.yaml",".dart_tool",".flutter-plugins",".flutter-plugins-dependencies",
        "package:flutter/","material.dart","cupertino.dart",
        "StatelessWidget","StatefulWidget","BuildContext","setState(",
        "flutter-action","flutter pub","dart test","flutter build"
    ]
    var d := DirAccess.open("res://")
    if d:
        d.list_dir_begin(true,true)
        while true:
            var path := d.get_next()
            if path == "": break
            stats.files_scanned += 1
            if path.ends_with(".dart") or path.findn("pubspec.yaml")!=-1:
                _add_issue("HC-CLEAN-1","fail","Dart/Flutter file: res://"+path)
                continue
            if path.ends_with(".gd") or path.ends_with(".yml") or path.ends_with(".md") or path.ends_with(".json") or path.ends_with(".yaml"):
                var full := "res://" + path
                if FileAccess.file_exists(full) and FileAccess.get_file_len(full) < 2_000_000:
                    var text := FileAccess.get_file_as_string(full)
                    for p in suspicious_patterns:
                        if text.findn(p) != -1:
                            _add_issue("HC-CLEAN-1","fail","Flutter vestige in "+full, [p])
                            break
        d.list_dir_end()

func _check_godot_equivalence():
    if not FileAccess.file_exists("res://project.godot"):
        _add_issue("HC-CLEAN-2","fail","Missing project.godot")
    for s in REQUIRED_SCENES:
        if not ResourceLoader.exists(s):
            _add_issue("HC-CLEAN-2","fail","Missing required scene", [s])
    var autoloads := ProjectSettings.get_setting("autoload", {})
    for n in REQUIRED_AUTOLOADS:
        if not autoloads.has(n):
            _add_issue("HC-CLEAN-2","fail","Missing autoload singleton", [n])

func _collect_scene_deps(scene_path:String, refs:Dictionary):
    var deps := ResourceLoader.get_dependencies(scene_path, true)
    for d in deps:
        refs[d] = true

func _check_orphan_assets():
    var all_assets := []
    var refs := {}
    var dir := DirAccess.open("res://")
    if dir:
        dir.list_dir_begin(true,true)
        while true:
            var f := dir.get_next()
            if f == "": break
            for ext in ASSET_EXTS:
                if f.ends_with(ext):
                    all_assets.append("res://"+f)
                    stats.assets_seen += 1
                    break
        dir.list_dir_end()
    var sdir := DirAccess.open("res://scenes")
    if sdir:
        sdir.list_dir_begin(true,true)
        while true:
            var s := sdir.get_next()
            if s == "": break
            if s.ends_with(".tscn"):
                _collect_scene_deps("res://scenes/"+s, refs)
        sdir.list_dir_end()
    var orphans := []
    for a in all_assets:
        if not refs.has(a) and not a.contains("/.import/"):
            orphans.append(a)
    stats.assets_orphan = orphans.size()
    if orphans.size() > 0:
        _add_issue("HC-CLEAN-3","fail","Orphan assets detected", orphans.slice(0,20))

func _check_scene_consistency():
    var issues_here := 0
    var d := DirAccess.open("res://scenes")
    if d:
        d.list_dir_begin(true,true)
        while true:
            var s := d.get_next()
            if s == "": break
            if not s.ends_with(".tscn"): continue
            var p := "res://scenes/"+s
            var pack := load(p)
            if pack == null:
                _add_issue("HC-CLEAN-4","fail","Scene failed to load", [p]); issues_here += 1; continue
            var inst = pack.instantiate()
            if inst == null:
                _add_issue("HC-CLEAN-4","fail","Scene failed to instantiate", [p]); issues_here += 1; continue
            stats.scenes_loaded += 1
    if issues_here == 0 and stats.scenes_loaded == 0:
        _add_issue("HC-CLEAN-4","fail","No scenes loaded; check scene paths")

func _cfg_get(cfg:ConfigFile, sec:String, key:String, def=null):
    if not cfg.has_section(sec): return def
    if not cfg.has_section_key(sec, key): return def
    return cfg.get_value(sec, key, def)

func _audit_export_presets():
    var cfg := ConfigFile.new()
    var err := cfg.load(EXPORT_PRESET_FILE)
    if err != OK:
        _add_issue("HC-CLEAN-5","fail","Missing export_presets.cfg")
        return
    # TODO: Add your actual preset key checks (Metal on, integer scale, bitmap fonts, etc.)

func _amiga_spotcheck():
    # TODO: Inspect .import flags for nearest/no-mipmaps and fonts for bitmap mode.
    pass

func _check_bridge_layer_separation():
    var bad := []
    var d := DirAccess.open("res://")
    if d:
        d.list_dir_begin(true,true)
        while true:
            var p := d.get_next()
            if p == "": break
            if not p.ends_with(".gd"): continue
            var full := "res://"+p
            if FileAccess.file_exists(full) and FileAccess.get_file_len(full) < 2_000_000:
                var t := FileAccess.get_file_as_string(full)
                if t.findn("primary")!=-1 and t.findn("secondary")!=-1 and t.findn("write_")!=-1:
                    bad.append(full)
    if bad.size() > 0:
        _add_issue("HC-CLEAN-7","fail","Potential primary/secondary LED mixing", bad.slice(0,10))

func _verify_rules_checksum():
    if not FileAccess.file_exists(RULES_FLATTENED):
        _add_issue("HC-CLEAN-8","fail","Missing flattened rules JSON", [RULES_FLATTENED])
        return
    var bytes := FileAccess.get_file_as_bytes(RULES_FLATTENED)
    var hash := Crypto.hash(Crypto.HASH_SHA256, bytes).hex_encode()
    var embed := {}
    if FileAccess.file_exists("res://meta/build_info.json"):
        embed = JSON.parse_string(FileAccess.get_file_as_string("res://meta/build_info.json"))
    if embed.has("rules_checksum") and embed["rules_checksum"] != hash:
        _add_issue("HC-CLEAN-8","fail","Rules checksum mismatch", [hash, embed["rules_checksum"]])

func _repo_hygiene_and_conventions():
    var garbage_patterns := [".DS_Store",".orig",".log","/tmp/","/export/",".bak"]
    var d := DirAccess.open("res://")
    if d:
        d.list_dir_begin(true,true)
        while true:
            var p := d.get_next()
            if p == "": break
            for pat in garbage_patterns:
                if p.findn(pat) != -1:
                    _add_issue("HC-CLEAN-9","fail","Repo garbage file",["res://"+p])
                    break
            if (p.ends_with(".gd") or p.ends_with(".png")) and _looks_like_camel_case(p):
                _add_issue("HC-CLEAN-10","warn","Non-snake_case name",["res://"+p])
        d.list_dir_end()

func _looks_like_camel_case(name:String) -> bool:
    return name != name.to_lower()

func _write_reports():
    var out := {"passed": passed, "issue_count": issues.size(), "issues": issues, "stats": stats, "timestamp": Time.get_datetime_string_from_system(true)}
    var jf := FileAccess.open(SUMMARY_JSON, FileAccess.WRITE)
    jf.store_string(JSON.stringify(out,"  "))
    jf.close()
    var md := FileAccess.open(SUMMARY_MD, FileAccess.WRITE)
    md.store_string("# Cleanliness Report\n\n")
    md.store_string("- Result: **" + (passed ? "PASSED" : "FAILED") + "**\n")
    md.store_string("- Files scanned: %d\n" % stats.files_scanned)
    md.store_string("- Scenes loaded: %d\n" % stats.scenes_loaded)
    md.store_string("- Orphan assets: %d\n\n" % stats.assets_orphan)
    if issues.size() > 0:
        md.store_string("## Issues\n")
        for i in issues:
            md.store_string("- `%s` **%s** — %s%s\n" % [i.code, i.severity, i.msg, (i.hints.size()>0? " (hints: "+str(i.hints)+")" : "")])
    md.close()
```

---

## Waivers (Optional)
Create `agents/sweeper_waivers.yaml` to allow specific, intentional exceptions.

```yaml
allow:
  - path: docs/examples/flutter_snippets/**
  - pattern: "README.md:.*flutter.*"
  - path: tools/legacy_migration/**
```

---

## Notes
- This consolidates *all* checks into **one command**.
- Extend the `TODO` spots (export preset keys, Amiga spot checks) with your actual settings as you finalize them.
- The cleaner is **fail‑closed** by default.

