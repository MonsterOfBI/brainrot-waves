using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Характеристики героя, которые меняются улучшениями во время забега и в Лаборатории.</summary>
    public class PlayerStats
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float bonusMaxHealth;
        public float maxHealthMultiplier = 1f;
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
        // Из Лаборатории
        public float coinMultiplier = 1f;
        public float crystalMultiplier = 1f;
        public int startCoins;
        public int rerolls;
        public int revives;
    }

    public enum WeaponId { Pistol, Rifle, Shotgun }

    public class WeaponDef
    {
        public WeaponId id;
        public LS name;
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
            id = WeaponId.Pistol, name = new LS("Пистолет", "Pistol"), damage = 22f, fireRate = 5f, automatic = false,
            spread = 0.4f, magazine = 12, reloadTime = 1.0f, price = 0, recoil = 0.8f, shake = 0.05f
        };

        public static readonly WeaponDef Rifle = new WeaponDef
        {
            id = WeaponId.Rifle, name = new LS("Автомат", "Rifle"), damage = 15f, fireRate = 11f, automatic = true,
            spread = 1.4f, magazine = 32, reloadTime = 1.6f, price = 60, recoil = 0.5f, shake = 0.04f
        };

        public static readonly WeaponDef Shotgun = new WeaponDef
        {
            id = WeaponId.Shotgun, name = new LS("Дробовик", "Shotgun"), damage = 13f, fireRate = 1.4f, automatic = false,
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

    public enum Rarity { Common, Rare, Epic }

    /// <summary>Одно улучшение из выбора «1 из 3».</summary>
    public class UpgradeDef
    {
        public LS name;
        public LS description;
        public Rarity rarity;
        public IconKind icon;
        public System.Action<PlayerStats> apply;
        public bool healsToFull;
        public float healAmount;

        public Color color => Upgrades.RarityColor(rarity);
    }

    public static class Upgrades
    {
        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return GameAssets.Hex("#BA68C8");
                case Rarity.Epic: return GameAssets.Hex("#FFB300");
                default: return GameAssets.Hex("#4FC3F7");
            }
        }

        public static LS RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return new LS("РЕДКОЕ", "RARE");
                case Rarity.Epic: return new LS("ЭПИЧЕСКОЕ", "EPIC");
                default: return new LS("ОБЫЧНОЕ", "COMMON");
            }
        }

        static UpgradeDef U(string ru, string en, string dRu, string dEn, Rarity r, IconKind icon, System.Action<PlayerStats> apply)
            => new UpgradeDef { name = new LS(ru, en), description = new LS(dRu, dEn), rarity = r, icon = icon, apply = apply };

        public static readonly UpgradeDef[] All =
        {
            U("Острые зубы", "Sharp teeth", "+20% к урону", "+20% damage", Rarity.Common, IconKind.Sword, s => s.damageMultiplier += 0.2f),
            U("Быстрые пальцы", "Quick fingers", "+15% к скорострельности", "+15% fire rate", Rarity.Common, IconKind.Bullet, s => s.fireRateMultiplier += 0.15f),
            new UpgradeDef { name = new LS("Пицца-сила", "Pizza power"), description = new LS("+25 к максимуму здоровья\nи лечение на 25", "+25 max health\nand heal 25"), rarity = Rarity.Common, icon = IconKind.Heart, apply = s => s.bonusMaxHealth += 25f, healAmount = 25f },
            U("Кроссовки Nike", "Fast sneakers", "+12% к скорости бега", "+12% move speed", Rarity.Common, IconKind.Boot, s => s.moveSpeedMultiplier += 0.12f),
            U("Большой магазин", "Big magazine", "+35% патронов в магазине", "+35% magazine size", Rarity.Common, IconKind.Ammo, s => s.magazineMultiplier += 0.35f),
            U("Шустрые руки", "Nimble hands", "Перезарядка на 25% быстрее", "Reload 25% faster", Rarity.Common, IconKind.Reload, s => s.reloadTimeMultiplier *= 0.75f),
            U("Вампиро-паста", "Vampire pasta", "+5 здоровья за каждого\nпобеждённого врага", "+5 health for every\ndefeated enemy", Rarity.Rare, IconKind.Fang, s => s.healPerKill += 5f),
            U("Глаз-алмаз", "Eagle eye", "+12% шанс критического\nурона (×2)", "+12% critical hit\nchance (×2)", Rarity.Rare, IconKind.Crosshair, s => s.critChance += 0.12f),
            U("Магнит-моцарелла", "Mozzarella magnet", "Монеты притягиваются\nв 2 раза дальше", "Coins are pulled\nfrom twice as far", Rarity.Common, IconKind.Magnet, s => s.coinMagnetRadius *= 2f),
            U("Двойной эспрессо", "Double espresso", "Способность\nперезаряжается на 25% быстрее", "Ability recharges\n25% faster", Rarity.Rare, IconKind.Bolt, s => s.abilityCooldownMultiplier *= 0.75f),
            U("Броня-пармезан", "Parmesan armor", "−12% получаемого урона", "−12% damage taken", Rarity.Rare, IconKind.Shield, s => s.damageTakenMultiplier *= 0.88f),
            U("Динамит-болоньезе", "Bolognese dynamite", "+35% к урону взрывов", "+35% explosion damage", Rarity.Common, IconKind.Bomb, s => s.explosionDamageMultiplier += 0.35f),
            U("Свежий воздух", "Fresh air", "+1.5 здоровья в секунду", "+1.5 health per second", Rarity.Rare, IconKind.Plus, s => s.regenPerSecond += 1.5f),
            U("Ярость брейнрота", "Brainrot rage", "+35% к урону,\n+10% к скорострельности", "+35% damage,\n+10% fire rate", Rarity.Epic, IconKind.Fire, s => { s.damageMultiplier += 0.35f; s.fireRateMultiplier += 0.1f; }),
            new UpgradeDef { name = new LS("Мама-миа!", "Mamma mia!"), description = new LS("Полное лечение\nи +15 к максимуму здоровья", "Full heal\nand +15 max health"), rarity = Rarity.Epic, icon = IconKind.Star, apply = s => s.bonusMaxHealth += 15f, healsToFull = true },
            U("Жадный нос", "Greedy nose", "+20% монет", "+20% coins", Rarity.Rare, IconKind.Coin, s => s.coinMultiplier += 0.2f),
        };

        public static List<UpgradeDef> PickThree()
        {
            var pool = new List<UpgradeDef>(All);
            var result = new List<UpgradeDef>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                // Редкие и эпические выпадают реже
                UpgradeDef pick = null;
                for (int tries = 0; tries < 4; tries++)
                {
                    pick = pool[Random.Range(0, pool.Count)];
                    float chance = pick.rarity == Rarity.Epic ? 0.3f : pick.rarity == Rarity.Rare ? 0.7f : 1f;
                    if (Random.value < chance) break;
                }
                pool.Remove(pick);
                result.Add(pick);
            }
            return result;
        }
    }

    /// <summary>Состояние текущего забега. Постоянный прогресс хранится в Meta.</summary>
    public static class RunState
    {
        public static BrainrotId hero = BrainrotId.Tralalero;
        public static readonly List<BrainrotId> arenaOrder = new List<BrainrotId>();
        public static int arenaIndex;
        public static int coins;
        public static int coinsCollected;
        public static int kills;
        public static float time;
        public static int threat;
        public static int wavesCleared;
        public static int bossesKilled;
        public static int bestCombo;
        public static int rerollsLeft;
        public static int revivesLeft;
        public static PlayerStats stats = new PlayerStats();
        public static readonly List<WeaponId> ownedWeapons = new List<WeaponId>();
        public static bool started;
        static float coinRemainder;

        public const int WavesPerArena = 5;

        public static BrainrotInfo HeroInfo => Brainrots.Get(hero);
        public static BrainrotId CurrentArenaOwner => arenaOrder[Mathf.Clamp(arenaIndex, 0, arenaOrder.Count - 1)];
        public static int ArenaCount => arenaOrder.Count;

        public static void StartNew(BrainrotId chosenHero, int threatLevel = -1)
        {
            hero = chosenHero;
            threat = threatLevel >= 0 ? threatLevel : Mathf.Clamp(Meta.P.selectedThreat, 0, Meta.P.threatUnlocked);
            arenaOrder.Clear();
            foreach (var info in Brainrots.All)
                if (info.id != chosenHero) arenaOrder.Add(info.id);

            arenaIndex = 0;
            kills = 0;
            time = 0f;
            wavesCleared = 0;
            bossesKilled = 0;
            bestCombo = 0;
            coinsCollected = 0;
            coinRemainder = 0f;
            stats = new PlayerStats();
            ownedWeapons.Clear();
            ownedWeapons.Add(WeaponId.Pistol);

            var info2 = HeroInfo;
            stats.moveSpeedMultiplier = info2.heroSpeedMultiplier;
            if (chosenHero == BrainrotId.Patapim) stats.regenPerSecond = 2f;
            if (chosenHero == BrainrotId.Cappuccino) stats.critChance += 0.2f;
            if (chosenHero == BrainrotId.Bombardiro) stats.explosionDamageMultiplier += 0.2f;

            Meta.ApplyLab(stats);
            coins = stats.startCoins;
            rerollsLeft = stats.rerolls;
            revivesLeft = stats.revives;
            started = true;
        }

        /// <summary>Добавляет монеты с учётом бонуса «Жадность».</summary>
        public static void AddCoins(int amount)
        {
            float v = amount * stats.coinMultiplier + coinRemainder;
            int whole = Mathf.FloorToInt(v);
            coinRemainder = v - whole;
            coins += whole;
            coinsCollected += whole;
        }

        public static int BestArena => Meta.P.bestArena;
        public static int Wins => Meta.P.wins;
    }
}
