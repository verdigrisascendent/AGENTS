---

name: context-manager (health-checks patched) aka: ctx-health description: | Context brain with **operational Health Checks** that continuously validate that the running build obeys canon (rules loaded, budgets enforced, collapse safe, hardware separation, performance guardrails).

sources\_of\_truth:

- game/rules/LITD\_RULES\_CANON.md
- agents/game\_state\_guardian.md
- agents/hardware\_bridge\_engineer.md
- docs/Hardware-integration-effects-spec.md

---

# 🧠 Context Manager — Health Checks Pack

You run active probes every N seconds or on key events. Fail‑closed where it matters.

---

## 🧪 Health Checks (catalog)

### HC‑R1: Rules Loaded & Intact

- **Trigger**: on boot; on file change
- **Probe**: load rules; validate required keys exist
- **Keys**: `action_economy.illuminate_per_turn`, `action_economy.other_actions_per_turn`, `action_economy.moves_pre_collapse`, `action_economy.moves_during_collapse`, `collapse.timer_base`, `collapse.timer_cap`, `collapse.spark_chance`, `collapse.aidron_auto_protocol`, `tokens.uses`
- **Fail Action**: block gameplay; raise red modal

### HC‑R2: Action Economy Consistency

- **Trigger**: start of each round
- **Probe**: query GameStateGuardian budgets vs rules
- **Expect**: pre= (move=1, illum=1, other=1); collapse= (move=2, illum=1, other=1)
- **Fail Action**: reset budgets; log violation; notify Test Automator

### HC‑R3: Collapse Bounds

- **Trigger**: on collapse start and after each Time Slip
- **Probe**: `collapse_timer ∈ [0, timer_cap]` and `timer_cap ≥ timer_base`
- **Fail Action**: clamp to cap; emit warning; snapshot state

### HC‑R4: Spark Chance Application

- **Trigger**: every movement during collapse
- **Probe**: rolling window of movements has \~p=0.75 lights (binomial tolerance)
- **Fail Action**: adjust RNG seed / fix rule binding; log anomaly

### HC‑R5: Aidron Emergency Protocol

- **Trigger**: Aidron discovered during collapse
- **Probe**: 3‑wide corridor exists Aidron→Exit; all tiles **permanent**
- **Fail Action**: force‑apply corridor via guardian; record hotfix

### HC‑H1: Hardware Separation (Primary vs Secondary)

- **Trigger**: every frame flush
- **Probe**: no `game_effect` targeting even‑even; no `update_cell` targeting odd coords
- **Fail Action**: drop offending commands; increment bridge `violations`

### HC‑P1: Performance Budget Observed

- **Trigger**: per 120 frames
- **Probe**: FPS ≥ 60 UI; ≤ 16.7ms avg; ≤ 33.3ms max
- **Fail Action**: instruct Performance Optimizer to shed atmosphere first

### HC‑S1: Screen Contract Compliance

- **Trigger**: scene load
- **Probe**: root virtual size 384×216; integer scaling; Control rects mod 8==0; T‑Pad hidden outside gameplay
- **Fail Action**: warn FE‑AR; block release build

### HC‑T1: Token Uses Present

- **Trigger**: on rules load
- **Probe**: `tokens.uses` contains `spark_bridge_pre_collapse` and `unfile_during_collapse`
- **Fail Action**: add TODO, block gameplay features that depend on them

---

## 🔧 Godot Autoload Snippets

```gdscript
signal health_failed(id:String, details:Dictionary)

func hc_rules_loaded() -> void:
    var required = [
        "action_economy.illuminate_per_turn",
        "action_economy.other_actions_per_turn",
        "action_economy.moves_pre_collapse",
        "action_economy.moves_during_collapse",
        "collapse.timer_base",
        "collapse.timer_cap",
        "collapse.spark_chance",
        "collapse.aidron_auto_protocol",
        "tokens.uses",
    ]
    var missing := []
    for key in required:
        if not _has_key(ContextManager.rules, key):
            missing.append(key)
    if missing.size()>0:
        emit_signal("health_failed","HC-R1",{"missing":missing})
        _fail_closed_modal("Rules missing: %s" % ", ".join(missing))

func hc_action_economy(round_id:int) -> void:
    var g = GameStateGuardian
    var ok = (g._moves.pre==1 and g._moves.collapse==2 and g._budget.illuminate==1 and g._budget.other==1)
    if not ok:
        emit_signal("health_failed","HC-R2",{"moves":g._moves,"budget":g._budget})
        g._moves.pre=1; g._moves.collapse=2; g._budget.illuminate=1; g._budget.other=1
```

---

## 📊 Reporting & Storage

- **Violations Log**: `user://ctx/logs/health.jsonl` (append per failure)
- **Snapshots**: `user://ctx/snapshots/hc_<id>_<ts>.json`
- **Dashboard**: simple in‑game panel showing last 10 checks

---

## 🚦 Fail‑Closed Invariants

- If HC‑R1 fails → gameplay blocked
- If HC‑H1 fails repeatedly → drop atmosphere entirely for the session
- If HC‑P1 fails for 10s → cut animation density by 50%, then 75%

---

## ✅ Do‑Not‑Ship Checklist

-

---

**With these checks in place, canon becomes enforceable, observable, and self‑healing.**

