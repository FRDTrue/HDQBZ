using System;
using System.Collections.Generic;
using Mutiny.Diagnostics;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MutinyCharacterAnimator : MonoBehaviour
    {
        private const int IdleFirstFrame = 1;
        // Frame 13 executes gotoAndPlay("static") in the SWF and frame 14 is the
        // transparent separator before the hit label. Neither frame is rendered.
        private const int IdleLastFrame = 12;
        private const int HitFirstFrame = 15;
        // Frame 35 executes gotoAndPlay("static") before it can be presented.
        private const int HitLastFrame = 34;

        private static readonly Dictionary<string, Sprite[]> s_FrameCache =
            new Dictionary<string, Sprite[]>(StringComparer.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_FrameCache.Clear();
        }

        private SpriteRenderer m_Renderer;
        private MutinyCharacter m_Character;
        private Sprite[] m_Frames;
        private int m_Frame = IdleFirstFrame;
        private bool m_HitRequested;
        private bool m_PlayingHitRecovery;
        private int m_HitHoldTicks;
        private float m_TickAccumulator;

        internal int CurrentFrame => m_Frame;
        internal bool IsInitialized => m_Frames != null && m_Frames.Length > 0;

        private void Awake()
        {
            m_Renderer = GetComponent<SpriteRenderer>();
            m_Character = GetComponent<MutinyCharacter>();
        }

        private void Start()
        {
            // Runtime-built characters are initialized explicitly by the builder.
            // Baked scene characters already have this component, so initialize them
            // from their serialized CharacterType when their first lifecycle starts.
            EnsureInitialized();
        }

        internal bool EnsureInitialized()
        {
            if (IsInitialized)
                return true;

            if (m_Character == null)
                m_Character = GetComponent<MutinyCharacter>();
            if (m_Character == null || string.IsNullOrEmpty(m_Character.CharacterType))
            {
                MutinyDebugLog.Warning("Animation",
                    $"character timeline initialization skipped name={name} reason=missing-character-type", this);
                return false;
            }

            Initialize(m_Character.CharacterType);
            return IsInitialized;
        }

        public void Initialize(string characterType)
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<SpriteRenderer>();
            if (m_Character == null)
                m_Character = GetComponent<MutinyCharacter>();

            m_Frames = LoadFrames(characterType);
            m_Frame = IdleFirstFrame;
            m_HitRequested = false;
            m_PlayingHitRecovery = false;
            m_HitHoldTicks = 0;
            m_TickAccumulator = 0f;
            ApplyFrame();
            MutinyDebugLog.Info("Animation",
                $"character timeline initialized name={name} type={characterType} static=1-12 hit=15-34", this);
        }

        public void PlayHit()
        {
            m_HitRequested = true;
            m_PlayingHitRecovery = false;
            m_HitHoldTicks = 0;
            m_Frame = HitFirstFrame;
            ApplyFrame();
        }

        private void Update()
        {
            if (m_Character == null)
                m_Character = GetComponent<MutinyCharacter>();

            if (m_Frames == null || m_Frames.Length == 0)
            {
                if (!EnsureInitialized())
                    return;
            }

            if (m_Character != null && !m_Character.IsAlive)
                return;

            m_TickAccumulator += Time.deltaTime;
            while (m_TickAccumulator >= MutinyPhysics.TimeStep)
            {
                m_TickAccumulator -= MutinyPhysics.TimeStep;
                AdvanceOriginalTick();
            }
        }

        internal void AdvanceOriginalTick()
        {
            if (m_HitRequested)
            {
                m_HitHoldTicks++;
                bool moving = m_Character != null && m_Character.PhysicsBody != null && !m_Character.PhysicsBody.IsAtRest;
                if (moving && m_HitHoldTicks < 50)
                {
                    // Character.advance repeatedly jumps to the "hit" label while the
                    // body is moving, holding the first hit pose.
                    m_Frame = HitFirstFrame;
                }
                else
                {
                    m_HitRequested = false;
                    m_PlayingHitRecovery = true;
                    m_HitHoldTicks = 0;
                }
            }

            if (m_PlayingHitRecovery)
            {
                m_Frame++;
                if (m_Frame > HitLastFrame)
                {
                    m_PlayingHitRecovery = false;
                    m_Frame = IdleFirstFrame;
                }
            }
            else if (!m_HitRequested)
            {
                m_Frame++;
                if (m_Frame > IdleLastFrame)
                    m_Frame = IdleFirstFrame;
            }

            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<SpriteRenderer>();
            int index = m_Frame - 1;
            if (m_Renderer != null && m_Frames != null && index >= 0 && index < m_Frames.Length && m_Frames[index] != null)
                m_Renderer.sprite = m_Frames[index];
        }

        public static readonly Dictionary<string, Vector2> CharacterPivots =
            new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase)
        {
            { "redPirate", new Vector2(12f / 28f, 15f / 30f) },              // (0.4286, 0.5000)
            { "bluePirate", new Vector2(12f / 28f, 15f / 30f) },             // (0.4286, 0.5000)
            { "cabinBoy", new Vector2(12f / 28f, 15f / 30f) },               // (0.4286, 0.5000)
            { "skeletonPirate", new Vector2(12f / 28f, 15f / 30f) },         // (0.4286, 0.5000)
            { "rainbowBeard", new Vector2(12f / 27f, 15f / 30f) },           // (0.4444, 0.5000)
            { "femalePirate", new Vector2(13f / 29f, 15f / 30f) },           // (0.4483, 0.5000)
            { "blindPirate", new Vector2(13f / 29f, 15f / 30f) },            // (0.4483, 0.5000)
            { "soldier", new Vector2(12f / 24f, 15f / 46f) },                // (0.5000, 0.3261) -> fixes soldier clipping into floor
            { "bossGuy", new Vector2(21f / 40f, 15f / 62f) },                // (0.5250, 0.2419) -> fixes bossGuy clipping into floor
            { "bossGuyZombie", new Vector2(21f / 40f, 15f / 62f) },          // (0.5250, 0.2419)
            { "soldierCaptain", new Vector2(17f / 34f, 15f / 36f) },         // (0.5000, 0.4167)
            { "blindPirateCaptain", new Vector2(14f / 28f, 15f / 34f) },     // (0.5000, 0.4412)
            { "femalePirateCaptain", new Vector2(15f / 31f, 15f / 32f) },    // (0.4839, 0.4688)
            { "rainbowBeardCaptain", new Vector2(18f / 36f, 15f / 39f) },    // (0.5000, 0.3846)
            { "oldPirateCaptain", new Vector2(17f / 36f, 15f / 34f) },       // (0.4722, 0.4412)
            { "cabinBoyCaptain", new Vector2(12f / 24f, 15f / 33f) },        // (0.5000, 0.4545)
            { "tribeChief", new Vector2(14f / 28f, 15f / 39f) },             // (0.5000, 0.3846)
            { "skeletonPirateCaptain", new Vector2(14f / 30f, 15f / 35f) },  // (0.4667, 0.4286)
            { "squid", new Vector2(12f / 24f, 15f / 39f) },                  // (0.5000, 0.3846) -> fixes squid clipping into floor
            { "bluePirateCaptain", new Vector2(14f / 29f, 15f / 35f) },      // (0.4828, 0.4286)
            { "redPirateCaptain", new Vector2(14f / 29f, 15f / 35f) },       // (0.4828, 0.4286)
            { "oldPirate", new Vector2(18f / 36f, 15f / 30f) },              // (0.5000, 0.5000)
            { "tribe", new Vector2(12f / 24f, 15f / 30f) },                  // (0.5000, 0.5000)
            { "monkey", new Vector2(14f / 29f, 15f / 30f) },                 // (0.4828, 0.5000)
            { "crab", new Vector2(20f / 40f, 15f / 30f) },                   // (0.5000, 0.5000)
            { "shark", new Vector2(16f / 32f, 15f / 30f) },                  // (0.5000, 0.5000)
            { "parrot", new Vector2(12f / 24f, 15f / 30f) }                  // (0.5000, 0.5000)
        };

        public static Vector2 GetCharacterPivot(string characterType)
        {
            if (!string.IsNullOrEmpty(characterType) && CharacterPivots.TryGetValue(characterType, out Vector2 pivot))
                return pivot;
            return new Vector2(0.5f, 0.5f);
        }

        public const int OriginalCharacterFrameCount = 35;

        public static Sprite[] LoadFrames(string characterType)
        {
            if (string.IsNullOrEmpty(characterType))
                return Array.Empty<Sprite>();
            if (s_FrameCache.TryGetValue(characterType, out Sprite[] cached) && cached != null && cached.Length > 0)
            {
                if (cached[0] != null)
                    return cached;
                s_FrameCache.Remove(characterType);
            }

            Vector2 pivot = GetCharacterPivot(characterType);
            var frameList = new List<Sprite>(OriginalCharacterFrameCount);
            for (int frame = 1; frame <= OriginalCharacterFrameCount; frame++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Characters/Animations/{characterType}/{frame}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        pivot,
                        MutinyPhysics.PixelsPerUnit);
                    sprite.name = $"{characterType}_{frame:D2}";
                    frameList.Add(sprite);
                }
                else
                {
                    Sprite sprite = Resources.Load<Sprite>($"Art/Characters/Animations/{characterType}/{frame}");
                    if (sprite != null)
                    {
                        frameList.Add(sprite);
                    }
                }
            }

            Sprite[] frames = frameList.ToArray();
            if (frames.Length > 0)
            {
                s_FrameCache[characterType] = frames;
            }
            return frames;
        }

        private static int ParseFrame(string name)
        {
            return int.TryParse(name, out int frame) ? frame : int.MaxValue;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MutinyDeadCharacterEffect : MonoBehaviour
    {
        public const int OriginalFrameCount = 24;
        private static readonly Vector2 OriginalPivot = new Vector2(12f / 26f, 4f / 22f);
        private static Sprite[] s_Frames;

        private SpriteRenderer m_Renderer;
        private int m_FrameIndex;
        private float m_TickAccumulator;

        public int CurrentFrame => m_FrameIndex + 1;
        public int FrameCount => s_Frames == null ? 0 : s_Frames.Length;
        public bool IsComplete => FrameCount > 0 && m_FrameIndex >= FrameCount - 1;

        private void Awake()
        {
            m_Renderer = GetComponent<SpriteRenderer>();
        }

        public static MutinyDeadCharacterEffect Spawn(MutinyCharacter character)
        {
            if (character == null)
                return null;

            PhysicsBodyState state = character.PhysicsBody != null
                ? character.PhysicsBody.State
                : default;
            float bottomExtent = character.PhysicsBody != null
                ? character.PhysicsBody.State.BottomExtent
                : 8f;

            GameObject corpse = new GameObject($"DeadCharacter_{character.name}");
            if (character.transform.parent != null)
                corpse.transform.SetParent(character.transform.parent, true);
            corpse.transform.position = MutinyPhysics.PixelToUnity(state.X, state.Y + bottomExtent);
            corpse.transform.rotation = Quaternion.identity;

            MutinyDeadCharacterEffect effect = corpse.AddComponent<MutinyDeadCharacterEffect>();
            int sortingOrder = character.GetComponent<SpriteRenderer>()?.sortingOrder ?? 20;
            effect.Initialize(sortingOrder);
            return effect;
        }

        public void Initialize(int sortingOrder)
        {
            EnsureFramesLoaded();
            m_FrameIndex = 0;
            m_TickAccumulator = 0f;
            if (FrameCount != OriginalFrameCount)
            {
                Debug.LogError(
                    $"[Mutiny:Death] Expected {OriginalFrameCount} deadCharacter frames but loaded {FrameCount}.", this);
            }
            if (m_Renderer != null)
            {
                m_Renderer.sortingOrder = sortingOrder;
                ApplyFrame();
            }
        }

        private void Update()
        {
            if (IsComplete)
                return;

            m_TickAccumulator += Time.deltaTime;
            while (m_TickAccumulator >= MutinyPhysics.TimeStep && !IsComplete)
            {
                m_TickAccumulator -= MutinyPhysics.TimeStep;
                AdvanceOriginalTick();
            }
        }

        public void AdvanceOriginalTick()
        {
            if (s_Frames == null || s_Frames.Length == 0 || IsComplete)
                return;

            m_FrameIndex++;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (m_Renderer != null && s_Frames != null &&
                m_FrameIndex >= 0 && m_FrameIndex < s_Frames.Length)
            {
                m_Renderer.sprite = s_Frames[m_FrameIndex];
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Frames = null;
        }

        private static void EnsureFramesLoaded()
        {
            if (s_Frames != null && s_Frames.Length > 0 && s_Frames[0] != null)
                return;

            var frameList = new List<Sprite>(OriginalFrameCount);
            for (int frame = 1; frame <= OriginalFrameCount; frame++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Characters/DeadCharacter/{frame}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        OriginalPivot,
                        MutinyPhysics.PixelsPerUnit);
                    sprite.name = $"deadCharacter_{frame:D2}";
                    frameList.Add(sprite);
                }
                else
                {
                    Sprite sprite = Resources.Load<Sprite>($"Art/Characters/DeadCharacter/{frame}");
                    if (sprite != null)
                        frameList.Add(sprite);
                }
            }
            s_Frames = frameList.ToArray();
        }

        private static int ParseFrame(string name)
        {
            return int.TryParse(name, out int frame) ? frame : int.MaxValue;
        }
    }
}
