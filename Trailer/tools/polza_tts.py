"""Polza.ai TTS helper: synthesizes text to a 16-bit mono WAV, optionally verifies it with Whisper.

Usage: python -I polza_tts.py <key_file> <out.wav> <model> <voice> "<text>" [--verify]
"""
import base64
import json
import sys
import urllib.request
import uuid
import wave

BASE = "https://api.polza.ai/api/v1"


def key_from(path):
    with open(path, encoding="utf-8") as f:
        return f.read().strip()


def post_json(url, key, payload):
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Authorization": "Bearer " + key, "Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read().decode("utf-8"))


def tts(key, model, voice, text, out_path):
    j = post_json(BASE + "/audio/speech", key, {"model": model, "input": text, "voice": voice})
    pcm = base64.b64decode(j["audio"])
    ctype = j.get("contentType", "")
    if ctype == "audio/pcm":
        rate = int(round(len(pcm) / 2 / j["duration"] / 1000.0)) * 1000 if j.get("duration") else 24000
        with wave.open(out_path, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(rate)
            w.writeframes(pcm)
    else:
        with open(out_path, "wb") as f:
            f.write(pcm)
    return {"duration": j.get("duration"), "contentType": ctype, "cost_rub": j.get("usage", {}).get("cost_rub")}


def transcribe(key, path, model="openai/whisper-1"):
    boundary = uuid.uuid4().hex
    with open(path, "rb") as f:
        data = f.read()
    parts = []
    for name, value in (("model", model), ("response_format", "verbose_json")):
        parts.append(f"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n".encode())
    parts.append(
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"a.wav\"\r\nContent-Type: audio/wav\r\n\r\n".encode()
        + data + b"\r\n"
    )
    parts.append(f"--{boundary}--\r\n".encode())
    req = urllib.request.Request(
        BASE + "/audio/transcriptions",
        data=b"".join(parts),
        headers={"Authorization": "Bearer " + key, "Content-Type": "multipart/form-data; boundary=" + boundary},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read().decode("utf-8"))


if __name__ == "__main__":
    key_file, out, model, voice, text = sys.argv[1:6]
    k = key_from(key_file)
    info = tts(k, model, voice, text, out)
    print(json.dumps(info, ensure_ascii=False))
    if "--verify" in sys.argv:
        t = transcribe(k, out)
        print("whisper:", t.get("text"))
