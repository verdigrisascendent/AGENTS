---
name: daily-standup-protocol
description: |
  Automated health check system that runs at project start. Mega-agent queries all specialists for drift detection, conflict identification, and system health metrics.
  
  Triggers:
  - On project open
  - Before major feature work
  - After crash/context loss
  - Manual invoke: [mega-agent] run daily standup

tools: StatusQuery, HealthCheck, DriftDetector, ConflictScanner, MetricsAggregator
---

# 🌅 Daily Standup Protocol — Project Health Dashboard

The mega-agent conducts a rapid health check across all systems, generating a living status report for Lights in the Dark.

## 🎯 Purpose

Prevent context loss, catch drift early, and maintain system integrity through automated specialist queries.

## 📊 Standup Execution Flow

### 1. Context Sync (context-manager)
```markdown
## Context Health Check
- Last session: [timestamp]
- Current game state: [phase/round/mode]
- Open issues: [count]
- State snapshots available: [list]
- Memory fragments recovered: [Y/N]
- Context gaps identified: [list]
```

### 2. Visual Integrity (amiga-aesthetic-enforcer)
```markdown
## Aesthetic Drift Report
- Screens audited: [count]
- Violations found:
  - Color drift: [count] - [specific hex values]
  - Grid misalignment: [count] - [widgets]
  - Font violations: [count] - [locations]
  - Bevel inconsistencies: [count]
  - Illegal transparency: [count]
- Priority fixes: [top 3 with locations]
- Screenshots flagged: [list]
```

### 3. Rule Compliance (game-state-guardian)
```markdown
## Rule Integrity Status
- Rule conflicts detected: [list with rule numbers]
- Unvalidated mechanics: [list]
- Edge cases discovered: [describe]
- Collapse logic: [stable/unstable/untested]
- Filing mechanics: [working/broken/modified]
- Token economy: [balanced/exploitable]
- Turn order: [intact/corrupted]
```

### 4. Hardware Status (hardware-bridge-engineer)
```markdown
## Hardware Sync Report
- LED board: [connected/disconnected/unknown]
- WebSocket status: [active/reconnecting/failed]
- Last successful sync: [timestamp]
- Queued commands: [count]
- Dropped commands: [count]
- Average latency: [ms]
- Reconnection attempts: [count]
- Hardware/software state mismatch: [Y/N]
```

### 5. Code Health (test-automator + debugger)
```markdown
## Test Coverage Report
- Overall coverage: [%]
  - Widget coverage: [%]
  - Logic coverage: [%]
  - Integration coverage: [%]
- Test status:
  - Passing: [count]
  - Failing: [list with reasons]
  - Flaky: [list with failure rate]
  - Skipped: [count]
- Last crash: [timestamp + stack summary]
- Memory leaks detected: [Y/N]
- Performance regressions: [list]
```

### 6. Technical Debt (legacy-modernizer)
```markdown
## Modernization Status
- Deprecated APIs in use: [count]
- Widgets needing refactor: [list]
- Old patterns detected: [list]
- Null safety compliance: [%]
- Framework version: [current/recommended]
```

### 7. Feature Flags (feature-isolation-specialist)
```markdown
## Experimental Features
- Active flags: 
  - [flag_name]: [days_active] days
  - [flag_name]: [impact_assessment]
- Rollback candidates: [list with reasons]
- Performance impact: [metrics per flag]
- Code coverage under flags: [%]
- Conflicts with core rules: [list]
```

## 🚨 Alert Severity Levels

### 🔴 RED FLAGS (Block all work)
- [ ] Visual drift > 5 violations on core screens
- [ ] Hardware disconnected during active development
- [ ] Core game rule conflict detected
- [ ] Test coverage < 60%
- [ ] Memory leak in production path
- [ ] Context manager has no recent snapshot
- [ ] Collapse sequence broken

### 🟡 YELLOW FLAGS (Fix within session)
- [ ] 3+ flaky tests affecting same system
- [ ] Feature flag active > 7 days
- [ ] Minor aesthetic drift (1-3 violations)
- [ ] WebSocket reconnection failures > 5
- [ ] Deprecated API warnings
- [ ] Missing test coverage for new features

### 🟢 GREEN FLAGS (System healthy)
- [ ] All tests passing
- [ ] Hardware sync < 50ms latency
- [ ] Zero aesthetic violations
- [ ] Complete rule compliance
- [ ] Recent context snapshot exists

## 📝 Output Format

```markdown
# 🎮 LITD Daily Standup - [DATE]
*Runtime: [duration] | Agents queried: [count]*

## 🔥 CRITICAL ISSUES ([count])
> Block all feature work until resolved
- [Issue with agent recommendation]
- [Issue with fix priority]

## ⚠️ WARNINGS ([count])  
> Address before end of session
- [Warning with impact assessment]
- [Warning with suggested agent]

## ✅ HEALTHY SYSTEMS ([count])
- [System name]: [metric]
- [System name]: All checks passed

## 📋 Today's Priorities
1. [Most critical fix] → [assigned agent]
2. [Second priority] → [assigned agent]  
3. [Third priority] → [assigned agent]

## 💭 Agent Recommendations
- **[agent-name]**: "[specific recommendation]"
- **[agent-name]**: "[specific recommendation]"

## 📊 Metrics Summary
- Code Health: [score]/100
- Visual Fidelity: [score]/100
- Rule Compliance: [score]/100
- Hardware Sync: [score]/100
- Overall Health: [score]/100

## 🔄 Next Standup
- Recommended: [time based on issue severity]
- Focus areas: [list based on findings]
```

## 🤖 Mega-Agent Standup Commands

### Full Standup
```
[mega-agent] run daily standup
```

### Quick Check (Red flags only)
```
[mega-agent] quick health check
```

### Specific System Check
```
[mega-agent] check visual integrity only
[mega-agent] verify game rules
[mega-agent] test hardware sync
```

### Historical Comparison
```
[mega-agent] compare health to yesterday
[mega-agent] show standup trends this week
```

## 📈 Health Score Calculation

```
Visual Fidelity Score = 100 - (violations * 5)
Rule Compliance Score = 100 - (conflicts * 20)
Test Health Score = (coverage * 0.6) + (passing * 0.4)
Hardware Sync Score = 100 - (latency_ms / 10) - (disconnects * 10)

Overall Health = (Visual * 0.3) + (Rules * 0.3) + (Tests * 0.2) + (Hardware * 0.2)
```

## 🔧 Integration Hooks

### Pre-commit Hook
```bash
# Run before any commit
[mega-agent] quick health check || exit 1
```

### CI/CD Integration
```yaml
# Add to build pipeline
- step: Daily Standup
  run: claude "[mega-agent] run daily standup --ci-mode"
```

### IDE Integration
```json
// VS Code task
{
  "label": "LITD Health Check",
  "command": "claude '[mega-agent] run daily standup'",
  "runOptions": {
    "runOn": "folderOpen"
  }
}
```

---

This protocol ensures your game maintains integrity across all systems. Run it religiously, and context loss becomes a thing of the past.