using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mutiny.Persistence;
using Mutiny.Verification;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Mutiny.Presentation.Editor
{
    [InitializeOnLoad]
    public static class MutinyLocalizationVerification
    {
        private const string SourcePath = "Assets/Mutiny/Localization/Mutiny.tsv";
        private const string OriginalWeaponPath = "Docs/10-OriginalEvidence/Artifacts/ReverseEngineering/Swf/deobfuscated/scripts/__Packages/com/nitrome/game/WeaponSelectButton.as";
        private const string OriginalScriptsPath = "Docs/10-OriginalEvidence/Artifacts/ReverseEngineering/Swf/deobfuscated/scripts/";
        private const string FontPath = "Assets/Resources/抖音美好体.otf";
        private const string PlayModeKey = "Mutiny.LocalizationPlayModeVerification";
        private const string LanguagePrefKey = "mutiny_language_v1";
        private static double s_PlayModeStart;

        static MutinyLocalizationVerification()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (SessionState.GetBool(PlayModeKey, false))
                ArmRunningGameCheck();
        }

        [MenuItem("Mutiny/Localization/Validate Play Mode")]
        public static void ValidatePlayMode()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Start the localization verification outside Play Mode.");
            EditorApplication.delayCall += BeginPlayMode;
        }

        private static void BeginPlayMode()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            SessionState.SetBool(PlayModeKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayModeKey, false))
                return;
            Debug.Log("[Localization] Play Mode state: " + state);
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;
            ArmRunningGameCheck();
        }

        private static void ArmRunningGameCheck()
        {
            s_PlayModeStart = EditorApplication.timeSinceStartup;
            EditorApplication.update -= VerifyRunningGame;
            EditorApplication.update += VerifyRunningGame;
        }

        private static void VerifyRunningGame()
        {
            if (!EditorApplication.isPlaying)
                return;
            MutinyGMManager gm = MutinyGMManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<MutinyGMManager>();
            MutinyLocalization.Initialize(gm);
            if (!MutinyLocalization.IsReady && EditorApplication.timeSinceStartup - s_PlayModeStart < 20d)
                return;
            EditorApplication.update -= VerifyRunningGame;
            SessionState.EraseBool(PlayModeKey);
            bool passed = false;
            bool hadLanguage = PlayerPrefs.HasKey(LanguagePrefKey);
            string previousLanguage = PlayerPrefs.GetString(LanguagePrefKey, string.Empty);
            string previousCode = MutinyLocalization.Code;
            try
            {
                if (!MutinyLocalization.IsReady ||
                    UnityEngine.Object.FindAnyObjectByType<MutinyFrontendController>() == null ||
                    Resources.Load<Font>("抖音美好体") == null)
                    throw new InvalidOperationException("Production frontend did not load localization text and font.");
                MutinyLevel1VerificationResult result = MutinyTurnActionUiVerificationTest.RunGMLanguage(gm);
                if (!result.Passed)
                    throw new InvalidOperationException(string.Join("\n", result.Failures));
                foreach (string line in result.Logs)
                    Debug.Log(line);
                MutinyLevel1VerificationResult speechResult = MutinySpeechLocalizationVerificationTest.Run(gm);
                foreach (string line in speechResult.Logs)
                    Debug.Log(line);
                if (!speechResult.Passed)
                    throw new InvalidOperationException(string.Join("\n", speechResult.Failures));
                passed = true;
                Debug.Log($"[Localization] GM-09 Play Mode verification passed: {result.PassedAssertions}/{result.TotalAssertions}; production frontend loaded text and font.");
                Debug.Log($"[Localization] Speech and tooltips Play Mode verification passed: {speechResult.PassedAssertions}/{speechResult.TotalAssertions}.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                MutinyLocalization.Select(previousCode);
                if (hadLanguage)
                    PlayerPrefs.SetString(LanguagePrefKey, previousLanguage);
                else
                    PlayerPrefs.DeleteKey(LanguagePrefKey);
                PlayerPrefs.Save();
                if (Application.isBatchMode)
                    EditorApplication.Exit(passed ? 0 : 1);
                else
                    EditorApplication.ExitPlaymode();
            }
        }

        [MenuItem("Mutiny/Localization/Validate Initial Tables")]
        public static void Validate()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
                throw new InvalidOperationException("Active Localization Settings are missing from the build.");
            var collection = LocalizationEditorSettings.GetStringTableCollection("Mutiny");
            if (collection == null)
                throw new InvalidOperationException("Mutiny String Table Collection is missing.");
            var englishLocale = LocalizationEditorSettings.GetLocale("en");
            var chineseLocale = LocalizationEditorSettings.GetLocale("zh-Hans");
            StringTable english = englishLocale == null ? null : collection.GetTable(englishLocale.Identifier) as StringTable;
            StringTable chinese = chineseLocale == null ? null : collection.GetTable(chineseLocale.Identifier) as StringTable;
            if (english == null || chinese == null)
                throw new InvalidOperationException("English or Simplified Chinese table is missing.");

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null || !font.dynamic)
                throw new InvalidOperationException("Bundled CJK dynamic font is missing.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (string line in File.ReadAllLines(SourcePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                string[] fields = line.Split('\t');
                if (fields.Length != 3 || !seen.Add(fields[0]))
                    throw new InvalidOperationException("Invalid or duplicate localization key: " + line);

                string en = fields[1].Replace("\\n", "\n");
                string zh = fields[2].Replace("\\n", "\n");
                if (string.IsNullOrWhiteSpace(en) || string.IsNullOrWhiteSpace(zh) ||
                    english.GetEntry(fields[0])?.LocalizedValue != en ||
                    chinese.GetEntry(fields[0])?.LocalizedValue != zh)
                    throw new InvalidOperationException("Missing or stale translation: " + fields[0]);

                string enSlots = string.Join(",", ExtractSlots(en));
                string zhSlots = string.Join(",", ExtractSlots(zh));
                if (enSlots != zhSlots)
                    throw new InvalidOperationException("Placeholder mismatch: " + fields[0]);
                foreach (char character in zh)
                {
                    if (!char.IsWhiteSpace(character) && !font.HasCharacter(character))
                        throw new InvalidOperationException($"Missing bundled glyph U+{(int)character:X4}: {fields[0]}");
                }
                if (fields[0].StartsWith("speech.", StringComparison.Ordinal) &&
                    MutinyLocalizedText.MeasureSpeechHeight(zh, 216f) > 98f)
                    throw new InvalidOperationException("Chinese dialogue overflows bubble: " + fields[0]);
                count++;
            }

            var originalWeapons = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(File.ReadAllText(OriginalWeaponPath),
                         @"hover_(\w+):\[""([^""]*)"",""([^""]*)""\]"))
                originalWeapons[match.Groups[1].Value] = new[] { match.Groups[2].Value, match.Groups[3].Value.Replace('|', '\n') };
            foreach (string weapon in new[] { "cherryBomb", "dynamite", "boulder", "piecesOfEight", "rumBottle",
                         "banana", "parachuteBomb", "woodenCrate", "gunpowderBarrel", "seagull", "mine",
                         "cannon", "anchor", "voodooDoll", "tidalWave" })
            {
                MutinyGameHUD.GetOriginalActionCopy(weapon, out string title, out string description);
                if (english.GetEntry("weapon." + weapon + ".name")?.LocalizedValue != title ||
                    english.GetEntry("weapon." + weapon + ".desc")?.LocalizedValue != description ||
                    !originalWeapons.TryGetValue(weapon, out string[] original) ||
                    original[0] != title || original[1] != description)
                    throw new InvalidOperationException("Original weapon copy changed in table: " + weapon);
            }
            int speechCount = 0;
            string originalTeams = File.ReadAllText(OriginalScriptsPath + "__Packages/com/nitrome/throwgame/Team.as");
            foreach (Match team in Regex.Matches(originalTeams, @"(\w+):\[([^\]]+)\]"))
            {
                MatchCollection lines = Regex.Matches(team.Groups[2].Value, "\"((?:\\\\.|[^\"])*)\"");
                if (lines.Count != 4)
                    throw new InvalidOperationException("Expected four original dialogue lines: " + team.Groups[1].Value);
                for (int index = 0; index < lines.Count; index++)
                {
                    string key = MutinySpeechController.LineKey(team.Groups[1].Value, index);
                    string original = lines[index].Groups[1].Value.Replace("\\'", "'");
                    if (english.GetEntry(key)?.LocalizedValue != original)
                        throw new InvalidOperationException("Original speech changed or missing: " + key);
                    speechCount++;
                }
            }
            if (speechCount != 60)
                throw new InvalidOperationException("Expected 60 battle speech lines from AS2.");
            int[] endingSprites = { 784, 788, 791, 793, 795 };
            for (int index = 0; index < endingSprites.Length; index++)
            {
                string actionPath = OriginalScriptsPath + $"DefineSprite_{endingSprites[index]}/frame_1/PlaceObject2_149_DangleFont_1/CLIPACTIONRECORD on(construct).as";
                Match action = Regex.Match(File.ReadAllText(actionPath), "text = \"([^\"]*)\";");
                if (!action.Success || english.GetEntry("speech.ending." + index)?.LocalizedValue != action.Groups[1].Value ||
                    MutinyEndingSequence.Dialogues[index].Text != action.Groups[1].Value)
                    throw new InvalidOperationException("Original ending dialogue changed: " + index);
            }
            Debug.Log($"[Localization] Validation passed: {count} paired keys, bundled glyphs, 15 SWF weapon copies, 60 battle/5 ending originals and 65 Chinese bubble layouts.");
        }

        public static void RebuildAndValidate()
        {
            MutinyLocalizationTableBuilder.Build();
            Validate();
        }

        private static IEnumerable<string> ExtractSlots(string value)
        {
            foreach (Match match in Regex.Matches(value, @"\{\d+\}"))
                yield return match.Value;
        }
    }
}
