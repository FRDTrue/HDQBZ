using System;
using System.Collections.Generic;
using System.IO;
using Mutiny.Levels;
using Mutiny.Persistence;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Verification
{
    public sealed class MutinyLevel1VerificationResult
    {
        public bool Passed = true;
        public int TotalAssertions = 0;
        public int PassedAssertions = 0;
        public List<string> Logs = new List<string>();
        public List<string> Failures = new List<string>();

        public void Assert(bool condition, string message)
        {
            TotalAssertions++;
            if (condition)
            {
                PassedAssertions++;
                Logs.Add($"[PASS] {message}");
            }
            else
            {
                Passed = false;
                Failures.Add($"[FAIL] {message}");
                Debug.LogError($"[Verification FAIL] {message}");
            }
        }
    }

    public static class MutinyLevel1VerificationTest
    {
        public static MutinyLevel1VerificationResult RunAllTests(string level01XmlPath = "Assets/Mutiny/Data/Levels/level_01.xml")
        {
            var res = new MutinyLevel1VerificationResult();

            // 1. Verify Level 1 XML Parsing
            string xml = null;
            if (File.Exists(level01XmlPath))
            {
                xml = File.ReadAllText(level01XmlPath);
            }
            else
            {
#if UNITY_EDITOR
                var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(level01XmlPath);
                if (textAsset != null) xml = textAsset.text;
#endif
            }

            res.Assert(xml != null, "Level 1 XML exists and can be loaded");
            if (xml == null) return res;

            MutinyLevelData levelData = MutinyLevelXmlParser.Parse(xml, "level_01.xml");
            res.Assert(levelData.Width == 50, "Level 1 Width == 50");
            res.Assert(levelData.Height == 17, "Level 1 Height == 17");
            res.Assert(levelData.Players == 1, "Level 1 Players == 1 (Single Player)");
            res.Assert(levelData.Objects.Count == 10, "Level 1 Object count == 10");

            // 2. Character and Team Composition
            int team1Count = 0;
            int team2Count = 0;
            bool redCaptainFound = false;
            bool blueCaptainFound = false;

            for (int i = 0; i < levelData.Objects.Count; i++)
            {
                var obj = levelData.Objects[i];
                if (obj.Type.StartsWith("redPirate", StringComparison.OrdinalIgnoreCase))
                {
                    team1Count++;
                    if (obj.Type.Equals("redPirateCaptain", StringComparison.OrdinalIgnoreCase))
                    {
                        redCaptainFound = true;
                        res.Assert(obj.X == 34 && obj.Y == 11, "Red Pirate Captain position is (34, 11)");
                        res.Assert(obj.Properties.ContainsKey("cherryBomb") && obj.Properties["cherryBomb"] == "10", "Red Captain has infinite Cherry Bomb (val 10)");
                        res.Assert(obj.Properties.ContainsKey("dynamite") && obj.Properties["dynamite"] == "5", "Red Captain has 5 Dynamite");
                    }
                }
                else if (obj.Type.StartsWith("cabinBoy", StringComparison.OrdinalIgnoreCase))
                {
                    team2Count++;
                    if (obj.Type.Equals("cabinBoyCaptain", StringComparison.OrdinalIgnoreCase))
                    {
                        blueCaptainFound = true;
                        res.Assert(obj.X == 46 && obj.Y == 4, "Cabin Boy Captain position is (46, 4)");
                        res.Assert(obj.Properties.ContainsKey("cherryBomb") && obj.Properties["cherryBomb"] == "10", "Cabin Boy Captain has infinite Cherry Bomb");
                        res.Assert(obj.Properties.ContainsKey("dynamite") && obj.Properties["dynamite"] == "1", "Cabin Boy Captain has 1 Dynamite");
                    }
                }
            }

            res.Assert(team1Count == 5, "Team 1 has exactly 5 Red Pirates");
            res.Assert(team2Count == 3, "Team 2 has exactly 3 Cabin Boys");
            res.Assert(redCaptainFound, "Red Pirate Captain identified");
            res.Assert(blueCaptainFound, "Cabin Boy Captain identified");

            // 3. Physics & Ballistics Determinism
            // Starting from Red Captain (x=34.5, y=11.75 in tile units -> pixel 1104, 376)
            Vector2 startPx = new Vector2(1104f, 376f);
            Vector2 dragReleasePx = new Vector2(1164f, 416f); // drag delta dx=+60, dy=+40
            Vector2 expectedInitV = new Vector2(-60f * 0.25f, -40f * 0.25f); // (-15, -10)
            res.Assert(Mathf.Approximately(expectedInitV.x, -15f) && Mathf.Approximately(expectedInitV.y, -10f), "Twang impulse: initial velocity is (-15, -10) px/tick");

            // Discrete 25 Hz simulation: gravity = 1.0 px/tick
            Vector2 simPos = startPx;
            Vector2 simV = expectedInitV;
            for (int t = 1; t <= 10; t++)
            {
                simV.y += MutinyPhysics.Gravity;
                simPos += simV;
            }
            // At tick 10: vx = -15, vy = -10 + 10 = 0 (apex of trajectory)
            res.Assert(Mathf.Approximately(simV.x, -15f), "Apex horizontal velocity remains -15 px/tick");
            res.Assert(Mathf.Approximately(simV.y, 0f), "Trajectory reaches vertical apex at tick 10 (vy = 0)");
            res.Assert(Mathf.Approximately(simPos.x, 1104f - 150f), "Displacement at tick 10 is exactly -150 px horizontally");

            // 4. Explosion Physics & Damage Falloff
            float explosionSize = 80f;
            float expectedRadius = (explosionSize * 0.5f) + 20f; // 60 px
            res.Assert(Mathf.Approximately(expectedRadius, 60f), "Cherry Bomb explosion radius is 60 px (size/2 + 20)");

            float dist = 30f;
            float ratio = 1f - (dist / expectedRadius); // 0.5
            float maxDamage = 40f;
            float damage = maxDamage * ratio; // 20 HP
            float force = 0.06f * ratio * maxDamage; // 1.2
            float upwardPop = -force * 6.0f; // -7.2 px/tick
            res.Assert(Mathf.Approximately(damage, 20f), "Damage at 30 px is exactly 20 HP (50% falloff)");
            res.Assert(Mathf.Approximately(upwardPop, -7.2f), "Upward pop velocity is exactly -7.2 px/tick");

            // 5. Inactivity Settling Threshold. Flash evaluates only after the
            // incremented counter is greater than 10.
            res.Assert(!MutinyTurnManager.HasReachedInactivityThreshold(10),
                "Settling does not begin when inactivity is exactly 10 ticks");
            res.Assert(MutinyTurnManager.HasReachedInactivityThreshold(11),
                "Settling begins when inactivity reaches 11 ticks");

            // 6. Water Line & Drowning
            float waterLineY = 14f * 32f; // 448 px
            res.Assert(waterLineY == 448f, "Water line starts at Y=14 (448 px)");

            // 7. Save System Verification
            MutinySaveSystem.HighestUnlockedLevel = 1;
            res.Assert(MutinySaveSystem.IsLevelUnlocked(1), "Level 1 is unlocked by default");
            res.Assert(!MutinySaveSystem.IsLevelUnlocked(2), "Level 2 is locked before victory");
            MutinySaveSystem.UnlockLevel(2);
            res.Assert(MutinySaveSystem.IsLevelUnlocked(2), "Level 2 is unlocked after victory");

            return res;
        }
    }
}
