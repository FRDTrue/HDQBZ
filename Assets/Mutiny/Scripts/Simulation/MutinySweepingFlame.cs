using System.Collections.Generic;
using Mutiny.Diagnostics;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinySweepingFlame : MonoBehaviour
    {
        public const int OriginalFrameCount = 11;
        public const int PropagationFrame = 4;
        public const float PropagationPixels = 8f;
        public const float CharacterHitDistanceSquared = 64f;
        public const float Damage = 30f;

        private readonly List<Sprite> m_Frames = new List<Sprite>(OriginalFrameCount);
        private SpriteRenderer m_Renderer;
        private string[,] m_Terrain;
        private int m_Width;
        private int m_Height;
        private bool m_ToRight;
        private int m_CurrentFrame;
        private float m_Accumulator;
        private bool m_CreatedNext;

        public int CurrentFrame => m_CurrentFrame + 1;
        public bool CreatedNext => m_CreatedNext;

        public static MutinySweepingFlame Spawn(
            Vector2 pixelPosition, bool toRight, string[,] terrain, int width, int height)
        {
            GameObject flameObject = new GameObject(toRight ? "SweepingFlameRight" : "SweepingFlameLeft");
            flameObject.transform.position = MutinyPhysics.PixelToUnity(pixelPosition.x, pixelPosition.y);
            MutinySweepingFlame flame = flameObject.AddComponent<MutinySweepingFlame>();
            flame.Initialize(pixelPosition, toRight, terrain, width, height);
            return flame;
        }

        public static readonly Vector2 FlamePivot = new Vector2(9f / 19f, 1f / 31f); // Symbol 905: origin (9, 30) of 19x31

        private void Awake()
        {
            m_Renderer = gameObject.AddComponent<SpriteRenderer>();
            m_Renderer.sortingOrder = MutinyWeapon.WeaponSortingOrder + 3;
            for (int frame = 1; frame <= OriginalFrameCount; frame++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Art/Effects/SweepingFlame/{frame}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        FlamePivot, MutinyPhysics.PixelsPerUnit);
                    m_Frames.Add(sprite);
                }
                else
                {
                    Sprite sprite = Resources.Load<Sprite>($"Art/Effects/SweepingFlame/{frame}");
                    if (sprite != null)
                        m_Frames.Add(sprite);
                }
            }

            if (m_Frames.Count > 0)
                m_Renderer.sprite = m_Frames[0];
        }

        private void Initialize(Vector2 pixelPosition, bool toRight, string[,] terrain, int width, int height)
        {
            m_ToRight = toRight;
            m_Terrain = terrain;
            m_Width = width;
            m_Height = height;
            ApplyOriginalSpawnHit(pixelPosition);
            MutinyDebugLog.Info("RumBottle",
                $"flame spawned direction={(toRight ? "right" : "left")} pos=({pixelPosition.x:F1},{pixelPosition.y:F1})", this);
        }

        private void Update()
        {
            m_Accumulator += Time.deltaTime;
            while (m_Accumulator >= MutinyPhysics.TimeStep)
            {
                m_Accumulator -= MutinyPhysics.TimeStep;
                AdvanceOriginalTick();
                if (this == null)
                    return;
            }
        }

        public void AdvanceOriginalTickForVerification()
        {
            AdvanceOriginalTick();
        }

        private void AdvanceOriginalTick()
        {
            m_CurrentFrame++;
            if (m_CurrentFrame == PropagationFrame - 1)
                CreateNext();

            if (m_CurrentFrame >= OriginalFrameCount)
            {
                Destroy(gameObject);
                return;
            }

            if (m_Frames.Count > 0 && m_Renderer != null)
                m_Renderer.sprite = m_Frames[m_CurrentFrame % m_Frames.Count];
        }

        private void CreateNext()
        {
            if (m_CreatedNext)
                return;
            m_CreatedNext = true;

            Vector2 nextPosition = MutinyPhysics.UnityToPixel(transform.position);
            nextPosition.x += m_ToRight ? PropagationPixels : -PropagationPixels;
            int column = Mathf.FloorToInt(nextPosition.x / 32f);
            int row = Mathf.FloorToInt(nextPosition.y / 32f);
            if (!IsSolidTile(m_Terrain, m_Width, m_Height, column, row) ||
                IsSolidTile(m_Terrain, m_Width, m_Height, column, row - 1))
                return;

            Spawn(nextPosition, m_ToRight, m_Terrain, m_Width, m_Height);
            FindAnyObjectByType<MutinyTurnManager>()?.NotifyAmbientActivity();
            MutinyDebugLog.Info("RumBottle",
                $"flame propagated direction={(m_ToRight ? "right" : "left")} pos=({nextPosition.x:F1},{nextPosition.y:F1})", this);
        }

        private static void ApplyOriginalSpawnHit(Vector2 flamePosition)
        {
            MutinyCharacter[] characters = FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                MutinyCharacter character = characters[i];
                if (character == null || !character.IsAlive || character.PhysicsBody == null)
                    continue;

                PhysicsBodyState state = character.PhysicsBody.State;
                float dx = state.X - flamePosition.x;
                float dy = state.Y + state.BottomExtent - flamePosition.y;
                if (dx * dx + dy * dy >= CharacterHitDistanceSquared)
                    continue;

                state.VelocityX = MutinyMatchRandom.NextValue() * 8f - 4f;
                state.VelocityY = -(MutinyMatchRandom.NextValue() * 2f + 6f);
                character.PhysicsBody.State = state;
                character.TakeDamage(Damage);
                MutinyDebugLog.Info("RumBottle",
                    $"flame hit character={character.name} damage={Damage:F0} velocity=({state.VelocityX:F2},{state.VelocityY:F2})", character);
            }
        }

        public static bool IsSolidTile(string[,] terrain, int width, int height, int column, int row)
        {
            if (terrain == null || column < 0 || column >= width || row < 0 || row >= height)
                return false;

            string tile = terrain[row, column];
            return !string.IsNullOrEmpty(tile) && tile != "-" &&
                   tile.IndexOf("ripple", System.StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
