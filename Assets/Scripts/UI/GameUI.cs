using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>
    /// Весь интерфейс боя: полоски, прицел, индикатор урона, комбо, сообщения,
    /// выбор улучшений, магазин, пауза, финал с наградой, заставка босса.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        RectTransform root;
        RectTransform hud;
        CanvasGroup hudGroup;
        RectTransform overlay;

        // HUD
        Image healthFill, healthLag, abilityFill, abilityIcon, reloadFill, bossFill, damageVignette, slowTint, coinIcon, comboFill;
        Text healthText, ammoText, weaponText, abilityText, abilityKey, coinsText, waveText, enemiesText, bannerTitle, bannerSubtitle, bossName, hintText, comboText, comboLabel;
        RectTransform crosshair, hitMarker, bossBar, banner, weaponSlots, combo, healthPanel;
        readonly List<Image> crosshairLines = new List<Image>();
        readonly List<Text> slotTexts = new List<Text>();
        float hitTimer, bannerTimer, bannerDuration, damageFlash, coinPunch, comboPunch, healthShake, abilityReadyFlash;
        bool hitCrit;
        float shownHealth = 1f, lagHealth = 1f;
        int shownCoins, lastCombo;
        bool abilityWasReady = true;
        Color bannerColor = Color.white;

        // Индикатор направления урона
        class DamageArrow
        {
            public Image image;
            public Vector3 source;
            public float timer;
        }
        readonly List<DamageArrow> arrows = new List<DamageArrow>();

        // Заставка босса
        RectTransform letterboxTop, letterboxBottom, bossCard;
        Text bossCardName, bossCardTitle;
        float letterbox, letterboxTarget;

        public bool ScreenOpen => overlay != null && overlay.childCount > 0;

        void Awake()
        {
            Instance = this;
            UIKit.EnsureEventSystem();
            root = UIKit.Canvas("GameCanvas", 10);
            root.SetParent(transform, false);
            BuildHUD();
            BuildBossIntro();
            overlay = UIKit.Rect(root, "Overlay");
            UIKit.Stretch(overlay);
            Loc.Changed += UpdateHint;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Loc.Changed -= UpdateHint;
        }

        public void Bind(PlayerController player, WeaponSystem weapons)
        {
            player.Health.Damaged += (amount, crit) =>
            {
                damageFlash = Mathf.Clamp01(damageFlash + 0.5f);
                healthShake = 0.3f;
            };
            player.Health.DamagedFrom += ShowDamageFrom;
            weapons.HitCrit += crit => { hitTimer = 0.15f; hitCrit = crit; };
            shownCoins = RunState.coins;
        }

        // ---------------- HUD ----------------

        void BuildHUD()
        {
            hud = UIKit.Rect(root, "HUD");
            UIKit.Stretch(hud);
            hudGroup = hud.gameObject.AddComponent<CanvasGroup>();

            damageVignette = UIKit.Image(hud, "DamageVignette", new Color(1f, 0f, 0f, 0f), false);
            UIKit.Stretch(damageVignette.rectTransform);
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

            // Стрелки направления урона
            for (int i = 0; i < 6; i++)
            {
                var img = UIKit.Icon(hud, "DamageArrow", IconKind.Play, new Color(1f, 0.15f, 0.2f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, 70f, new Vector2(0.5f, 0.5f));
                img.rectTransform.sizeDelta = new Vector2(46f, 80f);
                img.preserveAspect = false;
                arrows.Add(new DamageArrow { image = img });
            }

            // Здоровье (слева снизу)
            var hpPanel = UIKit.Image(hud, "HealthPanel", UIKit.Panel);
            healthPanel = hpPanel.rectTransform;
            UIKit.Place(healthPanel, Vector2.zero, new Vector2(30f, 30f), new Vector2(470f, 90f));
            UIKit.Icon(healthPanel, "HeartIcon", IconKind.Heart, GameAssets.Hex("#FF5F7E"), new Vector2(0f, 0f), new Vector2(14f, 12f), 42f, Vector2.zero).gameObject.AddComponent<Pulse>().amount = 0.06f;
            var heroName = UIKit.Text(healthPanel, "Hero", RunState.HeroInfo.name, 22, TextAnchor.UpperLeft, RunState.HeroInfo.themeColor, true);
            UIKit.Place(heroName.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(430f, 28f));
            var healthBack = UIKit.Image(healthPanel, "HealthBack", new Color(0f, 0f, 0f, 0.5f));
            UIKit.Place(healthBack.rectTransform, new Vector2(0f, 0f), new Vector2(64f, 14f), new Vector2(390f, 36f));
            healthLag = UIKit.Image(healthBack.rectTransform, "Lag", new Color(1f, 0.9f, 0.9f, 0.8f));
            UIKit.Stretch(healthLag.rectTransform, 3f);
            healthFill = UIKit.Image(healthBack.rectTransform, "Fill", UIKit.Green);
            UIKit.Stretch(healthFill.rectTransform, 3f);
            healthText = UIKit.Text(healthBack.rectTransform, "Text", "", 24, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Stretch(healthText.rectTransform);

            // Способность
            var abilityPanel = UIKit.Image(hud, "AbilityPanel", UIKit.Panel);
            UIKit.Place(abilityPanel.rectTransform, Vector2.zero, new Vector2(515f, 30f), new Vector2(90f, 90f));
            abilityIcon = UIKit.Icon(abilityPanel.rectTransform, "Icon", AbilityIcon(RunState.hero), RunState.HeroInfo.themeColor, new Vector2(0.5f, 0.5f), Vector2.zero, 62f, new Vector2(0.5f, 0.5f));
            abilityFill = UIKit.Image(abilityPanel.rectTransform, "Cooldown", new Color(0f, 0f, 0f, 0.7f));
            abilityFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            abilityFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            abilityFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            abilityFill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            abilityKey = UIKit.Text(abilityPanel.rectTransform, "Key", "", 24, TextAnchor.LowerRight, Color.white, true);
            UIKit.Place(abilityKey.rectTransform, new Vector2(1f, 0f), new Vector2(-6f, 2f), new Vector2(80f, 30f), new Vector2(1f, 0f));
            abilityText = UIKit.Text(hud, "AbilityName", RunState.HeroInfo.abilityName, 20, TextAnchor.LowerLeft, Color.white);
            UIKit.Place(abilityText.rectTransform, Vector2.zero, new Vector2(515f, 126f), new Vector2(320f, 28f));

            // Оружие (справа снизу)
            var weaponPanel = UIKit.Image(hud, "WeaponPanel", UIKit.Panel);
            UIKit.Place(weaponPanel.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(340f, 110f));
            weaponText = UIKit.Text(weaponPanel.rectTransform, "Weapon", "", 26, TextAnchor.UpperLeft, UIKit.Accent, true);
            UIKit.Place(weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -10f), new Vector2(300f, 32f));
            UIKit.Icon(weaponPanel.rectTransform, "AmmoIcon", IconKind.Ammo, UIKit.Accent, new Vector2(0f, 0f), new Vector2(16f, 34f), 40f, Vector2.zero);
            ammoText = UIKit.Text(weaponPanel.rectTransform, "Ammo", "", 46, TextAnchor.LowerRight, Color.white, true);
            UIKit.Place(ammoText.rectTransform, new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(260f, 56f));
            var reloadBack = UIKit.Image(weaponPanel.rectTransform, "ReloadBack", new Color(0f, 0f, 0f, 0.5f));
            UIKit.Place(reloadBack.rectTransform, new Vector2(0f, 0f), new Vector2(18f, 14f), new Vector2(120f, 10f));
            reloadFill = UIKit.Image(reloadBack.rectTransform, "Fill", UIKit.Cyan);
            UIKit.Stretch(reloadFill.rectTransform);
            weaponSlots = UIKit.Rect(hud, "Slots");
            UIKit.Place(weaponSlots, new Vector2(1f, 0f), new Vector2(-30f, 150f), new Vector2(340f, 30f));

            // Волна (сверху)
            var topPanel = UIKit.Image(hud, "TopPanel", UIKit.Panel);
            UIKit.Place(topPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 76f));
            UIKit.Icon(topPanel.rectTransform, "WaveIcon", IconKind.Wave, UIKit.Cyan, new Vector2(0f, 0.5f), new Vector2(16f, 0f), 40f, new Vector2(0f, 0.5f));
            waveText = UIKit.Text(topPanel.rectTransform, "Wave", "", 30, TextAnchor.MiddleCenter, Color.white, true);
            UIKit.Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(540f, 38f));
            enemiesText = UIKit.Text(topPanel.rectTransform, "Enemies", "", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            UIKit.Place(enemiesText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(540f, 28f));

            // Монеты
            var coinPanel = UIKit.Image(hud, "CoinPanel", UIKit.Panel);
            UIKit.Place(coinPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(210f, 64f));
            coinIcon = UIKit.Icon(coinPanel.rectTransform, "CoinIcon", IconKind.Coin, UIKit.Accent, new Vector2(0f, 0.5f), new Vector2(14f, 0f), 42f, new Vector2(0f, 0.5f));
            coinsText = UIKit.Text(coinPanel.rectTransform, "Coins", "0", 34, TextAnchor.MiddleRight, UIKit.Accent, true);
            UIKit.Place(coinsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(140f, 50f), new Vector2(1f, 0.5f));

            // Комбо (справа по центру)
            combo = UIKit.Rect(hud, "Combo");
            UIKit.Place(combo, new Vector2(1f, 0.5f), new Vector2(-60f, 120f), new Vector2(300f, 110f), new Vector2(1f, 0.5f));
            comboLabel = UIKit.Text(combo, "Label", new LS("КОМБО", "COMBO"), 26, TextAnchor.UpperRight, UIKit.Pink, true);
            UIKit.Place(comboLabel.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(300f, 32f), new Vector2(1f, 1f));
            comboText = UIKit.Text(combo, "Count", "", 64, TextAnchor.MiddleRight, Color.white, true);
            UIKit.Place(comboText.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, -4f), new Vector2(300f, 70f), new Vector2(1f, 0.5f));
            var comboBack = UIKit.Image(combo, "Back", new Color(0f, 0f, 0f, 0.5f));
            UIKit.Place(comboBack.rectTransform, new Vector2(1f, 0f), Vector2.zero, new Vector2(200f, 10f), new Vector2(1f, 0f));
            comboFill = UIKit.Image(comboBack.rectTransform, "Fill", UIKit.Pink);
            UIKit.Stretch(comboFill.rectTransform, 2f);
            combo.gameObject.SetActive(false);

            hintText = UIKit.Text(hud, "Hint", "", 18, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.6f));
            UIKit.Place(hintText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(560f, 80f));
            UpdateHint();

            // Полоска босса
            bossBar = UIKit.Rect(hud, "BossBar");
            UIKit.Place(bossBar, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 64f), new Vector2(0.5f, 1f));
            UIKit.Icon(bossBar, "Skull", IconKind.Skull, UIKit.Pink, new Vector2(0f, 0f), new Vector2(-50f, -6f), 44f, new Vector2(0f, 0f));
            bossName = UIKit.Text(bossBar, "Name", "", 28, TextAnchor.UpperCenter, UIKit.Pink, true);
            UIKit.Place(bossName.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 34f), new Vector2(0.5f, 1f));
            var bossBack = UIKit.Image(bossBar, "Back", new Color(0f, 0f, 0f, 0.6f));
            UIKit.Place(bossBack.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 26f), new Vector2(0.5f, 0f));
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

        void UpdateHint()
        {
            if (hintText == null) return;
            hintText.text = Loc.T(
                Controls.KeyName(GameAction.Forward) + Controls.KeyName(GameAction.Left) + Controls.KeyName(GameAction.Back) + Controls.KeyName(GameAction.Right) + " ходить · " + Controls.KeyName(GameAction.Sprint) + " бег · " + Controls.KeyName(GameAction.Jump) + " прыжок · ЛКМ стрелять · " +
                Controls.KeyName(GameAction.Reload) + " перезарядка · 1/2/3 оружие · " + Controls.KeyName(GameAction.Ability) + " способность · Esc пауза",
                Controls.KeyName(GameAction.Forward) + Controls.KeyName(GameAction.Left) + Controls.KeyName(GameAction.Back) + Controls.KeyName(GameAction.Right) + " move · " + Controls.KeyName(GameAction.Sprint) + " sprint · " + Controls.KeyName(GameAction.Jump) + " jump · LMB shoot · " +
                Controls.KeyName(GameAction.Reload) + " reload · 1/2/3 weapons · " + Controls.KeyName(GameAction.Ability) + " ability · Esc pause");
        }

        public static IconKind AbilityIcon(BrainrotId id)
        {
            switch (id)
            {
                case BrainrotId.Tralalero: return IconKind.Fang;
                case BrainrotId.Patapim: return IconKind.Magnet;
                case BrainrotId.Chimpanzini: return IconKind.Bomb;
                case BrainrotId.Bombardiro: return IconKind.Fire;
                case BrainrotId.Cappuccino: return IconKind.Clock;
                default: return IconKind.Bolt;
            }
        }

        public void ShowBanner(string title, string subtitle, float seconds, Color? color = null)
        {
            bannerTitle.text = title;
            bannerColor = color ?? Color.white;
            bannerTitle.color = bannerColor;
            bannerSubtitle.text = subtitle;
            bannerTimer = seconds;
            bannerDuration = seconds;
            banner.gameObject.SetActive(true);
        }

        void ShowDamageFrom(Vector3 source)
        {
            DamageArrow free = arrows[0];
            foreach (var a in arrows)
                if (a.timer < free.timer) free = a;
            free.source = source;
            free.timer = 1.2f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var player = PlayerController.Instance;
            if (gm == null || player == null) return;
            float dt = Time.unscaledDeltaTime;

            // Здоровье: основная полоса + «тающая» белая полоса
            float hp = player.Health.Fraction;
            shownHealth = Mathf.MoveTowards(shownHealth, hp, dt * 3f);
            if (hp > lagHealth) lagHealth = hp;
            else lagHealth = Mathf.MoveTowards(lagHealth, hp, dt * (lagHealth - hp > 0.01f && damageFlash < 0.2f ? 0.6f : 0f));
            healthFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, shownHealth), 1f);
            healthLag.rectTransform.anchorMax = new Vector2(Mathf.Max(0.001f, lagHealth), 1f);
            healthFill.color = Color.Lerp(GameAssets.Hex("#FF4D5E"), UIKit.Green, Mathf.InverseLerp(0.2f, 0.7f, hp));
            healthText.text = Mathf.CeilToInt(player.Health.Current) + " / " + Mathf.CeilToInt(player.Health.maxHealth);
            healthShake = Mathf.MoveTowards(healthShake, 0f, dt);
            healthPanel.anchoredPosition = new Vector2(30f, 30f) + UnityEngine.Random.insideUnitCircle * healthShake * 20f;

            damageFlash = Mathf.MoveTowards(damageFlash, 0f, dt * 2f);
            damageVignette.color = new Color(1f, 0f, 0f, damageFlash * 0.18f);
            slowTint.color = new Color(0.55f, 0.35f, 0.2f, HeroAbility.SlowTimeActive ? 0.15f : 0f);

            // Стрелки урона
            foreach (var a in arrows)
            {
                a.timer -= dt;
                if (a.timer <= 0f)
                {
                    a.image.enabled = false;
                    continue;
                }
                a.image.enabled = true;
                Vector3 dir = a.source - player.transform.position;
                Vector3 local = player.transform.InverseTransformDirection(dir);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                Vector2 pos = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * 170f;
                a.image.rectTransform.anchoredPosition = pos;
                a.image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f - angle);
                float alpha = Mathf.Clamp01(a.timer / 0.5f) * 0.9f;
                a.image.color = new Color(1f, 0.15f, 0.2f, alpha);
                a.image.rectTransform.localScale = Vector3.one * (1f + Mathf.Clamp01(a.timer - 1f) * 3f);
            }

            // Способность
            var ability = gm.Ability;
            if (ability != null)
            {
                float cd = ability.CooldownFraction;
                abilityFill.rectTransform.anchorMax = new Vector2(1f, Mathf.Max(cd, 0f));
                abilityFill.enabled = cd > 0.01f;
                abilityKey.text = Controls.KeyName(GameAction.Ability);
                abilityKey.color = cd <= 0f ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                bool ready = cd <= 0f;
                if (ready && !abilityWasReady) abilityReadyFlash = 0.4f;
                abilityWasReady = ready;
                abilityReadyFlash = Mathf.MoveTowards(abilityReadyFlash, 0f, dt);
                abilityIcon.color = ready ? RunState.HeroInfo.themeColor * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f)) + Color.white * abilityReadyFlash * 2f : RunState.HeroInfo.themeColor * 0.5f;
                abilityIcon.rectTransform.localScale = Vector3.one * (1f + abilityReadyFlash * 0.8f);
            }

            // Оружие
            var weapons = gm.Weapons;
            if (weapons != null)
            {
                weaponText.text = weapons.CurrentDef.name;
                ammoText.text = weapons.IsReloading ? "<size=28>" + Loc.T("перезарядка", "reloading") + "</size>" : weapons.Ammo + " <size=28>/ " + weapons.MagazineSize + "</size>";
                ammoText.color = weapons.Ammo <= Mathf.Max(1, weapons.MagazineSize / 5) && !weapons.IsReloading ? UIKit.Pink : Color.white;
                reloadFill.rectTransform.anchorMax = new Vector2(weapons.IsReloading ? weapons.ReloadProgress : 0f, 1f);
                UpdateSlots(weapons);
            }

            // Монеты
            if (shownCoins != RunState.coins)
            {
                if (RunState.coins > shownCoins) coinPunch = 0.25f;
                shownCoins = (int)Mathf.MoveTowards(shownCoins, RunState.coins, Mathf.Max(1f, Mathf.Abs(RunState.coins - shownCoins) * dt * 8f));
            }
            coinsText.text = shownCoins.ToString();
            coinPunch = Mathf.MoveTowards(coinPunch, 0f, dt);
            coinIcon.rectTransform.localScale = Vector3.one * (1f + coinPunch * 1.2f);
            coinIcon.rectTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.unscaledTime * 3f) * 40f, 0f);

            // Комбо
            int c = gm.Combo;
            combo.gameObject.SetActive(c >= 3);
            if (c > lastCombo) comboPunch = 0.3f;
            lastCombo = c;
            if (c >= 3)
            {
                comboText.text = "×" + c;
                comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt);
                comboText.rectTransform.localScale = Vector3.one * (1f + comboPunch * 1.5f);
                comboText.color = Color.Lerp(Color.white, c >= 25 ? UIKit.Accent : UIKit.Pink, Mathf.Clamp01(c / 25f));
                comboFill.rectTransform.anchorMax = new Vector2(gm.ComboFraction, 1f);
            }

            // Волна
            waveText.text = gm.StatusTitle;
            enemiesText.text = gm.StatusSubtitle;

            // Босс
            var boss = gm.Boss;
            bool showBoss = boss != null && !boss.IsDead && letterbox < 0.5f;
            bossBar.gameObject.SetActive(showBoss);
            if (showBoss)
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
            Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            for (int i = 0; i < crosshairLines.Count; i++)
                crosshairLines[i].rectTransform.anchoredPosition = dirs[i] * (11f + spread);
            crosshair.gameObject.SetActive(!ScreenOpen && letterbox < 0.1f);

            // Баннер
            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                float t = 1f - bannerTimer / bannerDuration;
                float appear = Mathf.Clamp01(t * 6f);
                float fade = Mathf.Clamp01(bannerTimer * 2f);
                banner.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, Ease.OutBack(appear));
                bannerTitle.color = new Color(bannerColor.r, bannerColor.g, bannerColor.b, fade);
                bannerSubtitle.color = new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, fade);
                if (bannerTimer <= 0f) banner.gameObject.SetActive(false);
            }

            hintText.gameObject.SetActive(RunState.arenaIndex == 0 && gm.WaveNumber <= 1);

            // Заставка босса: чёрные полосы сверху и снизу
            letterbox = Mathf.MoveTowards(letterbox, letterboxTarget, dt * 2.5f);
            float lb = Ease.InOutCubic(letterbox);
            letterboxTop.anchoredPosition = new Vector2(0f, 140f * (1f - lb));
            letterboxBottom.anchoredPosition = new Vector2(0f, -140f * (1f - lb));
            hudGroup.alpha = 1f - lb;
            letterboxTop.gameObject.SetActive(letterbox > 0.001f);
            letterboxBottom.gameObject.SetActive(letterbox > 0.001f);
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
                string key = Controls.KeyName((GameAction)((int)GameAction.Weapon1 + Mathf.Min(index, 2)));
                slotTexts[i].text = key + " " + WeaponDef.Get(weapons.SlotId(index)).name;
                slotTexts[i].color = active ? UIKit.Accent : new Color(1f, 1f, 1f, 0.5f);
            }
        }

        // ---------------- Заставка босса ----------------

        void BuildBossIntro()
        {
            letterboxTop = UIKit.Image(root, "LetterboxTop", Color.black, false).rectTransform;
            UIKit.Place(letterboxTop, new Vector2(0.5f, 1f), new Vector2(0f, 140f), new Vector2(4000f, 140f), new Vector2(0.5f, 1f));
            letterboxBottom = UIKit.Image(root, "LetterboxBottom", Color.black, false).rectTransform;
            UIKit.Place(letterboxBottom, new Vector2(0.5f, 0f), new Vector2(0f, -140f), new Vector2(4000f, 140f), new Vector2(0.5f, 0f));

            bossCard = UIKit.Rect(root, "BossCard");
            UIKit.Place(bossCard, new Vector2(0f, 0f), new Vector2(120f, 170f), new Vector2(1400f, 200f), new Vector2(0f, 0f));
            var stripe = UIKit.Image(bossCard, "Stripe", UIKit.Pink);
            UIKit.Place(stripe.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(14f, 170f), new Vector2(0f, 0.5f));
            var tag = UIKit.Text(bossCard, "Tag", new LS("БОСС", "BOSS"), 34, TextAnchor.UpperLeft, UIKit.Pink, true);
            UIKit.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(40f, 0f), new Vector2(600f, 44f), new Vector2(0f, 1f));
            bossCardName = UIKit.Text(bossCard, "Name", "", 96, TextAnchor.MiddleLeft, Color.white, true);
            UIKit.Place(bossCardName.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(1400f, 110f), new Vector2(0f, 0.5f));
            bossCardTitle = UIKit.Text(bossCard, "Title", "", 38, TextAnchor.LowerLeft, UIKit.Accent, true);
            UIKit.Place(bossCardTitle.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 0f), new Vector2(1400f, 50f), new Vector2(0f, 0f));
            bossCard.gameObject.SetActive(false);
        }

        public void ShowBossIntro(BrainrotInfo info)
        {
            letterboxTarget = 1f;
            bossCardName.text = info.name.ToUpper();
            bossCardName.color = info.themeColor;
            bossCardTitle.text = "«" + info.bossTitle + "»";
            bossCard.gameObject.SetActive(true);
            foreach (var s in bossCard.GetComponents<SlideIn>()) Destroy(s);
            SlideIn.Add(bossCard, new Vector2(-900f, 0f), 0.5f, 0.6f);
            banner.gameObject.SetActive(false);
            bannerTimer = 0f;
        }

        public void HideBossIntro()
        {
            letterboxTarget = 0f;
            bossCard.gameObject.SetActive(false);
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
            screen.gameObject.AddComponent<ScreenIntro>();
            return screen.rectTransform;
        }

        Text Title(RectTransform screen, string text, float y, Color color, int size = 64)
        {
            var title = UIKit.Text(screen, "Title", text, size, TextAnchor.MiddleCenter, color, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1700f, size * 1.4f), new Vector2(0.5f, 0.5f));
            PopIn.Add(title, 0f, 0.4f);
            return title;
        }

        public void ShowUpgrades(List<UpgradeDef> options, Action<UpgradeDef> onPick, Action onReroll = null)
        {
            var screen = NewScreen(new Color(0.03f, 0.02f, 0.08f, 0.75f));
            Title(screen, Loc.T("ВЫБЕРИ УЛУЧШЕНИЕ", "CHOOSE AN UPGRADE"), 340f, UIKit.Accent);
            var sub = UIKit.Text(screen, "Sub", Loc.T("Волна пройдена! Возьми одну награду", "Wave cleared! Take one reward"), 30, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(1400f, 50f), new Vector2(0.5f, 0.5f));

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                float x = (i - (options.Count - 1) / 2f) * 470f;
                var card = UIKit.Button(screen, "Card", "", UIKit.Panel, () => onPick(option));
                var cardRect = card.GetComponent<RectTransform>();
                UIKit.Place(cardRect, new Vector2(0.5f, 0.5f), new Vector2(x, -20f), new Vector2(430f, 440f), new Vector2(0.5f, 0.5f));
                PopIn.Add(card, 0.08f + i * 0.09f, 0.4f);

                var glow = UIKit.Image(cardRect, "Glow", new Color(option.color.r, option.color.g, option.color.b, 0.12f));
                UIKit.Stretch(glow.rectTransform, 6f);
                var stripe = UIKit.Image(cardRect, "Stripe", option.color);
                UIKit.Place(stripe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(400f, 18f), new Vector2(0.5f, 1f));
                var rarity = UIKit.Text(cardRect, "Rarity", Upgrades.RarityName(option.rarity), 20, TextAnchor.UpperCenter, option.color, true);
                UIKit.Place(rarity.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(400f, 28f), new Vector2(0.5f, 1f));

                var iconBack = UIKit.Image(cardRect, "IconBack", new Color(option.color.r * 0.4f, option.color.g * 0.4f, option.color.b * 0.4f, 1f));
                iconBack.sprite = UIKit.Circle;
                UIKit.Place(iconBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(124f, 124f), new Vector2(0.5f, 1f));
                var icon = UIKit.Icon(iconBack.rectTransform, "Icon", option.icon, option.color, new Vector2(0.5f, 0.5f), Vector2.zero, 84f, new Vector2(0.5f, 0.5f));
                if (option.rarity == Rarity.Epic) icon.gameObject.AddComponent<Pulse>().amount = 0.1f;

                var name = UIKit.Text(cardRect, "Name", option.name, 36, TextAnchor.MiddleCenter, option.color, true);
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(400f, 60f), new Vector2(0.5f, 0.5f));
                var desc = UIKit.Text(cardRect, "Desc", option.description, 27, TextAnchor.UpperCenter, Color.white);
                UIKit.Place(desc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -66f), new Vector2(390f, 140f), new Vector2(0.5f, 1f));
                var key = UIKit.Text(cardRect, "Key", "[" + (i + 1) + "]", 24, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.5f));
                UIKit.Place(key.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(200f, 30f), new Vector2(0.5f, 0f));
                card.gameObject.AddComponent<KeyShortcut>().Setup(i + 1, () => onPick(option));
            }

            if (onReroll != null)
            {
                bool can = RunState.rerollsLeft > 0;
                var reroll = UIKit.IconButton(screen, "Reroll", IconKind.Reroll, new LS("Переброс (" + RunState.rerollsLeft + ")", "Reroll (" + RunState.rerollsLeft + ")"),
                    can ? UIKit.Purple : new Color(0.25f, 0.2f, 0.3f, 0.8f), () => { if (RunState.rerollsLeft > 0) onReroll(); else SoundFX.Play(SoundFX.Sfx.Empty); }, 30);
                UIKit.Place(reroll.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(380f, 76f), new Vector2(0.5f, 0f));
                reroll.interactable = can;
                PopIn.Add(reroll, 0.4f);
            }
        }

        public class ShopItem
        {
            public string name;
            public string description;
            public int price;
            public Color color;
            public IconKind icon = IconKind.Star;
            public bool soldOut;
            public Func<bool> buy; // true если покупка удалась
        }

        public void ShowShop(Func<List<ShopItem>> getItems, Action onContinue)
        {
            var screen = NewScreen(new Color(0.03f, 0.02f, 0.08f, 0.85f));
            Title(screen, Loc.T("МАГАЗИН МАМЫ-МИИ", "MAMMA MIA'S SHOP"), 410f, UIKit.Accent);
            var coinsRow = UIKit.Rect(screen, "CoinsRow");
            UIKit.Place(coinsRow, new Vector2(0.5f, 0.5f), new Vector2(0f, 335f), new Vector2(400f, 60f), new Vector2(0.5f, 0.5f));
            UIKit.Icon(coinsRow, "Coin", IconKind.Coin, UIKit.Accent, new Vector2(0.5f, 0.5f), new Vector2(-90f, 0f), 50f, new Vector2(0.5f, 0.5f));
            var coins = UIKit.Text(coinsRow, "Coins", "", 44, TextAnchor.MiddleLeft, UIKit.Accent, true);
            UIKit.Place(coins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-55f, 0f), new Vector2(300f, 60f), new Vector2(0f, 0.5f));

            var grid = UIKit.Rect(screen, "Grid");
            UIKit.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(1500f, 560f), new Vector2(0.5f, 1f));
            bool first = true;

            void Rebuild()
            {
                for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
                coins.text = RunState.coins.ToString();
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
                    if (first) PopIn.Add(button, 0.05f + i * 0.05f, 0.3f);
                    var stripe = UIKit.Image(rect, "Stripe", item.color);
                    UIKit.Place(stripe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(430f, 14f), new Vector2(0.5f, 1f));
                    UIKit.Icon(rect, "Icon", item.icon, item.color, new Vector2(0f, 1f), new Vector2(22f, -36f), 64f, new Vector2(0f, 1f));
                    var n = UIKit.Text(rect, "Name", item.name, 32, TextAnchor.MiddleLeft, item.color, true);
                    UIKit.Place(n.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -40f), new Vector2(340f, 56f), new Vector2(0f, 1f));
                    var d = UIKit.Text(rect, "Desc", item.description, 24, TextAnchor.UpperCenter, Color.white);
                    UIKit.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(420f, 80f), new Vector2(0.5f, 1f));
                    var p = UIKit.Text(rect, "Price", item.soldOut ? Loc.T("КУПЛЕНО", "SOLD") : Loc.Plural(item.price, "монета", "монеты", "монет", "coin", "coins"), 32, TextAnchor.LowerCenter, item.soldOut ? UIKit.Green : (affordable ? UIKit.Accent : UIKit.Pink), true);
                    UIKit.Place(p.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(420f, 44f), new Vector2(0.5f, 0f));
                    button.interactable = !item.soldOut;
                }
                first = false;
            }

            Rebuild();

            var next = UIKit.IconButton(screen, "Next", IconKind.Play, new LS("В БОЙ!", "FIGHT!"), UIKit.PlayColor, onContinue, 40);
            UIKit.Place(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(420f, 90f), new Vector2(0.5f, 0f));
            next.gameObject.AddComponent<Pulse>().amount = 0.03f;
        }

        public void ShowPause(Action onResume, Action onMenu)
        {
            var screen = NewScreen(new Color(0.03f, 0.02f, 0.08f, 0.8f));
            UIKit.Icon(screen, "PauseIcon", IconKind.Pause, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), 80f, new Vector2(0.5f, 0.5f));
            Title(screen, Loc.T("ПАУЗА", "PAUSED"), 260f, Color.white, 90);

            var stats = RunState.stats;
            string info = Loc.T("Герой: ", "Hero: ") + RunState.HeroInfo.name + (RunState.threat > 0 ? Loc.T(" · Угроза ", " · Threat ") + RunState.threat : "") + "\n" +
                          Loc.T("Урон ×", "Damage ×") + stats.damageMultiplier.ToString("0.00") + Loc.T(" · Скорострельность ×", " · Fire rate ×") + stats.fireRateMultiplier.ToString("0.00") + "\n" +
                          Loc.T("Крит ", "Crit ") + Mathf.RoundToInt(stats.critChance * 100f) + Loc.T("% · Скорость ×", "% · Speed ×") + stats.moveSpeedMultiplier.ToString("0.00") + "\n" +
                          Loc.T("Побеждено врагов: ", "Enemies defeated: ") + RunState.kills + Loc.T(" · Лучшее комбо: ", " · Best combo: ") + RunState.bestCombo;
            var infoText = UIKit.Text(screen, "Info", info, 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
            UIKit.Place(infoText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1300f, 160f), new Vector2(0.5f, 0.5f));

            var resume = UIKit.IconButton(screen, "Resume", IconKind.Play, new LS("Продолжить", "Resume"), UIKit.OkColor, onResume, 36);
            UIKit.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(480f, 86f), new Vector2(0.5f, 0.5f));
            SlideIn.Add(resume, new Vector2(0f, -40f), 0.05f, 0.3f);

            var settings = UIKit.IconButton(screen, "Settings", IconKind.Gear, new LS("Настройки", "Settings"), new Color(0.25f, 0.4f, 0.75f, 1f), () => SettingsScreen.Show(overlay, null), 32);
            UIKit.Place(settings.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(480f, 80f), new Vector2(0.5f, 0.5f));
            SlideIn.Add(settings, new Vector2(0f, -40f), 0.1f, 0.3f);

            var menu = UIKit.IconButton(screen, "Menu", IconKind.Exit, new LS("Выйти в меню", "Main menu"), UIKit.Purple, onMenu, 32);
            UIKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -225f), new Vector2(480f, 80f), new Vector2(0.5f, 0.5f));
            SlideIn.Add(menu, new Vector2(0f, -40f), 0.15f, 0.3f);
            var warn = UIKit.Text(screen, "Warn", new LS("Кристаллы за забег всё равно начислятся", "You still get crystals for this run"), 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.5f));
            UIKit.Place(warn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(900f, 34f), new Vector2(0.5f, 0.5f));
        }

        public void ShowEnd(bool victory, Meta.RunReward reward, Action onRetry, Action onMenu)
        {
            var screen = NewScreen(victory ? new Color(0.05f, 0.08f, 0.04f, 0.88f) : new Color(0.12f, 0.02f, 0.05f, 0.9f));
            UIKit.Icon(screen, "Icon", victory ? IconKind.Trophy : IconKind.Skull, victory ? UIKit.Accent : UIKit.Pink, new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), 100f, new Vector2(0.5f, 0.5f)).gameObject.AddComponent<Pulse>();
            Title(screen, victory ? Loc.T("ПОБЕДА! БРЕЙНРОТЫ ПОВЕРЖЕНЫ!", "VICTORY! THE BRAINROTS ARE DEFEATED!") : Loc.T("ТЕБЯ ЗАБРЕЙНРОТИЛИ!", "YOU GOT BRAINROTTED!"), 320f, victory ? UIKit.Accent : UIKit.Pink, 68);

            int minutes = Mathf.FloorToInt(RunState.time / 60f);
            int seconds = Mathf.FloorToInt(RunState.time % 60f);
            string arenaLine = victory
                ? Loc.T("Пройдены все арены: ", "All arenas cleared: ") + RunState.ArenaCount
                : Loc.T("Арена: ", "Arena: ") + (RunState.arenaIndex + 1) + Loc.T(" из ", " of ") + RunState.ArenaCount + " (" + Brainrots.Get(RunState.CurrentArenaOwner).arenaName + ")";
            string text = Loc.T("Герой: ", "Hero: ") + RunState.HeroInfo.name + "\n" + arenaLine + "\n" +
                          Loc.T("Время: ", "Time: ") + minutes + ":" + seconds.ToString("00") +
                          (reward != null && reward.newRecord ? "\n<color=#FFC93C>" + Loc.T("НОВЫЙ РЕКОРД!", "NEW RECORD!") + "</color>" : "");
            var stats = UIKit.Text(screen, "Stats", text, 30, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1400f, 160f), new Vector2(0.5f, 0.5f));

            if (reward != null)
            {
                var panel = UIKit.Image(screen, "Reward", UIKit.Panel);
                UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(900f, 300f), new Vector2(0.5f, 0.5f));
                PopIn.Add(panel, 0.2f, 0.4f);
                var pr = panel.rectTransform;
                string Line(string ru, string en, string value, int crystals) => "<color=#FFFFFFCC>" + Loc.T(ru, en) + " " + value + "</color>   <color=#64FFDA>+" + crystals + "</color>";
                string lines =
                    Line("Волн пройдено:", "Waves cleared:", reward.waves.ToString(), reward.fromWaves) + "\n" +
                    Line("Боссов побеждено:", "Bosses defeated:", reward.bosses.ToString(), reward.fromBosses) + "\n" +
                    Line("Врагов побеждено:", "Enemies defeated:", reward.kills.ToString(), reward.fromKills) + "\n" +
                    Line("Монет собрано:", "Coins collected:", reward.coins.ToString(), reward.fromCoins) + "\n" +
                    Line("Лучшее комбо:", "Best combo:", reward.combo.ToString(), reward.fromCombo) +
                    (reward.fromVictory > 0 ? "\n" + Line("Победа!", "Victory!", "", reward.fromVictory) : "") +
                    (reward.multiplier > 1.001f ? "\n<color=#FFC93C>" + Loc.T("Множитель ×", "Multiplier ×") + reward.multiplier.ToString("0.00") + "</color>" : "");
                var l = UIKit.Text(pr, "Lines", lines, 25, TextAnchor.UpperLeft, Color.white);
                UIKit.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(560f, 270f), new Vector2(0f, 1f));

                UIKit.Icon(pr, "Crystal", IconKind.Crystal, UIKit.Crystal, new Vector2(1f, 0.5f), new Vector2(-200f, 30f), 110f, new Vector2(0.5f, 0.5f)).gameObject.AddComponent<Pulse>().amount = 0.08f;
                var total = UIKit.Text(pr, "Total", "+0", 56, TextAnchor.MiddleCenter, UIKit.Crystal, true);
                UIKit.Place(total.rectTransform, new Vector2(1f, 0.5f), new Vector2(-200f, -60f), new Vector2(300f, 70f), new Vector2(0.5f, 0.5f));
                total.gameObject.AddComponent<CountUp>().Setup(total, reward.total, "+", 1.2f, 0.5f);
                var have = UIKit.Text(pr, "Have", Loc.T("Всего кристаллов: ", "Total crystals: ") + Meta.P.crystals, 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
                UIKit.Place(have.rectTransform, new Vector2(1f, 0f), new Vector2(-200f, 20f), new Vector2(340f, 30f), new Vector2(0.5f, 0f));

                if (reward.threatUnlocked)
                {
                    var unlock = UIKit.Text(screen, "Threat", Loc.T("Открыт новый уровень угрозы: ", "New threat level unlocked: ") + Meta.P.threatUnlocked + "!", 30, TextAnchor.MiddleCenter, UIKit.Pink, true);
                    UIKit.Place(unlock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(1400f, 44f), new Vector2(0.5f, 0.5f));
                    unlock.gameObject.AddComponent<Pulse>();
                }
            }

            var retry = UIKit.IconButton(screen, "Retry", IconKind.Reload, new LS("Ещё раз", "Play again"), UIKit.OkColor, onRetry, 36);
            UIKit.Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-250f, -310f), new Vector2(440f, 86f), new Vector2(0.5f, 0.5f));
            PopIn.Add(retry, 0.6f);
            var menu = UIKit.IconButton(screen, "Menu", IconKind.Flask, new LS("Меню и Лаборатория", "Menu & Lab"), UIKit.Purple, onMenu, 32);
            UIKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(250f, -310f), new Vector2(440f, 86f), new Vector2(0.5f, 0.5f));
            PopIn.Add(menu, 0.7f);
        }
    }

    /// <summary>Число плавно «набегает» (награда в конце забега).</summary>
    public class CountUp : MonoBehaviour
    {
        Text text;
        int target;
        string prefix;
        float duration, delay, t;
        int last = -1;

        public void Setup(Text label, int value, string pre, float seconds, float wait)
        {
            text = label;
            target = value;
            prefix = pre;
            duration = seconds;
            delay = wait;
        }

        void Update()
        {
            if (delay > 0f) { delay -= Time.unscaledDeltaTime; return; }
            t += Time.unscaledDeltaTime / duration;
            int v = Mathf.RoundToInt(target * Ease.OutCubic(t));
            if (v != last)
            {
                last = v;
                text.text = prefix + v;
                if (v > 0 && v % 3 == 0) SoundFX.Play(SoundFX.Sfx.Coin, 0.25f, 0.1f);
            }
            if (t >= 1f)
            {
                text.text = prefix + target;
                text.gameObject.AddComponent<Pulse>().amount = 0.06f;
                enabled = false;
            }
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
