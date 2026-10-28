import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, Easing, interpolate, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COPY } from "../lib/copy";
import { BODY } from "../lib/fonts";
import { Ctx, fit, Flash, Logo, MenuBackdrop, outline, Portrait, useShake, useUnit } from "../lib/ui";

const BURST = ["Tralalero", "TungTung", "Bombardiro", "Chimpanzini", "Cappuccino", "Patapim", "Ballerina", "Lirili", "Boneca", "Glorbo", "Bobritto", "Girafa"];

/** 4.4–6.8 с: дроп музыки, логотип как в меню, брейнроты разлетаются от центра. */
export const LogoScene: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width, height } = useVideoConfig();
  const v = ctx.fmt === "v";
  const c = COPY[ctx.lang];
  const shake = useShake([0, 1], 26 * u);
  const logoSize = v ? fit("BRAINROT", 210 * u, width * 0.9) : fit("BRAINROT WAVES", 200 * u, width * 0.9);
  const R = Math.hypot(width, height) * 0.42;

  return (
    <AbsoluteFill style={{ translate: shake }}>
      <MenuBackdrop seed="logo" />
      {BURST.map((id, i) => {
        const a = (i / BURST.length) * Math.PI * 2 + 0.3;
        const k = interpolate(frame, [0, 26, 72], [0.15, 0.85, 1.05], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) });
        const s = 360 * u;
        return (
          <Portrait
            key={id}
            id={id}
            style={{
              position: "absolute",
              width: s,
              height: s,
              left: width / 2 - s / 2 + Math.cos(a) * R * k * (v ? 0.7 : 1),
              top: height / 2 - s / 2 + Math.sin(a) * R * k * (v ? 1.1 : 0.75),
              scale: interpolate(frame, [0, 20], [0.2, 1.25], { extrapolateRight: "clamp", easing: Easing.out(Easing.back(1.6)) }),
              rotate: `${(i % 2 ? 1 : -1) * (10 + frame * 0.6)}deg`,
              opacity: 0.95,
            }}
          />
        );
      })}
      <AbsoluteFill style={{ justifyContent: "center", alignItems: "center", gap: 30 * u }}>
        <Logo size={logoSize} twoLines={v} />
        <div
          style={{
            fontFamily: BODY,
            fontWeight: 800,
            fontSize: (v ? 50 : 54) * u,
            color: "white",
            letterSpacing: 2 * u,
            textAlign: "center",
            maxWidth: width * 0.86,
            textShadow: outline(5 * u, "#2A1240"),
            opacity: interpolate(frame, [10, 16], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }),
            translate: interpolate(frame, [10, 18], ["0px 40px", "0px 0px"], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
              easing: Easing.out(Easing.cubic),
            }),
          }}
        >
          {c.tagline}
        </div>
      </AbsoluteFill>
      <Flash frames={8} strength={1} />
      <Audio src={staticFile("sfx/slam.wav")} volume={0.75} />
      <Audio src={staticFile("sfx/crunch.wav")} volume={0.8} />
    </AbsoluteFill>
  );
};
