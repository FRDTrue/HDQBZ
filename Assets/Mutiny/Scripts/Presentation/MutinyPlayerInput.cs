using Mutiny.Diagnostics;
using Mutiny.Levels;
using Mutiny.Network;
using Mutiny.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mutiny.Presentation
{
    public enum MutinyPlayerInteractionState
    {
        CharacterSelection,
        ActionMenu,
        WeaponReady,
        Aiming
    }

    [DisallowMultipleComponent]
    public sealed class MutinyPlayerInput : MonoBehaviour
    {
        [Header("References")]
        public MutinyTurnManager TurnManager;
        public MutinyTrajectoryRenderer TrajectoryRenderer;
        public Camera GameCamera;

        [Header("Original Mouse Settings (pixels)")]
        public float CharacterSelectionRadiusPixels = 30f;
        public float DragSelectionRadiusPixels = 30f;
        public float MinDragDistancePixels = 5f;

        [Header("Android Touch Settings (screen pixels)")]
        public float MobileTapDragThresholdPixels = 24f;

        public string ActiveWeapon { get; private set; }
        public MutinyPlayerInteractionState InteractionState { get; private set; } =
            MutinyPlayerInteractionState.CharacterSelection;
        public bool IsActionMenuOpen => InteractionState == MutinyPlayerInteractionState.ActionMenu;
        public bool IsControllerAiming => m_ControllerAiming && InteractionState == MutinyPlayerInteractionState.Aiming;
        public float ControllerPower { get; private set; }
        [Min(0f)] public float ControllerPowerChangePerSecond = 0.5f;
        public MutinyCharacter ControllerFocusedCharacter => m_ControllerFocusedCharacter;
        public bool IsAiming => InteractionState == MutinyPlayerInteractionState.Aiming ||
                                (m_ArmedCannon != null &&
                                 (m_ArmedCannon.IsDraggingBody || m_ArmedCannon.IsDraggingPin));
        public bool IsWeaponReady => InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                                     EquippedWeapon != null;
        public bool IsAwaitingBoxPlacement => HasPendingBoxPlacement();
        public MutinyWeapon EquippedWeapon => m_EquippedWeapon != null && !m_EquippedWeapon.IsFired
            ? m_EquippedWeapon
            : null;
        public MutinyVoodooDoll ArmedVoodooDoll => m_ArmedVoodooDoll != null && !m_ArmedVoodooDoll.IsFired
            ? m_ArmedVoodooDoll
            : null;
        public MutinyWoodenCrate ArmedWoodenCrate => m_ArmedWoodenCrate != null &&
                                                       m_ArmedWoodenCrate.HasPendingPlacement
            ? m_ArmedWoodenCrate
            : null;
        public MutinyGunpowderBarrel ArmedGunpowderBarrel => m_ArmedGunpowderBarrel != null &&
                                                               m_ArmedGunpowderBarrel.HasPendingPlacement
            ? m_ArmedGunpowderBarrel
            : null;
        public MutinyAnchor ArmedAnchor => m_ArmedAnchor != null && !m_ArmedAnchor.IsFired
            ? m_ArmedAnchor
            : null;
        public MutinyCannon ArmedCannon => m_ArmedCannon != null && !m_ArmedCannon.IsFinished
            ? m_ArmedCannon
            : null;
        public bool IsControllerCannonReady => InteractionState == MutinyPlayerInteractionState.WeaponReady &&
            m_ArmedCannon != null && !m_ArmedCannon.IsFired && !m_ArmedCannon.IsFirePending;
        public MutinyPiecesOfEight ArmedPiecesOfEight => m_ArmedPiecesOfEight != null &&
                                                          !m_ArmedPiecesOfEight.IsFinished
            ? m_ArmedPiecesOfEight
            : null;

        // Legacy query name used by proximity/pickup consumers: this reports
        // self-throw AIMING (Controller.twanging), not physical dragging.
        // Never use it to suppress the character overlay: Character is not draggable.
        public bool IsCharacterThrowDragInProgress(MutinyCharacter character)
        {
            return character != null &&
                   InteractionState == MutinyPlayerInteractionState.Aiming &&
                   string.IsNullOrEmpty(ActiveWeapon) &&
                   TurnManager != null &&
                   TurnManager.CurrentTeam != null &&
                   TurnManager.CurrentTeam.SelectedCharacter == character;
        }

        private Vector3 m_AimOrigin;
        private bool m_ControllerAiming;
        private bool m_ControllerTriggerMustRelease = true;
        // Pixel-space drag direction. Solid.twang launches opposite the drag endpoint.
        private Vector2 m_ControllerPullDirection;
        private MutinyCharacter m_ControllerFocusedCharacter;
        private bool m_ControllerCharacterFocusInitialized;
        private string[,] m_CachedTerrain;
        private int m_GridWidth;
        private int m_GridHeight;
        private MutinyTeam m_ObservedTeam;
        private bool m_WasTurnActive;
        private MutinyCharacter m_HoveredCharacter;
        private MutinyVoodooDoll m_ArmedVoodooDoll;
        private MutinyWoodenCrate m_ArmedWoodenCrate;
        private MutinyAnchor m_ArmedAnchor;
        private MutinyGunpowderBarrel m_ArmedGunpowderBarrel;
        private MutinyCannon m_ArmedCannon;
        private MutinyWeapon m_EquippedWeapon;
        private MutinySpecialWeaponCursor m_SpecialWeaponCursor;
        private const int NoActiveTouchId = int.MinValue;
        private int m_ActiveTouchId = NoActiveTouchId;
        private Vector2 m_LastTouchPosition;
        private bool m_IsMobileCameraDragging;
        private Vector2 m_LastMobileCameraDragPosition;
        private bool m_IsPendingMobileTap;
        private Vector2 m_MobileTapStartPosition;
        private int m_SecondaryCameraTouchId = NoActiveTouchId;
        private Vector2 m_LastSecondaryCameraTouchPosition;
        // Two-finger pinch. The fingers only become a zoom once their distance
        // changes past the deadzone; below it the second finger keeps its
        // original camera-pan role.
        private const float PinchStartDeadzonePixels = 6f;
        private bool m_IsPinching;
        private float m_PinchPreviousDistancePixels = -1f;
        // Unlike ordinary projectiles, PiecesOfEight remains equipped and is reused
        // for coin 2..8 while the turn stays in ActionExecuting.
        private MutinyPiecesOfEight m_ArmedPiecesOfEight;

        // Online lockstep: replayed remote commands re-enter the commit points
        // below; the flag stops them from being echoed back to the peer and
        // exempts them from the turn-ownership gates, because a peer's commit
        // is trusted simulation input even though this machine does not own
        // that team.
        private bool m_ReplayingRemoteCommand;
        private bool m_RemoteHooked;
        // Final DragBodyTo pointer of the current cannon interaction; the swept
        // body move in ReleasePointer needs this value to replay identically.
        private Vector2? m_CannonBodyDragTargetPixels;

        private void Start()
        {
            if (TurnManager == null)
                TurnManager = FindAnyObjectByType<MutinyTurnManager>();

            if (TrajectoryRenderer == null)
            {
                var trObj = new GameObject("TrajectoryRenderer");
                trObj.transform.SetParent(transform, false);
                trObj.AddComponent<LineRenderer>();
                TrajectoryRenderer = trObj.AddComponent<MutinyTrajectoryRenderer>();
            }

            if (GameCamera == null)
                GameCamera = Camera.main;

            if (GameCamera != null)
            {
                MutinyCameraController cameraController = GameCamera.GetComponent<MutinyCameraController>();
                if (cameraController == null)
                    cameraController = GameCamera.gameObject.AddComponent<MutinyCameraController>();
                cameraController.TurnManager = TurnManager;
                cameraController.PlayerInput = this;
            }

            EnsureSpecialWeaponCursor();

            CacheTerrain();
            ResetForCurrentTurn();
        }

        public void CacheTerrain()
        {
            var controller = FindAnyObjectByType<MutinyLevelController>();
            if (controller != null && controller.LevelXml != null)
            {
                var levelData = MutinyLevelXmlParser.Parse(controller.LevelXml.text);
                m_CachedTerrain = levelData.Terrain;
                m_GridWidth = levelData.Width;
                m_GridHeight = levelData.Height;
            }
        }

        private void Update()
        {
            HookNetworkIfPresent();

            // popup is above the game root in the Flash timeline. While it is
            // visible (including the four-tick fade) its buttons consume mouse
            // input; character selection and aiming must not see the same click.
            if (MutinyTransitionManager.IsTransitionActive)
            {
                CancelControllerAim();
                ResetMobileGestureOwnership();
                ClearSpecialWeaponCursor();
                ClearHoveredCharacter();
                if (InteractionState == MutinyPlayerInteractionState.Aiming)
                    HideTrajectory();
                return;
            }

            MutinyGameHUD hud = FindAnyObjectByType<MutinyGameHUD>();
            if (hud != null && (hud.IsQuitPromptShowRequested || hud.IsQuitPromptVisible ||
                                (hud.Speech != null && hud.Speech.HasActiveBubble)))
            {
                // Android touch input does not have to produce an IMGUI mouse
                // event. Forward its first press through the same speech state
                // machine before blocking gameplay input.
                if (hud.Speech != null && hud.Speech.HasActiveBubble && Application.isMobilePlatform &&
                    TryReadPointer(out PointerFrameState speechPointer) && speechPointer.PressedThisFrame)
                {
                    Vector2 canvasPoint = MutinyGameHUD.ScreenToCanvasPoint(
                        new Vector2(speechPointer.Position.x, Screen.height - speechPointer.Position.y),
                        Screen.width, Screen.height);
                    if (new Rect(0f, 0f, 550f, 400f).Contains(canvasPoint))
                        hud.Speech.Click();
                }
                ResetMobileGestureOwnership();
                ClearSpecialWeaponCursor();
                ClearHoveredCharacter();
                if (InteractionState == MutinyPlayerInteractionState.Aiming)
                    HideTrajectory();
                return;
            }

            if (!CanProcessCurrentTurnInput())
            {
                CancelControllerAim();
                ResetControllerCharacterFocus();
                ResetMobileGestureOwnership();
                ClearSpecialWeaponCursor();
                ClearHoveredCharacter();
                m_WasTurnActive = false;
                if (InteractionState == MutinyPlayerInteractionState.Aiming)
                    HideTrajectory();
                return;
            }

            MutinyTeam currentTeam = TurnManager.CurrentTeam;
            bool placingBoxWeapon = HasPendingBoxPlacement();
            bool awaitingPiecesOfEight = IsAwaitingPiecesOfEight();
            if (currentTeam != m_ObservedTeam)
                ResetForCurrentTurn();
            else if (!m_WasTurnActive &&
                     currentTeam != null &&
                     !currentTeam.IsAiControlled &&
                     currentTeam.SelectedCharacter != null &&
                     currentTeam.SelectedCharacter.IsAlive &&
                     InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                     !placingBoxWeapon && !awaitingPiecesOfEight)
            {
                // A character throw leaves the weapon action available. Reopen the
                // action menu when the board has settled.
                ClearEquippedWeapon();
                InteractionState = MutinyPlayerInteractionState.ActionMenu;
                ActiveWeapon = null;
            }

            m_WasTurnActive = true;

            if (currentTeam == null || currentTeam.IsAiControlled)
            {
                ClearSpecialWeaponCursor();
                ClearHoveredCharacter();
                return;
            }

            MutinyInputHub controllerHub = MutinyInputHub.Instance;
            if (controllerHub != null && controllerHub.IsControllerActive)
            {
                ResetMobileGestureOwnership();
                MutinyCharacter selected = currentTeam.SelectedCharacter;
                if (selected != null && selected.IsAlive)
                {
                    UpdateSpecialWeaponCursor(selected, controllerHub.PointerPosition,
                        controllerHub.IsConfirmHeld);
                    UpdateHoveredCharacter(currentTeam,
                        GetMouseWorldPosition(controllerHub.PointerPosition));
                }
                else ClearSpecialWeaponCursor();
                if (controllerHub.TryConsumeGameplay(out MutinyInputHub.ControllerFrame controllerFrame))
                    ProcessControllerFrame(controllerFrame, controllerHub);
                return;
            }

            ResetControllerCharacterFocus();
            UpdateSecondaryMobileCameraTouch();

            if (!TryReadPointer(out PointerFrameState pointer))
            {
                ClearSpecialWeaponCursor();
                ClearHoveredCharacter();
                return;
            }

            if (pointer.CanceledThisFrame)
            {
                HandlePointerCancellation();
                return;
            }

            Vector3 mouseWorld = GetMouseWorldPosition(pointer.Position);

            if (m_IsPendingMobileTap)
            {
                AdvanceMobileTapCandidate(pointer, currentTeam, mouseWorld);
                return;
            }

            if (m_IsMobileCameraDragging)
            {
                AdvanceMobileCameraDrag(pointer);
                return;
            }

            // Global weapon triggers in flight: Seagull and Banana take priority
            // over any character selection or action states.
            if (pointer.PressedThisFrame)
            {
                if (MutinySeagull.TryRequestPlayerShot(currentTeam))
                {
                    DispatchLocal(MutinyNetCommandType.SeagullShot, currentTeam.TeamNumber, -1,
                        null, default, default, -1);
                    return;
                }

                if (MutinyBanana.TryRequestPlayerDetonation(currentTeam))
                {
                    DispatchLocal(MutinyNetCommandType.BananaDetonate, currentTeam.TeamNumber, -1,
                        null, default, default, -1);
                    return;
                }
            }

            UpdateHoveredCharacter(currentTeam, mouseWorld);

            // A new turn begins without SelectedCharacter.  Character-selection
            // input must therefore run before any code that requires one.
            if (InteractionState == MutinyPlayerInteractionState.CharacterSelection)
            {
                ClearSpecialWeaponCursor();
                if (pointer.PressedThisFrame)
                {
                    MutinyCharacter clicked = FindCharacterNearPosition(
                        mouseWorld, currentTeam, CharacterSelectionRadiusPixels);
                    if (clicked != null)
                        TrySelectCharacter(currentTeam, mouseWorld);
                    else
                        TryBeginMobileCameraDrag(pointer.Position);
                }
                return;
            }

            MutinyCharacter selectedCharacter = currentTeam.SelectedCharacter;
            if (selectedCharacter == null || !selectedCharacter.IsAlive)
            {
                ClearSpecialWeaponCursor();
                ReturnToCharacterSelection();
                return;
            }

            UpdateSpecialWeaponCursor(selectedCharacter, pointer.Position, pointer.IsPressed);

            if (ShouldShowCancelWeapon(selectedCharacter) && selectedCharacter.PhysicsBody != null)
            {
                Vector2 mousePixels = MutinyPhysics.UnityToPixel(mouseWorld);
                float centerX = selectedCharacter.PhysicsBody.State.X;
                float centerY = selectedCharacter.PhysicsBody.State.Y + 33f;
                if (Mathf.Abs(mousePixels.x - centerX) <= 10f && Mathf.Abs(mousePixels.y - centerY) <= 10f)
                {
                    MutinyCursorManager.NotifyHoverInteractable();
                }
            }

            if (InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                (m_SpecialWeaponCursor == null || m_SpecialWeaponCursor.CurrentMode == MutinySpecialWeaponCursor.Mode.None))
            {
                Vector3 readyOrigin = GetReadyActionOrigin(selectedCharacter);
                float distancePixels = PixelDistance(mouseWorld, readyOrigin);
                if (distancePixels <= DragSelectionRadiusPixels && CanAim(selectedCharacter))
                {
                    MutinyCursorManager.NotifyHoverInteractable();
                }
            }

            // CancelWeaponButton.onPress is consumed before TileSystem begins a
            // drag.
            if (TryHandleCancelOverlayPrimaryPointer(selectedCharacter, mouseWorld, pointer.PressedThisFrame))
                return;

            if (pointer.SecondaryPressedThisFrame &&
                TryCancelAimFromSecondaryPointer())
            {
                return;
            }

            if (InteractionState == MutinyPlayerInteractionState.ActionMenu)
                return;

            if (InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                ShouldHandleWeaponReadyPrimaryInput(
                    pointer.PressedThisFrame, pointer.IsPressed))
            {
                if (Application.isMobilePlatform && IsMobileTapActivatedInteraction())
                {
                    BeginMobileTapCandidate(pointer.Position);
                    return;
                }

                bool cannonSelected = ActiveWeapon != null &&
                    ActiveWeapon.Equals("cannon", System.StringComparison.OrdinalIgnoreCase);
                if (TryHandleCannonInput(pointer, selectedCharacter, mouseWorld))
                    return;
                if (cannonSelected)
                {
                    TryBeginMobileCameraDrag(pointer.Position);
                    return;
                }

                if (TrySelectVoodooTarget(currentTeam, selectedCharacter, mouseWorld))
                    return;

                if (TryActivateClickWeapon(selectedCharacter, mouseWorld))
                    return;

                Vector3 readyOrigin = GetReadyActionOrigin(selectedCharacter);
                float distancePixels = PixelDistance(mouseWorld, readyOrigin);
                if (distancePixels <= DragSelectionRadiusPixels && CanAim(selectedCharacter))
                {
                    TryBeginAimFromPrimaryPointer(selectedCharacter, mouseWorld);
                }
                else
                {
                    TryBeginMobileCameraDrag(pointer.Position);
                }
            }

            if (InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                ActiveWeapon != null && ActiveWeapon.Equals("cannon", System.StringComparison.OrdinalIgnoreCase) &&
                TryHandleCannonInput(pointer, selectedCharacter, mouseWorld))
                return;

            AdvanceAimPointer(selectedCharacter, mouseWorld, pointer.IsPressed, pointer.ReleasedThisFrame);
        }

        // Banana.advanceMotion checks Controller.tileSystem.mouseButtonDown after
        // Weapon.fire has spent CanShoot. Its next click therefore must remain
        // routable while this team is in ActionExecuting, unlike ordinary input.
        private bool CanProcessCurrentTurnInput()
        {
            if (TurnManager == null)
                return false;

            // Online lockstep: only the owning side may act on its turn. The
            // remote replay path bypasses this method entirely.
            if (!IsLocalPlayerTurn())
                return false;

            bool placingBoxWeapon = HasPendingBoxPlacement();
            bool awaitingPiecesOfEight = IsAwaitingPiecesOfEight();
            bool awaitingBananaDetonation = MutinyBanana.HasPlayerDetonatableBanana(TurnManager.CurrentTeam);
            bool awaitingSeagullShot = MutinySeagull.HasPlayerActiveFlight(TurnManager.CurrentTeam);
            bool awaitingParachuteFan = MutinyParachuteBomb.HasPlayerActiveFlight(TurnManager.CurrentTeam);
            return TurnManager.CurrentPhase == TurnPhase.TurnActive || placingBoxWeapon ||
                   awaitingPiecesOfEight || awaitingBananaDetonation || awaitingSeagullShot ||
                   awaitingParachuteFan;
        }

        internal readonly struct PointerFrameState
        {
            public readonly Vector2 Position;
            public readonly bool PressedThisFrame;
            public readonly bool IsPressed;
            public readonly bool ReleasedThisFrame;
            public readonly bool CanceledThisFrame;
            public readonly bool SecondaryPressedThisFrame;

            public PointerFrameState(
                Vector2 position,
                bool pressedThisFrame,
                bool isPressed,
                bool releasedThisFrame,
                bool canceledThisFrame,
                bool secondaryPressedThisFrame = false)
            {
                Position = position;
                PressedThisFrame = pressedThisFrame;
                IsPressed = isPressed;
                ReleasedThisFrame = releasedThisFrame;
                CanceledThisFrame = canceledThisFrame;
                SecondaryPressedThisFrame = secondaryPressedThisFrame;
            }
        }

        /// <summary>
        /// True when this machine may drive the team that is currently acting.
        /// Outside a LAN match every local team qualifies; inside one, only the
        /// team this peer owns does, so neither player can see or use the
        /// opponent's menus.
        /// </summary>
        private bool IsLocalPlayerTurn()
        {
            if (!MutinyNetworkManager.IsOnlineMatch)
                return true;
            return TurnManager != null && TurnManager.CurrentTeam != null &&
                   MutinyNetworkManager.IsLocalTeam(TurnManager.CurrentTeam.TeamNumber);
        }

        private bool TryReadPointer(out PointerFrameState pointer)
        {
            if (Application.isMobilePlatform)
                return TryReadTouchPointer(Touchscreen.current, out pointer);

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                pointer = default;
                return false;
            }

            pointer = new PointerFrameState(
                mouse.position.ReadValue(),
                mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.isPressed,
                mouse.leftButton.wasReleasedThisFrame,
                false,
                mouse.rightButton.wasPressedThisFrame);
            return true;
        }

        private bool TryReadTouchPointer(Touchscreen touchscreen, out PointerFrameState pointer)
        {
            if (touchscreen == null)
            {
                pointer = default;
                return false;
            }

            if (m_ActiveTouchId == NoActiveTouchId)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var candidate = touchscreen.touches[i];
                    if (!ShouldAcquireTouch(m_ActiveTouchId, candidate.press.wasPressedThisFrame))
                        continue;

                    m_ActiveTouchId = candidate.touchId.ReadValue();
                    break;
                }
            }

            if (m_ActiveTouchId == NoActiveTouchId)
            {
                pointer = default;
                return false;
            }

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                if (touch.touchId.ReadValue() != m_ActiveTouchId)
                    continue;

                Vector2 position = touch.position.ReadValue();
                m_LastTouchPosition = position;
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                pointer = CreateTouchPointerState(
                    position,
                    phase,
                    touch.press.wasPressedThisFrame,
                    touch.press.isPressed,
                    touch.press.wasReleasedThisFrame);

                if (touch.press.wasReleasedThisFrame || pointer.CanceledThisFrame)
                {
                    m_ActiveTouchId = NoActiveTouchId;
                    // A pinch without its first finger cannot continue.
                    EndPinchGesture();
                }
                return true;
            }

            // A platform focus change can remove a touch slot without delivering
            // a normal Ended event. Convert that loss into cancellation so an aim
            // or cannon drag can never remain latched or accidentally fire.
            pointer = new PointerFrameState(
                m_LastTouchPosition, false, false, false, true);
            m_ActiveTouchId = NoActiveTouchId;
            return true;
        }

        private static bool ShouldAcquireTouch(int activeTouchId, bool wasPressedThisFrame)
        {
            return activeTouchId == NoActiveTouchId && wasPressedThisFrame;
        }

        internal static bool ShouldAcquireTouchForVerification(
            bool alreadyHasActiveTouch, bool wasPressedThisFrame)
        {
            return ShouldAcquireTouch(
                alreadyHasActiveTouch ? 1 : NoActiveTouchId,
                wasPressedThisFrame);
        }

        internal static PointerFrameState TouchPhaseToPointerStateForVerification(
            UnityEngine.InputSystem.TouchPhase phase)
        {
            bool pressed = phase == UnityEngine.InputSystem.TouchPhase.Began;
            bool held = pressed || phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                        phase == UnityEngine.InputSystem.TouchPhase.Stationary;
            bool released = phase == UnityEngine.InputSystem.TouchPhase.Ended;
            return CreateTouchPointerState(Vector2.zero, phase, pressed, held, released);
        }

        private static PointerFrameState CreateTouchPointerState(
            Vector2 position,
            UnityEngine.InputSystem.TouchPhase phase,
            bool pressedThisFrame,
            bool isPressed,
            bool releasedThisFrame)
        {
            bool canceled = phase == UnityEngine.InputSystem.TouchPhase.Canceled;
            return new PointerFrameState(
                position,
                pressedThisFrame,
                isPressed,
                releasedThisFrame && !canceled,
                canceled);
        }

        private void HandlePointerCancellation()
        {
            m_IsMobileCameraDragging = false;
            m_IsPendingMobileTap = false;
            EndPinchGesture();
            m_ArmedCannon?.CancelPointer();
            if (InteractionState == MutinyPlayerInteractionState.Aiming)
                CancelCurrentAim();
            HideTrajectory();
            ClearSpecialWeaponCursor();
            ClearHoveredCharacter();
        }

        private bool IsMobileTapActivatedInteraction()
        {
            if (IsVoodooTargetSelection())
                return true;
            if (string.IsNullOrEmpty(ActiveWeapon))
                return false;

            return ActiveWeapon.Equals("woodenCrate", System.StringComparison.OrdinalIgnoreCase) ||
                   ActiveWeapon.Equals("gunpowderBarrel", System.StringComparison.OrdinalIgnoreCase) ||
                   ActiveWeapon.Equals("anchor", System.StringComparison.OrdinalIgnoreCase) ||
                   ActiveWeapon.Equals("seagull", System.StringComparison.OrdinalIgnoreCase) ||
                   ActiveWeapon.Equals("tidalWave", System.StringComparison.OrdinalIgnoreCase);
        }

        private void BeginMobileTapCandidate(Vector2 screenPosition)
        {
            m_IsPendingMobileTap = true;
            m_MobileTapStartPosition = screenPosition;
        }

        private void AdvanceMobileTapCandidate(
            PointerFrameState pointer, MutinyTeam currentTeam, Vector3 pointerWorld)
        {
            float dragDistance = Vector2.Distance(pointer.Position, m_MobileTapStartPosition);
            if (ShouldConvertPendingTapToCameraDrag(
                    dragDistance, MobileTapDragThresholdPixels))
            {
                m_IsPendingMobileTap = false;
                if (TryBeginMobileCameraDrag(m_MobileTapStartPosition))
                    AdvanceMobileCameraDrag(pointer);
                return;
            }

            if (!pointer.ReleasedThisFrame)
                return;

            m_IsPendingMobileTap = false;
            MutinyCharacter selectedCharacter = currentTeam != null
                ? currentTeam.SelectedCharacter
                : null;
            if (selectedCharacter == null || !selectedCharacter.IsAlive)
                return;
            if (TrySelectVoodooTarget(currentTeam, selectedCharacter, pointerWorld))
                return;
            TryActivateClickWeapon(selectedCharacter, pointerWorld);
        }

        internal static bool ShouldConvertPendingTapToCameraDrag(
            float dragDistancePixels, float thresholdPixels)
        {
            return dragDistancePixels > Mathf.Max(0f, thresholdPixels);
        }

        private bool TryBeginMobileCameraDrag(Vector2 screenPosition)
        {
            if (!Application.isMobilePlatform || IsAiming)
                return false;

            if (GameCamera == null)
                GameCamera = Camera.main;
            MutinyCameraController cameraController =
                GameCamera != null ? GameCamera.GetComponent<MutinyCameraController>() : null;
            if (cameraController == null || !cameraController.CanStartMobileTouchPan())
                return false;

            m_IsMobileCameraDragging = true;
            m_LastMobileCameraDragPosition = screenPosition;
            ClearSpecialWeaponCursor();
            ClearHoveredCharacter();
            return true;
        }

        private void AdvanceMobileCameraDrag(PointerFrameState pointer)
        {
            Vector2 delta = pointer.Position - m_LastMobileCameraDragPosition;
            m_LastMobileCameraDragPosition = pointer.Position;

            if ((pointer.IsPressed || pointer.ReleasedThisFrame) &&
                delta.sqrMagnitude > Mathf.Epsilon)
            {
                MutinyCameraController cameraController =
                    GameCamera != null ? GameCamera.GetComponent<MutinyCameraController>() : null;
                if (cameraController == null || !cameraController.PanByMobileTouchDelta(delta))
                    m_IsMobileCameraDragging = false;
            }

            if (pointer.ReleasedThisFrame || !pointer.IsPressed)
                m_IsMobileCameraDragging = false;
        }

        /// <summary>True while the two-finger pinch owns the touch gesture.</summary>
        internal bool IsPinching => m_IsPinching;

        internal static bool ShouldStartPinch(float distanceChangePixels, float deadzonePixels)
        {
            return Mathf.Abs(distanceChangePixels) > Mathf.Max(0f, deadzonePixels);
        }

        internal static float ResolvePinchDistanceRatio(
            float previousDistancePixels, float currentDistancePixels)
        {
            return previousDistancePixels > 0.0001f
                ? currentDistancePixels / previousDistancePixels
                : 1f;
        }

        /// <summary>
        /// Production two-finger step. The first observed distance only sets the
        /// baseline; once the fingers move apart or together past the deadzone
        /// the gesture becomes a pinch and owns both fingers, so the ordinary
        /// secondary-touch camera pan stops for the rest of that gesture.
        /// </summary>
        internal bool AdvancePinchGesture(Vector2 primaryPixels, Vector2 secondaryPixels)
        {
            float distance = Vector2.Distance(primaryPixels, secondaryPixels);
            float previous = m_PinchPreviousDistancePixels;
            m_PinchPreviousDistancePixels = distance;
            if (previous < 0f)
                return false;
            if (!m_IsPinching && !ShouldStartPinch(distance - previous, PinchStartDeadzonePixels))
                return false;

            m_IsPinching = true;
            MutinyCameraController controller = GetGameCameraController();
            if (controller != null)
                controller.ZoomByPinchRatio(ResolvePinchDistanceRatio(previous, distance));
            return true;
        }

        private void EndPinchGesture()
        {
            m_IsPinching = false;
            m_PinchPreviousDistancePixels = -1f;
        }

        private Vector2 ReadActiveTouchPosition(Touchscreen touchscreen)
        {
            if (touchscreen != null)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var touch = touchscreen.touches[i];
                    if (touch.touchId.ReadValue() == m_ActiveTouchId)
                        return touch.position.ReadValue();
                }
            }
            return m_LastTouchPosition;
        }

        private void UpdateSecondaryMobileCameraTouch()
        {
            if (!Application.isMobilePlatform)
                return;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                m_SecondaryCameraTouchId = NoActiveTouchId;
                EndPinchGesture();
                return;
            }

            if (m_SecondaryCameraTouchId != NoActiveTouchId)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var touch = touchscreen.touches[i];
                    if (touch.touchId.ReadValue() != m_SecondaryCameraTouchId)
                        continue;

                    UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        m_SecondaryCameraTouchId = NoActiveTouchId;
                        EndPinchGesture();
                        return;
                    }

                    Vector2 position = touch.position.ReadValue();
                    Vector2 delta = position - m_LastSecondaryCameraTouchPosition;
                    m_LastSecondaryCameraTouchPosition = position;
                    bool released = phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                                    touch.press.wasReleasedThisFrame;

                    // Pinch first: once the two fingers change their distance the
                    // zoom owns the gesture, so the secondary finger never pans
                    // and no new tap or drag candidate starts underneath it.
                    if (AdvancePinchGesture(ReadActiveTouchPosition(touchscreen), position))
                    {
                        m_IsMobileCameraDragging = false;
                        m_IsPendingMobileTap = false;
                    }
                    else if ((touch.press.isPressed || released) &&
                             delta.sqrMagnitude > Mathf.Epsilon)
                    {
                        MutinyCameraController controller = GetGameCameraController();
                        if (controller == null ||
                            !controller.PanByMobileTouchDelta(delta, true))
                            m_SecondaryCameraTouchId = NoActiveTouchId;
                    }

                    if (released)
                    {
                        m_SecondaryCameraTouchId = NoActiveTouchId;
                        EndPinchGesture();
                    }
                    return;
                }

                m_SecondaryCameraTouchId = NoActiveTouchId;
                EndPinchGesture();
                return;
            }

            if (m_ActiveTouchId == NoActiveTouchId || m_IsMobileCameraDragging)
                return;

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var candidate = touchscreen.touches[i];
                int touchId = candidate.touchId.ReadValue();
                if (touchId == m_ActiveTouchId || !candidate.press.wasPressedThisFrame)
                    continue;

                // The first finger remains down. A second finger pressing the
                // cross cancels the aim before it can become a camera gesture.
                if (TryCancelAimFromSecondaryTouch(
                        GetMouseWorldPosition(candidate.position.ReadValue())))
                    return;

                MutinyCameraController cameraController = GetGameCameraController();
                bool aiming = IsAiming;
                bool canPan = cameraController != null &&
                              cameraController.CanStartMobileTouchPan(aiming);
                bool canZoom = cameraController != null &&
                               cameraController.CanStartMobilePinchZoom(aiming);
                if (!canPan && !canZoom)
                    return;

                m_SecondaryCameraTouchId = touchId;
                m_LastSecondaryCameraTouchPosition = candidate.position.ReadValue();
                EndPinchGesture();
                return;
            }
        }

        private MutinyCameraController GetGameCameraController()
        {
            if (GameCamera == null)
                GameCamera = Camera.main;
            return GameCamera != null
                ? GameCamera.GetComponent<MutinyCameraController>()
                : null;
        }

        private void UpdateSpecialWeaponCursor(
            MutinyCharacter selectedCharacter, Vector2 mousePosition, bool leftButtonHeld)
        {
            EnsureSpecialWeaponCursor();

            MutinySpecialWeaponCursor.Mode mode = MutinySpecialWeaponCursor.Mode.None;
            float rotationDegrees = 0f;
            bool animateFan = false;
            MutinyParachuteBomb activeBomb = MutinyParachuteBomb.FindPlayerActiveFlight(TurnManager?.CurrentTeam);
            if (activeBomb != null)
            {
                mode = MutinySpecialWeaponCursor.Mode.ParachuteFan;
                float mousePixelX = GetMouseWorldPixelX(mousePosition);
                rotationDegrees = mousePixelX >= activeBomb.PhysicsBody.State.X ? -90f : 90f;
                animateFan = leftButtonHeld &&
                    (MutinyInputHub.Instance == null || !MutinyInputHub.Instance.IsControllerActive ||
                     activeBomb.ControllerFanCanStart);
            }
            else if (selectedCharacter != null && InteractionState == MutinyPlayerInteractionState.WeaponReady)
            {
                if (m_ArmedAnchor != null && m_ArmedAnchor.Owner == selectedCharacter &&
                    !m_ArmedAnchor.IsFired && !m_ArmedAnchor.IsFinished)
                {
                    mode = MutinySpecialWeaponCursor.Mode.Anchor;
                }
                else if (IsVoodooTargetSelection() && m_ArmedVoodooDoll.Owner == selectedCharacter &&
                         !IsPointerOverVoodooDoll(mousePosition))
                {
                    mode = MutinySpecialWeaponCursor.Mode.VoodooDoll;
                }
                else if (string.Equals(ActiveWeapon, "seagull", System.StringComparison.OrdinalIgnoreCase) ||
                    (m_EquippedWeapon is MutinySeagull && !m_EquippedWeapon.IsFired))
                {
                    mode = MutinySpecialWeaponCursor.Mode.Seagull;
                }
                else if (string.Equals(ActiveWeapon, "tidalWave", System.StringComparison.OrdinalIgnoreCase) ||
                         (m_EquippedWeapon is MutinyTidalWave && !m_EquippedWeapon.IsFired))
                {
                    mode = MutinySpecialWeaponCursor.Mode.TidalWave;
                }
                else
                {
                    mode = ResolveBoxPlacementCursorMode(
                        MutinyPhysics.UnityToPixel(GetMouseWorldPosition(mousePosition)));
                }
            }

            m_SpecialWeaponCursor.SetMode(mode, mousePosition, rotationDegrees, animateFan);
        }

        private bool IsPointerOverVoodooDoll(Vector2 mousePosition)
        {
            SpriteRenderer dollRenderer = m_ArmedVoodooDoll != null
                ? m_ArmedVoodooDoll.SpriteRenderer
                : null;
            return dollRenderer != null && dollRenderer.enabled &&
                   dollRenderer.bounds.Contains(GetMouseWorldPosition(mousePosition));
        }

        private float GetMouseWorldPixelX(Vector2 mousePosition)
        {
            return MutinyPhysics.UnityToPixel(GetMouseWorldPosition(mousePosition)).x;
        }

        private void EnsureSpecialWeaponCursor()
        {
            if (m_SpecialWeaponCursor != null)
                return;

            var cursorObject = new GameObject("SpecialWeaponCursor");
            cursorObject.transform.SetParent(transform, false);
            m_SpecialWeaponCursor = cursorObject.AddComponent<MutinySpecialWeaponCursor>();
        }

        internal void UpdateSpecialWeaponCursorForVerification(
            MutinyCharacter selectedCharacter, Vector2 mousePosition, bool leftButtonHeld)
        {
            UpdateSpecialWeaponCursor(selectedCharacter, mousePosition, leftButtonHeld);
        }

        internal void UpdateBoxPlacementCursorForVerification(Vector2 pixelPosition)
        {
            EnsureSpecialWeaponCursor();
            m_SpecialWeaponCursor.SetMode(ResolveBoxPlacementCursorMode(pixelPosition), Vector2.zero);
        }

        internal string SpecialWeaponCursorModeForVerification =>
            m_SpecialWeaponCursor != null ? m_SpecialWeaponCursor.CurrentMode.ToString() : "None";
        internal bool HasVisibleSpecialCursor =>
            m_SpecialWeaponCursor != null &&
            m_SpecialWeaponCursor.CurrentMode != MutinySpecialWeaponCursor.Mode.None;

        internal Texture2D SpecialWeaponCursorTextureForVerification =>
            m_SpecialWeaponCursor != null ? m_SpecialWeaponCursor.CurrentTextureForVerification : null;

        internal float SpecialWeaponCursorRotationForVerification =>
            m_SpecialWeaponCursor != null ? m_SpecialWeaponCursor.RotationDegrees : 0f;

        internal bool SpecialWeaponCursorAnimatingForVerification =>
            m_SpecialWeaponCursor != null && m_SpecialWeaponCursor.IsFanAnimating;

        internal int SpecialWeaponCursorFanFrameForVerification =>
            m_SpecialWeaponCursor != null ? m_SpecialWeaponCursor.CurrentFanFrame : 0;

        internal void AdvanceSpecialWeaponCursorFanFrameForVerification()
        {
            m_SpecialWeaponCursor?.AdvanceFanFrameForVerification();
        }

        private MutinySpecialWeaponCursor.Mode ResolveBoxPlacementCursorMode(Vector2 pixelPosition)
        {
            if (m_ArmedWoodenCrate != null && m_ArmedWoodenCrate.HasPendingPlacement)
            {
                return m_ArmedWoodenCrate.CanPlaceNext(pixelPosition)
                    ? MutinySpecialWeaponCursor.Mode.WoodenCrate
                    : MutinySpecialWeaponCursor.Mode.Cross;
            }
            if (m_ArmedGunpowderBarrel != null && m_ArmedGunpowderBarrel.HasPendingPlacement)
            {
                return m_ArmedGunpowderBarrel.CanPlaceNext(pixelPosition)
                    ? MutinySpecialWeaponCursor.Mode.GunpowderBarrel
                    : MutinySpecialWeaponCursor.Mode.Cross;
            }
            return MutinySpecialWeaponCursor.Mode.None;
        }

        private bool HasPendingBoxPlacement()
        {
            return (m_ArmedWoodenCrate != null && m_ArmedWoodenCrate.HasPendingPlacement) ||
                   (m_ArmedGunpowderBarrel != null && m_ArmedGunpowderBarrel.HasPendingPlacement);
        }

        private void ClearSpecialWeaponCursor()
        {
            if (m_SpecialWeaponCursor != null)
                m_SpecialWeaponCursor.Clear();
        }

        private void OnDisable()
        {
            HandlePointerCancellation();
            ResetMobileGestureOwnership();
            ClearSpecialWeaponCursor();
        }

        private void ResetMobileGestureOwnership()
        {
            m_ActiveTouchId = NoActiveTouchId;
            m_SecondaryCameraTouchId = NoActiveTouchId;
            m_IsMobileCameraDragging = false;
            m_IsPendingMobileTap = false;
            EndPinchGesture();
        }

        /// <summary>
        /// The verification entry drops every owned gesture through the same
        /// production reset the pointer-cancel and level-reset paths use.
        /// </summary>
        internal void ResetMobileGestureOwnershipForVerification() => ResetMobileGestureOwnership();

        // The verification entry invokes exactly the Update phase gate; it avoids
        // only physical Mouse.current acquisition.
        internal bool CanProcessCurrentTurnInputForVerification()
        {
            return CanProcessCurrentTurnInput();
        }

        public static bool SupportsControllerWeapon(string weaponType)
        {
            return string.IsNullOrEmpty(weaponType) ||
                   weaponType.Equals("cherryBomb", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("dynamite", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("boulder", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("mine", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("rumBottle", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("cannonball", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("cannon", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("banana", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("parachuteBomb", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("piecesOfEight", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("voodooDoll", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("woodenCrate", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("gunpowderBarrel", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("seagull", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("tidalWave", System.StringComparison.OrdinalIgnoreCase) ||
                   weaponType.Equals("anchor", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsControllerPointerWeaponReady =>
            InteractionState == MutinyPlayerInteractionState.WeaponReady &&
            (IsVoodooTargetSelection() || IsControllerClickWeapon(ActiveWeapon));

        private static bool IsControllerClickWeapon(string weaponType)
        {
            return string.Equals(weaponType, "woodenCrate", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "gunpowderBarrel", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "anchor", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "seagull", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weaponType, "tidalWave", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool SelectControllerWeapon(string weaponType)
        {
            if (!SupportsControllerWeapon(weaponType) || !SelectWeapon(weaponType)) return false;
            if (IsControllerCannonReady) SyncControllerCannonPointer(MutinyInputHub.Instance);
            return true;
        }

        private void ResetControllerAction()
        {
            m_ControllerAiming = false;
            m_ControllerTriggerMustRelease = true;
            ControllerPower = 0f;
        }

        public void CancelControllerAim()
        {
            if (IsControllerAiming) CancelCurrentAim();
            else ResetControllerAction();
        }

        internal void ProcessControllerFrame(MutinyInputHub.ControllerFrame frame, MutinyInputHub hub)
        {
            if (!CanProcessCurrentTurnInput() ||
                TurnManager.CurrentTeam == null || TurnManager.CurrentTeam.IsAiControlled ||
                MutinyTransitionManager.IsTransitionActive)
                return;
            MutinyGameHUD hud = FindAnyObjectByType<MutinyGameHUD>();
            if (hud != null && (hud.IsQuitPromptVisible ||
                (hud.Speech != null && hud.Speech.HasActiveBubble))) return;
            if (frame.Back)
            {
                TryControllerBack();
                return; // Back always wins over Confirm, even when returning is prohibited.
            }
            MutinyTeam team = TurnManager.CurrentTeam;
            if (frame.Confirm)
            {
                if (MutinySeagull.TryRequestPlayerShot(team)) return;
                if (MutinyBanana.TryRequestPlayerDetonation(team)) return;
            }
            // Fan input is sampled by the flying bomb itself. Keep its selected
            // character and cursor alive while ActionExecuting admits input.
            if (MutinyParachuteBomb.HasPlayerActiveFlight(team)) return;
            if (InteractionState == MutinyPlayerInteractionState.CharacterSelection)
            {
                ProcessControllerCharacterSelection(team, frame, hub);
                return;
            }
            if (InteractionState == MutinyPlayerInteractionState.ActionMenu) return;
            MutinyCharacter character = GetHumanSelectedCharacter();
            if (character == null)
            {
                CancelControllerAim();
                ReturnToCharacterSelection();
                return;
            }
            if ((character.WeaponLocked && !IsAwaitingPiecesOfEight()) ||
                !SupportsControllerWeapon(ActiveWeapon)) return;
            Vector3 pointerWorld = GetMouseWorldPosition(hub.PointerPosition);
            if (!IsControllerAiming && frame.Confirm &&
                TryHandleCancelOverlayPrimaryPointer(character, pointerWorld, true))
                return;
            if (IsVoodooTargetSelection())
            {
                if (frame.Confirm) TrySelectVoodooTarget(team, character, pointerWorld);
                return;
            }
            if (IsControllerClickWeapon(ActiveWeapon))
            {
                if (frame.Confirm) TryActivateClickWeapon(character, pointerWorld);
                return;
            }
            bool triggerHeld = frame.IncreasePower > MutinyInputHub.TriggerDeadzone ||
                               frame.DecreasePower > MutinyInputHub.TriggerDeadzone;
            MutinyCannon cannon = m_ArmedCannon;
            if (!triggerHeld)
                m_ControllerTriggerMustRelease = false;
            if (InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                !m_ControllerTriggerMustRelease && triggerHeld && CanAim(character))
            {
                if (cannon != null && !cannon.TryBeginControllerAim()) return;
                if (!BeginAim(character, GetReadyActionOrigin(character))) return;
                m_ControllerAiming = true;
                m_ControllerPullDirection = cannon != null ? -cannon.FireDirectionPixels : Vector2.zero;
                ControllerPower = 0f;
            }
            if (cannon != null && !IsControllerAiming)
            {
                MoveControllerCannon(frame, hub);
                return;
            }
            if (!IsControllerAiming) return;
            if (frame.Stick.sqrMagnitude > 0.0001f)
                m_ControllerPullDirection = new Vector2(frame.Stick.x, -frame.Stick.y).normalized;
            float increase = Mathf.Clamp01((frame.IncreasePower - MutinyInputHub.TriggerDeadzone) /
                                          (1f - MutinyInputHub.TriggerDeadzone));
            float decrease = Mathf.Clamp01((frame.DecreasePower - MutinyInputHub.TriggerDeadzone) /
                                          (1f - MutinyInputHub.TriggerDeadzone));
            ControllerPower = Mathf.Clamp01(ControllerPower + (increase - decrease) *
                Mathf.Max(0f, ControllerPowerChangePerSecond) * frame.DeltaTime);
            if (cannon != null)
            {
                cannon.UpdateControllerAim(m_ControllerPullDirection, ControllerPower);
                if (frame.Confirm && cannon.CommitControllerShot(TurnManager))
                {
                    character.ConsumeWeapon("cannon");
                    // Same replay contract as the mouse release path: the peer
                    // re-runs body placement (no-op here) and the pin pull pose.
                    DispatchLocal(MutinyNetCommandType.CannonCommit,
                        TurnManager.CurrentTeam.TeamNumber,
                        IndexOfCharacter(TurnManager.CurrentTeam, character),
                        "cannon", cannon.NetworkBodyPixel, cannon.NetworkPinPixel, -1);
                    cannon.SetAimingState(false);
                    HideTrajectory();
                    InteractionState = MutinyPlayerInteractionState.WeaponReady;
                    ResetControllerAction();
                }
                return;
            }
            Vector2 origin = MutinyPhysics.UnityToPixel(m_AimOrigin);
            float force = MutinyWeaponFactory.GetTwangMaxForce(ActiveWeapon) * ControllerPower;
            Vector2 endpoint = origin + m_ControllerPullDirection * (force * 4f);
            Vector3 worldEndpoint = MutinyPhysics.PixelToUnity(endpoint.x, endpoint.y);
            ShowTrajectory(worldEndpoint);
            if (frame.Confirm)
            {
                ResolveAimRelease(character, worldEndpoint);
                ResetControllerAction();
            }
        }

        private void MoveControllerCannon(MutinyInputHub.ControllerFrame frame, MutinyInputHub hub)
        {
            if (!IsControllerCannonReady || GameCamera == null || hub == null) return;
            float scale = Mathf.Min(Screen.width / 550f, Screen.height / 400f);
            Vector2 screenDelta = frame.Stick * (hub.PointerPixelsPerSecond * scale * frame.DeltaTime);
            float z = -GameCamera.transform.position.z;
            Vector3 worldDelta = GameCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, z)) -
                                 GameCamera.ScreenToWorldPoint(new Vector3(0f, 0f, z));
            if (frame.Stick.sqrMagnitude > 0.0001f)
            {
                Vector2 position = new Vector2(m_ArmedCannon.PhysicsBody.State.X, m_ArmedCannon.PhysicsBody.State.Y);
                m_ArmedCannon.MoveForController(position + MutinyPhysics.UnityToPixel(worldDelta));
            }
            SyncControllerCannonPointer(hub);
        }

        private void SyncControllerCannonPointer(MutinyInputHub hub)
        {
            if (GameCamera == null || hub == null || m_ArmedCannon == null) return;
            Vector3 screen = GameCamera.WorldToScreenPoint(m_ArmedCannon.transform.position);
            hub.SetPointerFromGui(new Vector2(screen.x, Screen.height - screen.y));
        }

        public bool TryControllerBack()
        {
            if (IsControllerAiming)
            {
                CancelCurrentAim();
                return true;
            }
            MutinyCharacter character = GetHumanSelectedCharacter();
            if (character == null || character.WeaponLocked) return false;
            if (InteractionState == MutinyPlayerInteractionState.WeaponReady && ShouldShowCancelWeapon(character))
            {
                CancelWeaponSelection();
                return true;
            }
            if (InteractionState == MutinyPlayerInteractionState.ActionMenu && character.CanThrow)
            {
                ReturnToCharacterSelection();
                return true;
            }
            return false;
        }

        internal void PrepareControllerCharacterFocus(MutinyInputHub hub)
        {
            // Frontend pages can coexist with a live or retained battle input.
            // Their UI pointer must not be repositioned by board character focus.
            if (hub == null || !hub.IsControllerActive || hub.CurrentContext != "board" ||
                !CanProcessCurrentTurnInput() ||
                TurnManager.CurrentTeam == null || TurnManager.CurrentTeam.IsAiControlled ||
                MutinyTransitionManager.IsTransitionActive) return;
            MutinyGameHUD hud = FindAnyObjectByType<MutinyGameHUD>();
            if (hud != null && (hud.IsQuitPromptShowRequested || hud.IsQuitPromptVisible ||
                (hud.Speech != null && hud.Speech.HasActiveBubble))) return;
            MutinyTeam team = TurnManager.CurrentTeam;
            if (team != m_ObservedTeam) ResetForCurrentTurn();
            if (InteractionState != MutinyPlayerInteractionState.CharacterSelection) return;
            if (m_ControllerFocusedCharacter != null &&
                (!m_ControllerFocusedCharacter.IsAlive || !team.Characters.Contains(m_ControllerFocusedCharacter)))
                m_ControllerFocusedCharacter = null;
            if (!m_ControllerCharacterFocusInitialized)
            {
                MutinyCameraController camera = GetGameCameraController();
                if (camera == null) return;
                MutinyCharacter target = camera.TurnFocusCharacter;
                if (target != null && target.IsAlive && team.Characters.Contains(target))
                    m_ControllerFocusedCharacter = target;
                m_ControllerCharacterFocusInitialized = true;
            }
            SyncControllerCharacterPointer(hub);
        }

        private void ResetControllerCharacterFocus()
        {
            m_ControllerFocusedCharacter = null;
            m_ControllerCharacterFocusInitialized = false;
        }

        private void SyncControllerCharacterPointer(MutinyInputHub hub)
        {
            if (m_ControllerFocusedCharacter == null || GameCamera == null || hub == null) return;
            Vector3 projected = GameCamera.WorldToScreenPoint(m_ControllerFocusedCharacter.transform.position);
            hub.SetPointerFromGui(new Vector2(projected.x, Screen.height - projected.y));
        }

        private void ProcessControllerCharacterSelection(MutinyTeam team,
            MutinyInputHub.ControllerFrame frame, MutinyInputHub hub)
        {
            if (GameCamera == null) GameCamera = Camera.main;
            if (GameCamera == null || hub == null) return;
            PrepareControllerCharacterFocus(hub);
            if (m_ControllerFocusedCharacter != null && !m_ControllerFocusedCharacter.IsAlive)
                m_ControllerFocusedCharacter = null;
            if (frame.Stick.sqrMagnitude > 0.0001f)
                m_ControllerFocusedCharacter = null;
            if (frame.Navigate != Vector2.zero)
            {
                MutinyCharacter first = null, next = null;
                float best = float.PositiveInfinity;
                foreach (MutinyCharacter candidate in team.Characters)
                {
                    if (candidate == null || !candidate.IsAlive) continue;
                    if (first == null) first = candidate;
                    if (m_ControllerFocusedCharacter == null || candidate == m_ControllerFocusedCharacter) continue;
                    Vector2 delta = candidate.transform.position - m_ControllerFocusedCharacter.transform.position;
                    float forward = Vector2.Dot(delta, frame.Navigate);
                    if (forward <= 0.001f) continue;
                    float lateral = Mathf.Abs(delta.x * frame.Navigate.y - delta.y * frame.Navigate.x);
                    float score = forward + lateral * 4f;
                    if (score < best) { best = score; next = candidate; }
                }
                m_ControllerFocusedCharacter = m_ControllerFocusedCharacter == null ? first : next ?? m_ControllerFocusedCharacter;
                if (m_ControllerFocusedCharacter != null)
                {
                    Vector3 projected = GameCamera.WorldToScreenPoint(m_ControllerFocusedCharacter.transform.position);
                    if (!GameCamera.pixelRect.Contains(projected))
                        GetGameCameraController()?.PanToCharacter(m_ControllerFocusedCharacter);
                }
            }
            SyncControllerCharacterPointer(hub);
            Vector3 pointerWorld = GetMouseWorldPosition(hub.PointerPosition);
            UpdateHoveredCharacter(team, pointerWorld);
            if (frame.Confirm)
                TrySelectCharacterReference(team, m_ControllerFocusedCharacter ??
                    FindCharacterNearPosition(pointerWorld, team, CharacterSelectionRadiusPixels));
        }

        public bool SelectWeapon(string weaponType)
        {
            // The weapon menu belongs to the turn owner; a LAN peer must not be
            // able to equip a weapon for the opponent's team. Replayed remote
            // commands are exempt: they are the opponent's own equip commit.
            if (!IsLocalPlayerTurn() && !m_ReplayingRemoteCommand)
            {
                MutinyDebugLog.Warning("Input",
                    $"weapon selection rejected: this machine does not own the current turn weapon={weaponType}", this);
                return false;
            }

            MutinyCharacter character = GetHumanSelectedCharacter();
            if (character == null || !character.CanShoot || character.WeaponLocked || !character.HasWeapon(weaponType))
            {
                MutinyDebugLog.Warning("Input",
                    $"weapon selection rejected weapon={weaponType} character={(character == null ? "none" : character.name)}", this);
                return false;
            }

            ClearEquippedWeapon();
            ActiveWeapon = weaponType;
            InteractionState = MutinyPlayerInteractionState.WeaponReady;
            m_EquippedWeapon = MutinyWeaponFactory.SpawnWeapon(weaponType, character);
            if (m_EquippedWeapon == null)
            {
                ActiveWeapon = null;
                InteractionState = MutinyPlayerInteractionState.ActionMenu;
                MutinyDebugLog.Warning("Input",
                    $"weapon selection rejected because production instance could not be created weapon={weaponType}", this);
                return false;
            }

            m_ArmedPiecesOfEight = m_EquippedWeapon as MutinyPiecesOfEight;
            m_ArmedVoodooDoll = m_EquippedWeapon as MutinyVoodooDoll;
            m_ArmedWoodenCrate = m_EquippedWeapon as MutinyWoodenCrate;
            m_ArmedGunpowderBarrel = m_EquippedWeapon as MutinyGunpowderBarrel;
            m_ArmedAnchor = m_EquippedWeapon as MutinyAnchor;
            m_ArmedCannon = m_EquippedWeapon as MutinyCannon;
            ResetControllerAction();
            HideTrajectory();
            MutinyDebugLog.Info("Input",
                $"weapon ready character={character.name} weapon={weaponType} instance={m_EquippedWeapon.name} pos=({m_EquippedWeapon.PhysicsBody.State.X:F1},{m_EquippedWeapon.PhysicsBody.State.Y:F1})", this);
            DispatchLocal(MutinyNetCommandType.EquipWeapon,
                TurnManager != null && TurnManager.CurrentTeam != null
                    ? TurnManager.CurrentTeam.TeamNumber : 0,
                IndexOfCharacter(TurnManager?.CurrentTeam, character), weaponType, default, default, -1);
            return true;
        }

        public bool SelectCharacterThrow()
        {
            if (!IsLocalPlayerTurn() && !m_ReplayingRemoteCommand)
            {
                MutinyDebugLog.Warning("Input",
                    "character throw selection rejected: this machine does not own the current turn", this);
                return false;
            }

            MutinyCharacter character = GetHumanSelectedCharacter();
            if (character == null || !character.CanThrow)
            {
                MutinyDebugLog.Warning("Input",
                    $"character throw selection rejected character={(character == null ? "none" : character.name)}", this);
                return false;
            }

            ActiveWeapon = null;
            InteractionState = MutinyPlayerInteractionState.WeaponReady;
            ResetControllerAction();
            HideTrajectory();
            MutinyDebugLog.Info("Input", $"character throw selected character={character.name}", this);
            return true;
        }

        public void CancelWeaponSelection()
        {
            ResetControllerAction();
            HideTrajectory();
            ClearEquippedWeapon();
            ActiveWeapon = null;

            MutinyCharacter character = GetHumanSelectedCharacter();
            InteractionState = character != null
                ? MutinyPlayerInteractionState.ActionMenu
                : MutinyPlayerInteractionState.CharacterSelection;
            MutinyDebugLog.Info("Input", $"weapon selection cancelled nextState={InteractionState}", this);
        }

        // Changing ownership must not leave an unfired weapon or held aim alive.
        // ClearEquippedWeapon preserves committed objects; action qualifications
        // and the selected character belong to the turn manager and are untouched.
        public void ReleaseUncommittedInputForAiTakeover()
        {
            HandlePointerCancellation();
            CancelWeaponSelection();
            ResetMobileGestureOwnership();
            ClearSpecialWeaponCursor();
            ClearHoveredCharacter();
            m_WasTurnActive = false;
        }

        public bool ShouldShowCancelWeapon(MutinyCharacter character)
        {
            MutinyCharacter selected = GetHumanSelectedCharacter();
            bool hasCancelableReadyAction =
                (EquippedWeapon != null && EquippedWeapon.Owner == character) ||
                (string.IsNullOrEmpty(ActiveWeapon) && character != null && character.CanThrow);
            return character != null && character == selected && character.IsAlive &&
                   !character.WeaponLocked && !character.IsSelfThrown &&
                   (InteractionState == MutinyPlayerInteractionState.WeaponReady ||
                    InteractionState == MutinyPlayerInteractionState.Aiming) &&
                   hasCancelableReadyAction;
        }

        public bool TryCancelWeaponFromOverlay(MutinyCharacter character, Vector3 mouseWorld)
        {
            if (!ShouldShowCancelWeapon(character) || character.PhysicsBody == null)
                return false;

            Vector2 mousePixels = MutinyPhysics.UnityToPixel(mouseWorld);
            float centerX = character.PhysicsBody.State.X;
            float centerY = character.PhysicsBody.State.Y + 33f;
            if (Mathf.Abs(mousePixels.x - centerX) > 10f || Mathf.Abs(mousePixels.y - centerY) > 10f)
                return false;

            MutinyDebugLog.Info("Input",
                $"cancel weapon button pressed character={character.name} weapon={ActiveWeapon}", this);
            CancelWeaponSelection();
            return true;
        }

        private bool TryHandleCancelOverlayPrimaryPointer(
            MutinyCharacter character, Vector3 pointerWorld, bool pressedThisFrame)
        {
            // CancelWeaponButton.onPress: entering the cross while held, or
            // releasing on it, is not a new button press.
            return pressedThisFrame && TryCancelWeaponFromOverlay(character, pointerWorld);
        }

        internal bool TryHandleCancelOverlayPrimaryPointerForVerification(
            MutinyCharacter character, Vector2 pixelPosition, bool pressedThisFrame)
        {
            return TryHandleCancelOverlayPrimaryPointer(character,
                MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y), pressedThisFrame);
        }

        private bool TryBeginAimFromPrimaryPointer(MutinyCharacter character, Vector3 pointerWorld)
        {
            if (InteractionState != MutinyPlayerInteractionState.WeaponReady ||
                character == null || !CanAim(character))
                return false;

            Vector3 readyOrigin = GetReadyActionOrigin(character);
            if (PixelDistance(pointerWorld, readyOrigin) > DragSelectionRadiusPixels)
                return false;

            return BeginAim(character, readyOrigin);
        }

        private bool BeginAim(MutinyCharacter character, Vector3 readyOrigin)
        {
            InteractionState = MutinyPlayerInteractionState.Aiming;
            m_AimOrigin = readyOrigin;
            m_EquippedWeapon?.SetAimingState(true);
            if (string.IsNullOrEmpty(ActiveWeapon))
                MutinyMine.NotifyCharacterBeganSelfThrowAim(character);
            MutinyDebugLog.Info("Input",
                $"aim started character={character.name} weapon={ActiveWeapon ?? "character"} origin={MutinyPhysics.UnityToPixel(m_AimOrigin)}", this);
            return true;
        }

        private void ResolveAimRelease(MutinyCharacter character, Vector3 pointerWorld)
        {
            if (InteractionState != MutinyPlayerInteractionState.Aiming)
                return;

            // MouseUp/Ended only resolves the aim; the cross owns onPress, not release.
            if (PixelDistance(pointerWorld, m_AimOrigin) >= MinDragDistancePixels)
            {
                Launch(character, pointerWorld);
                HideTrajectory();
                return;
            }

            CancelCurrentAim();
        }

        private void AdvanceAimPointer(
            MutinyCharacter character, Vector3 pointerWorld, bool isPressed, bool releasedThisFrame)
        {
            if (InteractionState != MutinyPlayerInteractionState.Aiming)
                return;

            // A pinch owns both fingers, so the aim stays where it was until the
            // zoom gesture ends instead of tracking the moving pinch finger.
            if (m_IsPinching)
                return;

            if (isPressed)
                ShowTrajectory(pointerWorld);
            if (releasedThisFrame)
                ResolveAimRelease(character, pointerWorld);
        }

        internal void AdvanceAimPointerForVerification(
            MutinyCharacter character, Vector2 pixelPosition, bool isPressed, bool releasedThisFrame)
        {
            AdvanceAimPointer(character,
                MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y), isPressed, releasedThisFrame);
        }

        private bool TryCancelAimFromSecondaryTouch(Vector3 pointerWorld)
        {
            return InteractionState == MutinyPlayerInteractionState.Aiming &&
                   TryCancelWeaponFromOverlay(GetHumanSelectedCharacter(), pointerWorld);
        }

        internal bool TryBeginAimFromPrimaryPointerForVerification(
            MutinyCharacter character, Vector2 pixelPosition)
        {
            return TryBeginAimFromPrimaryPointer(character,
                MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y));
        }

        internal void ResolveAimReleaseForVerification(MutinyCharacter character, Vector2 pixelPosition)
        {
            ResolveAimRelease(character,
                MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y));
        }

        internal bool TryCancelAimFromSecondaryTouchForVerification(Vector2 pixelPosition)
        {
            return TryCancelAimFromSecondaryTouch(
                MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y));
        }

        public void CancelCurrentAim()
        {
            if (InteractionState != MutinyPlayerInteractionState.Aiming)
                return;

            if (IsControllerAiming && m_ArmedCannon != null)
                m_ArmedCannon.CancelPointer();
            ResetControllerAction();
            HideTrajectory();
            InteractionState = MutinyPlayerInteractionState.WeaponReady;
            m_EquippedWeapon?.SetAimingState(false);
            MutinyCharacter character = GetHumanSelectedCharacter();
            if (character != null)
                m_AimOrigin = GetReadyActionOrigin(character);
            MutinyDebugLog.Info("Input",
                $"current aim cancelled; weapon retained weapon={ActiveWeapon} instance={(m_EquippedWeapon == null ? "none" : m_EquippedWeapon.name)}", this);
        }

        private bool TryCancelAimFromSecondaryPointer()
        {
            if (InteractionState != MutinyPlayerInteractionState.Aiming)
                return false;

            CancelCurrentAim();
            return true;
        }

        internal bool TryCancelAimFromSecondaryPointerForVerification()
        {
            return TryCancelAimFromSecondaryPointer();
        }

        internal bool TryCancelWeaponFromOverlayForVerification(MutinyCharacter character, Vector2 pixelPosition)
        {
            return TryCancelWeaponFromOverlay(character,
                MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y));
        }

        internal void BeginAimForVerification(MutinyCharacter character)
        {
            if (character == GetHumanSelectedCharacter() && CanAim(character))
            {
                m_AimOrigin = GetReadyActionOrigin(character);
                InteractionState = MutinyPlayerInteractionState.Aiming;
                m_EquippedWeapon?.SetAimingState(true);
                if (string.IsNullOrEmpty(ActiveWeapon))
                    MutinyMine.NotifyCharacterBeganSelfThrowAim(character);
            }
        }

        public void ReturnToCharacterSelection()
        {
            if (!IsLocalPlayerTurn() && !m_ReplayingRemoteCommand)
                return;

            MutinyCharacter selected = GetHumanSelectedCharacter();
            if (selected != null && !selected.CanThrow)
                return;

            ResetControllerAction();
            ResetControllerCharacterFocus();
            HideTrajectory();
            ClearEquippedWeapon();
            ActiveWeapon = null;
            if (selected != null)
                selected.IsSelected = false;
            InteractionState = MutinyPlayerInteractionState.CharacterSelection;
        }

        public void EndTurn()
        {
            if (!IsLocalPlayerTurn() && !m_ReplayingRemoteCommand)
            {
                MutinyDebugLog.Warning("Input",
                    "end turn rejected: this machine does not own the current turn", this);
                return;
            }

            MutinyCharacter character = GetHumanSelectedCharacter();
            if (character == null)
            {
                MutinyDebugLog.Warning("Input", "end turn rejected because no human character is selected", this);
                return;
            }

            HideTrajectory();
            ResetControllerAction();
            ClearHoveredCharacter();
            ClearEquippedWeapon();
            ActiveWeapon = null;
            character.CanThrow = false;
            character.CanShoot = false;
            InteractionState = MutinyPlayerInteractionState.WeaponReady;
            MutinyDebugLog.Info("Input", $"end turn character={character.name}", this);
            DispatchLocal(MutinyNetCommandType.PassTurn, TurnManager.CurrentTeam.TeamNumber,
                IndexOfCharacter(TurnManager.CurrentTeam, character), null, default, default, -1);
            TurnManager.NotifyActionStarted();
        }

        private void TrySelectCharacter(MutinyTeam team, Vector3 mouseWorld)
        {
            MutinyCharacter clicked = FindCharacterNearPosition(mouseWorld, team, CharacterSelectionRadiusPixels);
            TrySelectCharacterReference(team, clicked);
        }

        private bool TrySelectCharacterReference(MutinyTeam team, MutinyCharacter clicked)
        {
            // During a LAN match only the owning machine may pick that team's
            // character, which is what opens its weapon menu. Replayed remote
            // commands are exempt: they are the opponent's own selection commit.
            if (team == null ||
                (!m_ReplayingRemoteCommand && !MutinyNetworkManager.IsLocalTeam(team.TeamNumber)))
                return false;
            if (clicked == null || !clicked.IsAlive || !team.Characters.Contains(clicked) ||
                (team.SelectedCharacter != null && !team.SelectedCharacter.CanThrow))
                return false;
            team.SelectCharacter(clicked);
            ClearHoveredCharacter();
            ResetControllerCharacterFocus();
            ActiveWeapon = null;
            InteractionState = MutinyPlayerInteractionState.ActionMenu;
            DispatchLocal(MutinyNetCommandType.SelectCharacter, team.TeamNumber,
                IndexOfCharacter(team, clicked), null, default, default, -1);
            MutinyDebugLog.Info("Input", $"character selected team=T{team.TeamNumber} character={clicked.name}", this);
            return true;
        }

        // Uses the same production selection method while bypassing only the
        // physical Mouse.current read required by Update.
        internal bool TrySelectCharacterForVerification(MutinyTeam team, Vector2 pixelPosition)
        {
            if (team == null)
                return false;

            MutinyCharacter before = team.SelectedCharacter;
            TrySelectCharacter(team, MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y));
            return team.SelectedCharacter != null && team.SelectedCharacter != before &&
                   InteractionState == MutinyPlayerInteractionState.ActionMenu;
        }

        private bool CanAim(MutinyCharacter character)
        {
            if (string.IsNullOrEmpty(ActiveWeapon))
                return character.CanThrow;

            if (ActiveWeapon.Equals("piecesOfEight", System.StringComparison.OrdinalIgnoreCase))
                return m_ArmedPiecesOfEight != null && m_ArmedPiecesOfEight.Owner == character &&
                       m_ArmedPiecesOfEight.CanFireNextCoin;

            if (ActiveWeapon.Equals("voodooDoll", System.StringComparison.OrdinalIgnoreCase))
                return character.CanShoot && character.HasWeapon(ActiveWeapon) &&
                       m_ArmedVoodooDoll != null && m_ArmedVoodooDoll.HasTarget;

            return character.CanShoot && character.HasWeapon(ActiveWeapon);
        }

        private bool TrySelectVoodooTarget(MutinyTeam team, MutinyCharacter owner, Vector3 mouseWorld)
        {
            if (!IsVoodooTargetSelection() || team == null || owner == null)
                return false;

            MutinyTeam enemyTeam = FindOpposingTeam(team);
            MutinyCharacter target = FindCharacterNearPosition(mouseWorld, enemyTeam, CharacterSelectionRadiusPixels);
            if (target == null)
                return false;

            if (!m_ArmedVoodooDoll.BindTarget(target))
                return true;

            ClearHoveredCharacter();
            DispatchLocal(MutinyNetCommandType.VoodooTarget, team.TeamNumber,
                IndexOfCharacter(team, owner), null, default, default,
                IndexOfCharacter(FindOpposingTeam(team), target));
            MutinyAudioManager.Instance?.PlaySFX("voodoo");
            MutinyDebugLog.Info("Input", $"voodoo target selected owner={owner.name} target={target.name}", this);
            return true;
        }

        internal bool TrySelectVoodooTargetForVerification(
            MutinyTeam team, MutinyCharacter owner, Vector3 mouseWorld)
        {
            return TrySelectVoodooTarget(team, owner, mouseWorld);
        }

        private bool TryHandleCannonInput(
            PointerFrameState pointer, MutinyCharacter character, Vector3 mouseWorld)
        {
            if (ActiveWeapon == null || !ActiveWeapon.Equals("cannon", System.StringComparison.OrdinalIgnoreCase) ||
                m_ArmedCannon == null || m_ArmedCannon.Owner != character || m_ArmedCannon.IsFinished)
                return false;

            Vector2 mousePixels = MutinyPhysics.UnityToPixel(mouseWorld);
            if (pointer.PressedThisFrame)
            {
                // The source tests pin first, then the 20px cannon-body circle.
                bool beganDrag = m_ArmedCannon.TryBeginPinDrag(mousePixels) ||
                                 m_ArmedCannon.TryBeginBodyDrag(mousePixels);
                if (beganDrag)
                    m_CannonBodyDragTargetPixels = m_ArmedCannon.NetworkBodyPixel;
                // A desktop Cannon owns the board just like the Flash mouse
                // implementation. On mobile, an actual blank press remains
                // available to the camera-drag gesture.
                return beganDrag || !Application.isMobilePlatform;
            }

            if (pointer.IsPressed)
            {
                if (m_ArmedCannon.IsDraggingPin)
                    m_ArmedCannon.DragPinTo(mousePixels);
                else if (m_ArmedCannon.IsDraggingBody)
                {
                    m_ArmedCannon.DragBodyTo(mousePixels);
                    m_CannonBodyDragTargetPixels = mousePixels;
                }
                return true;
            }

            if (pointer.ReleasedThisFrame)
            {
                // Capture the commit pose before ReleasePointer applies the swept
                // body move; the peer replays body drag and pin pull from it.
                Vector2 pinFinal = m_ArmedCannon.NetworkPinPixel;
                Vector2 bodyFinal = m_CannonBodyDragTargetPixels ?? m_ArmedCannon.NetworkBodyPixel;
                m_CannonBodyDragTargetPixels = null;
                bool committed = m_ArmedCannon.ReleasePointer(TurnManager);
                if (committed)
                {
                    // Character.weaponExpired removes Cannon only after its visual
                    // lifecycle completes.  Unity inventory stores counts, so spend
                    // this selected weapon exactly once when the pin commits.
                    character.ConsumeWeapon("cannon");
                    MutinyDebugLog.Info("Input", $"cannon pin committed character={character.name}", this);
                }
                DispatchLocal(MutinyNetCommandType.CannonCommit,
                    TurnManager != null && TurnManager.CurrentTeam != null ? TurnManager.CurrentTeam.TeamNumber : 0,
                    IndexOfCharacter(TurnManager?.CurrentTeam, character),
                    "cannon", bodyFinal, pinFinal, -1);
                return true;
            }

            return true; // A selected Cannon owns board input while it is placed.
        }

        private bool TryActivateClickWeapon(MutinyCharacter character, Vector3 mouseWorld)
        {
            if (character == null || string.IsNullOrEmpty(ActiveWeapon))
                return false;

            // Placements and click-activated weapons (crate, barrel, anchor,
            // seagull route, tidal wave) replay from the click pixel alone.
            DispatchLocal(MutinyNetCommandType.LaunchWeapon,
                TurnManager != null && TurnManager.CurrentTeam != null ? TurnManager.CurrentTeam.TeamNumber : 0,
                IndexOfCharacter(TurnManager?.CurrentTeam, character),
                ActiveWeapon, default, MutinyPhysics.UnityToPixel(mouseWorld), -1);

            if (ActiveWeapon.Equals("woodenCrate", System.StringComparison.OrdinalIgnoreCase))
            {
                if (m_ArmedWoodenCrate == null || m_ArmedWoodenCrate.Owner != character)
                    return false;

                bool firstPlacement = !m_ArmedWoodenCrate.HasPlacedAny;
                bool placed = m_ArmedWoodenCrate.TryPlaceAt(MutinyPhysics.UnityToPixel(mouseWorld));
                if (!placed)
                    return true; // A placeable weapon consumes this stage click even when rejected.

                if (firstPlacement)
                {
                    character.ConsumeWeapon(ActiveWeapon);
                    MutinyDebugLog.Info("Input", $"wooden crate sequence committed character={character.name}", this);
                }

                if (!m_ArmedWoodenCrate.HasPendingPlacement)
                {
                    ActiveWeapon = null;
                    m_EquippedWeapon = null;
                    m_ArmedWoodenCrate = null;
                    MutinyDebugLog.Info("Input", "wooden crate third placement committed", this);
                }
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                return true;
            }

            if (ActiveWeapon.Equals("gunpowderBarrel", System.StringComparison.OrdinalIgnoreCase))
            {
                if (m_ArmedGunpowderBarrel == null || m_ArmedGunpowderBarrel.Owner != character)
                    return false;
                bool first = !m_ArmedGunpowderBarrel.HasPlacedAny;
                if (!m_ArmedGunpowderBarrel.TryPlaceAt(MutinyPhysics.UnityToPixel(mouseWorld)))
                    return true;
                if (first)
                {
                    // TryPlaceAt commits the production turn exactly once, matching
                    // BoxWeapon.place. Input owns only inventory consumption.
                    character.ConsumeWeapon(ActiveWeapon);
                    MutinyDebugLog.Info("Input", $"gunpowder barrel sequence committed character={character.name}", this);
                }
                if (!m_ArmedGunpowderBarrel.HasPendingPlacement)
                {
                    ActiveWeapon = null;
                    m_EquippedWeapon = null;
                    m_ArmedGunpowderBarrel = null;
                    MutinyDebugLog.Info("Input", "gunpowder barrel second placement committed", this);
                }
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                return true;
            }

            if (ActiveWeapon.Equals("anchor", System.StringComparison.OrdinalIgnoreCase))
            {
                if (m_ArmedAnchor == null || m_ArmedAnchor.Owner != character)
                    return false;

                if (!m_ArmedAnchor.DropAt(MutinyPhysics.UnityToPixel(mouseWorld).x))
                    return true;

                character.ConsumeWeapon(ActiveWeapon);
                TurnManager?.NotifyActionStarted();
                MutinyDebugLog.Info("Input", $"anchor drop committed character={character.name} x={m_ArmedAnchor.PhysicsBody.State.X:F1}", this);
                ActiveWeapon = null;
                m_EquippedWeapon = null;
                m_ArmedAnchor = null;
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                return true;
            }

            if (!character.CanShoot || !character.HasWeapon(ActiveWeapon))
                return false;

            if (ActiveWeapon.Equals("seagull", System.StringComparison.OrdinalIgnoreCase))
            {
                if (m_EquippedWeapon is MutinySeagull seagull && seagull.Owner == character)
                {
                    // Seagull.place(-300, content._ymouse): this first press picks
                    // the route height; later presses are consumed by the live bird.
                    seagull.PlaceAtFlightHeight(MutinyPhysics.UnityToPixel(mouseWorld).y);
                    character.ConsumeWeapon(ActiveWeapon);
                    character.CanShoot = false;
                    character.CanThrow = false;
                    TurnManager.NotifyActionStarted();
                    m_EquippedWeapon = null;
                    ActiveWeapon = null;
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                    MutinyDebugLog.Info("Input", $"seagull path placed y={seagull.FlightY:F1}", this);
                    return true;
                }
            }
            else if (ActiveWeapon.Equals("tidalWave", System.StringComparison.OrdinalIgnoreCase))
            {
                if (m_EquippedWeapon is MutinyTidalWave tidalWave && tidalWave.Owner == character)
                {
                    MutinyLevelRoot root = FindAnyObjectByType<MutinyLevelRoot>();
                    float waterPixelY = root != null
                        ? -root.WaterLevelY * MutinyPhysics.PixelsPerUnit
                        : 448f;
                    tidalWave.StartWave(-550f, waterPixelY);
                    character.ConsumeWeapon(ActiveWeapon);
                    character.CanShoot = false;
                    character.CanThrow = false;
                    TurnManager.NotifyActionStarted();
                    m_EquippedWeapon = null;
                    ActiveWeapon = null;
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                    return true;
                }
            }

            return false;
        }

        // Verification invokes the same click-weapon production route as Update;
        // only physical pointer acquisition is omitted.
        internal bool TryActivateClickWeaponForVerification(MutinyCharacter character, Vector2 pixelPosition)
        {
            return TryActivateClickWeapon(character, MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y));
        }

        internal static bool ShouldHandleWeaponReadyPrimaryInput(
            bool wasPressedThisFrame, bool isPressed)
        {
            // BoxWeapon placement is a click request, not a held-button request.
            // An invalid press remains ignored until the button is released and
            // a new press edge is received at another position.
            return wasPressedThisFrame && isPressed;
        }

        // Verification invokes the same generic release path as Update; only the
        // physical pointer press/hold/release events are omitted.
        internal bool TryLaunchWeaponForVerification(
            MutinyCharacter character, Vector2 startPixels, Vector2 releasePixels)
        {
            if (character == null || character != GetHumanSelectedCharacter() ||
                string.IsNullOrEmpty(ActiveWeapon) || !CanLaunchActiveWeapon(character))
                return false;

            m_AimOrigin = MutinyPhysics.PixelToUnity(startPixels.x, startPixels.y);
            Launch(character, MutinyPhysics.PixelToUnity(releasePixels.x, releasePixels.y));
            return true;
        }

        private void ShowTrajectory(Vector3 mouseWorld)
        {
            if (m_CachedTerrain == null)
                CacheTerrain();

            if (TrajectoryRenderer == null || m_CachedTerrain == null)
                return;

            Vector2 startPixels = MutinyPhysics.UnityToPixel(m_AimOrigin);
            Vector2 dragPixels = MutinyPhysics.UnityToPixel(mouseWorld);
            TrajectoryRenderer.ShowTrajectory(
                startPixels,
                dragPixels,
                m_CachedTerrain,
                m_GridWidth,
                m_GridHeight,
                MutinyWeaponFactory.GetTwangMaxForce(ActiveWeapon),
                MutinyWeaponFactory.GetPredictionWeight(ActiveWeapon),
                ActiveWeapon);
        }

        private void Launch(MutinyCharacter character, Vector3 releaseWorldPosition)
        {
            Vector2 startPixels = MutinyPhysics.UnityToPixel(m_AimOrigin);
            Vector2 dragPixels = MutinyPhysics.UnityToPixel(releaseWorldPosition);

            // Every twang-path commit (self throw, click weapons, voodoo and the
            // piecesOfEight coins) funnels through here; one command type covers all.
            DispatchLocal(MutinyNetCommandType.CommitThrow,
                TurnManager != null && TurnManager.CurrentTeam != null ? TurnManager.CurrentTeam.TeamNumber : 0,
                IndexOfCharacter(TurnManager?.CurrentTeam, character),
                ActiveWeapon, startPixels, dragPixels, -1);

            if (!string.IsNullOrEmpty(ActiveWeapon) &&
                ActiveWeapon.Equals("piecesOfEight", System.StringComparison.OrdinalIgnoreCase) &&
                m_ArmedPiecesOfEight != null && m_ArmedPiecesOfEight.Owner == character &&
                m_ArmedPiecesOfEight.CanFireNextCoin)
            {
                bool firstCoin = m_ArmedPiecesOfEight.TimesFired == 0;
                m_ArmedPiecesOfEight.SetAimingState(false);
                m_ArmedPiecesOfEight.Twang(startPixels, dragPixels);
                if (m_ArmedPiecesOfEight.IsFired && firstCoin)
                    character.ConsumeWeapon(ActiveWeapon);
                MutinyDebugLog.Info("Input",
                    $"piecesOfEight launched character={character.name} coin={m_ArmedPiecesOfEight.TimesFired + 1}/{MutinyPiecesOfEight.TotalCoins} first={firstCoin}", this);
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                return;
            }

            if (!string.IsNullOrEmpty(ActiveWeapon) && character.HasWeapon(ActiveWeapon) && character.CanShoot)
            {
                if (ActiveWeapon.Equals("voodooDoll", System.StringComparison.OrdinalIgnoreCase))
                {
                    MutinyVoodooDoll doll = m_ArmedVoodooDoll;
                    if (doll != null && doll.HasTarget)
                    {
                        doll.SetAimingState(false);
                        doll.Twang(startPixels, dragPixels);
                        if (doll.IsFired)
                        {
                            character.ConsumeWeapon(ActiveWeapon);
                            MutinyDebugLog.Info("Input",
                                $"voodoo launched character={character.name} target={doll.TargetCharacter.name} start={startPixels} drag={dragPixels}", this);
                            m_EquippedWeapon = null;
                            m_ArmedVoodooDoll = null;
                            ActiveWeapon = null;
                        }
                    }
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                    return;
                }

                string launchedWeaponType = ActiveWeapon;
                MutinyWeapon weapon = m_EquippedWeapon;
                if (weapon == null || weapon.Owner != character || weapon.IsFired)
                {
                    MutinyDebugLog.Warning("Input",
                        $"launch rejected because equipped instance is missing weapon={launchedWeaponType}", this);
                    InteractionState = MutinyPlayerInteractionState.WeaponReady;
                    return;
                }

                weapon.SetAimingState(false);
                weapon.Twang(startPixels, dragPixels);
                if (!weapon.IsFired)
                    return;
                character.ConsumeWeapon(launchedWeaponType);
                // Mine.as/Weapon.as have no launch sound. Its first original sound
                // is mine_beep when proximity activates the countdown.
                if (!launchedWeaponType.Equals("mine", System.StringComparison.OrdinalIgnoreCase))
                    MutinyAudioManager.Instance?.PlaySFX("click");
                MutinyDebugLog.Info("Input",
                    $"equipped weapon launched character={character.name} weapon={launchedWeaponType} instance={weapon.name} start={startPixels} drag={dragPixels}", this);
                m_EquippedWeapon = null;
                ActiveWeapon = null;
                character.CanShoot = false;
                character.CanThrow = false;
            }
            else if (string.IsNullOrEmpty(ActiveWeapon) && character.CanThrow)
            {
                TryCommitCharacterThrow(character, startPixels, dragPixels);
            }

            InteractionState = MutinyPlayerInteractionState.WeaponReady;
        }

        internal bool TryCommitCharacterThrow(
            MutinyCharacter character, Vector2 startPixels, Vector2 dragPixels)
        {
            if (character == null || character != GetHumanSelectedCharacter() || !character.CanThrow)
                return false;

            MutinyPhysicsBody body = character.PhysicsBody;
            if (body == null)
            {
                Debug.LogError($"[MutinyPlayerInput] {character.name} has no physics body.", character);
                InteractionState = MutinyPlayerInteractionState.WeaponReady;
                return false;
            }

            body.Twang(startPixels, dragPixels);
            character.MarkSelfThrown("player input");
            character.CanThrow = false;
            MutinyDebugLog.Info("Input",
                $"character throw committed character={character.name} start={startPixels} drag={dragPixels}; original Character.twang has no direct SFX", this);
            TurnManager.NotifyActionStarted();
            InteractionState = MutinyPlayerInteractionState.WeaponReady;
            return true;
        }

        private void ResetForCurrentTurn()
        {
            ResetControllerAction();
            ResetControllerCharacterFocus();
            ResetMobileGestureOwnership();
            HideTrajectory();
            ClearEquippedWeapon();
            m_ObservedTeam = TurnManager != null ? TurnManager.CurrentTeam : null;
            m_WasTurnActive = TurnManager != null && TurnManager.CurrentPhase == TurnPhase.TurnActive;
            ClearHoveredCharacter();
            if (m_ObservedTeam != null && !m_ObservedTeam.IsAiControlled && m_ObservedTeam.SelectedCharacter != null)
                m_ObservedTeam.SelectedCharacter.IsSelected = false;
            ActiveWeapon = null;
            InteractionState = MutinyPlayerInteractionState.CharacterSelection;
            MutinyDebugLog.Info("Input",
                $"reset for team={(m_ObservedTeam == null ? "none" : $"T{m_ObservedTeam.TeamNumber}")} ai={m_ObservedTeam?.IsAiControlled}", this);
        }

        private MutinyCharacter GetHumanSelectedCharacter()
        {
            if (TurnManager == null ||
                (TurnManager.CurrentPhase != TurnPhase.TurnActive &&
                 !IsAwaitingPiecesOfEight() && !HasPendingBoxPlacement()))
                return null;

            MutinyTeam team = TurnManager.CurrentTeam;
            if (team == null || team.IsAiControlled)
                return null;

            MutinyCharacter character = team.SelectedCharacter;
            return character != null && character.IsAlive ? character : null;
        }

        private void HideTrajectory()
        {
            if (TrajectoryRenderer != null)
                TrajectoryRenderer.HideTrajectory();
        }

        private void OnDestroy() => UnhookNetwork();

        private void HookNetworkIfPresent()
        {
            if (m_RemoteHooked || !MutinyNetworkManager.Exists)
                return;
            MutinyNetworkManager.Instance.OnCommandReceived += OnRemoteCommandReceived;
            m_RemoteHooked = true;
        }

        private void UnhookNetwork()
        {
            if (!m_RemoteHooked)
                return;
            if (MutinyNetworkManager.Exists)
                MutinyNetworkManager.Instance.OnCommandReceived -= OnRemoteCommandReceived;
            m_RemoteHooked = false;
        }

        private void OnRemoteCommandReceived(MutinyNetCommand command)
        {
            ExecuteRemoteCommand(in command);
        }

        // Replay a peer's commit through the same production routes the local
        // player used; the flag stops the replay from echoing back over the wire.
        // Public so the scripted Play Mode loopback (a separate dynamic assembly)
        // can drive it like the network layer does.
        public void ExecuteRemoteCommand(in MutinyNetCommand command)
        {
            if (TurnManager == null)
                return;
            MutinyTeam team = ResolveTeam(command.TeamNumber);
            if (team == null)
                return;

            m_ReplayingRemoteCommand = true;
            try
            {
                switch (command.Type)
                {
                    case MutinyNetCommandType.SelectCharacter:
                        TrySelectCharacterReference(team, CharacterAt(team, command.CharacterIndex));
                        break;
                    case MutinyNetCommandType.EquipWeapon:
                        SelectWeapon(command.WeaponType);
                        break;
                    case MutinyNetCommandType.CommitThrow:
                    {
                        MutinyCharacter thrower = CharacterAt(team, command.CharacterIndex);
                        if (string.IsNullOrEmpty(command.WeaponType))
                        {
                            // Bare self-throw: launch routes demand an equipped
                            // weapon, so replay the dedicated throw commit instead.
                            TryCommitCharacterThrow(thrower,
                                new Vector2(command.OriginX, command.OriginY),
                                new Vector2(command.ReleaseX, command.ReleaseY));
                        }
                        else
                        {
                            TryLaunchWeaponForVerification(thrower,
                                new Vector2(command.OriginX, command.OriginY),
                                new Vector2(command.ReleaseX, command.ReleaseY));
                        }
                        break;
                    }
                    case MutinyNetCommandType.LaunchWeapon:
                        TryActivateClickWeaponForVerification(
                            CharacterAt(team, command.CharacterIndex),
                            new Vector2(command.ReleaseX, command.ReleaseY));
                        break;
                    case MutinyNetCommandType.VoodooTarget:
                    {
                        MutinyCharacter voodooOwner = CharacterAt(team, command.CharacterIndex);
                        MutinyCharacter voodooTarget = CharacterAt(FindOpposingTeam(team), command.TargetIndex);
                        if (voodooOwner != null && voodooTarget != null && voodooTarget.PhysicsBody != null)
                        {
                            Vector2 targetPixels = new Vector2(
                                voodooTarget.PhysicsBody.State.X, voodooTarget.PhysicsBody.State.Y);
                            TrySelectVoodooTarget(team, voodooOwner,
                                MutinyPhysics.PixelToUnity(targetPixels.x, targetPixels.y));
                        }
                        break;
                    }
                    case MutinyNetCommandType.CannonCommit:
                    {
                        MutinyCharacter gunner = CharacterAt(team, command.CharacterIndex);
                        if (m_ArmedCannon == null || m_ArmedCannon.IsFinished ||
                            m_ArmedCannon.Owner != gunner)
                            break;
                        if (m_ArmedCannon.TryBeginBodyDrag(m_ArmedCannon.NetworkBodyPixel))
                        {
                            m_ArmedCannon.DragBodyTo(new Vector2(command.OriginX, command.OriginY));
                            m_ArmedCannon.ReleasePointer(TurnManager);
                        }
                        if (m_ArmedCannon.TryBeginControllerAim())
                        {
                            m_ArmedCannon.DragPinTo(new Vector2(command.ReleaseX, command.ReleaseY));
                            if (m_ArmedCannon.ReleasePointer(TurnManager))
                                gunner?.ConsumeWeapon("cannon");
                        }
                        break;
                    }
                    case MutinyNetCommandType.PassTurn:
                        EndTurn();
                        break;
                    case MutinyNetCommandType.SeagullShot:
                        MutinySeagull.TryRequestPlayerShot(team);
                        break;
                    case MutinyNetCommandType.BananaDetonate:
                        MutinyBanana.TryRequestPlayerDetonation(team);
                        break;
                }
            }
            finally
            {
                m_ReplayingRemoteCommand = false;
            }
        }

        private void DispatchLocal(MutinyNetCommandType type, int teamNumber, int characterIndex,
            string weaponType, Vector2 originPixels, Vector2 releasePixels, int targetIndex)
        {
            if (m_ReplayingRemoteCommand)
                return;
            MutinyNetworkManager manager = MutinyNetworkManager.Instance;
            if (manager == null)
                return;
            manager.SendCommand(new MutinyNetCommand
            {
                Type = type,
                TeamNumber = teamNumber,
                CharacterIndex = characterIndex,
                WeaponType = weaponType ?? string.Empty,
                OriginX = originPixels.x,
                OriginY = originPixels.y,
                ReleaseX = releasePixels.x,
                ReleaseY = releasePixels.y,
                TargetIndex = targetIndex
            });
        }

        private static int IndexOfCharacter(MutinyTeam team, MutinyCharacter character)
        {
            if (team == null || team.Characters == null || character == null)
                return -1;
            return team.Characters.IndexOf(character);
        }

        private static MutinyCharacter CharacterAt(MutinyTeam team, int index)
        {
            return team?.Characters != null && index >= 0 && index < team.Characters.Count
                ? team.Characters[index]
                : null;
        }

        private MutinyTeam ResolveTeam(int teamNumber)
        {
            if (TurnManager == null)
                return null;
            if (TurnManager.Team1 != null && TurnManager.Team1.TeamNumber == teamNumber)
                return TurnManager.Team1;
            if (TurnManager.Team2 != null && TurnManager.Team2.TeamNumber == teamNumber)
                return TurnManager.Team2;
            return null;
        }

        private Vector3 GetReadyActionOrigin(MutinyCharacter character)
        {
            MutinyWeapon equipped = EquippedWeapon;
            if (!string.IsNullOrEmpty(ActiveWeapon) && equipped != null && equipped.PhysicsBody != null)
            {
                return MutinyPhysics.PixelToUnity(
                    equipped.PhysicsBody.State.X, equipped.PhysicsBody.State.Y);
            }
            return character != null ? character.transform.position : Vector3.zero;
        }

        private void UpdateHoveredCharacter(MutinyTeam team, Vector3 mouseWorld)
        {
            MutinyCharacter hovered = null;
            if (InteractionState == MutinyPlayerInteractionState.CharacterSelection)
                hovered = FindCharacterNearPosition(mouseWorld, team, CharacterSelectionRadiusPixels);
            else if (IsVoodooTargetSelection())
                hovered = FindCharacterNearPosition(mouseWorld, FindOpposingTeam(team), CharacterSelectionRadiusPixels);

            if (hovered != null && hovered.IsAlive && (m_SpecialWeaponCursor == null || m_SpecialWeaponCursor.CurrentMode == MutinySpecialWeaponCursor.Mode.None))
                MutinyCursorManager.NotifyHoverInteractable();

            if (hovered == m_HoveredCharacter)
                return;

            ClearHoveredCharacter();
            m_HoveredCharacter = hovered;
            if (m_HoveredCharacter != null)
                m_HoveredCharacter.IsHovered = true;
        }

        private void ClearHoveredCharacter()
        {
            if (m_HoveredCharacter != null)
                m_HoveredCharacter.IsHovered = false;
            m_HoveredCharacter = null;
        }

        private MutinyCharacter FindCharacterNearPosition(Vector3 position, MutinyTeam team, float radiusPixels)
        {
            if (team == null || team.Characters == null)
                return null;

            MutinyCharacter closest = null;
            float closestDistance = radiusPixels;

            for (int i = 0; i < team.Characters.Count; i++)
            {
                MutinyCharacter character = team.Characters[i];
                if (character == null || !character.IsAlive)
                    continue;

                float distance = PixelDistance(position, character.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = character;
                }
            }

            return closest;
        }

        private bool IsAwaitingPiecesOfEight()
        {
            return m_ArmedPiecesOfEight != null && m_ArmedPiecesOfEight.IsAwaitingNextCoin &&
                   TurnManager != null && TurnManager.CurrentTeam != null &&
                   TurnManager.CurrentTeam.SelectedCharacter == m_ArmedPiecesOfEight.Owner;
        }

        private bool IsPiecesOfEightSequenceLocked()
        {
            return m_ArmedPiecesOfEight != null && m_ArmedPiecesOfEight.IsAwaitingNextCoin;
        }

        private bool CanLaunchActiveWeapon(MutinyCharacter character)
        {
            if (ActiveWeapon != null && ActiveWeapon.Equals("piecesOfEight", System.StringComparison.OrdinalIgnoreCase))
            {
                return m_ArmedPiecesOfEight != null && m_ArmedPiecesOfEight.Owner == character &&
                       m_ArmedPiecesOfEight.CanFireNextCoin;
            }
            return character.CanShoot && character.HasWeapon(ActiveWeapon);
        }

        private bool IsVoodooTargetSelection()
        {
            return InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                   ActiveWeapon != null &&
                   ActiveWeapon.Equals("voodooDoll", System.StringComparison.OrdinalIgnoreCase) &&
                   m_ArmedVoodooDoll != null && !m_ArmedVoodooDoll.IsFired &&
                   !m_ArmedVoodooDoll.HasTarget;
        }

        private void ClearEquippedWeapon()
        {
            MutinyWeapon equipped = m_EquippedWeapon;
            if (equipped == null)
                equipped = m_ArmedPiecesOfEight != null ? m_ArmedPiecesOfEight :
                    m_ArmedVoodooDoll != null ? m_ArmedVoodooDoll :
                    m_ArmedWoodenCrate != null ? m_ArmedWoodenCrate :
                    m_ArmedGunpowderBarrel != null ? m_ArmedGunpowderBarrel :
                    m_ArmedAnchor != null ? m_ArmedAnchor : m_ArmedCannon;

            bool committed = equipped != null && equipped.IsFired;
            if (equipped is MutinyWoodenCrate crate)
                committed |= crate.HasPlacedAny;
            if (equipped is MutinyGunpowderBarrel barrel)
                committed |= barrel.HasPlacedAny;
            if (equipped is MutinyPiecesOfEight pieces)
                committed |= pieces.TimesFired > 0;
            if (equipped is MutinyCannon cannon)
                committed |= cannon.IsFirePending;

            if (equipped != null && !committed)
            {
                MutinyDebugLog.Info("Input",
                    $"unequipped pending weapon={equipped.WeaponType} instance={equipped.name}", this);
                if (Application.isPlaying)
                    Destroy(equipped.gameObject);
                else
                    DestroyImmediate(equipped.gameObject);
            }

            m_EquippedWeapon = null;
            m_ArmedPiecesOfEight = null;
            m_ArmedVoodooDoll = null;
            m_ArmedWoodenCrate = null;
            m_ArmedGunpowderBarrel = null;
            m_ArmedAnchor = null;
            m_ArmedCannon = null;
        }

        private MutinyTeam FindOpposingTeam(MutinyTeam team)
        {
            if (team == null)
                return null;

            if (TurnManager != null)
            {
                if (TurnManager.Team1 == team && TurnManager.Team2 != null)
                    return TurnManager.Team2;
                if (TurnManager.Team2 == team && TurnManager.Team1 != null)
                    return TurnManager.Team1;
            }

            MutinyTeam[] teams = FindObjectsByType<MutinyTeam>(FindObjectsSortMode.None);
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i] != null && teams[i] != team && teams[i].TeamNumber != team.TeamNumber)
                    return teams[i];
            }
            return null;
        }

        private static float PixelDistance(Vector3 first, Vector3 second)
        {
            return Vector2.Distance(MutinyPhysics.UnityToPixel(first), MutinyPhysics.UnityToPixel(second));
        }

        private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
        {
            if (GameCamera == null)
                GameCamera = Camera.main;

            if (GameCamera == null)
                return Vector3.zero;

            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, 0f);
            screenPoint.z = -GameCamera.transform.position.z;
            Vector3 world = GameCamera.ScreenToWorldPoint(screenPoint);
            world.z = 0f;
            return world;
        }
    }
}
