using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Клиент Polza.ai (OpenAI-совместимый API) — только для редактора, в сборку игры не попадает.
    /// Ключ берётся из переменной окружения POLZA_API_KEY или из файла UserSettings/PolzaAI.key
    /// (папка UserSettings в .gitignore, ключ не уходит в репозиторий).
    /// </summary>
    public static class PolzaAI
    {
        public const string BaseUrl = "https://api.polza.ai/api/v1";
        public const string KeyFile = "UserSettings/PolzaAI.key";
        public const string DefaultModel = "openai/gpt-4o-mini";

        static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        public static string ApiKey
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("POLZA_API_KEY");
                if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
                return File.Exists(KeyFile) ? File.ReadAllText(KeyFile).Trim() : "";
            }
            set
            {
                Directory.CreateDirectory(Path.GetDirectoryName(KeyFile));
                File.WriteAllText(KeyFile, (value ?? "").Trim());
            }
        }

        public static bool HasKey => !string.IsNullOrEmpty(ApiKey);

#pragma warning disable 0649 // поля заполняет JsonUtility
        [Serializable] class Msg { public string role; public string content; }
        [Serializable] class ChatRequest { public string model; public Msg[] messages; public float temperature = 0.8f; }
        [Serializable] class Choice { public Msg message; }
        [Serializable] class ChatResponse { public Choice[] choices; }
        [Serializable] class Balance { public string amount; public string available; }
#pragma warning restore 0649

        /// <summary>Отправить запрос модели и получить текст ответа.</summary>
        public static async Task<string> Chat(string prompt, string system = null, string model = DefaultModel)
        {
            var messages = string.IsNullOrWhiteSpace(system)
                ? new[] { new Msg { role = "user", content = prompt } }
                : new[] { new Msg { role = "system", content = system }, new Msg { role = "user", content = prompt } };
            var body = JsonUtility.ToJson(new ChatRequest { model = model, messages = messages });
            var json = await Send(HttpMethod.Post, "/chat/completions", body);
            var resp = JsonUtility.FromJson<ChatResponse>(json);
            if (resp?.choices == null || resp.choices.Length == 0)
                throw new Exception("Пустой ответ Polza.ai: " + json);
            return resp.choices[0].message.content;
        }

        /// <summary>Доступный баланс в рублях.</summary>
        public static async Task<string> GetBalance()
        {
            var b = JsonUtility.FromJson<Balance>(await Send(HttpMethod.Get, "/balance", null));
            return $"{b.available} ₽ доступно (всего {b.amount} ₽)";
        }

        static async Task<string> Send(HttpMethod method, string path, string body)
        {
            var key = ApiKey;
            if (string.IsNullOrEmpty(key))
                throw new Exception("Не задан ключ Polza.ai: укажите его в окне Brainrot Waves → ИИ (Polza.ai).");
            using var req = new HttpRequestMessage(method, BaseUrl + path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            if (body != null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var res = await Http.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Polza.ai {(int)res.StatusCode}: {text}");
            return text;
        }
    }

    /// <summary>Окно для запросов к ИИ прямо из редактора.</summary>
    public class PolzaAIWindow : EditorWindow
    {
        string key, model = PolzaAI.DefaultModel, system = "Ты помогаешь делать игру Brainrot Waves. Отвечай по-русски.";
        string prompt = "", output = "", status = "";
        bool busy;
        Vector2 scroll;

        [MenuItem("Brainrot Waves/ИИ (Polza.ai)")]
        public static void Open() => GetWindow<PolzaAIWindow>("ИИ (Polza.ai)");

        void OnEnable() => key = PolzaAI.ApiKey;

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            key = EditorGUILayout.PasswordField("API-ключ", key);
            if (GUILayout.Button("Сохранить", GUILayout.Width(90))) { PolzaAI.ApiKey = key; status = "Ключ сохранён в " + PolzaAI.KeyFile; }
            EditorGUILayout.EndHorizontal();

            model = EditorGUILayout.TextField("Модель", model);
            EditorGUILayout.LabelField("Системная инструкция");
            system = EditorGUILayout.TextArea(system, GUILayout.MinHeight(36));
            EditorGUILayout.LabelField("Запрос");
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.MinHeight(70));

            using (new EditorGUI.DisabledScope(busy))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(busy ? "Ждём ответ…" : "Отправить")) Run(async () => output = await PolzaAI.Chat(prompt, system, model), "Готово");
                if (GUILayout.Button("Баланс", GUILayout.Width(90))) Run(async () => status = await PolzaAI.GetBalance(), null);
                if (GUILayout.Button("Копировать", GUILayout.Width(90))) EditorGUIUtility.systemCopyBuffer = output;
                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(output, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        async void Run(Func<Task> action, string doneStatus)
        {
            busy = true; status = "Запрос…"; Repaint();
            try { await action(); if (doneStatus != null) status = doneStatus; }
            catch (Exception e) { status = e.Message; Debug.LogError(e); }
            finally { busy = false; Repaint(); }
        }
    }
}
