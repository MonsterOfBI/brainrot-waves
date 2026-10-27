using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    public enum BrainrotId
    {
        // Сюжетные (хозяева арен)
        Tralalero, Patapim, Chimpanzini, Bombardiro, Cappuccino, TungTung,
        // Новые (только выживание и защита)
        Ballerina, Lirili, Boneca, TrippiTroppi, FrigoCamelo, VacaSaturno, Bobritto, Glorbo, Burbaloni, Trulimero,
        ChefCrab, Svinino, Orangutini, CactoHippo, Bombombini, RhinoToaster, Blueberrini, Cocofanto, Girafa, TaTaSahur
    }

    /// <summary>Как брейнрот ведёт себя, когда он враг (архетипы как в Brotato / Vampire Survivors / Risk of Rain).</summary>
    public enum EnemyBehavior
    {
        Runner,     // быстро бежит и кусает (часто стаей)
        Chaser,     // идёт напролом, бьёт сильно
        Thrower,    // держит дистанцию и кидается
        Flyer,      // летает и бомбит сверху
        Dasher,     // подкрадывается и делает рывок
        Leaper,     // прыгает с ударом по земле
        Exploder,   // камикадзе: подбегает и взрывается
        Tank,       // медленный, очень живучий, иногда таранит
        Sniper,     // стреляет издалека после прицеливания (красный лазер)
        Summoner,   // держится сзади и призывает мелочь
        Teleporter  // телепортируется к цели
    }

    public enum ArenaTheme { Beach, Forest, Jungle, Desert, City, NightVillage }

    /// <summary>Тип навыка героя. Каждый тип настраивается силой, радиусом, длительностью.</summary>
    public enum AbilityType
    {
        Dash, Roots, Grenade, CarpetBomb, SlowTime, Slam, Heal, Shield, Nova, Chain,
        Turret, Blink, Freeze, Vortex, Rage, Poison, Beam, Whirlwind, Mines
    }

    /// <summary>Навык героя (Q или E).</summary>
    public class AbilityDef
    {
        public LS name;
        public LS description;
        public AbilityType type;
        public float cooldown = 8f;
        public float power = 1f;      // множитель урона/силы
        public float radius;          // 0 — по умолчанию для типа
        public float duration;        // 0 — по умолчанию для типа
        public int count;             // 0 — по умолчанию для типа
        public Projectile.Kind projectile = Projectile.Kind.Orb;

        public IconKind Icon
        {
            get
            {
                switch (type)
                {
                    case AbilityType.Dash: return IconKind.Boot;
                    case AbilityType.Roots: return IconKind.Leaf;
                    case AbilityType.Grenade: case AbilityType.CarpetBomb: case AbilityType.Mines: return IconKind.Bomb;
                    case AbilityType.SlowTime: return IconKind.Clock;
                    case AbilityType.Slam: case AbilityType.Chain: return IconKind.Bolt;
                    case AbilityType.Heal: return IconKind.Plus;
                    case AbilityType.Shield: return IconKind.Shield;
                    case AbilityType.Nova: return IconKind.Star;
                    case AbilityType.Turret: return IconKind.Tower;
                    case AbilityType.Blink: case AbilityType.Vortex: return IconKind.Spiral;
                    case AbilityType.Freeze: return IconKind.Snow;
                    case AbilityType.Rage: return IconKind.Fire;
                    case AbilityType.Poison: return IconKind.Cloud;
                    case AbilityType.Beam: return IconKind.Crosshair;
                    default: return IconKind.Reload;
                }
            }
        }
    }

    /// <summary>Что нужно сделать, чтобы открыть героя бесплатно (вместо покупки за кристаллы).</summary>
    public enum UnlockGoal { None, StoryArenas, StoryWins, BossesTotal, SurvivalWave, DefenseWave, KillsTotal }

    public class BrainrotInfo
    {
        public BrainrotId id;
        public bool story;
        public LS name;
        public LS shortName;
        public LS description;

        // Арена (у сюжетных — их собственная, у новых — где они чаще встречаются)
        public ArenaTheme arena;
        public LS arenaName;
        public Color themeColor;

        // Герой
        public LS passiveName;
        public LS passiveDescription;
        public System.Action<PlayerStats> passive;
        public int extraJumps;
        public AbilityDef q;
        public AbilityDef e;
        public float heroMaxHealth = 100f;
        public float heroSpeedMultiplier = 1f;

        // Открытие
        public int price;
        public UnlockGoal goal;
        public int goalValue;

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
        static AbilityDef A(string ru, string en, string dRu, string dEn, AbilityType type, float cd, float power = 1f, float radius = 0f, float duration = 0f, int count = 0, Projectile.Kind proj = Projectile.Kind.Orb)
            => new AbilityDef { name = new LS(ru, en), description = new LS(dRu, dEn), type = type, cooldown = cd, power = power, radius = radius, duration = duration, count = count, projectile = proj };

        static LS L(string ru, string en) => new LS(ru, en);
        static Color C(string hex) => GameAssets.Hex(hex);

        public static readonly BrainrotInfo[] All =
        {
            // ================= СЮЖЕТНЫЕ =================
            new BrainrotInfo
            {
                id = BrainrotId.Tralalero, story = true,
                name = L("Тралалеро Тралала", "Tralalero Tralala"), shortName = L("Тралалеро", "Tralalero"),
                description = L("Трёхногая акула в синих кроссовках. Самая быстрая на пляже.", "A three-legged shark in blue sneakers. The fastest on the beach."),
                arena = ArenaTheme.Beach, arenaName = L("Пляж Тралалеро", "Tralalero Beach"), themeColor = C("#3FA9F5"),
                passiveName = L("Кроссовки", "Sneakers"), passiveDescription = L("+25% к скорости бега", "+25% move speed"),
                q = A("Акулий рывок", "Shark dash", "Рывок вперёд, кусает всех на пути", "Dash forward, biting everyone in the way", AbilityType.Dash, 5f),
                e = A("Водоворот", "Whirlpool", "Затягивает врагов в воронку и бьёт их", "Pulls enemies into a whirlpool and hurts them", AbilityType.Vortex, 12f),
                heroMaxHealth = 100f, heroSpeedMultiplier = 1.25f,
                behavior = EnemyBehavior.Runner, enemyHealth = 40f, enemySpeed = 7f, enemyDamage = 8f, enemySize = 0.9f, coins = 2,
                bossTitle = L("Король пляжа", "King of the beach")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Patapim, story = true,
                name = L("Брр Брр Патапим", "Brr Brr Patapim"), shortName = L("Патапим", "Patapim"),
                description = L("Лесное дерево с лицом обезьяны и огромными ногами.", "A forest tree with a monkey face and huge feet."),
                arena = ArenaTheme.Forest, arenaName = L("Лес Патапима", "Patapim Forest"), themeColor = C("#5DBB4C"),
                passiveName = L("Корни", "Roots"), passiveDescription = L("Восстанавливает 2 здоровья в секунду", "Regenerates 2 health per second"),
                passive = s => s.regenPerSecond += 2f,
                q = A("Хватка корней", "Root grip", "Корни держат врагов вокруг и ранят их", "Roots hold nearby enemies and hurt them", AbilityType.Roots, 12f),
                e = A("Кора-щит", "Bark shield", "5 секунд получаешь на 70% меньше урона", "Take 70% less damage for 5 seconds", AbilityType.Shield, 16f),
                heroMaxHealth = 110f,
                behavior = EnemyBehavior.Chaser, enemyHealth = 70f, enemySpeed = 4f, enemyDamage = 12f, enemySize = 1.05f, coins = 3,
                bossTitle = L("Древний Патапим", "Ancient Patapim")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Chimpanzini, story = true,
                name = L("Шимпанзини Бананини", "Chimpanzini Bananini"), shortName = L("Шимпанзини", "Chimpanzini"),
                description = L("Шимпанзе внутри банана. Прыгает и швыряется бананами.", "A chimp inside a banana. Jumps around and throws bananas."),
                arena = ArenaTheme.Jungle, arenaName = L("Джунгли Шимпанзини", "Chimpanzini Jungle"), themeColor = C("#FFD93B"),
                passiveName = L("Обезьянья ловкость", "Monkey agility"), passiveDescription = L("Двойной прыжок", "Double jump"), extraJumps = 1,
                q = A("Банановая граната", "Banana grenade", "Бросает взрывной банан", "Throws an explosive banana", AbilityType.Grenade, 6f, 1f, 0f, 0f, 0, Projectile.Kind.Banana),
                e = A("Банановый шквал", "Banana storm", "Бананы разлетаются во все стороны", "Bananas fly in every direction", AbilityType.Nova, 11f, 1f, 0f, 0f, 18, Projectile.Kind.Banana),
                heroSpeedMultiplier = 1.1f,
                price = 400, goal = UnlockGoal.StoryArenas, goalValue = 2,
                behavior = EnemyBehavior.Thrower, enemyHealth = 45f, enemySpeed = 4.5f, enemyDamage = 10f, enemySize = 0.9f, coins = 3,
                bossTitle = L("Банановый король", "Banana king")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Bombardiro, story = true,
                name = L("Бомбардиро Крокодило", "Bombardiro Crocodilo"), shortName = L("Бомбардиро", "Bombardiro"),
                description = L("Крокодил-бомбардировщик. Летает и сбрасывает бомбы.", "A crocodile bomber plane. Flies and drops bombs."),
                arena = ArenaTheme.Desert, arenaName = L("Пустыня Бомбардиро", "Bombardiro Desert"), themeColor = C("#6B8E3D"),
                passiveName = L("Взрывотехник", "Demolition expert"), passiveDescription = L("+20% к урону от взрывов", "+20% explosion damage"),
                passive = s => s.explosionDamageMultiplier += 0.2f,
                q = A("Ковровая бомбардировка", "Carpet bombing", "Град бомб перед героем", "A hail of bombs in front of the hero", AbilityType.CarpetBomb, 12f),
                e = A("Минное поле", "Minefield", "Раскидывает мины вокруг себя", "Scatters mines around you", AbilityType.Mines, 14f),
                price = 1000, goal = UnlockGoal.SurvivalWave, goalValue = 15,
                behavior = EnemyBehavior.Flyer, enemyHealth = 55f, enemySpeed = 5f, enemyDamage = 16f, enemySize = 1f, coins = 4,
                bossTitle = L("Командир эскадрильи", "Squadron commander")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Cappuccino, story = true,
                name = L("Капучино Ассассино", "Cappuccino Assassino"), shortName = L("Капучино", "Cappuccino"),
                description = L("Чашка капучино-ниндзя с катанами. Бьёт молниеносно.", "A ninja cappuccino cup with katanas. Strikes like lightning."),
                arena = ArenaTheme.City, arenaName = L("Город Капучино", "Cappuccino City"), themeColor = C("#A9744F"),
                passiveName = L("Точный удар", "Precise strike"), passiveDescription = L("+20% шанс критического урона", "+20% critical hit chance"),
                passive = s => s.critChance += 0.2f,
                q = A("Эспрессо-время", "Espresso time", "Враги замедляются на 5 секунд", "Enemies slow down for 5 seconds", AbilityType.SlowTime, 15f),
                e = A("Двойная доза", "Double shot", "6 секунд стреляешь на 60% быстрее", "Shoot 60% faster for 6 seconds", AbilityType.Rage, 14f),
                heroMaxHealth = 90f, heroSpeedMultiplier = 1.1f,
                price = 700, goal = UnlockGoal.BossesTotal, goalValue = 4,
                behavior = EnemyBehavior.Dasher, enemyHealth = 50f, enemySpeed = 5f, enemyDamage = 14f, enemySize = 0.9f, coins = 3,
                bossTitle = L("Мастер эспрессо", "Espresso master")
            },
            new BrainrotInfo
            {
                id = BrainrotId.TungTung, story = true,
                name = L("Тунг Тунг Тунг Сахур", "Tung Tung Tung Sahur"), shortName = L("Тунг Тунг", "Tung Tung"),
                description = L("Деревянный человечек с битой. Приходит ночью.", "A wooden man with a bat. Comes at night."),
                arena = ArenaTheme.NightVillage, arenaName = L("Ночная деревня Сахур", "Sahur Night Village"), themeColor = C("#C8935A"),
                passiveName = L("Крепкое дерево", "Hard wood"), passiveDescription = L("+60% к максимальному здоровью", "+60% max health"),
                q = A("Тунг-удар", "Tung smash", "Удар битой по земле: урон и отброс вокруг", "Smash the ground: damage and knockback around", AbilityType.Slam, 8f),
                e = A("Бита-вихрь", "Bat whirl", "Крутится с битой 3 секунды, задевая всех рядом", "Spins with the bat for 3 seconds, hitting everyone nearby", AbilityType.Whirlwind, 12f),
                heroMaxHealth = 160f, heroSpeedMultiplier = 0.95f,
                price = 1500, goal = UnlockGoal.StoryWins, goalValue = 1,
                behavior = EnemyBehavior.Leaper, enemyHealth = 110f, enemySpeed = 3.6f, enemyDamage = 18f, enemySize = 1.1f, coins = 4,
                bossTitle = L("Ночной стукач", "Night knocker")
            },

            // ================= НОВЫЕ (выживание и защита) =================
            new BrainrotInfo
            {
                id = BrainrotId.Ballerina,
                name = L("Балерина Капучина", "Ballerina Cappuccina"), shortName = L("Балерина", "Ballerina"),
                description = L("Балерина с чашкой капучино вместо головы. Кружится и не даёт себя поймать.", "A ballerina with a cappuccino cup for a head. Spins and never gets caught."),
                arena = ArenaTheme.City, themeColor = C("#F48FB1"),
                passiveName = L("Грация", "Grace"), passiveDescription = L("+20% к скорости, +5% крит", "+20% speed, +5% crit"),
                passive = s => s.critChance += 0.05f,
                q = A("Пируэт", "Pirouette", "Кружится 3 секунды, раня всех вокруг", "Spins for 3 seconds, hurting everyone around", AbilityType.Whirlwind, 9f, 0.9f),
                e = A("Гран-жете", "Grand jeté", "Прыжок вперёд с ударной волной при приземлении", "Leaps forward with a shockwave on landing", AbilityType.Blink, 7f),
                heroMaxHealth = 90f, heroSpeedMultiplier = 1.2f,
                price = 0,
                behavior = EnemyBehavior.Dasher, enemyHealth = 45f, enemySpeed = 5.5f, enemyDamage = 12f, enemySize = 0.9f, coins = 3,
                bossTitle = L("Прима-балерина", "Prima ballerina")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Lirili,
                name = L("Лирили Ларила", "Lirilì Larilà"), shortName = L("Лирили", "Lirilì"),
                description = L("Слон-кактус в сандалиях с часами. Умеет останавливать время.", "A cactus elephant in sandals with a clock. Can stop time."),
                arena = ArenaTheme.Desert, themeColor = C("#7CB342"),
                passiveName = L("Колючая шкура", "Prickly hide"), passiveDescription = L("−15% получаемого урона", "−15% damage taken"),
                passive = s => s.damageTakenMultiplier *= 0.85f,
                q = A("Остановка времени", "Time stop", "Почти останавливает врагов на 3 секунды", "Almost freezes enemies for 3 seconds", AbilityType.SlowTime, 18f, 1f, 0f, 3f),
                e = A("Кактусовые иглы", "Cactus needles", "Выпускает иглы во все стороны", "Shoots needles in every direction", AbilityType.Nova, 8f, 0.9f, 0f, 0f, 20),
                heroMaxHealth = 130f, heroSpeedMultiplier = 0.9f,
                price = 600, goal = UnlockGoal.SurvivalWave, goalValue = 10,
                behavior = EnemyBehavior.Tank, enemyHealth = 220f, enemySpeed = 2.6f, enemyDamage = 20f, enemySize = 1.3f, coins = 6,
                bossTitle = L("Хранитель времени", "Keeper of time")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Boneca,
                name = L("Бонека Амбалабу", "Boneca Ambalabu"), shortName = L("Бонека", "Boneca"),
                description = L("Лягушка в покрышке на человеческих ногах. Прыгает очень высоко.", "A frog in a tyre on human legs. Jumps very high."),
                arena = ArenaTheme.Jungle, themeColor = C("#43A047"),
                passiveName = L("Лягушачьи лапы", "Frog legs"), passiveDescription = L("Двойной прыжок, +10% к скорости", "Double jump, +10% speed"), extraJumps = 1,
                q = A("Шинный удар", "Tyre slam", "Удар об землю: урон и отброс", "Ground slam: damage and knockback", AbilityType.Slam, 7f, 0.9f),
                e = A("Липкий язык", "Sticky tongue", "Притягивает врагов к точке и ранит", "Pulls enemies to a point and hurts them", AbilityType.Vortex, 10f, 0.8f, 6f),
                heroSpeedMultiplier = 1.1f,
                price = 0,
                behavior = EnemyBehavior.Leaper, enemyHealth = 80f, enemySpeed = 4f, enemyDamage = 14f, enemySize = 1f, coins = 4,
                bossTitle = L("Король болота", "Swamp king")
            },
            new BrainrotInfo
            {
                id = BrainrotId.TrippiTroppi,
                name = L("Триппи Троппи", "Trippi Troppi"), shortName = L("Триппи", "Trippi"),
                description = L("Креветка с кошачьей головой. Нападают стаями.", "A shrimp with a cat's head. Attacks in swarms."),
                arena = ArenaTheme.Beach, themeColor = C("#FF8A65"),
                passiveName = L("Кошачья реакция", "Cat reflexes"), passiveDescription = L("+15% крит, +10% скорострельность", "+15% crit, +10% fire rate"),
                passive = s => { s.critChance += 0.15f; s.fireRateMultiplier += 0.1f; },
                q = A("Креветочный рывок", "Shrimp dash", "Рывок вперёд, раня всех на пути", "Dash forward, hurting everyone in the way", AbilityType.Dash, 4f, 0.8f),
                e = A("Мяу-волна", "Meow wave", "Оглушающий визг замораживает врагов рядом", "A stunning screech freezes nearby enemies", AbilityType.Freeze, 12f),
                heroMaxHealth = 85f, heroSpeedMultiplier = 1.15f,
                price = 300,
                behavior = EnemyBehavior.Runner, enemyHealth = 22f, enemySpeed = 8f, enemyDamage = 6f, enemySize = 0.7f, coins = 1,
                bossTitle = L("Царь-креветка", "Shrimp tsar")
            },
            new BrainrotInfo
            {
                id = BrainrotId.FrigoCamelo,
                name = L("Фриго Камело", "Frigo Camelo"), shortName = L("Фриго", "Frigo"),
                description = L("Верблюд-холодильник в ботинках. Дышит морозом.", "A fridge camel in boots. Breathes frost."),
                arena = ArenaTheme.Desert, themeColor = C("#80DEEA"),
                passiveName = L("Морозилка", "Freezer"), passiveDescription = L("−20% урона, +20% здоровья", "−20% damage taken, +20% health"),
                passive = s => { s.damageTakenMultiplier *= 0.8f; s.maxHealthMultiplier += 0.2f; },
                q = A("Ледяной выдох", "Frost breath", "Замораживает врагов вокруг на 3 секунды", "Freezes enemies around for 3 seconds", AbilityType.Freeze, 10f, 1f, 10f),
                e = A("Холодильный щит", "Fridge shield", "5 секунд почти не получаешь урона", "Take almost no damage for 5 seconds", AbilityType.Shield, 18f, 1.2f),
                heroMaxHealth = 120f, heroSpeedMultiplier = 0.9f,
                price = 800, goal = UnlockGoal.DefenseWave, goalValue = 10,
                behavior = EnemyBehavior.Tank, enemyHealth = 200f, enemySpeed = 2.8f, enemyDamage = 18f, enemySize = 1.25f, coins = 6,
                bossTitle = L("Ледяной караван", "Frozen caravan")
            },
            new BrainrotInfo
            {
                id = BrainrotId.VacaSaturno,
                name = L("Ла Вака Сатурно Сатурнита", "La Vaca Saturno Saturnita"), shortName = L("Вака Сатурно", "Vaca Saturno"),
                description = L("Космическая корова с кольцом Сатурна. Управляет гравитацией.", "A space cow with Saturn's ring. Controls gravity."),
                arena = ArenaTheme.NightVillage, themeColor = C("#B39DDB"),
                passiveName = L("Космическое молоко", "Cosmic milk"), passiveDescription = L("+1.5 здоровья в секунду", "+1.5 health per second"),
                passive = s => s.regenPerSecond += 1.5f,
                q = A("Гравитация Сатурна", "Saturn's gravity", "Огромная воронка тянет врагов и бьёт их", "A huge vortex pulls enemies in and hurts them", AbilityType.Vortex, 12f, 1.2f, 10f),
                e = A("Кольца", "Rings", "Кольцо снарядов вокруг коровы", "A ring of projectiles around the cow", AbilityType.Nova, 9f, 1f, 0f, 0f, 24),
                heroMaxHealth = 120f, heroSpeedMultiplier = 0.95f,
                price = 900, goal = UnlockGoal.KillsTotal, goalValue = 1500,
                behavior = EnemyBehavior.Summoner, enemyHealth = 90f, enemySpeed = 3.4f, enemyDamage = 10f, enemySize = 1.15f, coins = 5,
                bossTitle = L("Королева орбиты", "Queen of the orbit")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Bobritto,
                name = L("Бобритто Бандито", "Bobritto Bandito"), shortName = L("Бобритто", "Bobritto"),
                description = L("Бобр-гангстер в шляпе с томми-ганом. Стреляет издалека.", "A gangster beaver in a fedora with a tommy gun. Shoots from afar."),
                arena = ArenaTheme.City, themeColor = C("#8D6E63"),
                passiveName = L("Мафия", "Mafia"), passiveDescription = L("+15% к урону, +15% монет", "+15% damage, +15% coins"),
                passive = s => { s.damageMultiplier += 0.15f; s.coinMultiplier += 0.15f; },
                q = A("Томми-ган", "Tommy gun", "6 секунд стреляешь на 70% быстрее", "Shoot 70% faster for 6 seconds", AbilityType.Rage, 12f, 1.15f),
                e = A("Динамит", "Dynamite", "Бросает связку динамита", "Throws a bundle of dynamite", AbilityType.Grenade, 7f, 1.3f, 6f, 0f, 0, Projectile.Kind.Bomb),
                price = 700,
                behavior = EnemyBehavior.Sniper, enemyHealth = 45f, enemySpeed = 4f, enemyDamage = 16f, enemySize = 0.9f, coins = 4,
                bossTitle = L("Крёстный бобр", "The Godbeaver")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Glorbo,
                name = L("Глорбо Фруттодрилло", "Glorbo Fruttodrillo"), shortName = L("Глорбо", "Glorbo"),
                description = L("Крокодил-арбуз. Плюётся семечками и больно кусает.", "A watermelon crocodile. Spits seeds and bites hard."),
                arena = ArenaTheme.Jungle, themeColor = C("#E53935"),
                passiveName = L("Сочный", "Juicy"), passiveDescription = L("+30% к здоровью, +4 здоровья за врага", "+30% health, +4 health per kill"),
                passive = s => { s.maxHealthMultiplier += 0.3f; s.healPerKill += 4f; },
                q = A("Арбузные семечки", "Watermelon seeds", "Град семечек во все стороны", "A burst of seeds in every direction", AbilityType.Nova, 7f, 1.1f, 0f, 0f, 16),
                e = A("Укус крокодила", "Croc bite", "Короткий рывок с огромным уроном", "A short dash with huge damage", AbilityType.Dash, 6f, 2f, 0f, 0.2f),
                heroMaxHealth = 120f,
                price = 500,
                behavior = EnemyBehavior.Chaser, enemyHealth = 95f, enemySpeed = 4.2f, enemyDamage = 15f, enemySize = 1.05f, coins = 4,
                bossTitle = L("Арбузный зверь", "Melon beast")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Burbaloni,
                name = L("Бурбалони Лулилоли", "Burbaloni Luliloli"), shortName = L("Бурбалони", "Burbaloni"),
                description = L("Капибара внутри кокоса. Спокойная, но непробиваемая.", "A capybara inside a coconut. Calm but unbreakable."),
                arena = ArenaTheme.Beach, themeColor = C("#A1887F"),
                passiveName = L("Спокойствие", "Chill"), passiveDescription = L("+2 здоровья в секунду, −10% урона", "+2 health per second, −10% damage taken"),
                passive = s => { s.regenPerSecond += 2f; s.damageTakenMultiplier *= 0.9f; },
                q = A("Кокосовый панцирь", "Coconut shell", "5 секунд получаешь на 70% меньше урона", "Take 70% less damage for 5 seconds", AbilityType.Shield, 14f),
                e = A("Покатушки", "Roll out", "Катится вперёд, сбивая врагов", "Rolls forward, knocking enemies down", AbilityType.Dash, 6f, 1.2f, 0f, 0.45f),
                heroMaxHealth = 140f, heroSpeedMultiplier = 0.95f,
                price = 600,
                behavior = EnemyBehavior.Tank, enemyHealth = 180f, enemySpeed = 3f, enemyDamage = 14f, enemySize = 1.15f, coins = 5,
                bossTitle = L("Кокосовая крепость", "Coconut fortress")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Trulimero,
                name = L("Трулимеро Трулишина", "Trulimero Trulicina"), shortName = L("Трулимеро", "Trulimero"),
                description = L("Рыба с кошачьей головой и человеческими ногами.", "A fish with a cat's head and human legs."),
                arena = ArenaTheme.Beach, themeColor = C("#4DD0E1"),
                passiveName = L("Скользкий", "Slippery"), passiveDescription = L("+15% к скорости, +20% к перезарядке", "+15% speed, +20% reload speed"),
                passive = s => s.reloadTimeMultiplier *= 0.8f,
                q = A("Волна", "Wave", "Волна сбивает и ранит врагов вокруг", "A wave knocks down and hurts enemies around", AbilityType.Slam, 7f, 0.9f, 9f),
                e = A("Ледяные брызги", "Icy splash", "Брызги замораживают врагов рядом", "Splashes freeze nearby enemies", AbilityType.Freeze, 11f, 0.9f, 8f),
                heroSpeedMultiplier = 1.15f,
                price = 400,
                behavior = EnemyBehavior.Runner, enemyHealth = 38f, enemySpeed = 7f, enemyDamage = 9f, enemySize = 0.9f, coins = 2,
                bossTitle = L("Рыбокот-гигант", "Giant fishcat")
            },
            new BrainrotInfo
            {
                id = BrainrotId.ChefCrab,
                name = L("Шеф Крабракадабра", "Chef Crabracadabra"), shortName = L("Шеф Краб", "Chef Crab"),
                description = L("Краб-повар и волшебник. Готовит молнии и зелья.", "A crab chef and wizard. Cooks lightning and potions."),
                arena = ArenaTheme.City, themeColor = C("#FF7043"),
                passiveName = L("Повар", "Cook"), passiveDescription = L("+6 здоровья за каждого врага", "+6 health per kill"),
                passive = s => s.healPerKill += 6f,
                q = A("Цепная молния", "Chain lightning", "Молния перескакивает между врагами", "Lightning jumps between enemies", AbilityType.Chain, 6f),
                e = A("Суп-зелье", "Potion soup", "Лечит 40% здоровья", "Heals 40% health", AbilityType.Heal, 16f),
                price = 800, goal = UnlockGoal.DefenseWave, goalValue = 15,
                behavior = EnemyBehavior.Thrower, enemyHealth = 50f, enemySpeed = 4.2f, enemyDamage = 11f, enemySize = 1f, coins = 4,
                bossTitle = L("Великий шеф", "Grand chef")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Svinino,
                name = L("Свинино Бомбондино", "Svinino Bombondino"), shortName = L("Свинино", "Svinino"),
                description = L("Свинья-бомба с фитилём. Подбегает и взрывается.", "A pig bomb with a fuse. Runs up and explodes."),
                arena = ArenaTheme.Desert, themeColor = C("#F06292"),
                passiveName = L("Порох", "Gunpowder"), passiveDescription = L("+40% к урону от взрывов", "+40% explosion damage"),
                passive = s => s.explosionDamageMultiplier += 0.4f,
                q = A("Свиная бомба", "Pig bomb", "Огромная бомба с большим взрывом", "A huge bomb with a big blast", AbilityType.Grenade, 8f, 1.6f, 7f, 0f, 0, Projectile.Kind.Bomb),
                e = A("Мины", "Mines", "Раскидывает мины вокруг", "Scatters mines around", AbilityType.Mines, 12f, 1.1f),
                price = 500,
                behavior = EnemyBehavior.Exploder, enemyHealth = 35f, enemySpeed = 6.5f, enemyDamage = 24f, enemySize = 0.9f, coins = 2,
                bossTitle = L("Ходячий арсенал", "Walking arsenal")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Orangutini,
                name = L("Орангутини Ананасини", "Orangutini Ananasini"), shortName = L("Орангутини", "Orangutini"),
                description = L("Орангутан в ананасе. Швыряется ананасами с неба.", "An orangutan in a pineapple. Rains pineapples from the sky."),
                arena = ArenaTheme.Jungle, themeColor = C("#FFB300"),
                passiveName = L("Сила джунглей", "Jungle might"), passiveDescription = L("+20% к урону", "+20% damage"),
                passive = s => s.damageMultiplier += 0.2f,
                q = A("Ананасовый град", "Pineapple rain", "Град ананасов перед героем", "A rain of pineapples in front of you", AbilityType.CarpetBomb, 10f, 1f, 0f, 0f, 14),
                e = A("Рёв", "Roar", "6 секунд: +60% скорострельности и +30% урона", "6 seconds: +60% fire rate and +30% damage", AbilityType.Rage, 15f),
                heroMaxHealth = 110f,
                price = 600,
                behavior = EnemyBehavior.Thrower, enemyHealth = 55f, enemySpeed = 4.2f, enemyDamage = 12f, enemySize = 1.05f, coins = 4,
                bossTitle = L("Ананасовый тиран", "Pineapple tyrant")
            },
            new BrainrotInfo
            {
                id = BrainrotId.CactoHippo,
                name = L("Иль Както Иппопотамо", "Il Cacto Hipopotamo"), shortName = L("Както Иппо", "Cacto Hippo"),
                description = L("Бегемот-кактус. Медленный, огромный и колючий.", "A cactus hippo. Slow, huge and prickly."),
                arena = ArenaTheme.Desert, themeColor = C("#9CCC65"),
                passiveName = L("Толстокожий", "Thick-skinned"), passiveDescription = L("+40% здоровья, −10% урона", "+40% health, −10% damage taken"),
                passive = s => { s.maxHealthMultiplier += 0.4f; s.damageTakenMultiplier *= 0.9f; },
                q = A("Иглобой", "Needle storm", "Шквал игл во все стороны", "A storm of needles in every direction", AbilityType.Nova, 7f, 1.2f, 0f, 0f, 22),
                e = A("Топот", "Stomp", "Мощный удар об землю", "A mighty ground stomp", AbilityType.Slam, 9f, 1.3f, 9f),
                heroMaxHealth = 150f, heroSpeedMultiplier = 0.85f,
                price = 700,
                behavior = EnemyBehavior.Tank, enemyHealth = 260f, enemySpeed = 2.5f, enemyDamage = 22f, enemySize = 1.4f, coins = 7,
                bossTitle = L("Колючая гора", "Prickly mountain")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Bombombini,
                name = L("Бомбомбини Гусини", "Bombombini Gusini"), shortName = L("Гусини", "Gusini"),
                description = L("Гусь-истребитель. Пикирует и бомбит с неба.", "A fighter-jet goose. Dives and bombs from the sky."),
                arena = ArenaTheme.Forest, themeColor = C("#90A4AE"),
                passiveName = L("Пилот", "Pilot"), passiveDescription = L("+25% к урону от взрывов, +10% скорость", "+25% explosion damage, +10% speed"),
                passive = s => s.explosionDamageMultiplier += 0.25f,
                q = A("Гусиный налёт", "Goose raid", "Град бомб перед героем", "A hail of bombs in front of you", AbilityType.CarpetBomb, 11f, 1.1f),
                e = A("Форсаж", "Afterburner", "Мгновенный рывок вперёд с ударом", "Instant boost forward with an impact", AbilityType.Blink, 6f, 1f, 0f, 0f),
                heroSpeedMultiplier = 1.1f,
                price = 900, goal = UnlockGoal.BossesTotal, goalValue = 10,
                behavior = EnemyBehavior.Flyer, enemyHealth = 50f, enemySpeed = 5.5f, enemyDamage = 14f, enemySize = 0.95f, coins = 4,
                bossTitle = L("Гусь-ас", "Ace goose")
            },
            new BrainrotInfo
            {
                id = BrainrotId.RhinoToaster,
                name = L("Рино Тостерино", "Rhino Toasterino"), shortName = L("Рино", "Rhino"),
                description = L("Носорог-тостер. Таранит всё на пути и стреляет горячими тостами.", "A toaster rhino. Rams everything and fires hot toast."),
                arena = ArenaTheme.City, themeColor = C("#BDBDBD"),
                passiveName = L("Стальной корпус", "Steel body"), passiveDescription = L("−20% получаемого урона", "−20% damage taken"),
                passive = s => s.damageTakenMultiplier *= 0.8f,
                q = A("Таран", "Ram", "Длинный рывок, сносящий врагов", "A long charge that smashes enemies", AbilityType.Dash, 6f, 1.3f, 0f, 0.5f),
                e = A("Горячий тост", "Hot toast", "Раскалённый луч пробивает всех врагов на линии", "A red-hot beam pierces every enemy in a line", AbilityType.Beam, 8f),
                heroMaxHealth = 130f, heroSpeedMultiplier = 0.95f,
                price = 800,
                behavior = EnemyBehavior.Dasher, enemyHealth = 110f, enemySpeed = 4f, enemyDamage = 18f, enemySize = 1.2f, coins = 5,
                bossTitle = L("Бронетостер", "Armored toaster")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Blueberrini,
                name = L("Блюберрини Октопуссини", "Blueberrini Octopussini"), shortName = L("Блюберрини", "Blueberrini"),
                description = L("Осьминог-черника. Стреляет чернилами издалека.", "A blueberry octopus. Shoots ink from afar."),
                arena = ArenaTheme.Beach, themeColor = C("#5C6BC0"),
                passiveName = L("Восемь рук", "Eight arms"), passiveDescription = L("+15% скорострельность, +30% патронов", "+15% fire rate, +30% magazine"),
                passive = s => { s.fireRateMultiplier += 0.15f; s.magazineMultiplier += 0.3f; },
                q = A("Чернильная туча", "Ink cloud", "Ядовитое облако ранит врагов внутри", "A toxic cloud hurts enemies inside", AbilityType.Poison, 9f),
                e = A("Щупальца", "Tentacles", "Щупальца держат врагов вокруг", "Tentacles hold enemies around you", AbilityType.Roots, 12f, 1.2f),
                price = 700,
                behavior = EnemyBehavior.Sniper, enemyHealth = 50f, enemySpeed = 4f, enemyDamage = 14f, enemySize = 1f, coins = 4,
                bossTitle = L("Чернильный кракен", "Ink kraken")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Cocofanto,
                name = L("Кокофанто Элефанто", "Cocofanto Elefanto"), shortName = L("Кокофанто", "Cocofanto"),
                description = L("Слон с головой-кокосом. Сбрасывает кокосы на головы врагов.", "An elephant with a coconut head. Drops coconuts on enemies."),
                arena = ArenaTheme.Jungle, themeColor = C("#795548"),
                passiveName = L("Слоновья мощь", "Elephant power"), passiveDescription = L("+35% здоровья, +10% урона", "+35% health, +10% damage"),
                passive = s => { s.maxHealthMultiplier += 0.35f; s.damageMultiplier += 0.1f; },
                q = A("Кокосовая лавина", "Coconut avalanche", "Град кокосов перед героем", "A hail of coconuts in front of you", AbilityType.CarpetBomb, 11f, 1.1f),
                e = A("Хобот-вихрь", "Trunk twister", "Воронка затягивает врагов", "A vortex pulls enemies in", AbilityType.Vortex, 12f),
                heroMaxHealth = 140f, heroSpeedMultiplier = 0.9f,
                price = 900,
                behavior = EnemyBehavior.Chaser, enemyHealth = 130f, enemySpeed = 3.5f, enemyDamage = 18f, enemySize = 1.25f, coins = 5,
                bossTitle = L("Кокосовый мамонт", "Coconut mammoth")
            },
            new BrainrotInfo
            {
                id = BrainrotId.Girafa,
                name = L("Жирафа Селестре", "Girafa Celestre"), shortName = L("Жирафа", "Girafa"),
                description = L("Жираф-космонавт. Телепортируется и стреляет звёздами.", "An astronaut giraffe. Teleports and shoots stars."),
                arena = ArenaTheme.NightVillage, themeColor = C("#FFCA28"),
                passiveName = L("Невесомость", "Zero gravity"), passiveDescription = L("+15% скорость, +10% крит", "+15% speed, +10% crit"),
                passive = s => s.critChance += 0.1f,
                q = A("Телепорт", "Teleport", "Мгновенный прыжок вперёд со вспышкой", "An instant jump forward with a flash", AbilityType.Blink, 5f, 1.2f),
                e = A("Звёздный лазер", "Star laser", "Луч пробивает всех врагов на линии", "A beam pierces every enemy in a line", AbilityType.Beam, 9f, 1.2f),
                heroSpeedMultiplier = 1.15f,
                price = 1200, goal = UnlockGoal.SurvivalWave, goalValue = 25,
                behavior = EnemyBehavior.Teleporter, enemyHealth = 60f, enemySpeed = 4.5f, enemyDamage = 14f, enemySize = 1.15f, coins = 4,
                bossTitle = L("Звёздный странник", "Star wanderer")
            },
            new BrainrotInfo
            {
                id = BrainrotId.TaTaSahur,
                name = L("Та Та Та Сахур", "Ta Ta Ta Sahur"), shortName = L("Та Та Та", "Ta Ta Ta"),
                description = L("Чайник-родственник Тунг Тунга. Свистит и обжигает паром.", "A kettle, Tung Tung's relative. Whistles and scalds with steam."),
                arena = ArenaTheme.NightVillage, themeColor = C("#78909C"),
                passiveName = L("Кипяток", "Boiling"), passiveDescription = L("+15% урона, −10% получаемого урона", "+15% damage, −10% damage taken"),
                passive = s => { s.damageMultiplier += 0.15f; s.damageTakenMultiplier *= 0.9f; },
                q = A("Паровая туча", "Steam cloud", "Облако кипятка ранит врагов", "A cloud of boiling steam hurts enemies", AbilityType.Poison, 8f, 1.1f),
                e = A("Свисток-турель", "Whistle turret", "Ставит турель, которая сама стреляет", "Places a turret that shoots on its own", AbilityType.Turret, 16f),
                heroMaxHealth = 110f,
                price = 1000, goal = UnlockGoal.KillsTotal, goalValue = 3000,
                behavior = EnemyBehavior.Summoner, enemyHealth = 85f, enemySpeed = 3.4f, enemyDamage = 11f, enemySize = 1.05f, coins = 5,
                bossTitle = L("Кипящий ужас", "Boiling horror")
            },
        };

        static Dictionary<BrainrotId, BrainrotInfo> map;
        static BrainrotInfo[] story;

        /// <summary>Шесть героев сюжетного режима.</summary>
        public static BrainrotInfo[] Story
        {
            get
            {
                if (story == null)
                {
                    var list = new List<BrainrotInfo>();
                    foreach (var i in All) if (i.story) list.Add(i);
                    story = list.ToArray();
                }
                return story;
            }
        }

        public static BrainrotInfo Get(BrainrotId id)
        {
            if (map == null)
            {
                map = new Dictionary<BrainrotId, BrainrotInfo>();
                foreach (var info in All) map[info.id] = info;
            }
            return map[id];
        }

        /// <summary>Герои, доступные в режиме.</summary>
        public static BrainrotInfo[] ForMode(GameMode mode) => mode == GameMode.Story ? Story : All;
    }
}
