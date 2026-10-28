import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COLORS, COPY } from "../lib/copy";
import { f, S, TUNG_AT, VO } from "../lib/timeline";
import { Clip, clip, Ctx, Flash, Slam, useShake, useUnit } from "../lib/ui";

/** 0–4.4 с: «Тунг… тунг… тунг…» — стробоскоп по гигантскому Тунг Тунг Сахуру, затем «Они идут волнами». */
export const Hook: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width } = useVideoConfig();
  const c = COPY[ctx.lang];
  const v = ctx.fmt === "v";
  const tung = TUNG_AT[ctx.lang].map((t) => f(t - S.hook[0]));
  const wavesAt = f(VO[ctx.lang][1].at - S.hook[0]);
  const word2 = wavesAt + f(ctx.lang === "en" ? 0.78 : 0.88);

  let vis = 0;
  for (const t of tung) {
    if (frame >= t) vis = Math.max(vis, interpolate(frame - t, [0, 12], [1, 0.32], { extrapolateRight: "clamp" }));
  }
  if (frame >= wavesAt - 3) vis = 1;
  const shake = useShake([...tung, wavesAt, word2], 22 * u);

  const pos = v
    ? [
        { x: 0, y: -520 },
        { x: 0, y: 0 },
        { x: 0, y: 520 },
      ]
    : [
        { x: -560, y: -150 },
        { x: 0, y: 120 },
        { x: 560, y: -60 },
      ];
  const tungSize = (v ? 200 : 210) * u;

  return (
    <AbsoluteFill style={{ backgroundColor: "black" }}>
      <AbsoluteFill style={{ opacity: vis, translate: shake }}>
        <Clip file={clip("hook_tung", ctx, false)} start={0.25} punch={false} sfx={0.35} />
      </AbsoluteFill>
      <AbsoluteFill
        style={{ background: "radial-gradient(circle at 50% 50%, rgba(0,0,0,0) 45%, rgba(10,4,30,0.75) 100%)" }}
      />
      {tung.map((t, i) => (
        <Sequence key={i} from={t} durationInFrames={Math.max(1, wavesAt - t)}>
          <AbsoluteFill style={{ justifyContent: "center", alignItems: "center", translate: shake }}>
            <Slam
              text={c.tung[i]}
              size={tungSize}
              color={i === 2 ? COLORS.pink : COLORS.yellow}
              rotate={[-8, 5, -4][i]}
              style={{ translate: `${pos[i].x * u}px ${pos[i].y * u}px` }}
            />
          </AbsoluteFill>
          <Flash frames={5} strength={0.55} />
          <Audio src={staticFile("sfx/slam.wav")} volume={0.7} />
          <Audio src={staticFile("sfx/punch.wav")} volume={0.8} />
        </Sequence>
      ))}
      <Sequence from={wavesAt}>
        <AbsoluteFill style={{ justifyContent: "center", alignItems: "center", gap: 10 * u, translate: shake }}>
          <Slam text={c.waves[0]} size={Math.min(150 * u, (width * 0.9) / (c.waves[0].length * 0.86))} />
          <Slam
            text={c.waves[1]}
            size={Math.min(170 * u, (width * 0.9) / (c.waves[1].length * 0.86))}
            color={COLORS.pink}
            delay={word2 - wavesAt}
          />
        </AbsoluteFill>
        <Flash frames={6} strength={0.5} />
        <Audio src={staticFile("sfx/slam.wav")} volume={0.7} />
        <Sequence from={word2 - wavesAt}>
          <Audio src={staticFile("sfx/crunch.wav")} volume={0.6} />
        </Sequence>
      </Sequence>
    </AbsoluteFill>
  );
};
