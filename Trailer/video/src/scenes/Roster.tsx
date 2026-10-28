import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, Easing, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COLORS, COPY } from "../lib/copy";
import { BODY } from "../lib/fonts";
import { ROSTER } from "../lib/roster";
import { f } from "../lib/timeline";
import { Chip, Clip, clip, Ctx, fit, Flash, MenuBackdrop, Portrait, Slam, useUnit } from "../lib/ui";

/** Стена из 26 карточек героев. */
const CardWall: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width, height } = useVideoConfig();
  const v = ctx.fmt === "v";
  const c = COPY[ctx.lang];
  const cols = v ? 5 : 9;
  const rows = Math.ceil(ROSTER.length / cols);
  const gap = 14 * u;
  const cardW = (width - 80 * u - gap * (cols - 1)) / cols;
  const cardH = Math.min(cardW * 1.25, (height - 120 * u - gap * (rows - 1)) / rows);
  const gridW = cols * cardW + (cols - 1) * gap;
  const gridH = rows * cardH + (rows - 1) * gap;
  return (
    <AbsoluteFill>
      <MenuBackdrop seed="wall" dark={0.25} />
      <div style={{ position: "absolute", left: (width - gridW) / 2, top: (height - gridH) / 2, width: gridW, height: gridH }}>
        {ROSTER.map((r, i) => {
          const col = i % cols;
          const row = Math.floor(i / cols);
          const t = frame - i * 0.8;
          return (
            <div
              key={r.id}
              style={{
                position: "absolute",
                left: col * (cardW + gap),
                top: row * (cardH + gap),
                width: cardW,
                height: cardH,
                borderRadius: 18 * u,
                background: `linear-gradient(160deg, ${r.color} 0%, ${COLORS.deep} 115%)`,
                border: `${3 * u}px solid rgba(255,255,255,0.18)`,
                boxShadow: `0 ${8 * u}px ${20 * u}px rgba(0,0,0,0.45)`,
                overflow: "hidden",
                scale: interpolate(t, [0, 6], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.out(Easing.back(2.2)) }),
                rotate: `${interpolate(t, [0, 8], [(i % 2 ? 1 : -1) * 18, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" })}deg`,
              }}
            >
              <Portrait id={r.id} style={{ position: "absolute", width: cardW * 1.35, height: cardW * 1.35, left: -cardW * 0.175, top: -cardW * 0.1 }} />
              <div
                style={{
                  position: "absolute",
                  bottom: 0,
                  left: 0,
                  right: 0,
                  padding: `${8 * u}px ${6 * u}px`,
                  background: "linear-gradient(0deg, rgba(10,4,30,0.9), rgba(10,4,30,0))",
                  fontFamily: BODY,
                  fontWeight: 800,
                  fontSize: Math.min(24 * u, cardW / 7.5),
                  color: "white",
                  textAlign: "center",
                  lineHeight: 1.05,
                }}
              >
                {ctx.lang === "en" ? r.en : r.ru}
              </div>
            </div>
          );
        })}
      </div>
      <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 50%, rgba(10,4,30,${v ? 0.88 : 0.75}) 0%, rgba(10,4,30,0) ${v ? 62 : 55}%)` }} />
      <AbsoluteFill style={{ justifyContent: "center", alignItems: "center", gap: 8 * u }}>
        <Slam text="26" size={(v ? 330 : 300) * u} color={COLORS.pink} delay={4} rotate={-5} />
        <Slam text={c.rosterTitle} size={fit(c.rosterTitle, 130 * u, width * 0.9)} delay={8} />
        <Chip text={c.rosterSub} size={40 * u} bg={COLORS.cyan} color={COLORS.ink} delay={14} />
      </AbsoluteFill>
      {[0, 6, 12, 18].map((d) => (
        <Sequence key={d} from={d}>
          <Audio src={staticFile("sfx/pop.wav")} volume={0.45} />
        </Sequence>
      ))}
      <Sequence from={4}>
        <Audio src={staticFile("sfx/slam.wav")} volume={0.7} />
      </Sequence>
    </AbsoluteFill>
  );
};

/** 12.8–16.4 с: настоящее меню с 26 героями → стена карточек → навык E (водоворот). */
export const Roster: React.FC<Ctx> = (ctx) => {
  const u = useUnit();
  const c = COPY[ctx.lang];
  return (
    <AbsoluteFill>
      <Sequence durationInFrames={f(1.6)}>
        <Clip file={clip("menu_roster", ctx)} start={0.2} rate={1.35} sfx={0.35} />
        <Flash frames={5} strength={0.7} />
      </Sequence>
      <Sequence from={f(1.6)} durationInFrames={f(1.2)}>
        <CardWall {...ctx} />
        <Flash frames={4} strength={0.6} />
      </Sequence>
      <Sequence from={f(2.8)} durationInFrames={f(0.8)}>
        <Clip file={clip("ability_vortex", ctx)} start={0.35} sfx={0.55} />
        <Flash frames={4} strength={0.6} />
        <AbsoluteFill style={{ justifyContent: "center", alignItems: "center", paddingTop: (ctx.fmt === "v" ? 700 : 420) * u }}>
          <Chip text={c.skills} size={64 * u} bg={COLORS.cyan} color={COLORS.ink} />
        </AbsoluteFill>
        <Audio src={staticFile("sfx/whoosh.wav")} volume={0.6} />
      </Sequence>
    </AbsoluteFill>
  );
};
