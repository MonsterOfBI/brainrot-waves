#!/usr/bin/env bash
# Записать несколько дублей подряд: bash Trailer/tools/run_batch.sh <shot1.json> <shot2.json> ...
cd "$(dirname "$0")/../.."
for s in "$@"; do
  start=$(date +%s)
  bash Trailer/tools/run_shot.sh "$s"
  echo "  ($(( $(date +%s) - start ))s)"
done
echo "BATCH DONE"
