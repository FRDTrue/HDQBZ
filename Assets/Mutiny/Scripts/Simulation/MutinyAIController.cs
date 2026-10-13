using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using UnityEngine;

namespace Mutiny.Simulation
{
    public enum AIMoveType
    {
        Pass,
        SelfThrow,
        ShootWeapon
    }

    public struct AIMove
    {
        public AIMoveType MoveType;
        public MutinyCharacter Character;
        public string WeaponType;
        public Vector2 LaunchVelocity;
        public Vector2 TargetPosition;
        public MutinyCharacter TargetCharacter;
        public float Score;
        public float SeagullFlightY;
        public int CannonRotationDegrees;
        public float[] SeagullShotXs;
        public Vector2[] BoxPossibilities;
        public bool UsesForcedWeaponSupply;
    }

    public enum MutinyAITurnGate
    {
        Cancel,
        Wait,
        Execute
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MutinyTeam))]
    public sealed class MutinyAIController : MonoBehaviour
    {
        public const float OriginalChestMoveRadiusPixels = 40f;
        public const float OriginalChestMoveBonus = 0.5f;

        // GM-only overlay. Keep the real per-character inventory untouched so
        // disabling the command restores the exact ammunition state.
        private static readonly string[] ForceableWeaponTypes =
        {
            "cherryBomb", "boulder", "dynamite", "piecesOfEight", "rumBottle",
            "banana", "parachuteBomb", "woodenCrate", "gunpowderBarrel", "seagull",
            "mine", "cannon", "anchor", "voodooDoll", "tidalWave"
        };

        public static int ForcedWeaponId { get; private set; }
        public static string ForcedWeaponType => ForcedWeaponId == 0 ? null : ForceableWeaponTypes[ForcedWeaponId - 1];
        /// <summary>How many weapons the GM weapon-cycle can force (ids 1..count; 0 clears).</summary>
        public static int ForceableWeaponCount => ForceableWeaponTypes.Length;
        public static bool ActionLogEnabled { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForcedWeaponOnPlayStart()
        {
            ForcedWeaponId = 0;
            ActionLogEnabled = false;
        }

        public static void SetActionLogEnabled(bool enabled) => ActionLogEnabled = enabled;

        public static bool TrySetForcedWeaponId(int weaponId)
        {
            if (weaponId < 0 || weaponId > ForceableWeaponTypes.Length)
                return false;
            ForcedWeaponId = weaponId;
            return true;
        }

        [Header("AI Settings")]
        [Tooltip("Retained for existing scenes. Flash uses exactly 50 character throw samples.")]
        public int TrajectorySamples = 50;

        [Header("AI Decision Replay")]
        public bool UseFixedDecisionSeed;
        public int FixedDecisionSeed;
        [Tooltip("Optional JSON trace created by a previous AI decision. Empty uses a fresh or fixed seed.")]
        public string ReplayDecisionTracePath;
        public bool SaveDecisionTrace = true;

        private MutinyTeam m_Team;
        private MutinyTurnManager m_TurnManager;
        private Coroutine m_TurnCoroutine;
        private bool m_LoggedMissingManager;
        private int m_DecisionSequence;
        private MutinyAIRandomStream m_Random;
        private MutinyAIDecisionTrace m_ReplayOverride;
        public MutinyAIDecisionTrace LastDecisionTrace { get; private set; }
        // GM-08 only: never mutate the level-authored per-character Luck.
        internal float? TakeoverLuckOverride { get; private set; }

        internal void SetTakeoverLuckOverride(float? luck) => TakeoverLuckOverride = luck;

        public const float MaxGmLuck = 99999f;
        public float? LevelLuckOverride { get; private set; }
        public static bool IsValidGmLuck(float luck) =>
            !float.IsNaN(luck) && !float.IsInfinity(luck) && luck >= 0f && luck <= MaxGmLuck;
        internal void SetLevelLuckOverride(float? luck) => LevelLuckOverride = luck;
        internal float GetEffectiveLuck(MutinyCharacter character) =>
            TakeoverLuckOverride ?? LevelLuckOverride ?? character.Luck;

        private sealed class DecisionWork
        {
            public int Id;
            public string Phase;
            public AIMove Best;
            public int CandidateCount;
            public string TracePath;
            public List<MutinyCharacter> Enemies;
            public List<MutinyCharacter> Allies;
            public string[,] Terrain;
            public int GridW;
            public int GridH;
            public float WaterY;
            public MutinyCharacter ContinuationCharacter;
            public IEnumerator Steps;
            public bool ForceNextFrame;
            public string ForcedWeaponType;
            public readonly Dictionary<MutinyCharacter, float> EffectiveLucks = new Dictionary<MutinyCharacter, float>();
        }

        private struct SelfThrowSample
        {
            public Vector2 Velocity;
            public Vector2 Landing;
        }

        public bool IsEvaluatingCandidates { get; private set; }

        private void Awake()
        {
            m_Team = GetComponent<MutinyTeam>();
        }

        private void Start()
        {
            BindTurnManager();
            TryStartCurrentTurn("Start");
        }

        private void Update()
        {
            if (m_TurnManager == null)
                BindTurnManager();

            if (m_TurnCoroutine == null &&
                ResolveTurnGate(m_TurnManager, m_Team) == MutinyAITurnGate.Execute)
            {
                BeginTurnRoutine("watchdog");
            }
        }

        private void OnDestroy()
        {
            IsEvaluatingCandidates = false;
            if (m_TurnManager != null)
            {
                m_TurnManager.OnTurnStarted -= HandleTurnStarted;
            }
        }

        private void OnDisable()
        {
            // A disabled MonoBehaviour stops its coroutine without executing the
            // cancellation branches below. Do not leave the camera's AI-thinking
            // priority gate latched if the team/controller is disabled mid-turn.
            IsEvaluatingCandidates = false;
            m_Random = null;
        }

        private void HandleTurnStarted(MutinyTeam activeTeam)
        {
            MutinyDebugLog.Info("AI",
                $"turn event activeTeam={TeamLabel(activeTeam)} self={TeamLabel(m_Team)} phase={m_TurnManager?.CurrentPhase}", this);
            if (activeTeam == m_Team && m_Team.IsAiControlled && !m_Team.IsDefeated)
                BeginTurnRoutine("turn event");
        }

        private IEnumerator ExecuteAITurnRoutine()
        {
            IsEvaluatingCandidates = true;
            MutinyDebugLog.Info("AI",
                $"thinking started team={TeamLabel(m_Team)} budget=30ms/frame", this);

            bool loggedWait = false;
            MutinyAITurnGate gate = ResolveTurnGate(m_TurnManager, m_Team);
            while (gate == MutinyAITurnGate.Wait)
            {
                if (!loggedWait)
                {
                    loggedWait = true;
                    MutinyDebugLog.Info("AI",
                        $"waiting for board readiness team={TeamLabel(m_Team)} phase={m_TurnManager.CurrentPhase}", this);
                }
                yield return null;
                gate = ResolveTurnGate(m_TurnManager, m_Team);
            }

            if (gate == MutinyAITurnGate.Cancel)
            {
                IsEvaluatingCandidates = false;
                MutinyDebugLog.Info("AI", "thinking cancelled because the active turn changed", this);
                m_TurnCoroutine = null;
                yield break;
            }

            var camera = FindAnyObjectByType<Mutiny.Presentation.MutinyCameraController>();

            DecisionWork work = null;
            Exception evaluationError = null;
            try { work = PrepareDecisionWork(); }
            catch (Exception exception) { evaluationError = exception; }

            var budget = Stopwatch.StartNew();
            while (evaluationError == null)
            {
                bool hasStep = false;
                try { hasStep = work.Steps.MoveNext(); }
                catch (Exception exception) { evaluationError = exception; }
                if (evaluationError != null || !hasStep)
                    break;
                if (!ShouldYieldDecisionFrame(work.ForceNextFrame, budget.ElapsedMilliseconds))
                    continue;
                work.ForceNextFrame = false;
                yield return null;
                gate = ResolveTurnGate(m_TurnManager, m_Team);
                if (gate == MutinyAITurnGate.Cancel)
                {
                    IsEvaluatingCandidates = false;
                    m_Random = null;
                    m_TurnCoroutine = null;
                    yield break;
                }
                while (gate == MutinyAITurnGate.Wait)
                {
                    yield return null;
                    gate = ResolveTurnGate(m_TurnManager, m_Team);
                }
                if (gate == MutinyAITurnGate.Cancel)
                {
                    IsEvaluatingCandidates = false;
                    m_Random = null;
                    m_TurnCoroutine = null;
                    yield break;
                }
                budget.Restart();
            }

            AIMove bestMove = CreatePassMove();
            if (evaluationError == null)
            {
                try { bestMove = FinishDecisionWork(work); }
                catch (Exception exception) { evaluationError = exception; }
            }
            if (evaluationError != null)
            {
                IsEvaluatingCandidates = false;
                m_Random = null;
                Debug.LogException(evaluationError, this);
                MutinyDebugLog.Warning("AI", "move evaluation failed; passing the turn", this);
                if (ActionLogEnabled)
                    Debug.Log($"[Mutiny:AI-Action] team={m_Team?.TeamNumber ?? 0} decision={work?.Id ?? 0} phase={work?.Phase ?? "unprepared"} action=Pass score=NA reason=evaluation-error:{evaluationError.GetType().Name} candidates={work?.CandidateCount ?? 0}", this);
                m_TurnManager.PassTurn();
                m_TurnCoroutine = null;
                yield break;
            }
            IsEvaluatingCandidates = false;

            if (bestMove.MoveType == AIMoveType.Pass || bestMove.Character == null)
            {
                MutinyDebugLog.Info("AI", "no safe or rewarding action found; passing the turn", this);
                LogCommittedAction(work, bestMove);
                m_TurnManager.PassTurn();
                m_TurnCoroutine = null;
                yield break;
            }

            m_Team.SelectCharacter(bestMove.Character);
            MutinyDebugLog.Info("AI",
                $"selected type={bestMove.MoveType} character={bestMove.Character.name} weapon={bestMove.WeaponType} score={bestMove.Score:0.00} velocity={bestMove.LaunchVelocity}", this);

            // Flash Team.advance assigns panToCharacter to the winning character
            // and does not call aiPerform until that target has cleared. The turn's
            // initial camera target may be a different pirate, so request it again
            // after the global candidate winner is known.
            if (camera != null)
            {
                BeginWinnerCameraPan(camera, bestMove.Character);
                while (camera.IsPanningToTurnTarget)
                {
                    yield return null;
                    gate = ResolveTurnGate(m_TurnManager, m_Team);
                    if (gate == MutinyAITurnGate.Cancel)
                    {
                        MutinyDebugLog.Info("AI", "selected move cancelled while panning to winner", this);
                        m_TurnCoroutine = null;
                        yield break;
                    }
                }
            }

            gate = ResolveTurnGate(m_TurnManager, m_Team);
            while (gate == MutinyAITurnGate.Wait)
            {
                yield return null;
                gate = ResolveTurnGate(m_TurnManager, m_Team);
            }

            if (gate == MutinyAITurnGate.Execute)
            {
                if (bestMove.Character != null && bestMove.Character.IsAlive)
                    LogCommittedAction(work, bestMove);
                ExecuteMove(bestMove);
            }
            else
                MutinyDebugLog.Info("AI", "selected move cancelled because active turn changed", this);

            m_TurnCoroutine = null;
        }

        private static bool BeginWinnerCameraPan(
            Mutiny.Presentation.MutinyCameraController camera,
            MutinyCharacter character)
        {
            if (camera == null || character == null)
                return false;
            camera.PanToCharacter(character);
            return camera.IsPanningToTurnTarget;
        }

        internal static bool BeginWinnerCameraPanForVerification(
            Mutiny.Presentation.MutinyCameraController camera,
            MutinyCharacter character)
            => BeginWinnerCameraPan(camera, character);

        public static MutinyAITurnGate ResolveTurnGate(MutinyTurnManager manager, MutinyTeam team)
        {
            if (manager == null || team == null || manager.CurrentTeam != team ||
                !team.IsAiControlled || team.IsDefeated ||
                manager.CurrentPhase == TurnPhase.GameOver ||
                manager.CurrentPhase == TurnPhase.NotStarted)
                return MutinyAITurnGate.Cancel;

            return manager.CurrentPhase == TurnPhase.TurnActive
                ? MutinyAITurnGate.Execute
                : MutinyAITurnGate.Wait;
        }

        public AIMove EvaluateBestMove()
        {
            DecisionWork work = PrepareDecisionWork();
            while (work.Steps.MoveNext()) { }
            return FinishDecisionWork(work);
        }

        internal static bool ShouldYieldDecisionFrame(bool forceNextFrame, long elapsedMilliseconds)
            => forceNextFrame || elapsedMilliseconds >= 30;

        internal IEnumerator EvaluateBestMoveStepsForVerification(Action<AIMove> completed)
        {
            DecisionWork work = PrepareDecisionWork();
            while (work.Steps.MoveNext())
                yield return null;
            completed?.Invoke(FinishDecisionWork(work));
        }

        internal void SetReplayTraceForVerification(MutinyAIDecisionTrace trace)
        {
            m_ReplayOverride = trace;
        }

        private DecisionWork PrepareDecisionWork()
        {
            if (m_Team == null)
                m_Team = GetComponent<MutinyTeam>();
            int decisionId = ++m_DecisionSequence;
            var work = new DecisionWork
            {
                Id = decisionId,
                Best = new AIMove { MoveType = AIMoveType.Pass, Score = float.NegativeInfinity },
                ForcedWeaponType = ForcedWeaponType
            };

            // Flash keeps dead characters in Team.characters when calculating the
            // opposing centroid; individual distance scoring then checks alive.
            var allChars = FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
            var enemies = new List<MutinyCharacter>();
            var allies = new List<MutinyCharacter>();

            for (int i = 0; i < allChars.Length; i++)
            {
                var ch = allChars[i];
                if (ch != null && ch.PhysicsBody != null)
                {
                    if (ch.TeamIndex != m_Team.TeamNumber)
                        enemies.Add(ch);
                    else
                        allies.Add(ch);
                }
            }

            // Team.advance concatenates candidates in Team.characters order.
            allies.Clear();
            foreach (MutinyCharacter character in m_Team.Characters)
                if (character != null && character.PhysicsBody != null)
                    allies.Add(character);

            work.Enemies = enemies;
            work.Allies = allies;
            foreach (MutinyCharacter character in allies)
                work.EffectiveLucks[character] = GetEffectiveLuck(character);

            if (enemies.Count == 0 || allies.Count == 0 || !ContainsAlive(enemies) || !ContainsAlive(allies))
            {
                work.Phase = "invalid-board";
                work.Steps = EmptyDecisionSteps();
                BeginDecisionRandom(work);
                return work;
            }

            // Retrieve terrain and water level
            string[,] terrainGrid = null;
            int gridW = 50;
            int gridH = 20;

            var controller = FindAnyObjectByType<MutinyLevelController>();
            if (controller != null && controller.LevelXml != null)
            {
                var lvlData = MutinyLevelXmlParser.Parse(controller.LevelXml.text);
                terrainGrid = lvlData.Terrain;
                gridW = lvlData.Width;
                gridH = lvlData.Height;
            }

            float waterPixelY = float.PositiveInfinity;
            var levelRoot = FindAnyObjectByType<MutinyLevelRoot>();
            if (levelRoot != null)
            {
                waterPixelY = -levelRoot.WaterLevelY * MutinyPhysics.PixelsPerUnit;
            }

            // Two-Phase check:
            // If a character was already chosen and moved (CanThrow consumed, CanShoot remaining),
            // only evaluate shooting for this selected character.
            work.Terrain = terrainGrid;
            work.GridW = gridW;
            work.GridH = gridH;
            work.WaterY = waterPixelY;
            MutinyCharacter activeChar = m_Team.SelectedCharacter;
            work.ContinuationCharacter = activeChar != null && activeChar.IsAlive &&
                !activeChar.CanThrow && activeChar.CanShoot ? activeChar : null;
            work.Phase = work.ContinuationCharacter != null ? "continuation" : "first-action";
            BeginDecisionRandom(work);
            work.Steps = EvaluateDecisionSteps(work);
            return work;
        }

        private static IEnumerator EmptyDecisionSteps() { yield break; }

        private IEnumerator EvaluateDecisionSteps(DecisionWork work)
        {
            List<MutinyCharacter> actors = work.ContinuationCharacter != null
                ? new List<MutinyCharacter> { work.ContinuationCharacter }
                : work.Allies;
            for (int actorIndex = 0; actorIndex < actors.Count; actorIndex++)
            {
                MutinyCharacter actor = actors[actorIndex];
                if (actor == null || !actor.IsAlive)
                {
                    if (actorIndex < actors.Count - 1)
                    {
                        work.ForceNextFrame = true;
                        yield return null;
                        work.ForceNextFrame = false;
                    }
                    continue;
                }
                if (work.ContinuationCharacter == null && actor.CanThrow)
                {
                    // Character.aiThink generates all 50 throws first, then scores
                    // one landing per call. This also preserves random draw order.
                    List<SelfThrowSample> throws = BuildSelfThrowSamples(actor, work);
                    yield return null;
                    foreach (SelfThrowSample sample in throws)
                    {
                        if (!actor.IsAlive)
                            break;
                        AIMove best = work.Best;
                        int count = work.CandidateCount;
                        EvaluateSelfThrowSample(actor, sample, work, ref best, ref count);
                        work.Best = best;
                        work.CandidateCount = count;
                        yield return null;
                    }
                }

                if (!actor.CanShoot)
                {
                    if (actorIndex < actors.Count - 1)
                    {
                        work.ForceNextFrame = true;
                        yield return null;
                        work.ForceNextFrame = false;
                    }
                    continue;
                }
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string weapon in EnumerateWeaponChoices(actor, work.ForcedWeaponType))
                {
                    if (!actor.IsAlive)
                        break;
                    if (!HasEffectiveWeapon(actor, weapon, work.ForcedWeaponType) || !seen.Add(weapon))
                        continue;
                    AIMove best = work.Best;
                    int count = work.CandidateCount;
                    EvaluateCharacterWeapons(actor, work.Enemies, work.Allies, work.Terrain,
                        work.GridW, work.GridH, work.WaterY, ref best, ref count, weapon,
                        work.ForcedWeaponType, work.EffectiveLucks[actor]);
                    work.Best = best;
                    work.CandidateCount = count;
                    yield return null;
                }
                // Team.advance breaks after the current unfinished character,
                // even when it finished before using its whole 30 ms budget.
                if (actorIndex < actors.Count - 1)
                {
                    work.ForceNextFrame = true;
                    yield return null;
                    work.ForceNextFrame = false;
                }
            }
        }

        private AIMove FinishDecisionWork(DecisionWork work)
        {
            AIMove move = work.Phase == "continuation" &&
                (work.Best.Character == null || work.Best.Score <= 0f)
                ? CreatePassMove() : work.Best;
            if (m_Random != null)
            {
                m_Random.AssertReplayComplete();
                m_Random.Trace.Winner = $"{move.MoveType}:{move.Character?.name}:{move.WeaponType}:{move.Score:R}";
                LastDecisionTrace = m_Random.Trace;
                if (SaveDecisionTrace)
                {
                    try
                    {
                        string directory = Path.Combine(Application.persistentDataPath, "ai-decisions");
                        Directory.CreateDirectory(directory);
                        string path = Path.Combine(directory,
                            $"team-{m_Team.TeamNumber}-decision-{work.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.json");
                        File.WriteAllText(path, JsonUtility.ToJson(m_Random.Trace, true));
                        work.TracePath = path;
                        MutinyDebugLog.Info("AI-Decision", $"trace={path} seed={m_Random.Trace.Seed}", this);
                    }
                    catch (IOException exception)
                    {
                        MutinyDebugLog.Warning("AI-Decision", $"cannot save trace: {exception.Message}", this);
                    }
                }
                m_Random = null;
            }
            LogDecisionSummary(work.Id, work.Phase, work.CandidateCount, move);
            return move;
        }

        private void BeginDecisionRandom(DecisionWork work)
        {
            LastDecisionTrace = null;
            MutinyAIDecisionTrace replay = m_ReplayOverride;
            m_ReplayOverride = null;
            if (!string.IsNullOrWhiteSpace(ReplayDecisionTracePath))
            {
                replay = JsonUtility.FromJson<MutinyAIDecisionTrace>(File.ReadAllText(ReplayDecisionTracePath));
                ReplayDecisionTracePath = null; // A trace describes one decision, not the next phase.
                if (replay == null || replay.TeamNumber != m_Team.TeamNumber || replay.Phase != work.Phase)
                    throw new InvalidDataException("AI replay trace team or phase does not match this decision");
            }
            if (replay != null && (replay.TeamNumber != m_Team.TeamNumber || replay.Phase != work.Phase))
                throw new InvalidDataException("AI replay trace team or phase does not match this decision");
            int seed = replay != null ? replay.Seed :
                UseFixedDecisionSeed ? FixedDecisionSeed : UnityEngine.Random.Range(1, int.MaxValue);
            m_Random = new MutinyAIRandomStream(seed, work.Id, m_Team.TeamNumber, work.Phase, replay);
        }

        private void LogDecisionSummary(int decisionId, string phase, int candidateCount, AIMove move)
        {
            string character = move.Character != null ? move.Character.name : "none";
            string weapon = string.IsNullOrEmpty(move.WeaponType) ? "none" : move.WeaponType;
            MutinyDebugLog.Info("AI-Decision",
                $"id={decisionId} phase={phase} candidates={candidateCount} type={move.MoveType} character={character} weapon={weapon} score={move.Score:0.0000} velocity=({move.LaunchVelocity.x:0.00},{move.LaunchVelocity.y:0.00}) target=({move.TargetPosition.x:0.0},{move.TargetPosition.y:0.0})",
                this);
        }

        private void LogCommittedAction(DecisionWork work, AIMove move)
        {
            if (!ActionLogEnabled)
                return;

            MutinyAIDecisionTrace trace = LastDecisionTrace;
            var line = new StringBuilder(384);
            line.Append("[Mutiny:AI-Action] team=").Append(m_Team.TeamNumber)
                .Append(" decision=").Append(work.Id)
                .Append(" seed=").Append(trace != null ? trace.Seed.ToString(CultureInfo.InvariantCulture) : "NA")
                .Append(" phase=").Append(work.Phase)
                .Append(" candidates=").Append(work.CandidateCount)
                .Append(" action=").Append(move.MoveType)
                .Append(" character=").Append(move.Character != null ? move.Character.name : "none")
                .Append(" weapon=").Append(string.IsNullOrEmpty(move.WeaponType) ? "none" : move.WeaponType)
                .Append(" score=").Append(Number(move.Score));

            if (move.MoveType != AIMoveType.Pass)
            {
                float luck = work.EffectiveLucks[move.Character];
                line.Append(" luck=").Append(Number(luck))
                    .Append(" start=").Append(Point(PositionOf(move.Character)))
                    .Append(" forcedWeapon=").Append(work.ForcedWeaponType ?? "none")
                    .Append(" forcedSupply=").Append(move.UsesForcedWeaponSupply);
                bool usesArcSamples = move.MoveType == AIMoveType.ShootWeapon &&
                    (MutinyWeaponFactoryCanFire(move.WeaponType) ||
                     string.Equals(move.WeaponType, "anchor", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(move.WeaponType, "cannon", StringComparison.OrdinalIgnoreCase));
                if (usesArcSamples)
                {
                    int samples = Mathf.FloorToInt(luck * m_Team.Characters.Count / Mathf.Max(1, m_Team.AliveCount));
                    line.Append(" weaponSamples=").Append(samples);
                }
                bool hasVelocity = move.MoveType == AIMoveType.SelfThrow ||
                    MutinyWeaponFactoryCanFire(move.WeaponType) ||
                    string.Equals(move.WeaponType, "cannon", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(move.WeaponType, "voodooDoll", StringComparison.OrdinalIgnoreCase);
                if (hasVelocity)
                    line.Append(" velocity=").Append(Point(move.LaunchVelocity));
                if (move.MoveType == AIMoveType.SelfThrow || hasVelocity ||
                    string.Equals(move.WeaponType, "anchor", StringComparison.OrdinalIgnoreCase))
                    line.Append(" predictedTarget=").Append(Point(move.TargetPosition));
                if (move.TargetCharacter != null)
                    line.Append(" targetCharacter=").Append(move.TargetCharacter.name);
                if (string.Equals(move.WeaponType, "cannon", StringComparison.OrdinalIgnoreCase))
                    line.Append(" cannonAngle=").Append(move.CannonRotationDegrees);
                if (string.Equals(move.WeaponType, "seagull", StringComparison.OrdinalIgnoreCase))
                    line.Append(" seagullFlightY=").Append(Number(move.SeagullFlightY))
                        .Append(" seagullShotXs=").Append(Numbers(move.SeagullShotXs));
                if (move.BoxPossibilities != null && move.BoxPossibilities.Length > 0)
                    line.Append(" boxPositions=").Append(Points(move.BoxPossibilities));
                if (string.Equals(move.WeaponType, "tidalWave", StringComparison.OrdinalIgnoreCase))
                    line.Append(" waveStart=(-550,")
                        .Append(Number(move.Character.PhysicsBody.WaterPixelY)).Append(')');
            }

            line.Append(" bestByAction=").Append(BestScoresByAction(trace));
            if (!string.IsNullOrEmpty(work.TracePath))
                line.Append(" trace=").Append(work.TracePath);
            if (move.MoveType == AIMoveType.Pass)
            {
                if (work.Best.Character != null)
                    line.Append(" bestRejectedAction=").Append(work.Best.MoveType)
                        .Append(" bestRejectedCharacter=").Append(work.Best.Character.name)
                        .Append(" bestRejectedWeapon=")
                        .Append(string.IsNullOrEmpty(work.Best.WeaponType) ? "none" : work.Best.WeaponType)
                        .Append(" bestRejectedTarget=").Append(Point(work.Best.TargetPosition))
                        .Append(" luck=").Append(Number(work.EffectiveLucks[work.Best.Character]));
                line.Append(" bestRejectedScore=")
                    .Append(work.Best.Character != null ? Number(work.Best.Score) : "NA")
                    .Append(" reason=")
                    .Append(work.Phase == "continuation" && work.Best.Character != null
                        ? "continuation-best-score-not-positive"
                        : "no-valid-candidate");
            }
            else
            {
                line.Append(" reason=").Append(work.Phase == "continuation"
                    ? "highest-score-above-zero"
                    : "highest-score-first-action-no-positive-threshold")
                    .Append(" tieRule=first-candidate-wins");
            }
            // GM-10 must remain visible in a player build; the ordinary debug
            // logger is intentionally disabled outside Editor/development builds.
            Debug.Log(line.ToString(), this);
        }

        private static string BestScoresByAction(MutinyAIDecisionTrace trace)
        {
            if (trace == null || trace.Candidates == null || trace.Candidates.Count == 0)
                return "[]";
            var order = new List<string>();
            var best = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (MutinyAICandidateRecord candidate in trace.Candidates)
            {
                string action = candidate.MoveType == nameof(AIMoveType.SelfThrow)
                    ? "jump" : string.IsNullOrEmpty(candidate.Weapon) ? candidate.MoveType : candidate.Weapon;
                if (!best.TryGetValue(action, out float previous))
                {
                    order.Add(action);
                    best.Add(action, candidate.Score);
                }
                else if (candidate.Score > previous)
                    best[action] = candidate.Score;
            }
            var result = new StringBuilder("[");
            for (int i = 0; i < order.Count; i++)
            {
                if (i > 0) result.Append(',');
                result.Append(order[i]).Append(':').Append(Number(best[order[i]]));
            }
            return result.Append(']').ToString();
        }

        private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Point(Vector2 point) => "(" + Number(point.x) + "," + Number(point.y) + ")";

        private static string Numbers(float[] values)
        {
            if (values == null || values.Length == 0) return "[]";
            var result = new StringBuilder("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) result.Append(',');
                result.Append(Number(values[i]));
            }
            return result.Append(']').ToString();
        }

        private static string Points(Vector2[] values)
        {
            if (values == null || values.Length == 0) return "[]";
            var result = new StringBuilder("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) result.Append(',');
                result.Append(Point(values[i]));
            }
            return result.Append(']').ToString();
        }

        private void EvaluateCharacterWeapons(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            ref AIMove bestMove,
            ref int candidateCount,
            string onlyWeaponType = null,
            string forcedWeaponType = null,
            float? decisionLuck = null)
        {
            if (m_Team == null)
                m_Team = GetComponent<MutinyTeam>();
            Vector2 shooterPos = new Vector2(shooter.PhysicsBody.State.X, shooter.PhysicsBody.State.Y);

            float effectiveLuck = decisionLuck ?? GetEffectiveLuck(shooter);
            int samples = Mathf.FloorToInt(effectiveLuck * m_Team.Characters.Count / Mathf.Max(1, m_Team.AliveCount));
            List<PhysicsBoxObstacle> simulationBoxes = MutinyBoxRegistry.GetObstacles();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string weaponType in EnumerateWeaponChoices(shooter, forcedWeaponType))
            {
                if (onlyWeaponType != null && !string.Equals(onlyWeaponType, weaponType, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!HasEffectiveWeapon(shooter, weaponType, forcedWeaponType) || !seen.Add(weaponType))
                    continue;

                if (string.Equals(weaponType, "tidalWave", StringComparison.OrdinalIgnoreCase))
                {
                    float score = ScoreTidalWave(enemies, allies, waterPixelY);
                    ConsiderCandidate(ref bestMove, new AIMove { MoveType = AIMoveType.ShootWeapon, Character = shooter, WeaponType = weaponType, Score = score }, ref candidateCount);
                    continue;
                }

                if (string.Equals(weaponType, "voodooDoll", StringComparison.OrdinalIgnoreCase))
                {
                    EvaluateVoodoo(shooter, enemies, terrainGrid, gridW, gridH, waterPixelY, ref bestMove, ref candidateCount);
                    continue;
                }

                if (string.Equals(weaponType, "seagull", StringComparison.OrdinalIgnoreCase))
                {
                    EvaluateSeagull(shooter, enemies, allies, terrainGrid, gridW, gridH, waterPixelY,
                        ref bestMove, ref candidateCount);
                    continue;
                }

                if (string.Equals(weaponType, "gunpowderBarrel", StringComparison.OrdinalIgnoreCase))
                {
                    EvaluateBoxWeapon(shooter, enemies, allies, terrainGrid, gridW, gridH, weaponType,
                        ref bestMove, ref candidateCount);
                    continue;
                }

                if (string.Equals(weaponType, "woodenCrate", StringComparison.OrdinalIgnoreCase))
                {
                    EvaluateBoxWeapon(shooter, enemies, allies, terrainGrid, gridW, gridH, weaponType,
                        ref bestMove, ref candidateCount);
                    continue;
                }

                if (string.Equals(weaponType, "anchor", StringComparison.OrdinalIgnoreCase))
                {
                    EvaluateAnchor(shooter, enemies, allies, terrainGrid, gridW, gridH, waterPixelY,
                        samples, ref bestMove, ref candidateCount);
                    continue;
                }

                if (string.Equals(weaponType, "cannon", StringComparison.OrdinalIgnoreCase))
                {
                    EvaluateCannon(shooter, enemies, allies, terrainGrid, gridW, gridH, waterPixelY,
                        samples, ref bestMove, ref candidateCount);
                    continue;
                }

                if (!MutinyWeaponFactoryCanFire(weaponType))
                    continue;

                PhysicsBodyState formalTemplate = CreateFormalWeaponPredictionTemplate(shooter, weaponType);
                for (int s = 0; s < samples; s++)
                {
                    Vector2 velocity = RandomArc(MutinyWeaponFactory.GetTwangMaxForce(weaponType));
                    PhysicsBodyState body = formalTemplate;
                    body.X = shooterPos.x;
                    body.Y = shooterPos.y;
                    body.VelocityX = velocity.x;
                    body.VelocityY = velocity.y;
                    Vector2 impact = SimulateWeaponImpact(body, weaponType, terrainGrid, gridW, gridH,
                        waterPixelY, simulationBoxes, out _);
                    float score = ScoreGenericWeaponCandidate(weaponType, impact, enemies, allies);
                    ConsiderCandidate(ref bestMove, new AIMove
                    {
                        MoveType = AIMoveType.ShootWeapon,
                        Character = shooter,
                        WeaponType = weaponType,
                        LaunchVelocity = velocity,
                        TargetPosition = impact,
                        Score = score
                    }, ref candidateCount);
                }
            }

            if (bestMove.MoveType == AIMoveType.ShootWeapon &&
                string.Equals(bestMove.WeaponType, forcedWeaponType, StringComparison.OrdinalIgnoreCase))
                bestMove.UsesForcedWeaponSupply = true;
        }

        private static IEnumerable<string> EnumerateWeaponChoices(MutinyCharacter character, string forcedWeaponType)
        {
            if (forcedWeaponType != null)
            {
                yield return forcedWeaponType;
                yield break;
            }
            foreach (KeyValuePair<string, int> entry in character.WeaponInventory)
                yield return entry.Key;
        }

        private static bool HasEffectiveWeapon(MutinyCharacter character, string weaponType, string forcedWeaponType)
            => forcedWeaponType != null
                ? string.Equals(weaponType, forcedWeaponType, StringComparison.OrdinalIgnoreCase)
                : character.HasWeapon(weaponType);

        // Verification seam for the production inventory dispatcher and candidate
        // generators. Tests supply an isolated board but do not duplicate any AI
        // formula or bypass the real per-weapon routing above.
        internal AIMove EvaluateCharacterWeaponsForVerification(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            out int candidateCount)
        {
            var bestMove = new AIMove
            {
                MoveType = AIMoveType.Pass,
                Score = float.NegativeInfinity
            };
            candidateCount = 0;
            EvaluateCharacterWeapons(shooter, enemies, allies, terrainGrid, gridW, gridH,
                waterPixelY, ref bestMove, ref candidateCount, forcedWeaponType: ForcedWeaponType);
            return bestMove;
        }

        private void EvaluateBoxWeapon(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            string weaponType,
            ref AIMove bestMove,
            ref int candidateCount)
        {
            // BoxWeapon.aiSimulation: sample ten locations around living enemies.
            // A candidate is admitted only when the same production CanPlace test
            // accepts it; Character.aiThink needs at least three candidates.
            if (terrainGrid == null || gridW <= 0 || gridH <= 0)
                return;

            float allyAverageX = 0f;
            int allyCount = 0;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null && allies[i].IsAlive && allies[i].PhysicsBody != null)
                {
                    allyAverageX += PositionOf(allies[i]).x;
                    allyCount++;
                }
            }
            if (allyCount == 0)
                return;
            allyAverageX /= allyCount;

            var livingEnemies = new List<MutinyCharacter>();
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null && enemies[i].IsAlive && enemies[i].PhysicsBody != null)
                    livingEnemies.Add(enemies[i]);
            if (livingEnemies.Count == 0)
                return;

            MutinyWeapon probe = MutinyWeaponFactory.SpawnWeapon(weaponType, shooter);
            if (probe == null)
                return;
            try
            {
                probe.PhysicsBody.SetTerrain(terrainGrid, gridW, gridH);
                var possibilities = new List<Vector2>(10);
                for (int sample = 0; sample < 10; sample++)
                {
                    MutinyCharacter enemy = livingEnemies[NextRandomInt(0, livingEnemies.Count)];
                    Vector2 target = PositionOf(enemy);
                    float sign = Mathf.Approximately(allyAverageX, target.x) ? 0f : Mathf.Sign(allyAverageX - target.x);
                    float offsetX = sign * NextRandomInt(16, 64);
                    float offsetY = NextRandomInt(-50, 50);
                    Vector2 candidate = new Vector2(target.x + offsetX, target.y + offsetY);
                    if (CanPlaceBoxWeapon(probe, candidate))
                        possibilities.Add(candidate);
                }

                if (possibilities.Count >= 3)
                {
                    ConsiderCandidate(ref bestMove, new AIMove
                    {
                        MoveType = AIMoveType.ShootWeapon,
                        Character = shooter,
                        WeaponType = weaponType,
                        Score = NextRandomFloat(0f, 1f),
                        BoxPossibilities = possibilities.ToArray()
                    }, ref candidateCount);
                }
            }
            finally
            {
                probe.gameObject.SetActive(false);
                Destroy(probe.gameObject);
            }
        }

        private static bool CanPlaceBoxWeapon(MutinyWeapon probe, Vector2 candidate)
        {
            if (probe is MutinyGunpowderBarrel barrel)
                return barrel.CanPlace(candidate);
            if (probe is MutinyWoodenCrate crate)
                return crate.CanPlace(candidate);
            return false;
        }

        private void EvaluateAnchor(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            int samples,
            ref AIMove bestMove,
            ref int candidateCount)
        {
            // Anchor.randomThrows: choose an x across the whole level, start at
            // y=-200 and fall vertically at 40 px/tick. Only floor contacts become
            // candidates; water misses are discarded before generic scoring.
            if (terrainGrid == null || gridW <= 0 || gridH <= 0 || float.IsInfinity(waterPixelY))
                return;

            int levelWidthPixels = gridW * (int)MutinyPhysics.PixelsPerUnit;
            List<PhysicsBoxObstacle> boxes = MutinyBoxRegistry.GetObstacles();
            for (int sample = 0; sample < Mathf.Max(0, samples); sample++)
            {
                int targetX = NextRandomInt(0, levelWidthPixels);
                PhysicsBodyState body = PhysicsBodyState.CreateDefault(targetX, MutinyAnchor.DropStartYPixels);
                body.Weight = 0f;
                body.LeftExtent = body.RightExtent = 48f;
                body.TopExtent = 96f;
                body.BottomExtent = 0f;
                body.HitsBoxes = true;

                bool hitFloor = false;
                // The source loop is bounded by water. Keep an additional guard so
                // malformed verification terrain cannot hang the whole AI turn.
                for (int tick = 0; tick < 1024 && body.Y < waterPixelY; tick++)
                {
                    body.VelocityX = 0f;
                    body.VelocityY = MutinyAnchor.DropSpeedPixelsPerTick;
                    StepResult result = MutinyPhysics.Step(ref body, terrainGrid, gridW, gridH, boxes);
                    if (result.HitFloor)
                    {
                        hitFloor = true;
                        break;
                    }
                }

                if (!hitFloor)
                    continue;

                Vector2 landing = new Vector2(body.X, body.Y);
                float score = ScoreGenericWeaponCandidate("anchor", landing, enemies, allies);
                ConsiderCandidate(ref bestMove, new AIMove
                {
                    MoveType = AIMoveType.ShootWeapon,
                    Character = shooter,
                    WeaponType = "anchor",
                    TargetPosition = landing,
                    Score = score
                }, ref candidateCount);
            }
        }

        private void EvaluateCannon(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            int samples,
            ref AIMove bestMove,
            ref int candidateCount)
        {
            // Cannon.randomThrows: choose a point within dragRange / 2 about
            // owner+(0,-100), advance the 10px cannon body through Solid collision,
            // then simulate a 30-force weight-zero cannonball from the corrected spot.
            // Cannon.aiPerform later repeats the same collision-aware body move.
            Vector2 owner = PositionOf(shooter);
            List<PhysicsBoxObstacle> placementBoxes = MutinyBoxRegistry.GetObstacles();
            int count = Mathf.Max(0, samples);
            for (int i = 0; i < count; i++)
            {
                int placementAngle = NextRandomInt(0, 360);
                float distance = NextRandomFloat(0f, 1f) * 65f; // Weapon.dragRange / 2
                float placementRadians = placementAngle * Mathf.Deg2Rad;
                Vector2 requestedPlacement = owner + new Vector2(
                    Mathf.Cos(placementRadians) * distance,
                    MutinyCannon.PlacementOffsetY + Mathf.Sin(placementRadians) * distance);

                // Cannon.randomThrows samples a second independent angle after
                // resolving placement. The placement direction does not aim the shot.
                int firingAngle = NextRandomInt(0, 360);
                float firingRadians = firingAngle * Mathf.Deg2Rad;
                Vector2 velocity = new Vector2(Mathf.Cos(firingRadians), Mathf.Sin(firingRadians)) * MutinyCannon.FireStrength;

                PhysicsBodyState cannonBody = PhysicsBodyState.CreateDefault(
                    owner.x, owner.y + MutinyCannon.InitialEquipmentOffsetY);
                cannonBody.Weight = 0f;
                cannonBody.HitsBoxes = true;
                cannonBody.LeftExtent = cannonBody.RightExtent =
                    cannonBody.TopExtent = cannonBody.BottomExtent = 10f;
                cannonBody.VelocityX = requestedPlacement.x - cannonBody.X;
                cannonBody.VelocityY = requestedPlacement.y - cannonBody.Y;
                MutinyPhysics.Step(ref cannonBody, terrainGrid, gridW, gridH, placementBoxes);
                Vector2 resolvedPlacement = new Vector2(cannonBody.X, cannonBody.Y);

                PhysicsBodyState ball = PhysicsBodyState.CreateDefault(resolvedPlacement.x, resolvedPlacement.y);
                ball.Weight = 0f;
                ball.HitsBoxes = true;
                ball.LeftExtent = ball.RightExtent = ball.TopExtent = ball.BottomExtent = 10f;
                ball.VelocityX = velocity.x;
                ball.VelocityY = velocity.y;
                Vector2 impact = SimulateWeaponImpact(ball, "cannonball", terrainGrid, gridW, gridH,
                    waterPixelY, placementBoxes, out _);
                float score = ScoreGenericWeaponCandidate("cannon", impact, enemies, allies);
                ConsiderCandidate(ref bestMove, new AIMove
                {
                    MoveType = AIMoveType.ShootWeapon,
                    Character = shooter,
                    WeaponType = "cannon",
                    LaunchVelocity = velocity,
                    // aiPerform receives the requested point and resolves it through
                    // the live cannon body against the current terrain/boxes again.
                    TargetPosition = requestedPlacement,
                    CannonRotationDegrees = firingAngle,
                    Score = score
                }, ref candidateCount);
            }
        }

        private void EvaluateSeagull(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            ref AIMove bestMove,
            ref int candidateCount)
        {
            // Seagull.aiSimulation: fly 100..199 px above the highest living
            // enemy, sample ten sorted x positions, then keep only positive shots.
            float highestEnemyY = float.PositiveInfinity;
            for (int i = 0; i < enemies.Count; i++)
            {
                MutinyCharacter enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && enemy.PhysicsBody != null)
                    highestEnemyY = Mathf.Min(highestEnemyY, enemy.PhysicsBody.State.Y);
            }
            if (float.IsInfinity(highestEnemyY))
                return;

            float flightY = highestEnemyY - 100f - NextRandomInt(0, 100);
            int levelWidthPixels = gridW * (int)MutinyPhysics.PixelsPerUnit;
            var candidates = new List<float>(10);
            for (int i = 0; i < 10; i++)
                candidates.Add(NextRandomInt(0, levelWidthPixels));
            candidates.Sort();

            var acceptedShots = new List<float>();
            float success = 0f;
            List<PhysicsBoxObstacle> simulationBoxes = MutinyBoxRegistry.GetObstacles();
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 impact = SimulateSeagullShotImpact(
                    candidates[i] + MutinySeagull.OriginalShotXOffset,
                    flightY,
                    terrainGrid,
                    gridW,
                    gridH,
                    waterPixelY,
                    simulationBoxes);
                float shotScore = ScoreSeagullShot(impact, enemies, false) +
                                  ScoreSeagullShot(impact, allies, true);
                if (shotScore > 0f)
                {
                    acceptedShots.Add(candidates[i]);
                    success += shotScore;
                }
            }

            // Character.as only creates an AI move when aiSimulation returned
            // more than one accepted firing coordinate.
            if (acceptedShots.Count <= 1)
                return;

            ConsiderCandidate(ref bestMove, new AIMove
            {
                MoveType = AIMoveType.ShootWeapon,
                Character = shooter,
                WeaponType = "seagull",
                Score = success,
                SeagullFlightY = flightY,
                SeagullShotXs = acceptedShots.ToArray()
            }, ref candidateCount);
        }

        private static Vector2 SimulateSeagullShotImpact(
            float startX,
            float startY,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            IReadOnlyList<PhysicsBoxObstacle> boxes)
        {
            PhysicsBodyState body = PhysicsBodyState.CreateDefault(startX, startY);
            body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = MutinySeagullFire.OriginalExtent;
            body.Weight = MutinySeagullFire.OriginalWeight;
            body.VelocityX = MutinySeagull.OriginalFlightSpeed;
            body.VelocityY = 0f;
            body.HitsBoxes = true;

            for (int tick = 0; tick < 512 && body.Y < waterPixelY; tick++)
            {
                StepResult result = MutinyPhysics.Step(ref body, terrainGrid, gridW, gridH, boxes);
                if (result.HitFloor || result.HitCeiling || result.HitLeftWall || result.HitRightWall)
                    break;
            }
            return new Vector2(body.X, body.Y);
        }

        private static float ScoreSeagullShot(
            Vector2 impact,
            List<MutinyCharacter> characters,
            bool allies)
        {
            float score = 0f;
            for (int i = 0; i < characters.Count; i++)
            {
                MutinyCharacter character = characters[i];
                if (character == null || !character.IsAlive || character.PhysicsBody == null)
                    continue;

                Vector2 target = PositionOf(character);
                float distance = Vector2.Distance(impact, target);
                score += ScoreSeagullDistance(distance, allies);
            }
            return score;
        }

        internal static float ScoreSeagullDistanceForVerification(float distance, bool allies)
            => ScoreSeagullDistance(distance, allies);

        private static float ScoreSeagullDistance(float distance, bool allies)
        {
            if (distance >= 40f)
                return 0f;
            return allies
                ? -(1.5f - distance / 40f)
                : 1f - distance / 40f;
        }

        private List<SelfThrowSample> BuildSelfThrowSamples(MutinyCharacter character, DecisionWork work)
        {
            Vector2 startPos = new Vector2(character.PhysicsBody.State.X, character.PhysicsBody.State.Y);
            const int originalSamples = 50;
            List<PhysicsBoxObstacle> simulationBoxes = MutinyBoxRegistry.GetObstacles();
            var samples = new List<SelfThrowSample>(originalSamples);
            for (int s = 0; s < originalSamples; s++)
            {
                Vector2 testVel = RandomArc(20f);
                var simBody = character.PhysicsBody.State;
                simBody.X = startPos.x;
                simBody.Y = startPos.y;
                simBody.VelocityX = testVel.x;
                simBody.VelocityY = testVel.y;
                simBody.HitsBoxes = true;
                Vector2 impactPx = SimulateCharacterLanding(simBody, work.Terrain, work.GridW, work.GridH, work.WaterY,
                    simulationBoxes, out _);
                samples.Add(new SelfThrowSample { Velocity = testVel, Landing = impactPx });
            }
            return samples;
        }

        private void EvaluateSelfThrowSample(MutinyCharacter character, SelfThrowSample sample,
            DecisionWork work, ref AIMove bestMove, ref int candidateCount)
        {
            Vector2 start = PositionOf(character);
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < work.Enemies.Count; i++)
                centroid += PositionOf(work.Enemies[i]);
            centroid /= work.Enemies.Count;

            // Character.aiThink calls cherryBomb.randomThrows(10) for each move
            // even though the decompiled score ignores those ten impact points.
            // Consume the same random draws so subsequent weapons retain order.
            if (HasEffectiveWeapon(character, "cherryBomb", work.ForcedWeaponType))
                for (int i = 0; i < 10; i++)
                    RandomArc(MutinyWeaponFactory.GetTwangMaxForce("cherryBomb"));

            float score = ScoreSelfThrow(character, start, sample.Landing, centroid,
                work.Enemies, work.Allies, work.WaterY, work.ForcedWeaponType);
            ConsiderCandidate(ref bestMove, new AIMove
            {
                MoveType = AIMoveType.SelfThrow,
                Character = character,
                LaunchVelocity = sample.Velocity,
                TargetPosition = sample.Landing,
                Score = score
            }, ref candidateCount);
        }

        private static AIMove CreatePassMove()
        {
            return new AIMove { MoveType = AIMoveType.Pass, Score = 0f };
        }

        private static bool ContainsAlive(List<MutinyCharacter> characters)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && characters[i].IsAlive)
                    return true;
            }
            return false;
        }

        private static Vector2 PositionOf(MutinyCharacter character)
        {
            PhysicsBodyState state = character.PhysicsBody.State;
            return new Vector2(state.X, state.Y);
        }

        private Vector2 RandomArc(float maxForce)
        {
            // Character.randomThrows and Weapon.randomThrows: 180 + int(random * 180).
            int degrees = 180 + NextRandomInt(0, 180);
            float force = NextRandomFloat(5f, maxForce);
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians) * force, Mathf.Sin(radians) * force);
        }

        private int NextRandomInt(int minimum, int maximum)
            => m_Random != null ? m_Random.NextInt(minimum, maximum) : UnityEngine.Random.Range(minimum, maximum);

        private float NextRandomFloat(float minimum, float maximum)
            => m_Random != null ? m_Random.NextFloat(minimum, maximum) : UnityEngine.Random.Range(minimum, maximum);

        private static bool MutinyWeaponFactoryCanFire(string weaponType)
        {
            return string.Equals(weaponType, "cherryBomb", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "dynamite", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "banana", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "cannonball", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "boulder", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "mine", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "parachuteBomb", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "piecesOfEight", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "rumBottle", StringComparison.OrdinalIgnoreCase);
        }

        private static PhysicsBodyState CreateWeaponSimulation(Vector2 start, string weaponType, Vector2 velocity)
        {
            PhysicsBodyState body = PhysicsBodyState.CreateDefault(start.x, start.y);
            body.VelocityX = velocity.x;
            body.VelocityY = velocity.y;
            body.Bounce = 0.2f;
            body.Friction = 0.3f;
            body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 9f;
            body.HitsBoxes = true;

            if (string.Equals(weaponType, "dynamite", StringComparison.OrdinalIgnoreCase))
            {
                body.Friction = 1.7f;
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 11f;
            }
            else if (string.Equals(weaponType, "banana", StringComparison.OrdinalIgnoreCase))
            {
                body.Bounce = 0.8f;
                body.Friction = 0.5f;
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 7f;
            }
            else if (string.Equals(weaponType, "boulder", StringComparison.OrdinalIgnoreCase))
            {
                // AI reaches Boulder through Weapon.aiPerform -> Weapon.fire, not
                // Boulder.release.  Candidate velocity is therefore the launch
                // velocity used by the actual weapon without a .5 conversion.
                body.Weight = 1.5f;
                body.Friction = 0.25f;
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 31f;
            }
            else if (string.Equals(weaponType, "mine", StringComparison.OrdinalIgnoreCase))
            {
                body.Friction = 1.5f;
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 14f;
            }
            else if (string.Equals(weaponType, "parachuteBomb", StringComparison.OrdinalIgnoreCase))
            {
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 11f;
            }
            else if (string.Equals(weaponType, "piecesOfEight", StringComparison.OrdinalIgnoreCase))
            {
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 7f;
            }
            else if (string.Equals(weaponType, "rumBottle", StringComparison.OrdinalIgnoreCase))
            {
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 14f;
            }
            else if (string.Equals(weaponType, "cannonball", StringComparison.OrdinalIgnoreCase))
            {
                body.Weight = 0f;
                body.LeftExtent = body.RightExtent = body.TopExtent = body.BottomExtent = 10f;
            }
            return body;
        }

        private static PhysicsBodyState CreateFormalWeaponPredictionTemplate(MutinyCharacter shooter, string weaponType)
        {
            // Initialize the real class once per weapon, then copy its pure state.
            // The probe is never fired or stepped, so contact/explosion/inventory
            // callbacks cannot run during candidate scoring.
            MutinyWeapon probe = MutinyWeaponFactory.SpawnWeapon(weaponType, shooter);
            if (probe == null || probe.PhysicsBody == null)
                throw new InvalidOperationException($"AI cannot initialize prediction weapon {weaponType}");
            try
            {
                probe.PhysicsBody.IsActive = false;
                return probe.PhysicsBody.State;
            }
            finally
            {
                probe.gameObject.SetActive(false);
                Destroy(probe.gameObject);
            }
        }

        internal static PhysicsBodyState CreateFormalWeaponPredictionTemplateForVerification(
            MutinyCharacter shooter, string weaponType)
            => CreateFormalWeaponPredictionTemplate(shooter, weaponType);

        // Legacy isolated-body seam used by existing motion tests. Production
        // candidates copy the formally initialized weapon state above.
        internal static PhysicsBodyState CreateWeaponSimulationForVerification(
            Vector2 start, string weaponType, Vector2 velocity)
        {
            return CreateWeaponSimulation(start, weaponType, velocity);
        }

        private static float ScoreGenericWeaponCandidate(
            string weaponType,
            Vector2 impact,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies)
        {
            float score = -0.01f;
            for (int i = 0; i < enemies.Count; i++)
            {
                MutinyCharacter enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                float distance = Vector2.Distance(impact, PositionOf(enemy));
                if (distance < 70f)
                {
                    score += 1.5f - distance / 70f;
                    score *= 1f + enemy.Evilness;
                }
            }

            for (int i = 0; i < allies.Count; i++)
            {
                MutinyCharacter ally = allies[i];
                if (ally == null || !ally.IsAlive)
                    continue;

                float distance = Vector2.Distance(impact, PositionOf(ally));
                if (distance < 40f)
                    score -= 1.5f - distance / 40f;
            }

            if (string.Equals(weaponType, "anchor", StringComparison.OrdinalIgnoreCase))
                score *= 0.5f;
            if (string.Equals(weaponType, "piecesOfEight", StringComparison.OrdinalIgnoreCase))
                score = (score - 0.5f) * 1.2f;
            return score;
        }

        private float ScoreSelfThrow(
            MutinyCharacter character,
            Vector2 start,
            Vector2 landing,
            Vector2 enemyCentroid,
            List<MutinyCharacter> enemies,
            List<MutinyCharacter> allies,
            float waterPixelY,
            string forcedWeaponType)
        {
            float score = -(landing.y - start.y) * 0.003f;
            score += (Mathf.Abs(landing.x - enemyCentroid.x) - Mathf.Abs(start.x - enemyCentroid.x)) / -500f;
            score += (Mathf.Abs(landing.y - enemyCentroid.y) - Mathf.Abs(start.y - enemyCentroid.y)) / -500f;
            if (landing.y >= waterPixelY)
                score -= 2f;

            for (int i = 0; i < enemies.Count; i++)
            {
                MutinyCharacter enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;

                float distance = Vector2.Distance(landing, PositionOf(enemy));
                float proximity;
                if (distance < 200f)
                {
                    proximity = 0.2f * (1f - distance / 200f);
                    if (distance < 100f)
                    {
                        proximity *= distance / 100f;
                        score -= 3f * (1f - distance / 100f);
                    }
                }
                else
                {
                    proximity = 0.3f * Mathf.Pow(0.75f, distance / 200f);
                }
                score += proximity;
                score *= 1f + enemy.Evilness;
            }

            for (int i = 0; i < allies.Count; i++)
            {
                MutinyCharacter ally = allies[i];
                if (ally == null || !ally.IsAlive)
                    continue;

                float distance = Vector2.Distance(landing, PositionOf(ally));
                if (ally == character && distance < 80f)
                    score -= 1f - distance / 80f;
                else if (ally != character && distance < 40f)
                    score -= 0.1f * (1f - distance / 40f);
            }

            MutinyTreasureChest[] chests = FindObjectsByType<MutinyTreasureChest>(FindObjectsSortMode.None);
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i] == null || chests[i].IsFinished)
                    continue;
                score += ScoreChestMoveLanding(landing, chests[i]);
            }

            if (HasEffectiveWeapon(character, "cherryBomb", forcedWeaponType))
                score = ScoreCherryBombFollowUpAtMoveLanding(score, character, landing, enemies);

            float movement = Vector2.Distance(landing, start);
            score += movement < 300f ? 0.3f * movement / 300f - 0.3f : -0.3f;
            return score;
        }

        public static float ScoreChestMoveLanding(Vector2 landing, MutinyTreasureChest chest)
        {
            if (chest == null || chest.IsFinished)
                return 0f;
            return Vector2.Distance(landing, new Vector2(chest.PixelX, chest.FloorPixelY)) <
                   OriginalChestMoveRadiusPixels
                ? OriginalChestMoveBonus
                : 0f;
        }

        private float ScoreCherryBombFollowUpAtMoveLanding(
            float score,
            MutinyCharacter character,
            Vector2 landing,
            List<MutinyCharacter> enemies)
        {
            // Character.aiThink calls cherryBomb.randomThrows(10), but then (as
            // confirmed in pcode) scores the character movement landing every time.
            // Preserve that scoring quirk without instantiating ten simulation objects.
            for (int sample = 0; sample < 10; sample++)
            {
                float followUp = 0f;
                for (int i = 0; i < enemies.Count; i++)
                {
                    MutinyCharacter enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive)
                        continue;
                    float distance = Vector2.Distance(landing, PositionOf(enemy));
                    if (distance < 100f)
                        followUp += (1f - distance / 100f) * (1f + enemy.Evilness) * 0.5f;
                }
                float selfDistance = Vector2.Distance(landing, PositionOf(character));
                if (selfDistance < 80f)
                    followUp -= 1f - selfDistance / 80f;
                if (followUp > 0f)
                    score += followUp;
            }
            return score;
        }

        private static float ScoreTidalWave(List<MutinyCharacter> enemies, List<MutinyCharacter> allies, float waterPixelY)
        {
            float score = 0f;
            for (int i = 0; i < enemies.Count; i++)
            {
                MutinyCharacter enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && PositionOf(enemy).y >= waterPixelY - 300f)
                    score += enemy.Health / enemy.MaxHealth * 0.5f;
            }
            for (int i = 0; i < allies.Count; i++)
            {
                MutinyCharacter ally = allies[i];
                if (ally != null && ally.IsAlive && PositionOf(ally).y >= waterPixelY - 300f)
                    score -= 1.5f;
            }
            return score;
        }

        private void EvaluateVoodoo(
            MutinyCharacter shooter,
            List<MutinyCharacter> enemies,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float waterPixelY,
            ref AIMove bestMove,
            ref int candidateCount)
        {
            List<PhysicsBoxObstacle> simulationBoxes = MutinyBoxRegistry.GetObstacles();
            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                MutinyCharacter enemy = enemies[enemyIndex];
                if (enemy == null || !enemy.IsAlive)
                    continue;
                Vector2 start = PositionOf(enemy);
                var velocities = new Vector2[2];
                var landings = new Vector2[2];
                // Character.randomThrows(2) generates both trajectories before
                // Character.aiThink draws either success perturbation.
                for (int sample = 0; sample < 2; sample++)
                {
                    velocities[sample] = RandomArc(20f);
                    PhysicsBodyState body = enemy.PhysicsBody.State;
                    body.X = start.x;
                    body.Y = start.y;
                    body.VelocityX = velocities[sample].x;
                    body.VelocityY = velocities[sample].y;
                    body.HitsBoxes = true;
                    landings[sample] = SimulateCharacterLanding(body, terrainGrid, gridW, gridH, waterPixelY,
                        simulationBoxes, out _);
                }
                for (int sample = 0; sample < 2; sample++)
                {
                    Vector2 landing = landings[sample];
                    float score = landing.y >= waterPixelY
                        ? 1f + NextRandomFloat(0f, 0.2f)
                        : NextRandomFloat(0f, 0.2f) - 0.5f;
                    ConsiderCandidate(ref bestMove, new AIMove
                    {
                        MoveType = AIMoveType.ShootWeapon,
                        Character = shooter,
                        WeaponType = "voodooDoll",
                        LaunchVelocity = velocities[sample],
                        TargetCharacter = enemy,
                        Score = score
                    }, ref candidateCount);
                }
            }
        }

        private void ConsiderCandidate(ref AIMove bestMove, AIMove candidate, ref int candidateCount)
        {
            candidateCount++;
            if (m_Random != null)
                m_Random.RecordCandidate(new MutinyAICandidateRecord
                {
                    Character = candidate.Character != null ? candidate.Character.name : "none",
                    Weapon = candidate.WeaponType,
                    MoveType = candidate.MoveType.ToString(),
                    Score = candidate.Score,
                    Velocity = candidate.LaunchVelocity,
                    Target = candidate.TargetPosition,
                    TargetCharacter = candidate.TargetCharacter != null ? candidate.TargetCharacter.name : null,
                    SeagullFlightY = candidate.SeagullFlightY,
                    SeagullShotXs = candidate.SeagullShotXs,
                    BoxPossibilities = candidate.BoxPossibilities,
                    CannonRotationDegrees = candidate.CannonRotationDegrees
                });
            // Flash Team.advance uses strict >, so the first candidate wins a tie.
            if (bestMove.Character == null || candidate.Score > bestMove.Score)
                bestMove = candidate;
        }

        private static Vector2 SimulateWeaponImpact(
            PhysicsBodyState body,
            string weaponType,
            string[,] grid,
            int gridW,
            int gridH,
            float waterPixelY,
            IReadOnlyList<PhysicsBoxObstacle> boxes,
            out int steps)
        {
            // Weapon.randomThrows executes advanceMotion until the concrete weapon
            // sets simulationFinished, with a source guard that breaks after the
            // 101st step. Cannonball has its own contact/bounds completion loop.
            int maxSteps = string.Equals(weaponType, "cannonball", StringComparison.OrdinalIgnoreCase)
                ? 4096
                : 101;
            steps = 0;
            while (steps < maxSteps)
            {
                if (string.Equals(weaponType, "parachuteBomb", StringComparison.OrdinalIgnoreCase))
                    MutinyParachuteBomb.ApplyOriginalAirMotion(ref body);

                StepResult res = MutinyPhysics.Step(ref body, grid, gridW, gridH, boxes);
                steps++;
                bool contacted = res.HitFloor || res.HitCeiling || res.HitLeftWall || res.HitRightWall;

                if (TerminatesSimulationOnContact(weaponType) && contacted)
                    return new Vector2(body.X, body.Y);

                if ((string.Equals(weaponType, "dynamite", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(weaponType, "mine", StringComparison.OrdinalIgnoreCase)) &&
                    body.VelocityX == 0f && Mathf.Abs(body.VelocityY) < 0.2f)
                    return new Vector2(body.X, body.Y);

                if (string.Equals(weaponType, "banana", StringComparison.OrdinalIgnoreCase) &&
                    body.VelocityX == 0f && Mathf.Abs(body.VelocityY) < 0.5f)
                    return new Vector2(body.X, body.Y);

                if (string.Equals(weaponType, "parachuteBomb", StringComparison.OrdinalIgnoreCase))
                    MutinyParachuteBomb.ApplyOriginalCeilingClamp(ref body);

                if (string.Equals(weaponType, "cannonball", StringComparison.OrdinalIgnoreCase) &&
                    (body.X < -300f || body.X > gridW * MutinyPhysics.PixelsPerUnit + 300f ||
                     body.Y < -300f || (!float.IsInfinity(waterPixelY) && body.Y > waterPixelY)))
                    return new Vector2(body.X, body.Y);
            }

            return new Vector2(body.X, body.Y);
        }

        private static bool TerminatesSimulationOnContact(string weaponType)
        {
            return string.Equals(weaponType, "cherryBomb", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "rumBottle", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "piecesOfEight", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "parachuteBomb", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "cannonball", StringComparison.OrdinalIgnoreCase);
        }

        private static Vector2 SimulateCharacterLanding(
            PhysicsBodyState body,
            string[,] grid,
            int gridW,
            int gridH,
            float waterPixelY,
            IReadOnlyList<PhysicsBoxObstacle> boxes,
            out int steps)
        {
            // Character.randomThrows has no arbitrary prediction horizon: it runs
            // until horizontal/vertical motion settles or the character reaches
            // water. Keep only a malformed-board safety guard.
            steps = 0;
            while ((body.VelocityX != 0f || Mathf.Abs(body.VelocityY) > 0.2f) &&
                   body.Y < waterPixelY && steps < 4096)
            {
                MutinyPhysics.Step(ref body, grid, gridW, gridH, boxes);
                steps++;
            }

            return new Vector2(body.X, body.Y);
        }

        internal static Vector2 SimulateWeaponImpactForVerification(
            PhysicsBodyState body,
            string weaponType,
            string[,] grid,
            int gridW,
            int gridH,
            float waterPixelY,
            out int steps)
        {
            return SimulateWeaponImpact(body, weaponType, grid, gridW, gridH, waterPixelY,
                MutinyBoxRegistry.GetObstacles(), out steps);
        }

        internal static Vector2 SimulateCharacterLandingForVerification(
            PhysicsBodyState body,
            string[,] grid,
            int gridW,
            int gridH,
            float waterPixelY,
            out int steps)
        {
            return SimulateCharacterLanding(body, grid, gridW, gridH, waterPixelY,
                MutinyBoxRegistry.GetObstacles(), out steps);
        }

        private void ExecuteMove(AIMove move)
        {
            var ch = move.Character;
            if (ch == null || !ch.IsAlive)
                return;

            if (move.MoveType == AIMoveType.ShootWeapon && !string.IsNullOrEmpty(move.WeaponType))
            {
                bool consumeInventory = !move.UsesForcedWeaponSupply;
                MutinyDebugLog.Info("AI",
                    $"firing weapon={move.WeaponType} character={ch.name} velocity={move.LaunchVelocity}", this);
                if (string.Equals(move.WeaponType, "tidalWave", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon waveWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (waveWeapon is MutinyTidalWave wave)
                    {
                        float waterY = ch.PhysicsBody.WaterPixelY;
                        wave.StartWave(-550f, waterY);
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                    }
                }
                else if (string.Equals(move.WeaponType, "seagull", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon birdWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (birdWeapon is MutinySeagull seagull)
                    {
                        seagull.PlaceForAi(move.SeagullFlightY, move.SeagullShotXs);
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                    }
                }
                else if (string.Equals(move.WeaponType, "gunpowderBarrel", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon barrelWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (barrelWeapon is MutinyGunpowderBarrel barrel && barrel.BeginAiPlacement(move.BoxPossibilities))
                    {
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                        MutinyDebugLog.Info("AI", $"gunpowder barrel AI sequence armed candidates={move.BoxPossibilities.Length}", this);
                    }
                }
                else if (string.Equals(move.WeaponType, "woodenCrate", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon crateWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (crateWeapon is MutinyWoodenCrate crate && crate.BeginAiPlacement(move.BoxPossibilities))
                    {
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                        MutinyDebugLog.Info("AI", $"wooden crate AI sequence armed candidates={move.BoxPossibilities.Length}", this);
                    }
                }
                else if (string.Equals(move.WeaponType, "anchor", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon anchorWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (anchorWeapon is MutinyAnchor anchor && anchor.DropForAi(move.TargetPosition.x))
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                }
                else if (string.Equals(move.WeaponType, "cannon", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon cannonWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (cannonWeapon is MutinyCannon cannon)
                    {
                        cannon.BeginAiFire(move.TargetPosition, move.CannonRotationDegrees, move.LaunchVelocity);
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                    }
                }
                else if (string.Equals(move.WeaponType, "voodooDoll", StringComparison.OrdinalIgnoreCase))
                {
                    MutinyWeapon dollWeapon = MutinyWeaponFactory.SpawnWeapon(move.WeaponType, ch);
                    if (dollWeapon is MutinyVoodooDoll doll)
                    {
                        doll.BindTarget(move.TargetCharacter);
                        doll.FireForAi(move.LaunchVelocity);
                        if (consumeInventory) ch.ConsumeWeapon(move.WeaponType);
                    }
                }
                else
                {
                    MutinyWeaponFactory.SpawnAndFire(move.WeaponType, ch, move.LaunchVelocity,
                        consumeInventory);
                }
                ch.CanThrow = false;
                ch.CanShoot = false;
                if (m_TurnManager != null)
                {
                    m_TurnManager.NotifyActionStarted();
                }
            }
            else if (move.MoveType == AIMoveType.SelfThrow)
            {
                MutinyDebugLog.Info("AI",
                    $"jumping character={ch.name} velocity={move.LaunchVelocity}; preserves CanShoot for phase 2; original Character.twang has no direct SFX", this);
                ch.PhysicsBody.SetVelocity(move.LaunchVelocity.x, move.LaunchVelocity.y);
                ch.MarkSelfThrown("AI");
                ch.CanThrow = false;
                // Authentic Flash parity: CanShoot remains true for phase 2!
                if (m_TurnManager != null)
                {
                    m_TurnManager.NotifyActionStarted();
                }
            }
            else
            {
                MutinyDebugLog.Info("AI", $"passing turn character={ch.name}", this);
                if (m_TurnManager != null)
                {
                    m_TurnManager.PassTurn();
                }
            }
        }

        // Test seam for the same execution method used by the turn coroutine.
        internal void ExecuteMoveForVerification(AIMove move, MutinyTurnManager turnManager = null)
        {
            if (turnManager != null)
                m_TurnManager = turnManager;
            else if (m_TurnManager == null)
                BindTurnManager();
            ExecuteMove(move);
        }

        private void BindTurnManager()
        {
            MutinyTurnManager found = FindAnyObjectByType<MutinyTurnManager>();
            if (found == null)
            {
                if (!m_LoggedMissingManager)
                {
                    m_LoggedMissingManager = true;
                    MutinyDebugLog.Warning("AI", $"team={TeamLabel(m_Team)} cannot find turn manager", this);
                }
                return;
            }

            if (m_TurnManager != null)
                m_TurnManager.OnTurnStarted -= HandleTurnStarted;
            m_TurnManager = found;
            m_TurnManager.OnTurnStarted -= HandleTurnStarted;
            m_TurnManager.OnTurnStarted += HandleTurnStarted;
            m_LoggedMissingManager = false;
            MutinyDebugLog.Info("AI", $"bound turn manager team={TeamLabel(m_Team)}", this);
        }

        private void TryStartCurrentTurn(string source)
        {
            if (ResolveTurnGate(m_TurnManager, m_Team) == MutinyAITurnGate.Execute)
                BeginTurnRoutine(source);
        }

        private void BeginTurnRoutine(string source)
        {
            if (m_TurnCoroutine != null)
                StopCoroutine(m_TurnCoroutine);
            MutinyDebugLog.Info("AI", $"starting turn routine source={source}", this);
            m_TurnCoroutine = StartCoroutine(ExecuteAITurnRoutine());
        }

        private static string TeamLabel(MutinyTeam team)
        {
            return team == null ? "none" : $"T{team.TeamNumber}";
        }
    }
}
