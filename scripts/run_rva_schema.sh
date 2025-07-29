#!/usr/bin/env bash
set -euo pipefail
godot --headless --path . --script res://tools/rva_run.gd -- schema-only