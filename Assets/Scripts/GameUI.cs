using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>
    /// Интерфейс: прицел, здоровье, номер волны, враги, монеты, экран поражения.
    /// Весь интерфейс создаётся кодом при запуске игры.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public Health playerHealth;
        public WaveManager waves;
        public Weapon weapon;

        const string BestWaveKey = "BestWave";

        Font font;
        Text crosshair, healthText, waveText, infoText, centerText, gameOverText;
        Image healthFill, damageFlash;
        GameObject gameOverPanel;
        float hitMarkerTimer, centerTimer, damageTimer;
        float lastHealth;
        bool gameOver;

        void Start()
        {
            Time.timeScale = 1f;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();

            lastHealth = playerHealth.Current;
            playerHealth.Changed += OnPlayerHealthChanged;
            playerHealth.Died += _ => ShowGameOver();
            waves.WaveStarted += wave => ShowCenter("ВОЛНА " + wave + "!", 2f);
            waves.WaveCleared += wave => ShowCenter("Волна " + wave + " пройдена!", 2f);
            if (weapon != null) weapon.Hit += () => hitMarkerTimer = 0.12f;
        }

        void OnPlayerHealthChanged()
        {
            if (playerHealth.Current < lastHealth) damageTimer = 0.3f;
            lastHealth = playerHealth.Current;
        }

        void Update()
        {
            // Здоровье
            float hp01 = playerHealth.maxHealth > 0f ? playerHealth.Current / playerHealth.maxHealth : 0f;
            healthFill.rectTransform.anchorMax = new Vector2(hp01, 1f);
            healthFill.color = Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(0.3f, 1f, 0.45f), hp01);
            healthText.text = Mathf.CeilToInt(playerHealth.Current) + " / " + Mathf.CeilToInt(playerHealth.maxHealth);

            // Волна и счёт
            waveText.text = waves.Wave > 0 ? "Волна " + waves.Wave : "Приготовься!";
            infoText.text = "Врагов: " + waves.Remaining + "\nУбито: " + waves.Kills + "\nМонеты: " + waves.Coins;

            if (waves.InBreak && !gameOver)
            {
                centerText.text = "Следующая волна через " + Mathf.CeilToInt(waves.BreakTimeLeft);
                centerText.enabled = true;
            }
            else if (centerTimer > 0f)
            {
                centerTimer -= Time.deltaTime;
                centerText.enabled = centerTimer > 0f;
            }
            else centerText.enabled = false;

            // Прицел краснеет при попадании
            hitMarkerTimer -= Time.deltaTime;
            crosshair.color = hitMarkerTimer > 0f ? new Color(1f, 0.2f, 0.2f) : Color.white;
            crosshair.fontSize = hitMarkerTimer > 0f ? 40 : 32;

            // Красная вспышка при получении урона
            damageTimer -= Time.unscaledDeltaTime;
            damageFlash.color = new Color(1f, 0f, 0f, Mathf.Clamp01(damageTimer) * 0.6f);

            if (gameOver)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }

        void ShowCenter(string message, float seconds)
        {
            centerText.text = message;
            centerTimer = seconds;
        }

        void ShowGameOver()
        {
            gameOver = true;
            int best = PlayerPrefs.GetInt(BestWaveKey, 0);
            bool record = waves.Wave > best;
            if (record)
            {
                best = waves.Wave;
                PlayerPrefs.SetInt(BestWaveKey, best);
                PlayerPrefs.Save();
            }

            gameOverText.text = "ТЕБЯ ЗАБРЕЙНРОТИЛИ!\n\n" +
                                "Волна: " + waves.Wave + (record ? "  (новый рекорд!)" : "") + "\n" +
                                "Рекорд: " + best + "\n" +
                                "Убито врагов: " + waves.Kills + "\n\n" +
                                "Нажми R, чтобы сыграть снова";
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
            PlayerController.LockCursor(false);
        }

        // ---------- Создание интерфейса ----------

        void BuildUI()
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform root = canvasObject.GetComponent<RectTransform>();

            damageFlash = CreateImage(root, "DamageFlash", new Color(1f, 0f, 0f, 0f));
            Stretch(damageFlash.rectTransform);
            damageFlash.raycastTarget = false;

            crosshair = CreateText(root, "Crosshair", "+", 32, TextAnchor.MiddleCenter);
            Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));

            // Полоска здоровья слева снизу
            var barBack = CreateImage(root, "HealthBar", new Color(0f, 0f, 0f, 0.5f));
            Place(barBack.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(420f, 40f), new Vector2(0f, 0f));
            healthFill = CreateImage(barBack.rectTransform, "Fill", Color.green);
            Stretch(healthFill.rectTransform);
            healthText = CreateText(barBack.rectTransform, "HealthText", "", 26, TextAnchor.MiddleCenter);
            Stretch(healthText.rectTransform);

            waveText = CreateText(root, "WaveText", "", 48, TextAnchor.UpperCenter);
            Place(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 70f), new Vector2(0.5f, 1f));

            infoText = CreateText(root, "InfoText", "", 28, TextAnchor.UpperRight);
            Place(infoText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 140f), new Vector2(1f, 1f));

            centerText = CreateText(root, "CenterText", "", 56, TextAnchor.MiddleCenter);
            Place(centerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1400f, 100f));
            centerText.enabled = false;

            var weaponText = CreateText(root, "WeaponText", weapon != null ? weapon.weaponName : "", 30, TextAnchor.LowerRight);
            Place(weaponText.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(400f, 50f), new Vector2(1f, 0f));

            var help = CreateText(root, "HelpText", "WASD — ходить, Shift — бег, Пробел — прыжок, ЛКМ — стрелять, Esc — курсор", 20, TextAnchor.LowerCenter);
            Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1200f, 30f), new Vector2(0.5f, 0f));
            help.color = new Color(1f, 1f, 1f, 0.6f);

            // Экран поражения
            var panel = CreateImage(root, "GameOver", new Color(0.1f, 0f, 0.15f, 0.85f));
            Stretch(panel.rectTransform);
            gameOverPanel = panel.gameObject;
            gameOverText = CreateText(panel.rectTransform, "GameOverText", "", 48, TextAnchor.MiddleCenter);
            Stretch(gameOverText.rectTransform);
            gameOverPanel.SetActive(false);
        }

        Text CreateText(RectTransform parent, string name, string value, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Place(rect, anchor, position, size, new Vector2(0.5f, 0.5f));
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
