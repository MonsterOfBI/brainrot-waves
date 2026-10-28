export type Lang = "en" | "ru";
export type Fmt = "h" | "v";

/** Все тексты ролика на двух языках. */
export const COPY = {
  en: {
    tung: ["TUNG!", "TUNG!", "TUNG!"],
    waves: ["THEY COME", "IN WAVES"],
    tagline: "THE ITALIAN BRAINROT WAVE SHOOTER",
    horde: "BLAST THE HORDE",
    arenasWord: ["ARENA", "ARENAS", "ARENAS", "ARENAS", "ARENAS", "ARENAS"],
    arenas: ["Tralalero Beach", "Patapim Forest", "Chimpanzini Jungle", "Bombardiro Desert", "Cappuccino City", "Sahur Night Village"],
    rosterTitle: "BRAINROTS",
    rosterSub: "PLAY AS ANY OF THEM",
    skills: "Q / E SKILLS",
    boss: "BOSS FIGHTS",
    modes: ["STORY", "SURVIVAL", "DEFENSE"],
    modesSub: ["5 arenas · 5 bosses", "endless waves", "save Nonna's pizzeria"],
    endTitle: "PLAY FREE",
    endSub: "RIGHT IN YOUR BROWSER",
    button: "PLAY NOW",
  },
  ru: {
    tung: ["ТУНГ!", "ТУНГ!", "ТУНГ!"],
    waves: ["ОНИ ИДУТ", "ВОЛНАМИ"],
    tagline: "ШУТЕР С ВОЛНАМИ ИТАЛЬЯНСКИХ БРЕЙНРОТОВ",
    horde: "РАЗНЕСИ ОРДУ",
    arenasWord: ["АРЕНА", "АРЕНЫ", "АРЕНЫ", "АРЕНЫ", "АРЕН", "АРЕН"],
    arenas: ["Пляж Тралалеро", "Лес Патапима", "Джунгли Шимпанзини", "Пустыня Бомбардиро", "Город Капучино", "Ночная деревня Сахур"],
    rosterTitle: "БРЕЙНРОТОВ",
    rosterSub: "ИГРАЙ ЗА ЛЮБОГО",
    skills: "НАВЫКИ Q / E",
    boss: "БИТВЫ С БОССАМИ",
    modes: ["СЮЖЕТ", "ВЫЖИВАНИЕ", "ЗАЩИТА"],
    modesSub: ["5 арен · 5 боссов", "бесконечные волны", "спаси пиццерию Нонны"],
    endTitle: "ИГРАЙ БЕСПЛАТНО",
    endSub: "ПРЯМО В БРАУЗЕРЕ",
    button: "ИГРАТЬ",
  },
} as const;

export const URL_TEXT = "brainrot-waves.vercel.app";

export const COLORS = {
  yellow: "#FFC93C",
  pink: "#FF5FA2",
  cyan: "#3DE0FF",
  green: "#5CFF8A",
  purple: "#5E35B1",
  deep: "#1B0F33",
  menu: "#3B2063",
  play: "#E8335F",
  ink: "#140A26",
};
