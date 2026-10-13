using Mutiny.Diagnostics;
using Mutiny.Presentation;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinyVoodooDoll : MutinyWeapon
    {
        public const int OriginalOwnerFlightTicks = 10;
        public const int OriginalTargetWaitTicks = 10;
        public const float OriginalFadeAlphaPerTick = 0.1f;

        public MutinyCharacter TargetCharacter { get; private set; }

        private int m_FramesOnThis = OriginalOwnerFlightTicks;
        private int m_FramesOnTarget = OriginalTargetWaitTicks;
        private Vector2 m_ThrowVelocity;
        private bool m_TargetFocusRequested;
        private bool m_TransferredTargetVelocity;

        public bool HasTarget => TargetCharacter != null && TargetCharacter.IsAlive;
        public bool IsTargetFocusRequested => m_TargetFocusRequested;
        public bool HasTransferredTargetVelocity => m_TransferredTargetVelocity;
        public int FramesOnThis => m_FramesOnThis;
        public int FramesOnTarget => m_FramesOnTarget;

        public static bool HasPlayerCameraHandoff(MutinyTeam inputTeam)
        {
            if (inputTeam == null || inputTeam.IsAiControlled)
                return false;

            MutinyVoodooDoll[] dolls = FindObjectsByType<MutinyVoodooDoll>(FindObjectsSortMode.None);
            for (int i = 0; i < dolls.Length; i++)
            {
                MutinyVoodooDoll doll = dolls[i];
                if (doll != null && doll.IsFired && !doll.IsFinished &&
                    doll.IsTargetFocusRequested && doll.Owner != null &&
                    inputTeam.Characters.Contains(doll.Owner))
                    return true;
            }

            return false;
        }

        protected override void Awake()
        {
            WeaponType = "voodooDoll";
            // VoodooDoll does not override Solid's default four 10 px extents.
            Extent = 10f;
            IsDraggable = false;
            IsTwangable = false;
            base.Awake();
            LoadSprite();
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            // VoodooDoll.as constructor: hitsBoxes = true.
            PhysicsBody.State.HitsBoxes = true;
            TargetCharacter = null;
            m_FramesOnThis = OriginalOwnerFlightTicks;
            m_FramesOnTarget = OriginalTargetWaitTicks;
            m_ThrowVelocity = Vector2.zero;
            m_TargetFocusRequested = false;
            m_TransferredTargetVelocity = false;
            IsTwangable = false;

            if (SpriteRenderer != null)
            {
                Color color = SpriteRenderer.color;
                color.a = 1f;
                SpriteRenderer.color = color;
            }

            PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
            PhysicsBody.OnSimulationStep += AdvanceOriginalTick;
        }

        public bool BindTarget(MutinyCharacter target)
        {
            if (IsFired || target == null || !target.IsAlive)
            {
                MutinyDebugLog.Warning("VoodooDoll",
                    $"target binding rejected fired={IsFired} target={(target == null ? "none" : target.name)}", this);
                return false;
            }

            TargetCharacter = target;
            IsTwangable = true;
            // VoodooDoll.setTargetCharacter returns the camera to the owner
            // before the player starts twanging the now-enabled doll.
            MutinyCameraController.RequestPanToCharacter(Owner);
            MutinyDebugLog.Info("VoodooDoll", $"target bound target={target.name}; twang enabled", this);
            return true;
        }

        public override void Fire(Vector2 velocityPx)
        {
            if (!HasTarget)
            {
                MutinyDebugLog.Warning("VoodooDoll", "fire rejected because no live target is bound", this);
                return;
            }

            if (IsFired)
                return;

            base.Fire(velocityPx);
            if (!IsFired)
                return;

            m_ThrowVelocity = new Vector2(PhysicsBody.State.VelocityX, PhysicsBody.State.VelocityY);
            // Weapon.twang sets track=true in the original. Register the exact
            // equipped instance instead of relying on scene-order fallback.
            MutinyCameraController.RequestTrackWeapon(this);
            MutinyDebugLog.Info("VoodooDoll",
                $"fired target={TargetCharacter.name} velocity=({m_ThrowVelocity.x:F1},{m_ThrowVelocity.y:F1})", this);
        }

        public override void Twang(Vector2 startPx, Vector2 dragPx)
        {
            if (!HasTarget)
                return;

            // VoodooDoll inherits Solid's ordinary 20-force twang limit.
            base.Twang(startPx, dragPx);
        }

        public void FireForAi(Vector2 velocityPx)
        {
            bool wasFired = IsFired;
            Fire(velocityPx);
            if (!wasFired && IsFired)
                MutinyAudioManager.Instance?.PlaySFX("voodoo");
        }

        public void AdvanceOriginalTickForVerification(bool assumeCameraFocused = true)
        {
            AdvanceOriginalTick(assumeCameraFocused);
        }

        protected override void Update()
        {
            // VoodooDoll overrides Weapon.advance in AS2. In particular, it does
            // not inherit generic water, rest, or bounds paths.
        }

        private void AdvanceOriginalTick()
        {
            AdvanceOriginalTick(false);
        }

        private void AdvanceOriginalTick(bool assumeCameraFocused)
        {
            if (!IsFired || IsFinished)
                return;

            if (m_FramesOnThis > 0)
            {
                m_FramesOnThis--;
                if (m_FramesOnThis <= 0)
                {
                    m_TargetFocusRequested = true;
                    // Original ordering: panToCharacter=target, then track=false.
                    // Both occur in this same VoodooDoll.advance transition.
                    MutinyCameraController.RequestPanToCharacter(TargetCharacter);
                    MutinyCameraController.ReleaseWeaponTracking(this);
                    MutinyDebugLog.Info("VoodooDoll", $"owner flight complete; camera target={TargetCharacter?.name}", this);
                }
                return;
            }

            if (!m_TransferredTargetVelocity)
            {
                if (!HasTargetCameraPanCompleted(assumeCameraFocused))
                    return;

                if (m_FramesOnTarget > 0)
                {
                    m_FramesOnTarget--;
                    return;
                }

                TransferVelocityToTarget();
                return;
            }

            FadeAndFinish();
        }

        private bool HasTargetCameraPanCompleted(bool assumeCameraFocused)
        {
            if (assumeCameraFocused)
                return true;

            MutinyCameraController cameraController = FindAnyObjectByType<MutinyCameraController>();
            // VoodooDoll.as waits for TileSystem.panToCharacter to stop being the
            // selected target. TileSystem clears it only after panTowards arrives.
            return cameraController == null || !cameraController.IsPanningToCharacter(TargetCharacter);
        }

        private void TransferVelocityToTarget()
        {
            if (m_TransferredTargetVelocity)
                return;

            m_TransferredTargetVelocity = true;
            if (TargetCharacter != null && TargetCharacter.PhysicsBody != null)
            {
                // AS2 writes the saved release velocity once. It does not deal
                // direct damage or continuously mirror the doll velocity.
                TargetCharacter.PhysicsBody.SetVelocity(m_ThrowVelocity.x, m_ThrowVelocity.y);
                MutinyDebugLog.Info("VoodooDoll",
                    $"target velocity transferred target={TargetCharacter.name} velocity=({m_ThrowVelocity.x:F1},{m_ThrowVelocity.y:F1})", this);
            }
        }

        private void FadeAndFinish()
        {
            if (SpriteRenderer != null)
            {
                Color color = SpriteRenderer.color;
                color.a -= OriginalFadeAlphaPerTick;
                SpriteRenderer.color = color;
                if (color.a > 0f)
                    return;
            }

            Finish();
            Destroy(gameObject, 0.1f);
        }

        public static readonly Vector2 OriginalPivot = new Vector2(9f / 19f, 12f / 26f); // Symbol 1027: origin (9, 14) of 19x26

        private void LoadSprite()
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/Weapons/VoodooDoll/1");
            if (texture != null && SpriteRenderer != null)
            {
                texture.filterMode = FilterMode.Point;
                SpriteRenderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    OriginalPivot, MutinyPhysics.PixelsPerUnit);
            }
            else
            {
                Sprite sprite = Resources.Load<Sprite>("Art/Weapons/VoodooDoll/1");
                if (sprite != null && SpriteRenderer != null)
                    SpriteRenderer.sprite = sprite;
            }
        }

        private void OnDestroy()
        {
            if (PhysicsBody != null)
                PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
        }
    }
}
