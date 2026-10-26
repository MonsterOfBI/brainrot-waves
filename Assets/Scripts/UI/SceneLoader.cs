using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>
    /// Экран загрузки между меню и боем: прогресс, совет, название арены.
    /// SceneLoader.Load("Game") вместо SceneManager.LoadScene.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        static SceneLoader instance;
        public static bool Busy => instance != null;

        static readonly LS[] Tips =
        {
            new LS("Монеты сами летят к тебе после каждой волны.", "Coins fly to you after every wave."),
            new LS("Не стой на месте — метатели бананов любят неподвижные цели.", "Keep moving — banana throwers love standing targets."),
            new LS("Кристаллы мозга даются даже за проигранный забег. Трать их в Лаборатории!", "You earn brain crystals even when you lose. Spend them in the Lab!"),
            new LS("Серия быстрых побед (комбо) приносит бонусные монеты.", "Quick kill streaks (combos) give bonus coins."),
            new LS("Способность можно использовать чаще с улучшением «Двойной эспрессо».", "Use your ability more often with the Double espresso upgrade."),
            new LS("Победа открывает новый уровень угрозы: враги злее, награда больше.", "Winning unlocks a new threat level: tougher enemies, bigger rewards."),
            new LS("Дробовик можно прервать во время перезарядки выстрелом.", "You can interrupt the shotgun reload by firing."),
            new LS("Красные стрелки у прицела показывают, откуда прилетел урон.", "Red arrows near the crosshair show where damage came from."),
            new LS("Перед боссом лучше пополнить здоровье в магазине.", "Top up your health in the shop before a boss."),
            new LS("Клавиши можно переназначить в настройках.", "You can rebind keys in the settings."),
        };

        CanvasGroup group;
        Image bar;
        Text percent;

        public static void Load(string scene)
        {
            if (instance != null) return;
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
            instance.StartCoroutine(instance.Run(scene));
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Build(string scene)
        {
            var root = UIKit.Canvas("LoaderCanvas", 1000);
            root.SetParent(transform, false);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;

            var bg = UIKit.Image(root, "Background", GameAssets.Hex("#160C2A"), false);
            UIKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;

            // Цветные полосы на фоне
            Color[] colors = { UIKit.Accent, UIKit.Pink, UIKit.Cyan, UIKit.Green };
            for (int i = 0; i < 4; i++)
            {
                var stripe = UIKit.Image(root, "Stripe", new Color(colors[i].r, colors[i].g, colors[i].b, 0.08f), false);
                UIKit.Place(stripe.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-600f + i * 400f, 0f), new Vector2(160f, 2600f), new Vector2(0.5f, 0.5f));
                stripe.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            }

            var brain = UIKit.Icon(root, "Brain", IconKind.Brain, UIKit.Pink, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), 180f, new Vector2(0.5f, 0.5f));
            brain.gameObject.AddComponent<Pulse>().amount = 0.08f;

            string title = Loc.T("ЗАГРУЗКА", "LOADING");
            string subtitle = "";
            if (scene == "Game" && RunState.started && RunState.ArenaCount > 0)
            {
                var owner = Brainrots.Get(RunState.CurrentArenaOwner);
                title = Loc.T("АРЕНА ", "ARENA ") + (RunState.arenaIndex + 1) + " / " + RunState.ArenaCount;
                subtitle = owner.arenaName;
                if (RunState.threat > 0) subtitle += " · " + Loc.T("Угроза ", "Threat ") + RunState.threat;
            }
            else if (scene == "MainMenu") title = Loc.T("ГЛАВНОЕ МЕНЮ", "MAIN MENU");

            var t = UIKit.Text(root, "Title", title, 72, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1600f, 90f), new Vector2(0.5f, 0.5f));
            var s = UIKit.Text(root, "Subtitle", subtitle, 34, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(s.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(1600f, 50f), new Vector2(0.5f, 0.5f));

            var barBack = UIKit.Image(root, "BarBack", new Color(0f, 0f, 0f, 0.55f));
            UIKit.Place(barBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(900f, 30f), new Vector2(0.5f, 0.5f));
            bar = UIKit.Image(barBack.rectTransform, "Fill", UIKit.Pink);
            UIKit.Stretch(bar.rectTransform, 4f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            percent = UIKit.Text(root, "Percent", "0%", 26, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            UIKit.Place(percent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -205f), new Vector2(400f, 40f), new Vector2(0.5f, 0.5f));

            var tipIcon = UIKit.Icon(root, "TipIcon", IconKind.Star, UIKit.Accent, new Vector2(0.5f, 0f), new Vector2(0f, 190f), 44f, new Vector2(0.5f, 0.5f));
            tipIcon.gameObject.AddComponent<Spin>().speed = -60f;
            var tip = UIKit.Text(root, "Tip", Loc.T("Совет: ", "Tip: ") + Tips[Random.Range(0, Tips.Length)], 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
            UIKit.Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1500f, 90f), new Vector2(0.5f, 0.5f));
        }

        void SetProgress(float p)
        {
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
            percent.text = Mathf.RoundToInt(Mathf.Clamp01(p) * 100f) + "%";
        }

        IEnumerator Run(string scene)
        {
            Build(scene);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.25f;
                group.alpha = t;
                yield return null;
            }
            group.alpha = 1f;
            Time.timeScale = 1f;

            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            float shown = 0f;
            float minTime = 0.7f;
            while (op.progress < 0.9f || shown < 0.85f || minTime > 0f)
            {
                minTime -= Time.unscaledDeltaTime;
                shown = Mathf.MoveTowards(shown, Mathf.Min(op.progress / 0.9f, 1f) * 0.85f, Time.unscaledDeltaTime * 1.6f);
                SetProgress(shown);
                yield return null;
            }
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            // Даём сцене построить арену за экраном загрузки
            for (int i = 0; i < 3; i++)
            {
                SetProgress(0.85f + 0.05f * i);
                yield return null;
            }
            SetProgress(1f);
            yield return new WaitForSecondsRealtime(0.15f);

            t = 1f;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime / 0.35f;
                group.alpha = t;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
