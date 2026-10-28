import React from "react";
import { Audio } from "@remotion/media";
import { AbsoluteFill, interpolate, Sequence, staticFile } from "remotion";
import "./lib/fonts";
import { Fmt, Lang } from "./lib/copy";
import { DURATION_S, f, LOGO_DROP, MUSIC_DROP_IN_TRACK, S, VO } from "./lib/timeline";
import { Arenas } from "./scenes/Arenas";
import { Boss } from "./scenes/Boss";
import { EndCard } from "./scenes/EndCard";
import { Gameplay } from "./scenes/Gameplay";
import { Hook } from "./scenes/Hook";
import { LogoScene } from "./scenes/LogoScene";
import { Modes } from "./scenes/Modes";
import { Roster } from "./scenes/Roster";

export type TrailerProps = { lang: Lang; fmt: Fmt };

const scene = (range: readonly [number, number]) => ({ from: f(range[0]), durationInFrames: f(range[1] - range[0]) });

/** Музыка из игры (boss_1.mp3): дроп на логотипе, приглушение под голос диктора, затухание в конце. */
const Music: React.FC<{ lang: Lang }> = ({ lang }) => {
  const lines = VO[lang];
  return (
    <Audio
      src={staticFile("music/boss_1.mp3")}
      trimBefore={f(MUSIC_DROP_IN_TRACK - LOGO_DROP)}
      volume={(fr) => {
        const t = fr / 30;
        const speaking = lines.some((l) => t >= l.at - 0.1 && t <= l.at + l.dur / (l.rate ?? 1) + 0.15);
        const base = t < LOGO_DROP ? 0.42 : 0.5;
        const duck = speaking ? 0.42 : 1;
        const fadeIn = interpolate(t, [0, 0.4], [0, 1], { extrapolateRight: "clamp" });
        const fadeOut = interpolate(t, [DURATION_S - 1.4, DURATION_S], [1, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
        return base * duck * fadeIn * fadeOut;
      }}
    />
  );
};

const Voice: React.FC<{ lang: Lang }> = ({ lang }) => (
  <>
    {VO[lang].map((l, i) => (
      <Sequence key={i} from={f(l.at)} name={`VO ${i + 1}`}>
        <Audio src={staticFile(`vo/${lang}/${i + 1}.wav`)} volume={0.95} playbackRate={l.rate ?? 1} preservePitch />
      </Sequence>
    ))}
  </>
);

export const Trailer: React.FC<TrailerProps> = ({ lang, fmt }) => {
  const ctx = { lang, fmt };
  return (
    <AbsoluteFill style={{ backgroundColor: "black" }}>
      <Sequence name="Hook" {...scene(S.hook)}>
        <Hook {...ctx} />
      </Sequence>
      <Sequence name="Logo" {...scene(S.logo)}>
        <LogoScene {...ctx} />
      </Sequence>
      <Sequence name="Gameplay" {...scene(S.gameplay)}>
        <Gameplay {...ctx} />
      </Sequence>
      <Sequence name="Arenas" {...scene(S.arenas)}>
        <Arenas {...ctx} />
      </Sequence>
      <Sequence name="Roster" {...scene(S.roster)}>
        <Roster {...ctx} />
      </Sequence>
      <Sequence name="Boss" {...scene(S.boss)}>
        <Boss {...ctx} />
      </Sequence>
      <Sequence name="Modes" {...scene(S.modes)}>
        <Modes {...ctx} />
      </Sequence>
      <Sequence name="End" {...scene(S.end)}>
        <EndCard {...ctx} />
      </Sequence>
      <Music lang={lang} />
      <Voice lang={lang} />
    </AbsoluteFill>
  );
};
