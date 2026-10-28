#!/usr/bin/env bash
# Записать один дубль: bash Trailer/tools/run_shot.sh Trailer/shots/<shot>.json
# Запускает дубль в открытом редакторе Unity и ждёт done.json рядом с кадрами.
set -u
cd "$(dirname "$0")/../.."
shot="$1"
id=$(python -I -c "import json,sys;print(json.load(open(sys.argv[1],encoding='utf-8'))['id'])" "$shot")
out=$(python -I -c "import json,sys;d=json.load(open(sys.argv[1],encoding='utf-8'));print(d.get('outDir','Logs/Trailer/out'))" "$shot")
scene=$(python -I -c "import json,sys;print(json.load(open(sys.argv[1],encoding='utf-8')).get('scene','Game'))" "$shot")
if [ "$scene" = "Portraits" ]; then done_file="$out/portraits/done.json"; else done_file="$out/$id/done.json"; fi
rm -f "$done_file"
# дождаться, пока редактор выйдет из прошлого Play Mode
for i in $(seq 1 60); do
  r=$(unity command script eval --no-banner --format json -- --code "return UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling ? \"busy\" : \"idle\";" 2>/dev/null | grep -oE '"result": "[a-z]+"' | head -1)
  [ "$r" = '"result": "idle"' ] && break
  sleep 2
done
res=$(unity command script eval --no-banner --format json -- --code "return BrainrotWaves.EditorTools.TrailerCapture.RunShot(\"$shot\");" 2>&1 | grep -oE '"result": "[^"]*"' | tail -1)
echo "$id: $res"
case "$res" in *started*) ;; *) exit 1;; esac
for i in $(seq 1 450); do
  [ -f "$done_file" ] && { echo "$id: done $(cat "$done_file")"; exit 0; }
  sleep 2
done
echo "$id: timeout"; exit 2
