using System;
using System.Collections.Generic;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using Mutiny.Network;
using UnityEngine;

namespace Mutiny.Simulation
{
    public enum TurnPhase
    {
        NotStarted,
        TurnActive,
        ActionExecuting,
        Settling,
        GameOver
    }

    public enum GameOverResult
    {
        None,
        Team1Wins,
        Team2Wins,
        Draw
    }

    [DisallowMultipleComponent]
    public sealed class MutinyTurnManager : MonoBehaviour
    {
        public const int InactivitySettlingThreshold = 10;

        [Header("Teams")]
        public MutinyTeam Team1;
        public MutinyTeam Team2;
        public MutinyTeam CurrentTeam;

        [Header("State")]
        public TurnPhase CurrentPhase = TurnPhase.NotStarted;
        public GameOverResult GameResult = GameOverResult.None;
        public int InactivityTicks = 0;
        public int TurnCount = 0;

        public event Action<MutinyTeam> OnTurnStarted;
        public event Action<MutinyTeam> OnTurnEnded;
        public event Action<GameOverResult> OnGameOver;

        private float m_TickAccumulator = 0f;
        private List<MutinyCharacter> m_AllCharacters = new List<MutinyCharacter>();
        private bool m_ActionCommittedThisTurn;
        private string m_LastRestBlocker;
        private int m_RestBlockerTicks;
        private MutinyTeam m_AiTakeoverTeam;

        public bool ActionCommittedThisTurn => m_ActionCommittedThisTurn;
        public bool IsAiTakeoverActive => m_AiTakeoverTeam != null;

        // GM-08: temporarily hand the current human turn to the normal AI.
        // Do not StartTurn/ResetTurnActions: a player jump may already be committed.
        public bool TryTakeOverCurrentPlayerTurn(float luck, out string error)
        {
            error = null;
            if (!MutinyAIController.IsValidGmLuck(luck))
            {
                error = "Luck must be a finite number from 0 to 99999.";
                return false;
            }
            // AI takeover consumes only the local machine's RNG and would desync
            // an online peer; GM-08 stays a local/single-player tool.
            MutinyLevelController takeoverModeOwner = GetComponentInParent<MutinyLevelController>() ??
                FindAnyObjectByType<MutinyLevelController>();
            if (takeoverModeOwner != null &&
                takeoverModeOwner.ActiveGameMode == MutinyGameMode.OnlineTwoPlayer)
            {
                error = "AI takeover is unavailable during online matches.";
                return false;
            }
            if (IsAiTakeoverActive || CurrentTeam == null || CurrentTeam.IsAiControlled ||
                CurrentTeam.IsDefeated || !HasPlayableTeams() ||
                !isActiveAndEnabled || !CurrentTeam.gameObject.activeInHierarchy)
            {
                error = "Requires an active human turn that is not already taken over.";
                return false;
            }

            MutinyCharacter selected = CurrentTeam.SelectedCharacter;
            bool afterJump = selected != null && selected.IsAlive &&
                             !selected.CanThrow && selected.CanShoot;
            bool ready = CurrentPhase == TurnPhase.TurnActive &&
                         (!m_ActionCommittedThisTurn || afterJump);
            bool settlingJump = afterJump && selected.IsSelfThrown &&
                                (CurrentPhase == TurnPhase.ActionExecuting || CurrentPhase == TurnPhase.Settling);
            if (!ready && !settlingJump)
            {
                error = "Takeover is allowed before an action or after a jump, not during a committed weapon sequence.";
                return false;
            }

            m_AiTakeoverTeam = CurrentTeam;
            CurrentTeam.IsAiControlled = true;
            foreach (var input in FindObjectsByType<Mutiny.Presentation.MutinyPlayerInput>(FindObjectsSortMode.None))
                if (input.TurnManager == this)
                    input.ReleaseUncommittedInputForAiTakeover();

            MutinyAIController ai = CurrentTeam.GetComponent<MutinyAIController>();
            if (ai == null)
                ai = CurrentTeam.gameObject.AddComponent<MutinyAIController>();
            ai.SetTakeoverLuckOverride(luck);
            ai.enabled = true; // Start/Update use the usual turn gate, including jump settlement.
            MutinyDebugLog.Info("Turn", $"GM-08 AI takeover team={TeamLabel(CurrentTeam)} phase={CurrentPhase} afterJump={afterJump} luck={luck}", this);
            return true;
        }

        private void RestorePlayerControl()
        {
            if (m_AiTakeoverTeam != null)
            {
                m_AiTakeoverTeam.GetComponent<MutinyAIController>()?.SetTakeoverLuckOverride(null);
                m_AiTakeoverTeam.IsAiControlled = false;
                MutinyDebugLog.Info("Turn", $"GM-08 player control restored team={TeamLabel(m_AiTakeoverTeam)}", this);
            }
            m_AiTakeoverTeam = null;
        }

        private void OnDestroy() => RestorePlayerControl();

        private void Start()
        {
            if (Team1 == null || Team2 == null)
            {
                var levelRoot = FindAnyObjectByType<MutinyLevelRoot>();
                if (levelRoot != null)
                {
                    Team1 = levelRoot.Team1;
                    Team2 = levelRoot.Team2;
                }
            }

            if (CurrentPhase == TurnPhase.NotStarted && Team1 != null && Team2 != null)
            {
                StartGame();
            }
        }

        public void Initialize(MutinyTeam team1, MutinyTeam team2)
        {
            Team1 = team1;
            Team2 = team2;
            StartGame();
        }

        public void StartGame()
        {
            StartGameWithForcedFirst(MutinyNetworkManager.ForcedFirstTeamNumber);
        }

        /// <summary>
        /// Verification entry: drives the same production start with an explicit
        /// opening rule, so the "a forced side consumes no match RNG" contract can
        /// be checked without a live LAN session.
        /// </summary>
        internal void StartGameWithForcedFirstForVerification(int forcedFirstTeamNumber)
        {
            StartGameWithForcedFirst(forcedFirstTeamNumber);
        }

        private void StartGameWithForcedFirst(int forcedFirstTeamNumber)
        {
            RestorePlayerControl();
            if (!HasPlayableTeams())
            {
                CurrentTeam = null;
                CurrentPhase = TurnPhase.NotStarted;
                GameResult = GameOverResult.None;
                Debug.LogError("[MutinyTurnManager] Cannot start: both teams must contain at least one character.", this);
                return;
            }

            TurnCount = 0;
            GameResult = GameOverResult.None;
            InactivityTicks = 0;
            m_ActionCommittedThisTurn = false;

            RefreshCharacterCache();

            // Only the random rule draws from MutinyMatchRandom. A forced online
            // side must not advance the shared sequence on one machine only, or
            // the chest drops and flame scatter would diverge from the peer.
            CurrentTeam = forcedFirstTeamNumber == 0
                ? ResolveOpeningTeam(Team1, Team2, 0, MutinyMatchRandom.NextValue())
                : ResolveOpeningTeam(Team1, Team2, forcedFirstTeamNumber, 0f);
            CurrentPhase = TurnPhase.TurnActive;

            if (CurrentTeam != null)
            {
                CurrentTeam.StartTurn();
                OnTurnStarted?.Invoke(CurrentTeam);
            }

            Debug.Log($"[MutinyTurnManager] Started with Team 1={Team1.AliveCount}, Team 2={Team2.AliveCount} alive.", this);
            MutinyDebugLog.Info("Turn",
                $"game started active={TeamLabel(CurrentTeam)} phase={CurrentPhase} team1Alive={Team1.AliveCount} team2Alive={Team2.AliveCount}", this);

            // Music follows the visible front-end page, not the lifetime of a
            // turn manager that may initialize behind the title screen.
            // MutinyFrontendController switches to game_music only when the
            // player actually enters Gameplay.
        }

        public void RefreshCharacterCache()
        {
            m_AllCharacters.Clear();
            AddTeamCharacters(Team1);
            AddTeamCharacters(Team2);
        }

        public void NotifyActionStarted()
        {
            m_ActionCommittedThisTurn = true;
            InactivityTicks = 0;
            CurrentPhase = TurnPhase.ActionExecuting;
            MutinyDebugLog.Info("Turn",
                $"action committed team={TeamLabel(CurrentTeam)} selected={CharacterLabel(CurrentTeam?.SelectedCharacter)} phase={CurrentPhase}", this);
        }

        public void NotifyAmbientActivity()
        {
            InactivityTicks = 0;
        }

        public void PassTurn()
        {
            if (CurrentPhase == TurnPhase.GameOver || CurrentPhase == TurnPhase.NotStarted || CurrentTeam == null)
            {
                MutinyDebugLog.Warning("Turn",
                    $"pass rejected team={TeamLabel(CurrentTeam)} phase={CurrentPhase}", this);
                return;
            }

            MutinyDebugLog.Info("Turn", $"pass requested team={TeamLabel(CurrentTeam)}", this);
            CurrentTeam.FinishTurn();
            NotifyActionStarted();
        }

        private void Update()
        {
            if (IsAiTakeoverActive && (CurrentTeam != m_AiTakeoverTeam ||
                CurrentPhase == TurnPhase.GameOver || CurrentPhase == TurnPhase.NotStarted))
                RestorePlayerControl();
            if (CurrentPhase == TurnPhase.GameOver || CurrentPhase == TurnPhase.NotStarted)
                return;

            m_TickAccumulator += Time.deltaTime;
            while (m_TickAccumulator >= MutinyPhysics.TimeStep)
            {
                m_TickAccumulator -= MutinyPhysics.TimeStep;
                AdvanceSimulationTick();
            }
        }

        public void AdvanceSimulationTick()
        {
            bool allAtRest = CheckAllBodiesAtRest(out string restBlocker);

            if (allAtRest)
            {
                if (!string.IsNullOrEmpty(m_LastRestBlocker))
                {
                    MutinyDebugLog.Info("Turn", $"board settled after blocker={m_LastRestBlocker}", this);
                    m_LastRestBlocker = null;
                }
                m_RestBlockerTicks = 0;
                InactivityTicks++;

                // Flash increments first and evaluates only when inactivity > 10.
                if (HasReachedInactivityThreshold(InactivityTicks))
                {
                    InactivityTicks = 0;
                    EvaluateTurnOrGameOver();
                }
            }
            else
            {
                if (!string.Equals(m_LastRestBlocker, restBlocker, StringComparison.Ordinal))
                {
                    m_LastRestBlocker = restBlocker;
                    m_RestBlockerTicks = 0;
                    MutinyDebugLog.Info("Turn",
                        $"waiting team={TeamLabel(CurrentTeam)} committed={m_ActionCommittedThisTurn} blocker={restBlocker}", this);
                }
                m_RestBlockerTicks++;
                if (m_RestBlockerTicks == 50 ||
                    (m_RestBlockerTicks > 50 && (m_RestBlockerTicks - 50) % 125 == 0))
                {
                    MutinyDebugLog.Warning("Turn",
                        $"still waiting ticks={m_RestBlockerTicks} {DescribeRestBlocker(restBlocker)}", this);
                }

                InactivityTicks = 0;
                if (m_ActionCommittedThisTurn)
                    CurrentPhase = TurnPhase.ActionExecuting;
            }
        }

        public static bool HasReachedInactivityThreshold(int inactivityTicks)
        {
            return inactivityTicks > InactivitySettlingThreshold;
        }

        public bool CheckAllBodiesAtRest()
        {
            return CheckAllBodiesAtRest(out _);
        }

        private bool CheckAllBodiesAtRest(out string blocker)
        {
            // 1. Check characters
            for (int i = 0; i < m_AllCharacters.Count; i++)
            {
                var ch = m_AllCharacters[i];
                if (ch == null)
                    continue;

                if (ch.IsResolvingHealthDisplay)
                {
                    blocker = $"character:{CharacterLabel(ch)}";
                    return false;
                }

                if (!ch.IsAlive)
                    continue;

                if (ch.PhysicsBody != null && !ch.PhysicsBody.IsAtRest)
                {
                    blocker = $"character:{CharacterLabel(ch)}";
                    return false;
                }
            }

            // 2. Check active weapons / projectiles
            var activeWeapons = FindObjectsByType<MutinyWeapon>(FindObjectsSortMode.None);
            for (int i = 0; i < activeWeapons.Length; i++)
            {
                if (activeWeapons[i] is MutinyCannon cannon && cannon.IsFirePending &&
                    CurrentTeam != null &&
                    cannon.Owner == CurrentTeam.SelectedCharacter)
                {
                    // Character.advance keeps inactivity at zero while its selected
                    // weapon has not fired. The AI delay and player's pin return
                    // are both part of a committed Cannon action.
                    blocker = "cannon:fire-pending";
                    return false;
                }

                if (activeWeapons[i] is MutinyGunpowderBarrel barrel &&
                    barrel.IsAiPlacementActive && barrel.HasPendingPlacement)
                {
                    blocker = "gunpowderBarrel:ai-placement";
                    return false;
                }

                if (activeWeapons[i] is MutinyWoodenCrate crate &&
                    crate.IsAiPlacementActive && crate.HasPendingPlacement)
                {
                    blocker = "woodenCrate:ai-placement";
                    return false;
                }

                if (activeWeapons[i] is MutinyPiecesOfEight coins &&
                    coins.IsAwaitingNextCoin && coins.Owner != null && coins.Owner.IsAlive)
                {
                    // Character.advance keeps Controller.inactivity at zero while the
                    // same unfinished piecesOfEight instance waits for coin 2..8.
                    blocker = $"piecesOfEight:awaiting {coins.TimesFired}/{MutinyPiecesOfEight.TotalCoins}";
                    return false;
                }
            }

            for (int i = 0; i < activeWeapons.Length; i++)
            {
                var w = activeWeapons[i];
                if (w is MutinyMine mine && mine.IsStored && !mine.IsActive)
                    continue; // Mine.limitedToTurn=false: an armed idle mine persists across turns.
                if (w != null && w.IsFired && !w.IsFinished)
                {
                    blocker = $"weapon:{w.WeaponType}/{w.name} fired={w.IsFired} finished={w.IsFinished}";
                    return false;
                }
            }

            // 3. Check active explosions
            var activeExplosions = FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
            if (activeExplosions.Length > 0)
            {
                blocker = $"explosions:{activeExplosions.Length}";
                return false;
            }

            // 4. Check active treasure chests (air-drops falling or item collection in progress)
            if (MutinyTreasureChestManager.SystemEnabled)
            {
                var chests = FindObjectsByType<MutinyTreasureChest>(FindObjectsSortMode.None);
                for (int i = 0; i < chests.Length; i++)
                {
                    var chest = chests[i];
                    if (chest != null && !chest.IsFinished)
                    {
                        if (chest.IsFalling)
                        {
                            blocker = $"chest:{chest.name} (falling y={chest.PixelY:0}/{chest.FloorPixelY:0})";
                            return false;
                        }
                        if (chest.CollectingCharacter != null)
                        {
                            blocker = $"chest:{chest.name} (collecting remaining={chest.RemainingContents})";
                            return false;
                        }
                    }
                }
            }

            blocker = null;
            return true;
        }

        private string DescribeRestBlocker(string blocker)
        {
            if (!string.IsNullOrEmpty(blocker) && blocker.StartsWith("character:", StringComparison.Ordinal))
            {
                for (int i = 0; i < m_AllCharacters.Count; i++)
                {
                    MutinyCharacter character = m_AllCharacters[i];
                    if (character == null || !string.Equals(blocker, $"character:{CharacterLabel(character)}", StringComparison.Ordinal))
                        continue;

                    MutinyPhysicsBody body = character.PhysicsBody;
                    if (body == null)
                        return $"blocker={blocker} physicsBody=missing alive={character.IsAlive}";

                    PhysicsBodyState state = body.State;
                    return $"blocker={blocker} alive={character.IsAlive} drowned={character.IsDrowned} " +
                           $"health={character.Health:F0} shownHealth={character.ShownHealth:F0} landDeath={character.HasLandDeathPresentation} " +
                           $"inWater={body.IsInWater} position=({state.X:F2},{state.Y:F2}) " +
                           $"velocity=({state.VelocityX:F3},{state.VelocityY:F3})";
                }
            }

            return $"blocker={blocker}";
        }

        private void EvaluateTurnOrGameOver()
        {
            // Empty team lists mean the level has not finished initializing, or this is
            // a stale manager from a level being replaced. Neither condition is a draw.
            if (!HasPlayableTeams())
            {
                RestorePlayerControl();
                CurrentTeam = null;
                CurrentPhase = TurnPhase.NotStarted;
                GameResult = GameOverResult.None;
                Debug.LogError("[MutinyTurnManager] Stopped because team character data is unavailable.", this);
                return;
            }

            // 1. Check Win / Loss / Draw conditions
            bool team1Defeated = Team1 == null || Team1.IsDefeated;
            bool team2Defeated = Team2 == null || Team2.IsDefeated;

            if (team1Defeated || team2Defeated)
            {
                RestorePlayerControl();
                MutinyLevelController controller = GetComponentInParent<MutinyLevelController>() ??
                    FindAnyObjectByType<MutinyLevelController>();
                CurrentPhase = TurnPhase.GameOver;
                if (team1Defeated && team2Defeated)
                {
                    GameResult = GameOverResult.Draw;
                }
                else if (team2Defeated)
                {
                    GameResult = GameOverResult.Team1Wins;

                    if (controller != null)
                    {
                        if (controller.ActiveGameMode == MutinyGameMode.SinglePlayer)
                            Mutiny.Persistence.MutinySaveSystem.UnlockLevel(controller.CurrentLevelIndex + 1);
                        if (controller.ActiveGameMode == MutinyGameMode.SinglePlayer && Team2 != null && Team2.IsAiControlled)
                            controller.AwardSinglePlayerLevelWin(Team1);
                    }
                }
                else
                {
                    GameResult = GameOverResult.Team2Wins;
                }

                controller?.RecordGameResult(GameResult);

                MutinyDebugLog.Info("Turn",
                    $"END-POP result={GameResult} team1Alive={Team1.AliveCount} team2Alive={Team2.AliveCount} level={controller?.CurrentLevelIndex} persistentObjectsRetained=true", this);
                OnGameOver?.Invoke(GameResult);
                return;
            }

            // Merely having no selected character is the normal state at the start of
            // an original Mutiny turn. A turn may only finish after an action (or an
            // explicit pass) has actually been committed.
            if (!m_ActionCommittedThisTurn)
            {
                bool resumedFromWait = CurrentPhase != TurnPhase.TurnActive;
                CurrentPhase = TurnPhase.TurnActive;
                if (resumedFromWait)
                {
                    MutinyDebugLog.Info("Turn",
                        $"board idle without committed action; resumed {TeamLabel(CurrentTeam)}", this);
                }
                return;
            }

            // 2. Check if current team finished its turn actions
            if (CurrentTeam != null && CurrentTeam.IsTurnComplete())
            {
                MutinyTeam finishedTeam = CurrentTeam;
                RestorePlayerControl();
                OnTurnEnded?.Invoke(CurrentTeam);
                CurrentTeam.FinishTurn();

                // Switch turn
                CurrentTeam = (CurrentTeam == Team1) ? Team2 : Team1;
                TurnCount++;
                CurrentPhase = TurnPhase.TurnActive;
                m_ActionCommittedThisTurn = false;

                FindAnyObjectByType<MutinyTreasureChestManager>()?.TryDropNew();
                CurrentTeam.StartTurn();
                OnTurnStarted?.Invoke(CurrentTeam);
                MutinyDebugLog.Info("Turn",
                    $"turn switched from={TeamLabel(finishedTeam)} to={TeamLabel(CurrentTeam)} turnCount={TurnCount} phase={CurrentPhase}", this);
            }
            else
            {
                // Current character still has remaining actions
                CurrentPhase = TurnPhase.TurnActive;
                m_ActionCommittedThisTurn = false;
                CurrentTeam?.ContinueSelectedCharacterAfterAction();
                MutinyDebugLog.Info("Turn",
                    $"continuing team={TeamLabel(CurrentTeam)} selected={CharacterLabel(CurrentTeam?.SelectedCharacter)} canThrow={CurrentTeam?.SelectedCharacter?.CanThrow} canShoot={CurrentTeam?.SelectedCharacter?.CanShoot}", this);
            }
        }

        /// <summary>
        /// Opening side of a match. 1 or 2 is the host's forced choice; 0 keeps
        /// the original rule: single-player always opens with the red team
        /// (team 2 is AI), while local and online two-player use the seeded coin
        /// flip, which both peers resolve identically from the match seed.
        /// </summary>
        internal static MutinyTeam ResolveOpeningTeam(MutinyTeam team1, MutinyTeam team2,
            int forcedFirstTeamNumber, float randomValue)
        {
            if (forcedFirstTeamNumber == 1)
                return team1;
            if (forcedFirstTeamNumber == 2)
                return team2;
            return team2 != null && !team2.IsAiControlled && randomValue >= 0.5f ? team2 : team1;
        }

        private static string TeamLabel(MutinyTeam team)
        {
            return team == null ? "none" : $"T{team.TeamNumber}{(team.IsAiControlled ? "(AI)" : "(Human)")}";
        }

        private static string CharacterLabel(MutinyCharacter character)
        {
            return character == null ? "none" : $"{character.name}/{character.CharacterType}";
        }

        private bool HasPlayableTeams()
        {
            return Team1 != null && Team2 != null &&
                   Team1.Characters != null && Team1.Characters.Count > 0 &&
                   Team2.Characters != null && Team2.Characters.Count > 0;
        }

        private void AddTeamCharacters(MutinyTeam team)
        {
            if (team == null || team.Characters == null)
                return;

            for (int i = 0; i < team.Characters.Count; i++)
            {
                MutinyCharacter character = team.Characters[i];
                if (character != null && !m_AllCharacters.Contains(character))
                    m_AllCharacters.Add(character);
            }
        }
    }
}

namespace Mutiny.Diagnostics
{
    public static class MutinyDebugLog
    {
        public static bool Enabled = true;
        private static bool s_ForceDisabledInUnityEditorBatchMode = Application.isBatchMode;

        public static void Info(string module, string message, UnityEngine.Object context = null)
        {
            if (Enabled && (UnityEngine.Debug.isDebugBuild || Application.isEditor) && !s_ForceDisabledInUnityEditorBatchMode)
                UnityEngine.Debug.Log($"[Mutiny:{module}] {message}", context);
        }

        public static void Warning(string module, string message, UnityEngine.Object context = null)
        {
            if (Enabled && (UnityEngine.Debug.isDebugBuild || Application.isEditor) && !s_ForceDisabledInUnityEditorBatchMode)
                UnityEngine.Debug.LogWarning($"[Mutiny:{module}] {message}", context);
        }
    }
}
