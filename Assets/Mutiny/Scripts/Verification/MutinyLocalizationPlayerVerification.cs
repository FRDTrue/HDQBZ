using System;
using System.Collections;
using Mutiny.Persistence;
using Mutiny.Presentation;
using UnityEngine;

namespace Mutiny.Verification
{
    // Optional standalone Player regression. Run with
    // -verifyLocalization first|saved-cn|saved-en on a test product identity.
    public sealed class MutinyLocalizationPlayerVerification : MonoBehaviour
    {
        private const string Switch = "-verifyLocalization";
        private static string s_Phase;
        private static string s_FirstException;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Arm()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, Switch);
            if (index < 0)
                return;
            s_Phase = index + 1 < args.Length ? args[index + 1] : string.Empty;
            Application.logMessageReceived += OnLog;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartVerification()
        {
            if (s_Phase == null)
                return;
            MutinyLocalizationPlayerVerification host = new GameObject("LocalizationPlayerVerification")
                .AddComponent<MutinyLocalizationPlayerVerification>();
            host.StartCoroutine(host.Verify());
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception && s_FirstException == null)
                s_FirstException = condition + "\n" + stackTrace;
        }

        private IEnumerator Verify()
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            MutinyGMManager gm;
            do
            {
                gm = MutinyGMManager.Instance ?? FindAnyObjectByType<MutinyGMManager>();
                MutinyLocalization.Initialize(gm);
                if (MutinyLocalization.IsReady && FindAnyObjectByType<MutinyFrontendController>() != null)
                    break;
                yield return null;
            } while (Time.realtimeSinceStartup < deadline);

            if (!MutinyLocalization.IsReady || gm == null)
            {
                Finish(false, "Runtime translations did not load.");
                yield break;
            }

            string initial = s_Phase == "saved-cn" ? MutinyLocalization.SimplifiedChinese : MutinyLocalization.English;
            if ((s_Phase != "first" && s_Phase != "saved-cn" && s_Phase != "saved-en") ||
                MutinyLocalization.Code != initial ||
                MutinySaveSystem.LanguageCode != (s_Phase == "first" ? string.Empty : initial))
            {
                Finish(false, "Unexpected startup language: " + MutinyLocalization.Code +
                              ", saved=" + MutinySaveSystem.LanguageCode + ", phase=" + s_Phase);
                yield break;
            }

            string expected = initial == MutinyLocalization.English ? "play" : "开始游戏";
            if (MutinyLocalization.Text("frontend.play", "play") != expected)
            {
                Finish(false, "Title translation was not available on startup.");
                yield break;
            }

            string[] commands =
                { "setlanguage en", "setlanguage zh-cn", "setlanguage en", "setlanguage zh-cn", "setlanguage en" };
            foreach (string command in commands)
            {
                string target = command == "setlanguage zh-cn"
                    ? MutinyLocalization.SimplifiedChinese : MutinyLocalization.English;
                expected = target == MutinyLocalization.SimplifiedChinese ? "开始游戏" : "play";
                if (!gm.ExecuteCommand(command) || MutinyLocalization.Code != target ||
                    MutinySaveSystem.LanguageCode != target ||
                    MutinyLocalization.Text("frontend.play", "play") != expected ||
                    MutinyLocalization.Text("frontend.scores", "scores") !=
                        (target == MutinyLocalization.English ? "scores" : "成绩") ||
                    MutinyLocalization.UseOriginalFont != (target == MutinyLocalization.English))
                {
                    Finish(false, "GM switch or title text failed: " + command);
                    yield break;
                }
                yield return null;
            }

            for (int frame = 0; frame < 4; frame++)
                yield return null;
            Finish(s_FirstException == null, s_FirstException ??
                ("phase=" + s_Phase + ", startup=" + initial + ", sequence=" + string.Join(" -> ", commands)));
        }

        private static void Finish(bool success, string detail)
        {
            Debug.Log("[Localization Player Smoke] " + (success ? "PASS " : "FAIL ") + detail);
            Application.logMessageReceived -= OnLog;
            Application.Quit(success ? 0 : 1);
        }
    }
}
