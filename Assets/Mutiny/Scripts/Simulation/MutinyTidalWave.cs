using System.Collections.Generic;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using Mutiny.Presentation;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinyTidalWave : MutinyWeapon
    {
        // TidalWave.advance only delegates to Weapon.advance after startWave().
        public override bool AdvancesMotionWhileReady => false;
        // TidalWave constructor omits show(); startWave() creates the visible clip.
        public override bool IsBodyVisibleWhileReady => false;
        protected override bool UsesInheritedSplashCheck => false; // emitted on startWave's first tick

        public const float OriginalStartX = -550f;
        public const float OriginalSpeed = 20f;
        public const float OriginalExitPadding = 550f;
        public const float OriginalHitHalfWidth = 150f;
        public const float OriginalHitHeightAboveWater = 300f;
        public const float OriginalDamagePerTick = 5f;
        private const int FramesPerSkyColour = 9;
        private const int VisibleFramesPerSkyColour = 5;

        // Flash Symbol 1058 (tidalWave): 499x352 px. Registration point (310.0, 350.0).
        // Unity Normalized Pivot: (310 / 499, 2 / 352).
        public static readonly Vector2 OriginalTidalWavePivot = new Vector2(310f / 499f, 2f / 352f);

        private readonly List<Sprite> m_Frames = new List<Sprite>(27);
        private int m_SkyColour = 1;
        private int m_AnimationFrame;
        private bool m_HasStartedSurfaceSplash;

        public int CurrentSkyColour => m_SkyColour;
        public int CurrentVisibleFrame => m_AnimationFrame + 1;
        public int CurrentSourceFrame => (m_SkyColour - 1) * FramesPerSkyColour + m_AnimationFrame + 1;

        public static int ResolveOriginalSkyColourForLevel(int level)
        {
            if ((level >= 1 && level <= 5) || (level >= 16 && level <= 21))
                return 1;
            if ((level >= 6 && level <= 10) || (level >= 22 && level <= 27))
                return 2;
            if ((level >= 11 && level <= 15) || (level >= 28 && level <= 33))
                return 3;
            return 1;
        }

        protected override void Awake()
        {
            WeaponType = "tidalWave";
            // TidalWave does not override Solid's default 10 px extents. Its
            // gameplay hit area is explicit in advance(), not a body extent.
            Extent = 10f;
            IsDraggable = false;
            IsTwangable = false;
            base.Awake();
            if (SpriteRenderer != null)
                SpriteRenderer.sortingOrder = MutinyLevelBuilder.TidalWaveSortingOrder;
            LoadFrames();
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            PhysicsBody.State.Weight = 0f;
            PhysicsBody.State.HitsTiles = false;
            PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
            PhysicsBody.OnSimulationStep += AdvanceOriginalTick;
            m_SkyColour = ResolveOriginalSkyColour();
            m_AnimationFrame = 0;
            m_HasStartedSurfaceSplash = false;
            ApplyAnimationFrame();
            // In Flash 2008 (TidalWave.as:15-24), constructor does not call show().
            // The wave graphic is hidden on the character during preview/ready,
            // and only becomes visible when startWave() is triggered on click.
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
        }

        public override void PrepareForEquip()
        {
            base.PrepareForEquip();
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = false;
        }

        // startX and direction remain in the signature for old callers, but the
        // original startWave() always uses -550 and a rightward velocity of 20.
        public void StartWave(float startX, float waterPixelY, float direction = 1f)
        {
            if (IsFired)
            {
                MutinyDebugLog.Warning("TidalWave", "duplicate start ignored", this);
                return;
            }

            m_SkyColour = ResolveOriginalSkyColour();
            m_AnimationFrame = 0;
            m_HasStartedSurfaceSplash = false;
            PhysicsBody.State.X = OriginalStartX;
            PhysicsBody.State.Y = waterPixelY;
            PhysicsBody.State.Weight = 0f;
            PhysicsBody.State.HitsTiles = false;
            PhysicsBody.WaterPixelY = waterPixelY;
            PhysicsBody.IsActive = true;
            PhysicsBody.SetVelocity(OriginalSpeed, 0f);
            transform.position = MutinyPhysics.PixelToUnity(OriginalStartX, waterPixelY);
            IsFired = true;
            IsFinished = false;
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = true;
            ApplyAnimationFrame();

            MutinyAudioManager.Instance?.PlaySFX("splash");

            if (Owner != null)
            {
                Owner.CanThrow = false;
                Owner.CanShoot = false;
            }

            MutinyDebugLog.Info("TidalWave",
                $"started pos=({OriginalStartX:F0},{waterPixelY:F0}) speed={OriginalSpeed:F0} sky={m_SkyColour}", this);
        }

        public void AdvanceOriginalTickForVerification()
        {
            AdvanceOriginalTick();
        }

        protected override void Update()
        {
            // Generic projectile rest/water rules are not meaningful for a wave.
            // PhysicsBody owns the movement and invokes AdvanceOriginalTick at 25 Hz.
        }

        private void AdvanceOriginalTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            EmitOriginalInitialSurfaceSplash();
            AdvanceAnimation();
            ApplyOriginalDamageWindow();

            if (PhysicsBody.State.X > ResolveLevelWidthPixels() + OriginalExitPadding)
            {
                Finish();
                if (SpriteRenderer != null)
                    SpriteRenderer.enabled = false;
                MutinyDebugLog.Info("TidalWave", "exited level right boundary", this);
                Destroy(gameObject, 0.1f);
            }
        }

        // TidalWave.advance calls Weapon.advance first. That inherited advance
        // changes Solid.overWater=true to y==water.y and runs splashCheck(),
        // which creates the colour-matched splash and plays splash exactly once
        // on the first 25 Hz tick.
        private void EmitOriginalInitialSurfaceSplash()
        {
            if (m_HasStartedSurfaceSplash)
                return;

            m_HasStartedSurfaceSplash = true;
            MutinyWaterSurface.SpawnSplash(PhysicsBody.State.X, PhysicsBody.WaterPixelY, m_SkyColour);
            MutinyDebugLog.Info("TidalWave",
                $"WPN-12-AUD-01 inherited splash tick pos=({PhysicsBody.State.X:F1},{PhysicsBody.WaterPixelY:F1}) sky={m_SkyColour}", this);
        }

        private void ApplyOriginalDamageWindow()
        {
            MutinyCharacter[] characters = Object.FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                MutinyCharacter character = characters[i];
                if (character == null || !character.IsAlive || character.PhysicsBody == null)
                    continue;

                PhysicsBodyState state = character.PhysicsBody.State;
                if (state.Y < PhysicsBody.WaterPixelY - OriginalHitHeightAboveWater ||
                    state.X < PhysicsBody.State.X - OriginalHitHalfWidth ||
                    state.X > PhysicsBody.State.X + OriginalHitHalfWidth)
                    continue;

                character.TakeDamage(OriginalDamagePerTick);
                MutinyDebugLog.Info("TidalWave",
                    $"hit character={character.name} damage={OriginalDamagePerTick:F0} waveX={PhysicsBody.State.X:F1}", character);
            }
        }

        private void AdvanceAnimation()
        {
            m_AnimationFrame = (m_AnimationFrame + 1) % VisibleFramesPerSkyColour;
            ApplyAnimationFrame();
        }

        private void ApplyAnimationFrame()
        {
            int sourceFrame = (m_SkyColour - 1) * FramesPerSkyColour + m_AnimationFrame;
            if (sourceFrame >= 0 && sourceFrame < m_Frames.Count && SpriteRenderer != null)
                SpriteRenderer.sprite = m_Frames[sourceFrame];
        }

        private int ResolveOriginalSkyColour()
        {
            MutinyLevelController controller = FindAnyObjectByType<MutinyLevelController>();
            int level = controller != null ? controller.CurrentLevelIndex : 1;
            return ResolveOriginalSkyColourForLevel(level);
        }

        private float ResolveLevelWidthPixels()
        {
            if (PhysicsBody.TryGetTerrain(out _, out int width, out _))
                return width * MutinyPhysics.PixelsPerUnit;

            MutinyLevelRoot root = FindAnyObjectByType<MutinyLevelRoot>();
            return root != null && root.Width > 0 ? root.Width * MutinyPhysics.PixelsPerUnit : 1600f;
        }

        private void LoadFrames()
        {
            m_Frames.Clear();
            for (int frame = 1; frame <= 27; frame++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Weapons/TidalWave/{frame}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        OriginalTidalWavePivot, MutinyPhysics.PixelsPerUnit);
                    m_Frames.Add(sprite);
                }
                else
                {
                    Sprite sprite = Resources.Load<Sprite>($"Art/Weapons/TidalWave/{frame}");
                    if (sprite != null)
                        m_Frames.Add(sprite);
                }
            }
        }

        private void OnDestroy()
        {
            if (PhysicsBody != null)
                PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
        }
    }
}
