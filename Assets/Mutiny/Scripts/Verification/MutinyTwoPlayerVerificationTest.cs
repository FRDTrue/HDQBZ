using Mutiny.Levels;
using Mutiny.Presentation;
using Mutiny.Persistence;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Verification
{
    /// <summary>Smoke checks through the real menu state machine and numbered level loader.</summary>
    public static class MutinyTwoPlayerVerificationTest
    {
        public static MutinyLevel1VerificationResult Run()
        {
            var result = new MutinyLevel1VerificationResult();
            var flow = new MutinyFrontendFlow();
            flow.PressPlay();
            flow.PressTwoPlayer();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.TwoPlayerLevelSelect &&
                flow.SelectedTwoPlayerLevel == 16 && !flow.StepTwoPlayerLevel(-1),
                "2P-NAV/SEL-01 menu enters two-player level 16 with no previous level");
            for (int i = 0; i < 17; i++)
                result.Assert(flow.StepTwoPlayerLevel(1), $"2P-SEL-01 next level {i + 17}");
            result.Assert(flow.SelectedTwoPlayerLevel == 33 && !flow.StepTwoPlayerLevel(1),
                "2P-SEL-01 next is hidden at 33");
            flow.PressTwoPlayerLevelSelectBack();
            flow.PressTwoPlayer();
            result.Assert(flow.SelectedTwoPlayerLevel == 33,
                "2P-SEL-01 selector remembers the last numbered level");
            result.Assert(MutinyLevelController.HasNumberedLevelData(33) &&
                flow.TrySelectTwoPlayerLevel(MutinyLevelController.HasNumberedLevelData) &&
                flow.CurrentPage == MutinyFrontendPage.Gameplay &&
                flow.ReturnToTwoPlayerLevelSelect(),
                "2P-ASSET-01 official level 33 is available through the production selector");

            GameObject host = new GameObject("TwoPlayerVerification_LevelController");
            try
            {
                MutinyLevelController controller = host.AddComponent<MutinyLevelController>();
                controller.ConfigureSession(MutinyGameMode.LocalTwoPlayer, resetVersusWins: true);
                result.Assert(MutinyLevelController.HasNumberedLevelData(16) &&
                    controller.TryLoadLevel(16),
                    "2P-DATA-01 production numbered loader accepts verified original 16");
                MutinyLevelRoot level = controller.CurrentLevel;
                result.Assert(level != null && level.Players == 1 && level.Team1 != null && level.Team2 != null &&
                    !level.Team1.IsAiControlled && !level.Team2.IsAiControlled &&
                    level.Team2.GetComponent<MutinyAIController>() == null,
                    "2P-DATA-01 menu mode overrides XML players=1 without changing XML metadata");
                result.Assert(!controller.TryLoadLevel(34) && controller.CurrentLevelIndex == 16 &&
                    controller.CurrentLevel == level,
                    "2P-ASSET-01 invalid 34 does not clamp or replace the active level");
                // Passing a turn can spawn a chest and needs a running scene.
                // Keep the resource audit runnable from the editor menu.
                if (Application.isPlaying)
                    VerifyTurnAndInput(level, result);
                result.Assert(MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team1Wins, 16,
                        controller.ActiveGameMode) == MutinyGameEndPopupKind.VersusPlayer1Wins &&
                    MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team2Wins, 16,
                        controller.ActiveGameMode) == MutinyGameEndPopupKind.VersusPlayer2Wins &&
                    MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Draw, 16,
                        controller.ActiveGameMode) == MutinyGameEndPopupKind.VersusDraw,
                    "2P-END-01 all production result kinds route to distinct versus popup states");
                int unlockedBefore = MutinySaveSystem.HighestUnlockedLevel;
                int scoreBefore = controller.SinglePlayerScore;
                VerifyProductionResult(controller, result, GameOverResult.Team1Wins, 1, 0);
                controller.RestartCurrentLevel();
                result.Assert(controller.Player1Wins == 1 && controller.Player2Wins == 0 &&
                    controller.CurrentLevelIndex == 16,
                    "2P-EXIT-01 restart preserves score and selected level");
                VerifyProductionResult(controller, result, GameOverResult.Team2Wins, 1, 1);
                controller.RestartCurrentLevel();
                VerifyProductionResult(controller, result, GameOverResult.Draw, 1, 1);
                result.Assert(MutinySaveSystem.HighestUnlockedLevel == unlockedBefore &&
                    controller.SinglePlayerScore == scoreBefore,
                    "2P-SEL/END-02 versus results do not change single-player unlock or score");
                for (int number = 16; number <= 33; number++)
                {
                    bool loaded = controller.TryLoadLevel(number);
                    MutinyLevelRoot current = controller.CurrentLevel;
                    result.Assert(loaded && current != null && controller.CurrentLevelIndex == number &&
                        current.Team1 != null && current.Team2 != null &&
                        current.Team1.AliveCount > 0 && current.Team2.AliveCount > 0 &&
                        !current.Team1.IsAiControlled && !current.Team2.IsAiControlled &&
                        current.SkyColour == MutinyOriginalBackground.SkyColourForLevel(number),
                        $"2P-ASSET-01 official level {number:D2} loads with two human teams and correct sky");
                }
                controller.ConfigureSession(MutinyGameMode.SinglePlayer);
                result.Assert(controller.TryLoadLevel(1) && controller.CurrentLevel.Team2.IsAiControlled,
                    "2P-DATA-01 returning to single-player rebuilds the AI team");
            }
            finally
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(host);
                else
                    Object.Destroy(host);
#else
                Object.Destroy(host);
#endif
            }
            return result;
        }

        private static void VerifyProductionResult(MutinyLevelController controller,
            MutinyLevel1VerificationResult result, GameOverResult expected, int p1, int p2)
        {
            MutinyLevelRoot level = controller.CurrentLevel;
            MutinyTurnManager turn = level.GetComponent<MutinyTurnManager>();
            turn.StartGame();
            if (expected == GameOverResult.Team1Wins || expected == GameOverResult.Draw)
                DrownTeam(turn.Team2);
            if (expected == GameOverResult.Team2Wins || expected == GameOverResult.Draw)
                DrownTeam(turn.Team1);
            for (int tick = 0; tick < 24 && turn.CurrentPhase != TurnPhase.GameOver; tick++)
                turn.AdvanceSimulationTick();
            result.Assert(turn.CurrentPhase == TurnPhase.GameOver && turn.GameResult == expected &&
                controller.Player1Wins == p1 && controller.Player2Wins == p2,
                $"2P-END-01 production drown and turn settlement produce {expected} with score {p1}:{p2}");
            for (int tick = 0; tick < 24; tick++)
                turn.AdvanceSimulationTick();
            result.Assert(controller.Player1Wins == p1 && controller.Player2Wins == p2,
                $"2P-END-01 repeated settlement cannot count {expected} twice");
        }

        private static void VerifyTurnAndInput(MutinyLevelRoot level,
            MutinyLevel1VerificationResult result)
        {
            MutinyTurnManager turn = level.GetComponent<MutinyTurnManager>();
            MutinyPlayerInput input = level.GetComponent<MutinyPlayerInput>();
            MutinyIngameTextArea text = level.GetComponent<MutinyIngameTextArea>();
            turn.StartGame();
            MutinyTeam first = turn.CurrentTeam;
            MutinyTeam second = first == turn.Team1 ? turn.Team2 : turn.Team1;
            result.Assert(first != null && !first.IsAiControlled && text.PendingLineCount > 0,
                "2P-TURN/FEED-01 production starter is human and queues the turn announcement");
            MutinyCharacter firstOwn = first.Characters[0];
            MutinyCharacter firstEnemy = second.Characters[0];
            result.Assert(!input.TrySelectCharacterForVerification(first,
                    new Vector2(firstEnemy.PhysicsBody.State.X, firstEnemy.PhysicsBody.State.Y)) &&
                input.TrySelectCharacterForVerification(first,
                    new Vector2(firstOwn.PhysicsBody.State.X, firstOwn.PhysicsBody.State.Y)),
                "2P-INP-01 active pointer rejects enemy and selects own character");
            turn.PassTurn();
            for (int tick = 0; tick < 24 && turn.CurrentTeam == first; tick++)
                turn.AdvanceSimulationTick();
            result.Assert(turn.CurrentTeam == second && turn.CurrentPhase == TurnPhase.TurnActive &&
                text.PendingLineCount > 1,
                "2P-TURN/FEED-01 production pass switches to the other human and queues the next prompt");
            input.ReturnToCharacterSelection();
            MutinyCharacter secondOwn = second.Characters[0];
            result.Assert(!input.TrySelectCharacterForVerification(second,
                    new Vector2(firstOwn.PhysicsBody.State.X, firstOwn.PhysicsBody.State.Y)) &&
                input.TrySelectCharacterForVerification(second,
                    new Vector2(secondOwn.PhysicsBody.State.X, secondOwn.PhysicsBody.State.Y)),
                "2P-INP-01 the second human can select only their own character");
        }

        private static void DrownTeam(MutinyTeam team)
        {
            foreach (MutinyCharacter character in team.Characters)
                character?.Drown();
        }
    }
}
