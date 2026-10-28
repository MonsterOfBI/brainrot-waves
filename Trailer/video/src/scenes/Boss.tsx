import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, Easing, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COLORS, COPY } from "../lib/copy";
import { f } from "../lib/timeline";
import { Clip, clip, Ctx, fit, Flash, Portrait, Slam, useShake, useUnit } from "../lib/ui";

/** Экран «БОСС» в стиле файтинга. */
const BossCard: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width, height } = useVideoConfig();
  const v = ctx.fmt === "v";
  const c = COPY[ctx.lang];
  const shake = useShake([0, 10], 24 * u);
  const s = (v ? 1250 : 1400) * u;
  return (
    <AbsoluteFill style={{ translate: shake, overflow: "hidden" }}>
      <AbsoluteFill
        style={{
          background: `repeating-linear-gradient(-24deg, ${COLORS.play} 0px, ${COLORS.play} ${60 * u}px, #8E1238 ${60 * u}px, #8E1238 ${120 * u}px)`,
          backgroundPositionX: frame * 14 * u,
        }}
      />
      <AbsoluteFill style={{ background: "radial-gradient(circle at 60% 50%, rgba(255,200,60,0.35) 0%, rgba(20,10,38,0.85) 75%)" }} />
      <Portrait
        id="Bombardiro"
        style={{
          position: "absolute",
          width: s,
          height: s,
          left: v ? width / 2 - s / 2 : width * 0.68 - s / 2,
          top: v ? height * 0.62 - s / 2 : height * 0.46 - s / 2,
          translate: interpolate(frame, [0, 10], [`${900 * u}px 0px`, "0px 0px"], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) }),
          scale: interpolate(frame, [0, 36], [1.5, 1.85], { extrapolateRight: "clamp" }),
          rotate: "-8deg",
        }}
      />
      <AbsoluteFill
        style={{
          justifyContent: v ? "flex-start" : "center",
          alignItems: v ? "center" : "flex-start",
          padding: v ? `${300 * u}px 0 0 0` : `0 0 0 ${110 * u}px`,
        }}
      >
        <Slam text={c.boss} size={fit(c.boss, (v ? 120 : 130) * u, v ? width * 0.9 : width * 0.55)} delay={3} rotate={-4} />
      </AbsoluteFill>
      <Flash frames={6} strength={0.9} color={COLORS.yellow} />
      <Audio src={staticFile("sfx/slam.wav")} volume={0.75} />
      <Audio src={staticFile("sfx/whoosh.wav")} volume={0.7} />
    </AbsoluteFill>
  );
};

/** В вертикали родная табличка с именем босса обрезается — закрываем её своей, в две строки. */
const BossNameVertical: React.FC = () => {
  const u = useUnit();
  const { width } = useVideoConfig();
  const size = fit("BOMBARDIRO", 130 * u, width * 0.9);
  return (
    <AbsoluteFill style={{ justifyContent: "flex-end", alignItems: "center", paddingBottom: 250 * u }}>
      <AbsoluteFill style={{ background: "linear-gradient(0deg, rgba(10,4,30,0.92) 0%, rgba(10,4,30,0.6) 28%, rgba(0,0,0,0) 45%)" }} />
      <Slam text="BOMBARDIRO" size={size} color="#9CCC65" delay={14} rotate={-3} />
      <Slam text="CROCODILO" size={size} color="#9CCC65" delay={22} rotate={-3} />
    </AbsoluteFill>
  );
};

/** 16.4–22.4 с: карточка босса → родная заставка Бомбардиро Крокодило → бой и ярость. */
export const Boss: React.FC<Ctx> = (ctx) => (
  <AbsoluteFill>
    <Sequence durationInFrames={f(1.2)}>
      <BossCard {...ctx} />
    </Sequence>
    <Sequence from={f(1.2)} durationInFrames={f(2.4)}>
      <Clip file={clip("boss_intro", ctx)} start={0.3} sfx={0.6} />
      {ctx.fmt === "v" ? <BossNameVertical /> : null}
      <Flash frames={5} strength={0.7} />
    </Sequence>
    <Sequence from={f(3.6)} durationInFrames={f(2.4)}>
      <Clip file={clip("boss_fight", ctx)} start={1.3} sfx={0.6} />
      <Flash frames={4} strength={0.6} />
    </Sequence>
  </AbsoluteFill>
);
