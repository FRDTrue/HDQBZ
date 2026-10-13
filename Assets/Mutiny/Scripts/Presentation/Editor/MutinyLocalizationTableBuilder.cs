using System;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Mutiny.Presentation.Editor
{
    public static class MutinyLocalizationTableBuilder
    {
        private const string DirectoryPath = "Assets/Mutiny/Localization";
        private const string SourcePath = DirectoryPath + "/Mutiny.tsv";
        private const string TablePath = DirectoryPath + "/Tables";
        private const string RuntimeTextPath = "Assets/Mutiny/Resources/Localization/MutinyRuntime.txt";

        [MenuItem("Mutiny/Localization/Rebuild String Tables")]
        public static void Build()
        {
            Directory.CreateDirectory(TablePath);
            EnsureSettings();
            Locale english = EnsureLocale("en", "English");
            Locale chinese = EnsureLocale("zh-Hans", "Chinese-Simplified");
            var collection = LocalizationEditorSettings.GetStringTableCollection("Mutiny") ??
                LocalizationEditorSettings.CreateStringTableCollection("Mutiny", TablePath);
            var en = collection.GetTable(english.Identifier) as StringTable ??
                collection.AddNewTable(english.Identifier) as StringTable;
            var zh = collection.GetTable(chinese.Identifier) as StringTable ??
                collection.AddNewTable(chinese.Identifier) as StringTable;
            if (en == null || zh == null)
                throw new InvalidOperationException("Failed to create Mutiny string tables.");

            int count = 0;
            foreach (string line in File.ReadAllLines(SourcePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                string[] fields = line.Split('\t');
                if (fields.Length != 3)
                    throw new FormatException($"Expected key, English and Chinese TSV fields: {line}");
                Set(en, fields[0], Decode(fields[1]));
                Set(zh, fields[0], Decode(fields[2]));
                count++;
            }
            LocalizationEditorSettings.SetPreloadTableFlag(en, true);
            LocalizationEditorSettings.SetPreloadTableFlag(zh, true);
            EditorUtility.SetDirty(en);
            EditorUtility.SetDirty(zh);
            EditorUtility.SetDirty(en.SharedData);
            File.Copy(SourcePath, RuntimeTextPath, true);
            AssetDatabase.ImportAsset(RuntimeTextPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Localization] Built {count} English/Chinese keys.");
        }

        private static void EnsureSettings()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
                return;
            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Mutiny Localization Settings";
            AssetDatabase.CreateAsset(settings, DirectoryPath + "/Localization Settings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        private static Locale EnsureLocale(string code, string displayName)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(code);
            if (locale != null)
                return locale;
            locale = Locale.CreateLocale(code);
            locale.LocaleName = displayName;
            AssetDatabase.CreateAsset(locale, DirectoryPath + "/Locale_" + code + ".asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void Set(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
        }

        private static string Decode(string value) => value.Replace("\\n", "\n");
    }
}
