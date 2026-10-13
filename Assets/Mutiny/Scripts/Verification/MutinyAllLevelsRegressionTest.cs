using System;
using System.Collections.Generic;
using System.IO;
using Mutiny.Levels;
using Mutiny.Persistence;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Verification
{
    public struct LevelRegressionRecord
    {
        public int LevelIndex;
        public string FileName;
        public int Width;
        public int Height;
        public int Players;
        public int ObjectCount;
        public int Team1Characters;
        public int Team2Characters;
        public bool HasTeam1Captain;
        public bool HasTeam2Captain;
        public bool HasWater;
        public bool AIPlayable;
        public bool Passed;
        public string ErrorMessage;
    }

    public static class MutinyAllLevelsRegressionTest
    {
        public static List<LevelRegressionRecord> RunAll18Levels(string levelsDir = "Assets/Mutiny/Data/Levels")
        {
            var records = new List<LevelRegressionRecord>();

            for (int lvl = 1; lvl <= 18; lvl++)
            {
                string padded = lvl.ToString("D2");
                string fileName = $"level_{padded}.xml";
                string fullPath = Path.Combine(levelsDir, fileName);

                var record = new LevelRegressionRecord
                {
                    LevelIndex = lvl,
                    FileName = fileName,
                    Passed = true
                };

                try
                {
                    string xml = null;
                    if (File.Exists(fullPath))
                    {
                        xml = File.ReadAllText(fullPath);
                    }
                    else
                    {
#if UNITY_EDITOR
                        var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
                        if (textAsset != null) xml = textAsset.text;
#endif
                    }

                    if (string.IsNullOrEmpty(xml))
                    {
                        record.Passed = false;
                        record.ErrorMessage = $"File not found at {fullPath}";
                        records.Add(record);
                        continue;
                    }

                    // 1. Parse XML
                    MutinyLevelData data = MutinyLevelXmlParser.Parse(xml, fileName);
                    record.Width = data.Width;
                    record.Height = data.Height;
                    record.Players = data.Players;
                    record.ObjectCount = data.Objects.Count;

                    // 2. Scan Objects
                    for (int i = 0; i < data.Objects.Count; i++)
                    {
                        var obj = data.Objects[i];
                        string t = obj.Type.Trim();

                        if (t.Equals("water", StringComparison.OrdinalIgnoreCase))
                        {
                            record.HasWater = true;
                        }
                        else if (t.Equals("potentialWeapons", StringComparison.OrdinalIgnoreCase))
                        {
                            // Weapon pool
                        }
                        else if (t.StartsWith("redPirate", StringComparison.OrdinalIgnoreCase))
                        {
                            record.Team1Characters++;
                            if (t.IndexOf("captain", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                record.HasTeam1Captain = true;
                            }
                        }
                        else
                        {
                            // Opponent team character
                            record.Team2Characters++;
                            if (t.IndexOf("captain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                t.Equals("bossGuy", StringComparison.OrdinalIgnoreCase) ||
                                t.Equals("bossGuyZombie", StringComparison.OrdinalIgnoreCase) ||
                                t.Equals("tribeChief", StringComparison.OrdinalIgnoreCase))
                            {
                                record.HasTeam2Captain = true;
                            }
                        }
                    }

                    // 3. AI and Playability checks
                    if (record.Team1Characters == 0)
                    {
                        record.Passed = false;
                        record.ErrorMessage = "No Team 1 characters found.";
                    }
                    else if (record.Team2Characters == 0)
                    {
                        record.Passed = false;
                        record.ErrorMessage = "No Team 2 characters found.";
                    }
                    else
                    {
                        record.AIPlayable = true;
                    }

                    // 4. Progression unlock check
                    MutinySaveSystem.UnlockLevel(lvl);
                    if (!MutinySaveSystem.IsLevelUnlocked(lvl))
                    {
                        record.Passed = false;
                        record.ErrorMessage = "Level failed to unlock in save system.";
                    }
                }
                catch (Exception ex)
                {
                    record.Passed = false;
                    record.ErrorMessage = ex.Message;
                }

                records.Add(record);
            }

            return records;
        }
    }
}

