import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COLORS, COPY } from "../lib/copy";
import { f } from "../lib/timeline";
import { Chip, Clip, clip, Ctx, fit, Flash, Slam, useUnit } from "../lib/ui";

const CUTS = [
  { id: "rifle_beach", start: 2.0, dur: 1.2 },
  { id: "shotgun_jungle", start: 1.2, dur: 1.2 },
  { id: "ability_bomb", start: 0.45, dur: 1.2 },
];

/** 6.8–10.4 с: нарезка геймплея в ритм — автомат, дробовик, ковровая бомбардировка. */
export const Gameplay: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width } = useVideoConfig();
  const c = COPY[ctx.lang];
  let at = 0;
  return (
    <AbsoluteFill>
      {CUTS.map((cut) => {
        const from = at;
        at += f(cut.dur);
        return (
          <Sequence key={cut.id} from={from} durationInFrames={f(cut.dur)}>
            <Clip file={clip(cut.id, ctx)} start={cut.start} sfx={0.55} />
            <Flash frames={4} strength={0.6} />
          </Sequence>
        );
      })}
      <Sequence from={3} durationInFrames={f(2.2)}>
        <AbsoluteFill
          style={{
            justifyContent: "center",
            alignItems: "center",
            paddingTop: ctx.fmt === "v" ? 0 : 140 * u,
            opacity: interpolate(frame, [f(2.0), f(2.3)], [1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }),
          }}
        >
          <Slam text={c.horde} size={fit(c.horde, 150 * u, width * 0.88)} rotate={-3} />
        </AbsoluteFill>
        <Audio src={staticFile("sfx/punch.wav")} volume={0.6} />
      </Sequence>
      <Sequence from={f(2.4) + 2}>
        <AbsoluteFill style={{ justifyContent: "flex-end", alignItems: "center", paddingBottom: (ctx.fmt === "v" ? 520 : 200) * u }}>
          <Chip text={ctx.lang === "en" ? "Q · CARPET BOMBING" : "Q · КОВРОВАЯ БОМБАРДИРОВКА"} size={42 * u} bg={COLORS.yellow} color={COLORS.ink} />
        </AbsoluteFill>
      </Sequence>
    </AbsoluteFill>
  );
};
