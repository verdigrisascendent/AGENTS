#!/usr/bin/env bash
set -euo pipefail
RULES_MD="game/rules/LITD_RULES_CANON.md"
MANIFEST="agents/manifest.yaml"
SCHEMA="agents/contract.schema.json"

[[ -f "$RULES_MD" ]] || { echo "Missing $RULES_MD"; exit 1; }
[[ -f "$MANIFEST" ]] || { echo "Missing $MANIFEST"; exit 1; }
[[ -f "$SCHEMA"   ]] || { echo "Missing $SCHEMA"; exit 1; }
echo "Canon + manifest + schema present."