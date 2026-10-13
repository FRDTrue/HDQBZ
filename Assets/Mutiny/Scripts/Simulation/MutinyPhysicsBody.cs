using System;
using System.Collections.Generic;
using Mutiny.Levels;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MutinyPhysicsBody : MonoBehaviour
    {
        [Header("Physics State")]
        public PhysicsBodyState State;

        [Header("Settings")]
        public bool IsActive = true;
        public bool SyncTransform = true;
        public bool ApplyWaterPhysics = true;
        public bool ApplyWaterMotion = true;
        public float WaterPixelY = float.PositiveInfinity;
        public bool IsInWater { get; private set; }

        public event Action OnFloorLanded;
        public event Action OnCeilingHit;
        public event Action OnWallHit;
        public event Action OnRest;
        public event Action OnEnterWater;
        public event Action OnBeforeSimulationStep;
        public event Action OnAfterMotionStep;
        public event Action OnWaterMotionAdjusted;
        public event Action OnSimulationStep;

        private float m_TimeAccumulator;
        private Vector2 m_PreviousTickPositionPixels;
        private Vector2 m_CurrentTickPositionPixels;
        private bool m_HasPresentationTick;
        private bool m_PresentationApplied;
        private static readonly HashSet<MutinyPhysicsBody> s_Bodies = new HashSet<MutinyPhysicsBody>();
        private static int s_LastRestoreFrame = -1;
        private string[,] m_CachedTerrain;
        private int m_GridWidth;
        private int m_GridHeight;

        // A few original clips deliberately teleport before every simulation
        // step. They may supply a display pose that does not interpolate that
        // discontinuity; all other bodies keep the normal tick interpolation.
        internal Func<Vector3?> PresentationPositionOverride { get; set; }

        // Parent-driven projectiles must not run a second autonomous simulation,
        // but may still render the completed tick using their parent's clock.
        internal MutinyPhysicsBody PresentationClockSource { get; set; }
        private bool CanInterpolatePresentation => IsActive || PresentationClockSource != null;

        public bool IsAtRest => State.IsAtRest;
        public long SimulationTickCount { get; private set; }
        // During OnSimulationStep this is the first pose the renderer presents
        // for the just-completed tick. Effects emitted there must use the same
        // origin instead of the ahead-of-render authoritative State position.
        public Vector2 CurrentStepStartPositionPixels { get; private set; }
        public float SimulationInterpolationAlpha =>
            PresentationClockSource != null
                ? PresentationClockSource.SimulationInterpolationAlpha
                : Mathf.Clamp01(m_TimeAccumulator / MutinyPhysics.TimeStep);

        // The simulation stays at 25 Hz. Both the visible object and its camera
        // target sample the same completed tick, one tick behind authority.
        public Vector3 PresentationPosition => SamplePresentationPosition(SimulationInterpolationAlpha);

        public Vector3 SamplePresentationPosition(float alpha)
        {
            if (!SyncTransform)
                return transform.position;

            Vector3? overriddenPosition = PresentationPositionOverride?.Invoke();
            if (overriddenPosition.HasValue)
                return overriddenPosition.Value;

            Vector2 current = new Vector2(State.X, State.Y);
            if (!CanInterpolatePresentation || !m_HasPresentationTick ||
                (current - m_CurrentTickPositionPixels).sqrMagnitude > 0.0001f)
                return MutinyPhysics.PixelToUnity(current.x, current.y);

            Vector2 sampled = Vector2.Lerp(m_PreviousTickPositionPixels,
                m_CurrentTickPositionPixels, Mathf.Clamp01(alpha));
            return MutinyPhysics.PixelToUnity(sampled.x, sampled.y);
        }

        private void OnEnable()
        {
            s_Bodies.Add(this);
        }

        private void OnDisable()
        {
            RestoreAuthoritativePose();
            s_Bodies.Remove(this);
        }

        private void Awake()
        {
            EnsureInitializedState();
        }

        private void Start()
        {
            EnsureInitializedState();
            // A child spawned in another body's Update may already have stepped
            // and rendered before its first Start. Never seed State from that
            // one-tick-delayed display pose or rewind its authoritative motion.
            if (!m_HasPresentationTick)
            {
                Vector2 px = MutinyPhysics.UnityToPixel(transform.position);
                State.X = px.x;
                State.Y = px.y;
            }
            else
                RestoreAuthoritativePose();

            CacheLevelTerrain();
        }

        private void EnsureInitializedState()
        {
            // Levels can be baked into a scene in edit mode. Older scenes were saved
            // before PhysicsBodyState was serializable, leaving every numeric field at
            // zero in Play Mode (including gravity). Extents are never all zero for a
            // valid Solid, so they provide a safe legacy-state check.
            if (State.LeftExtent != 0f || State.RightExtent != 0f ||
                State.TopExtent != 0f || State.BottomExtent != 0f)
                return;

            Vector2 px = MutinyPhysics.UnityToPixel(transform.position);
            State = PhysicsBodyState.CreateDefault(px.x, px.y);
        }

        public void CacheLevelTerrain()
        {
            var levelRoot = FindAnyObjectByType<MutinyLevelRoot>();
            if (levelRoot != null)
            {
                WaterPixelY = -levelRoot.WaterLevelY * MutinyPhysics.PixelsPerUnit;

                // Find level controller if present to get terrain array
                var controller = FindAnyObjectByType<MutinyLevelController>();
                if (controller != null && controller.LevelXml != null)
                {
                    var levelData = MutinyLevelXmlParser.Parse(controller.LevelXml.text);
                    m_CachedTerrain = levelData.Terrain;
                    m_GridWidth = levelData.Width;
                    m_GridHeight = levelData.Height;
                }
            }
        }

        public void SetTerrain(string[,] terrain, int width, int height)
        {
            m_CachedTerrain = terrain;
            m_GridWidth = width;
            m_GridHeight = height;
        }

        public bool TryGetTerrain(out string[,] terrain, out int width, out int height)
        {
            if (m_CachedTerrain == null)
                CacheLevelTerrain();

            terrain = m_CachedTerrain;
            width = m_GridWidth;
            height = m_GridHeight;
            return terrain != null && width > 0 && height > 0;
        }

        private void Update()
        {
            // Restore every body before the first 25 Hz step of this render
            // frame. A step may query another body's Transform before that
            // body's own Update has run.
            if (s_LastRestoreFrame != Time.frameCount)
            {
                s_LastRestoreFrame = Time.frameCount;
                foreach (MutinyPhysicsBody body in s_Bodies)
                {
                    if (body != null)
                        body.RestoreAuthoritativePose();
                }
            }

            AdvanceSimulationFrame(Time.deltaTime);
        }

        internal void AdvanceSimulationFrameForVerification(float deltaTime)
        {
            RestoreAuthoritativePose();
            AdvanceSimulationFrame(deltaTime);
        }

        private void AdvanceSimulationFrame(float deltaTime)
        {
            if (!IsActive)
                return;

            if (m_CachedTerrain == null)
            {
                CacheLevelTerrain();
            }

            m_TimeAccumulator += deltaTime;
            int maxSubSteps = 5; // Prevent spiral of death
            int steps = 0;

            while (m_TimeAccumulator >= MutinyPhysics.TimeStep && steps < maxSubSteps)
            {
                m_TimeAccumulator -= MutinyPhysics.TimeStep;
                steps++;
                AdvanceSimulationTick();
            }

            if (SyncTransform)
            {
                transform.position = MutinyPhysics.PixelToUnity(State.X, State.Y);
            }
        }

        private void LateUpdate()
        {
            ApplyPresentationPose(SimulationInterpolationAlpha);
        }

        private void ApplyPresentationPose(float alpha)
        {
            if (!CanInterpolatePresentation || !SyncTransform)
                return;

            Vector3 presentation = SamplePresentationPosition(alpha);
            Vector3 authoritative = MutinyPhysics.PixelToUnity(State.X, State.Y);
            // Resting bodies need no temporary Transform swap at all.
            if ((presentation - authoritative).sqrMagnitude <= 0.00000001f)
            {
                RestoreAuthoritativePose();
                return;
            }

            transform.position = presentation;
            m_PresentationApplied = true;
        }

        private void RestoreAuthoritativePose()
        {
            if (!m_PresentationApplied)
                return;

            transform.position = MutinyPhysics.PixelToUnity(State.X, State.Y);
            m_PresentationApplied = false;
        }

        internal void ApplyPresentationPoseForVerification() => ApplyPresentationPose(SimulationInterpolationAlpha);
        internal void RestoreAuthoritativePoseForVerification() => RestoreAuthoritativePose();

        public StepResult AdvanceSimulationTick()
        {
            SimulationTickCount++;
            // Flash weapon advanceMotion overrides rotate before Solid.advanceMotion.
            OnBeforeSimulationStep?.Invoke();
            // A pre-step callback can place a weapon at its owner. Capture after
            // that transition so rendering never blends from the old location.
            Vector2 tickStartPosition = new Vector2(State.X, State.Y);
            CurrentStepStartPositionPixels = tickStartPosition;

            List<PhysicsBoxObstacle> boxes = null;
            if (State.HitsBoxes)
            {
                // Controller.boxes is one shared level list in Flash. Querying the
                // same registry here makes every hitsBoxes Solid (characters and
                // thrown weapons included) collide with both BoxWeapon subtypes.
                boxes = MutinyBoxRegistry.GetObstacles(this);
            }
            StepResult result = MutinyPhysics.Step(ref State, m_CachedTerrain, m_GridWidth, m_GridHeight, boxes, this);

            // Solid.contact runs inside advanceMotion in Flash. Character floor
            // correction therefore precedes Character.advance's airborne rotation.
            if (result.HitFloor)
                OnFloorLanded?.Invoke();
            if (result.HitCeiling)
                OnCeilingHit?.Invoke();
            if (result.HitLeftWall || result.HitRightWall)
                OnWallHit?.Invoke();
            if (result.IsAtRest)
                OnRest?.Invoke();

            OnAfterMotionStep?.Invoke();
            EvaluateWaterState();
            OnSimulationStep?.Invoke();
            m_PreviousTickPositionPixels = tickStartPosition;
            m_CurrentTickPositionPixels = new Vector2(State.X, State.Y);
            m_HasPresentationTick = true;
            return result;
        }

        public void EvaluateWaterState()
        {
            if (!ApplyWaterPhysics)
                return;

            if (float.IsInfinity(WaterPixelY))
            {
                var levelRoot = FindAnyObjectByType<MutinyLevelRoot>();
                if (levelRoot != null && levelRoot.WaterLevelY != 0f)
                {
                    WaterPixelY = -levelRoot.WaterLevelY * MutinyPhysics.PixelsPerUnit;
                }
            }

            if (State.Y <= WaterPixelY)
                return;

            if (!IsInWater)
            {
                IsInWater = true;
                OnEnterWater?.Invoke();
            }

            // Only Character.advance applies the original underwater damping.
            // Solid.splashCheck on weapons detects a crossing but does not alter
            // their velocity or turn their flight into a slow, stuck projectile.
            if (!ApplyWaterMotion)
                return;

            // Flash Character.advance: motion continues below the water after the
            // one-shot crossing event, with drag and a capped downward velocity.
            State.VelocityX *= 0.8f;
            State.VelocityY *= 0.8f;
            if (State.VelocityY > 1.5f)
            {
                State.VelocityY -= 4f;
                if (State.VelocityY < 1.5f)
                    State.VelocityY = 1.5f;
                OnWaterMotionAdjusted?.Invoke();
            }
        }

        public void AddVelocity(float vx, float vy)
        {
            State.VelocityX += vx;
            State.VelocityY += vy;
        }

        public void SetVelocity(float vx, float vy)
        {
            State.VelocityX = vx;
            State.VelocityY = vy;
        }

        public void Twang(Vector2 startPx, Vector2 dragPx, float maxForce = MutinyPhysics.DefaultTwangMaxForce)
        {
            Vector2 launchVel = MutinyPhysics.CalculateTwangVelocity(startPx, dragPx, maxForce);
            State.VelocityX = launchVel.x;
            State.VelocityY = launchVel.y;
        }
    }
}
