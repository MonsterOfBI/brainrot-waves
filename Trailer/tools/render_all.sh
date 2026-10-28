#!/usr/bin/env bash
# Рендер всех версий ролика + нормализация громкости до -14 LUFS (YouTube/TikTok) с ограничением пиков.
# bash Trailer/tools/render_all.sh [Trailer-EN-16x9 ...]
set -e
cd "$(dirname "$0")/../video"
FF="node_modules/@remotion/compositor-win32-x64-msvc/ffmpeg.exe"
ids=("$@")
[ ${#ids[@]} -eq 0 ] && ids=(Trailer-EN-16x9 Trailer-RU-16x9 Trailer-EN-9x16 Trailer-RU-9x16)
mkdir -p out/raw
for id in "${ids[@]}"; do
  name="BrainrotWaves_$(echo "$id" | sed 's/Trailer-//; s/-/_/')"
  echo "== $id -> out/$name.mp4"
  npx remotion render "$id" "out/raw/$name.mp4" --codec=h264 --crf=18 --audio-bitrate=320k --log=error
  "$FF" -y -loglevel error -i "out/raw/$name.mp4" -c:v copy -af "loudnorm=I=-14:TP=-1.0:LRA=11" -ar 48000 -c:a aac -b:a 320k -movflags +faststart "out/$name.mp4"
done
echo "RENDER DONE"
