using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    public enum BrainrotId
    {
        Tralalero,
        Patapim,
        Chimpanzini,
        Bombardiro,
        Cappuccino,
        TungTung
    }

    /// <summary>Как брейнрот ведёт себя, когда он враг.</summary>
    public enum EnemyBehavior
    {
        Runner,   // быстро бежит и кусает
        Chaser,   // идёт напролом, замедляет ударом
        Thrower,  // держит дистанцию и кидается
        Flyer,    // летает и бомбит сверху
        Dasher,   // подкрадывается и делает рывок
        Leaper    // прыгает на игрока с ударом по земле
    }

    public enum ArenaTheme
    {
        Beach,
        Forest,
        Jungle,
        Desert,
        City,
        NightVillage
    }

    public class BrainrotInfo
    {
        public BrainrotId id;
        public string name;
        public string shortName;
        public string description;

        // Арена, где этот брейнрот — хозяин и босс
        public ArenaTheme arena;
        public string arenaName;
        public Color themeColor;

        // Герой
        public string passiveName;
        public string passiveDescription;
        public string abilityName;
        public string abilityDescription;
        public float abilityCooldown;
        public float heroMaxHealth;
        public float heroSpeedMultiplier;

        // Враг
        public EnemyBehavior behavior;
        public float enemyHealth;
        public float enemySpeed;
        public float enemyDamage;
        public float enemySize;
        public int coins;
        public string bossTitle;
    }

    public static class Brainrots
    {
        public static readonly BrainrotInfo[] All =
        {
            new BrainrotInfo
            {
                id = BrainrotId.Tralalero,
                name = "Тралалеро Тралала",
                shortName = "Тралалеро",
                description = "Трёхногая акула в синих кроссовках. Самая быстрая на пляже.",
                arena = ArenaTheme.Beach,
                arenaName = "Пляж Тралалеро",
                themeColor = GameAssets.Hex("#3FA9F5"),
                passiveName = "Кроссовки",
                passiveDescription = "+25% к скорости бега",
                abilityName = "Акулий рывок",
                abilityDescription = "Резкий рывок вперёд, кусает всех на пути",
                abilityCooldown = 5f,
                heroMaxHealth = 100f,
                heroSpeedMultiplier = 1.25f,
                behavior = EnemyBehavior.Runner,
                enemyHealth = 40f,
                enemySpeed = 7f,
                enemyDamage = 8f,
                enemySize = 0.9f,
                coins = 2,
                bossTitle = "Король пляжа"
            },
            new BrainrotInfo
            {
                id = BrainrotId.Patapim,
                name = "Брр Брр Патапим",
                shortName = "Патапим",
                description = "Лесное дерево с лицом обезьяны и огромными ногами.",
                arena = ArenaTheme.Forest,
                arenaName = "Лес Патапима",
                themeColor = GameAssets.Hex("#5DBB4C"),
                passiveName = "Корни",
                passiveDescription = "Восстанавливает 2 здоровья в секунду",
                abilityName = "Хватка корней",
                abilityDescription = "Корни держат врагов вокруг и ранят их",
                abilityCooldown = 12f,
                heroMaxHealth = 110f,
                heroSpeedMultiplier = 1f,
                behavior = EnemyBehavior.Chaser,
                enemyHealth = 70f,
                enemySpeed = 4f,
                enemyDamage = 12f,
                enemySize = 1.05f,
                coins = 3,
                bossTitle = "Древний Патапим"
            },
            new BrainrotInfo
            {
                id = BrainrotId.Chimpanzini,
                name = "Шимпанзини Бананини",
                shortName = "Шимпанзини",
                description = "Шимпанзе внутри банана. Прыгает и швыряется бананами.",
                arena = ArenaTheme.Jungle,
                arenaName = "Джунгли Шимпанзини",
                themeColor = GameAssets.Hex("#FFD93B"),
                passiveName = "Обезьянья ловкость",
                passiveDescription = "Двойной прыжок",
                abilityName = "Банановая граната",
                abilityDescription = "Бросает взрывной банан",
                abilityCooldown = 6f,
                heroMaxHealth = 100f,
                heroSpeedMultiplier = 1.1f,
                behavior = EnemyBehavior.Thrower,
                enemyHealth = 45f,
                enemySpeed = 4.5f,
                enemyDamage = 10f,
                enemySize = 0.9f,
                coins = 3,
                bossTitle = "Банановый король"
            },
            new BrainrotInfo
            {
                id = BrainrotId.Bombardiro,
                name = "Бомбардиро Крокодило",
                shortName = "Бомбардиро",
                description = "Крокодил-бомбардировщик. Летает и сбрасывает бомбы.",
                arena = ArenaTheme.Desert,
                arenaName = "Пустыня Бомбардиро",
                themeColor = GameAssets.Hex("#6B8E3D"),
                passiveName = "Взрывотехник",
                passiveDescription = "+20% к урону от взрывов",
                abilityName = "Ковровая бомбардировка",
                abilityDescription = "Град бомб перед героем",
                abilityCooldown = 12f,
                heroMaxHealth = 100f,
                heroSpeedMultiplier = 1f,
                behavior = EnemyBehavior.Flyer,
                enemyHealth = 55f,
                enemySpeed = 5f,
                enemyDamage = 16f,
                enemySize = 1f,
                coins = 4,
                bossTitle = "Командир эскадрильи"
            },
            new BrainrotInfo
            {
                id = BrainrotId.Cappuccino,
                name = "Капучино Ассассино",
                shortName = "Капучино",
                description = "Чашка капучино-ниндзя с катанами. Бьёт молниеносно.",
                arena = ArenaTheme.City,
                arenaName = "Город Капучино",
                themeColor = GameAssets.Hex("#A9744F"),
                passiveName = "Точный удар",
                passiveDescription = "+20% шанс критического урона",
                abilityName = "Эспрессо-время",
                abilityDescription = "Враги замедляются на 5 секунд",
                abilityCooldown = 15f,
                heroMaxHealth = 90f,
                heroSpeedMultiplier = 1.1f,
                behavior = EnemyBehavior.Dasher,
                enemyHealth = 50f,
                enemySpeed = 5f,
                enemyDamage = 14f,
                enemySize = 0.9f,
                coins = 3,
                bossTitle = "Мастер эспрессо"
            },
            new BrainrotInfo
            {
                id = BrainrotId.TungTung,
                name = "Тунг Тунг Тунг Сахур",
                shortName = "Тунг Тунг",
                description = "Деревянный человечек с битой. Приходит ночью.",
                arena = ArenaTheme.NightVillage,
                arenaName = "Ночная деревня Сахур",
                themeColor = GameAssets.Hex("#C8935A"),
                passiveName = "Крепкое дерево",
                passiveDescription = "+60% к максимальному здоровью",
                abilityName = "Тунг-удар",
                abilityDescription = "Удар битой по земле: урон и отброс вокруг",
                abilityCooldown = 8f,
                heroMaxHealth = 160f,
                heroSpeedMultiplier = 0.95f,
                behavior = EnemyBehavior.Leaper,
                enemyHealth = 110f,
                enemySpeed = 3.6f,
                enemyDamage = 18f,
                enemySize = 1.1f,
                coins = 4,
                bossTitle = "Ночной стукач"
            }
        };

        static Dictionary<BrainrotId, BrainrotInfo> map;

        public static BrainrotInfo Get(BrainrotId id)
        {
            if (map == null)
            {
                map = new Dictionary<BrainrotId, BrainrotInfo>();
                foreach (var info in All) map[info.id] = info;
            }
            return map[id];
        }
    }
}
