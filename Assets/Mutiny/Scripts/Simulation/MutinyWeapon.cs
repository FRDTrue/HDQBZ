using System;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class MutinyWeapon : MonoBehaviour
    {
        // Dynamic Character-layer clips are created after all XML characters.
        // Keep them above the largest initial character holder and below water.
        public const int WeaponSortingOrder = 180;

        [Header("Weapon Identity")]
        public string WeaponType = "weapon";
        public MutinyCharacter Owner;

        [Header("State")]
        public bool IsFired = false;
        public bool IsFinished = false;
        public bool IsTwangable = true;
        public bool IsDraggable = false;
        public float TwangMaxForce = MutinyPhysics.DefaultTwangMaxForce;

        [Header("Physics Extents (px)")]
        public float Extent = 9f; // default 9 for cherry bomb

        public MutinyPhysicsBody PhysicsBody { get; protected set; }
        public SpriteRenderer SpriteRenderer { get; protected set; }

        public event Action OnFired;
        public event Action OnFinished;

        /// <summary>
        /// The Flash Character.equip position is only the initial position. Most
        /// Weapon subclasses continue to run Solid.advanceMotion before they are
        /// fired, so gravity and each weapon's extents determine the visible ready
        /// position on the terrain. Special click/place weapons which override
        /// advance without calling advanceMotion opt out below.
        /// </summary>
        public virtual bool AdvancesMotionWhileReady => true;

        /// <summary>
        /// Constructors which omit Clip.show have no weapon body on the character;
        /// their cursor/placement guide is the only ready-stage presentation.
        /// </summary>
        public virtual bool IsBodyVisibleWhileReady => true;

        // BoxWeapon AI tracks its next planned placement, not a moving Solid.
        // Other weapons retain their existing transform-based camera targets.
        public virtual Vector2? AiPlacementCameraTargetPixels => null;

        /// <summary>Matches Controller.twanging == this for pre-fire overrides.</summary>
        public bool IsBeingAimed { get; private set; }

        protected virtual void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
            SpriteRenderer.sortingOrder = WeaponSortingOrder;

            PhysicsBody = GetComponent<MutinyPhysicsBody>();
            if (PhysicsBody == null)
            {
                PhysicsBody = gameObject.AddComponent<MutinyPhysicsBody>();
            }
            PhysicsBody.ApplyWaterMotion = false;
        }

        private bool m_OverWater = true;
        // Cannonball and some weapon subclasses do not execute Weapon.advance's
        // inherited splashCheck; they supply their own post-motion behavior.
        protected virtual bool UsesInheritedSplashCheck => true;
        private readonly MutinyRotationState m_RotationState = new MutinyRotationState();
        private bool m_HasLoggedRotationVelocity;
        private float m_LastLoggedRotationVelocityX;

        public float LogicalRotationDegrees => m_RotationState.LogicalAngle;

        public virtual void Initialize(MutinyCharacter owner)
        {
            Owner = owner;
            IsFired = false;
            IsFinished = false;
            IsBeingAimed = false;
            m_OverWater = true;
            m_RotationState.Reset(RotationTransform != null ? RotationTransform.localEulerAngles.z : 0f);
            m_HasLoggedRotationVelocity = false;

            if (Owner != null)
            {
                transform.position = Owner.transform.position;
            }

            Vector2 px = MutinyPhysics.UnityToPixel(transform.position);
            PhysicsBody.State = PhysicsBodyState.CreateDefault(px.x, px.y);
            PhysicsBody.State.LeftExtent = Extent;
            PhysicsBody.State.RightExtent = Extent;
            PhysicsBody.State.TopExtent = Extent;
            PhysicsBody.State.BottomExtent = Extent;
            PhysicsBody.State.Bounce = 0.2f;
            PhysicsBody.State.Friction = 0.3f;
            PhysicsBody.State.HitsTiles = true;
            PhysicsBody.ApplyWaterMotion = false;

            if (Owner != null && Owner.PhysicsBody != null && !float.IsInfinity(Owner.PhysicsBody.WaterPixelY))
            {
                PhysicsBody.WaterPixelY = Owner.PhysicsBody.WaterPixelY;
            }
            PhysicsBody.CacheLevelTerrain();

            PhysicsBody.OnFloorLanded += () => OnContact(CollisionSide.Floor);
            PhysicsBody.OnCeilingHit += () => OnContact(CollisionSide.Ceiling);
            PhysicsBody.OnWallHit += () => OnContact(CollisionSide.Wall);
            PhysicsBody.OnEnterWater += HandleEnterWater;
            PhysicsBody.OnAfterMotionStep -= AdvanceInheritedSplashCheck;
            PhysicsBody.OnAfterMotionStep += AdvanceInheritedSplashCheck;
            PhysicsBody.OnBeforeSimulationStep -= AdvanceOriginalRotationTick;
            PhysicsBody.OnBeforeSimulationStep += AdvanceOriginalRotationTick;
            MutinyDebugLog.Info("Weapon",
                $"initialized type={WeaponType} owner={(Owner == null ? "none" : Owner.name)} waterY={PhysicsBody.WaterPixelY}", this);
        }

        // Character.equip in the Flash game creates the real weapon before the
        // player starts pulling it.  Keep that instance fixed at the original
        // equipment point until a weapon-specific commit method activates it.
        public virtual void PrepareForEquip()
        {
            if (Owner == null || PhysicsBody == null)
                return;

            Vector2 equipOffset = GetOriginalEquipOffsetPixels(WeaponType);
            Vector2 ownerPixels = Owner.PhysicsBody != null
                ? new Vector2(Owner.PhysicsBody.State.X, Owner.PhysicsBody.State.Y)
                : MutinyPhysics.UnityToPixel(Owner.transform.position);
            PhysicsBody.State.X = ownerPixels.x + equipOffset.x;
            PhysicsBody.State.Y = ownerPixels.y + equipOffset.y;
            PhysicsBody.SetVelocity(0f, 0f);
            // Character.advance calls equippedWeapon.advance every tick in the
            // original. Keeping ordinary weapon motion active is what lets a
            // banana (7 px extent), rum bottle (14 px), boulder (31 px), etc.
            // settle at their own visibly different heights instead of hovering
            // forever at the shared creation coordinate.
            PhysicsBody.IsActive = AdvancesMotionWhileReady;
            transform.position = MutinyPhysics.PixelToUnity(PhysicsBody.State.X, PhysicsBody.State.Y);
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = IsBodyVisibleWhileReady;
            MutinyDebugLog.Info("Weapon",
                $"equipped type={WeaponType} owner={Owner.name} pos=({PhysicsBody.State.X:F1},{PhysicsBody.State.Y:F1}) readyMotion={AdvancesMotionWhileReady} bodyVisible={IsBodyVisibleWhileReady}", this);
        }

        public static Vector2 GetOriginalEquipOffsetPixels(string weaponType)
        {
            // Character.as::equip uses (x, y - 10) for every weapon and applies
            // one additional 20 px upward offset only to Boulder.
            return string.Equals(weaponType, "boulder", StringComparison.OrdinalIgnoreCase)
                ? new Vector2(0f, -30f)
                : new Vector2(0f, -10f);
        }

        public void SetAimingState(bool aiming)
        {
            IsBeingAimed = aiming && !IsFired && !IsFinished;
        }

        public virtual void Fire(Vector2 velocityPx)
        {
            if (IsFired)
            {
                MutinyDebugLog.Warning("Weapon", $"duplicate fire ignored type={WeaponType}", this);
                return;
            }

            IsFired = true;
            IsFinished = false;
            IsBeingAimed = false;
            PhysicsBody.IsActive = true;
            // Weapon.fire(vx, vy) assigns the supplied velocity without a cap.
            // Only Solid.twang (below) clamps a pointer throw to twangMaxForce;
            // Weapon.release's separate 20 px/tick rule is not this path.
            PhysicsBody.SetVelocity(velocityPx.x, velocityPx.y);
            MutinyDebugLog.Info("Weapon",
                $"fired type={WeaponType} owner={(Owner == null ? "none" : Owner.name)} velocity={velocityPx}", this);

            if (Owner != null)
            {
                Owner.CanShoot = false;
                Owner.CanThrow = false;
            }

            OnFired?.Invoke();

            var turnManager = FindAnyObjectByType<MutinyTurnManager>();
            if (turnManager != null)
            {
                turnManager.NotifyActionStarted();
            }
        }

        public virtual void Twang(Vector2 startPx, Vector2 dragPx)
        {
            Vector2 launchVel = MutinyPhysics.CalculateTwangVelocity(startPx, dragPx, TwangMaxForce);
            Fire(launchVel);
        }

        protected virtual void OnContact(CollisionSide side)
        {
            // Base weapon contact handling (overridden by CherryBomb, Dynamite, etc.)
        }

        // Most weapons rotate their root sprite.  Symbols with a non-rotating
        // overlay can override this to rotate only their original child layer.
        protected virtual Transform RotationTransform => transform;

        protected virtual void HandleEnterWater()
        {
            OnWaterSubmerged();
        }

        private void AdvanceInheritedSplashCheck()
        {
            if (!UsesInheritedSplashCheck || !PhysicsBody.ApplyWaterPhysics ||
                !IsFired || IsFinished)
                return;

            PhysicsBodyState state = PhysicsBody.State;
            MutinyWaterSurface.CheckSplashCrossing(state.X, state.Y,
                PhysicsBody.WaterPixelY, ref m_OverWater);
        }

        protected virtual void OnWaterSubmerged()
        {
            // Optional hook for subclasses (e.g. Dynamite extinguishing fuse)
        }

        protected virtual void Update()
        {
            // Original Weapon.update is presentation-only. Weapon.advance applies
            // its finish conditions on the fixed simulation tick, not on render
            // time, world X, water depth, or an arbitrary lifetime.
        }

        protected void AdvanceInheritedFinishTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            PhysicsBodyState state = PhysicsBody.State;
            MutinyLevelRoot levelRoot = FindAnyObjectByType<MutinyLevelRoot>();
            bool belowLevel = levelRoot != null && levelRoot.Height > 0 &&
                              state.VelocityY > 0f &&
                              state.Y > levelRoot.Height * MutinyPhysics.PixelsPerUnit;
            bool atRest = state.VelocityX == 0f && Mathf.Abs(state.VelocityY) < 0.2f;
            if (!belowLevel && !atRest)
                return;

            MutinyDebugLog.Info("Weapon", $"original finish type={WeaponType} reason={(belowLevel ? "level-bottom" : "at-rest")}", this);
            Finish();
            Destroy(gameObject, 0.1f);
        }

        protected void AdvanceOriginalRotationTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            float delta = MutinyRotationRules.WeaponMotionDelta(
                WeaponType, PhysicsBody.State.VelocityX);
            m_RotationState.AddDelta(PhysicsBody.SimulationTickCount, delta);
            if (MutinyRotationRules.WeaponRotationMultiplier(WeaponType) > 0f &&
                (!m_HasLoggedRotationVelocity ||
                 !Mathf.Approximately(m_LastLoggedRotationVelocityX, PhysicsBody.State.VelocityX)))
            {
                MutinyDebugLog.Info("Rotation",
                    $"weapon angular step tick={PhysicsBody.SimulationTickCount} type={WeaponType} vx={PhysicsBody.State.VelocityX:F2} delta={delta:F2} target={m_RotationState.LogicalAngle:F2}", this);
                m_LastLoggedRotationVelocityX = PhysicsBody.State.VelocityX;
                m_HasLoggedRotationVelocity = true;
            }
        }

        protected virtual void LateUpdate()
        {
            // Only the five source subclasses with an explicit rotation formula
            // own this presentation channel. Cannon aiming and other weapon-specific
            // transforms must remain untouched by the common rotation presenter.
            if (PhysicsBody == null || RotationTransform == null ||
                MutinyRotationRules.WeaponRotationMultiplier(WeaponType) <= 0f)
                return;

            RotationTransform.localRotation = Quaternion.Euler(
                0f, 0f, m_RotationState.Sample(PhysicsBody.SimulationInterpolationAlpha));
        }

        internal float SampleOriginalRotation(float alpha)
        {
            return m_RotationState.Sample(alpha);
        }

        internal void ResetOriginalRotation(float angle)
        {
            m_RotationState.Reset(angle);
            m_HasLoggedRotationVelocity = false;
            if (RotationTransform != null)
                RotationTransform.localRotation = Quaternion.Euler(0f, 0f, m_RotationState.LogicalAngle);
        }

        public virtual void Finish()
        {
            if (IsFinished)
                return;

            IsFinished = true;
            MutinyDebugLog.Info("Weapon", $"finished type={WeaponType} name={name}", this);
            OnFinished?.Invoke();
        }
    }

    public enum CollisionSide
    {
        Floor,
        Ceiling,
        Wall,
        Water
    }
}
