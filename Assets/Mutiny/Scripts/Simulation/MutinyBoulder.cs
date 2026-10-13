using System.Collections.Generic;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using UnityEngine;

namespace Mutiny.Simulation
{
    /// <summary>Flash Boulder: rolling 62px Solid that shoves and damages characters.</summary>
    [DisallowMultipleComponent]
    public sealed class MutinyBoulder : MutinyWeapon
    {
        public const float ExtentPixels = 31f;
        public const float WeightPerTick = 1.5f;
        public const float FrictionPerFloorContact = 0.25f;
        public const float RotationDegreesPerVelocityX = 2.5f;
        public const float CharacterContactExtent = 32f;
        public const float DamagePerVelocityX = 1.5f;
        public const float SettleVelocityYThreshold = 0.5f;
        public const float VisibilityDecrementPerTick = 0.1f;

        private float m_Visibility = 2f;
        // Solid.splashCheck starts over water and emits on either crossing.  Boulder
        // inherits Weapon.advance, so it uses this one-shot crossing check rather
        // than Character's continuous underwater movement.
        private bool m_OverWater = true;
        private SpriteRenderer m_RotatingRenderer;
        public float Visibility => m_Visibility;
        public bool IsOverWater => m_OverWater;
        public Transform RotatingVisual => m_RotatingRenderer != null ? m_RotatingRenderer.transform : null;

        protected override Transform RotationTransform =>
            m_RotatingRenderer != null ? m_RotatingRenderer.transform : base.RotationTransform;

        protected override void Awake()
        {
            WeaponType = "boulder";
            Extent = ExtentPixels;
            base.Awake();
            CreateOriginalVisualLayers();
            LoadOriginalVisualLayers();
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            PhysicsBody.State.LeftExtent = ExtentPixels;
            PhysicsBody.State.RightExtent = ExtentPixels;
            PhysicsBody.State.TopExtent = ExtentPixels;
            PhysicsBody.State.BottomExtent = ExtentPixels;
            PhysicsBody.State.Weight = WeightPerTick;
            PhysicsBody.State.Friction = FrictionPerFloorContact;
            PhysicsBody.State.HitsBoxes = true;
            // Weapon.advance -> Solid.splashCheck only creates a splash and updates
            // overWater.  It never applies Character.advance's underwater drag.
            PhysicsBody.ApplyWaterPhysics = false;
            PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
            PhysicsBody.OnSimulationStep += AdvanceOriginalTick;
            m_Visibility = 2f;
            m_OverWater = true;
            if (SpriteRenderer != null)
            {
                SpriteRenderer.color = Color.white;
                SpriteRenderer.enabled = true;
            }
            if (m_RotatingRenderer != null)
            {
                m_RotatingRenderer.color = Color.white;
                m_RotatingRenderer.enabled = true;
            }
        }

        public override void Fire(Vector2 velocityPx)
        {
            if (IsFired)
            {
                base.Fire(velocityPx);
                return;
            }

            base.Fire(velocityPx);
            // Boulder.release has a .5 multiplier, but the normal paths never call
            // it: a player uses Weapon.twang and AI uses Weapon.fire.  Do not put
            // that drag-release-only conversion into Fire or every real throw is
            // made half-strength.
            MutinyDebugLog.Info("Boulder",
                $"fired direct velocity=({PhysicsBody.State.VelocityX:F2},{PhysicsBody.State.VelocityY:F2})", this);
        }

        protected override void Update()
        {
            // Boulder applies its inherited Weapon.advance completion checks in
            // the fixed 25 Hz simulation callback below.
        }

        public void AdvanceOriginalTickForVerification()
        {
            if (PhysicsBody == null || IsFinished)
                return;

            // Exercise the same 25 Hz callback sequence used in play: boulder
            // rotation, Solid physics, Boulder contact/fade, then Weapon finish and
            // splash handling.  Do not call the post-motion hook directly.
            PhysicsBody.AdvanceSimulationTick();
            transform.position = MutinyPhysics.PixelToUnity(PhysicsBody.State.X, PhysicsBody.State.Y);
        }

        private void AdvanceOriginalTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            // This callback happens after Solid.advanceMotion. The inherited weapon
            // rotation callback has already rotated only the source `rotating`
            // child by vx * 2.5 in Flash coordinates; its upper overlay stays put.
            PhysicsBodyState boulderState = PhysicsBody.State;
            if (boulderState.VelocityX == 0f && Mathf.Abs(boulderState.VelocityY) < SettleVelocityYThreshold)
            {
                m_Visibility -= VisibilityDecrementPerTick;
                if (m_Visibility < 1f)
                    ApplyWhiteOut(m_Visibility);
                if (m_Visibility < 0f)
                {
                    Finish();
                    Destroy(gameObject, 0.2f);
                    MutinyDebugLog.Info("Boulder", "finished after original settle fade", this);
                    return;
                }
            }

            ApplyCharacterContacts(boulderState);

            // These are Weapon.advance's two source finish checks.  Its
            // Solid.splashCheck equivalent runs below without changing velocity.
            if (boulderState.VelocityY > 0f &&
                boulderState.Y > ResolveLevelHeightPixels())
            {
                Finish();
                Destroy(gameObject, 0.2f);
                MutinyDebugLog.Info("Boulder", "finished below original level bottom", this);
            }
            else if (boulderState.VelocityX == 0f && Mathf.Abs(boulderState.VelocityY) < 0.2f)
            {
                Finish();
                Destroy(gameObject, 0.2f);
                MutinyDebugLog.Info("Boulder", "finished at inherited Weapon rest threshold", this);
            }

            // Weapon.advance invokes Solid.splashCheck after its two terminal
            // checks, provided Boulder.advanceMotion did not already set finished.
            // This must run even when the map-bottom/rest branch set finished on
            // this tick, matching the already-entered AS2 outer condition.
            AdvanceSplashCheck();
        }

        private float ResolveLevelHeightPixels()
        {
            if (PhysicsBody.TryGetTerrain(out _, out _, out int gridHeight))
                return gridHeight * MutinyPhysics.PixelsPerUnit;

            // Controller.tileSystem.levelHeight is always present in the Flash
            // level.  A Unity scene may expose it through LevelRoot before its
            // terrain cache is populated, so preserve the same bottom boundary.
            MutinyLevelRoot root = FindAnyObjectByType<MutinyLevelRoot>();
            return root != null && root.Height > 0
                ? root.Height * MutinyPhysics.PixelsPerUnit
                : float.PositiveInfinity;
        }

        private void AdvanceSplashCheck()
        {
            if (float.IsInfinity(PhysicsBody.WaterPixelY))
                return;

            bool overWater = PhysicsBody.State.Y < PhysicsBody.WaterPixelY;
            if (m_OverWater != overWater)
            {
                MutinyWaterSurface.SpawnSplash(PhysicsBody.State.X, PhysicsBody.WaterPixelY);
                Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX("splash");
                MutinyDebugLog.Info("Boulder",
                    $"water crossing y={PhysicsBody.State.Y:F1} waterY={PhysicsBody.WaterPixelY:F1}", this);
            }
            m_OverWater = overWater;
        }

        private void ApplyCharacterContacts(PhysicsBodyState boulderState)
        {
            // Boulder.as iterates Controller.teams, not arbitrary scene objects.
            MutinyTeam[] teams = FindObjectsByType<MutinyTeam>(FindObjectsSortMode.None);
            for (int teamIndex = 0; teamIndex < teams.Length; teamIndex++)
            {
                List<MutinyCharacter> characters = teams[teamIndex].Characters;
                if (characters == null)
                    continue;

                for (int characterIndex = 0; characterIndex < characters.Count; characterIndex++)
                {
                    MutinyCharacter character = characters[characterIndex];
                    if (character == null || !character.IsAlive || character == Owner || character.PhysicsBody == null)
                        continue;

                    PhysicsBodyState characterState = character.PhysicsBody.State;
                    if (characterState.Y - characterState.TopExtent > boulderState.Y + CharacterContactExtent ||
                        characterState.Y + characterState.BottomExtent < boulderState.Y - CharacterContactExtent ||
                        Mathf.Abs(characterState.X - boulderState.X) > CharacterContactExtent)
                        continue;

                    if (characterState.X > boulderState.X)
                    {
                        characterState.X = boulderState.X + CharacterContactExtent;
                        if (boulderState.VelocityX > 0f)
                            characterState.VelocityX += boulderState.VelocityX;
                    }
                    else
                    {
                        characterState.X = boulderState.X - CharacterContactExtent;
                        if (boulderState.VelocityX < 0f)
                            characterState.VelocityX += boulderState.VelocityX;
                    }

                    character.PhysicsBody.State = characterState;
                    character.TakeDamage(Mathf.Abs(boulderState.VelocityX) * DamagePerVelocityX);
                }
            }
        }

        private void ApplyWhiteOut(float visibility)
        {
            if (SpriteRenderer == null)
                return;

            // Global.whiteOut is additive while visibility > .5. The default Unity
            // sprite material cannot encode its additive term, so preserve exact
            // final white-alpha behavior and keep the bright half opaque pending a
            // dedicated parity material.
            Color color = visibility <= 0.5f
                ? new Color(1f, 1f, 1f, Mathf.Max(0f, visibility * 2f))
                : Color.white;
            SpriteRenderer.color = color;
            if (m_RotatingRenderer != null)
                m_RotatingRenderer.color = color;
        }

        private void CreateOriginalVisualLayers()
        {
            Transform rotating = transform.Find("Rotating");
            if (rotating == null)
            {
                var rotatingObject = new GameObject("Rotating");
                rotating = rotatingObject.transform;
                rotating.SetParent(transform, false);
            }

            rotating.localPosition = Vector3.zero;
            rotating.localRotation = Quaternion.identity;
            rotating.localScale = Vector3.one;
            m_RotatingRenderer = rotating.GetComponent<SpriteRenderer>();
            if (m_RotatingRenderer == null)
                m_RotatingRenderer = rotating.gameObject.AddComponent<SpriteRenderer>();

            // DefineSprite_872: 869 "rotating" at depth 1, then 871 at depth 3.
            m_RotatingRenderer.sortingOrder = WeaponSortingOrder;
            SpriteRenderer.sortingOrder = WeaponSortingOrder + 1;
        }

        private void LoadOriginalVisualLayers()
        {
            if (SpriteRenderer == null || m_RotatingRenderer == null)
                return;

            Sprite rotating = Resources.Load<Sprite>("Art/Weapons/Boulder/rotating");
            Sprite overlay = Resources.Load<Sprite>("Art/Weapons/Boulder/overlay");
            if (rotating != null && overlay != null)
            {
                m_RotatingRenderer.sprite = rotating;
                SpriteRenderer.sprite = overlay;
                return;
            }

            // Keep existing scenes visible until Unity imports the two original
            // child exports.  The fallback is intentionally not used once both
            // source layers are available.
            SpriteRenderer.sprite = Resources.Load<Sprite>("Art/Weapons/Boulder/1");
            m_RotatingRenderer.enabled = false;
            MutinyDebugLog.Warning("Boulder",
                "missing rotating/overlay source sprites; using legacy flattened fallback", this);
        }

        private void OnDestroy()
        {
            if (PhysicsBody != null)
                PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
        }
    }
}
