"""Собирает записанные дубли в клипы для монтажа.

python -I Trailer/tools/build_clips.py [id ...]
  Logs/Trailer/out/<id>/f*.jpg + events.csv  ->  Trailer/video/public/clips/<id>.mp4 (60 fps, без звука)
                                               Trailer/video/public/clips/<id>.sfx.wav (звуки игры по журналу событий)
Без аргументов обрабатывает все готовые дубли (у которых есть done.json), пропуская уже собранные.
"""
import array
import json
import math
import os
import random
import subprocess
import sys
import wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Logs", "Trailer", "out")
CLIPS = os.path.join(ROOT, "Trailer", "video", "public", "clips")
CACHE = os.path.join(ROOT, "Logs", "Trailer", "sfx_cache")
AUDIO = os.path.join(ROOT, "Assets", "Resources", "Audio")
FF = os.path.join(ROOT, "Trailer", "video", "node_modules", "@remotion", "compositor-win32-x64-msvc", "ffmpeg.exe")
RATE = 48000

# событие -> [(папка звука, громкость, разброс высоты)] — громкости как в SoundFX игры
MAP = {
    "shot_Rifle": [("Rifle", 0.55, 0.08), ("Hit", 0.22, 0.1)],
    "shot_Shotgun": [("Shotgun", 0.9, 0.05), ("Hit", 0.3, 0.1)],
    "shot_Pistol": [("Pistol", 0.7, 0.08), ("Hit", 0.25, 0.1)],
    "death": [("EnemyDie", 0.55, 0.15), ("Explosion", 0.4, 0.2), ("Coin", 0.2, 0.1)],
    "boss_death": [("EnemyDie", 1.0, 0.0), ("Explosion", 0.9, 0.0)],
    "reload": [("Reload", 0.6, 0.0)],
    "banner": [("WaveStart", 0.8, 0.0)],
    "rage": [("Explosion", 1.0, 0.0), ("Ability", 0.6, 0.0)],
    "boss_intro": [("Explosion", 0.7, 0.0)],
    "click": [("Click", 0.5, 0.05)],
}
ABILITY_EXTRA = {"CarpetBomb": "Explosion", "Mines": "Explosion", "Grenade": "Explosion", "Nova": "Explosion",
                 "Slam": "Explosion", "Dash": "Dash", "Vortex": "Ability"}


def decode(path):
    """ogg/mp3 -> float-стерео 48 кГц (кэшируется как wav)."""
    os.makedirs(CACHE, exist_ok=True)
    rel = os.path.relpath(path, AUDIO).replace(os.sep, "_")
    wav = os.path.join(CACHE, os.path.splitext(rel)[0] + ".wav")
    if not os.path.exists(wav):
        subprocess.run([FF, "-y", "-loglevel", "error", "-i", path, "-ac", "2", "-ar", str(RATE), "-c:a", "pcm_s16le", wav], check=True)
    with wave.open(wav) as w:
        data = array.array("h", w.readframes(w.getnframes()))
    return [v / 32768.0 for v in data]


_bank = {}


def variants(folder):
    if folder not in _bank:
        d = os.path.join(AUDIO, folder)
        files = sorted(f for f in os.listdir(d) if f.lower().endswith((".ogg", ".wav", ".mp3")))
        _bank[folder] = [decode(os.path.join(d, f)) for f in files]
    return _bank[folder]


def mix_events(events, frames, fps, seed):
    rnd = random.Random(seed)
    n = int(math.ceil(frames / fps * RATE)) * 2
    buf = array.array("f", bytes(4 * n))
    busy = {}  # не больше 3 одинаковых звуков на кадр, как пул источников в игре
    for frame, name in events:
        layers = list(MAP.get(name, []))
        if name.startswith("ability_"):
            layers = [("Ability", 0.8, 0.0)]
            extra = ABILITY_EXTRA.get(name[8:])
            if extra:
                layers.append((extra, 0.9, 0.1))
        key = (frame, name)
        busy[key] = busy.get(key, 0) + 1
        if busy[key] > 3:
            continue
        for folder, vol, pv in layers:
            vs = variants(folder)
            if not vs:
                continue
            src = vs[rnd.randrange(len(vs))]
            pitch = 1.0 + rnd.uniform(-pv, pv)
            start = int(frame / fps * RATE) * 2
            m = int(len(src) / 2 / pitch)
            for i in range(m):
                j = int(i * pitch) * 2
                k = start + i * 2
                if k + 1 >= n or j + 1 >= len(src):
                    break
                buf[k] += src[j] * vol
                buf[k + 1] += src[j + 1] * vol
    peak = max(1e-6, max(abs(v) for v in buf))
    gain = min(1.0, 0.89 / peak)
    out = array.array("h", (int(max(-1.0, min(1.0, math.tanh(v * gain * 1.2) / math.tanh(1.2)) * 32767)) for v in buf))
    return out


def build(shot_id):
    d = os.path.join(OUT, shot_id)
    done = json.load(open(os.path.join(d, "done.json"), encoding="utf-8"))
    fps = done["fps"]
    os.makedirs(CLIPS, exist_ok=True)
    mp4 = os.path.join(CLIPS, shot_id + ".mp4")
    if not os.path.exists(mp4) or os.path.getmtime(mp4) < os.path.getmtime(os.path.join(d, "done.json")):
        subprocess.run([FF, "-y", "-loglevel", "error", "-framerate", str(fps), "-i", os.path.join(d, "f%05d.jpg"),
                        "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", "-movflags", "+faststart", mp4], check=True)
    events = []
    ev = os.path.join(d, "events.csv")
    if os.path.exists(ev):
        for line in open(ev, encoding="utf-8").read().splitlines()[1:]:
            if "," in line:
                f, name = line.split(",", 1)
                events.append((int(f), name.strip()))
    pcm = mix_events(events, done["frames"], fps, hash(shot_id) & 0xFFFF)
    with wave.open(os.path.join(CLIPS, shot_id + ".sfx.wav"), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    return done["frames"], len(events)


def main(ids):
    if not ids:
        ids = sorted(x for x in os.listdir(OUT)
                     if os.path.exists(os.path.join(OUT, x, "done.json")) and x not in ("portraits", "test_beach")
                     and os.path.exists(os.path.join(OUT, x, "f00000.jpg")))
    meta_path = os.path.join(CLIPS, "clips.json")
    meta = json.load(open(meta_path, encoding="utf-8")) if os.path.exists(meta_path) else {}
    for i in ids:
        frames, nev = build(i)
        meta[i] = {"frames": frames, "fps": 60, "seconds": round(frames / 60, 3)}
        print(i, frames, "frames,", nev, "events")
    json.dump(meta, open(meta_path, "w", encoding="utf-8"), indent=1, sort_keys=True)


if __name__ == "__main__":
    main(sys.argv[1:])
