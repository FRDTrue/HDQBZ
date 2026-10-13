using System;
using System.Collections.Generic;
using Mutiny.Levels;
using Mutiny.Network;
using Mutiny.Simulation;
using Mutiny.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mutiny.Presentation
{
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class MutinyCameraController : MonoBehaviour
    {
        private const float OriginalHorizontalPixels = 550f;
        private const float OriginalVerticalPixels = 400f;
        private const float OriginalEdgePixels = 40f;
        private const float OriginalMaxScrollPixelsPerTick = 10f;
        private const float OriginalScrollAccelerationPixelsPerTick = 1f;
        private const float OriginalTrackingPixelsPerTick = 30f;
        private const float OriginalTrackingVerticalOffsetPixels = 50f;
        // Pinch zoom limits around the authored framing: 1x is the original
        // 550x400 viewport (orthographicSize 6.25), larger zooms in.
        public const float MinimumZoomScale = 0.6f;
        public const float MaximumZoomScale = 2f;
        // DefineSprite_1813_cursor: labelled scroll1 at frame 78 and scroll2
        // at frame 87. These are exact 31x22 PNG exports, not redrawn arrows.
        private const string ScrollCardinalPng =
            "iVBORw0KGgoAAAANSUhEUgAAAB8AAAAWCAYAAAA4oUfxAAAAjUlEQVR4XtXO2wqAIBRE0f7/p4stJbi7vDgqLQjCM8dx235kP7/p9gv/Ho5Wy1c8oCmf/QB3F5w7OIJ7K2YOp7mzwdwLSe67IeOlFHc9IufFBPe8IuvlXu74RN4X9PD9r8h6uZc7HpHzYoJ7bsh4KcVdDeZeSHJfxczhNHcWnDs4gnunFWNZMZYVX6LFB3vmBBk/U38EAAAAAElFTkSuQmCC";
        private const string ScrollDiagonalPng =
            "iVBORw0KGgoAAAANSUhEUgAAAB8AAAAWCAYAAAA4oUfxAAAAgklEQVR4Xu2OSw6AIBQDuf9VOQSmGxPHD/A+6oJJTBTaOqUsFj+hOR4XzYq6HJuFm8OoyzELrdbK7S7qccjKtIA6HPEwJaA8B7wMCyjLcgRDAsqxGEVXQBmWInkU0D0L0dwK6I7hDC4FdM5gFicBnTGUyUFA3wxkswvonZdv8NmPTWyeFwBK1h/xJgAAAABJRU5ErkJggg==";

        public MutinyTurnManager TurnManager;
        public MutinyPlayerInput PlayerInput;

        private MutinySpeechController m_Speech;

        private Camera m_Camera;
        private MutinyLevelRoot m_LevelRoot;
        private MutinyFrontendController m_Frontend;
        private MutinyTeam m_PreviousTeam;
        private Transform m_TurnPanTarget;
        private MutinyCharacter m_TurnFocusCharacter;
        private MutinyWeapon m_TrackedWeapon;
        private Vector2 m_EdgeVelocityPixelsPerSecond;
        private float m_ZoomScale = 1f;
        private bool m_AirDropCameraWasLocked;
        private Vector2 m_DesktopScrollDirection;
        private bool m_DesktopScrollCursorWasVisible;
        private Texture2D m_ScrollCardinalTexture;
        private Texture2D m_ScrollDiagonalTexture;
        public static bool ExplosionCameraEnabled { get; private set; }
        private readonly List<MutinyCharacter> m_ExplosionCharacters = new List<MutinyCharacter>();
        private MutinyCharacter m_ExplosionFocus;
        private MutinyTeam m_ExplosionTeam;
        private bool m_ExplosionSelectionMade;
        private bool HasExplosionCameraTarget => m_ExplosionCharacters.Count > 0 &&
            (!m_ExplosionSelectionMade || m_ExplosionFocus != null);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetExplosionCameraSetting() => SetExplosionCameraEnabled(false);

        public static void SetExplosionCameraEnabled(bool enabled)
        {
            // Enabling is not retroactive; only subsequent actual hits qualify.
            if (ExplosionCameraEnabled == enabled && enabled)
                return;
            ExplosionCameraEnabled = enabled;
            foreach (MutinyCameraController controller in FindObjectsByType<MutinyCameraController>(FindObjectsSortMode.None))
                controller.ClearExplosionCamera();
        }

        private void Awake()
        {
            m_Camera = GetComponent<Camera>();
            ApplyViewportLetterbox();
        }

        private void OnEnable()
        {
            ApplyViewportLetterbox();
            MutinyExplosion.KnockbackApplied += OnExplosionKnockback;
        }

        private void OnDisable()
        {
            MutinyExplosion.KnockbackApplied -= OnExplosionKnockback;
            ClearExplosionCamera();
            if (m_DesktopScrollCursorWasVisible)
                Cursor.visible = PlayerInput == null || !PlayerInput.HasVisibleSpecialCursor;
            m_DesktopScrollCursorWasVisible = false;
            m_DesktopScrollDirection = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (m_ScrollCardinalTexture != null)
                Destroy(m_ScrollCardinalTexture);
            if (m_ScrollDiagonalTexture != null)
                Destroy(m_ScrollDiagonalTexture);
        }

        private void Update()
        {
            ApplyViewportLetterbox();
        }

        public bool IsTrackingAirDrop => FindFallingChest() != null;
        /// <summary>Pinch zoom factor: 1x is the authored framing, larger zooms in.</summary>
        public float ZoomScale => m_ZoomScale;
        public bool IsPanningToTurnTarget => m_TurnPanTarget != null;
        public MutinyCharacter TurnFocusCharacter
        {
            get
            {
                EnsureReferences();
                RefreshTurnPanTarget();
                return m_TurnFocusCharacter;
            }
        }

        public static void ResetCamerasForLevel(MutinyLevelRoot level)
        {
            if (level == null || !Application.isPlaying)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.GetComponent<MutinyCameraController>() == null)
                mainCamera.gameObject.AddComponent<MutinyCameraController>();

            foreach (MutinyCameraController controller in FindObjectsByType<MutinyCameraController>(FindObjectsSortMode.None))
                controller.ResetForLevel(level);
        }

        public void ResetForLevel(MutinyLevelRoot level)
        {
            if (level == null)
                return;

            if (m_DesktopScrollCursorWasVisible)
                Cursor.visible = PlayerInput == null || !PlayerInput.HasVisibleSpecialCursor;
            m_DesktopScrollCursorWasVisible = false;
            m_DesktopScrollDirection = Vector2.zero;
            m_EdgeVelocityPixelsPerSecond = Vector2.zero;
            m_PreviousTeam = null;
            m_TurnPanTarget = null;
            m_TurnFocusCharacter = null;
            m_TrackedWeapon = null;
            m_AirDropCameraWasLocked = false;
            ClearExplosionCamera();

            // Bind synchronously: the previous root may survive until end-of-frame
            // Destroy, so a scene search can still return the old turn/speech.
            m_LevelRoot = level;
            TurnManager = level.GetComponent<MutinyTurnManager>();
            PlayerInput = level.GetComponent<MutinyPlayerInput>();
            m_Speech = level.GetComponent<MutinySpeechController>();
            ApplyViewportLetterbox();
            // Every level opens at the authored framing; a zoom belongs to the
            // session it was made in, not to the next one.
            ResetZoom();

            // TileSystem.readXML always calls panCamera(0, 0, true). Flash stores
            // the viewport top-left, whereas Unity stores its centre (Y is up).
            // Use the normal clamp, including water.y - 320, before any speech pan.
            SetClampedPosition(new Vector3(
                OriginalHorizontalPixels * 0.5f / MutinyPhysics.PixelsPerUnit,
                -OriginalVerticalPixels * 0.5f / MutinyPhysics.PixelsPerUnit,
                transform.position.z));
        }

        public void PanToTarget(Transform target)
        {
            m_TurnPanTarget = target;
        }

        public void PanToCharacter(MutinyCharacter character)
        {
            // PiecesOfEight.next sets track=false before assigning
            // panToCharacter. Keep that ordering explicit so a resolved coin can
            // never win FindActionTarget while the camera is returning.
            m_TrackedWeapon = null;
            m_TurnPanTarget = character != null ? character.transform : null;
        }

        public void TrackWeapon(MutinyWeapon weapon)
        {
            m_TrackedWeapon = weapon;
            if (weapon != null)
                m_TurnPanTarget = null;
        }

        public static void RequestTrackWeapon(MutinyWeapon weapon)
        {
            MutinyCameraController[] controllers = FindObjectsByType<MutinyCameraController>(FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                    controllers[i].TrackWeapon(weapon);
            }
        }

        public static void ReleaseWeaponTracking(MutinyWeapon weapon)
        {
            MutinyCameraController[] controllers = FindObjectsByType<MutinyCameraController>(FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null && controllers[i].m_TrackedWeapon == weapon)
                    controllers[i].m_TrackedWeapon = null;
            }
        }

        public static void RequestPanToCharacter(MutinyCharacter character)
        {
            MutinyCameraController[] controllers = FindObjectsByType<MutinyCameraController>(FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                    controllers[i].PanToCharacter(character);
            }
        }

        private void LateUpdate()
        {
            AdvanceCamera(Time.deltaTime);
            UpdateScrollCursorVisibility();
        }

        private void AdvanceCamera(float deltaTime)
        {
            m_DesktopScrollDirection = Vector2.zero;
            ApplyViewportLetterbox();
            EnsureReferences();
            if (m_Camera == null || m_LevelRoot == null || TurnManager == null)
                return;

            RefreshTurnPanTarget();
            RefreshExplosionCamera();

            if (MutinyTransitionManager.IsTransitionActive)
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                return;
            }

            // Optional, explicitly non-Flash priority: an actual blast interrupts
            // weapon/airdrop focus immediately, using the same displayed pose.
            if (m_ExplosionFocus != null)
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                PanTowards(GetPresentationPosition(m_ExplosionFocus.transform),
                    OriginalTrackingPixelsPerTick, OriginalTrackingVerticalOffsetPixels, deltaTime);
                return;
            }

            // TileSystem.advanceScrolling returns before every automatic camera
            // branch while Controller.dragging is set (human player dragging).
            if (PlayerInput != null && PlayerInput.IsAiming)
            {
                if (PlayerInput.IsControllerAiming)
                    AdvanceControllerScrolling(deltaTime);
                return;
            }

            if (m_Speech == null)
                m_Speech = TurnManager.GetComponent<MutinySpeechController>();
            // Original TileSystem checks the speech target ahead of chest, weapon,
            // turn and manual scrolling. The bubble position is captured once.
            if (m_Speech != null && m_Speech.HasActiveBubble)
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                PanTowards(m_Speech.BubbleWorldPosition, 50f, 0f, deltaTime);
                return;
            }

            // TileSystem.advanceScrolling returns before the falling-chest branch
            // while currentTeam.aiFinished is false. Unity evaluates synchronously,
            // but exposes that interval so camera priority remains source-authentic.
            MutinyAIController activeAi = TurnManager.CurrentTeam != null
                ? TurnManager.CurrentTeam.GetComponent<MutinyAIController>()
                : null;
            if (ShouldPauseForAiThinking(
                    TurnManager.CurrentTeam != null && TurnManager.CurrentTeam.IsAiControlled,
                    activeAi != null && activeAi.IsEvaluatingCandidates))
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                return;
            }

            MutinyTreasureChest fallingChest = FindFallingChest();
            if (fallingChest != null)
            {
                if (!m_AirDropCameraWasLocked)
                {
                    m_AirDropCameraWasLocked = true;
                    MutinyDebugLog.Info("Camera",
                        $"airdrop tracking started chest={fallingChest.name} timeTaken={fallingChest.TimeTaken} speed=50px/tick", this);
                }
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                PanTowards(fallingChest.PresentationPosition, 50f, 0f, deltaTime);
                return;
            }
            if (m_AirDropCameraWasLocked)
            {
                m_AirDropCameraWasLocked = false;
                MutinyDebugLog.Info("Camera",
                    "airdrop tracking released; normal action/edge camera controls resumed", this);
            }

            // Original BoxWeapon.aiContinue refreshes trackX/Y before its 40-tick
            // placement delay. The root's Transform remains at the first box.
            if (TryGetAiBoxPlacementCameraTarget(out Vector3 placementTarget))
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                PanTowards(placementTarget, OriginalTrackingPixelsPerTick,
                    OriginalTrackingVerticalOffsetPixels, deltaTime);
                return;
            }

            Transform actionTarget = FindActionTarget();
            if (actionTarget != null)
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                // Seagull.advance uses trackY = y + 100, unlike the ordinary
                // weapon target. The visible body and camera share one pose.
                float verticalOffset = actionTarget.GetComponent<MutinySeagull>() != null
                    ? -OriginalTrackingVerticalOffsetPixels
                    : OriginalTrackingVerticalOffsetPixels;
                PanTowards(GetPresentationPosition(actionTarget),
                    OriginalTrackingPixelsPerTick, verticalOffset, deltaTime);
                return;
            }

            if (m_TurnPanTarget != null)
            {
                if (m_ExplosionSelectionMade &&
                    m_ExplosionCharacters.Contains(m_TurnPanTarget.GetComponent<MutinyCharacter>()))
                {
                    // Defer a queued return, rather than cancelling the existing
                    // AI/Voodoo camera-arrival handshake. It cannot bypass the
                    // no-relay lock while this blast batch is still moving.
                    return;
                }
                if (PanTowards(GetPresentationPosition(m_TurnPanTarget),
                        OriginalTrackingPixelsPerTick, OriginalTrackingVerticalOffsetPixels, deltaTime))
                    m_TurnPanTarget = null;
                return;
            }

            AdvanceEdgeScrolling();
        }

        internal static bool ShouldPauseForAiThinking(bool isAiControlled, bool isEvaluatingCandidates)
            => isAiControlled && isEvaluatingCandidates;

        private void EnsureReferences()
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();
            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<MutinyTurnManager>();
            if (PlayerInput == null)
                PlayerInput = FindAnyObjectByType<MutinyPlayerInput>();
            if (m_LevelRoot == null)
                m_LevelRoot = FindAnyObjectByType<MutinyLevelRoot>();
        }

        private Transform FindActionTarget()
        {
            if (TurnManager.CurrentPhase != TurnPhase.ActionExecuting &&
                TurnManager.CurrentPhase != TurnPhase.Settling)
                return null;

            // BoxWeapon.place calls Weapon.place (track=true) and then clears
            // track again in the same call. While a human waits for the next box,
            // the original therefore has no action target and falls straight
            // through to ordinary edge/key scrolling. Resolve this before any
            // stale explicit target from the preceding action can take priority.
            if (IsAwaitingPlayerBoxPlacement())
                return null;

            // Flash follows the selected character's equipped Cannon.trackX/Y,
            // which Cannon.update assigns from its child cannonball. Resolve that
            // exact owner before scanning unrelated fired weapons in the scene.
            MutinyCharacter selectedCharacter = TurnManager.CurrentTeam != null
                ? TurnManager.CurrentTeam.SelectedCharacter
                : null;
            if (selectedCharacter != null)
            {
                MutinyCannon[] cannons = FindObjectsByType<MutinyCannon>(FindObjectsSortMode.None);
                for (int i = 0; i < cannons.Length; i++)
                {
                    MutinyCannon cannon = cannons[i];
                    if (cannon != null && cannon.Owner == selectedCharacter &&
                        cannon.IsFired && !cannon.IsFinished && cannon.CameraFocusTarget != null)
                        return cannon.CameraFocusTarget;
                }
            }

            // Weapon.track in the Flash original belongs to the currently
            // equipped weapon, not to whichever fired object happens to be found
            // first in the scene. Pieces of Eight explicitly refreshes this on
            // every launch and clears it on every intermediate resolution.
            if (m_TrackedWeapon != null)
            {
                // VoodooDoll.advance permanently clears track when it hands the
                // camera to its target. Never let a stale explicit reference
                // override that source-authentic transition.
                if (m_TrackedWeapon is MutinyVoodooDoll trackedDoll &&
                    trackedDoll.IsTargetFocusRequested)
                {
                    m_TrackedWeapon = null;
                }
                else
                {
                    if (!IsBoxPlacementWeapon(m_TrackedWeapon) &&
                        m_TrackedWeapon.IsFired && !m_TrackedWeapon.IsFinished &&
                        !(m_TrackedWeapon is MutinyMine trackedMine && trackedMine.IsStored))
                        return m_TrackedWeapon.transform;
                    m_TrackedWeapon = null;
                }
            }

            // During the gap between coins, track=false and panToCharacter owns
            // the camera. Once that pan completes there must be no action target,
            // even if the owner is still settling from unrelated motion.
            if (MutinyPiecesOfEight.HasPlayerAwaitingNextCoin(TurnManager.CurrentTeam))
                return null;

            MutinyWeapon[] weapons = FindObjectsByType<MutinyWeapon>(FindObjectsSortMode.None);
            for (int i = 0; i < weapons.Length; i++)
            {
                // BoxWeapon.place clears track on the placed Solid. AI focus is
                // supplied by the root's planned point above; never fall back to
                // a persistent first box, including after sequence completion.
                if (IsBoxPlacementWeapon(weapons[i]))
                    continue;
                // After ten ticks the original doll sets track=false for the rest
                // of its lifetime. panToCharacter owns the one-time move to the
                // victim; once that move clears, neither the fading doll nor the
                // moving victim is an automatic action target.
                if (weapons[i] is MutinyVoodooDoll doll && doll.IsTargetFocusRequested)
                    continue;
                // Cannon.update sets trackX/trackY from its child cannonball in the
                // Flash game. Keep the placed cannon stationary and follow that
                // separate projectile directly in Unity.
                if (weapons[i] is MutinyCannon cannon && cannon.CameraFocusTarget != null)
                    return cannon.CameraFocusTarget;
                // Mine.advanceMotion marks a settled mine finished in Flash.
                // It stays armed on the map, but it no longer owns the camera.
                if (weapons[i] is MutinyMine mine && mine.IsStored)
                    continue;
                if (weapons[i] != null && weapons[i].IsFired && !weapons[i].IsFinished)
                    return weapons[i].transform;
            }

            MutinyCharacter selected = TurnManager.CurrentTeam != null
                ? TurnManager.CurrentTeam.SelectedCharacter
                : null;
            if (selected != null && selected.IsAlive && selected.PhysicsBody != null &&
                !(m_ExplosionSelectionMade && m_ExplosionCharacters.Contains(selected)) &&
                !selected.PhysicsBody.IsAtRest)
                return selected.transform;

            return null;
        }

        private void ClearExplosionCamera()
        {
            m_ExplosionCharacters.Clear();
            m_ExplosionFocus = null;
            m_ExplosionTeam = null;
            m_ExplosionSelectionMade = false;
        }

        private static bool IsExplosionCharacterMoving(MutinyCharacter character) =>
            character != null && character.isActiveAndEnabled && !character.IsDrowned &&
            !character.HasLandDeathPresentation && character.PhysicsBody != null &&
            character.PhysicsBody.isActiveAndEnabled && character.PhysicsBody.IsActive &&
            !character.PhysicsBody.IsAtRest;

        private bool HasExplosionCameraContext() =>
            TurnManager != null && TurnManager.CurrentTeam != null && m_LevelRoot != null &&
            TurnManager.CurrentPhase != TurnPhase.NotStarted &&
            TurnManager.CurrentPhase != TurnPhase.GameOver;

        private void OnExplosionKnockback(IReadOnlyList<MutinyCharacter> characters)
        {
            if (!ExplosionCameraEnabled)
                return;
            EnsureReferences();
            RefreshExplosionCamera(false);
            if (!HasExplosionCameraContext())
                return;
            foreach (MutinyCharacter character in characters)
            {
                bool belongsToLevel = character != null &&
                    ((TurnManager.Team1 != null && TurnManager.Team1.Characters.Contains(character)) ||
                     (TurnManager.Team2 != null && TurnManager.Team2.Characters.Contains(character)));
                if (belongsToLevel && IsExplosionCharacterMoving(character) &&
                    !m_ExplosionCharacters.Contains(character))
                    m_ExplosionCharacters.Add(character);
            }
            if (m_ExplosionCharacters.Count > 0)
                m_ExplosionTeam = TurnManager.CurrentTeam;
        }

        private void RefreshExplosionCamera(bool selectTarget = true)
        {
            if (!ExplosionCameraEnabled || !HasExplosionCameraContext() ||
                (m_ExplosionTeam != null && m_ExplosionTeam != TurnManager.CurrentTeam))
            {
                ClearExplosionCamera();
                return;
            }
            m_ExplosionCharacters.RemoveAll(character => !IsExplosionCharacterMoving(character));
            if (!IsExplosionCharacterMoving(m_ExplosionFocus))
                m_ExplosionFocus = null;
            if (m_ExplosionCharacters.Count == 0)
            {
                ClearExplosionCamera();
                return;
            }
            if (!selectTarget || m_ExplosionSelectionMade)
                return;

            // Aggregate all hit batches before this LateUpdate; do not choose
            // based on Explosion Update order. Lock once, including after release.
            float nearestDistance = float.PositiveInfinity;
            foreach (MutinyCharacter character in m_ExplosionCharacters)
            {
                float distance = ((Vector2)GetPresentationPosition(character.transform) -
                                  (Vector2)transform.position).sqrMagnitude;
                if (distance < nearestDistance ||
                    (distance == nearestDistance && m_ExplosionFocus != null &&
                     character.GetInstanceID().CompareTo(m_ExplosionFocus.GetInstanceID()) < 0))
                {
                    nearestDistance = distance;
                    m_ExplosionFocus = character;
                }
            }
            m_ExplosionSelectionMade = true;
        }

        internal MutinyCharacter ExplosionFocusForVerification => m_ExplosionFocus;

        private static bool IsBoxPlacementWeapon(MutinyWeapon weapon) =>
            weapon is MutinyWoodenCrate || weapon is MutinyGunpowderBarrel;

        private bool TryGetAiBoxPlacementCameraTarget(out Vector3 target)
        {
            target = default;
            if (TurnManager == null ||
                (TurnManager.CurrentPhase != TurnPhase.ActionExecuting &&
                 TurnManager.CurrentPhase != TurnPhase.Settling) ||
                TurnManager.CurrentTeam == null || !TurnManager.CurrentTeam.IsAiControlled)
                return false;

            MutinyCharacter selected = TurnManager.CurrentTeam.SelectedCharacter;
            if (selected == null || !selected.IsAlive ||
                (selected.IsSelfThrown && selected.PhysicsBody != null && !selected.PhysicsBody.IsAtRest))
                return false;

            foreach (MutinyWeapon weapon in FindObjectsByType<MutinyWeapon>(FindObjectsSortMode.None))
            {
                if (weapon.Owner != selected || !weapon.isActiveAndEnabled)
                    continue;
                Vector2? planned = weapon.AiPlacementCameraTargetPixels;
                if (!planned.HasValue)
                    continue;
                target = MutinyPhysics.PixelToUnity(planned.Value.x, planned.Value.y);
                return true;
            }
            return false;
        }

        internal Vector3? AiBoxPlacementTargetForVerification =>
            TryGetAiBoxPlacementCameraTarget(out Vector3 target) ? target : (Vector3?)null;

        internal Transform FindActionTargetForVerification() => FindActionTarget();
        internal MutinyWeapon TrackedWeaponForVerification => m_TrackedWeapon;
        internal MutinySpeechController SpeechForVerification => m_Speech;
        internal void SetLevelRootForVerification(MutinyLevelRoot root) => m_LevelRoot = root;
        internal void AdvanceCameraForVerification(float deltaTime) => AdvanceCamera(deltaTime);
        internal void AdvanceCameraForVerification()
        {
            EnsureReferences();
            if (m_TurnPanTarget != null &&
                PanTowards(GetPresentationPosition(m_TurnPanTarget), OriginalTrackingPixelsPerTick))
                m_TurnPanTarget = null;
        }
        internal bool CanAcceptManualScrollingForVerification()
        {
            return !HasExplosionCameraTarget && m_TurnPanTarget == null &&
                   FindActionTarget() == null && CanUseManualScrolling();
        }

        public bool HasReachedVoodooTarget(MutinyCharacter target)
        {
            if (target == null || m_Camera == null || m_LevelRoot == null)
                return true;

            Vector3 desired = GetPresentationPosition(target.transform);
            desired.y += OriginalTrackingVerticalOffsetPixels / MutinyPhysics.PixelsPerUnit;
            desired.z = transform.position.z;
            desired = ClampPosition(desired);
            return Vector2.Distance(transform.position, desired) < 0.01f;
        }

        public bool IsPanningToCharacter(MutinyCharacter character)
        {
            return character != null && m_TurnPanTarget == character.transform;
        }

        private void RefreshTurnPanTarget()
        {
            if (TurnManager == null || TurnManager.CurrentTeam == m_PreviousTeam) return;
            m_PreviousTeam = TurnManager.CurrentTeam;
            m_TurnFocusCharacter = FindTurnPanCharacter(m_PreviousTeam);
            m_TurnPanTarget = m_TurnFocusCharacter != null ? m_TurnFocusCharacter.transform : null;
        }

        private MutinyCharacter FindTurnPanCharacter(MutinyTeam team)
        {
            if (team == null || team.Characters == null)
                return null;

            if (team.TotalTurnsTaken == 1)
            {
                MutinyCharacter captain = team.GetCaptain();
                if (captain != null)
                    return captain;
            }

            MutinyCharacter nearest = null;
            float nearestDistance = float.PositiveInfinity;
            Vector2 visualCentre = transform.position;
            for (int i = 0; i < team.Characters.Count; i++)
            {
                MutinyCharacter character = team.Characters[i];
                if (character == null || !character.IsAlive)
                    continue;

                float distance = Vector2.SqrMagnitude((Vector2)character.transform.position - visualCentre);
                if (distance < nearestDistance)
                {
                    nearest = character;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void AdvanceEdgeScrolling()
        {
            if (IsFrontendPageVisible())
            {
                m_DesktopScrollDirection = Vector2.zero;
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                return;
            }
            if (MutinyInputHub.Instance != null && MutinyInputHub.Instance.IsControllerActive)
            {
                AdvanceControllerScrolling(Time.deltaTime);
                return;
            }
            bool canScroll = CanUseManualScrolling() &&
                             TurnManager.CurrentTeam != null &&
                             !TurnManager.CurrentTeam.IsAiControlled &&
                             PlayerInput != null &&
                             !PlayerInput.IsActionMenuOpen &&
                             !PlayerInput.IsAiming;

            Vector2 direction = Vector2.zero;
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (canScroll)
            {
                bool scrollLeft = keyboard != null && (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed);
                bool scrollRight = keyboard != null && (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed);
                bool scrollDown = keyboard != null && (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed);
                bool scrollUp = keyboard != null && (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed);

                // Mobile builds expose pointer state for touch-driven UI, but
                // there is no persistent hover cursor. Treating that pointer as
                // a desktop mouse makes its idle/default position (commonly
                // 0,0) continuously trigger the left and bottom edge zones.
                if (ShouldUseMouseEdgeScrolling(Application.isMobilePlatform, mouse != null))
                {
                    Vector2 position = mouse.position.ReadValue();
                    Rect pixelRect = m_Camera != null ? m_Camera.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
                    float scale = Mathf.Min(Screen.width / OriginalHorizontalPixels, Screen.height / OriginalVerticalPixels);
                    float edgePixels = OriginalEdgePixels * scale;
                    CalculateMouseEdgeScroll(position, pixelRect, Screen.width, Screen.height, edgePixels,
                        ref scrollLeft, ref scrollRight, ref scrollDown, ref scrollUp);
                }

                if (scrollLeft)
                    direction.x = -1f;
                if (scrollRight)
                    direction.x = 1f;
                if (scrollDown)
                    direction.y = -1f;
                if (scrollUp)
                    direction.y = 1f;
            }

            m_DesktopScrollDirection = direction;

            float maximumSpeed = OriginalMaxScrollPixelsPerTick / MutinyPhysics.TimeStep;
            float acceleration = OriginalScrollAccelerationPixelsPerTick /
                                 (MutinyPhysics.TimeStep * MutinyPhysics.TimeStep);
            m_EdgeVelocityPixelsPerSecond = Vector2.MoveTowards(
                m_EdgeVelocityPixelsPerSecond,
                direction * maximumSpeed,
                acceleration * Time.deltaTime);

            Vector2 movementPixels = m_EdgeVelocityPixelsPerSecond * Time.deltaTime;
            Vector3 positionWorld = transform.position;
            positionWorld.x += movementPixels.x / MutinyPhysics.PixelsPerUnit;
            positionWorld.y += movementPixels.y / MutinyPhysics.PixelsPerUnit;
            SetClampedPosition(positionWorld);
        }

        private bool AdvanceControllerScrolling(float deltaTime)
        {
            MutinyInputHub hub = MutinyInputHub.Instance;
            bool canScroll = hub != null && hub.IsControllerActive && hub.CurrentContext == "board" &&
                             !HasExplosionCameraTarget &&
                             TurnManager != null && TurnManager.CurrentTeam != null &&
                             !TurnManager.CurrentTeam.IsAiControlled && CanUseManualScrolling() &&
                             PlayerInput != null && !PlayerInput.IsActionMenuOpen &&
                             (!PlayerInput.IsAiming || PlayerInput.IsControllerAiming) &&
                             (m_TurnPanTarget == null || PlayerInput.IsControllerAiming) &&
                             FindActionTarget() == null;
            if (!canScroll)
            {
                m_EdgeVelocityPixelsPerSecond = Vector2.zero;
                return false;
            }
            Vector2 direction = Vector2.ClampMagnitude(hub.Frame.Pan, 1f);
            if (direction.sqrMagnitude > 0f && PlayerInput.IsControllerAiming)
                m_TurnPanTarget = null;
            float maximumSpeed = OriginalMaxScrollPixelsPerTick / MutinyPhysics.TimeStep;
            float acceleration = OriginalScrollAccelerationPixelsPerTick /
                                 (MutinyPhysics.TimeStep * MutinyPhysics.TimeStep);
            m_EdgeVelocityPixelsPerSecond = Vector2.MoveTowards(m_EdgeVelocityPixelsPerSecond,
                direction * maximumSpeed, acceleration * deltaTime);
            Vector3 position = transform.position;
            position.x += m_EdgeVelocityPixelsPerSecond.x * deltaTime / MutinyPhysics.PixelsPerUnit;
            position.y += m_EdgeVelocityPixelsPerSecond.y * deltaTime / MutinyPhysics.PixelsPerUnit;
            SetClampedPosition(position);
            return true;
        }

        private bool CanUseManualScrolling()
        {
            if (TurnManager == null)
                return false;

            // The original camera has no blanket ActionExecuting lock. Once a
            // Pieces of Eight coin resolves, track=false; after panToCharacter
            // reaches the owner, ordinary edge/key scrolling is available while
            // the same weapon waits for the next throw.
            return TurnManager.CurrentPhase == TurnPhase.TurnActive ||
                   MutinyPiecesOfEight.HasPlayerAwaitingNextCoin(TurnManager.CurrentTeam) ||
                   MutinyVoodooDoll.HasPlayerCameraHandoff(TurnManager.CurrentTeam) ||
                   IsAwaitingPlayerBoxPlacement();
        }

        private bool IsAwaitingPlayerBoxPlacement()
        {
            return TurnManager != null && TurnManager.CurrentTeam != null &&
                   !TurnManager.CurrentTeam.IsAiControlled &&
                   PlayerInput != null && PlayerInput.IsAwaitingBoxPlacement;
        }

        internal bool CanUseManualScrollingForVerification() => CanUseManualScrolling();
        internal bool IsDesktopScrollArrowVisible =>
            !IsFrontendPageVisible() &&
            ShouldDrawScrollArrow(Application.isMobilePlatform, m_DesktopScrollDirection);
        internal Vector2 DesktopScrollDirectionForVerification => m_DesktopScrollDirection;

        private bool IsFrontendPageVisible()
        {
            if (m_Frontend == null)
                m_Frontend = FindAnyObjectByType<MutinyFrontendController>();
            return m_Frontend != null && m_Frontend.CurrentPage != MutinyFrontendPage.Gameplay;
        }
        internal static bool ShouldDrawScrollArrow(bool isMobilePlatform, Vector2 desktopDirection) =>
            !isMobilePlatform && desktopDirection.sqrMagnitude > 0f;
        internal Texture2D ScrollArrowTextureForVerification(bool diagonal)
        {
            EnsureScrollArrowTextures();
            return diagonal ? m_ScrollDiagonalTexture : m_ScrollCardinalTexture;
        }

        internal static bool ShouldUseMouseEdgeScrolling(bool isMobilePlatform, bool hasMouse)
        {
            return hasMouse && !isMobilePlatform;
        }

        internal static void CalculateMouseEdgeScroll(
            Vector2 mousePosition,
            Rect viewportPixelRect,
            float screenWidth,
            float screenHeight,
            float edgePixels,
            ref bool scrollLeft,
            ref bool scrollRight,
            ref bool scrollDown,
            ref bool scrollUp)
        {
            // The 40 px zone starts at the fixed-aspect viewport edge, but
            // its outward side extends through any letterbox/pillarbox bar.
            // Only positions outside the actual game window are ineligible.
            if (mousePosition.x < 0f || mousePosition.x > screenWidth ||
                mousePosition.y < 0f || mousePosition.y > screenHeight)
                return;

            scrollLeft |= mousePosition.x < viewportPixelRect.xMin + edgePixels;
            scrollRight |= mousePosition.x > viewportPixelRect.xMax - edgePixels;
            scrollDown |= mousePosition.y < viewportPixelRect.yMin + edgePixels;
            scrollUp |= mousePosition.y > viewportPixelRect.yMax - edgePixels;
        }

        /// <summary>The authored orthographic size for the 400 px tall viewport (6.25 at 32 PPU).</summary>
        internal static float OriginalOrthographicSize =>
            OriginalVerticalPixels * 0.5f / MutinyPhysics.PixelsPerUnit;

        internal static float ClampZoomScale(float zoomScale) =>
            Mathf.Clamp(zoomScale, MinimumZoomScale, MaximumZoomScale);

        internal static float ResolveOrthographicSize(float zoomScale) =>
            OriginalOrthographicSize / Mathf.Max(0.0001f, zoomScale);

        /// <summary>
        /// Two-finger pinch. distanceRatio is this frame's finger distance over
        /// the previous one, so spreading the fingers zooms the view in. Zoom is
        /// local presentation only: it never reaches the deterministic simulation.
        /// The mobile gate lives with the callers (CanStartMobilePinchZoom and the
        /// touch layer), so the scripted verification can drive this same path.
        /// </summary>
        public bool ZoomByPinchRatio(float distanceRatio) => ApplyZoomRatio(distanceRatio);

        // Verification drives the same production zoom application, exactly like
        // ApplyMobileTouchPanForVerification does for the touch pan.
        internal bool ZoomByPinchRatioForVerification(float distanceRatio) => ApplyZoomRatio(distanceRatio);

        private bool ApplyZoomRatio(float distanceRatio)
        {
            EnsureReferences();
            if (m_Camera == null || float.IsNaN(distanceRatio) || distanceRatio <= 0f)
                return false;

            float next = ClampZoomScale(m_ZoomScale * distanceRatio);
            if (Mathf.Approximately(next, m_ZoomScale))
                return false;

            m_ZoomScale = next;
            m_Camera.orthographicSize = ResolveOrthographicSize(m_ZoomScale);
            // A narrower or wider view can put the clamped centre outside the
            // visible window; re-apply the level bounds with the new framing.
            SetClampedPosition(transform.position);
            return true;
        }

        private void ResetZoom()
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();
            m_ZoomScale = 1f;
            if (m_Camera != null)
                m_Camera.orthographicSize = OriginalOrthographicSize;
        }

        public bool CanStartMobileTouchPan(bool allowWhileAiming = false)
        {
            EnsureReferences();
            return Application.isMobilePlatform &&
                   !HasExplosionCameraTarget &&
                   m_Camera != null && m_LevelRoot != null && TurnManager != null &&
                   TurnManager.CurrentTeam != null && !TurnManager.CurrentTeam.IsAiControlled &&
                   PlayerInput != null && !PlayerInput.IsActionMenuOpen &&
                   IsAimingCompatibleWithMobilePan(PlayerInput.IsAiming, allowWhileAiming) &&
                   m_TurnPanTarget == null && FindActionTarget() == null && CanUseManualScrolling();
        }

        internal static bool IsAimingCompatibleWithMobilePan(
            bool isAiming, bool allowWhileAiming)
        {
            return !isAiming || allowWhileAiming;
        }

        /// <summary>
        /// Pinch-zoom gate. It mirrors the touch-pan gate but deliberately stays
        /// true while the camera is auto-following an action or panning to its
        /// own target: zooming a tracked shot is useful and never fights the
        /// automatic pan.
        /// </summary>
        public bool CanStartMobilePinchZoom(bool allowWhileAiming = false)
        {
            EnsureReferences();
            return Application.isMobilePlatform &&
                   !HasExplosionCameraTarget &&
                   m_Camera != null && m_LevelRoot != null && TurnManager != null &&
                   TurnManager.CurrentTeam != null && !TurnManager.CurrentTeam.IsAiControlled &&
                   PlayerInput != null && !PlayerInput.IsActionMenuOpen &&
                   IsAimingCompatibleWithMobilePan(PlayerInput.IsAiming, allowWhileAiming) &&
                   CanUseManualScrolling();
        }

        public bool PanByMobileTouchDelta(Vector2 screenDelta, bool allowWhileAiming = false)
        {
            if (!CanStartMobileTouchPan(allowWhileAiming))
                return false;

            ApplyMobileTouchPan(screenDelta);
            return true;
        }

        internal void ApplyMobileTouchPanForVerification(Vector2 screenDelta)
        {
            EnsureReferences();
            ApplyMobileTouchPan(screenDelta);
        }

        private void ApplyMobileTouchPan(Vector2 screenDelta)
        {
            float viewportWidth = m_Camera != null ? m_Camera.pixelRect.width : Screen.width;
            float viewportHeight = m_Camera != null ? m_Camera.pixelRect.height : Screen.height;

            Vector2 worldDelta = ScreenDeltaToWorldDelta(
                screenDelta, viewportWidth, viewportHeight,
                m_Camera.orthographicSize, m_Camera.aspect);
            m_EdgeVelocityPixelsPerSecond = Vector2.zero;
            Vector3 next = transform.position;
            next.x -= worldDelta.x;
            next.y -= worldDelta.y;
            SetClampedPosition(next);
        }

        internal static Vector2 ScreenDeltaToWorldDelta(
            Vector2 screenDelta, float screenWidth, float screenHeight,
            float orthographicSize, float aspect)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
                return Vector2.zero;

            float visibleHeight = orthographicSize * 2f;
            float visibleWidth = visibleHeight * aspect;
            return new Vector2(
                screenDelta.x * visibleWidth / screenWidth,
                screenDelta.y * visibleHeight / screenHeight);
        }

        private MutinyTreasureChest FindFallingChest()
        {
            if (!MutinyTreasureChestManager.SystemEnabled)
                return null;

            // AIR-02 quick battle: the drop is fast enough to watch from the
            // normal turn view, so the camera deliberately stays with the action
            // instead of panning down with the chest.
            if (!ShouldTrackAirDrop(MutinyNetworkManager.QuickBattleActive))
                return null;

            MutinyTreasureChest[] chests = FindObjectsByType<MutinyTreasureChest>(FindObjectsSortMode.None);
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i] != null && chests[i].IsFalling && chests[i].TimeTaken < 100)
                    return chests[i];
            }
            return null;
        }

        internal static bool ShouldTrackAirDrop(bool quickBattle) => !quickBattle;

        private static Vector3 GetPresentationPosition(Transform target)
        {
            MutinyPhysicsBody body = target.GetComponent<MutinyPhysicsBody>();
            return body != null ? body.PresentationPosition : target.position;
        }

        private bool PanTowards(Vector3 targetWorld, float pixelsPerTick,
            float verticalOffsetPixels = OriginalTrackingVerticalOffsetPixels, float deltaTime = -1f)
        {
            Vector3 desired = targetWorld;
            desired.y += verticalOffsetPixels / MutinyPhysics.PixelsPerUnit;
            desired.z = transform.position.z;
            desired = ClampPosition(desired);

            float speedWorldPerSecond = pixelsPerTick /
                                        (MutinyPhysics.PixelsPerUnit * MutinyPhysics.TimeStep);
            Vector3 next = Vector3.MoveTowards(transform.position, desired,
                speedWorldPerSecond * (deltaTime >= 0f ? deltaTime : Time.deltaTime));
            SetClampedPosition(next);
            return Vector2.Distance(transform.position, desired) < 0.01f;
        }

        private void SetClampedPosition(Vector3 position)
        {
            transform.position = ClampPosition(position);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            float minX = 0f;
            float maxX = Mathf.Max(0f, m_LevelRoot.Width);
            float minY = -Mathf.Max(0f, m_LevelRoot.Height);
            float maxY = 0f;

            // Flash clamps cameraY to water.y - 320. With a 400 px viewport this
            // keeps the camera centre at least 120 px above the water line.
            float waterLimitedMinY = m_LevelRoot.WaterLevelY +
                                     120f / MutinyPhysics.PixelsPerUnit;
            minY = Mathf.Max(minY, waterLimitedMinY);

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            position.z = transform.position.z;
            return position;
        }

        public static Rect CalculateViewportRect(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                return new Rect(0f, 0f, 1f, 1f);

            float targetAspect = OriginalHorizontalPixels / OriginalVerticalPixels;
            float windowAspect = (float)screenWidth / screenHeight;

            if (Mathf.Abs(windowAspect - targetAspect) < 0.001f)
                return new Rect(0f, 0f, 1f, 1f);

            if (windowAspect > targetAspect)
            {
                float insetW = targetAspect / windowAspect;
                float insetX = (1f - insetW) * 0.5f;
                return new Rect(insetX, 0f, insetW, 1f);
            }
            else
            {
                float insetH = windowAspect / targetAspect;
                float insetY = (1f - insetH) * 0.5f;
                return new Rect(0f, insetY, 1f, insetH);
            }
        }

        public void ApplyViewportLetterbox()
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();

            if (m_Camera == null)
                return;

            Rect targetRect = CalculateViewportRect(Screen.width, Screen.height);
            if (m_Camera.rect != targetRect)
            {
                m_Camera.rect = targetRect;
            }
        }

        public static void DrawLetterboxBars()
        {
            float scale = Mathf.Min(Screen.width / OriginalHorizontalPixels, Screen.height / OriginalVerticalPixels);
            float canvasW = OriginalHorizontalPixels * scale;
            float canvasH = OriginalVerticalPixels * scale;
            float left = (Screen.width - canvasW) * 0.5f;
            float top = (Screen.height - canvasH) * 0.5f;

            Color oldColor = GUI.color;
            GUI.color = Color.black;

            if (left > 0.5f)
            {
                float rightX = left + canvasW;
                float rightW = Screen.width - rightX;
                GUI.DrawTexture(new Rect(0f, 0f, left, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
                GUI.DrawTexture(new Rect(rightX, 0f, rightW, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            }

            if (top > 0.5f)
            {
                float bottomY = top + canvasH;
                float bottomH = Screen.height - bottomY;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, top), Texture2D.whiteTexture, ScaleMode.StretchToFill);
                GUI.DrawTexture(new Rect(0f, bottomY, Screen.width, bottomH), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            }

            GUI.color = oldColor;
        }

        private void UpdateScrollCursorVisibility()
        {
            bool visible = IsDesktopScrollArrowVisible;
            if (visible)
                Cursor.visible = false;
            else if (m_DesktopScrollCursorWasVisible)
                Cursor.visible = PlayerInput == null || !PlayerInput.HasVisibleSpecialCursor;
            m_DesktopScrollCursorWasVisible = visible;
        }

        internal static bool ResolveScrollArrow(Vector2 screenDirection,
            out bool diagonal, out float rotationDegrees)
        {
            int x = screenDirection.x > 0f ? 1 : screenDirection.x < 0f ? -1 : 0;
            int y = screenDirection.y > 0f ? 1 : screenDirection.y < 0f ? -1 : 0;
            diagonal = x != 0 && y != 0;
            rotationDegrees = 0f;
            if (x == 0 && y == 0)
                return false;

            // TileSystem.advance: scroll1 base points right; scroll2 base
            // points up-right. Its 3x3 angle matrix plus 45 degrees for
            // diagonals yields these clockwise screen-space rotations.
            if (diagonal)
                rotationDegrees = y < 0 ? (x > 0 ? 0f : 270f) : (x > 0 ? 90f : 180f);
            else if (x < 0)
                rotationDegrees = 180f;
            else if (y > 0)
                rotationDegrees = 90f;
            else if (y < 0)
                rotationDegrees = 270f;
            return true;
        }

        private void EnsureScrollArrowTextures()
        {
            if (m_ScrollCardinalTexture == null)
                m_ScrollCardinalTexture = DecodeScrollArrow(ScrollCardinalPng, "OriginalCursor_Scroll1");
            if (m_ScrollDiagonalTexture == null)
                m_ScrollDiagonalTexture = DecodeScrollArrow(ScrollDiagonalPng, "OriginalCursor_Scroll2");
        }

        private static Texture2D DecodeScrollArrow(string encodedPng, string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (ImageConversion.LoadImage(texture, Convert.FromBase64String(encodedPng), true))
                return texture;
            Destroy(texture);
            return null;
        }

        private void DrawScrollArrow()
        {
            if (!IsDesktopScrollArrowVisible)
                return;

            Vector2 screenDirection = new Vector2(m_DesktopScrollDirection.x, -m_DesktopScrollDirection.y);
            if (!ResolveScrollArrow(screenDirection, out bool diagonal, out float rotation))
                return;
            EnsureScrollArrowTextures();
            Texture2D texture = diagonal ? m_ScrollDiagonalTexture : m_ScrollCardinalTexture;
            if (texture == null)
                return;

            float scale = Mathf.Min(Screen.width / OriginalHorizontalPixels,
                Screen.height / OriginalVerticalPixels);
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;
            Vector2 pointer = mouse.position.ReadValue();
            Vector2 centre = new Vector2(pointer.x, Screen.height - pointer.y);

            Rect arrowRect = new Rect(centre.x - 15f * scale,
                centre.y - 11f * scale, texture.width * scale, texture.height * scale);
            int previousDepth = GUI.depth;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.depth = -1000;
            GUIUtility.RotateAroundPivot(rotation, centre);
            GUI.DrawTexture(arrowRect, texture, ScaleMode.StretchToFill, true);
            GUI.matrix = previousMatrix;
            GUI.depth = previousDepth;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.depth = 1000;
                DrawLetterboxBars();
                DrawScrollArrow();
            }
        }
    }
}
