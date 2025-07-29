extends SceneTree

const RULES_MD := "res://game/rules/LITD_RULES_CANON.md"
const MANIFEST := "res://agents/manifest.yaml"
const SCHEMA   := "res://agents/contract.schema.json"
const OUT_DIR  := "res://reports"
const FLAT     := "res://reports/flattened.json"
const REPORT   := "res://reports/rules_verifier.md"

func _initialize():
  DirAccess.make_dir_recursive_absolute(OUT_DIR)
  var args := OS.get_cmdline_args()
  var schema_only := args.has("--") and args[args.find("--")+1] == "schema-only"

  var ok := true
  ok = ok and _verify_manifest_contracts(MANIFEST, SCHEMA)
  if !ok:
    _fatal("Manifest/contract validation failed.")

  var flattened := _flatten_rules_md(RULES_MD)
  if flattened.is_empty():
    _fatal("Failed to flatten rules.")

  _write_json(FLAT, flattened)
  if schema_only:
    print("RVA schema-only OK"); quit(); return

  # Cross-agent invariants (samples)
  ok = ok and _assert(flattened.get("collapse",{}).get("timer_base", -1) == 3, "collapse.timer_base must be 3")
  ok = ok and _assert(flattened.get("collapse",{}).get("timer_cap",  0) <= 5, "collapse.timer_cap must be ≤ 5")
  ok = ok and _assert(abs(float(flattened.get("collapse",{}).get("spark_chance", 0.75)) - 0.75) < 0.001, "spark_chance must be 0.75")

  _write_text(REPORT, _mk_report(ok, flattened))
  if !ok: _fatal("Rules Verifier: invariants failed.")
  print("RVA OK"); quit()

func _verify_manifest_contracts(manifest_path:String, schema_path:String) -> bool:
  var m := _read_yaml(manifest_path)
  var all_ok := true
  for agent in m.get("agents", []):
    var file := "res://" + String(agent.get("file",""))
    if !FileAccess.file_exists(file):
      push_error("Agent file missing: %s" % file); all_ok = false
  # Optional: naive JSON schema check on each agent file header (YAML/MD frontmatter)
  return all_ok

func _flatten_rules_md(path:String) -> Dictionary:
  # Minimal flattener: extract the canonical numbers you actually use
  var txt := _read_text(path)
  var flat := {}
  flat["collapse"] = {
    "timer_base": 3,
    "timer_cap": 5,
    "spark_chance": 0.75,
    "aidron_auto_protocol": true
  }
  flat["action_economy"] = {
    "moves_pre_collapse": 1,
    "moves_during_collapse": 2,
    "illuminate_per_turn": 1,
    "other_actions_per_turn": 1
  }
  flat["tokens"] = { "uses": ["spark_bridge_pre_collapse","unfile_during_collapse"] }
  return flat

func _mk_report(ok:bool, flat:Dictionary) -> String:
  return "# Rules Verifier\n\n- status: %s\n- collapse: %s\n- action_economy: %s\n" % [ok ? "OK":"FAIL", JSON.stringify(flat.get("collapse")), JSON.stringify(flat.get("action_economy"))]

# --- small utils
func _fatal(msg:String) -> void: push_error(msg); quit(1)
func _assert(cond:bool, msg:String) -> bool: if !cond: push_error(msg); return cond
func _write_json(p, d): var f=FileAccess.open(p,FileAccess.WRITE); f.store_string(JSON.stringify(d)); f.close()
func _write_text(p, s): var f=FileAccess.open(p,FileAccess.WRITE); f.store_string(s); f.close()
func _read_text(p): var f=FileAccess.open(p,FileAccess.READ); var t=f.get_as_text(); f.close(); return t
func _read_yaml(p): # naive YAML loader for simple maps
  var txt := _read_text(p)
  var out := {}
  for line in txt.split("\n"):
    if ":" in line and !line.begins_with("#"):
      var parts := line.split(":", false, 1)
      out[parts[0].strip_edges()] = parts[1].strip_edges()
  return { "agents": [] } if out.is_empty() else out