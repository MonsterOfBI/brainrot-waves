using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Всё, что сохраняется между забегами. Хранится одной JSON-строкой,
    /// поэтому позже её легко положить в облако Яндекс Игр / VK Игр.
    /// </summary>
    [Serializable]
    public class Profile
    {
        public int version = 1;
        public int crystals;
        public int crystalsEarnedTotal;
        public List<string> labKeys = new List<string>();
        public List<int> labRanks = new List<int>();
        public List<int> unlockedHeroes = new List<int>();
        public int bestArena;
        public int wins;
        public int runs;
        public int totalKills;
        public int bestCombo;
        public int threatUnlocked;     // максимальный открытый уровень угрозы
        public int selectedThreat;
        public int lastHero;            // BrainrotId последнего выбранного героя
        public int lastMode;            // GameMode
        // Достижения (для открытия героев)
        public int bossesTotal;
        public int bestSurvivalWave;
        public int bestDefenseWave;
        public int defenseWins;

        public int LabRank(string key)
        {
            int i = labKeys.IndexOf(key);
            return i >= 0 ? labRanks[i] : 0;
        }

        public void SetLabRank(string key, int rank)
        {
            int i = labKeys.IndexOf(key);
            if (i >= 0) labRanks[i] = rank;
            else
            {
                labKeys.Add(key);
                labRanks.Add(rank);
            }
        }

        public bool HeroUnlocked(BrainrotId id) => unlockedHeroes.Contains((int)id) || Meta.HeroPrice(id) <= 0 || Meta.GoalMet(Brainrots.Get(id));
    }

    /// <summary>Одно постоянное улучшение в Лаборатории.</summary>
    public class LabUpgrade
    {
        public string key;
        public LS name;
        public LS description;   // {0} — суммарный эффект
        public IconKind icon;
        public Color color;
        public int maxRank;
        public int baseCost;
        public float perRank;
        public Func<float, string> format;
        public Action<PlayerStats, float> apply; // (stats, суммарный эффект)

        public int CostForRank(int currentRank) => Mathf.RoundToInt(baseCost * Mathf.Pow(Meta.CostGrowth, currentRank));
        public float Total(int rank) => perRank * rank;
    }

    /// <summary>
    /// Экономика между забегами.
    /// Кристаллы мозга даются за каждый забег (даже проигранный), тратятся в Лаборатории
    /// на постоянные улучшения и на открытие новых героев. Цены растут в 1.65 раза за ранг —
    /// так первые ранги покупаются почти сразу, а последние требуют нескольких хороших забегов.
    /// </summary>
    public static class Meta
    {
        const string Key = "BW_Profile";
        public const float CostGrowth = 1.65f;
        public const int MaxThreat = 5;

        static Profile profile;

        public static Profile P
        {
            get
            {
                if (profile == null) Load();
                return profile;
            }
        }

        static Color C(string hex) => GameAssets.Hex(hex);

        static string Pct(float v) => "+" + Mathf.RoundToInt(v * 100f) + "%";
        static string MinusPct(float v) => "−" + Mathf.RoundToInt(v * 100f) + "%";

        public static readonly LabUpgrade[] Lab =
        {
            new LabUpgrade { key = "hp", name = new LS("Живучесть", "Vitality"), description = new LS("{0} к максимуму здоровья", "{0} max health"),
                icon = IconKind.Heart, color = C("#FF5F7E"), maxRank = 5, baseCost = 12, perRank = 0.10f, format = Pct,
                apply = (s, v) => s.maxHealthMultiplier += v },
            new LabUpgrade { key = "dmg", name = new LS("Сила", "Power"), description = new LS("{0} к урону", "{0} damage"),
                icon = IconKind.Sword, color = C("#FF8A3D"), maxRank = 5, baseCost = 15, perRank = 0.06f, format = Pct,
                apply = (s, v) => s.damageMultiplier += v },
            new LabUpgrade { key = "rate", name = new LS("Быстрые пальцы", "Quick fingers"), description = new LS("{0} к скорострельности", "{0} fire rate"),
                icon = IconKind.Bullet, color = C("#FFC93C"), maxRank = 5, baseCost = 15, perRank = 0.05f, format = Pct,
                apply = (s, v) => s.fireRateMultiplier += v },
            new LabUpgrade { key = "armor", name = new LS("Броня", "Armor"), description = new LS("{0} получаемого урона", "{0} damage taken"),
                icon = IconKind.Shield, color = C("#7FA7FF"), maxRank = 5, baseCost = 20, perRank = 0.04f, format = MinusPct,
                apply = (s, v) => s.damageTakenMultiplier *= 1f - v },
            new LabUpgrade { key = "speed", name = new LS("Кроссовки", "Sneakers"), description = new LS("{0} к скорости бега", "{0} move speed"),
                icon = IconKind.Boot, color = C("#3DE0FF"), maxRank = 3, baseCost = 12, perRank = 0.04f, format = Pct,
                apply = (s, v) => s.moveSpeedMultiplier += v },
            new LabUpgrade { key = "crit", name = new LS("Глаз-алмаз", "Eagle eye"), description = new LS("{0} шанс крита", "{0} crit chance"),
                icon = IconKind.Crosshair, color = C("#FFE066"), maxRank = 4, baseCost = 20, perRank = 0.03f, format = Pct,
                apply = (s, v) => s.critChance += v },
            new LabUpgrade { key = "regen", name = new LS("Свежий воздух", "Fresh air"), description = new LS("+{0} здоровья в секунду", "+{0} health per second"),
                icon = IconKind.Plus, color = C("#5CFF8A"), maxRank = 4, baseCost = 25, perRank = 0.25f, format = v => v.ToString("0.##"),
                apply = (s, v) => s.regenPerSecond += v },
            new LabUpgrade { key = "greed", name = new LS("Жадность", "Greed"), description = new LS("{0} монет в забеге", "{0} coins in a run"),
                icon = IconKind.Coin, color = C("#FFC107"), maxRank = 5, baseCost = 15, perRank = 0.10f, format = Pct,
                apply = (s, v) => s.coinMultiplier += v },
            new LabUpgrade { key = "magnet", name = new LS("Магнит", "Magnet"), description = new LS("{0} к радиусу сбора монет", "{0} pickup radius"),
                icon = IconKind.Magnet, color = C("#E066FF"), maxRank = 3, baseCost = 10, perRank = 0.25f, format = Pct,
                apply = (s, v) => s.coinMagnetRadius *= 1f + v },
            new LabUpgrade { key = "capital", name = new LS("Стартовый капитал", "Starting cash"), description = new LS("+{0} монет в начале забега", "+{0} coins at run start"),
                icon = IconKind.Chest, color = C("#FFB300"), maxRank = 4, baseCost = 20, perRank = 20f, format = v => Mathf.RoundToInt(v).ToString(),
                apply = (s, v) => s.startCoins += Mathf.RoundToInt(v) },
            new LabUpgrade { key = "ability", name = new LS("Двойной эспрессо", "Double espresso"), description = new LS("{0} к перезарядке способности", "{0} ability cooldown"),
                icon = IconKind.Bolt, color = C("#B388FF"), maxRank = 4, baseCost = 25, perRank = 0.06f, format = MinusPct,
                apply = (s, v) => s.abilityCooldownMultiplier *= 1f - v },
            new LabUpgrade { key = "reroll", name = new LS("Переброс", "Reroll"), description = new LS("+{0} перебросов улучшений за забег", "+{0} upgrade rerolls per run"),
                icon = IconKind.Reroll, color = C("#4FC3F7"), maxRank = 3, baseCost = 40, perRank = 1f, format = v => Mathf.RoundToInt(v).ToString(),
                apply = (s, v) => s.rerolls += Mathf.RoundToInt(v) },
            new LabUpgrade { key = "revive", name = new LS("Второй шанс", "Second chance"), description = new LS("Один раз за забег воскреснуть с половиной здоровья", "Revive once per run with half health"),
                icon = IconKind.Star, color = C("#FFD54F"), maxRank = 1, baseCost = 150, perRank = 1f, format = v => Mathf.RoundToInt(v).ToString(),
                apply = (s, v) => s.revives += Mathf.RoundToInt(v) },
            new LabUpgrade { key = "wisdom", name = new LS("Мудрость", "Wisdom"), description = new LS("{0} кристаллов за забег", "{0} crystals per run"),
                icon = IconKind.Crystal, color = C("#64FFDA"), maxRank = 3, baseCost = 40, perRank = 0.10f, format = Pct,
                apply = (s, v) => s.crystalMultiplier += v },
        };

        // ---------- Герои ----------

        /// <summary>
        /// Цена открытия героя в кристаллах (0 — открыт сразу).
        /// Героя можно открыть двумя путями (как в Vampire Survivors и Hades): выполнить задание бесплатно или купить.
        /// </summary>
        public static int HeroPrice(BrainrotId id) => Brainrots.Get(id).price;

        /// <summary>Сколько уже сделано для задания открытия.</summary>
        public static int GoalProgress(UnlockGoal goal)
        {
            var p = P;
            switch (goal)
            {
                case UnlockGoal.StoryArenas: return p.bestArena;
                case UnlockGoal.StoryWins: return p.wins;
                case UnlockGoal.BossesTotal: return p.bossesTotal;
                case UnlockGoal.SurvivalWave: return p.bestSurvivalWave;
                case UnlockGoal.DefenseWave: return p.bestDefenseWave;
                case UnlockGoal.KillsTotal: return p.totalKills;
                default: return 0;
            }
        }

        public static bool GoalMet(BrainrotInfo info) => info.goal != UnlockGoal.None && GoalProgress(info.goal) >= info.goalValue;

        /// <summary>Текст задания: «Пройди 2 арены в сюжете (1/2)».</summary>
        public static string GoalText(BrainrotInfo info)
        {
            if (info.goal == UnlockGoal.None) return "";
            int v = info.goalValue;
            string what;
            switch (info.goal)
            {
                case UnlockGoal.StoryArenas: what = Loc.T("Пройди арен в сюжете: " + v, "Clear story arenas: " + v); break;
                case UnlockGoal.StoryWins: what = Loc.T("Пройди сюжет до конца", "Beat the story"); break;
                case UnlockGoal.BossesTotal: what = Loc.T("Победи боссов: " + v, "Defeat bosses: " + v); break;
                case UnlockGoal.SurvivalWave: what = Loc.T("Доживи до волны " + v + " в выживании", "Reach wave " + v + " in survival"); break;
                case UnlockGoal.DefenseWave: what = Loc.T("Продержись " + v + " волн в защите", "Hold " + v + " waves in defense"); break;
                default: what = Loc.T("Победи врагов: " + v, "Defeat enemies: " + v); break;
            }
            return what + " (" + Mathf.Min(GoalProgress(info.goal), v) + "/" + v + ")";
        }

        public static bool TryUnlockHero(BrainrotId id)
        {
            if (P.HeroUnlocked(id))
            {
                if (!P.unlockedHeroes.Contains((int)id)) { P.unlockedHeroes.Add((int)id); Save(); }
                return true;
            }
            int price = HeroPrice(id);
            if (P.crystals < price) return false;
            P.crystals -= price;
            P.unlockedHeroes.Add((int)id);
            Save();
            return true;
        }

        // ---------- Уровни угрозы (как «Жар» в Hades или «Опасность» в Brotato) ----------

        public static float ThreatHealth(int threat) => 1f + 0.18f * threat;
        public static float ThreatDamage(int threat) => 1f + 0.12f * threat;
        public static float ThreatReward(int threat) => 1f + 0.25f * threat;

        // ---------- Лаборатория ----------

        public static int Rank(LabUpgrade u) => P.LabRank(u.key);

        public static bool CanBuy(LabUpgrade u)
        {
            int r = Rank(u);
            return r < u.maxRank && P.crystals >= u.CostForRank(r);
        }

        public static bool Buy(LabUpgrade u)
        {
            if (!CanBuy(u)) return false;
            int r = Rank(u);
            P.crystals -= u.CostForRank(r);
            P.SetLabRank(u.key, r + 1);
            Save();
            return true;
        }

        /// <summary>Сколько кристаллов вложено в Лабораторию (вернутся при сбросе).</summary>
        public static int SpentInLab()
        {
            int sum = 0;
            foreach (var u in Lab)
                for (int r = 0; r < Rank(u); r++) sum += u.CostForRank(r);
            return sum;
        }

        public static void RefundLab()
        {
            P.crystals += SpentInLab();
            P.labKeys.Clear();
            P.labRanks.Clear();
            Save();
        }

        public static void ApplyLab(PlayerStats stats)
        {
            foreach (var u in Lab)
            {
                int r = Rank(u);
                if (r > 0) u.apply(stats, u.Total(r));
            }
        }

        // ---------- Награда за забег ----------

        public class RunReward
        {
            public int waves, bosses, kills, coins, combo;
            public int fromWaves, fromBosses, fromKills, fromCoins, fromCombo, fromVictory;
            public float multiplier;
            public int total;
            public bool newRecord;
            public bool threatUnlocked;
            public readonly List<BrainrotInfo> unlockedHeroes = new List<BrainrotInfo>();
        }

        public static RunReward ComputeReward(bool victory)
        {
            var r = new RunReward
            {
                waves = RunState.wavesCleared,
                bosses = RunState.bossesKilled,
                kills = RunState.kills,
                coins = RunState.coinsCollected,
                combo = RunState.bestCombo
            };
            // Экономика 2.0: героев открывают за десятки забегов, а не за один
            bool story = RunState.mode == GameMode.Story;
            r.fromWaves = r.waves * (story ? 1 : 2);
            r.fromBosses = r.bosses * (story ? 15 : 10);
            r.fromKills = r.kills / 6;
            r.fromCoins = r.coins / 15;
            r.fromCombo = Mathf.Min(r.combo / 10, 10);
            r.fromVictory = victory ? (story ? 40 : 50) : 0;
            r.multiplier = (story ? ThreatReward(RunState.threat) : 1f) * RunState.stats.crystalMultiplier;
            int sum = r.fromWaves + r.fromBosses + r.fromKills + r.fromCoins + r.fromCombo + r.fromVictory;
            r.total = Mathf.Max(1, Mathf.RoundToInt(sum * r.multiplier));
            return r;
        }

        /// <summary>Записывает итог забега в профиль и сохраняет.</summary>
        public static RunReward FinishRun(bool victory)
        {
            var reward = ComputeReward(victory);
            var p = P;
            p.crystals += reward.total;
            p.crystalsEarnedTotal += reward.total;
            p.runs++;
            p.totalKills += RunState.kills;
            if (RunState.bestCombo > p.bestCombo) p.bestCombo = RunState.bestCombo;
            var locked = new List<BrainrotInfo>();
            foreach (var h in Brainrots.All) if (!p.HeroUnlocked(h.id)) locked.Add(h);
            p.bossesTotal += RunState.bossesKilled;

            if (RunState.mode == GameMode.Survival)
            {
                if (RunState.wavesCleared > p.bestSurvivalWave) { p.bestSurvivalWave = RunState.wavesCleared; reward.newRecord = true; }
            }
            else if (RunState.mode == GameMode.Defense)
            {
                if (RunState.wavesCleared > p.bestDefenseWave) { p.bestDefenseWave = RunState.wavesCleared; reward.newRecord = true; }
                if (victory) p.defenseWins++;
            }
            else
            {
                int reached = victory ? RunState.ArenaCount : RunState.arenaIndex;
                if (reached > p.bestArena)
                {
                    p.bestArena = reached;
                    reward.newRecord = true;
                }
                if (victory) p.wins++;
            }

            // Кто открылся за этот забег по заданиям
            foreach (var h in locked)
                if (p.HeroUnlocked(h.id)) { reward.unlockedHeroes.Add(h); p.unlockedHeroes.Add((int)h.id); }

            if (victory && RunState.mode == GameMode.Story)
            {
                if (RunState.threat >= p.threatUnlocked && p.threatUnlocked < MaxThreat)
                {
                    p.threatUnlocked = RunState.threat + 1;
                    reward.threatUnlocked = true;
                }
            }
            Save();
            return reward;
        }

        // ---------- Сохранение ----------

        public static void Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            profile = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { profile = JsonUtility.FromJson<Profile>(json); }
                catch { profile = null; }
            }
            if (profile == null)
            {
                profile = new Profile();
                // Перенос старых рекордов
                profile.bestArena = PlayerPrefs.GetInt("BW_BestArena", 0);
                profile.wins = PlayerPrefs.GetInt("BW_Wins", 0);
                profile.lastHero = PlayerPrefs.GetInt("BW_LastHero", 0);
            }
            if (profile.labKeys == null) profile.labKeys = new List<string>();
            if (profile.labRanks == null) profile.labRanks = new List<int>();
            if (profile.unlockedHeroes == null) profile.unlockedHeroes = new List<int>();
            while (profile.labRanks.Count < profile.labKeys.Count) profile.labRanks.Add(0);
            if (profile.version < 2)
            {
                // Переход на экономику 2.0: уже открытые герои остаются открытыми
                profile.version = 2;
                profile.bossesTotal = Mathf.Max(profile.bossesTotal, profile.wins * 5);
            }
            // Победы из старой версии игры открывают первый уровень угрозы
            if (profile.wins > 0 && profile.threatUnlocked == 0) profile.threatUnlocked = 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(P));
            PlayerPrefs.Save();
        }

        /// <summary>Для разработчика: стереть весь прогресс.</summary>
        public static void ResetAll()
        {
            profile = new Profile();
            Save();
        }
    }
}
