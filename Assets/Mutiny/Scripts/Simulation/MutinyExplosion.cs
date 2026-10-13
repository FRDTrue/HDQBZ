using System;
using System.Collections.Generic;
using Mutiny.Levels;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinyExplosion : MonoBehaviour
    {
        public const int ExplosionSortingOrder = 200;

        // Presentation consumes one complete hit batch, never individual loop
        // entries. This event does not change damage or the simulation clock.
        public static event Action<IReadOnlyList<MutinyCharacter>> KnockbackApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPresentationEvents() => KnockbackApplied = null;

        [Header("Explosion Properties (Flash units)")]
        public float PixelX;
        public float PixelY;
        public float Size = 80f;
        public float MaxDamage = 40f;
        public float Radius;
        public MutinyCharacter Caster;
        // Most original callers play their own sound at the action that creates an
        // explosion. RumBottle does so on contact, before Explosion.hit on frame 3.
        public bool PlayPopOnHit = true;

        [Header("Animation")]
        public Sprite[] AnimationFrames;
        public float FrameRate = 25f; // 25 Hz Nitrome standard

        private SpriteRenderer m_SpriteRenderer;
        private int m_CurrentFrame = 0;
        private float m_FrameTimer = 0f;
        private bool m_HitApplied = false;

        public static MutinyExplosion Spawn(
            Vector2 pixelPos,
            float size = 80f,
            float maxDamage = 40f,
            MutinyCharacter caster = null,
            bool playPopOnHit = true)
        {
            GameObject go = new GameObject("Explosion");
            Vector3 unityPos = MutinyPhysics.PixelToUnity(pixelPos.x, pixelPos.y);
            go.transform.position = unityPos;

            // Flash scale: size / 100
            float scale = size / 100f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var explosion = go.AddComponent<MutinyExplosion>();
            explosion.PixelX = pixelPos.x;
            explosion.PixelY = pixelPos.y;
            explosion.Size = size;
            explosion.MaxDamage = maxDamage;
            explosion.Radius = (size * 0.5f) + 20f; // Exact Flash formula: size/2 + 20
            explosion.Caster = caster;
            explosion.PlayPopOnHit = playPopOnHit;

            return explosion;
        }

        public static MutinyExplosion SpawnAtUnityPos(Vector3 unityPos, float size = 80f, float maxDamage = 40f, MutinyCharacter caster = null)
        {
            Vector2 px = MutinyPhysics.UnityToPixel(unityPos);
            return Spawn(px, size, maxDamage, caster);
        }

        private void Awake()
        {
            m_SpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            m_SpriteRenderer.sortingOrder = ExplosionSortingOrder;
            LoadSprites();
        }

        private void Start()
        {
            if (Radius <= 0f)
            {
                Radius = (Size * 0.5f) + 20f;
            }

            if (AnimationFrames != null && AnimationFrames.Length > 0)
            {
                m_SpriteRenderer.sprite = AnimationFrames[0];
            }
        }

        private void LoadSprites()
        {
            if (AnimationFrames != null && AnimationFrames.Length > 0)
                return;

            var frames = new List<Sprite>();
            for (int i = 1; i <= 8; i++)
            {
                Sprite sp = Resources.Load<Sprite>($"Art/Effects/Explosion/{i}");
                if (sp != null)
                {
                    frames.Add(sp);
                }
            }

            AnimationFrames = frames.ToArray();
        }

        private void Update()
        {
            m_FrameTimer += Time.deltaTime;
            float frameDuration = 1f / FrameRate;

            while (m_FrameTimer >= frameDuration)
            {
                m_FrameTimer -= frameDuration;
                m_CurrentFrame++;

                // Flash AS2 pcode: cl.hit() is called on frame 3 (0-indexed frame 2)
                if (m_CurrentFrame == 2 && !m_HitApplied)
                {
                    ApplyHit();
                    m_HitApplied = true;
                }

                if (AnimationFrames != null && m_CurrentFrame < AnimationFrames.Length)
                {
                    m_SpriteRenderer.sprite = AnimationFrames[m_CurrentFrame];
                }
                else if (m_CurrentFrame >= 8)
                {
                    // Flash AS2: frame 8 calls stop(); cl.destroy();
                    if (!m_HitApplied)
                    {
                        ApplyHit();
                        m_HitApplied = true;
                    }
                    Destroy(gameObject);
                    return;
                }
            }
        }

        public void ApplyHit()
        {
            var audioMgr = FindAnyObjectByType<Mutiny.Presentation.MutinyAudioManager>();
            if (PlayPopOnHit && audioMgr != null)
            {
                audioMgr.PlaySFX("pop");
            }

            // 1. Damage and launch characters in radius (Flash AS2 Explosion.hit)
            var knockedCharacters = KnockbackApplied != null ? new List<MutinyCharacter>() : null;
            var characters = FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                var ch = characters[i];
                if (!ch.IsAlive || ch.PhysicsBody == null)
                    continue;

                float charX = ch.PhysicsBody.State.X;
                float charY = ch.PhysicsBody.State.Y;

                float dx = charX - PixelX;
                float dy = charY - PixelY;
                float distSq = dx * dx + dy * dy;

                if (distSq <= Radius * Radius)
                {
                    float dist = Mathf.Sqrt(distSq);
                    float normX;
                    float normY;

                    if (dist < 0.0001f)
                    {
                        normX = 0f;
                        normY = -1f; // Flash Y negative is upwards
                        dist = 1f;
                    }
                    else
                    {
                        normX = dx / dist;
                        normY = dy / dist;
                    }

                    // Flash: ratio = 1 - dist / radius
                    float ratio = 1.0f - (dist / Radius);
                    // Flash: force = 0.06 * ratio * maxDamage
                    float force = 0.06f * ratio * MaxDamage;

                    // Flash AS2 exact impulse formula:
                    // velocityX += normX * 5 * force;
                    // velocityY += normY * 5 * force - force * 6; (subtracting is upward pop in Flash)
                    ch.PhysicsBody.State.VelocityX += normX * 5.0f * force;
                    ch.PhysicsBody.State.VelocityY += (normY * 5.0f * force) - (force * 6.0f);

                    if (force > 0f && !ch.PhysicsBody.IsAtRest)
                        knockedCharacters?.Add(ch);

                    // Flash: subtractHealth(maxDamage * ratio)
                    ch.TakeDamage(MaxDamage * ratio);
                    if (Caster != null)
                        Caster.Evilness += ratio;
                }
            }

            if (knockedCharacters != null && knockedCharacters.Count > 0)
                KnockbackApplied?.Invoke(knockedCharacters);

            // Flash Explosion.hit checks every Controller.boxes entry against the
            // nearest point on its asymmetric AABB. Wooden crates remove themselves
            // from that registry when their `explode` timeline starts.
            List<PhysicsBoxObstacle> boxes = MutinyBoxRegistry.GetObstacles();
            for (int i = 0; i < boxes.Count; i++)
            {
                MutinyPhysicsBody boxBody = boxes[i].Body;
                if (boxBody == null)
                    continue;

                PhysicsBodyState box = boxes[i].State;
                float nearestX = Mathf.Clamp(PixelX, box.X - box.LeftExtent, box.X + box.RightExtent);
                float nearestY = Mathf.Clamp(PixelY, box.Y - box.TopExtent, box.Y + box.BottomExtent);
                float dx = PixelX - nearestX;
                float dy = PixelY - nearestY;
                if (dx * dx + dy * dy > Radius * Radius)
                    continue;

                MutinyWoodenCrate crate = boxBody.GetComponent<MutinyWoodenCrate>();
                if (crate != null)
                    crate.Explode();
                else
                {
                    MutinyGunpowderBarrel barrel = boxBody.GetComponent<MutinyGunpowderBarrel>();
                    if (barrel != null)
                        barrel.Explode();
                }
            }
        }
    }
}
