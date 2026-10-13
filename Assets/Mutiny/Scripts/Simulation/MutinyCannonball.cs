using System.Collections.Generic;
using Mutiny.Diagnostics;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinyCannonball : MutinyWeapon
    {
        protected override bool UsesInheritedSplashCheck => false; // Cannonball.advance bypasses Weapon.advance
        public const float ExplosionSize = 100f;
        public const float ExplosionDamage = 50f;
        private Vector2 m_VisibleTickStartPixels;

        protected override void Awake()
        {
            WeaponType = "cannonball";
            Extent = 10f;
            base.Awake();
            // Cannon.fire creates this clip after the Cannon clip.  Flash assigns
            // it the next highest depth, so the ball stays above the cannon while
            // Cannon.update follows it.  Sharing a Unity sorting order leaves the
            // two coplanar sprites with unstable draw order and visible flicker.
            SpriteRenderer.sortingOrder = WeaponSortingOrder + 1;
            Sprite sprite = Resources.Load<Sprite>("Art/Weapons/Cannonball/1");
            if (sprite != null)
                SpriteRenderer.sprite = sprite;
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            PhysicsBody.State.Weight = 0f;
            PhysicsBody.State.HitsBoxes = true;
            // Cannonball.advanceMotion exits at y > water before Weapon.advance's
            // generic splashCheck can run.
            PhysicsBody.ApplyWaterPhysics = false;
            PhysicsBody.OnBeforeSimulationStep -= CaptureVisibleTickStart;
            PhysicsBody.OnBeforeSimulationStep += CaptureVisibleTickStart;
            PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
            PhysicsBody.OnSimulationStep += AdvanceOriginalTick;
        }

        private void CaptureVisibleTickStart()
        {
            PhysicsBodyState state = PhysicsBody.State;
            m_VisibleTickStartPixels = new Vector2(state.X, state.Y);
        }

        public void SetLaunchPosition(Vector2 pixelPosition)
        {
            PhysicsBody.State.X = pixelPosition.x;
            PhysicsBody.State.Y = pixelPosition.y;
            transform.position = MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y);
        }

        // Cannon.fire calls Cannonball.fire directly.  In the original this is
        // Weapon.fire, which only assigns the supplied velocity; it does not call
        // Weapon.release, so the cannon's fixed 30 force must not be clamped to 20
        // and it must not spend the owner's actions for a second time.
        public override void Fire(Vector2 velocityPx)
        {
            if (IsFired)
            {
                MutinyDebugLog.Warning("Cannonball", "duplicate fire ignored", this);
                return;
            }

            IsFired = true;
            IsFinished = false;
            PhysicsBody.IsActive = true;
            PhysicsBody.SetVelocity(velocityPx.x, velocityPx.y);
            MutinyDebugLog.Info("Cannonball", $"fired velocity={velocityPx}", this);
        }

        protected override void Update()
        {
            // Source Cannonball.advance owns its completion and water behavior.
        }

        protected override void OnContact(CollisionSide side)
        {
            if (IsFired && !IsFinished)
                Explode();
        }

        private void AdvanceOriginalTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            PhysicsBodyState state = PhysicsBody.State;
            if (IsOutsideOriginalBounds(state))
            {
                FinishWithoutExplosion("original cannonball boundary");
                return;
            }

            // Flash leaves smoke at the post-motion position, matching its
            // immediately rendered ball. Unity presents the ball one completed
            // tick behind authority, so the corresponding visible pose is the
            // start of this step. Spawning at the new authoritative position
            // puts a fresh puff up to 30 px ahead of the visible cannonball.
            MutinyRumBottleSmokeTrail.Spawn(m_VisibleTickStartPixels);
            ApplyCharacterContact(state);
        }

        private bool IsOutsideOriginalBounds(PhysicsBodyState state)
        {
            if (state.X < -300f || state.Y < -300f)
                return true;
            if (PhysicsBody.TryGetTerrain(out _, out int width, out _ ) && state.X > width * MutinyPhysics.PixelsPerUnit + 300f)
                return true;
            return !float.IsInfinity(PhysicsBody.WaterPixelY) && state.Y > PhysicsBody.WaterPixelY;
        }

        private void ApplyCharacterContact(PhysicsBodyState ball)
        {
            MutinyTeam[] teams = FindObjectsByType<MutinyTeam>(FindObjectsSortMode.None);
            for (int i = 0; i < teams.Length; i++)
            {
                List<MutinyCharacter> characters = teams[i].Characters;
                if (characters == null)
                    continue;
                for (int j = 0; j < characters.Count; j++)
                {
                    MutinyCharacter character = characters[j];
                    if (character == null || !character.IsAlive || character == Owner || character.PhysicsBody == null)
                        continue;
                    PhysicsBodyState target = character.PhysicsBody.State;
                    if (target.X - target.LeftExtent <= ball.X + ball.RightExtent &&
                        target.X + target.RightExtent >= ball.X - ball.LeftExtent &&
                        target.Y - target.TopExtent <= ball.Y + ball.BottomExtent &&
                        target.Y + target.BottomExtent >= ball.Y - ball.TopExtent)
                    {
                        // The original character branch is silent. User-requested
                        // feedback now routes both impact kinds through the same
                        // one-shot completion gate, avoiding duplicate pops.
                        Explode();
                        return;
                    }
                }
            }
        }

        public void Explode()
        {
            if (IsFinished)
                return;
            Vector2 position = new Vector2(PhysicsBody.State.X, PhysicsBody.State.Y);
            PhysicsBody.SetVelocity(0f, 0f);
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
            Finish();
            // The impact owns the one pop. Explosion.hit only applies damage,
            // so its frame-3 callback must never emit a second sound.
            MutinyExplosion.Spawn(position, ExplosionSize, ExplosionDamage, Owner,
                playPopOnHit: false);
            Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX("pop");
            MutinyDebugLog.Info("Cannonball", $"exploded x={position.x:F1} y={position.y:F1} pop=once", this);
        }

        private void FinishWithoutExplosion(string reason)
        {
            if (IsFinished)
                return;
            PhysicsBody.SetVelocity(0f, 0f);
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
            Finish();
            MutinyDebugLog.Info("Cannonball", reason, this);
        }

        private void OnDestroy()
        {
            if (PhysicsBody != null)
            {
                PhysicsBody.OnBeforeSimulationStep -= CaptureVisibleTickStart;
                PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
            }
        }
    }
}
