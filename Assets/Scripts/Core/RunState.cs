using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Характеристики героя, которые меняются улучшениями во время забега.</summary>
    public class PlayerStats
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float bonusMaxHealth;
        public float moveSpeedMultiplier = 1f;
        public float magazineMultiplier = 1f;
        public float reloadTimeMultiplier = 1f;
        public float critChance = 0.05f;
        public float healPerKill;
        public float coinMagnetRadius = 4f;
        public float abilityCooldownMultiplier = 1f;
        public float damageTakenMultiplier = 1f;
        public float explosionDamageMultiplier = 1f;
        public float regenPerSecond;
    }

    public enum WeaponId { Pistol, Rifle, Shotgun }

    public class WeaponDef
    {
        public WeaponId id;
        public string name;
        public float damage;
        public float fireRate;
        public bool automatic;
        public int pellets = 1;
        public float spread;
        public int magazine;
        public float reloadTime;
        public int price;
        public float recoil;
        public float shake;

        public static readonly WeaponDef Pistol = new WeaponDef
        {
            id = WeaponId.Pistol, name = "Пистолет", damage = 22f, fireRate = 5f, automatic = false,
            spread = 0.4f, magazine = 12, reloadTime = 1.0f, price = 0, recoil = 0.8f, shake = 0.05f
        };

        public static readonly WeaponDef Rifle = new WeaponDef
        {
            id = WeaponId.Rifle, name = "Автомат", damage = 15f, fireRate = 11f, automatic = true,
            spread = 1.4f, magazine = 32, reloadTime = 1.6f, price = 60, recoil = 0.5f, shake = 0.04f
        };

        public static readonly WeaponDef Shotgun = new WeaponDef
        {
            id = WeaponId.Shotgun, name = "Дробовик", damage = 13f, fireRate = 1.4f, automatic = false,
            pellets = 9, spread = 6.5f, magazine = 6, reloadTime = 1.9f, price = 80, recoil = 1.6f, shake = 0.18f
        };

        public static WeaponDef Get(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Rifle: return Rifle;
                case WeaponId.Shotgun: return Shotgun;
                default: return Pistol;
            }
        }
    }

    /// <summary>Одно улучшение из выбора «1 из 3».</summary>
    public class UpgradeDef
    {
        public string name;
        public string description;
        public Color color;
        public System.Action<PlayerStats> apply;
        public bool healsToFull;
        public float healAmount;
    }

    public static class Upgrades
    {
        static readonly Color Common = GameAssets.Hex("#4FC3F7");
        static readonly Color Rare = GameAssets.Hex("#BA68C8");
        static readonly Color Epic = GameAssets.Hex("#FFB300");

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef { name = "Острые зубы", description = "+20% к урону", color = Common, apply = s => s.damageMultiplier += 0.2f },
            new UpgradeDef { name = "Быстрые пальцы", description = "+15% к скорострельности", color = Common, apply = s => s.fireRateMultiplier += 0.15f },
            new UpgradeDef { name = "Пицца-сила", description = "+25 к максимуму здоровья\nи лечение на 25", color = Common, apply = s => s.bonusMaxHealth += 25f, healAmount = 25f },
            new UpgradeDef { name = "Кроссовки Nike", description = "+12% к скорости бега", color = Common, apply = s => s.moveSpeedMultiplier += 0.12f },
            new UpgradeDef { name = "Большой магазин", description = "+35% патронов в магазине", color = Common, apply = s => s.magazineMultiplier += 0.35f },
            new UpgradeDef { name = "Шустрые руки", description = "Перезарядка на 25% быстрее", color = Common, apply = s => s.reloadTimeMultiplier *= 0.75f },
            new UpgradeDef { name = "Вампиро-паста", description = "+5 здоровья за каждого\nпобеждённого врага", color = Rare, apply = s => s.healPerKill += 5f },
            new UpgradeDef { name = "Глаз-алмаз", description = "+12% шанс критического\nурона (×2)", color = Rare, apply = s => s.critChance += 0.12f },
            new UpgradeDef { name = "Магнит-моцарелла", description = "Монеты притягиваются\nв 2 раза дальше", color = Common, apply = s => s.coinMagnetRadius *= 2f },
            new UpgradeDef { name = "Двойной эспрессо", description = "Способность Q\nперезаряжается на 25% быстрее", color = Rare, apply = s => s.abilityCooldownMultiplier *= 0.75f },
            new UpgradeDef { name = "Броня-пармезан", description = "-12% получаемого урона", color = Rare, apply = s => s.damageTakenMultiplier *= 0.88f },
            new UpgradeDef { name = "Динамит-болоньезе", description = "+35% к урону взрывов", color = Common, apply = s => s.explosionDamageMultiplier += 0.35f },
            new UpgradeDef { name = "Свежий воздух", description = "+1.5 здоровья в секунду", color = Rare, apply = s => s.regenPerSecond += 1.5f },
            new UpgradeDef { name = "Ярость брейнрота", description = "+35% к урону,\n+10% к скорострельности", color = Epic, apply = s => { s.damageMultiplier += 0.35f; s.fireRateMultiplier += 0.1f; } },
            new UpgradeDef { name = "Мама-миа!", description = "Полное лечение\nи +15 к максимуму здоровья", color = Epic, apply = s => s.bonusMaxHealth += 15f, healsToFull = true },
        };

        public static List<UpgradeDef> PickThree()
        {
            var pool = new List<UpgradeDef>(All);
            var result = new List<UpgradeDef>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                // Эпические выпадают реже
                UpgradeDef pick = null;
                for (int tries = 0; tries < 4; tries++)
                {
                    pick = pool[Random.Range(0, pool.Count)];
                    if (pick.color != Epic || Random.value < 0.35f) break;
                }
                pool.Remove(pick);
                result.Add(pick);
            }
            return result;
        }
    }

    /// <summary>Состояние текущего забега. Между забегами ничего не сохраняется (кроме рекорда).</summary>
    public static class RunState
    {
        public static BrainrotId hero = BrainrotId.Tralalero;
        public static readonly List<BrainrotId> arenaOrder = new List<BrainrotId>();
        public static int arenaIndex;
        public static int coins;
        public static int kills;
        public static float time;
        public static PlayerStats stats = new PlayerStats();
        public static readonly List<WeaponId> ownedWeapons = new List<WeaponId>();
        public static bool started;

        public const int WavesPerArena = 5;

        public static BrainrotInfo HeroInfo => Brainrots.Get(hero);
        public static BrainrotId CurrentArenaOwner => arenaOrder[Mathf.Clamp(arenaIndex, 0, arenaOrder.Count - 1)];
        public static int ArenaCount => arenaOrder.Count;

        public static void StartNew(BrainrotId chosenHero)
        {
            hero = chosenHero;
            arenaOrder.Clear();
            foreach (var info in Brainrots.All)
                if (info.id != chosenHero) arenaOrder.Add(info.id);

            arenaIndex = 0;
            coins = 0;
            kills = 0;
            time = 0f;
            stats = new PlayerStats();
            ownedWeapons.Clear();
            ownedWeapons.Add(WeaponId.Pistol);

            var info2 = HeroInfo;
            stats.moveSpeedMultiplier = info2.heroSpeedMultiplier;
            if (chosenHero == BrainrotId.Patapim) stats.regenPerSecond = 2f;
            if (chosenHero == BrainrotId.Cappuccino) stats.critChance += 0.2f;
            if (chosenHero == BrainrotId.Bombardiro) stats.explosionDamageMultiplier += 0.2f;
            started = true;
        }

        // ---- Рекорды (единственное, что сохраняется) ----
        const string BestArenaKey = "BW_BestArena";
        const string WinsKey = "BW_Wins";

        public static int BestArena => PlayerPrefs.GetInt(BestArenaKey, 0);
        public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);

        public static bool SaveResult(bool victory)
        {
            int reached = victory ? ArenaCount : arenaIndex;
            bool record = reached > BestArena;
            if (record) PlayerPrefs.SetInt(BestArenaKey, reached);
            if (victory) PlayerPrefs.SetInt(WinsKey, Wins + 1);
            PlayerPrefs.Save();
            return record;
        }
    }
}
