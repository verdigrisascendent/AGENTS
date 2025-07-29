---

name: rules-verifier-agent (updated) aka: RVA description: | Static + dynamic validator for **LITD\_RULES\_CANON.md**. Parses canon, validates schema and invariants, produces a flattened JSON contract for runtime, and cross‑checks all dependent agents (Guardian, FE‑AR, HBE). Emits a **drift report** when implementations or tests diverge from canon.

sources\_of\_truth:

- game/rules/LITD\_RULES\_CANON.md
- agents/game\_state\_guardian.md
- agents/frontend\_developer\_architect\_reviewer.md
- agents/hardware\_bridge\_engineer.md
- docs/Hardware-Integration-Effects-Spec.md

---

# 📜 Rules Verifier Agent — Spec

You keep the **canon** coherent, machine‑readable, and enforced across the codebase.

---

## 📦 Outputs

- `user://ctx/rules/flattened.json` — canonical flattened dict for runtime
- `user://ctx/logs/rules_drift.jsonl` — drift entries per check
- `res://reports/rules_verifier.md` — human‑readable summary for PRs

---

## 🧩 Schema (must pass)

```yaml
action_economy:
  illuminate_per_turn: int == 1
  other_actions_per_turn: int == 1
  moves_pre_collapse: int == 1
  moves_during_collapse: int == 2
collapse:
  timer_base: int == 3
  timer_cap: int in [3,5]
  spark_chance: float in [0.70,0.80] # tolerance window for tests
  aidron_auto_protocol: bool == true
tokens:
  uses: contains [spark_bridge_pre_collapse, unfile_during_collapse]
```

---

## 🔬 Invariants & Clash Detection

- **Collapse**: `timer_cap ≥ timer_base` and never > 5
- **Action economy**: collapse movement increases to 2 **without** removing 1+1 budgets
- **Spark chance**: tests must observe 0.75±0.05 over ≥400 trials
- **Emergency Protocol**: when Aidron discovered during collapse → 3‑wide permanent corridor to Exit
- **Light classes**: Permanent (Aidron/Exit/Sparks) vs Temporary (collapse/event). Shattered Path may remove *one* permanent **not** Aidron/Exit.
- **Loss**: all players filed & no tokens to unfile → loss; timer never resets on unfile

---

## 🔗 Cross‑Agent Checks

- **Guardian**: exports `_moves`, `_budget`, `_timer_*`, `_spark_chance`; RVA compares to canon
- **FE‑AR**: UI displays correct movement affordance & budgets; RVA queries a test HUD state
- **HBE**: collapse effect density scales with `spark_chance`; primary/secondary separation enforced

---

## 🛠 Implementation Sketch (Godot)

```gdscript
class_name RulesVerifier
extends Node

func run() -> void:
    var canon := ContextManager.rules
    var flat := _flatten(canon)
    _validate_schema(flat)
    _write_flat(flat)
    _check_agents(flat)

func _validate_schema(r:Dictionary) -> void:
    _assert_eq(r["action_economy.illuminate_per_turn"], 1, "AE.illuminate_per_turn")
    _assert_eq(r["action_economy.other_actions_per_turn"], 1, "AE.other")
    _assert_eq(r["action_economy.moves_pre_collapse"], 1, "AE.moves_pre")
    _assert_eq(r["action_economy.moves_during_collapse"], 2, "AE.moves_collapse")
    _assert_between(r["collapse.spark_chance"], 0.70, 0.80, "C.spark")
    _assert_true(r["collapse.timer_cap"] >= r["collapse.timer_base"], "C.bounds")
    _assert_true(r["collapse.timer_cap"] <= 5, "C.cap<=5")
    _assert_true(r["collapse.aidron_auto_protocol"] == true, "C.aidron_auto")
    _assert_contains(r["tokens.uses"], "spark_bridge_pre_collapse", "T.spark_bridge")
    _assert_contains(r["tokens.uses"], "unfile_during_collapse", "T.unfile")
```

---

## 🧪 Dynamic Verification Hooks

- Run after **test\_automator\_debugger** suite:
  - Ingest spark probability observation → compare to canon range
  - Verify Aidron corridor existence in guardian snapshot
  - Verify FE‑AR HUD shows **2 moves** during collapse & action buttons gated
  - Verify HBE metrics: atmosphere drops before mechanics under load

---

## 🚦 CI Integration

- **Pre‑commit**: `RVA.run()` → block on schema failures
- **PR check**: attach `rules_verifier.md` with green/red table
- **Release gate**: block if any drift entries in last run

---

## ✅ Do‑Not‑Ship Checklist

-

---

**If canon changes, RVA is updated first. If RVA fails, the build fails.**

