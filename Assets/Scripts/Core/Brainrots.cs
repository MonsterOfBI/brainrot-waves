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
        public LS name;
        public LS shortName;
        public LS description;

        // Арена, где этот брейнрот — хозяин и босс
        public ArenaTheme arena;
        public LS arenaName;
        public Color themeColor;

        // Герой
        public LS passiveName;
        public LS passiveDescription;
        public LS abilityName;
        public LS abilityDescription;
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
        public LS bossTitle;
    }

    public static class Brainrots
    {
        public static readonly BrainrotInfo[] All =
        {
            new BrainrotInfo
            {
                id = BrainrotId.Tralalero,
                name = new LS("Тралалеро Тралала", "Tralalero Tralala"),
                shortName = new LS("Тралалеро", "Tralalero"),
                description = new LS("Трёхногая акула в синих кроссовках. Самая быстрая на пляже.", "A three-legged shark in blue sneakers. The fastest on the beach."),
                arena = ArenaTheme.Beach,
                arenaName = new LS("Пляж Тралалеро", "Tralalero Beach"),
                themeColor = GameAssets.Hex("#3FA9F5"),
                passiveName = new LS("Кроссовки", "Sneakers"),
                passiveDescription = new LS("+25% к скорости бега", "+25% move speed"),
                abilityName = new LS("Акулий рывок", "Shark dash"),
                abilityDescription = new LS("Резкий рывок вперёд, кусает всех на пути", "A quick dash forward that bites everyone in the way"),
                abilityCooldown = 5f,
                heroMaxHealth = 100f,
                heroSpeedMultiplier = 1.25f,
                behavior = EnemyBehavior.Runner,
                enemyHealth = 40f,
                enemySpeed = 7f,
                enemyDamage = 8f,
                enemySize = 0.9f,
                coins = 2,
                bossTitle = new LS("Король пляжа", "King of the beach")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Patapim,
                name = new LS("Брр Брр Патапим", "Brr Brr Patapim"),
                shortName = new LS("Патапим", "Patapim"),
                description = new LS("Лесное дерево с лицом обезьяны и огромными ногами.", "A forest tree with a monkey face and huge feet."),
                arena = ArenaTheme.Forest,
                arenaName = new LS("Лес Патапима", "Patapim Forest"),
                themeColor = GameAssets.Hex("#5DBB4C"),
                passiveName = new LS("Корни", "Roots"),
                passiveDescription = new LS("Восстанавливает 2 здоровья в секунду", "Regenerates 2 health per second"),
                abilityName = new LS("Хватка корней", "Root grip"),
                abilityDescription = new LS("Корни держат врагов вокруг и ранят их", "Roots hold nearby enemies and hurt them"),
                abilityCooldown = 12f,
                heroMaxHealth = 110f,
                heroSpeedMultiplier = 1f,
                behavior = EnemyBehavior.Chaser,
                enemyHealth = 70f,
                enemySpeed = 4f,
                enemyDamage = 12f,
                enemySize = 1.05f,
                coins = 3,
                bossTitle = new LS("Древний Патапим", "Ancient Patapim")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Chimpanzini,
                name = new LS("Шимпанзини Бананини", "Chimpanzini Bananini"),
                shortName = new LS("Шимпанзини", "Chimpanzini"),
                description = new LS("Шимпанзе внутри банана. Прыгает и швыряется бананами.", "A chimp inside a banana. Jumps around and throws bananas."),
                arena = ArenaTheme.Jungle,
                arenaName = new LS("Джунгли Шимпанзини", "Chimpanzini Jungle"),
                themeColor = GameAssets.Hex("#FFD93B"),
                passiveName = new LS("Обезьянья ловкость", "Monkey agility"),
                passiveDescription = new LS("Двойной прыжок", "Double jump"),
                abilityName = new LS("Банановая граната", "Banana grenade"),
                abilityDescription = new LS("Бросает взрывной банан", "Throws an explosive banana"),
                abilityCooldown = 6f,
                heroMaxHealth = 100f,
                heroSpeedMultiplier = 1.1f,
                behavior = EnemyBehavior.Thrower,
                enemyHealth = 45f,
                enemySpeed = 4.5f,
                enemyDamage = 10f,
                enemySize = 0.9f,
                coins = 3,
                bossTitle = new LS("Банановый король", "Banana king")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Bombardiro,
                name = new LS("Бомбардиро Крокодило", "Bombardiro Crocodilo"),
                shortName = new LS("Бомбардиро", "Bombardiro"),
                description = new LS("Крокодил-бомбардировщик. Летает и сбрасывает бомбы.", "A crocodile bomber plane. Flies and drops bombs."),
                arena = ArenaTheme.Desert,
                arenaName = new LS("Пустыня Бомбардиро", "Bombardiro Desert"),
                themeColor = GameAssets.Hex("#6B8E3D"),
                passiveName = new LS("Взрывотехник", "Demolition expert"),
                passiveDescription = new LS("+20% к урону от взрывов", "+20% explosion damage"),
                abilityName = new LS("Ковровая бомбардировка", "Carpet bombing"),
                abilityDescription = new LS("Град бомб перед героем", "A hail of bombs in front of the hero"),
                abilityCooldown = 12f,
                heroMaxHealth = 100f,
                heroSpeedMultiplier = 1f,
                behavior = EnemyBehavior.Flyer,
                enemyHealth = 55f,
                enemySpeed = 5f,
                enemyDamage = 16f,
                enemySize = 1f,
                coins = 4,
                bossTitle = new LS("Командир эскадрильи", "Squadron commander")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Cappuccino,
                name = new LS("Капучино Ассассино", "Cappuccino Assassino"),
                shortName = new LS("Капучино", "Cappuccino"),
                description = new LS("Чашка капучино-ниндзя с катанами. Бьёт молниеносно.", "A ninja cappuccino cup with katanas. Strikes like lightning."),
                arena = ArenaTheme.City,
                arenaName = new LS("Город Капучино", "Cappuccino City"),
                themeColor = GameAssets.Hex("#A9744F"),
                passiveName = new LS("Точный удар", "Precise strike"),
                passiveDescription = new LS("+20% шанс критического урона", "+20% critical hit chance"),
                abilityName = new LS("Эспрессо-время", "Espresso time"),
                abilityDescription = new LS("Враги замедляются на 5 секунд", "Enemies slow down for 5 seconds"),
                abilityCooldown = 15f,
                heroMaxHealth = 90f,
                heroSpeedMultiplier = 1.1f,
                behavior = EnemyBehavior.Dasher,
                enemyHealth = 50f,
                enemySpeed = 5f,
                enemyDamage = 14f,
                enemySize = 0.9f,
                coins = 3,
                bossTitle = new LS("Мастер эспрессо", "Espresso master")
            },
            new BrainrotInfo
            {
                id = BrainrotId.TungTung,
                name = new LS("Тунг Тунг Тунг Сахур", "Tung Tung Tung Sahur"),
                shortName = new LS("Тунг Тунг", "Tung Tung"),
                description = new LS("Деревянный человечек с битой. Приходит ночью.", "A wooden man with a bat. Comes at night."),
                arena = ArenaTheme.NightVillage,
                arenaName = new LS("Ночная деревня Сахур", "Sahur Night Village"),
                themeColor = GameAssets.Hex("#C8935A"),
                passiveName = new LS("Крепкое дерево", "Hard wood"),
                passiveDescription = new LS("+60% к максимальному здоровью", "+60% max health"),
                abilityName = new LS("Тунг-удар", "Tung smash"),
                abilityDescription = new LS("Удар битой по земле: урон и отброс вокруг", "Smashes the ground with the bat: damage and knockback around"),
                abilityCooldown = 8f,
                heroMaxHealth = 160f,
                heroSpeedMultiplier = 0.95f,
                behavior = EnemyBehavior.Leaper,
                enemyHealth = 110f,
                enemySpeed = 3.6f,
                enemyDamage = 18f,
                enemySize = 1.1f,
                coins = 4,
                bossTitle = new LS("Ночной стукач", "Night knocker")
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
