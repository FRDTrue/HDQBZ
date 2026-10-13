using System.Collections.Generic;
using Mutiny.Diagnostics;
using Mutiny.Presentation;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinyMine : MutinyWeapon
    {
        public const int IgnoreTicks = 10;
        public const int CountdownTicks = 60;
        public const float TriggerRadiusPixels = 60f;
        public const int InThrowFrame = 1;
        public const int ArmStartFrame = 11;
        public const int ArmStopFrame = 16;
        public const int WarnStartFrame = 21;
        public const int WarnLastVisibleFrame = 29;
        private static readonly int[] BeepTimes = { 0, 15, 30, 38, 45, 49, 53, 55, 57, 59 };

        private enum PresentationState
        {
            InThrow,
            Arming,
            Armed,
            Warning
        }

        private readonly List<Sprite> m_Frames = new();
        private int m_IgnoreTicks;
        private int m_Countdown;
        private int m_NextBeepIndex;
        private bool m_Stored;
        private bool m_Active;
        private bool m_Exploded;
        private PresentationState m_PresentationState;
        private int m_AnimationFrame;

        public bool IsStored => m_Stored;
        public bool IsActive => m_Active;
        public int CountdownRemaining => m_Countdown;
        public bool BlocksTurn => !m_Stored || m_Active;
        public int CurrentAnimationFrame => m_AnimationFrame + 1;
        public bool IsArmingAnimation => m_PresentationState == PresentationState.Arming;
        public bool IsWarningAnimation => m_PresentationState == PresentationState.Warning;

        // Controller.unloadLevel destroys the stored mines separately from the
        // level tiles. Unity weapon instances are also spawned outside the level
        // root, so remove them before a rebuilt level can observe the old map.
        public static int ClearForLevelUnload()
        {
            MutinyMine[] mines = FindObjectsByType<MutinyMine>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < mines.Length; i++)
            {
                MutinyMine mine = mines[i];
                if (mine == null)
                    continue;

                if (mine.PhysicsBody != null)
                    mine.PhysicsBody.IsActive = false;
                mine.gameObject.SetActive(false);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(mine.gameObject);
                else
                    Destroy(mine.gameObject);
#else
                Destroy(mine.gameObject);
#endif
            }
            return mines.Length;
        }

        public static readonly Vector2 OriginalPivot = new Vector2(18f / 38f, 17f / 35f); // Symbol 1024: origin (18, 18) of 38x35

        protected override void Awake()
        {
            WeaponType = "mine";
            Extent = 14f;
            base.Awake();
            for (int i = 1; i <= 30; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Weapons/Mine/{i}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite frame = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        OriginalPivot, MutinyPhysics.PixelsPerUnit);
                    m_Frames.Add(frame);
                }
                else
                {
                    Sprite frame = Resources.Load<Sprite>($"Art/Weapons/Mine/{i}");
                    if (frame != null) m_Frames.Add(frame);
                }
            }
            if (m_Frames.Count > 0) SpriteRenderer.sprite = m_Frames[0];
        }

        public override void Initialize(MutinyCharacter owner)
        {
            base.Initialize(owner);
            PhysicsBody.State.LeftExtent = PhysicsBody.State.RightExtent = 14f;
            PhysicsBody.State.TopExtent = PhysicsBody.State.BottomExtent = 14f;
            PhysicsBody.State.Friction = 1.5f;
            PhysicsBody.State.HitsBoxes = true;
            PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
            PhysicsBody.OnSimulationStep += AdvanceOriginalTick;
            m_IgnoreTicks = IgnoreTicks;
            m_Countdown = CountdownTicks;
            m_NextBeepIndex = 0;
            m_Stored = m_Active = m_Exploded = false;
            m_PresentationState = PresentationState.InThrow;
            SetAnimationFrame(InThrowFrame);
        }

        protected override void Update()
        {
            // Mine.advance owns its lifecycle, including a stored armed mine.
        }

        public void AdvanceOriginalTickForVerification() => AdvanceOriginalTick();

        private void AdvanceOriginalTick()
        {
            if (!IsFired || IsFinished || PhysicsBody == null)
                return;

            AdvancePresentationTick();

            if (!m_Active)
                CheckForProximity();

            PhysicsBodyState state = PhysicsBody.State;
            if (!m_Stored && state.VelocityX == 0f && Mathf.Abs(state.VelocityY) < 0.2f)
            {
                m_Stored = true;
                if (!m_Active)
                    StartArmAnimation();
                MutinyDebugLog.Info("Mine", "stored and armed", this);
            }

            if (!m_Active || m_Exploded)
                return;

            m_Countdown--;
            int elapsed = CountdownTicks - m_Countdown;
            while (m_NextBeepIndex < BeepTimes.Length && elapsed >= BeepTimes[m_NextBeepIndex])
            {
                Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX("mine_beep");
                m_NextBeepIndex++;
            }

            if (m_Countdown <= 0)
                Explode();
        }

        private void CheckForProximity()
        {
            if (--m_IgnoreTicks > 0)
                return;

            MutinyTeam[] teams = FindObjectsByType<MutinyTeam>(FindObjectsSortMode.None);
            for (int i = 0; i < teams.Length; i++)
            {
                List<MutinyCharacter> characters = teams[i].Characters;
                if (characters == null) continue;
                for (int j = 0; j < characters.Count; j++)
                {
                    MutinyCharacter character = characters[j];
                    if (character == null || !character.IsAlive || character.PhysicsBody == null)
                        continue;
                    if (TryActivateForCharacter(character, IsCharacterSelfThrowAiming(character)))
                        return;
                }
            }
        }

        /// <summary>
        /// TileSystem.mouseDown assigns Controller.twanging before Controller advances
        /// stored mines. Notify the mines from that same production input transition
        /// so activation is not dependent on Unity component Update ordering.
        /// </summary>
        public static bool NotifyCharacterBeganSelfThrowAim(MutinyCharacter character)
        {
            bool activated = false;
            MutinyMine[] mines = FindObjectsByType<MutinyMine>(FindObjectsSortMode.None);
            for (int i = 0; i < mines.Length; i++)
            {
                MutinyMine mine = mines[i];
                if (mine != null && mine.m_IgnoreTicks <= 0)
                    activated |= mine.TryActivateForCharacter(character, true);
            }
            return activated;
        }

        private bool TryActivateForCharacter(MutinyCharacter character, bool isSelfThrowAiming)
        {
            if (!IsFired || m_Active || m_Exploded || character == null ||
                !character.IsAlive || character.PhysicsBody == null)
                return false;

            PhysicsBodyState target = character.PhysicsBody.State;
            bool stationary = target.VelocityX == 0f && Mathf.Abs(target.VelocityY) <= 0.2f;
            // Mine.as: an otherwise stationary character is ignored only when it
            // is not Controller.twanging. Pulling Throw Self therefore qualifies.
            if (stationary && !isSelfThrowAiming)
                return false;

            float dx = target.X - PhysicsBody.State.X;
            float dy = target.Y + target.BottomExtent - PhysicsBody.State.Y;
            if (dx * dx + dy * dy >= TriggerRadiusPixels * TriggerRadiusPixels)
                return false;

            m_Active = true;
            StartWarningAnimation();
            PlayInitialBeep();
            MutinyDebugLog.Info("Mine",
                $"warn triggered by={character.name} twanging={isSelfThrowAiming} velocity=({target.VelocityX:F2},{target.VelocityY:F2})",
                this);
            return true;
        }

        private static bool IsCharacterSelfThrowAiming(MutinyCharacter character)
        {
            MutinyPlayerInput[] inputs = FindObjectsByType<MutinyPlayerInput>(FindObjectsSortMode.None);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null && inputs[i].IsCharacterThrowDragInProgress(character))
                    return true;
            }
            return false;
        }

        private void PlayInitialBeep()
        {
            if (m_NextBeepIndex != 0)
                return;
            MutinyAudioManager.Instance?.PlaySFX("mine_beep");
            m_NextBeepIndex = 1;
        }

        private void StartArmAnimation()
        {
            m_PresentationState = PresentationState.Arming;
            SetAnimationFrame(ArmStartFrame);
        }

        private void StartWarningAnimation()
        {
            m_PresentationState = PresentationState.Warning;
            SetAnimationFrame(WarnStartFrame);
        }

        private void AdvancePresentationTick()
        {
            if (m_PresentationState == PresentationState.Arming)
            {
                if (CurrentAnimationFrame < ArmStopFrame)
                    SetAnimationFrame(CurrentAnimationFrame + 1);
                else
                    m_PresentationState = PresentationState.Armed;
            }
            else if (m_PresentationState == PresentationState.Warning)
            {
                int next = CurrentAnimationFrame >= WarnLastVisibleFrame
                    ? WarnStartFrame
                    : CurrentAnimationFrame + 1;
                SetAnimationFrame(next);
            }
        }

        private void SetAnimationFrame(int oneBasedFrame)
        {
            m_AnimationFrame = Mathf.Clamp(oneBasedFrame - 1, 0, Mathf.Max(0, m_Frames.Count - 1));
            if (SpriteRenderer != null && m_Frames.Count > 0)
                SpriteRenderer.sprite = m_Frames[m_AnimationFrame];
        }

        public void Explode()
        {
            if (m_Exploded || IsFinished) return;
            m_Exploded = true;
            Vector2 position = new Vector2(PhysicsBody.State.X, PhysicsBody.State.Y);
            MutinyExplosion.Spawn(position, 250f, 70f, Owner, playPopOnHit: false);
            Mutiny.Presentation.MutinyAudioManager.Instance?.PlaySFX("pop");
            Finish();
            if (SpriteRenderer != null) SpriteRenderer.enabled = false;
            Destroy(gameObject, 0.1f);
            MutinyDebugLog.Info("Mine", "exploded size=250 damage=70", this);
        }

        private void OnDestroy()
        {
            if (PhysicsBody != null) PhysicsBody.OnSimulationStep -= AdvanceOriginalTick;
        }
    }
}
