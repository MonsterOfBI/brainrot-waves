import React from "react";
import { Composition, Folder } from "remotion";
import { DURATION_S, FPS } from "./lib/timeline";
import { Trailer, TrailerProps } from "./Trailer";

const VARIANTS: { id: string; props: TrailerProps; width: number; height: number }[] = [
  { id: "Trailer-EN-16x9", props: { lang: "en", fmt: "h" }, width: 1920, height: 1080 },
  { id: "Trailer-RU-16x9", props: { lang: "ru", fmt: "h" }, width: 1920, height: 1080 },
  { id: "Trailer-EN-9x16", props: { lang: "en", fmt: "v" }, width: 1080, height: 1920 },
  { id: "Trailer-RU-9x16", props: { lang: "ru", fmt: "v" }, width: 1080, height: 1920 },
];

export const RemotionRoot: React.FC = () => (
  <Folder name="BrainrotWaves">
    {VARIANTS.map((v) => (
      <Composition
        key={v.id}
        id={v.id}
        component={Trailer}
        durationInFrames={DURATION_S * FPS}
        fps={FPS}
        width={v.width}
        height={v.height}
        defaultProps={v.props}
      />
    ))}
  </Folder>
);
