import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, Easing, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COLORS, COPY } from "../lib/copy";
import { BODY } from "../lib/fonts";
import { f, MODE_AT, S } from "../lib/timeline";
import { Clip, clip, Ctx, fit, Flash, MenuBackdrop, outline, Slam, useUnit } from "../lib/ui";

const PANELS = [
  { id: "rifle_beach", start: 0.0, color: COLORS.cyan },
  { id: "survival_city", start: 0.0, color: COLORS.pink },
  { id: "defense_pizzeria", start: 0.0, color: COLORS.yellow },
];
const EXPAND_AT = 25.75;

/** 22.4–26.4 с: три режима на разделённом экране, по одному на слово диктора; защита раскрывается на весь кадр. */
export const Modes: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width, height } = useVideoConfig();
  const v = ctx.fmt === "v";
  const c = COPY[ctx.lang];
  const gap = 10 * u;
  const n = PANELS.length;
  const pw = v ? width : (width - gap * (n - 1)) / n;
  const ph = v ? (height - gap * (n - 1)) / n : height;
  const expand = f(EXPAND_AT - S.modes[0]);

  return (
    <AbsoluteFill style={{ backgroundColor: COLORS.ink }}>
      <MenuBackdrop seed="modes" dark={0.4} />
      {PANELS.map((p, i) => {
        const at = f(MODE_AT[ctx.lang][i] - S.modes[0]);
        const t = frame - at;
        if (t < 0) return null;
        const x = v ? 0 : i * (pw + gap);
        const y = v ? i * (ph + gap) : 0;
        const label = c.modes[i];
        return (
          <div
            key={p.id}
            style={{
              position: "absolute",
              left: x,
              top: y,
              width: pw,
              height: ph,
              overflow: "hidden",
              borderRadius: 6 * u,
              translate: interpolate(t, [0, 8], [v ? `${width}px 0px` : `0px ${height}px`, "0px 0px"], {
                extrapolateRight: "clamp",
                easing: Easing.out(Easing.cubic),
              }),
            }}
          >
            <Sequence from={at} layout="none">
              <div style={{ position: "absolute", width, height, left: (pw - width) / 2, top: (ph - height) / 2 }}>
                <Clip file={clip(p.id, ctx)} start={p.start + 0.15} sfx={i === n - 1 ? 0.5 : 0.3} punch={false} />
              </div>
              <AbsoluteFill style={{ background: `linear-gradient(0deg, ${p.color}AA 0%, rgba(0,0,0,0) 45%)` }} />
              <AbsoluteFill style={{ justifyContent: "flex-end", alignItems: "center", paddingBottom: (v ? 50 : 120) * u, gap: 6 * u }}>
                <Slam text={label} size={fit(label, (v ? 110 : 96) * u, pw * 0.9)} color="white" />
                <div style={{ fontFamily: BODY, fontWeight: 800, fontSize: (v ? 40 : 38) * u, color: "white", textShadow: outline(4 * u) }}>
                  {c.modesSub[i]}
                </div>
              </AbsoluteFill>
              <Flash frames={5} strength={0.6} />
              <Audio src={staticFile("sfx/whoosh.wav")} volume={0.5} />
              <Audio src={staticFile("sfx/punch.wav")} volume={0.6} />
            </Sequence>
          </div>
        );
      })}
      <Sequence from={expand}>
        <Clip file={clip("defense_pizzeria", ctx)} start={2.6} sfx={0.55} />
        <AbsoluteFill style={{ background: `linear-gradient(0deg, ${COLORS.yellow}99 0%, rgba(0,0,0,0) 35%)` }} />
        <AbsoluteFill style={{ justifyContent: "flex-end", alignItems: "center", paddingBottom: (v ? 330 : 120) * u, gap: 6 * u }}>
          <Slam text={c.modes[2]} size={fit(c.modes[2], 130 * u, width * 0.8)} color="white" />
          <div style={{ fontFamily: BODY, fontWeight: 800, fontSize: 48 * u, color: "white", textShadow: outline(5 * u) }}>{c.modesSub[2]}</div>
        </AbsoluteFill>
        <Flash frames={5} strength={0.7} />
      </Sequence>
    </AbsoluteFill>
  );
};
