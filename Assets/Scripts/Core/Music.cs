using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Музыкальный режиссёр (как в Hades и Minecraft): музыка не играет одна и та же без остановки.
    /// На арене чередуются: боевая тема арены → спокойная → тишина (только звуки природы) → другая боевая.
    /// У босса своя музыка, отличная от арен. Переходы — плавные (crossfade).
    /// Файлы: Resources/Music/menu, arena_[Тема], combat_N, calm_N, boss_N.
    /// </summary>
    public class Music : MonoBehaviour
    {
        public enum Mood { Menu, Combat, Calm, Silence, Boss }

        static Music instance;
        AudioSource a, b;
        AudioSource current;
        float fade = 1.5f;
        float duck = 1f, duckTarget = 1f;
        AudioClip wantClip;
        float playTime;
        int waveCounter;
        int combatIndex, calmIndex, bossIndex;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly List<AudioClip> combatTracks = new List<AudioClip>();
        static readonly List<AudioClip> calmTracks = new List<AudioClip>();
        static readonly List<AudioClip> bossTracks = new List<AudioClip>();

        public static Mood CurrentMood { get; private set; } = Mood.Silence;

        static Music I
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("Music");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Music>();
                    instance.Init();
                }
                return instance;
            }
        }

        void Init()
        {
            a = gameObject.AddComponent<AudioSource>();
            b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { a, b })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.ignoreListenerPause = true;
                s.volume = 0f;
            }
            current = a;
            LoadList("combat_", combatTracks);
            LoadList("calm_", calmTracks);
            LoadList("boss_", bossTracks);
            combatIndex = Random.Range(0, 100);
            calmIndex = Random.Range(0, 100);
            bossIndex = Random.Range(0, 100);
        }

        static void LoadList(string prefix, List<AudioClip> list)
        {
            list.Clear();
            for (int i = 1; i <= 12; i++)
            {
                var c = Resources.Load<AudioClip>("Music/" + prefix + i);
                if (c != null) list.Add(c);
            }
        }

        AudioClip Load(string name)
        {
            if (cache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Music/" + name);
            cache[name] = c;
            return c;
        }

        static AudioClip Pick(List<AudioClip> list, ref int index)
        {
            if (list.Count == 0) return null;
            index++;
            return list[index % list.Count];
        }

        // ---------- Публичное ----------

        public static void Menu()
        {
            var m = I;
            m.waveCounter = 0;
            m.Play(m.Load("menu"), Mood.Menu, 2f);
        }

        /// <summary>Новая арена: сбрасываем цикл, первая волна — тема арены.</summary>
        public static void NewArena(ArenaTheme theme)
        {
            I.waveCounter = 0;
            I.arena = theme;
        }

        ArenaTheme arena;

        /// <summary>Начало волны. Чередование: тема арены, тема арены, спокойная, тишина, другая боевая, ...</summary>
        public static void WaveStart()
        {
            var m = I;
            int step = m.waveCounter % 5;
            m.waveCounter++;
            switch (step)
            {
                case 0:
                case 1:
                    m.Play(m.Load("arena_" + m.arena) ?? Pick(combatTracks, ref m.combatIndex), Mood.Combat, 1.5f);
                    break;
                case 2:
                    m.Play(Pick(calmTracks, ref m.calmIndex), Mood.Calm, 2.5f);
                    break;
                case 3:
                    m.Play(null, Mood.Silence, 3f);
                    break;
                default:
                    m.Play(Pick(combatTracks, ref m.combatIndex), Mood.Combat, 1.5f);
                    break;
            }
        }

        /// <summary>Перерыв между волнами: если музыка играет больше 4 минут подряд — даём ушам отдохнуть.</summary>
        public static void Break()
        {
            var m = I;
            if (m.playTime > 240f && CurrentMood != Mood.Silence) m.Play(null, Mood.Silence, 3f);
        }

        public static void Boss()
        {
            var m = I;
            m.Play(Pick(bossTracks, ref m.bossIndex) ?? m.Load("arena_" + m.arena), Mood.Boss, 1f);
        }

        /// <summary>Спокойная музыка (магазин, после победы над боссом).</summary>
        public static void Calm()
        {
            var m = I;
            m.Play(Pick(calmTracks, ref m.calmIndex), Mood.Calm, 2f);
        }

        public static void Stop(float fadeTime = 1f) => I.Play(null, Mood.Silence, fadeTime);

        /// <summary>Приглушить музыку (открыт экран выбора, пауза).</summary>
        public static void Duck(bool on) => I.duckTarget = on ? 0.45f : 1f;

        // ---------- Внутреннее ----------

        void Play(AudioClip clip, Mood mood, float fadeTime)
        {
            CurrentMood = mood;
            fade = Mathf.Max(0.1f, fadeTime);
            if (clip != null && current.clip == clip && current.isPlaying) return;
            wantClip = clip;
            var next = current == a ? b : a;
            if (clip != null)
            {
                next.clip = clip;
                next.volume = 0f;
                next.time = 0f;
                next.Play();
                playTime = 0f;
            }
            else next.Stop();
            current = next;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            duck = Mathf.MoveTowards(duck, duckTarget, dt * 1.5f);
            float target = SoundFX.MusicVolume * duck;
            var other = current == a ? b : a;
            current.volume = current.isPlaying ? Mathf.MoveTowards(current.volume, target, dt / fade * Mathf.Max(target, 0.05f)) : 0f;
            other.volume = Mathf.MoveTowards(other.volume, 0f, dt / fade * Mathf.Max(target, 0.05f));
            if (other.volume <= 0.001f && other.isPlaying) other.Stop();
            if (current.isPlaying) playTime += dt;
        }
    }
}
