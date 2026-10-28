import { loadFont } from "@remotion/fonts";
import { staticFile } from "remotion";

// Шрифты самой игры: Unbounded (заголовки) и Exo 2 (текст) — оба вариативные, с кириллицей.
loadFont({ family: "Unbounded", url: staticFile("fonts/Title.ttf"), weight: "200 900" });
loadFont({ family: "Exo2", url: staticFile("fonts/Body.ttf"), weight: "100 900" });

export const TITLE = "Unbounded, sans-serif";
export const BODY = "Exo2, sans-serif";
