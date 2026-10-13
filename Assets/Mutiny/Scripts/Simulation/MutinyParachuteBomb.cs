using System.Collections.Generic;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using Mutiny.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mutiny.Simulation
{
    /// <summary>Flash ParachuteBomb: a 30-force projectile that opens a chute mid-flight.</summary>
    [DisallowMultipleComponent]
    public sealed class MutinyParachuteBomb : MutinyWeapon
    {
        public const float OriginalExtentPixels = 11f;
        public const float OriginalTwangMaxForce = 30f;
        public const float HorizontalDragPerTick = 0.95f;
        public const float FanImpulsePerTick = 0.2f;
        public const int FanSoundIntervalTicks = 12;
        public const float ChuteOpenVelocityThreshold = -10f;
        public const float CeilingClampPixels = -300f;
        public const float ExplosionSize = 160f;
        public const float ExplosionDamage = 50f;

        // DefineSprite 939: closed frame 1, opening label at frame 11, open label
        // at frame 26, and frame 30 calls gotoAndPlay("open"). Indices are zero-based.
        private const int OriginalFrameCount = 30;
        private const int ClosedFuseFrameCount = 4;
        private const int OpeningFrameIndex = 10;
        private const int OpenLoopFrameIndex = 25;
        private const int OpenLoopActionFrameIndex = 29;
        private const int NoMobileFanTouchId = int.MinValue;

        private readonly List<Sprite> m_Frames = new List<Sprite>(OriginalFrameCount);
        private int m_FramesFromFire;
        private int m_CurrentFrame;
        private bool m_OverWater = true;
        private bool? m_VerificationFanHeld;
        private float m_VerificationMousePixelX;
        private bool m_ChuteOpenedThisTick;
        private float m_ClosedFuseTickAccumulator;
        private int m_MobileFanTouchId = NoMobileFanTouchId;
        private bool m_MobileFanHeld;
        private bool m_MobileFanPulsePending;
        private Vector2 m_MobileFanScreenPosition;
        private bool m_ControllerFanMustRelease;

        public bool ChuteOpen { get; private set; }
        public bool IsFanActive { get; private set; }
        public bool ControllerFanCanStart => !m_ControllerFanMustRelease;
        public int FramesFromFire => m_FramesFromFire;
        public int CurrentAnimationFrame => m_CurrentFrame + 1;

        protected override void Awake()
        {
            WeaponType = "parachuteBomb";
            Extent = OriginalExtentPixels;
            TwangMaxForce = OriginalTwangMaxForce;
            base.Awake();
            LoadFrames();
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            PhysicsBody.State.LeftExtent = OriginalExtentPixels;
            PhysicsBody.State.RightExtent = OriginalExtentPixels;
            PhysicsBody.State.TopExtent = OriginalExtentPixels;
            PhysicsBody.State.BottomExtent = OriginalExtentPixels;
            PhysicsBody.State.HitsBoxes = true;

            // Weapon.advance uses splashCheck only. It does not apply the project's
            // Character-style underwater drag, and crossing water is not contact().
            PhysicsBody.ApplyWaterPhysics = false;
            PhysicsBody.OnBeforeSimulationStep -= AdvanceOriginalMotionTick;
            PhysicsBody.OnBeforeSimulationStep += AdvanceOriginalMotionTick;
            PhysicsBody.OnSimulationStep -= AdvanceOriginalPostMotionTick;
            PhysicsBody.OnSimulationStep += AdvanceOriginalPostMotionTick;
            PhysicsBody.OnSimulationStep -= EmitOriginalSmokeTrail;
            PhysicsBody.OnSimulationStep += EmitOriginalSmokeTrail;

            ChuteOpen = false;
            IsFanActive = false;
            m_FramesFromFire = 0;
            m_CurrentFrame = 0;
            m_OverWater = true;
            m_VerificationFanHeld = null;
            m_ChuteOpenedThisTick = false;
            m_ClosedFuseTickAccumulator = 0f;
            m_MobileFanTouchId = NoMobileFanTouchId;
            m_MobileFanHeld = false;
            m_MobileFanPulsePending = false;
            m_MobileFanScreenPosition = Vector2.zero;
            m_ControllerFanMustRelease = true;
            ApplyFrame();
        }

        public override void Fire(Vector2 velocityPx)
        {
            base.Fire(velocityPx);
            if (IsFired)
            {
                // A commits the throw. Require a fresh press before A can fan it.
                m_ControllerFanMustRelease = true;
                PhysicsBody.IsActive = true;
                MutinyDebugLog.Info("ParachuteBomb",
                    $"fired velocity=({PhysicsBody.State.VelocityX:F2},{PhysicsBody.State.VelocityY:F2})", this);
            }
        }

        // Runs one actual MutinyPhysicsBody production tick. The optional fan input
        // replaces physical mouse polling only for the verification caller.
        public void AdvanceOriginalTickForVerification(bool fanHeld, float mousePixelX)
        {
            if (PhysicsBody == null || IsFinished)
                return;

            m_VerificationFanHeld = fanHeld;
            m_VerificationMousePixelX = mousePixelX;
            PhysicsBody.AdvanceSimulationTick();
            SyncTransformFromState();
            m_VerificationFanHeld = null;
        }

        protected override void Update()
        {
            MutinyInputHub controller = MutinyInputHub.Instance;
            if (IsFired && controller != null && controller.IsControllerActive &&
                !controller.IsConfirmHeld)
                m_ControllerFanMustRelease = false;
            CaptureMobileFanInput();
            AdvanceClosedFusePresentation(Time.deltaTime);

            // ParachuteBomb inherits Weapon.advance, whose only lifecycle exits are
            // map-bottom and exact rest. No arbitrary lifetime or water deadline
            // may remove it or turn a splash into an explosion.
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            if ((PhysicsBody.State.VelocityY > 0f &&
                 PhysicsBody.State.Y > ResolveLevelHeightPixels()) ||
                (PhysicsBody.State.VelocityX == 0f && Mathf.Abs(PhysicsBody.State.VelocityY) < 0.2f))
            {
                FinishWithoutExplosion("weapon-advance");
            }
        }

        private void AdvanceOriginalMotionTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            // ParachuteBomb.advanceMotion performs these operations before
            // Solid.advanceMotion adds weight and moves/collides.
            PhysicsBodyState state = PhysicsBody.State;
            ApplyOriginalAirMotion(ref state);
            PhysicsBody.State = state;

            if (!ChuteOpen && PhysicsBody.State.VelocityY > ChuteOpenVelocityThreshold)
            {
                ChuteOpen = true;
                m_CurrentFrame = OpeningFrameIndex;
                m_ChuteOpenedThisTick = true;
                ApplyFrame();
                MutinyDebugLog.Info("ParachuteBomb",
                    $"chute opened tick={m_FramesFromFire + 1} vy={PhysicsBody.State.VelocityY:F2}", this);
            }

            m_FramesFromFire++;
            ApplyFanInput();
        }

        private void AdvanceOriginalPostMotionTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            // Weapon.advance calls splashCheck after motion. It only makes a splash
            // and sound when the over-water boolean changes; it never calls contact.
            AdvanceSplashCheck();

            if (m_ChuteOpenedThisTick)
                m_ChuteOpenedThisTick = false;
            else
                AdvanceChuteAnimation();

            PhysicsBodyState state = PhysicsBody.State;
            if (ApplyOriginalCeilingClamp(ref state))
            {
                PhysicsBody.State = state;
                SyncTransformFromState();
                MutinyDebugLog.Info("ParachuteBomb", "clamped at original y=-300 ceiling", this);
            }
        }

        internal static void ApplyOriginalAirMotion(ref PhysicsBodyState state)
        {
            if (state.VelocityY > 1f)
                state.VelocityY = Mathf.Max(1f, state.VelocityY - 2f);
            state.VelocityX *= HorizontalDragPerTick;
        }

        internal static bool ApplyOriginalCeilingClamp(ref PhysicsBodyState state)
        {
            if (state.Y >= CeilingClampPixels)
                return false;
            state.Y = CeilingClampPixels;
            if (state.VelocityY < 0f)
                state.VelocityY = 0f;
            return true;
        }

        private void EmitOriginalSmokeTrail()
        {
            // ParachuteBomb.advance emits in both ready and fired states until the
            // opening threshold changes chuteOpen. Keep this separate from the
            // fired-only motion callback so the stopped outer frame still smokes.
            if (!IsFinished && !ChuteOpen && PhysicsBody != null)
                MutinyRumBottleSmokeTrail.Spawn(PhysicsBody.CurrentStepStartPositionPixels);
        }

        private void ApplyFanInput()
        {
            if (!IsHumanOwned() || !TryGetFanInput(out bool held, out float mousePixelX))
            {
                IsFanActive = false;
                return;
            }

            IsFanActive = held;
            if (!held)
                return;

            // Original direction is intentionally opposite the cursor side:
            // mouse left pushes right, mouse right pushes left.
            if (mousePixelX - PhysicsBody.State.X < 0f)
                PhysicsBody.State.VelocityX += FanImpulsePerTick;
            else
                PhysicsBody.State.VelocityX -= FanImpulsePerTick;

            if (m_FramesFromFire % FanSoundIntervalTicks == 0)
                MutinyAudioManager.Instance?.PlaySFX("fan");
        }

        private bool TryGetFanInput(out bool held, out float mousePixelX)
        {
            // Fan steering is continuous per-tick input; online lockstep only
            // syncs discrete commands, so online matches drop with no fan.
            if (Mutiny.Network.MutinyNetworkManager.IsOnlineMatch)
            {
                held = false;
                mousePixelX = 0f;
                return false;
            }

            if (m_VerificationFanHeld.HasValue)
            {
                held = m_VerificationFanHeld.Value;
                mousePixelX = m_VerificationMousePixelX;
                return true;
            }

            MutinyInputHub controller = MutinyInputHub.Instance;
            if (controller != null && controller.IsControllerActive)
            {
                Camera controllerCamera = Camera.main;
                if (!controller.IsConfirmHeld)
                    m_ControllerFanMustRelease = false;
                held = controller.CurrentContext == "board" && controller.IsConfirmHeld &&
                    !m_ControllerFanMustRelease;
                mousePixelX = controllerCamera != null
                    ? MutinyPhysics.UnityToPixel(controllerCamera.ScreenToWorldPoint(new Vector3(
                        controller.PointerPosition.x, controller.PointerPosition.y,
                        -controllerCamera.transform.position.z))).x
                    : 0f;
                return controllerCamera != null;
            }

            if (Application.isMobilePlatform)
            {
                Camera mobileCamera = Camera.main;
                if (mobileCamera == null)
                {
                    held = false;
                    mousePixelX = 0f;
                    return false;
                }

                held = ConsumeMobileFanActive();
                Vector3 mobileWorld = mobileCamera.ScreenToWorldPoint(
                    new Vector3(
                        m_MobileFanScreenPosition.x,
                        m_MobileFanScreenPosition.y,
                        -mobileCamera.transform.position.z));
                mousePixelX = MutinyPhysics.UnityToPixel(mobileWorld).x;
                return true;
            }

            Mouse mouse = Mouse.current;
            Camera camera = Camera.main;
            if (mouse == null || camera == null)
            {
                held = false;
                mousePixelX = 0f;
                return false;
            }

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = camera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -camera.transform.position.z));
            held = mouse.leftButton.isPressed;
            mousePixelX = MutinyPhysics.UnityToPixel(world).x;
            return true;
        }

        private void CaptureMobileFanInput()
        {
            if (!Application.isMobilePlatform || !IsFired || IsFinished || !IsHumanOwned())
                return;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                m_MobileFanTouchId = NoMobileFanTouchId;
                m_MobileFanHeld = false;
                return;
            }

            if (m_MobileFanTouchId != NoMobileFanTouchId)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    var touch = touchscreen.touches[i];
                    if (touch.touchId.ReadValue() != m_MobileFanTouchId)
                        continue;

                    m_MobileFanScreenPosition = touch.position.ReadValue();
                    if (touch.press.wasPressedThisFrame)
                        m_MobileFanPulsePending = true;
                    m_MobileFanHeld = touch.press.isPressed;
                    UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                        phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                        touch.press.wasReleasedThisFrame)
                    {
                        m_MobileFanTouchId = NoMobileFanTouchId;
                        m_MobileFanHeld = false;
                    }
                    return;
                }

                m_MobileFanTouchId = NoMobileFanTouchId;
                m_MobileFanHeld = false;
            }

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var candidate = touchscreen.touches[i];
                if (!candidate.press.wasPressedThisFrame && !candidate.press.isPressed)
                    continue;

                m_MobileFanTouchId = candidate.touchId.ReadValue();
                m_MobileFanScreenPosition = candidate.position.ReadValue();
                m_MobileFanHeld = candidate.press.isPressed;
                if (candidate.press.wasPressedThisFrame)
                    m_MobileFanPulsePending = true;
                return;
            }
        }

        internal static bool ResolveMobileFanActive(
            bool touchHeld, bool pulsePending)
        {
            return touchHeld || pulsePending;
        }

        private bool ConsumeMobileFanActive()
        {
            bool active = ResolveMobileFanActive(
                m_MobileFanHeld, m_MobileFanPulsePending);
            m_MobileFanPulsePending = false;
            return active;
        }

        internal void SetMobileFanStateForVerification(
            bool touchHeld, bool pulsePending)
        {
            m_MobileFanHeld = touchHeld;
            m_MobileFanPulsePending = pulsePending;
        }

        internal bool ConsumeMobileFanActiveForVerification()
        {
            return ConsumeMobileFanActive();
        }

        private void AdvanceSplashCheck()
        {
            if (float.IsInfinity(PhysicsBody.WaterPixelY))
                return;

            bool overWater = PhysicsBody.State.Y < PhysicsBody.WaterPixelY;
            if (m_OverWater != overWater)
            {
                MutinyWaterSurface.SpawnSplash(PhysicsBody.State.X, PhysicsBody.WaterPixelY);
                MutinyAudioManager.Instance?.PlaySFX("splash");
                MutinyDebugLog.Info("ParachuteBomb",
                    $"water crossing y={PhysicsBody.State.Y:F1} waterY={PhysicsBody.WaterPixelY:F1}", this);
            }
            m_OverWater = overWater;
        }

        private void AdvanceChuteAnimation()
        {
            if (m_Frames.Count == 0 || SpriteRenderer == null)
                return;

            m_CurrentFrame++;
            // Frame 30 only contains gotoAndPlay("open"). Flash executes that
            // action before rendering, so presenting the exported transparent
            // frame causes the periodic one-tick disappearance seen in Unity.
            if (m_CurrentFrame >= OpenLoopActionFrameIndex)
                m_CurrentFrame = OpenLoopFrameIndex;
            ApplyFrame();
        }

        private void AdvanceClosedFusePresentation(float deltaTime)
        {
            if (IsFinished || ChuteOpen || m_Frames.Count == 0)
                return;

            m_ClosedFuseTickAccumulator += deltaTime;
            while (m_ClosedFuseTickAccumulator >= MutinyPhysics.TimeStep)
            {
                m_ClosedFuseTickAccumulator -= MutinyPhysics.TimeStep;
                AdvanceClosedFuseFrame();
            }
        }

        private void AdvanceClosedFuseFrame()
        {
            // The outer parachuteBomb clip stops on frame 1, while its nested
            // DefineSprite 930 fuse keeps looping through four frames. Composite
            // exports 1..4 reproduce that nested animation without advancing the
            // stopped outer timeline into the opening sequence.
            m_CurrentFrame = (m_CurrentFrame + 1) % ClosedFuseFrameCount;
            ApplyFrame();
        }

        internal void AdvanceClosedFuseFrameForVerification()
        {
            if (!ChuteOpen && !IsFinished)
                AdvanceClosedFuseFrame();
        }

        public static MutinyParachuteBomb FindPlayerActiveFlight(MutinyTeam inputTeam)
        {
            if (inputTeam == null || inputTeam.IsAiControlled)
                return null;

            MutinyParachuteBomb[] bombs = Object.FindObjectsByType<MutinyParachuteBomb>(FindObjectsSortMode.None);
            for (int i = 0; i < bombs.Length; i++)
            {
                MutinyParachuteBomb bomb = bombs[i];
                if (bomb != null && bomb.IsFired && !bomb.IsFinished && bomb.Owner != null &&
                    inputTeam.Characters.Contains(bomb.Owner))
                    return bomb;
            }
            return null;
        }

        public static bool HasPlayerActiveFlight(MutinyTeam inputTeam)
        {
            return FindPlayerActiveFlight(inputTeam) != null;
        }

        public static readonly Vector2 OriginalPivot = new Vector2(18f / 36f, 14f / 60f); // Symbol 939: origin (18, 46) of 36x60

        private void LoadFrames()
        {
            m_Frames.Clear();
            for (int frame = 1; frame <= OriginalFrameCount; frame++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Weapons/ParachuteBomb/{frame}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        OriginalPivot, MutinyPhysics.PixelsPerUnit);
                    m_Frames.Add(sprite);
                }
                else
                {
                    Sprite sprite = Resources.Load<Sprite>($"Art/Weapons/ParachuteBomb/{frame}");
                    if (sprite != null)
                        m_Frames.Add(sprite);
                }
            }
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (SpriteRenderer != null && m_CurrentFrame >= 0 && m_CurrentFrame < m_Frames.Count)
                SpriteRenderer.sprite = m_Frames[m_CurrentFrame];
        }

        protected override void OnContact(CollisionSide side)
        {
            base.OnContact(side);
            if (IsFired && !IsFinished)
                Explode(side);
        }

        private void Explode(CollisionSide side)
        {
            Vector2 position = new Vector2(PhysicsBody.State.X, PhysicsBody.State.Y);
            Finish();
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;

            // ParachuteBomb.contact creates the Explosion and plays pop in the same
            // tick, before Explosion.hit reaches its visual damage frame.
            MutinyExplosion.Spawn(position, ExplosionSize, ExplosionDamage, Owner, playPopOnHit: false);
            MutinyAudioManager.Instance?.PlaySFX("pop");
            Destroy(gameObject, 0.1f);
            MutinyDebugLog.Info("ParachuteBomb",
                $"exploded side={side} pos=({position.x:F1},{position.y:F1})", this);
        }

        private void FinishWithoutExplosion(string reason)
        {
            Finish();
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
            Destroy(gameObject, 0.1f);
            MutinyDebugLog.Info("ParachuteBomb", $"finished reason={reason}", this);
        }

        private float ResolveLevelHeightPixels()
        {
            if (PhysicsBody.TryGetTerrain(out _, out _, out int height))
                return height * MutinyPhysics.PixelsPerUnit;

            MutinyLevelRoot root = FindAnyObjectByType<MutinyLevelRoot>();
            return root != null && root.Height > 0 ? root.Height * MutinyPhysics.PixelsPerUnit : 1200f;
        }

        private void SyncTransformFromState()
        {
            transform.position = MutinyPhysics.PixelToUnity(PhysicsBody.State.X, PhysicsBody.State.Y);
        }

        private bool IsHumanOwned()
        {
            MutinyTeam[] teams = FindObjectsByType<MutinyTeam>(FindObjectsSortMode.None);
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i] != null && teams[i].Characters.Contains(Owner))
                    return !teams[i].IsAiControlled;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (PhysicsBody == null)
                return;
            PhysicsBody.OnBeforeSimulationStep -= AdvanceOriginalMotionTick;
            PhysicsBody.OnSimulationStep -= AdvanceOriginalPostMotionTick;
            PhysicsBody.OnSimulationStep -= EmitOriginalSmokeTrail;
        }
    }
}
