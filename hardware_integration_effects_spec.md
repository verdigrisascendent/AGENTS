---

name: hardware\_integration\_effects\_spec aka: HIES version: 0.1 (Godot + ESP32/Teensy) description: | Canonical specification for mapping Lights in the Dark game state and events to the physical LED rig. Encodes: grid mapping, command protocol, effect library, timing, and rule bindings. **Primary LEDs** render game mechanics; **Secondary LEDs** render atmosphere — strictly separated.

sources\_of\_truth:

- agents/hardware\_bridge\_engineer.md (runtime implementation)
- game/rules/LITD\_RULES\_CANON.md (collapse & timing constraints)
- context-manager (for rule keys & drift detection)

---

# 🔌 Hardware Integration — Effects & Protocol (Canonical)

> **Goal:** A two-layer lighting system where mechanics (Primary) and atmosphere (Secondary) run independently but stay synchronized with game events.

---

## 1) Grid Mapping (Game → Physical)

**Game grid:** 8×6 (logical)

**Physical matrix:** 16×11 (LED coordinates `x∈[0..15], y∈[0..10]`)

**Assignment:**

- **Primary LEDs**: even coordinates (x%2==0 && y%2==0) — **game state only**
- **Secondary LEDs**: odd coordinates — **atmospheric effects only**

**Mapping function**

```gdscript
# Map game cell (gx,gy) where gx∈[0..7], gy∈[0..5] → primary LED (px,py)
# Places each cell on a 2×2 block in a 16×12 virtual, then clipped to 16×11 usable.
func map_game_to_primary(gx:int, gy:int) -> Vector2i:
    var px := clamp(gx * 2, 0, 15)   # even x
    var py := clamp(gy * 2, 0, 10)   # even y, top-left origin
    return Vector2i(px, py)
```

> If the physical panel is serpentine-wired, apply the serpentine transform **after** mapping.

**Secondary sampling for atmosphere around a cell**

```gdscript
func secondary_neighbors(px:Vector2i) -> PackedVector2Array:
    return PackedVector2Array([
        px + Vector2i(1, 0), px + Vector2i(-1, 0),
        px + Vector2i(0, 1), px + Vector2i(0,-1)
    ]).filter(func(v): return ((v.x|v.y) & 1) == 1) # odd coords only
```

**Turn-based light durations**

- `duration_turns` ∈ {1,2,3,"perm"}
- Decay semantics: when `duration_turns` decrements to 0, LED turns off (Primary) or fades out (Secondary).

---

## 2) Command Protocol (WebSocket JSON)

**Transport:** Godot `WebSocketPeer` → ESP32/Arduino → Teensy → LED matrix. 30 FPS frame cadence.

**Envelope**

```json
{
  "type": "update_cell|game_effect|clear|brightness|effect|light_primaries|batch",
  "id": "uuid",
  "ts": 1730000000,
  "data": { /* type-specific */ }
}
```

**Common fields**

- `id`: correlates `ACK`/`ERR`
- `ts`: ms since epoch (sender clock)

**ACK/ERR**

```json
{ "type":"ACK",  "command_id":"uuid", "ok":true }
{ "type":"ERROR","command_id":"uuid","code":"BUFFER_OVERFLOW","msg":"..." }
```

### 2.1 `update_cell` — primary mechanics

```json
{
  "type": "update_cell",
  "data": {
    "x": 0, "y": 0,                 // physical coords (even-even)
    "rgb": [255,255,255],             // 0..255
    "duration_turns": 1,              // 1|2|3|"perm"
    "pulse": "none|slow|medium|fast" // mechanics pulse (optional)
  }
}
```

### 2.2 `game_effect` — atmospheric effects (secondary)

```json
{
  "type": "game_effect",
  "data": {
    "name": "void_breathing|fear_ripples|edge_anxiety|illuminate_attempt|light_bloom|memory_corridor|heartbeat|movement_trail|player_presence|filed_status|signal_network|collapse_mode",
    "params": { /* effect-specific */ }
  }
}
```

### 2.3 `effect` — basic patterns

```json
{ "type":"effect", "data": { "name":"test_pattern|flash|rain", "params": {"duration_ms": 500} } }
```

### 2.4 `brightness` — global dimmer

```json
{ "type":"brightness", "data": { "level": 0.0 } } // 0..1
```

### 2.5 `light_primaries` — emergency mode

```json
{ "type":"light_primaries", "data": { "enable": true } }
```

### 2.6 `batch` — command batching (bridge decides split)

```json
{ "type":"batch", "data": { "commands": [ /* full command envelopes */ ] } }
```

---

## 3) Effect Library (Atmosphere)

**All Secondary-only**; never touch Primary. Color values are palette-aligned, brightness is scalar.

### 🌑 Darkness Effects

- **void\_breathing** — ultra-dim purple (1–5%); global slow sine.
  - params: `{ "intensity":0.03, "period_ms":4000 }`
- **fear\_ripples** — red rings from noise/movement points.
  - params: `{ "sources":[{"x":px,"y":py}], "speed":1.0, "width":1 }`
- **edge\_anxiety** — 1% white erratic flicker at edges.
  - params: `{ "prob":0.05 }`

### 💡 Light Effects

- **illuminate\_attempt** — spiral charge → petal bloom.
  - params: `{ "center":{"x":px,"y":py}, "charge_ms":300, "bloom_ms":200 }`
- **light\_bloom** — success flash with white/cyan expansion.
  - params: `{ "center":..., "radius":3, "duration_ms":600 }`
- **memory\_corridor** — lightning line between orthogonal cells + rainbow afterglow.
  - params: `{ "path":[{"x":..,"y":..},...], "afterglow_ms":1500 }`

### ❤️ Player Effects

- **heartbeat** — double-thump aura around player.
  - params: `{ "center":..., "color":[r,g,b], "period_ms":900 }`
- **movement\_trail** — green fading trail from prev→next.
  - params: `{ "path":[...], "decay_ms":800 }`
- **player\_presence** — color-coded aura by player type.
  - params: `{ "center":..., "player":"P1|P2|P3|P4" }`

### 🚨 Emergency/Status

- **filed\_status** — SOS Morse on Primary + chaotic red spin on Secondary.
  - params: `{ "player_id":..., "sos":true }`
- **signal\_network** — expanding cyan rings; reveal cadence.
  - params: `{ "centers":[...], "waves":3, "interval_ms":250 }`
- **collapse\_mode** — reality fracture pack:
  - params: `{ "enable":true, "intensity":0.6 }`
  - behavior:
    - violent flicker
    - orange/red shift
    - debris sparks on secondaries
    - random dimension glitches (purple/cyan/yellow)
    - global heartbeat accelerates

---

## 4) Memory Corridor — Animation Spec

1. **Charge**: purple buildup on path endpoints (250–500ms)
2. **Strike**: white lightning along orthogonal path (≤200ms)
3. **Afterglow**: rainbow fade (1–2s)
4. **Establish**: set Primary LEDs along path to **perm** state
5. **Sparkle**: occasional twinkle on secondaries (subtle)

---

## 5) Real-time Sync & Timing

- **Frame cadence**: 30 FPS renderer loop on bridge; queue flush every 33ms.
- **Immediate processing** for `update_cell` and `light_primaries`.
- **Rate limits**: ≤ 100 cmds/sec (bridge batches opportunistically).
- **Clock discipline**: bridge stamps `ts`; Teensy ignores if older than last frame.

**Update order (per frame)**

```
1. Apply mechanics (Primary updates)
2. Apply atmosphere (Secondary effects)
3. Clamp brightness & write LED buffer
4. Swap buffers (VSync) → ACK cmds
```

---

## 6) Collapse Sequence (Effects)

**Progression**

- Flicker intensity ↑ with `collapse_timer` nearing 0
- Palette shift: normal → orange → red (ramps at 66% / 33%)
- Debris: random white/orange sparks; density tied to timer
- Fractures: random color glitches (purple/cyan/yellow) with perlin jitter
- Final countdown: synchronized heartbeat from 1.0s → 0.2s period

**Bindings to rules** (via `ContextManager.rules.collapse`)

- `timer_base` and `timer_cap` shape schedule
- `spark_chance` influences debris density
- `aidron_auto_protocol` triggers 3‑wide permanent corridor on discovery

---

## 7) Effect Sync Protocol

```
DM Controller → WebSocket → ESP32/Arduino → Teensy → LED Matrix
        ↓                                   ↓
   Game Events                        Physical Effects
```

**Priority rules**

- Safety mechanics (Primary) **always** override atmosphere region writes.
- If bandwidth constrained: drop atmosphere first; keep Primary perfect.

---

## 8) Special Integration Features

- **void\_whispers** — boosted void breathing for 15s
  - `{ "name":"void_whispers", "params": { "duration_ms":15000, "intensity":0.06 } }`
- **victory\_aurora** — overlapping signal networks → aurora sweep
- **aidron\_nearby** — pre-collapse 10s warning state
- **exit\_portal** — signal network at Exit tile
- **test\_pattern** — corners + center lit for calibration

---

## 9) Safety & Separation Guarantees

- Primary LEDs: **never** used for atmosphere.
- Secondary LEDs: **never** store game state; can mirror low-intensity shadows only.
- Bridge enforces with write filters:

```gdscript
func write_primary(x:int,y:int,color:Color):
    if ((x|y) & 1) == 1: return # reject odd coords
    _primary_buf[x][y] = color

func write_secondary(x:int,y:int,color:Color):
    if ((x|y) & 1) == 0: return # reject even-even
    _secondary_buf[x][y] = color
```

---

## 10) Calibration & Tests

- **Test Pattern**:
  - Primaries: even-even white; secondaries off → verify separation
  - Secondaries: odd coords blue; primaries off
- **Latency**: command→ACK < 50ms typical
- **Throughput**: flood 200 cmds/s → expect graceful backlog & batch
- **Serpentine Addressing**: toggle to verify row parity handling
- **Burn-in Prevention**: rotate idle patterns; never hold full white

---

## 11) Godot Bridge Stubs (client)

```gdscript
class_name LedBridge
extends Node
var ws := WebSocketPeer.new()
var queue: Array = []
var last_flush := 0

func _process(delta: float) -> void:
    if ws.get_ready_state() == WebSocketPeer.STATE_OPEN:
        ws.poll()
        if Time.get_ticks_msec() - last_flush >= 33:
            _flush_batch()

func send(cmd:Dictionary) -> void:
    queue.append(cmd)

func update_cell(px:int, py:int, rgb:Color, turns) -> void:
    send({"type":"update_cell","id":UUID.v4(),"ts":Time.get_ticks_msec(),
          "data":{"x":px,"y":py,"rgb":[rgb.r8, rgb.g8, rgb.b8],"duration_turns":turns}})

func game_effect(name:String, params:Dictionary={}) -> void:
    send({"type":"game_effect","id":UUID.v4(),"ts":Time.get_ticks_msec(),"data":{"name":name,"params":params}})

func _flush_batch():
    if queue.is_empty(): return
    var payload := {"type":"batch","id":UUID.v4(),"ts":Time.get_ticks_msec(),"data":{"commands":queue}}
    ws.send_text(JSON.stringify(payload))
    queue.clear(); last_flush = Time.get_ticks_msec()
```

---

## 12) Rule Bindings (ContextManager)

- Pull `collapse.timer_base`, `collapse.timer_cap`, `collapse.spark_chance`, `collapse.aidron_auto_protocol`
- Adjust effect density & schedules accordingly
- Gate **collapse\_mode** enablement on canon phase

---

### ✅ Implementation Checklist (Do-Not-Ship if any ❌)

**Protocol**

-

**Mapping**

-

**Effects**

-

**Rules**

-

---

This spec is the single source for the LED rig’s behavior. The **Hardware Bridge Engineer** implements this contract; tests must validate it end-to-end.

