using System;
using UnityEngine;

namespace BrainrotWaves
{
    public enum Lang { RU, EN }

    /// <summary>
    /// Перевод игры на русский и английский. Каждый текст хранится сразу на двух языках:
    /// Loc.T("Привет", "Hello") вернёт строку на текущем языке.
    /// </summary>
    public static class Loc
    {
        const string Key = "BW_Lang";
        static bool loaded;
        static Lang current;

        /// <summary>Вызывается при смене языка (чтобы интерфейс перерисовался).</summary>
        public static event Action Changed;

        public static Lang Current
        {
            get
            {
                if (!loaded) Load();
                return current;
            }
        }

        public static bool RU => Current == Lang.RU;

        static void Load()
        {
            loaded = true;
            if (PlayerPrefs.HasKey(Key))
            {
                current = (Lang)Mathf.Clamp(PlayerPrefs.GetInt(Key), 0, 1);
                return;
            }
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                case SystemLanguage.Ukrainian:
                case SystemLanguage.Belarusian:
                    current = Lang.RU;
                    break;
                default:
                    current = Lang.EN;
                    break;
            }
        }

        public static void Set(Lang lang)
        {
            loaded = true;
            if (current == lang) return;
            current = lang;
            PlayerPrefs.SetInt(Key, (int)lang);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Toggle() => Set(Current == Lang.RU ? Lang.EN : Lang.RU);

        public static string T(string ru, string en) => Current == Lang.RU ? ru : en;

        /// <summary>Число с правильным окончанием: 1 монета, 2 монеты, 5 монет / 1 coin, 5 coins.</summary>
        public static string Plural(int n, string ru1, string ru2, string ru5, string en1, string enMany)
        {
            if (Current == Lang.EN) return n + " " + (n == 1 ? en1 : enMany);
            int a = Mathf.Abs(n) % 100;
            int b = a % 10;
            string word = a > 10 && a < 20 ? ru5 : b == 1 ? ru1 : b >= 2 && b <= 4 ? ru2 : ru5;
            return n + " " + word;
        }
    }

    /// <summary>Строка сразу на двух языках. Сама превращается в текст на текущем языке.</summary>
    [Serializable]
    public struct LS
    {
        public string ru;
        public string en;

        public LS(string ru, string en)
        {
            this.ru = ru;
            this.en = en;
        }

        public string Value => Loc.Current == Lang.RU ? ru : en;
        public override string ToString() => Value;
        public string ToUpper() => Value.ToUpperInvariant();
        public static implicit operator string(LS s) => s.Value;
    }
}
