import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, Sequence, staticFile, useVideoConfig } from "remotion";
import { COLORS, COPY } from "../lib/copy";
import { TITLE } from "../lib/fonts";
import { Chip, Clip, clip, Ctx, Flash, outline, Slam, useUnit } from "../lib/ui";

const FLY = ["beach", "forest", "jungle", "desert", "city", "night"];
const CUT = 12; // кадров на арену — ровно один бит

/** 10.4–12.8 с: шесть арен по одной на бит, счётчик 1→6. */
export const Arenas: React.FC<Ctx> = (ctx) => {
  const u = useUnit();
  const { width } = useVideoConfig();
  const c = COPY[ctx.lang];
  const v = ctx.fmt === "v";
  return (
    <AbsoluteFill>
      {FLY.map((name, i) => (
        <Sequence key={name} from={i * CUT} durationInFrames={i === FLY.length - 1 ? 2 * CUT : CUT}>
          <Clip file={clip("cine_fly_" + name, ctx, false)} start={0.9} sfx={0.35} />
          <Flash frames={3} strength={0.45} />
          <AbsoluteFill
            style={{
              flexDirection: v ? "column" : "row",
              justifyContent: v ? "flex-start" : "flex-start",
              alignItems: v ? "center" : "flex-end",
              padding: v ? `${260 * u}px 0 0 0` : `0 0 ${110 * u}px ${110 * u}px`,
              gap: 26 * u,
            }}
          >
            <Slam text={String(i + 1)} size={(v ? 300 : 280) * u} color={COLORS.pink} rotate={-6} />
            <div style={{ display: "flex", flexDirection: "column", alignItems: v ? "center" : "flex-start", gap: 18 * u, paddingBottom: v ? 0 : 40 * u }}>
              <div
                style={{
                  fontFamily: TITLE,
                  fontWeight: 900,
                  fontSize: (v ? 110 : 120) * u,
                  color: COLORS.yellow,
                  textShadow: outline(8 * u),
                  lineHeight: 1,
                }}
              >
                {c.arenasWord[i]}
              </div>
              <Chip text={c.arenas[i]} size={38 * u} bg={COLORS.cyan} color={COLORS.ink} style={{ maxWidth: width * 0.8 }} />
            </div>
          </AbsoluteFill>
          <Audio src={staticFile("sfx/punch.wav")} volume={0.45} />
        </Sequence>
      ))}
    </AbsoluteFill>
  );
};
