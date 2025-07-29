# 📘 LITD_RULES_CANON.md (v8.0)

The canonical ruleset for **Lights in the Dark**, enforced by agents and referenced by all automation.

---

## 🎲 Game Summary
- **Players:** 4–5 + 1 DM
- **Age:** 8+
- **Time:** 20–30 min
- **Theme:** Memory vault collapse in total darkness

---

## 🔦 Game Phases

### Phase 1: The Opening
- All players begin invisible
- No light unless Bluepea beacon is enabled
- Goal: establish communication through Signals

### Phase 2: The Search
- Players can perform:
  - `Signal` (produces noise)
  - `Illuminate` (attempts to generate light)
- Filers begin waking based on noise

### Phase 3: The Network
- Players can:
  - Forge Memory Sparks using tokens (permanent light corridors)
  - Create intentional safe zones
  - Choose visibility vs. vulnerability strategically

### Phase 4: The Escape
- Aidron and Exit become permanent light sources once discovered
- Goal: reach the Exit before the Vault collapses
- When first player escapes, the **Vault Collapses**

---

## 🧠 Core Mechanics

### Light & Shadow Logic
| State     | Visibility     | Vulnerability    |
|-----------|----------------|------------------|
| Light     | Visible        | Protected        |
| Darkness  | Invisible      | Vulnerable       |

- Filers follow same rules
- Only Aidron, Exit, and Memory Sparks generate **permanent light**
- **All other lights** are temporary unless collapse rules say otherwise

### Filers
- Dormant until noise is made
- Awaken gradually
- Can only file players in **darkness**
- During collapse, they pursue only **lit tiles with players**

### Memory Tokens
- Used for:
  - Creating Memory Spark bridges (pre-collapse)
  - **Unfiling** self (collapse only, skip next turn after)

### Action Economy
Each player turn allows:
- 1 `Illuminate` attempt
- 1 additional action (`Signal`, `Interact`, `Use Token`, etc.)
- 1 movement (or **2 during Collapse**)

---

## 💥 Vault Collapse (Endgame Phase)

### Trigger:
- First player enters the Exit

### Effect:
- “VAULT COLLAPSING” status appears
- **Collapse Timer = 3 Rounds** (can extend to 5 with Time Slips)

### Collapse Rules:
- Players move **2 tiles per turn**, no noise penalty
- **75% chance** any movement creates a brief light spark (1 round only)
- Filed players can unfile by spending Memory Token + rolling on Vault Memory Table
- All temporary light sources now flicker (red/orange), except:
  - Aidron
  - Exit
  - Memory Sparks

### Aidron Emergency Protocol
- If discovered during Collapse, auto-activates
- Projects **3-wide corridor** of permanent light to Exit

---

## 🎲 Collapse Events Table (d6)
| Roll | Event            | Effect                                                  |
|------|------------------|----------------------------------------------------------|
| 1    | Light Cascade     | 2×2 grid lights for 1 round                             |
| 2    | Debris Path       | Temporary light bridge forms between 2 players          |
| 3    | Memory Echo       | All player positions flash with light                   |
| 4    | Time Slip         | +1 Round (max 5 total)                                  |
| 5-6  | Shattered Path    | One non-Aidron/Exit permanent light disappears          |

---

## ⚠️ Clarified Rulings

- **Aidron Activation:** Instant during collapse
- **Filers (Collapse):** Only target lit squares with players
- **Light Classification:** Permanent = Aidron, Exit, Spark; others = flicker or event
- **Memory Sparks:** Created using tokens, pre-collapse only
- **Illuminate Limit:** One attempt per turn
- **Light Sparks:** Only triggered by movement during collapse, 75% chance, 1-turn duration
- **No collapse reset on unfile**
- **If all players filed during collapse → game lost**

---

## 🚧 Edge Cases
| Situation | Ruling |
|----------|--------|
| Multiple players exit same round | Ignored — turns are sequential |
| Collapse starts, then everyone gets filed | Vault wins |
| Player unfiles after collapse | Timer continues, no reset |

---

## 🔗 Used By
- `game_state_guardian.md`
- `test-automator-debugger.md`
- `context_manager.md`
- `frontend_developer.md`

This file is the unambiguous source of truth for **all gameplay enforcement**.
