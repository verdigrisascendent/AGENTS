#!/usr/bin/env bash
set -euo pipefail
mkdir -p res://reports/health res://tests/.golden || true 2>/dev/null || true
godot --headless --path . \
  --test --doctool --script res://addons/gut/gut_cmdln.gd \
  -gdir=res://tests/unit,res://tests/fuzz -gexit