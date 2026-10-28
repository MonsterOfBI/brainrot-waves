# Промо-ролик Brainrot Waves (30 секунд)

Четыре версии: EN/RU × 16:9/9:16. Кадры записаны из настоящей игры, монтаж и графика собраны в [Remotion](https://www.remotion.dev).

Готовые файлы лежат в `video/out/`:

| Файл | Формат |
|---|---|
| `BrainrotWaves_EN_16x9.mp4` | 1920×1080, YouTube, itch.io, Steam |
| `BrainrotWaves_RU_16x9.mp4` | 1920×1080 |
| `BrainrotWaves_EN_9x16.mp4` | 1080×1920, TikTok, Shorts, Reels |
| `BrainrotWaves_RU_9x16.mp4` | 1080×1920 |

## Как устроено

1. **Запись кадров в Unity.**
   - `Assets/Scripts/Trailer/TrailerDirector.cs` — «режиссёр» дубля. Он есть только в редакторе (`#if UNITY_EDITOR`) и в сборку игры не попадает. Режиссёр настраивает забег: героя, режим, арену, врагов, босса. Бот-оператор сам целится, стреляет и применяет навыки. Кадры (JPG) пишутся с постоянной частотой 60 fps через `Time.captureFramerate`, а в `events.csv` записывается момент каждого выстрела, смерти, навыка и ярости босса.
   - `Assets/Editor/TrailerCapture.cs` открывает сцену, ставит размер окна Game и входит в Play Mode. Перед записью он сохраняет прогресс и язык игрока, а `RestorePrefs()` их возвращает.
   - Дубли описаны в JSON. Их генерирует `tools/make_shots.py` в папку `shots/` для всех языков и форматов.
   - Запуск при открытом редакторе: `bash Trailer/tools/run_batch.sh Trailer/shots/<дубль>.json ...` (нужен `unity` CLI и пакет `com.unity.pipeline`).
2. **Клипы.** `python -I Trailer/tools/build_clips.py` собирает кадры в MP4 и сводит для каждого клипа звуковую дорожку из звуков игры (`Assets/Resources/Audio`) по журналу событий. Результат: `video/public/clips/`.
3. **Озвучка.** ElevenLabs v4 (голос `brian`) через Polza.ai: `tools/polza_tts.py`, тексты в `tools/vo_lines.json`. Готовые файлы: `video/public/vo/`.
4. **Монтаж.** `video/src/` — сцены в `scenes/`, тексты EN/RU в `lib/copy.ts`, тайминги в `lib/timeline.ts`. Склейки стоят на сетке бита музыки `boss_1.mp3` из игры (150 BPM), дроп совпадает с логотипом.

## Править и пересобирать

```bash
cd Trailer/video
npm i
npx remotion studio
```

```bash
npx remotion render Trailer-EN-16x9 out/BrainrotWaves_EN_16x9.mp4 --crf=18
```

Папки `public/clips`, `public/music` и `public/portraits/*.png` в git не хранятся. Их восстанавливают так: записать дубли, затем выполнить `build_clips.py`, скопировать `Assets/Resources/Music/boss_1.mp3` в `public/music/`, а рендеры персонажей — из `Logs/Trailer/out/portraits/`.
