using Mutiny.Diagnostics;
using UnityEngine;

namespace Mutiny.Simulation
{
    /// <summary>Flash Cannon: collision-aware placement, pull-back pin, then a separate cannonball.</summary>
    [DisallowMultipleComponent]
    public sealed class MutinyCannon : MutinyWeapon
    {
        // DefineSprite 1900 contains a 200 px visible circle in a 365 px canvas.
        // TileSystem scales it to 130%, so the visible radius is 130 px. Keep the
        // placement guide and its actual constraint centred 100 px above the
        // character, so the source circle's bottom rests on the owner position.
        public const float RangeCircleSourceRadius = 100f;
        public const float RangeCircleScale = 1.3f;
        public const float RangeCircleVisibleRadius = RangeCircleSourceRadius * RangeCircleScale;
        public const float PlacementOffsetY = -100f;
        public const float InitialEquipmentOffsetY = -10f;
        public const float PlacementRadius = 120f;
        public const float BodyClickRadius = 20f;
        public const float PinClickRadius = 8f;
        public const float PinRestX = -21f;
        public const float PinPlacementY = -0.25f;
        public const float PinMinX = -40f;
        public const float PinFireThresholdX = -30f;
        public const float PinReturnPerTick = 15f;
        public const float FireStrength = 30f;
        public const string FireSoundName = "cannon_explosion";

        // Symbol 850 is the 53x38 composite; its body child is shape 849 with
        // the same world registration represented inside a 46x38 raster.
        public static readonly Vector2 OriginalPivot = new Vector2(26f / 53f, 19f / 38f);
        public static readonly Vector2 OriginalBodyPivot = new Vector2(19f / 46f, 19f / 38f);
        private static readonly Vector2 PinPivot = new Vector2(8f / 35f, 8f / 16f);
        private static readonly Vector2 RangeCirclePivot = new Vector2(0.5f, 0.5f);

        private float m_TickAccumulator;
        private float m_Visibility = 2f;
        private float m_RangeCircleAlpha;
        private float m_FireStrength;
        private int m_AiFireTicksRemaining;
        private Vector2 m_AiFireVelocity;
        private Vector2 m_BodyDragStart;
        private Vector2 m_BodyDragPointer;
        private bool m_BodyDragPointerDirty;
        private bool m_DraggingBody;
        private bool m_DraggingPin;
        private MutinyCannonball m_Cannonball;
        private SpriteRenderer m_PinRenderer;
        private SpriteRenderer m_RangeCircleRenderer;
        private Vector2 m_PreviousPlacement;
        private Vector2 m_CurrentPlacement;
        private float m_PlacementPresentationElapsed;
        private bool m_HasPlacementPresentation;

        public override bool AdvancesMotionWhileReady => false;
        public float PinX { get; private set; } = PinRestX;
        public float RangeCircleAlpha => m_RangeCircleAlpha;
        public int RotationDegrees { get; private set; }
        public bool IsDraggingBody => m_DraggingBody;
        public bool IsDraggingPin => m_DraggingPin;
        public MutinyCannonball Cannonball => m_Cannonball;
        public bool IsAiFirePending => m_AiFireTicksRemaining > 0 && !IsFired && !IsFinished;
        public bool IsFirePending => !IsFired && !IsFinished &&
            (m_AiFireTicksRemaining > 0 || m_FireStrength > 4f);
        public Vector2 FireDirectionPixels => DirectionForRotation();
        public SpriteRenderer PinRenderer => m_PinRenderer;
        public SpriteRenderer RangeCircleRenderer => m_RangeCircleRenderer;
        public Transform CameraFocusTarget => m_Cannonball != null ? m_Cannonball.transform : null;
        public Vector2 PlacementCenterPixels => GetPlacementCenterPixels();

        protected override void Awake()
        {
            WeaponType = "cannon";
            Extent = 10f;
            base.Awake();
            BuildOriginalVisualLayers();
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            PhysicsBody.State.Weight = 0f;
            PhysicsBody.State.HitsBoxes = true;
            PhysicsBody.IsActive = false;
            // This weapon owns its 25 Hz clock. Expose its display pose without
            // enabling a second autonomous physics simulation.
            PhysicsBody.PresentationPositionOverride = SamplePlacementPresentationPosition;
            PlaceAtEquipmentPosition();
            PinX = PinRestX;
            RotationDegrees = 0;
            m_Visibility = 2f;
            m_RangeCircleAlpha = 0f;
            m_AiFireTicksRemaining = 0;
            ApplyVisualRotation();
            UpdatePinVisual();
            UpdateRangeCircleVisual();
            SetBodyAlpha(1f);
            SetRangeCircleAlpha(0f);
        }

        public override void PrepareForEquip()
        {
            base.PrepareForEquip();
            // Cannon.advance owns the body's Solid step. Letting MutinyPhysicsBody.Update
            // run as well would move the placeable body twice per original tick.
            PhysicsBody.IsActive = false;
            PlaceAtEquipmentPosition();
            UpdateRangeCircleVisual();
        }

        public void PlaceAtEquipmentPosition()
        {
            if (Owner == null)
                return;
            Vector2 ownerPosition = Owner.PhysicsBody != null
                ? new Vector2(Owner.PhysicsBody.State.X, Owner.PhysicsBody.State.Y)
                : MutinyPhysics.UnityToPixel(Owner.transform.position);
            SetPosition(ownerPosition.x, ownerPosition.y + InitialEquipmentOffsetY);
        }

        public bool TryBeginBodyDrag(Vector2 mousePixels)
        {
            if (IsFired || m_DraggingPin || Vector2.Distance(mousePixels, Position) >= BodyClickRadius)
                return false;

            m_DraggingBody = true;
            m_BodyDragStart = Position;
            m_BodyDragPointer = mousePixels;
            m_BodyDragPointerDirty = false;
            PhysicsBody.SetVelocity(0f, 0f);
            return true;
        }

        public bool TryBeginPinDrag(Vector2 mousePixels)
        {
            if (IsFired || m_DraggingBody || Vector2.Distance(mousePixels, PinWorldPosition()) >= PinClickRadius)
                return false;
            m_DraggingPin = true;
            return true;
        }

        public void DragBodyTo(Vector2 mousePixels)
        {
            if (!m_DraggingBody || (mousePixels - m_BodyDragPointer).sqrMagnitude <= Mathf.Epsilon)
                return;

            m_BodyDragPointer = mousePixels;
            m_BodyDragPointerDirty = true;
        }

        public void DragPinTo(Vector2 mousePixels)
        {
            if (!m_DraggingPin)
                return;
            Vector2 delta = mousePixels - Position;
            float degrees = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 180f;
            RotationDegrees = Mathf.RoundToInt(Mathf.Repeat(degrees, 360f));
            // Cannon.as rotates the body away from the cursor, then reads the pin's
            // local X. Its value is therefore the negative pointer distance.
            PinX = Mathf.Clamp(-delta.magnitude, PinMinX, PinRestX);
            ApplyVisualRotation();
            UpdatePinVisual();
        }

        // Controller placement owns the cannon, without a held mouse drag. Use
        // the same range clamp and swept Solid motion as body dragging and AI.
        public void MoveForController(Vector2 targetPixels)
        {
            if (IsFired || IsFinished || IsFirePending || m_DraggingBody || m_DraggingPin)
                return;
            MoveBodyTowardWithCollision(ClampToPlacementCircle(targetPixels), 1f);
        }

        public bool TryBeginControllerAim() => !IsFirePending && !IsFinished &&
            TryBeginPinDrag(PinWorldPosition());

        public void UpdateControllerAim(Vector2 pullDirectionPixels, float visualPower)
        {
            if (!m_DraggingPin || IsFired || IsFinished || pullDirectionPixels.sqrMagnitude < 0.0001f)
                return;
            float pullDistance = Mathf.Lerp(-PinRestX, -PinMinX, Mathf.Clamp01(visualPower));
            DragPinTo(Position + pullDirectionPixels.normalized * pullDistance);
        }

        public bool CommitControllerShot(MutinyTurnManager turnManager)
        {
            if (!m_DraggingPin || IsFired || IsFinished || IsFirePending)
                return false;
            m_DraggingPin = false;
            // User-authorized mapping: A fires even when the visual pin is at
            // rest. The mouse release threshold remains in ReleasePointer.
            LoadShot(turnManager);
            return true;
        }

        public bool ReleasePointer(MutinyTurnManager turnManager)
        {
            if (m_DraggingBody)
            {
                // Preserve a short click-drag-release even when press and release
                // happen between two 25 Hz weapon ticks. The final move still uses
                // the production collision sweep and original half-distance step.
                if (m_BodyDragPointerDirty)
                {
                    Vector2 displayStart = MutinyPhysics.UnityToPixel(
                        SamplePlacementPresentationPosition().Value);
                    AdvanceBodyDragOriginalTick();
                    RecordPlacementPresentation(displayStart, 0f);
                }
                StopBodyDrag();
                return false;
            }

            if (!m_DraggingPin)
                return false;

            m_DraggingPin = false;
            if (PinX >= PinFireThresholdX)
            {
                m_FireStrength = 0f;
                return false;
            }

            LoadShot(turnManager);
            return true;
        }

        private void LoadShot(MutinyTurnManager turnManager)
        {
            m_FireStrength = FireStrength;
            if (Owner != null)
            {
                Owner.CanShoot = false;
                Owner.CanThrow = false;
            }
            turnManager?.NotifyActionStarted();
            MutinyDebugLog.Info("Cannon", $"pin released x={PinX:F1}; loading {FireStrength:F0}", this);
        }

        public void CancelPointer()
        {
            if (m_DraggingBody)
            {
                SetPosition(m_BodyDragStart.x, m_BodyDragStart.y);
                PhysicsBody.SetVelocity(0f, 0f);
                m_BodyDragPointerDirty = false;
                m_DraggingBody = false;
            }

            if (m_DraggingPin)
            {
                m_DraggingPin = false;
                PinX = PinRestX;
                m_FireStrength = 0f;
                UpdatePinVisual();
            }
        }

        /// <summary>
        /// Cannon.aiPerform advances the body once toward the sampled placement so
        /// terrain and placeable boxes can correct it, then waits 25 original ticks.
        /// </summary>
        public void BeginAiFire(Vector2 placement, int rotationDegrees, Vector2 velocity)
        {
            if (IsFired || IsFinished)
                return;

            MoveBodyTowardWithCollision(placement, 1f);
            RotationDegrees = Mathf.RoundToInt(Mathf.Repeat(rotationDegrees, 360f));
            ApplyVisualRotation();
            UpdatePinVisual();
            m_AiFireVelocity = velocity;
            m_AiFireTicksRemaining = 25;
            MutinyDebugLog.Info("Cannon",
                $"AI armed requested=({placement.x:F1},{placement.y:F1}) resolved=({Position.x:F1},{Position.y:F1}) angle={RotationDegrees} delayTicks={m_AiFireTicksRemaining} velocity={velocity}", this);
        }

        protected override void Update()
        {
            AdvanceSimulationFrame(Time.deltaTime);
        }

        internal void AdvanceSimulationFrameForVerification(float deltaTime) =>
            AdvanceSimulationFrame(deltaTime);

        private void AdvanceSimulationFrame(float deltaTime)
        {
            if (IsFinished)
                return;
            // Complete the display segment even after pointer release. Logical
            // positions and collisions are never advanced by this render clock.
            m_PlacementPresentationElapsed += deltaTime;
            m_TickAccumulator += deltaTime;
            while (m_TickAccumulator >= MutinyPhysics.TimeStep)
            {
                m_TickAccumulator -= MutinyPhysics.TimeStep;
                AdvanceOriginalTick();
            }
        }

        public void AdvanceOriginalTickForVerification() => AdvanceOriginalTick();

        private void AdvanceOriginalTick()
        {
            // Cannon.advance starts with Solid.advanceMotion. Only a player body
            // drag has non-zero body velocity here in the Unity implementation.
            if (!IsFired && m_DraggingBody)
            {
                Vector2 previous = Position;
                AdvanceBodyDragOriginalTick();
                if ((Position - previous).sqrMagnitude > 0.00000001f)
                    RecordPlacementPresentation(previous, m_TickAccumulator);
            }

            AdvanceRangeCircleOriginalTick();

            if (m_AiFireTicksRemaining > 0)
            {
                m_AiFireTicksRemaining--;
                if (m_AiFireTicksRemaining < 1)
                    FireCannon(m_AiFireVelocity);
                return;
            }

            if (!IsFired)
            {
                if (!m_DraggingPin)
                {
                    PinX = Mathf.Min(PinRestX, PinX + PinReturnPerTick);
                    UpdatePinVisual();
                    if (Mathf.Approximately(PinX, PinRestX) && m_FireStrength > 4f)
                    {
                        float radians = RotationDegrees * Mathf.Deg2Rad;
                        FireCannon(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * m_FireStrength);
                    }
                }
                return;
            }

            m_Visibility = Mathf.Max(0f, m_Visibility - 0.1f);
            if (m_Visibility < 1f)
                SetBodyAlpha(m_Visibility);

            // Cannon.update changes only trackX/trackY. The placed cannon remains
            // stationary while the independent cannonball is followed by camera.
            if (m_Cannonball != null && m_Cannonball.IsFinished && Mathf.Approximately(m_Visibility, 0f))
            {
                if (SpriteRenderer != null)
                    SpriteRenderer.enabled = false;
                if (m_PinRenderer != null)
                    m_PinRenderer.enabled = false;
                Finish();
                Destroy(m_Cannonball.gameObject, 0.2f);
                Destroy(gameObject, 0.2f);
            }
        }

        private void AdvanceBodyDragOriginalTick()
        {
            m_BodyDragPointerDirty = false;
            if (Mathf.Abs(m_BodyDragPointer.x - m_BodyDragStart.x) < 5f &&
                Mathf.Abs(m_BodyDragPointer.y - m_BodyDragStart.y) < 5f)
            {
                SetPosition(m_BodyDragStart.x, m_BodyDragStart.y);
                PhysicsBody.SetVelocity(0f, 0f);
                return;
            }

            // A fast mouse sample can jump outside the guide between ticks. Keep
            // ownership of the drag and clamp that target to the circle instead of
            // treating the sampled overshoot as a synthetic mouse-up. The move
            // still uses the production Solid collision sweep.
            Vector2 constrainedTarget = ClampToPlacementCircle(m_BodyDragPointer);
            Vector2 previous = Position;
            MoveBodyTowardWithCollision(constrainedTarget, 0.5f);
            Vector2 actualDelta = Position - previous;
            PhysicsBody.SetVelocity(actualDelta.x, actualDelta.y);
        }

        private void MoveBodyTowardWithCollision(Vector2 target, float velocityScale)
        {
            Vector2 current = Position;
            Vector2 velocity = (target - current) * velocityScale;
            PhysicsBody.SetVelocity(velocity.x, velocity.y);
            PhysicsBody.AdvanceSimulationTick();
            SetPosition(PhysicsBody.State.X, PhysicsBody.State.Y);
            PhysicsBody.SetVelocity(0f, 0f);
        }

        private Vector2 ClampToPlacementCircle(Vector2 target)
        {
            if (Owner == null || Owner.PhysicsBody == null)
                return target;

            Vector2 center = GetPlacementCenterPixels();
            Vector2 offset = target - center;
            if (offset.sqrMagnitude <= PlacementRadius * PlacementRadius)
                return target;

            offset = offset.normalized * PlacementRadius;
            return center + offset;
        }

        private void StopBodyDrag()
        {
            m_DraggingBody = false;
            m_BodyDragPointerDirty = false;
            PhysicsBody.SetVelocity(0f, 0f);
        }

        private void FireCannon(Vector2 velocity)
        {
            if (IsFired)
                return;
            // Never launch from the one-tick-delayed display coordinate.
            ResetPlacementPresentation();
            IsFired = true;
            m_FireStrength = 0f;
            StopBodyDrag();
            GameObject ballObject = new GameObject("Cannonball");
            m_Cannonball = ballObject.AddComponent<MutinyCannonball>();
            m_Cannonball.Initialize(Owner);
            m_Cannonball.SetLaunchPosition(Position);
            m_Cannonball.Fire(velocity);
            // The AS2 logical name is "cannon explosion"; the imported Unity
            // resource is normalized to cannon_explosion.wav.
            Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX(FireSoundName);
            MutinyDebugLog.Info("Cannon", $"fired from=({Position.x:F1},{Position.y:F1}) angle={RotationDegrees} velocity={velocity}", this);
        }

        private void AdvanceRangeCircleOriginalTick()
        {
            float target = IsFired ? 0f : 1f;
            m_RangeCircleAlpha = Mathf.MoveTowards(m_RangeCircleAlpha, target, 0.2f);
            SetRangeCircleAlpha(m_RangeCircleAlpha);
            UpdateRangeCircleVisual();
        }

        private void BuildOriginalVisualLayers()
        {
            if (SpriteRenderer != null)
                SpriteRenderer.sprite = LoadRuntimeSprite("Art/Weapons/Cannon/Body", OriginalBodyPivot);

            GameObject pinObject = new GameObject("Pin");
            pinObject.transform.SetParent(transform, false);
            m_PinRenderer = pinObject.AddComponent<SpriteRenderer>();
            m_PinRenderer.sortingOrder = WeaponSortingOrder - 1;
            m_PinRenderer.sprite = LoadRuntimeSprite("Art/Weapons/Cannon/Pin", PinPivot);

            GameObject rangeObject = new GameObject("RangeCircle");
            // Controller.rangeCircle is a stage-level sibling in Flash. Keeping it
            // outside the cannon hierarchy prevents body drag/rotation from ever
            // changing the owner-bottom-anchored placement guide.
            m_RangeCircleRenderer = rangeObject.AddComponent<SpriteRenderer>();
            m_RangeCircleRenderer.sortingOrder = WeaponSortingOrder - 2;
            m_RangeCircleRenderer.sprite = LoadRuntimeSprite("Art/Weapons/Cannon/RangeCircle", RangeCirclePivot);
            m_RangeCircleRenderer.transform.localScale = Vector3.one * RangeCircleScale;
        }

        private static Sprite LoadRuntimeSprite(string resourcePath, Vector2 pivot)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return Resources.Load<Sprite>(resourcePath);

            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                pivot, MutinyPhysics.PixelsPerUnit);
        }

        private void UpdatePinVisual()
        {
            if (m_PinRenderer == null)
                return;
            m_PinRenderer.transform.localPosition = new Vector3(
                PinX / MutinyPhysics.PixelsPerUnit,
                -PinPlacementY / MutinyPhysics.PixelsPerUnit,
                0f);
        }

        private void UpdateRangeCircleVisual()
        {
            if (m_RangeCircleRenderer == null || Owner == null || Owner.PhysicsBody == null)
                return;

            Vector2 center = GetPlacementCenterPixels();
            Transform rangeTransform = m_RangeCircleRenderer.transform;
            rangeTransform.position = MutinyPhysics.PixelToUnity(center.x, center.y);
            rangeTransform.rotation = Quaternion.identity;
            rangeTransform.localScale = Vector3.one * RangeCircleScale;
        }

        private void SetBodyAlpha(float alpha)
        {
            Color color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            if (SpriteRenderer != null)
                SpriteRenderer.color = color;
            if (m_PinRenderer != null)
                m_PinRenderer.color = color;
        }

        private void SetRangeCircleAlpha(float alpha)
        {
            if (m_RangeCircleRenderer == null)
                return;
            Color color = m_RangeCircleRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            m_RangeCircleRenderer.color = color;
            m_RangeCircleRenderer.enabled = color.a > 0f;
        }

        private Vector2 Position => new Vector2(PhysicsBody.State.X, PhysicsBody.State.Y);

        // Network replay captures the exact board/pin pose at commit time so the
        // peer can reproduce body sweeps and fire strength without pointer events.
        internal Vector2 NetworkBodyPixel => Position;
        internal Vector2 NetworkPinPixel => PinWorldPosition();

        private Vector2 GetPlacementCenterPixels()
        {
            if (Owner == null)
                return Position;

            Vector2 ownerPosition = Owner.PhysicsBody != null
                ? new Vector2(Owner.PhysicsBody.State.X, Owner.PhysicsBody.State.Y)
                : MutinyPhysics.UnityToPixel(Owner.transform.position);
            return ownerPosition + Vector2.up * PlacementOffsetY;
        }

        private Vector2 PinWorldPosition() => Position + DirectionForRotation() * PinX;

        private Vector2 DirectionForRotation()
        {
            float radians = RotationDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private void ApplyVisualRotation()
        {
            // Flash uses positive clockwise angles in its Y-down stage. Unity's
            // displayed Y axis is up, so the equivalent sprite rotation is negated.
            transform.rotation = Quaternion.Euler(0f, 0f, -RotationDegrees);
            // rangeCircle is attached to the controller in Flash, so cannon
            // rotation must not move its owner-bottom-anchored world position.
            UpdateRangeCircleVisual();
        }

        private void SetPosition(float x, float y)
        {
            PhysicsBody.State.X = x;
            PhysicsBody.State.Y = y;
            transform.position = MutinyPhysics.PixelToUnity(x, y);
            m_HasPlacementPresentation = false;
            UpdateRangeCircleVisual();
        }

        private void RecordPlacementPresentation(Vector2 previous, float elapsed)
        {
            m_PreviousPlacement = previous;
            m_CurrentPlacement = Position;
            m_PlacementPresentationElapsed = elapsed;
            m_HasPlacementPresentation = true;
        }

        private Vector3? SamplePlacementPresentationPosition()
        {
            Vector2 pixels = Position;
            if (m_HasPlacementPresentation && !IsFired &&
                (pixels - m_CurrentPlacement).sqrMagnitude < 0.0001f)
                pixels = Vector2.Lerp(m_PreviousPlacement, m_CurrentPlacement,
                    Mathf.Clamp01(m_PlacementPresentationElapsed / MutinyPhysics.TimeStep));
            return MutinyPhysics.PixelToUnity(pixels.x, pixels.y);
        }

        private void ResetPlacementPresentation()
        {
            m_HasPlacementPresentation = false;
            transform.position = MutinyPhysics.PixelToUnity(Position.x, Position.y);
        }

        protected override void LateUpdate()
        {
            transform.position = SamplePlacementPresentationPosition().Value;
        }

        internal void ApplyPlacementPresentationForVerification() => LateUpdate();

        private void OnDestroy()
        {
            if (PhysicsBody != null)
                PhysicsBody.PresentationPositionOverride = null;
            if (m_RangeCircleRenderer == null)
                return;

            if (Application.isPlaying)
                Destroy(m_RangeCircleRenderer.gameObject);
            else
                DestroyImmediate(m_RangeCircleRenderer.gameObject);
        }
    }
}
