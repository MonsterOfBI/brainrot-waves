import React from "react";
import { Audio, Video } from "@remotion/media";
import {
  AbsoluteFill,
  Easing,
  Img,
  interpolate,
  random,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { COLORS, Fmt, Lang } from "./copy";
import { BODY, TITLE } from "./fonts";
import { FPS } from "./timeline";

export type Ctx = { lang: Lang; fmt: Fmt };

/** Имя клипа: дубли с интерфейсом записаны на каждом языке, пролёты — общие. */
export const clip = (id: string, ctx: Ctx, hud = true) => (hud ? `${id}_${ctx.lang}_${ctx.fmt}` : `${id}_${ctx.fmt}`);

/** Масштаб под формат: 1 для 1920x1080 и для 1080x1920 (по короткой стороне). */
export const useUnit = () => {
  const { width, height } = useVideoConfig();
  return Math.min(width, height) / 1080;
};

/** Кадры из игры: видео + звуки игры этого дубля, с «ударом» масштаба на склейке. */
export const Clip: React.FC<{
  file: string;
  start: number; // секунда исходника, с которой начинать
  rate?: number;
  sfx?: number;
  punch?: boolean;
  style?: React.CSSProperties;
  videoStyle?: React.CSSProperties;
}> = ({ file, start, rate = 1, sfx = 0.5, punch = true, style, videoStyle }) => {
  const frame = useCurrentFrame();
  const trim = Math.round(start * FPS);
  return (
    <AbsoluteFill style={{ overflow: "hidden", backgroundColor: "black", ...style }}>
      <AbsoluteFill
        style={{
          scale: punch
            ? interpolate(frame, [0, 9], [1.14, 1], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) })
            : 1,
        }}
      >
        <Video
          src={staticFile(`clips/${file}.mp4`)}
          trimBefore={trim}
          playbackRate={rate}
          muted
          objectFit="cover"
          style={{ width: "100%", height: "100%", ...videoStyle }}
        />
      </AbsoluteFill>
      {sfx > 0 ? (
        <Audio src={staticFile(`clips/${file}.sfx.wav`)} trimBefore={trim} playbackRate={rate} volume={sfx * 0.7} />
      ) : null}
    </AbsoluteFill>
  );
};

/** Белая вспышка в начале склейки. */
export const Flash: React.FC<{ frames?: number; strength?: number; color?: string }> = ({
  frames = 6,
  strength = 0.85,
  color = "white",
}) => {
  const frame = useCurrentFrame();
  return (
    <AbsoluteFill
      style={{
        backgroundColor: color,
        opacity: interpolate(frame, [0, frames], [strength, 0], { extrapolateRight: "clamp" }),
        pointerEvents: "none",
      }}
    />
  );
};

/** Обводка текста тенями — одинаково выглядит в превью и при рендере. */
export const outline = (px: number, color: string = COLORS.ink) => {
  const shadows: string[] = [];
  const steps = 16;
  for (let i = 0; i < steps; i++) {
    const a = (i / steps) * Math.PI * 2;
    shadows.push(`${(Math.cos(a) * px).toFixed(1)}px ${(Math.sin(a) * px).toFixed(1)}px 0 ${color}`);
  }
  shadows.push(`0 ${px * 1.6}px 0 ${color}`, `0 ${px * 2.4}px ${px * 3}px rgba(0,0,0,0.45)`);
  return shadows.join(", ");
};

/** Подбор кегля, чтобы строка Unbounded влезла в ширину. */
export const fit = (text: string, base: number, maxWidth: number, k = /[а-яё]/i.test(text) ? 0.98 : 0.88) =>
  Math.min(base, maxWidth / Math.max(1, text.length * k));

/** Крупный заголовок с «шлепком»: вылетает из большого масштаба и чуть поворачивается. */
export const Slam: React.FC<{
  text: string;
  size: number;
  color?: string;
  delay?: number; // кадры
  rotate?: number;
  style?: React.CSSProperties;
}> = ({ text, size, color = COLORS.yellow, delay = 0, rotate = 0, style }) => {
  const frame = useCurrentFrame() - delay;
  if (frame < 0) return null;
  return (
    <div
      style={{
        fontFamily: TITLE,
        fontWeight: 900,
        fontSize: size,
        lineHeight: 1.02,
        color,
        textShadow: outline(size * 0.07),
        textTransform: "uppercase",
        whiteSpace: "nowrap",
        letterSpacing: -size * 0.02,
        scale: interpolate(frame, [0, 5, 9], [2.6, 0.92, 1], {
          extrapolateRight: "clamp",
          easing: Easing.out(Easing.cubic),
        }),
        opacity: interpolate(frame, [0, 2], [0, 1], { extrapolateRight: "clamp" }),
        rotate: `${rotate}deg`,
        ...style,
      }}
    >
      {text}
    </div>
  );
};

/** Плашка-подпись (как кнопки в меню игры). */
export const Chip: React.FC<{ text: string; size: number; bg?: string; color?: string; delay?: number; style?: React.CSSProperties }> = ({
  text,
  size,
  bg = COLORS.pink,
  color = "white",
  delay = 0,
  style,
}) => {
  const frame = useCurrentFrame() - delay;
  if (frame < 0) return null;
  return (
    <div
      style={{
        fontFamily: TITLE,
        fontWeight: 800,
        fontSize: size,
        color,
        background: bg,
        padding: `${size * 0.32}px ${size * 0.7}px`,
        borderRadius: size * 0.45,
        boxShadow: `0 ${size * 0.18}px 0 rgba(0,0,0,0.35), 0 ${size * 0.4}px ${size}px rgba(0,0,0,0.35)`,
        textTransform: "uppercase",
        whiteSpace: "nowrap",
        translate: interpolate(frame, [0, 7], [`0px ${size * 1.2}px`, "0px 0px"], {
          extrapolateRight: "clamp",
          easing: Easing.out(Easing.back(2)),
        }),
        opacity: interpolate(frame, [0, 3], [0, 1], { extrapolateRight: "clamp" }),
        ...style,
      }}
    >
      {text}
    </div>
  );
};

export const SmallText: React.FC<{ text: string; size: number; color?: string; style?: React.CSSProperties }> = ({
  text,
  size,
  color = "white",
  style,
}) => (
  <div style={{ fontFamily: BODY, fontWeight: 700, fontSize: size, color, textShadow: outline(size * 0.08), ...style }}>{text}</div>
);

/** Фон меню игры: фиолетовый градиент и летающие цветные кубы. */
export const MenuBackdrop: React.FC<{ seed?: string; dark?: number }> = ({ seed = "bg", dark = 0 }) => {
  const frame = useCurrentFrame();
  const { width, height } = useVideoConfig();
  const colors = [COLORS.pink, COLORS.cyan, COLORS.yellow, COLORS.green, "#7E57C2", "#FF8A65"];
  return (
    <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 45%, #6A3FC4 0%, ${COLORS.menu} 45%, ${COLORS.deep} 100%)` }}>
      {new Array(18).fill(0).map((_, i) => {
        const x = random(`${seed}x${i}`) * width;
        const y = random(`${seed}y${i}`) * height;
        const s = (0.04 + random(`${seed}s${i}`) * 0.07) * Math.min(width, height);
        const drift = (random(`${seed}d${i}`) - 0.5) * 3;
        return (
          <div
            key={i}
            style={{
              position: "absolute",
              left: x,
              top: y + frame * drift,
              width: s,
              height: s,
              borderRadius: s * 0.12,
              background: `linear-gradient(135deg, ${colors[i % colors.length]}, rgba(0,0,0,0.35))`,
              boxShadow: `inset ${-s * 0.15}px ${-s * 0.15}px 0 rgba(0,0,0,0.25)`,
              rotate: `${random(`${seed}r${i}`) * 90 + frame * (1 + drift)}deg`,
              opacity: 0.85,
            }}
          />
        );
      })}
      <AbsoluteFill style={{ backgroundColor: "black", opacity: dark }} />
    </AbsoluteFill>
  );
};

/** Рендер персонажа на прозрачном фоне. */
export const Portrait: React.FC<{ id: string; style?: React.CSSProperties }> = ({ id, style }) => (
  <Img src={staticFile(`portraits/${id}.png`)} style={{ objectFit: "contain", ...style }} />
);

/** Логотип как в меню игры. */
export const Logo: React.FC<{ size: number; twoLines?: boolean; delay?: number }> = ({ size, twoLines, delay = 0 }) => {
  const frame = useCurrentFrame() - delay;
  if (frame < 0) return null;
  const st: React.CSSProperties = {
    fontFamily: TITLE,
    fontWeight: 900,
    fontSize: size,
    lineHeight: 0.98,
    color: COLORS.yellow,
    textShadow: outline(size * 0.075, "#2A1240"),
    letterSpacing: -size * 0.02,
    whiteSpace: "nowrap",
    textAlign: "center",
  };
  return (
    <div
      style={{
        scale: interpolate(frame, [0, 6, 11], [3.2, 0.9, 1], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) }),
        rotate: `${interpolate(frame, [0, 8], [-8, -2], { extrapolateRight: "clamp" })}deg`,
        opacity: interpolate(frame, [0, 2], [0, 1], { extrapolateRight: "clamp" }),
      }}
    >
      {twoLines ? (
        <>
          <div style={st}>BRAINROT</div>
          <div style={{ ...st, color: COLORS.pink }}>WAVES</div>
        </>
      ) : (
        <div style={st}>
          BRAINROT <span style={{ color: COLORS.pink }}>WAVES</span>
        </div>
      )}
    </div>
  );
};

/** Тряска кадра (для «шлепков»). */
export const useShake = (hits: number[], amount: number) => {
  const frame = useCurrentFrame();
  let dx = 0;
  let dy = 0;
  for (const h of hits) {
    const t = frame - h;
    if (t >= 0 && t < 10) {
      const k = (1 - t / 10) * amount;
      dx += Math.sin(t * 2.7 + h) * k;
      dy += Math.cos(t * 3.1 + h) * k;
    }
  }
  return `${dx.toFixed(1)}px ${dy.toFixed(1)}px`;
};
