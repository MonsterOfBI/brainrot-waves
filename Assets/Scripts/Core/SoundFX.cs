using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Звуки и музыка, сгенерированные кодом (без файлов). Позже их легко заменить настоящими звуками.
    /// </summary>
    public class SoundFX : MonoBehaviour
    {
        public enum Sfx
        {
            Pistol, Rifle, Shotgun, Reload, Hit, Crit, EnemyDie, Explosion, Coin, Heal,
            Hurt, Upgrade, Click, Ability, BossRoar, Throw, Dash, Jump, WaveStart, Victory, Defeat, Empty
        }

        const int Rate = 22050;
        static SoundFX instance;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource music;
        int nextSource;
        readonly Dictionary<int, AudioClip> musicCache = new Dictionary<int, AudioClip>();

        public static float MasterVolume = 0.8f;
        public static float MusicVolume = 0.35f;

        public static SoundFX I
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SoundFX");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<SoundFX>();
                    instance.Init();
                }
                return instance;
            }
        }

        void Init()
        {
            for (int i = 0; i < 24; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool.Add(s);
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = MusicVolume;
            music.ignoreListenerPause = true;

            clips[Sfx.Pistol] = Gun(0.12f, 900f, 0.9f);
            clips[Sfx.Rifle] = Gun(0.08f, 1300f, 0.7f);
            clips[Sfx.Shotgun] = Gun(0.28f, 450f, 1f);
            clips[Sfx.Reload] = Clicks(new[] { 0f, 0.12f }, 1800f);
            clips[Sfx.Empty] = Clicks(new[] { 0f }, 2500f);
            clips[Sfx.Hit] = Tone(0.05f, 1400f, 900f, Wave.Square, 0.35f);
            clips[Sfx.Crit] = Tone(0.09f, 2000f, 2600f, Wave.Square, 0.4f);
            clips[Sfx.EnemyDie] = Tone(0.25f, 700f, 120f, Wave.Square, 0.5f, 0.3f);
            clips[Sfx.Explosion] = Noise(0.7f, 1f, 0.12f);
            clips[Sfx.Coin] = Arp(new[] { 1318f, 1975f }, 0.06f, Wave.Square, 0.3f);
            clips[Sfx.Heal] = Arp(new[] { 523f, 659f, 784f, 1046f }, 0.06f, Wave.Sine, 0.5f);
            clips[Sfx.Hurt] = Tone(0.2f, 220f, 80f, Wave.Saw, 0.6f, 0.4f);
            clips[Sfx.Upgrade] = Arp(new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.07f, Wave.Square, 0.35f);
            clips[Sfx.Click] = Tone(0.04f, 900f, 900f, Wave.Square, 0.25f);
            clips[Sfx.Ability] = Tone(0.4f, 200f, 900f, Wave.Saw, 0.45f, 0.2f);
            clips[Sfx.BossRoar] = Tone(1.4f, 140f, 60f, Wave.Saw, 0.7f, 0.5f);
            clips[Sfx.Throw] = Tone(0.15f, 500f, 1200f, Wave.Sine, 0.3f, 0.3f);
            clips[Sfx.Dash] = Noise(0.25f, 0.5f, 0.6f);
            clips[Sfx.Jump] = Tone(0.12f, 300f, 600f, Wave.Square, 0.25f);
            clips[Sfx.WaveStart] = Arp(new[] { 392f, 392f, 587f }, 0.12f, Wave.Square, 0.4f);
            clips[Sfx.Victory] = Arp(new[] { 523f, 659f, 784f, 1046f, 784f, 1046f, 1318f }, 0.13f, Wave.Square, 0.4f);
            clips[Sfx.Defeat] = Arp(new[] { 392f, 349f, 311f, 262f }, 0.22f, Wave.Saw, 0.4f);
        }

        public static void Play(Sfx sfx, float volume = 1f, float pitchVariation = 0.05f)
        {
            var self = I;
            if (!self.clips.TryGetValue(sfx, out var clip)) return;
            var source = self.pool[self.nextSource];
            self.nextSource = (self.nextSource + 1) % self.pool.Count;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, volume * MasterVolume);
        }

        /// <summary>Включает музыку. Разные «стили» для меню, каждой арены и боссов.</summary>
        public static void PlayMusic(int style)
        {
            var self = I;
            if (!self.musicCache.TryGetValue(style, out var clip))
            {
                clip = self.GenerateMusic(style);
                self.musicCache[style] = clip;
            }
            if (self.music.clip == clip && self.music.isPlaying) return;
            self.music.clip = clip;
            self.music.volume = MusicVolume;
            self.music.Play();
        }

        public static void StopMusic()
        {
            if (instance != null) instance.music.Stop();
        }

        // ---------------- Генерация ----------------

        enum Wave { Sine, Square, Saw, Triangle }

        static float Osc(Wave wave, float phase)
        {
            phase -= Mathf.Floor(phase);
            switch (wave)
            {
                case Wave.Square: return phase < 0.5f ? 1f : -1f;
                case Wave.Saw: return phase * 2f - 1f;
                case Wave.Triangle: return phase < 0.5f ? phase * 4f - 1f : 3f - phase * 4f;
                default: return Mathf.Sin(phase * Mathf.PI * 2f);
            }
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Tone(float length, float f0, float f1, Wave wave, float vol, float noise = 0f)
        {
            int n = (int)(length * Rate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                float env = Mathf.Min(1f, i / (Rate * 0.004f)) * (1f - t) * (1f - t);
                float s = Osc(wave, phase) * (1f - noise) + (Random.value * 2f - 1f) * noise;
                d[i] = s * env * vol;
            }
            return Make("tone", d);
        }

        static AudioClip Noise(float length, float vol, float lowpass)
        {
            int n = (int)(length * Rate);
            var d = new float[n];
            float y = 0f;
            float boom = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                y += (Random.value * 2f - 1f - y) * lowpass;
                boom += 60f / Rate;
                float low = Mathf.Sin(boom * Mathf.PI * 2f * (1f - t * 0.5f)) * 0.6f;
                float env = Mathf.Pow(1f - t, 2.2f);
                d[i] = Mathf.Clamp((y * 3f + low * (lowpass < 0.3f ? 1f : 0f)) * env * vol, -1f, 1f);
            }
            return Make("noise", d);
        }

        static AudioClip Gun(float length, float tone, float vol)
        {
            int n = (int)(length * Rate);
            var d = new float[n];
            float y = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                y += (Random.value * 2f - 1f - y) * 0.5f;
                phase += Mathf.Lerp(tone, tone * 0.3f, t) / Rate;
                float env = Mathf.Pow(1f - t, 3f);
                d[i] = Mathf.Clamp((y * 0.8f + Osc(Wave.Square, phase) * 0.35f) * env * vol, -1f, 1f);
            }
            return Make("gun", d);
        }

        static AudioClip Clicks(float[] times, float tone)
        {
            float length = times[times.Length - 1] + 0.06f;
            int n = (int)(length * Rate);
            var d = new float[n];
            foreach (float start in times)
            {
                int s = (int)(start * Rate);
                int len = (int)(0.05f * Rate);
                for (int i = 0; i < len && s + i < n; i++)
                {
                    float t = (float)i / len;
                    d[s + i] += Osc(Wave.Square, i * tone / Rate) * (1f - t) * 0.3f;
                }
            }
            return Make("click", d);
        }

        static AudioClip Arp(float[] notes, float noteLength, Wave wave, float vol)
        {
            int per = (int)(noteLength * Rate);
            int n = per * notes.Length + (int)(0.15f * Rate);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                float phase = 0f;
                int len = per + (int)(0.15f * Rate);
                for (int i = 0; i < len && k * per + i < n; i++)
                {
                    phase += notes[k] / Rate;
                    float t = (float)i / len;
                    float env = Mathf.Min(1f, i / (Rate * 0.003f)) * Mathf.Pow(1f - t, 2f);
                    d[k * per + i] += Osc(wave, phase) * env * vol * 0.6f;
                }
            }
            return Make("arp", d);
        }

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        /// <summary>Генерирует зацикленную мелодию в стиле «итальянской» чиптюн-музыки.</summary>
        AudioClip GenerateMusic(int style)
        {
            var rng = new System.Random(1234 + style * 97);
            bool boss = style >= 100;
            int baseStyle = style % 100;
            float bpm = boss ? 150f : 112f + baseStyle * 6f;
            int root = 45 + (baseStyle * 5) % 12; // тональность
            bool minor = boss || baseStyle == 5 || baseStyle == 3;
            int[] scale = minor ? new[] { 0, 2, 3, 5, 7, 8, 10 } : new[] { 0, 2, 4, 5, 7, 9, 11 };
            int[][] progressions =
            {
                new[] { 0, 5, 3, 4 }, new[] { 0, 3, 4, 4 }, new[] { 0, 5, 1, 4 }, new[] { 0, 4, 5, 3 }
            };
            int[] prog = progressions[(baseStyle + (boss ? 2 : 0)) % progressions.Length];

            int bars = 8;
            float beat = 60f / bpm;
            float barLength = beat * 4f;
            int n = (int)(bars * barLength * Rate);
            var d = new float[n];

            // Мелодия: случайная, но повторяется каждые 2 такта
            int steps = 8;
            var melody = new int[steps * 2];
            for (int i = 0; i < melody.Length; i++) melody[i] = rng.Next(0, 10) < 2 ? -1 : rng.Next(0, 8);

            for (int bar = 0; bar < bars; bar++)
            {
                int chordDegree = prog[bar % prog.Length];
                int chordRoot = root + scale[chordDegree % 7];

                // Бас: восьмые
                for (int e = 0; e < 8; e++)
                {
                    float start = bar * barLength + e * beat * 0.5f;
                    int note = chordRoot - 12 + (e % 2 == 1 ? 12 : 0);
                    AddNote(d, start, beat * 0.45f, Midi(note), Wave.Triangle, 0.22f);
                }

                // Арпеджио
                for (int s = 0; s < 16; s++)
                {
                    float start = bar * barLength + s * beat * 0.25f;
                    int[] chord = { 0, 2, 4, 7 };
                    int deg = chordDegree + chord[s % 4] % 7;
                    int note = root + 12 + scale[deg % 7] + (deg >= 7 ? 12 : 0) + (chord[s % 4] == 7 ? 12 : 0);
                    AddNote(d, start, beat * 0.2f, Midi(note), Wave.Square, 0.045f);
                }

                // Мелодия
                for (int s = 0; s < steps; s++)
                {
                    int m = melody[(bar % 2) * steps + s];
                    if (m < 0) continue;
                    float start = bar * barLength + s * beat * 0.5f;
                    int note = root + 24 + scale[(chordDegree + m) % 7] + ((chordDegree + m) >= 7 ? 12 : 0);
                    AddNote(d, start, beat * 0.42f, Midi(note), Wave.Square, 0.07f);
                }

                // Ударные
                for (int b = 0; b < 8; b++)
                {
                    float start = bar * barLength + b * beat * 0.5f;
                    if (b % 4 == 0 || (boss && b % 2 == 0)) AddKick(d, start);
                    if (b % 4 == 2) AddSnare(d, start, rng);
                    AddHat(d, start, rng);
                }
            }

            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
            return Make("music" + style, d);
        }

        static void AddNote(float[] d, float start, float length, float freq, Wave wave, float vol)
        {
            int s = (int)(start * Rate);
            int len = (int)(length * Rate);
            float phase = 0f;
            for (int i = 0; i < len && s + i < d.Length; i++)
            {
                phase += freq / Rate;
                float t = (float)i / len;
                float env = Mathf.Min(1f, i / (Rate * 0.005f)) * (1f - t * 0.7f);
                if (t > 0.9f) env *= (1f - t) * 10f;
                d[s + i] += Osc(wave, phase) * env * vol;
            }
        }

        static void AddKick(float[] d, float start)
        {
            int s = (int)(start * Rate);
            int len = (int)(0.15f * Rate);
            float phase = 0f;
            for (int i = 0; i < len && s + i < d.Length; i++)
            {
                float t = (float)i / len;
                phase += Mathf.Lerp(150f, 45f, t) / Rate;
                d[s + i] += Mathf.Sin(phase * Mathf.PI * 2f) * (1f - t) * 0.35f;
            }
        }

        static void AddSnare(float[] d, float start, System.Random rng)
        {
            int s = (int)(start * Rate);
            int len = (int)(0.12f * Rate);
            for (int i = 0; i < len && s + i < d.Length; i++)
            {
                float t = (float)i / len;
                d[s + i] += ((float)rng.NextDouble() * 2f - 1f) * (1f - t) * (1f - t) * 0.15f;
            }
        }

        static void AddHat(float[] d, float start, System.Random rng)
        {
            int s = (int)(start * Rate);
            int len = (int)(0.03f * Rate);
            float prev = 0f;
            for (int i = 0; i < len && s + i < d.Length; i++)
            {
                float t = (float)i / len;
                float x = (float)rng.NextDouble() * 2f - 1f;
                d[s + i] += (x - prev) * (1f - t) * 0.04f;
                prev = x;
            }
        }
    }
}
