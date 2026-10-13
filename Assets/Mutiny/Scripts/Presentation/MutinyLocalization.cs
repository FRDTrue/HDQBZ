using System;
using System.Collections.Generic;
using Mutiny.Persistence;
using UnityEngine;

namespace Mutiny.Presentation
{
    /// <summary>Bundled translations used by the IMGUI presentation layer.</summary>
    public static class MutinyLocalization
    {
        public const string English = "en";
        public const string SimplifiedChinese = "zh-Hans";
        private const string RuntimeTextResource = "Localization/MutinyRuntime";

        private static readonly Dictionary<string, Dictionary<string, string>> RuntimeText =
            new Dictionary<string, Dictionary<string, string>>();
        private static readonly HashSet<string> MissingKeys = new HashSet<string>();
        private static bool s_Started;
        private static bool s_RuntimeTextLoaded;
        private static string s_Code = SimplifiedChinese;

        public static event Action Changed;
        public static string Code => s_Code;
        public static bool IsReady => HasRuntimeText(English) && HasRuntimeText(SimplifiedChinese);
        public static bool UseOriginalFont => s_Code == English ||
            !HasRuntimeText(s_Code);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            RuntimeText.Clear();
            MissingKeys.Clear();
            s_Started = false;
            s_RuntimeTextLoaded = false;
            s_Code = SimplifiedChinese;
            Changed = null;
        }

        public static void Initialize(MonoBehaviour host)
        {
            if (s_Started || host == null)
                return;
            s_Started = true;
            EnsureRuntimeText();
            string saved = MutinySaveSystem.LanguageCode;
            s_Code = IsSupported(saved) ? saved : SimplifiedChinese;
        }

        public static void Select(string code)
        {
            if (!IsSupported(code))
                return;
            if (code == s_Code)
            {
                MutinySaveSystem.LanguageCode = code;
                return;
            }
            s_Code = code;
            MutinySaveSystem.LanguageCode = code;
            Changed?.Invoke();
        }

        private static bool IsSupported(string code) => code == English || code == SimplifiedChinese;

        private static bool HasRuntimeText(string code)
        {
            EnsureRuntimeText();
            return RuntimeText.TryGetValue(code, out Dictionary<string, string> entries) &&
                   entries.ContainsKey("frontend.play");
        }

        private static bool TryRuntimeText(string code, string key, out string value)
        {
            EnsureRuntimeText();
            if (RuntimeText.TryGetValue(code, out Dictionary<string, string> entries) &&
                entries.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                return true;
            value = null;
            return false;
        }

        private static void EnsureRuntimeText()
        {
            if (s_RuntimeTextLoaded)
                return;
            s_RuntimeTextLoaded = true;
            TextAsset asset = Resources.Load<TextAsset>(RuntimeTextResource);
            if (asset == null)
            {
                Debug.LogError("[Localization] Bundled runtime text is missing.");
                return;
            }

            var english = new Dictionary<string, string>(StringComparer.Ordinal);
            var chinese = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string rawLine in asset.text.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r').TrimStart('\uFEFF');
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                string[] fields = line.Split('\t');
                if (fields.Length != 3 || string.IsNullOrEmpty(fields[0]) || english.ContainsKey(fields[0]))
                {
                    Debug.LogError("[Localization] Invalid or duplicate runtime text row: " + line);
                    english.Clear();
                    chinese.Clear();
                    break;
                }
                english.Add(fields[0], fields[1].Replace("\\n", "\n"));
                chinese.Add(fields[0], fields[2].Replace("\\n", "\n"));
            }
            RuntimeText[English] = english;
            RuntimeText[SimplifiedChinese] = chinese;
        }

        public static string Text(string key, string englishFallback, params object[] arguments)
        {
            if (string.IsNullOrEmpty(key))
                return Format(englishFallback, arguments);
            if (TryRuntimeText(s_Code, key, out string runtimeValue))
                return Format(runtimeValue, arguments);
            if (TryRuntimeText(English, key, out string runtimeEnglish))
            {
                if (s_Code != English)
                    ReportMissing(key);
                return Format(runtimeEnglish, arguments);
            }
            ReportMissing(key);
            return Format(englishFallback ?? key, arguments);
        }

        private static string Format(string value, object[] arguments)
        {
            if (arguments == null || arguments.Length == 0 || value == null)
                return value;
            return string.Format(value, arguments);
        }

        private static void ReportMissing(string key)
        {
            string id = s_Code + ":" + key;
            if (MissingKeys.Add(id))
                Debug.LogWarning($"[Localization] Missing translation: {id}");
        }
    }
}
