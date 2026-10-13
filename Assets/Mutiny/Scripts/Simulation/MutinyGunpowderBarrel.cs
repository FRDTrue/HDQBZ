using System;
using System.Collections.Generic;
using Mutiny.Diagnostics;
using UnityEngine;

namespace Mutiny.Simulation
{
    /// <summary>
    /// Flash BoxWeapon/GunpowderBarrel: one selection places two static explosive boxes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MutinyGunpowderBarrel : MutinyWeapon
    {
        // BoxWeapon.advanceMotion is gated by fired in the Flash source.
        public override bool AdvancesMotionWhileReady => false;
        // BoxWeapon constructor omits show(); each legal place() reveals one box.
        public override bool IsBodyVisibleWhileReady => false;
        // Original BoxWeapon waits indefinitely for the follow-up placement.
        // Placement remains pending until its original click/AI state resolves.

        public const float LeftExtentPixels = 16f;
        public const float RightExtentPixels = 15f;
        public const int OriginalPlacementCount = 2;
        public const int OriginalTimelineFrames = 12;
        public const int OriginalExplodeLabelFrame = 11;
        public const int AiPlaceDelayTicks = 40;
        public const int AiDelayAfterPlaceTicks = 10;

        private readonly List<Sprite> m_Frames = new();
        private MutinyGunpowderBarrel m_NextBox;
        private MutinyGunpowderBarrel m_ParentBox;
        private int m_CreateMore = OriginalPlacementCount - 1;
        private bool m_IsRegistered;
        private bool m_IsExploding;
        private float m_AnimationAccumulator;
        private int m_AnimationFrame;
        private float m_SequenceAccumulator;
        private Vector2[] m_AiList;
        private Vector2 m_AiNext;
        private int m_AiIndex;
        private float m_AiOffset;
        private int m_AiDelay;
        private int m_AiDelayAfter;

        public MutinyGunpowderBarrel NextBox => m_NextBox;
        public bool HasPlacedAny => IsFired;
        public bool HasPendingPlacement => !IsFinished && FindPendingBox() != null;
        public bool IsExploding => m_IsExploding;
        public int PlacedCount => CountPlaced(GetRootBox());
        public int CurrentAnimationFrame => m_AnimationFrame + 1;
        public bool IsAiPlacementActive => m_AiList != null;
        public override Vector2? AiPlacementCameraTargetPixels =>
            m_ParentBox == null && IsAiPlacementActive && HasPendingPlacement &&
            Owner != null && Owner.IsAlive &&
            !float.IsNaN(m_AiNext.x) && !float.IsNaN(m_AiNext.y) &&
            !float.IsInfinity(m_AiNext.x) && !float.IsInfinity(m_AiNext.y)
                ? m_AiNext : (Vector2?)null;

        protected override void Awake()
        {
            WeaponType = "gunpowderBarrel";
            Extent = LeftExtentPixels;
            base.Awake();
            LoadFrames();
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            // BoxWeapon constructor: left/top=16, right/bottom=15; Solid defaults
            // remain weight=1, bounce=.2, friction=.3.
            PhysicsBody.State.LeftExtent = LeftExtentPixels;
            PhysicsBody.State.TopExtent = LeftExtentPixels;
            PhysicsBody.State.RightExtent = RightExtentPixels;
            PhysicsBody.State.BottomExtent = RightExtentPixels;
            PhysicsBody.State.Weight = MutinyPhysics.Gravity;
            PhysicsBody.State.Bounce = 0.2f;
            PhysicsBody.State.Friction = 0.3f;
            PhysicsBody.State.HitsTiles = true;
            PhysicsBody.State.HitsBoxes = true;
            PhysicsBody.IsActive = false;
            // BoxWeapon.advanceMotion never invokes Weapon.splashCheck; a placed
            // barrel keeps falling through water under ordinary Solid gravity.
            PhysicsBody.ApplyWaterPhysics = false;

            m_CreateMore = OriginalPlacementCount - 1;
            m_NextBox = null;
            m_IsRegistered = false;
            m_IsExploding = false;
            m_AnimationAccumulator = 0f;
            m_AnimationFrame = 0;
            m_SequenceAccumulator = 0f;
            m_AiList = null;
            m_AiDelay = 0;
            m_AiDelayAfter = 0;
            SetVisible(false);
        }

        /// <summary>Routes a stage click to the first unplaced BoxWeapon child.</summary>
        public bool TryPlaceAt(Vector2 pixelPosition)
        {
            MutinyGunpowderBarrel pending = FindPendingBox();
            if (pending == null)
                return false;
            if (!pending.CanPlace(pixelPosition))
            {
                MutinyDebugLog.Info("GunpowderBarrel", $"placement rejected pos=({pixelPosition.x:F1},{pixelPosition.y:F1}) placed={PlacedCount}", this);
                return false;
            }

            bool firstPlacement = !IsFired;
            pending.Place(pixelPosition);
            if (firstPlacement)
                FindAnyObjectByType<MutinyTurnManager>()?.NotifyActionStarted();
            return true;
        }

        /// <summary>Checks the same pending chain node that will receive the next click.</summary>
        public bool CanPlaceNext(Vector2 pixelPosition)
        {
            MutinyGunpowderBarrel pending = FindPendingBox();
            return pending != null && pending.CanPlace(pixelPosition);
        }

        /// <summary>Original BoxWeapon.canPlace, retaining raw pixel coordinates.</summary>
        public bool CanPlace(Vector2 pixelPosition)
        {
            if (PhysicsBody == null || !PhysicsBody.TryGetTerrain(out string[,] terrain, out int width, out int height))
                return false;
            return CanPlace(pixelPosition, terrain, width, height);
        }

        public bool CanPlace(Vector2 pixelPosition, string[,] terrain, int gridWidth, int gridHeight)
        {
            if (terrain == null || gridWidth <= 0 || gridHeight <= 0)
                return false;

            float px = pixelPosition.x;
            float py = pixelPosition.y;
            int left = Mathf.FloorToInt((px - LeftExtentPixels) / 32f);
            int right = Mathf.FloorToInt((px + LeftExtentPixels) / 32f);
            int top = Mathf.FloorToInt((py - LeftExtentPixels) / 32f);
            int bottom = Mathf.FloorToInt((py + LeftExtentPixels) / 32f);
            for (int x = left; x <= right; x++)
                for (int y = top; y <= bottom; y++)
                    if (IsSolidTile(terrain, x, y, gridWidth, gridHeight))
                        return false;

            // The original retains the first support surface as a vertical boundary.
            float obstructionY = (bottom + 1) * 32f;
            for (int y = bottom; y < gridHeight; y++)
            {
                bool foundSolid = false;
                for (int x = left; x <= right; x++)
                {
                    if (IsSolidTile(terrain, x, y, gridWidth, gridHeight))
                    {
                        // Original canPlace accepts the first supporting column;
                        // the complete barrel width does not need ground beneath it.
                        foundSolid = true;
                        break;
                    }
                }
                if (foundSolid)
                {
                    obstructionY = (y + 1) * 32f;
                    break;
                }
            }

            List<PhysicsBoxObstacle> boxes = MutinyBoxRegistry.GetObstacles(PhysicsBody);
            for (int i = 0; i < boxes.Count; i++)
            {
                PhysicsBodyState other = boxes[i].State;
                if (other.X - other.LeftExtent <= px + LeftExtentPixels &&
                    other.X + other.RightExtent >= px - RightExtentPixels &&
                    other.Y + other.BottomExtent >= py - RightExtentPixels)
                {
                    if (other.Y - other.TopExtent < obstructionY)
                        obstructionY = other.Y - other.TopExtent;
                    if (other.Y - other.TopExtent <= py + LeftExtentPixels)
                        return false;
                }
            }

            MutinyTreasureChest[] chests = FindObjectsByType<MutinyTreasureChest>(FindObjectsSortMode.None);
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i] == null)
                    continue;
                Vector2 chestPx = new Vector2(chests[i].PixelX, chests[i].PixelY);
                if (chestPx.x - 16f <= px + LeftExtentPixels &&
                    chestPx.x + 16f >= px - RightExtentPixels &&
                    chestPx.y + 16f >= py - LeftExtentPixels)
                    return false;
            }

            MutinyCharacter[] characters = FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                MutinyCharacter character = characters[i];
                if (character == null || !character.IsAlive || character.PhysicsBody == null)
                    continue;
                PhysicsBodyState body = character.PhysicsBody.State;
                if (body.X - body.LeftExtent <= px + LeftExtentPixels &&
                    body.X + body.RightExtent >= px - RightExtentPixels &&
                    body.Y + body.BottomExtent >= py - RightExtentPixels &&
                    body.Y - body.TopExtent <= obstructionY)
                    return false;
            }
            return true;
        }

        /// <summary>Called by MutinyExplosion after its original AABB-radius check.</summary>
        public void Explode()
        {
            if (m_IsExploding)
                return;
            UnregisterPlacedCrate();
            m_IsExploding = true;
            if (PhysicsBody != null)
                PhysicsBody.IsActive = false;
            // DefineSprite 968: label `explode` is frame 11. It still displays
            // the static child for that frame; frame 12 removes it and destroys it.
            m_AnimationFrame = Mathf.Min(OriginalExplodeLabelFrame - 1, m_Frames.Count - 1);
            ApplyFrame();
            MutinyExplosion.Spawn(new Vector2(PhysicsBody.State.X, PhysicsBody.State.Y), 150f, 30f, null);
            MutinyDebugLog.Info("GunpowderBarrel", $"exploded pos={PhysicsBody?.State.X:F1},{PhysicsBody?.State.Y:F1}", this);
        }

        public static List<PhysicsBoxObstacle> GetPhysicsObstacles(MutinyPhysicsBody requester = null)
        {
            return MutinyBoxRegistry.GetObstacles(requester);
        }

        protected override void Update()
        {
            AdvancePlacementSequence();
            if (m_IsExploding)
                AdvanceExplosionAnimation();
        }

        private void OnDestroy() => UnregisterPlacedCrate();

        private MutinyGunpowderBarrel FindPendingBox()
        {
            if (!IsFired)
                return this;
            return m_NextBox != null ? m_NextBox.FindPendingBox() : null;
        }

        private MutinyGunpowderBarrel GetRootBox()
        {
            MutinyGunpowderBarrel root = this;
            while (root.m_ParentBox != null)
                root = root.m_ParentBox;
            return root;
        }

        private void Place(Vector2 pixelPosition)
        {
            transform.position = MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y);
            PhysicsBody.State.X = pixelPosition.x;
            PhysicsBody.State.Y = pixelPosition.y;
            PhysicsBody.State.VelocityX = 0f;
            PhysicsBody.State.VelocityY = 0f;
            PhysicsBody.IsActive = true;
            IsFired = true;
            SetVisible(true);
            if (!m_IsRegistered)
            {
                MutinyBoxRegistry.Register(PhysicsBody);
                m_IsRegistered = true;
            }
            if (Owner != null)
            {
                Owner.CanShoot = false;
                Owner.CanThrow = false;
            }

            if (m_CreateMore > 0)
            {
                m_NextBox = MutinyWeaponFactory.SpawnWeapon("gunpowderBarrel", Owner) as MutinyGunpowderBarrel;
                if (m_NextBox != null)
                {
                    m_NextBox.m_CreateMore = m_CreateMore - 1;
                    m_NextBox.m_ParentBox = this;
                    if (PhysicsBody.TryGetTerrain(out string[,] terrain, out int width, out int height))
                        m_NextBox.PhysicsBody.SetTerrain(terrain, width, height);
                    m_NextBox.SetVisible(false);
                }
            }
            else
            {
                Finish();
            }
            MutinyDebugLog.Info("GunpowderBarrel", $"placed index={PlacedCount}/{OriginalPlacementCount} pos=({pixelPosition.x:F1},{pixelPosition.y:F1}) next={(m_NextBox != null)}", this);
        }

        // BoxWeapon.advance runs even when its own Solid body is no longer moving.
        // Keep the sequence on the root at the original 25 Hz, rather than tying it
        // to PhysicsBody.OnSimulationStep (which stops for an exploded parent).
        private void AdvancePlacementSequence()
        {
            if (m_ParentBox != null || (IsFinished && m_AiList == null))
                return;

            m_SequenceAccumulator += Time.deltaTime;
            while (m_SequenceAccumulator >= MutinyPhysics.TimeStep)
            {
                m_SequenceAccumulator -= MutinyPhysics.TimeStep;
                AdvanceSequenceTick();
            }
        }

        private void AdvanceSequenceTick()
        {
            if (m_AiDelay > 0)
            {
                m_AiDelay--;
                if (m_AiDelay == 0)
                {
                    MutinyGunpowderBarrel pending = FindPendingBox();
                    if (pending != null)
                    {
                        pending.Place(m_AiNext);
                        pending.SetVisible(true);
                        m_AiDelayAfter = AiDelayAfterPlaceTicks;
                        MutinyDebugLog.Info("GunpowderBarrel", $"AI placed index={PlacedCount}/{OriginalPlacementCount} pos={m_AiNext}", this);
                    }
                }
            }
            else if (m_AiDelayAfter > 0)
            {
                m_AiDelayAfter--;
                if (m_AiDelayAfter == 0 && FindPendingBox() != null)
                    AiContinue();
            }

            if (m_NextBox != null && m_NextBox.IsFinished && !IsFinished)
            {
                Finish();
                m_AiList = null;
                MutinyDebugLog.Info("GunpowderBarrel", $"placement sequence finished count={PlacedCount}", this);
            }
        }

        internal void AdvancePlacementTickForVerification() => AdvanceSequenceTick();

        /// <summary>BoxWeapon.aiPerform: retain the first three source candidates.</summary>
        public bool BeginAiPlacement(Vector2[] possibilities)
        {
            if (possibilities == null || possibilities.Length < 3 || Owner == null || !Owner.IsAlive)
                return false;

            int count = Mathf.Min(3, possibilities.Length);
            m_AiList = new Vector2[count];
            Array.Copy(possibilities, m_AiList, count);
            m_AiIndex = 0;
            m_AiOffset = 0f;
            m_AiNext = new Vector2(float.NaN, float.NaN);
            m_AiDelay = 0;
            m_AiDelayAfter = 0;
            AiContinue();
            MutinyDebugLog.Info("GunpowderBarrel", $"AI armed candidates={count} firstDelay={m_AiDelay}", this);
            return m_AiList != null && m_AiDelay > 0;
        }

        private void AiContinue()
        {
            if (m_AiList == null || m_AiList.Length == 0)
                return;

            m_AiDelay = AiPlaceDelayTicks;
            // Original quirk: first call has undefined aiNext coordinates, then
            // advances aiIndex before selecting a candidate. Keep that ordering.
            if (!float.IsNaN(m_AiNext.x) && CanPlace(m_AiNext + Vector2.up * -48f) && UnityEngine.Random.value >= 0.4f)
            {
                m_AiOffset -= 48f;
                m_AiNext.y -= 48f;
                return;
            }

            for (int attempts = 0; attempts < m_AiList.Length * 12; attempts++)
            {
                m_AiIndex++;
                if (m_AiIndex >= m_AiList.Length)
                {
                    m_AiIndex = 0;
                    m_AiOffset -= 32f;
                }
                Vector2 candidate = m_AiList[m_AiIndex] + Vector2.up * m_AiOffset;
                if (CanPlace(candidate))
                {
                    m_AiNext = candidate;
                    return;
                }
            }

            // Source assumes one of its candidates remains valid. Avoid an endless
            // loop in a changed Unity board while keeping the failed action visible.
            m_AiList = null;
            MutinyDebugLog.Warning("GunpowderBarrel", "AI found no legal follow-up barrel placement", this);
        }

        public static readonly Vector2 OriginalPivot = new Vector2(16f / 33f, 16f / 32f); // Symbol 968: origin (16, 16) of 33x32

        private void LoadFrames()
        {
            m_Frames.Clear();
            for (int i = 1; i <= OriginalTimelineFrames; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Weapons/GunpowderBarrel/{i}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite frame = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        OriginalPivot, MutinyPhysics.PixelsPerUnit);
                    m_Frames.Add(frame);
                }
                else
                {
                    Sprite frame = Resources.Load<Sprite>($"Art/Weapons/GunpowderBarrel/{i}");
                    if (frame != null)
                        m_Frames.Add(frame);
                }
            }
            ApplyFrame();
        }

        private void AdvanceExplosionAnimation()
        {
            m_AnimationAccumulator += Time.deltaTime;
            while (m_AnimationAccumulator >= MutinyPhysics.TimeStep)
            {
                m_AnimationAccumulator -= MutinyPhysics.TimeStep;
                m_AnimationFrame++;
                if (m_AnimationFrame >= m_Frames.Count)
                {
                    // BoxWeapon.explode only changes the movie-clip timeline; it
                    // deliberately leaves the chain alive so later crates can still
                    // be placed. The completed final box may now be destroyed.
                    SetVisible(false);
                    if (IsFinished)
                        Destroy(gameObject);
                    return;
                }
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (SpriteRenderer != null && m_AnimationFrame >= 0 && m_AnimationFrame < m_Frames.Count)
                SpriteRenderer.sprite = m_Frames[m_AnimationFrame];
        }

        private void SetVisible(bool visible)
        {
            if (SpriteRenderer != null)
                SpriteRenderer.enabled = visible;
        }

        private void UnregisterPlacedCrate()
        {
            if (!m_IsRegistered)
                return;
            MutinyBoxRegistry.Unregister(PhysicsBody);
            m_IsRegistered = false;
        }

        private static int CountPlaced(MutinyGunpowderBarrel box)
        {
            if (box == null)
                return 0;
            return (box.IsFired ? 1 : 0) + CountPlaced(box.m_NextBox);
        }

        private static bool IsSolidTile(string[,] terrain, int col, int row, int width, int height)
        {
            if (col < 0 || col >= width || row < 0 || row >= height)
                return false;
            string tile = terrain[row, col];
            return !string.IsNullOrEmpty(tile) && tile != "-" &&
                   tile.IndexOf("ripple", System.StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
