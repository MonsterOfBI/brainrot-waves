import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, Easing, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { COLORS, COPY, URL_TEXT } from "../lib/copy";
import { BODY, TITLE } from "../lib/fonts";
import { Clip, clip, Ctx, fit, Flash, Logo, outline, Portrait, Slam, useShake, useUnit } from "../lib/ui";

const CROWD = ["Chimpanzini", "Cappuccino", "Tralalero", "TungTung", "Bombardiro", "Patapim", "Ballerina"];

/** 26.4–30 с: логотип, «играй бесплатно в браузере», адрес и кнопка. */
export const EndCard: React.FC<Ctx> = (ctx) => {
  const frame = useCurrentFrame();
  const u = useUnit();
  const { width, height } = useVideoConfig();
  const v = ctx.fmt === "v";
  const c = COPY[ctx.lang];
  const shake = useShake([0], 22 * u);
  const logoSize = v ? fit("BRAINROT", 190 * u, width * 0.88) : fit("BRAINROT WAVES", 170 * u, width * 0.8);
  const pulse = 1 + Math.max(0, Math.sin((frame - 26) / 5)) * 0.06;
  const crowdS = (v ? 330 : 300) * u;

  return (
    <AbsoluteFill style={{ translate: shake }}>
      <Clip
        file={clip("cine_fly_beach", ctx, false)}
        start={0.4}
        rate={0.6}
        sfx={0}
        punch={false}
        videoStyle={{ filter: "blur(14px) brightness(0.5) saturate(1.25)" }}
      />
      <AbsoluteFill style={{ background: `linear-gradient(180deg, ${COLORS.menu}CC 0%, ${COLORS.deep}E6 100%)` }} />
      {CROWD.map((id, i) => {
        const t = frame - 6 - i * 2;
        const x = (width / (CROWD.length + 1)) * (i + 1) - crowdS / 2;
        return (
          <Portrait
            key={id}
            id={id}
            style={{
              position: "absolute",
              width: crowdS,
              height: crowdS,
              left: x,
              top: height - crowdS * (v ? 0.78 : 0.74),
              translate: interpolate(t, [0, 8], [`0px ${crowdS}px`, "0px 0px"], {
                extrapolateLeft: "clamp",
                extrapolateRight: "clamp",
                easing: Easing.out(Easing.back(1.8)),
              }),
              rotate: `${(i % 2 ? 1 : -1) * (6 + Math.sin(frame / 6 + i) * 4)}deg`,
            }}
          />
        );
      })}
      <AbsoluteFill
        style={{
          justifyContent: "center",
          alignItems: "center",
          gap: (v ? 36 : 26) * u,
          paddingBottom: (v ? 380 : 220) * u,
        }}
      >
        <Logo size={logoSize} twoLines={v} />
        <Slam text={c.endTitle} size={fit(c.endTitle, 110 * u, width * 0.86)} color="white" delay={10} />
        <div
          style={{
            fontFamily: BODY,
            fontWeight: 800,
            fontSize: 50 * u,
            color: COLORS.cyan,
            textShadow: outline(5 * u),
            opacity: interpolate(frame, [14, 20], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }),
          }}
        >
          {c.endSub}
        </div>
        <div
          style={{
            display: "flex",
            flexDirection: v ? "column" : "row",
            alignItems: "center",
            gap: 26 * u,
            marginTop: 14 * u,
            opacity: interpolate(frame, [20, 26], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }),
            translate: interpolate(frame, [20, 28], ["0px 40px", "0px 0px"], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
              easing: Easing.out(Easing.cubic),
            }),
          }}
        >
          <div
            style={{
              fontFamily: TITLE,
              fontWeight: 900,
              fontSize: 64 * u,
              color: "white",
              background: COLORS.play,
              padding: `${22 * u}px ${70 * u}px`,
              borderRadius: 26 * u,
              boxShadow: `0 ${10 * u}px 0 #9E1A3E, 0 ${18 * u}px ${40 * u}px rgba(0,0,0,0.5)`,
              scale: pulse,
            }}
          >
            {c.button}
          </div>
          <div
            style={{
              fontFamily: BODY,
              fontWeight: 800,
              fontSize: 48 * u,
              color: "white",
              background: "rgba(10,4,30,0.75)",
              border: `${4 * u}px solid ${COLORS.cyan}`,
              padding: `${16 * u}px ${36 * u}px`,
              borderRadius: 999,
            }}
          >
            {URL_TEXT}
          </div>
        </div>
      </AbsoluteFill>
      <Flash frames={8} strength={1} />
      <Audio src={staticFile("sfx/slam.wav")} volume={0.75} />
      <Audio src={staticFile("sfx/crunch.wav")} volume={0.7} />
      <Sequence from={10}>
        <Audio src={staticFile("sfx/victory.wav")} volume={0.55} />
      </Sequence>
    </AbsoluteFill>
  );
};
