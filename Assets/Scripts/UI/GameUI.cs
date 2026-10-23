using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>
    /// Весь интерфейс боя: полоски, прицел, сообщения, выбор улучшений, магазин, пауза, финал.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        RectTransform root;
        RectTransform hud;
        RectTransform overlay;

        // HUD
        Image healthFill, healthBack, abilityFill, abilityIcon, reloadFill, bossFill, damageVignette, slowTint, lowHealthPulse;
        Text healthText, ammoText, weaponText, abilityText, abilityKey, coinsText, waveText, enemiesText, bannerTitle, bannerSubtitle, bossName, hintText;
        RectTransform crosshair, hitMarker, bossBar, banner, weaponSlots;
        readonly List<Image> crosshairLines = new List<Image>();
        readonly List<Text> slotTexts = new List<Text>();
        float hitTimer, bannerTimer, bannerDuration, damageFlash;
        bool hitCrit;
        float shownHealth = 1f;
        int shownCoins;

        public bool ScreenOpen => overlay != null && overlay.childCount > 0;

        void Awake()
        {
            Instance = this;
            UIKit.EnsureEventSystem();
            root = UIKit.Canvas("GameCanvas", 10);
            root.SetParent(transform, false);
            BuildHUD();
            overlay = UIKit.Rect(root, "Overlay");
            UIKit.Stretch(overlay);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Bind(PlayerController player, WeaponSystem weapons)
        {
            player.Health.Damaged += (amount, crit) => damageFlash = Mathf.Clamp01(damageFlash + 0.5f);
            weapons.HitCrit += crit => { hitTimer = 0.15f; hitCrit = crit; };
            shownCoins = RunState.coins;
        }

        // ---------------- HUD ----------------

        void BuildHUD()
        {
            hud = UIKit.Rect(root, "HUD");
            UIKit.Stretch(hud);

            damageVignette = UIKit.Image(hud, "DamageVignette", new Color(1f, 0f, 0f, 0f), false);
            UIKit.Stretch(damageVignette.rectTransform);
            lowHealthPulse = UIKit.Image(hud, "LowHealth", new Color(0.8f, 0f, 0.1f, 0f), false);
            UIKit.Stretch(lowHealthPulse.rectTransform);
            slowTint = UIKit.Image(hud, "SlowTint", new Color(0.55f, 0.35f, 0.2f, 0f), false);
            UIKit.Stretch(slowTint.rectTransform);

            // Прицел
            crosshair = UIKit.Rect(hud, "Crosshair");
            UIKit.Place(crosshair, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f), new Vector2(0.5f, 0.5f));
            Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            foreach (var d in dirs)
            {
                var line = UIKit.Image(crosshair, "Line", Color.white, false);
                var size = d.x != 0f ? new Vector2(12f, 3f) : new Vector2(3f, 12f);
                UIKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), d * 11f, size, new Vector2(0.5f, 0.5f));
                line.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
                crosshairLines.Add(line);
            }
            var dot = UIKit.Image(crosshair, "Dot", Color.white, true);
            dot.sprite = UIKit.Circle;
            UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f), new Vector2(0.5f, 0.5f));

            hitMarker = UIKit.Rect(hud, "HitMarker");
            UIKit.Place(hitMarker, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f), new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 4; i++)
            {
                var line = UIKit.Image(hitMarker, "X", Color.white, false);
                float a = 45f + i * 90f;
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                UIKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), dir * 14f, new Vector2(12f, 3f), new Vector2(0.5f, 0.5f));
                line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, a);
            }
            hitMarker.gameObject.SetActive(false);

            // Здоровье (слева снизу)
            var hpPanel = UIKit.Image(hud, "HealthPanel", UIKit.Panel);
            UIKit.Place(hpPanel.rectTransform, Vector2.zero, new Vector2(30f, 30f), new Vector2(460f, 86f));
            var heroName = UIKit.Text(hpPanel.rectTransform, "Hero", RunState.HeroInfo.name, 22, TextAnchor.UpperLeft, RunState.HeroInfo.themeColor, true);
            UIKit.Place(heroName.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(420f, 28f));
            healthBack = UIKit.Image(hpPanel.rectTransform, "HealthBack", new Color(0f, 0f, 0f, 0.5f));
            UIKit.Place(healthBack.rectTransform, new Vector2(0f, 0f), new Vector2(16f, 14f), new Vector2(428f, 34f));
            healthFill = UIKit.Image(healthBack.rectTransform, "Fill", UIKit.Green);
            UIKit.Stretch(healthFill.rectTransform, 3f);
            healthText = UIKit.Text(healthBack.rectTransform, "Text", "", 24, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Stretch(healthText.rectTransform);

            // Способность
            var abilityPanel = UIKit.Image(hud, "AbilityPanel", UIKit.Panel);
            UIKit.Place(abilityPanel.rectTransform, Vector2.zero, new Vector2(500f, 30f), new Vector2(86f, 86f));
            abilityIcon = UIKit.Image(abilityPanel.rectTransform, "Icon", RunState.HeroInfo.themeColor);
            UIKit.Stretch(abilityIcon.rectTransform, 8f);
            abilityFill = UIKit.Image(abilityPanel.rectTransform, "Cooldown", new Color(0f, 0f, 0f, 0.7f));
            abilityFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            abilityFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            abilityFill.rectTransform.offsetMin = new Vector2(8f, 8f);
            abilityFill.rectTransform.offsetMax = new Vector2(-8f, -8f);
            abilityKey = UIKit.Text(abilityPanel.rectTransform, "Key", "Q", 40, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Stretch(abilityKey.rectTransform);
            abilityText = UIKit.Text(hud, "AbilityName", RunState.HeroInfo.abilityName, 20, TextAnchor.LowerLeft, Color.white);
            UIKit.Place(abilityText.rectTransform, Vector2.zero, new Vector2(500f, 120f), new Vector2(320f, 28f));

            // Оружие (справа снизу)
            var weaponPanel = UIKit.Image(hud, "WeaponPanel", UIKit.Panel);
            UIKit.Place(weaponPanel.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(340f, 110f));
            weaponText = UIKit.Text(weaponPanel.rectTransform, "Weapon", "", 26, TextAnchor.UpperLeft, UIKit.Accent, true);
            UIKit.Place(weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -10f), new Vector2(300f, 32f));
            ammoText = UIKit.Text(weaponPanel.rectTransform, "Ammo", "", 46, TextAnchor.LowerRight, Color.white, true);
            UIKit.Place(ammoText.rectTransform, new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(300f, 56f));
            var reloadBack = UIKit.Image(weaponPanel.rectTransform, "ReloadBack", new Color(0f, 0f, 0f, 0.5f));
            UIKit.Place(reloadBack.rectTransform, new Vector2(0f, 0f), new Vector2(18f, 14f), new Vector2(150f, 10f));
            reloadFill = UIKit.Image(reloadBack.rectTransform, "Fill", UIKit.Cyan);
            UIKit.Stretch(reloadFill.rectTransform);
            weaponSlots = UIKit.Rect(hud, "Slots");
            UIKit.Place(weaponSlots, new Vector2(1f, 0f), new Vector2(-30f, 150f), new Vector2(340f, 30f));

            // Монеты, волна, враги (сверху)
            var topPanel = UIKit.Image(hud, "TopPanel", UIKit.Panel);
            UIKit.Place(topPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(560f, 76f));
            waveText = UIKit.Text(topPanel.rectTransform, "Wave", "", 30, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(540f, 38f));
            enemiesText = UIKit.Text(topPanel.rectTransform, "Enemies", "", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            UIKit.Place(enemiesText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(540f, 28f));

            var coinPanel = UIKit.Image(hud, "CoinPanel", UIKit.Panel);
            UIKit.Place(coinPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(200f, 64f));
            var coinIcon = UIKit.Image(coinPanel.rectTransform, "CoinIcon", UIKit.Accent);
            coinIcon.sprite = UIKit.Circle;
            UIKit.Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(38f, 38f), new Vector2(0f, 0.5f));
            coinsText = UIKit.Text(coinPanel.rectTransform, "Coins", "0", 34, TextAnchor.MiddleRight, UIKit.Accent, true);
            UIKit.Place(coinsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(140f, 50f), new Vector2(1f, 0.5f));

            hintText = UIKit.Text(hud, "Hint", "WASD ходить · Shift бег · Пробел прыжок · ЛКМ стрелять · R перезарядка · 1/2/3 оружие · Q способность · Esc пауза", 18, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.6f));
            UIKit.Place(hintText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(560f, 60f));

            // Полоска босса
            bossBar = UIKit.Rect(hud, "BossBar");
            UIKit.Place(bossBar, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 60f), new Vector2(0.5f, 1f));
            bossName = UIKit.Text(bossBar, "Name", "", 28, TextAnchor.UpperCenter, UIKit.Pink, true);
            UIKit.Place(bossName.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 34f), new Vector2(0.5f, 1f));
            var bossBack = UIKit.Image(bossBar, "Back", new Color(0f, 0f, 0f, 0.6f));
            UIKit.Place(bossBack.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 24f), new Vector2(0.5f, 0f));
            bossFill = UIKit.Image(bossBack.rectTransform, "Fill", UIKit.Pink);
            UIKit.Stretch(bossFill.rectTransform, 3f);
            bossBar.gameObject.SetActive(false);

            // Большое сообщение по центру
            banner = UIKit.Rect(hud, "Banner");
            UIKit.Place(banner, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1600f, 200f), new Vector2(0.5f, 0.5f));
            bannerTitle = UIKit.Text(banner, "Title", "", 76, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Place(bannerTitle.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1600f, 100f), new Vector2(0.5f, 1f));
            bannerSubtitle = UIKit.Text(banner, "Subtitle", "", 34, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(bannerSubtitle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1600f, 60f), new Vector2(0.5f, 0f));
            banner.gameObject.SetActive(false);
        }

        public void ShowBanner(string title, string subtitle, float seconds, Color? color = null)
        {
            bannerTitle.text = title;
            bannerTitle.color = color ?? Color.white;
            bannerSubtitle.text = subtitle;
            bannerTimer = seconds;
            bannerDuration = seconds;
            banner.gameObject.SetActive(true);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null || player == null) return;
            float dt = Time.unscaledDeltaTime;

            // Здоровье
            float hp = player.Health.Fraction;
            shownHealth = Mathf.MoveTowards(shownHealth, hp, dt * 2f);
            healthFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, shownHealth), 1f);
            healthFill.color = Color.Lerp(GameAssets.Hex("#FF4D5E"), UIKit.Green, Mathf.InverseLerp(0.2f, 0.7f, hp));
            healthText.text = Mathf.CeilToInt(player.Health.Current) + " / " + Mathf.CeilToInt(player.Health.maxHealth);

            damageFlash = Mathf.MoveTowards(damageFlash, 0f, dt * 2f);
            damageVignette.color = new Color(1f, 0f, 0f, damageFlash * 0.35f);
            float low = hp < 0.3f && !player.Health.IsDead ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) * (0.3f - hp) : 0f;
            lowHealthPulse.color = new Color(0.8f, 0f, 0.1f, low);
            slowTint.color = new Color(0.55f, 0.35f, 0.2f, HeroAbility.SlowTimeActive ? 0.18f : 0f);

            // Способность
            var ability = gm.Ability;
            if (ability != null)
            {
                float cd = ability.CooldownFraction;
                abilityFill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(cd, 0.2f));
                abilityFill.enabled = cd > 0.01f;
                abilityKey.color = cd <= 0f ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                abilityIcon.color = cd <= 0f ? RunState.HeroInfo.themeColor * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f)) : RunState.HeroInfo.themeColor * 0.5f;
            }

            // Оружие
            var weapons = gm.Weapons;
            if (weapons != null)
            {
                weaponText.text = weapons.CurrentDef.name;
                ammoText.text = weapons.IsReloading ? "<size=30>перезарядка</size>" : weapons.Ammo + " <size=28>/ " + weapons.MagazineSize + "</size>";
                ammoText.color = weapons.Ammo <= Mathf.Max(1, weapons.MagazineSize / 5) && !weapons.IsReloading ? UIKit.Pink : Color.white;
                reloadFill.rectTransform.anchorMax = new Vector2(weapons.IsReloading ? weapons.ReloadProgress : 0f, 1f);
                UpdateSlots(weapons);
            }

            // Монеты
            if (shownCoins != RunState.coins) shownCoins = (int)Mathf.MoveTowards(shownCoins, RunState.coins, Mathf.Max(1f, Mathf.Abs(RunState.coins - shownCoins) * dt * 8f));
            coinsText.text = shownCoins.ToString();

            // Волна
            waveText.text = gm.StatusTitle;
            enemiesText.text = gm.StatusSubtitle;

            // Босс
            var boss = gm.Boss;
            bossBar.gameObject.SetActive(boss != null && !boss.IsDead);
            if (boss != null && !boss.IsDead)
            {
                bossName.text = boss.Info.name.ToUpper() + " — " + boss.Info.bossTitle;
                bossFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, boss.Health.Fraction), 1f);
            }

            // Прицел и попадания
            hitTimer -= dt;
            hitMarker.gameObject.SetActive(hitTimer > 0f);
            if (hitTimer > 0f)
            {
                foreach (var g in hitMarker.GetComponentsInChildren<Image>()) g.color = hitCrit ? UIKit.Accent : Color.white;
                hitMarker.localScale = Vector3.one * (1f + hitTimer * 2f);
            }
            float spread = weapons != null ? (weapons.CurrentDef.spread * 2f + player.MoveFraction * 8f) : 0f;
            for (int i = 0; i < crosshairLines.Count; i++)
            {
                Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
                crosshairLines[i].rectTransform.anchoredPosition = dirs[i] * (11f + spread);
            }
            crosshair.gameObject.SetActive(!ScreenOpen);

            // Баннер
            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                float t = 1f - bannerTimer / bannerDuration;
                float appear = Mathf.Clamp01(t * 6f);
                float fade = Mathf.Clamp01(bannerTimer * 2f);
                banner.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, appear);
                var c = bannerTitle.color;
                bannerTitle.color = new Color(c.r, c.g, c.b, fade);
                bannerSubtitle.color = new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, fade);
                if (bannerTimer <= 0f) banner.gameObject.SetActive(false);
            }

            hintText.gameObject.SetActive(RunState.arenaIndex == 0 && gm.WaveNumber <= 1);
        }

        void UpdateSlots(WeaponSystem weapons)
        {
            while (slotTexts.Count < weapons.SlotCount)
            {
                int i = slotTexts.Count;
                var t = UIKit.Text(weaponSlots, "Slot", "", 20, TextAnchor.MiddleRight, Color.white, true);
                UIKit.Place(t.rectTransform, new Vector2(1f, 0.5f), new Vector2(-i * 115f, 0f), new Vector2(110f, 30f), new Vector2(1f, 0.5f));
                slotTexts.Add(t);
            }
            for (int i = 0; i < slotTexts.Count; i++)
            {
                int index = weapons.SlotCount - 1 - i;
                if (index < 0) { slotTexts[i].text = ""; continue; }
                bool active = index == weapons.CurrentIndex;
                slotTexts[i].text = (index + 1) + " " + WeaponDef.Get(weapons.SlotId(index)).name;
                slotTexts[i].color = active ? UIKit.Accent : new Color(1f, 1f, 1f, 0.5f);
            }
        }

        // ---------------- Экраны ----------------

        public void CloseScreen()
        {
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
        }

        RectTransform NewScreen(Color dim)
        {
            CloseScreen();
            var screen = UIKit.Image(overlay, "Screen", dim, false);
            screen.raycastTarget = true;
            UIKit.Stretch(screen.rectTransform);
            return screen.rectTransform;
        }

        public void ShowUpgrades(List<UpgradeDef> options, Action<UpgradeDef> onPick)
        {
            var screen = NewScreen(new Color(0.03f, 0.02f, 0.08f, 0.75f));
            var title = UIKit.Text(screen, "Title", "ВЫБЕРИ УЛУЧШЕНИЕ", 64, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 90f), new Vector2(0.5f, 0.5f));
            var sub = UIKit.Text(screen, "Sub", "Волна пройдена! Возьми одну награду", 30, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1400f, 50f), new Vector2(0.5f, 0.5f));

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                float x = (i - (options.Count - 1) / 2f) * 460f;
                var card = UIKit.Button(screen, "Card", "", UIKit.Panel, () => onPick(option));
                UIKit.Place(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(x, -20f), new Vector2(420f, 420f), new Vector2(0.5f, 0.5f));
                var cardRect = card.GetComponent<RectTransform>();
                var stripe = UIKit.Image(cardRect, "Stripe", option.color);
                UIKit.Place(stripe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(392f, 18f), new Vector2(0.5f, 1f));
                var icon = UIKit.Image(cardRect, "Icon", option.color);
                icon.sprite = UIKit.Circle;
                UIKit.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(110f, 110f), new Vector2(0.5f, 1f));
                var letter = UIKit.Text(icon.rectTransform, "Letter", option.name.Substring(0, 1), 64, TextAnchor.MiddleCenter, Color.white, true);
                UIKit.Stretch(letter.rectTransform);
                var name = UIKit.Text(cardRect, "Name", option.name, 38, TextAnchor.MiddleCenter, option.color, true);
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(390f, 60f), new Vector2(0.5f, 0.5f));
                var desc = UIKit.Text(cardRect, "Desc", option.description, 28, TextAnchor.UpperCenter, Color.white);
                UIKit.Place(desc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(380f, 140f), new Vector2(0.5f, 1f));
                var key = UIKit.Text(cardRect, "Key", "[" + (i + 1) + "]", 24, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.5f));
                UIKit.Place(key.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(200f, 30f), new Vector2(0.5f, 0f));
                card.gameObject.AddComponent<KeyShortcut>().Setup(i + 1, () => onPick(option));
            }
        }

        public class ShopItem
        {
            public string name;
            public string description;
            public int price;
            public Color color;
            public bool soldOut;
            public Func<bool> buy; // true если покупка удалась
        }

        public void ShowShop(Func<List<ShopItem>> getItems, Action onContinue)
        {
            var screen = NewScreen(new Color(0.03f, 0.02f, 0.08f, 0.85f));
            var title = UIKit.Text(screen, "Title", "МАГАЗИН МАМЫ-МИИ", 64, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 400f), new Vector2(1400f, 90f), new Vector2(0.5f, 0.5f));
            var coins = UIKit.Text(screen, "Coins", "", 40, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(coins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 60f), new Vector2(0.5f, 0.5f));

            var grid = UIKit.Rect(screen, "Grid");
            UIKit.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1500f, 560f), new Vector2(0.5f, 0.5f));

            void Rebuild()
            {
                for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
                coins.text = "Монеты: " + RunState.coins;
                var items = getItems();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    int col = i % 3, row = i / 3;
                    bool affordable = !item.soldOut && RunState.coins >= item.price;
                    var button = UIKit.Button(grid, "Item", "", affordable ? UIKit.Panel : new Color(0.15f, 0.12f, 0.2f, 0.7f), () =>
                    {
                        if (item.soldOut || RunState.coins < item.price) { SoundFX.Play(SoundFX.Sfx.Empty); return; }
                        if (item.buy())
                        {
                            RunState.coins -= item.price;
                            SoundFX.Play(SoundFX.Sfx.Upgrade, 0.7f);
                            Rebuild();
                        }
                    });
                    var rect = button.GetComponent<RectTransform>();
                    UIKit.Place(rect, new Vector2(0.5f, 1f), new Vector2((col - 1) * 490f, -row * 280f), new Vector2(460f, 250f), new Vector2(0.5f, 1f));
                    var stripe = UIKit.Image(rect, "Stripe", item.color);
                    UIKit.Place(stripe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(430f, 14f), new Vector2(0.5f, 1f));
                    var n = UIKit.Text(rect, "Name", item.name, 34, TextAnchor.MiddleCenter, item.color, true);
                    UIKit.Place(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(430f, 50f), new Vector2(0.5f, 1f));
                    var d = UIKit.Text(rect, "Desc", item.description, 24, TextAnchor.UpperCenter, Color.white);
                    UIKit.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(420f, 80f), new Vector2(0.5f, 1f));
                    var p = UIKit.Text(rect, "Price", item.soldOut ? "КУПЛЕНО" : item.price + " монет", 32, TextAnchor.LowerCenter, item.soldOut ? UIKit.Green : (affordable ? UIKit.Accent : UIKit.Pink), true);
                    UIKit.Place(p.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(420f, 44f), new Vector2(0.5f, 0f));
                    button.interactable = !item.soldOut;
                }
            }

            Rebuild();

            var next = UIKit.Button(screen, "Next", "В БОЙ! ▶", GameAssets.Hex("#E53977"), onContinue, 40);
            UIKit.Place(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 90f), new Vector2(0.5f, 0f));
        }

        public void ShowPause(Action onResume, Action onMenu)
        {
            var screen = NewScreen(new Color(0.03f, 0.02f, 0.08f, 0.8f));
            var title = UIKit.Text(screen, "Title", "ПАУЗА", 90, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1000f, 120f), new Vector2(0.5f, 0.5f));

            var stats = RunState.stats;
            string info = "Герой: " + RunState.HeroInfo.name + "\n" +
                          "Урон ×" + stats.damageMultiplier.ToString("0.00") + " · Скорострельность ×" + stats.fireRateMultiplier.ToString("0.00") + "\n" +
                          "Крит " + Mathf.RoundToInt(stats.critChance * 100f) + "% · Скорость ×" + stats.moveSpeedMultiplier.ToString("0.00") + "\n" +
                          "Побеждено врагов: " + RunState.kills;
            var infoText = UIKit.Text(screen, "Info", info, 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
            UIKit.Place(infoText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1200f, 160f), new Vector2(0.5f, 0.5f));

            var resume = UIKit.Button(screen, "Resume", "Продолжить", GameAssets.Hex("#2BB673"), onResume, 38);
            UIKit.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(460f, 90f), new Vector2(0.5f, 0.5f));

            var sensLabel = UIKit.Text(screen, "Sens", "", 26, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(sensLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(600f, 40f), new Vector2(0.5f, 0.5f));
            void UpdateSens() => sensLabel.text = "Чувствительность мыши: " + PlayerController.MouseSensitivity.ToString("0.00");
            UpdateSens();
            var minus = UIKit.Button(screen, "Minus", "−", UIKit.Panel, () => { PlayerController.MouseSensitivity = Mathf.Max(0.02f, PlayerController.MouseSensitivity - 0.02f); PlayerPrefs.SetFloat("BW_Sens", PlayerController.MouseSensitivity); UpdateSens(); }, 40);
            UIKit.Place(minus.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-360f, -130f), new Vector2(70f, 60f), new Vector2(0.5f, 0.5f));
            var plus = UIKit.Button(screen, "Plus", "+", UIKit.Panel, () => { PlayerController.MouseSensitivity = Mathf.Min(0.5f, PlayerController.MouseSensitivity + 0.02f); PlayerPrefs.SetFloat("BW_Sens", PlayerController.MouseSensitivity); UpdateSens(); }, 40);
            UIKit.Place(plus.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(360f, -130f), new Vector2(70f, 60f), new Vector2(0.5f, 0.5f));

            var menu = UIKit.Button(screen, "Menu", "Выйти в меню", GameAssets.Hex("#8E44AD"), onMenu, 34);
            UIKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(460f, 80f), new Vector2(0.5f, 0.5f));
        }

        public void ShowEnd(bool victory, bool record, Action onRetry, Action onMenu)
        {
            var screen = NewScreen(victory ? new Color(0.05f, 0.08f, 0.04f, 0.85f) : new Color(0.12f, 0.02f, 0.05f, 0.88f));
            string titleText = victory ? "ПОБЕДА! БРЕЙНРОТЫ ПОВЕРЖЕНЫ!" : "ТЕБЯ ЗАБРЕЙНРОТИЛИ!";
            var title = UIKit.Text(screen, "Title", titleText, 72, TextAnchor.MiddleCenter, victory ? UIKit.Accent : UIKit.Pink, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(1700f, 110f), new Vector2(0.5f, 0.5f));

            int minutes = Mathf.FloorToInt(RunState.time / 60f);
            int seconds = Mathf.FloorToInt(RunState.time % 60f);
            string arenaLine = victory
                ? "Пройдены все арены: " + RunState.ArenaCount
                : "Арена: " + (RunState.arenaIndex + 1) + " из " + RunState.ArenaCount + " (" + Brainrots.Get(RunState.CurrentArenaOwner).arenaName + ")";
            string text = "Герой: " + RunState.HeroInfo.name + "\n" + arenaLine + "\n" +
                          "Побеждено врагов: " + RunState.kills + "\n" +
                          "Время: " + minutes + ":" + seconds.ToString("00") + "\n" +
                          (record ? "<color=#FFC93C>НОВЫЙ РЕКОРД!</color>" : "Лучший результат: " + RunState.BestArena + " арен");
            var stats = UIKit.Text(screen, "Stats", text, 34, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1400f, 280f), new Vector2(0.5f, 0.5f));

            var retry = UIKit.Button(screen, "Retry", "Ещё раз", GameAssets.Hex("#2BB673"), onRetry, 38);
            UIKit.Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-250f, -200f), new Vector2(440f, 90f), new Vector2(0.5f, 0.5f));
            var menu = UIKit.Button(screen, "Menu", "Главное меню", GameAssets.Hex("#8E44AD"), onMenu, 38);
            UIKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(250f, -200f), new Vector2(440f, 90f), new Vector2(0.5f, 0.5f));
        }
    }

    /// <summary>Нажатие клавиши 1/2/3 выбирает карточку.</summary>
    public class KeyShortcut : MonoBehaviour
    {
        int number;
        Action action;

        public void Setup(int key, Action onPress)
        {
            number = key;
            action = onPress;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if ((number == 1 && kb.digit1Key.wasPressedThisFrame) ||
                (number == 2 && kb.digit2Key.wasPressedThisFrame) ||
                (number == 3 && kb.digit3Key.wasPressedThisFrame))
            {
                SoundFX.Play(SoundFX.Sfx.Click, 0.6f);
                action?.Invoke();
            }
        }
    }
}
