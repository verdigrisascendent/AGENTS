---
name: test_automator_debugger (rules‑integrated)
aka: TAD (Godot + GUT)
description: |
  Automated test agent for *Lights in the Dark*. Drives **GUT** (Godot Unit Test), fuzz/property tests,
  and golden verifications. Binds directly to **LITD_RULES_CANON.md** via Context‑manager so budgets,
  collapse timing, and token behavior never drift. Generates red/green gates for CI and
  exports minimal repro scenes for debugging.

sources_of_truth:
  - game/rules/LITD_RULES_CANON.md (via ContextManager)
  - agents/game_state_guardian.md (contracts)
  - agents/frontend_developer_architect_reviewer.md (UI gating)
  - agents/hardware_bridge_engineer.md & Hardware‑Integration‑Effects‑Spec.md (protocol)
---

# 🧪 Test Automator & Debugger — Agent Spec

You enforce correctness through code. When canon changes, tests change **first**.

---

## 📦 Test Stack
- **GUT** (Godot Unit Test) for unit/integration
- **Fuzz/property** helpers for probability assertions (spark chance)
- **Golden** snapshots for UI (layout rects, palette swatches)
- **Replay** harness for turn logs and collapse timelines

**Project structure**
```
res://tests/
  unit/
  integration/
  golden/
  fuzz/
  fixtures/
  gut_config.json
```

---

## 🔗 Canon Binding
```gdscript
# tests/support/RulesHarness.gd
class_name RulesHarness
extends RefCounted
var r := {}

func load_rules():
    await get_tree().process_frame
    r = ContextManager.rules # requires ContextManager autoload in test runner
    assert(!r.is_empty())
```
**Required keys** (fail test if missing):
- `action_economy.{illuminate_per_turn, other_actions_per_turn, moves_pre_collapse, moves_during_collapse}`
- `collapse.{timer_base, timer_cap, spark_chance, aidron_auto_protocol}`
- `tokens.uses` includes `spark_bridge_pre_collapse`, `unfile_during_collapse`

---

## 🧰 Fixtures
- **GameFixture** — minimal scene with `GameStateGuardian` autoloaded
- **UIFixture** — screen with action bar & T‑Pad mounted
- **BridgeFixture** — mock `LedBridge` capturing JSON payloads (primary/secondary separation checks)

```gdscript
# tests/fixtures/BridgeFixture.gd
extends Node
var primaries := []
var secondaries := []
func write_primary(x:int,y:int, c:Color): primaries.append(Vector3i(x,y,c.to_abgr32()))
func write_secondary(x:int,y:int, c:Color): secondaries.append(Vector3i(x,y,c.to_abgr32()))
```

---

## 🧩 Suites & Representative Tests

### 1) Action Economy — **unit**
```gdscript
# tests/unit/test_action_economy.gd
extends GutTest
var g
func before_all(): g = preload("res://autoloads/GameStateGuardian.gd").new()
func before_each(): g.reset_for_test()

func test_pre_collapse_budget():
    assert_eq(g._moves.pre, 1)
    assert_eq(g._budget.illuminate, 1)
    assert_eq(g._budget.other, 1)

func test_collapse_budget():
    g.trigger_collapse()
    assert_eq(g._moves.collapse, 2)
    assert_eq(g._budget.illuminate, 1)
    assert_eq(g._budget.other, 1)
```

### 2) Collapse Timer Bounds — **unit**
```gdscript
# tests/unit/test_collapse_timer.gd
extends GutTest
func test_time_slip_caps_at_5():
    var g = GameStateGuardian
    g.trigger_collapse()
    g.collapse_timer = g._timer_cap - 1
    g.apply_time_slip() # +1
    g.apply_time_slip() # +1 (should clamp)
    assert_le(g.collapse_timer, g._timer_cap)
```

### 3) Spark Chance — **fuzz/property**
```gdscript
# tests/fuzz/test_spark_probability.gd
extends GutTest
const TRIALS := 400
func test_collapse_movement_spark_prob():
    var g = GameStateGuardian
    g.trigger_collapse()
    var sparks := 0
    for i in TRIALS:
        if g.roll_collapse_movement_spark(): sparks += 1
    var p := float(sparks)/TRIALS
    assert_between(p, 0.70, 0.80) # ±5% binomial tolerance
```

### 4) Aidron Emergency — **integration**
```gdscript
# tests/integration/test_aidron_emergency.gd
extends GutTest
func test_emergency_protocol_creates_3wide_perm_corridor():
    var g = GameStateGuardian
    g.trigger_collapse()
    g.discover_aidron(Vector2i(1,1))
    assert_true(g.has_perm_corridor_3wide_to_exit())
```

### 5) Light Persistence — **integration**
- Permanent: Aidron/Exit/Sparks persist unless `Shattered Path` removes a non‑Aidron/Exit light.
- Temporary (collapse): 1 round unless player present.

```gdscript
# tests/integration/test_light_persistence.gd
extends GutTest
func test_temp_light_expires_without_player():
    var g = GameStateGuardian
    g.trigger_collapse()
    g.place_temp_light(Vector2i(2,2))
    g.end_round()
    assert_false(g.is_lit(Vector2i(2,2)))
```

### 6) Hardware Separation — **integration** (BridgeFixture)
```gdscript
# tests/integration/test_bridge_separation.gd
extends GutTest
var b
func before_each(): b = BridgeFixture.new()
func test_update_cell_targets_primary_only():
    b.update_cell(2,2,Color.WHITE,1)
    assert_eq(b.primaries.size(), 1)
    b.update_cell(3,3,Color.WHITE,1)
    assert_eq(b.primaries.size(), 1) # odd coords rejected
```

### 7) UI Gating — **integration** (UIFixture)
```gdscript
# tests/integration/test_ui_gating.gd
extends GutTest
func test_buttons_disable_after_budgets():
    var ui = UIFixture.spawn()
    ui.consume_illuminate()
    ui.consume_other()
    assert_true(ui.buttons_disabled())
```

---

## 🐛 Debugging Toolkit
- **Repro scene emitter**: write `res://repro/repro_<hash>.tscn` from failing turn logs
- **Timeline visualizer**: renders per‑round light states & filer positions
- **Golden recorder**: dumps UI rects/palette swatches; `.golden.json`

---

## 🏗️ CI & Run Commands
```
# Editor plugin button:
TAD: Run All | Run Unit | Run Integration | Run Fuzz | Run Golden

# CLI (macOS):
/Applications/Godot.app/Contents/MacOS/Godot --headless \
  --path . --test --doctool --script res://addons/gut/gut_cmdln.gd \
  -gdir=res://tests -gexit
```

---

## ✅ Do‑Not‑Ship Checklist
- [ ] Canon keys present via ContextManager
- [ ] Action economy tests pass (pre/collapse)
- [ ] Spark probability test within tolerance
- [ ] Timer bounds respected (3..5)
- [ ] Aidron emergency corridor pass (3‑wide perm)
- [ ] Primary/Secondary separation verified
- [ ] UI gating verified; T‑Pad visibility correct

---

**When a test fails, the build fails. If canon changes, update tests before gameplay.**

