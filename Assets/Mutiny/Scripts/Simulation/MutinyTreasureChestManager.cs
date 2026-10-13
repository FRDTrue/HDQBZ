using System;
using System.Collections.Generic;
using Mutiny.Diagnostics;
using Mutiny.Levels;
using UnityEngine;

namespace Mutiny.Simulation
{
    [DisallowMultipleComponent]
    public sealed class MutinyTreasureChestManager : MonoBehaviour
    {
        // AIR-01: restored after the temporary AIRDBG disable.  Keep this property as
        // the single production gate so camera/HUD code observes the same state.
        public static bool SystemEnabled => true;
        public const int MaximumChestCount = 3;

        private readonly List<string> m_PotentialWeapons = new List<string>();
        private readonly List<int> m_ValidDropColumns = new List<int>();
        private readonly List<MutinyTreasureChest> m_Chests = new List<MutinyTreasureChest>();
        private MutinyLevelData m_LevelData;
        private MutinyLevelRoot m_LevelRoot;
        private float m_TickAccumulator;

        public IReadOnlyList<MutinyTreasureChest> Chests => m_Chests;
        public IReadOnlyList<string> PotentialWeapons => m_PotentialWeapons;
        public IReadOnlyList<int> ValidDropColumns => m_ValidDropColumns;
        public MutinyLevelRoot LevelRoot => m_LevelRoot;
        public float SimulationInterpolationAlpha =>
            Mathf.Clamp01(m_TickAccumulator / MutinyPhysics.TimeStep);

        public void Initialize(MutinyLevelData levelData, MutinyLevelRoot levelRoot)
        {
            m_LevelData = levelData;
            m_LevelRoot = levelRoot;
            BuildPotentialWeaponList();
            BuildValidDropColumns();
            MutinyDebugLog.Info("Chest",
                $"airdrop initialized level={m_LevelData?.Name ?? "unknown"} validColumns={m_ValidDropColumns.Count} weightedWeapons={m_PotentialWeapons.Count} max={MaximumChestCount}", this);
        }

        private void Update()
        {
            AdvanceSimulationFrame(Time.deltaTime);
        }

        internal void AdvanceSimulationFrameForVerification(float deltaTime) =>
            AdvanceSimulationFrame(deltaTime);

        private void AdvanceSimulationFrame(float deltaTime)
        {
            m_TickAccumulator += deltaTime;
            while (m_TickAccumulator >= MutinyPhysics.TimeStep)
            {
                m_TickAccumulator -= MutinyPhysics.TimeStep;
                for (int i = m_Chests.Count - 1; i >= 0; i--)
                {
                    if (m_Chests[i] == null)
                        m_Chests.RemoveAt(i);
                    else
                        m_Chests[i].AdvanceOriginalTick();
                }
            }
        }

        public bool TryDropNew()
        {
            if (!SystemEnabled || m_LevelData == null || m_LevelRoot == null ||
                m_Chests.Count >= MaximumChestCount ||
                m_ValidDropColumns.Count == 0 || m_PotentialWeapons.Count == 0)
            {
                MutinyDebugLog.Info("Chest",
                    $"drop skipped ready={m_LevelData != null && m_LevelRoot != null} active={m_Chests.Count}/{MaximumChestCount} validColumns={m_ValidDropColumns.Count} weightedWeapons={m_PotentialWeapons.Count}", this);
                return false;
            }

            int column = m_ValidDropColumns[MutinyMatchRandom.NextInt(0, m_ValidDropColumns.Count)];
            int floorRow = FindFirstSolidRow(column);
            if (floorRow < 0)
            {
                MutinyDebugLog.Info("Chest", $"drop attempt rejected column={column} reason=no-solid-floor", this);
                return false;
            }

            float x = column * 32f + 16f;
            float floorY = floorRow * 32f;
            if (!IsDropPositionClear(x, floorY, out string blockedBy))
            {
                // The Flash game samples exactly one valid column per turn and gives
                // up when it is occupied.  Do not retry another column here.
                MutinyDebugLog.Info("Chest",
                    $"drop attempt rejected column={column} x={x:0} floorY={floorY:0} reason={blockedBy}", this);
                return false;
            }

            int contentCount = 1 + MutinyMatchRandom.NextInt(0, 3);
            var contents = new List<string>(contentCount);
            for (int i = 0; i < contentCount; i++)
                contents.Add(m_PotentialWeapons[MutinyMatchRandom.NextInt(0, m_PotentialWeapons.Count)]);

            GameObject chestObject = new GameObject($"TreasureChest_{m_Chests.Count:D2}");
            chestObject.transform.SetParent(m_LevelRoot.ObjectsHolder, false);
            MutinyTreasureChest chest = chestObject.AddComponent<MutinyTreasureChest>();
            chest.Initialize(this, x, floorY, contents);
            m_Chests.Add(chest);
            MutinyDebugLog.Info("Chest",
                $"dropped name={chestObject.name} column={column} floorY={floorY:0} contents={string.Join(",", contents)}", chest);
            return true;
        }

        public void Unregister(MutinyTreasureChest chest)
        {
            m_Chests.Remove(chest);
        }

        private void BuildPotentialWeaponList()
        {
            m_PotentialWeapons.Clear();
            for (int i = 0; i < m_LevelData.Objects.Count; i++)
            {
                MutinyLevelObject obj = m_LevelData.Objects[i];
                if (!string.Equals(obj.Type, "potentialWeapons", StringComparison.Ordinal))
                    continue;

                foreach (KeyValuePair<string, string> property in obj.Properties)
                {
                    if (property.Key == "x" || property.Key == "y" || property.Key == "type" ||
                        property.Key == "luck" || property.Key == "maxChests")
                        continue;

                    if (!int.TryParse(property.Value, out int weight) || weight <= 0)
                        continue;
                    for (int n = 0; n < weight; n++)
                        m_PotentialWeapons.Add(property.Key);
                }
                break;
            }
        }

        private void BuildValidDropColumns()
        {
            m_ValidDropColumns.Clear();
            for (int x = 0; x < m_LevelData.Width; x++)
            {
                for (int y = 0; y < m_LevelData.Height; y++)
                {
                    if (string.Equals(m_LevelData.Background[y, x], "antichest", StringComparison.Ordinal))
                        break;
                    if (IsSolid(m_LevelData.Terrain[y, x]))
                    {
                        m_ValidDropColumns.Add(x);
                        break;
                    }
                }
            }
        }

        private int FindFirstSolidRow(int column)
        {
            for (int y = 0; y < m_LevelData.Height; y++)
            {
                if (IsSolid(m_LevelData.Terrain[y, column]))
                    return y;
            }
            return -1;
        }

        private bool IsDropPositionClear(float x, float floorY, out string blockedBy)
        {
            for (int i = 0; i < m_LevelRoot.Characters.Count; i++)
            {
                MutinyCharacter character = m_LevelRoot.Characters[i];
                if (character == null || !character.IsAlive || character.PhysicsBody == null)
                    continue;
                PhysicsBodyState state = character.PhysicsBody.State;
                if (state.X >= x - 32f && state.X <= x + 32f && state.Y <= floorY + 32f)
                {
                    blockedBy = $"character:{character.name}";
                    return false;
                }
            }

            for (int i = 0; i < m_Chests.Count; i++)
            {
                if (m_Chests[i] != null && Mathf.Approximately(m_Chests[i].PixelX, x))
                {
                    blockedBy = $"chest:{m_Chests[i].name}";
                    return false;
                }
            }

            List<PhysicsBoxObstacle> boxes = MutinyBoxRegistry.GetObstacles();
            for (int i = 0; i < boxes.Count; i++)
            {
                if (Mathf.Abs(boxes[i].State.X - x) < 32f)
                {
                    blockedBy = $"box:{boxes[i].Body.name}";
                    return false;
                }
            }
            blockedBy = null;
            return true;
        }

        private static bool IsSolid(string tile)
        {
            return !string.IsNullOrEmpty(tile) && tile != "-" &&
                   tile.IndexOf("ripple", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
