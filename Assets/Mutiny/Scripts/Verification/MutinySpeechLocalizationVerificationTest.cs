using System;
using System.Reflection;
using Mutiny.Levels;
using Mutiny.Presentation;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Verification
{
    public static class MutinySpeechLocalizationVerificationTest
    {
        private const float Tick = 1f / 25f;
        private static readonly string[] Opponents =
        {
            "squid", "crab", "shark", "parrot", "monkey", "soldier", "blindPirate", "femalePirate",
            "oldPirate", "rainbowBeard", "cabinBoy", "tribe", "skeletonPirate", "bossGuy", "bossGuyZombie"
        };

        public static MutinyLevel1VerificationResult Run(MutinyGMManager gm)
        {
            var result = new MutinyLevel1VerificationResult();
            string previousLanguage = MutinyLocalization.Code;
            MutinyFrontendController frontend = UnityEngine.Object.FindAnyObjectByType<MutinyFrontendController>();
            bool frontendActive = frontend != null && frontend.gameObject.activeSelf;
            MutinyAudioManager audio = MutinyAudioManager.Instance;
            bool sfxEnabled = audio.SfxEnabled;
            try
            {
                // The fixture is a started battle without a title-page overlay.
                // Team initialization, speech ticks, clicks, deaths and game-over
                // events all use production entry points; no speech state is injected.
                if (frontend != null)
                    frontend.gameObject.SetActive(false);
                audio.SfxEnabled = true;
                // The batch editor may reload assemblies again after scene Awake.
                // Reload through the same asset-loading entry used by cold startup.
                typeof(MutinyAudioManager).GetMethod("LoadAllAudioClips", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(audio, null);
                gm.ExecuteCommand("setlanguage zh-cn");
                VerifyLanguageSwitch(result, gm, audio);
                foreach (string opponent in Opponents)
                {
                    VerifyConversation(result, opponent, true);
                    VerifyConversation(result, opponent, false);
                }
                foreach (MutinyCornerControl control in Enum.GetValues(typeof(MutinyCornerControl)))
                {
                    string expected = control == MutinyCornerControl.Quit ? "退出" :
                        control == MutinyCornerControl.Music ? "音乐" : "音效";
                    result.Assert(MutinyGameHUD.ResolveLocalizedCornerTooltip(control) == expected,
                        "LOC-CORNER-01 Chinese hover text: " + control);
                    Rect hit = MutinyGameHUD.ResolveOriginalCornerHitRect(control);
                    Rect bubble = MutinyGameHUD.ResolveCornerBubbleRect(control);
                    result.Assert(MutinyGameHUD.IsCornerHovered(control, hit.center, false) &&
                        MutinyGameHUD.IsCornerHovered(control, bubble.center, true) &&
                        !MutinyGameHUD.IsCornerHovered(control, bubble.center, false),
                        "LOC-CORNER-01 original icon entry and existing bubble hover retention: " + control);
                    Vector2 textSize = MutinyLocalizedText.MeasureTooltipSize(expected);
                    result.Assert(textSize.x <= bubble.width - 4f && textSize.y <= 18f &&
                        MutinyGameHUD.IsCornerHovered(control, new Vector2(bubble.x + 0.5f, bubble.center.y), true),
                        "LOC-CORNER-01 full Chinese glyphs fit expanded tooltip and its edge retains hover: " + control);
                }
                gm.ExecuteCommand("setlanguage en");
                foreach (MutinyCornerControl control in Enum.GetValues(typeof(MutinyCornerControl)))
                    result.Assert(MutinyGameHUD.ResolveLocalizedCornerTooltip(control) ==
                        MutinyGameHUD.ResolveOriginalCornerTooltip(control), "LOC-CORNER-01 English hover text: " + control);
            }
            finally
            {
                audio.SfxEnabled = sfxEnabled;
                MutinyLocalization.Select(previousLanguage);
                if (frontend != null)
                    frontend.gameObject.SetActive(frontendActive);
            }
            return result;
        }

        private static void VerifyLanguageSwitch(MutinyLevel1VerificationResult result, MutinyGMManager gm,
            MutinyAudioManager audio)
        {
            var fixture = new BattleFixture("squid");
            int voices = 0;
            Action<string> observeVoice = clip => { if (clip == "redPirate" || clip == "squid") voices++; };
            audio.SfxPlayed += observeVoice;
            try
            {
                MutinySpeechController speech = fixture.Speech;
                speech.AdvanceSpeechForVerification(5 * Tick + 0.00001f);
                MutinyCharacter speaker = speech.Speaker;
                gm.ExecuteCommand("setlanguage en");
                result.Assert(!speech.IsBubbleVisible && speech.VisibleText.Length == 0,
                    "LOC-SPEECH-02 switching during start delay keeps the bubble hidden");
                speech.AdvanceSpeechForVerification(5 * Tick);
                result.Assert(speech.IsBubbleVisible && speech.VisibleText.Length == 0,
                    "LOC-SPEECH-02 language switch does not restart ten-tick delay");
                speech.AdvanceSpeechForVerification(Tick);
                string partialEnglish = speech.VisibleText;
                gm.ExecuteCommand("setlanguage zh-cn");
                string chinese = MutinyLocalization.Text("speech.squid.0", null);
                result.Assert(partialEnglish.Length == 3 && speech.VisibleText.Length > 0 &&
                    speech.VisibleText.Length < chinese.Length && chinese.StartsWith(speech.VisibleText) &&
                    speech.Speaker == speaker && voices == 1,
                    "LOC-SPEECH-02 partial text re-resolves immediately without restarting speaker or voice");
                speech.Click();
                result.Assert(speech.VisibleText == chinese,
                    "LOC-SPEECH-02 production click completes the current Chinese line");
                speech.AdvanceSpeechForVerification(2 * Tick);
                gm.ExecuteCommand("setlanguage en");
                result.Assert(speech.VisibleText == MutinyLocalization.Text("speech.squid.0", null) && voices == 1,
                    "LOC-SPEECH-02 a completed line stays complete when switching to longer English");
                speech.AdvanceSpeechForVerification(158 * Tick);
                result.Assert(speech.Speaker == speaker && speech.IsBubbleVisible,
                    "LOC-SPEECH-02 completed line retains original 160-tick hold");
                speech.AdvanceSpeechForVerification(Tick);
                result.Assert(speech.Speaker == fixture.Enemy && !speech.IsBubbleVisible && voices == 2,
                    "LOC-SPEECH-02 next tick starts opponent response with exactly one new voice");
                gm.ExecuteCommand("setlanguage zh-cn");
            }
            finally
            {
                audio.SfxPlayed -= observeVoice;
                fixture.Dispose();
            }
        }

        private static void VerifyConversation(MutinyLevel1VerificationResult result, string opponent, bool playerWins)
        {
            var fixture = new BattleFixture(opponent);
            try
            {
                MutinySpeechController speech = fixture.Speech;
                if (playerWins)
                {
                    speech.AdvanceSpeechForVerification(1f);
                    speech.Click();
                    AssertLine(result, speech, opponent, 0);
                    speech.AdvanceSpeechForVerification(161 * Tick);
                    speech.AdvanceSpeechForVerification(1f);
                    speech.Click();
                    AssertLine(result, speech, opponent, 1);
                    speech.AdvanceSpeechForVerification(161 * Tick);
                    result.Assert(!speech.HasPendingOrActiveSpeech,
                        "LOC-SPEECH-01 introduction completes through production ticks: " + opponent);
                }
                (playerWins ? fixture.Enemy : fixture.Player).Drown();
                fixture.Manager.PassTurn();
                for (int tick = 0; tick < 11; tick++)
                    fixture.Manager.AdvanceSimulationTick();
                speech.AdvanceSpeechForVerification(1f);
                speech.Click();
                result.Assert(fixture.Manager.GameResult == (playerWins ? GameOverResult.Team1Wins : GameOverResult.Team2Wins) &&
                    speech.IsPlayingEndingLine, "LOC-SPEECH-01 actual death/settling emits victory speech: " + opponent);
                AssertLine(result, speech, opponent, playerWins ? 2 : 3);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void AssertLine(MutinyLevel1VerificationResult result, MutinySpeechController speech,
            string opponent, int index)
        {
            string expected = MutinyLocalization.Text(MutinySpeechController.LineKey(opponent, index), null);
            result.Assert(!string.IsNullOrEmpty(expected) && speech.VisibleText == expected &&
                expected != MutinySpeechController.LineKey(opponent, index),
                "LOC-SPEECH-01 production localized line: " + opponent + "." + index);
        }

        private sealed class BattleFixture : IDisposable
        {
            private readonly GameObject m_Root = new GameObject("LocalizedSpeechBattleVerification");
            public readonly MutinyCharacter Player;
            public readonly MutinyCharacter Enemy;
            public readonly MutinyTurnManager Manager;
            public readonly MutinySpeechController Speech;

            public BattleFixture(string opponent)
            {
                // Keep game-over bookkeeping inside the fixture instead of
                // locating the scene's controller and awarding saved unlocks.
                m_Root.AddComponent<MutinyLevelController>().ConfigureSession(MutinyGameMode.LocalTwoPlayer);
                var playerTeamObject = new GameObject("PlayerTeam");
                playerTeamObject.transform.SetParent(m_Root.transform);
                var enemyTeamObject = new GameObject("EnemyTeam");
                enemyTeamObject.transform.SetParent(m_Root.transform);
                MutinyTeam team1 = playerTeamObject.AddComponent<MutinyTeam>();
                team1.TeamNumber = 1;
                MutinyTeam team2 = enemyTeamObject.AddComponent<MutinyTeam>();
                team2.TeamNumber = 2;
                team2.IsAiControlled = true;
                Player = playerTeamObject.AddComponent<MutinyCharacter>();
                Player.CharacterType = "redPirateCaptain";
                Player.TeamIndex = 1;
                Enemy = enemyTeamObject.AddComponent<MutinyCharacter>();
                Enemy.CharacterType = opponent + (opponent == "tribe" ? "Chief" : "Captain");
                Enemy.TeamIndex = 2;
                team1.RegisterCharacter(Player);
                team2.RegisterCharacter(Enemy);
                Manager = m_Root.AddComponent<MutinyTurnManager>();
                Manager.Team1 = team1;
                Manager.Team2 = team2;
                Speech = m_Root.AddComponent<MutinySpeechController>();
                Speech.Initialize(Manager);
                Manager.Initialize(team1, team2);
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(m_Root);
        }
    }
}
