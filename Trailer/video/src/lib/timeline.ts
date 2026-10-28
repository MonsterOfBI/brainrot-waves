import { Lang } from "./copy";

export const FPS = 30;
export const DURATION_S = 30;

/** Музыка: boss_1.mp3 из игры, дроп на 29.9 с трека — ставим его на появление логотипа. */
export const MUSIC_DROP_IN_TRACK = 29.9;
export const BEAT = 60 / 150; // 150 BPM

/** Сцены (секунды), все границы — на сетке бита от дропа. */
export const S = {
  hook: [0, 4.4],
  logo: [4.4, 6.8],
  gameplay: [6.8, 10.4],
  arenas: [10.4, 12.8],
  roster: [12.8, 16.4],
  boss: [16.4, 22.4],
  modes: [22.4, 26.4],
  end: [26.4, 30],
} as const;

export const LOGO_DROP = S.logo[0];

/** Когда звучит каждая фраза диктора (секунды) и длительность из vo.json. */
export const VO: Record<Lang, { at: number; dur: number; rate?: number }[]> = {
  en: [
    { at: 0.25, dur: 2.56 },
    { at: 2.75, dur: 1.52 },
    { at: 6.9, dur: 4.56 },
    { at: 12.9, dur: 3.68 },
    { at: 16.7, dur: 4.16 },
    { at: 22.5, dur: 4.16 },
    { at: 26.8, dur: 2.96 },
  ],
  ru: [
    { at: 0.15, dur: 3.52, rate: 1.15 },
    { at: 3.1, dur: 1.68 },
    { at: 6.85, dur: 4.72 },
    { at: 12.9, dur: 3.36 },
    { at: 16.6, dur: 4.32 },
    { at: 22.5, dur: 3.92 },
    { at: 26.55, dur: 3.28 },
  ],
};

/** Моменты трёх «тунг» внутри первой фразы (секунды от начала ролика). */
export const TUNG_AT: Record<Lang, number[]> = {
  en: [0.25, 0.89, 1.79],
  ru: [0.15, 0.74, 2.39],
};

/** Моменты слов «Сюжет / Выживание / Защита» (секунды от начала ролика). */
export const MODE_AT: Record<Lang, number[]> = {
  en: [22.5, 23.86, 25.14],
  ru: [22.5, 23.6, 24.82],
};

export const f = (s: number) => Math.round(s * FPS);
