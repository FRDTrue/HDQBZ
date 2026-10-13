using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutiny.Simulation
{
    [Serializable]
    public struct PhysicsBodyState
    {
        public float X; // Flash pixel coordinates
        public float Y;
        public float VelocityX;
        public float VelocityY;

        public float Weight; // default 1.0f
        public float Bounce; // default 0.2f
        public float Friction; // default 0.3f (2.0f for Character)

        public float LeftExtent; // default 6
        public float RightExtent; // default 6
        public float TopExtent; // default 8
        public float BottomExtent; // default 8

        public bool HitsTiles;
        public bool HitsBoxes;

        public bool IsAtRest => Mathf.Abs(VelocityX) < 0.001f && Mathf.Abs(VelocityY) < 0.2f;

        public static PhysicsBodyState CreateDefault(float x, float y)
        {
            return new PhysicsBodyState
            {
                X = x,
                Y = y,
                VelocityX = 0f,
                VelocityY = 0f,
                Weight = 1.0f,
                Bounce = 0.2f,
                Friction = 0.3f,
                LeftExtent = 6f,
                RightExtent = 6f,
                TopExtent = 8f,
                BottomExtent = 8f,
                HitsTiles = true,
                HitsBoxes = false
            };
        }
    }

    public readonly struct PhysicsBoxObstacle
    {
        public readonly MutinyPhysicsBody Body;
        public readonly PhysicsBodyState State;

        public PhysicsBoxObstacle(MutinyPhysicsBody body, PhysicsBodyState state)
        {
            Body = body;
            State = state;
        }
    }

    /// <summary>
    /// Level-scoped equivalent of the Flash Controller.boxes array. Every placed
    /// BoxWeapon subtype shares this collection, so characters and projectiles see
    /// crates and barrels as the same kind of Solid terrain obstacle.
    /// </summary>
    public static class MutinyBoxRegistry
    {
        private static readonly List<MutinyPhysicsBody> Bodies = new();

        public static int Count
        {
            get
            {
                PruneDestroyedBodies();
                return Bodies.Count;
            }
        }

        public static void Register(MutinyPhysicsBody body)
        {
            if (body != null && !Bodies.Contains(body))
                Bodies.Add(body);
        }

        public static void Unregister(MutinyPhysicsBody body)
        {
            if (body != null)
                Bodies.Remove(body);
        }

        public static List<PhysicsBoxObstacle> GetObstacles(MutinyPhysicsBody requester = null)
        {
            PruneDestroyedBodies();
            var obstacles = new List<PhysicsBoxObstacle>(Bodies.Count);
            for (int i = 0; i < Bodies.Count; i++)
            {
                MutinyPhysicsBody body = Bodies[i];
                if (body != requester)
                    obstacles.Add(new PhysicsBoxObstacle(body, body.State));
            }
            return obstacles;
        }

        /// <summary>
        /// Called synchronously at the level lifecycle boundary. Unity destroys the
        /// previous level at end-of-frame, so waiting for OnDestroy would allow its
        /// boxes to leak into the newly built level for one frame.
        /// </summary>
        public static void ResetForLevel()
        {
            Bodies.Clear();
        }

        /// <summary>
        /// Original changeLevel/endGame calls unloadLevel, which destroys every
        /// Controller.boxes member. This is a level-unload boundary, not the
        /// GameOver/speech boundary. Include pending BoxWeapon chain nodes as
        /// well as registered, placed obstacles.
        /// </summary>
        public static int ClearForLevelUnload()
        {
            var targets = new HashSet<GameObject>();
            AddBoxWeaponTargets<MutinyWoodenCrate>(targets);
            AddBoxWeaponTargets<MutinyGunpowderBarrel>(targets);

            for (int i = 0; i < Bodies.Count; i++)
            {
                MutinyPhysicsBody body = Bodies[i];
                if (body != null)
                    targets.Add(body.gameObject);
            }

            // Collision state must disappear synchronously. Object destruction is
            // end-of-frame in Play Mode, so disabling first also removes visuals,
            // updates and input eligibility before the next level is built.
            Bodies.Clear();
            foreach (GameObject target in targets)
            {
                if (target == null)
                    continue;
                target.SetActive(false);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(target);
                else
                    UnityEngine.Object.Destroy(target);
#else
                UnityEngine.Object.Destroy(target);
#endif
            }
            return targets.Count;
        }

        private static void AddBoxWeaponTargets<T>(HashSet<GameObject> targets)
            where T : Component
        {
            T[] instances = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] != null)
                    targets.Add(instances[i].gameObject);
            }
        }

        private static void PruneDestroyedBodies()
        {
            for (int i = Bodies.Count - 1; i >= 0; i--)
            {
                if (Bodies[i] == null)
                    Bodies.RemoveAt(i);
            }
        }
    }

    public struct StepResult
    {
        public bool HitFloor;
        public bool HitCeiling;
        public bool HitLeftWall;
        public bool HitRightWall;
        public bool IsAtRest;
    }

    public static class MutinyPhysics
    {
        public const float PixelsPerUnit = 32f;
        public const float TimeStep = 0.04f; // 25 Hz = 1/25 s
        public const float DefaultTwangMaxForce = 20.0f;
        public const float Gravity = 1.0f; // 1 px/tick default Flash gravity
        public const float DefaultGravity = 1.0f;

        public static Vector3 PixelToUnity(float px, float py)
        {
            return new Vector3(px / PixelsPerUnit, -py / PixelsPerUnit, 0f);
        }

        public static Vector2 UnityToPixel(Vector3 unityPos)
        {
            return new Vector2(unityPos.x * PixelsPerUnit, -unityPos.y * PixelsPerUnit);
        }

        public static Vector2 CalculateTwangVelocity(Vector2 startPosPx, Vector2 dragPosPx, float maxForce = DefaultTwangMaxForce)
        {
            float vx = (dragPosPx.x - startPosPx.x) * -0.25f;
            float vy = (dragPosPx.y - startPosPx.y) * -0.25f;

            float sqrLen = vx * vx + vy * vy;
            if (sqrLen > maxForce * maxForce)
            {
                float len = Mathf.Sqrt(sqrLen);
                vx *= maxForce / len;
                vy *= maxForce / len;
            }

            return new Vector2(vx, vy);
        }

        public static StepResult Step(
            ref PhysicsBodyState body,
            string[,] terrainGrid,
            int gridWidth,
            int gridHeight,
            IReadOnlyList<PhysicsBoxObstacle> boxes = null,
            MutinyPhysicsBody self = null)
        {
            var result = new StepResult();

            body.VelocityY += body.Weight;

            if (body.VelocityX == 0f && body.VelocityY == 0f)
            {
                result.IsAtRest = true;
                return result;
            }

            float startX = body.X;
            float startY = body.Y;
            float endX = body.X + body.VelocityX;
            float endY = body.Y + body.VelocityY;

            int stepX = body.VelocityX < 0f ? -1 : 1;
            int stepY = body.VelocityY < 0f ? -1 : 1;

            int minGridCol = (int)Mathf.Floor((startX - body.LeftExtent * stepX) / 32f);
            int maxGridCol = (int)Mathf.Floor((endX + body.RightExtent * stepX) / 32f);
            int minGridRow = (int)Mathf.Floor((startY - body.TopExtent * stepY) / 32f);
            int maxGridRow = (int)Mathf.Floor((endY + body.BottomExtent * stepY) / 32f);

            bool hitY = false;
            bool targetYFromBox = false;
            float targetY = body.Y;

            // 1. Vertical Movement & Collision
            if (body.VelocityY != 0f)
            {
                int leftCol = (int)Mathf.Floor((body.X - body.LeftExtent) / 32f);
                int rightCol = (int)Mathf.Floor((body.X + body.RightExtent) / 32f);

                if (body.HitsTiles && terrainGrid != null)
                {
                    int r = minGridRow + stepY;
                    while (r != maxGridRow + stepY)
                    {
                        for (int c = leftCol; c <= rightCol; c++)
                        {
                            if (IsSolidTile(terrainGrid, c, r, gridWidth, gridHeight))
                            {
                                hitY = true;
                                break;
                            }
                        }

                        if (hitY)
                        {
                            if (body.VelocityY > 0f)
                            {
                                targetY = (r * 32f) - body.BottomExtent - 0.1f;
                            }
                            else
                            {
                                targetY = (r * 32f) + body.TopExtent + 32.1f;
                            }
                            break;
                        }

                        r += stepY;
                    }
                }

                // Solid.advanceMotion scans Controller.boxes after terrain and
                // keeps the nearer vertical collision from either source.
                if (body.HitsBoxes && boxes != null)
                {
                    for (int i = 0; i < boxes.Count; i++)
                    {
                        PhysicsBoxObstacle obstacle = boxes[i];
                        if (obstacle.Body == null || obstacle.Body == self)
                            continue;
                        PhysicsBodyState box = obstacle.State;
                        if (box.X - box.LeftExtent > body.X + body.RightExtent ||
                            box.X + box.RightExtent < body.X - body.LeftExtent)
                            continue;

                        float candidateY;
                        if (body.VelocityY > 0f)
                        {
                            if (box.Y < body.Y || box.Y - box.TopExtent > body.Y + body.VelocityY + body.BottomExtent)
                                continue;
                            candidateY = box.Y - box.TopExtent - body.BottomExtent - 0.1f;
                            if (!hitY || targetY > candidateY)
                            {
                                targetY = candidateY;
                                hitY = true;
                                targetYFromBox = true;
                            }
                        }
                        else
                        {
                            if (box.Y > body.Y || box.Y + box.BottomExtent < body.Y + body.VelocityY - body.TopExtent)
                                continue;
                            candidateY = box.Y + box.BottomExtent + body.TopExtent + 0.1f;
                            if (!hitY || targetY < candidateY)
                            {
                                targetY = candidateY;
                                hitY = true;
                                targetYFromBox = true;
                            }
                        }
                    }
                }

                if (hitY)
                {
                    // A box candidate can point back through the body's starting
                    // position when an explosion or a corner contact has already
                    // left the two AABBs slightly overlapped. Flash applies that
                    // candidate directly; in Unity it can put the character into
                    // the supporting tile beneath a settled box. Keep the original
                    // axis start whenever the box correction would create a new
                    // terrain overlap. The contact/bounce still resolves this tick.
                    if (targetYFromBox && body.HitsTiles &&
                        WouldOverlapSolidTerrain(
                            body.X, targetY, body,
                            terrainGrid, gridWidth, gridHeight))
                    {
                        targetY = startY;
                    }

                    body.VelocityY *= -body.Bounce;
                    body.Y = targetY;

                    if (body.VelocityY < 0f) // Rebounded upwards off floor
                    {
                        // Ground friction
                        float absVx = Mathf.Abs(body.VelocityX);
                        float newAbsVx = Mathf.Max(0f, absVx - body.Friction);
                        body.VelocityX = newAbsVx * Mathf.Sign(body.VelocityX);

                        result.HitFloor = true;
                    }
                    else
                    {
                        result.HitCeiling = true;
                    }
                }
                else
                {
                    body.Y += body.VelocityY;
                }
            }

            // 2. Horizontal Movement & Collision
            bool hitX = false;
            bool targetXFromBox = false;
            float targetX = body.X;

            if (body.VelocityX != 0f)
            {
                int topRow = (int)Mathf.Floor((body.Y - body.TopExtent) / 32f);
                int bottomRow = (int)Mathf.Floor((body.Y + body.BottomExtent) / 32f);

                if (body.HitsTiles && terrainGrid != null)
                {
                    int c = minGridCol + stepX;
                    while (c != maxGridCol + stepX)
                    {
                        for (int r = topRow; r <= bottomRow; r++)
                        {
                            if (IsSolidTile(terrainGrid, c, r, gridWidth, gridHeight))
                            {
                                hitX = true;
                                break;
                            }
                        }

                        if (hitX)
                        {
                            if (body.VelocityX > 0f)
                            {
                                targetX = (c * 32f) - body.RightExtent - 0.1f;
                                result.HitRightWall = true;
                            }
                            else
                            {
                                targetX = (c * 32f) + body.LeftExtent + 32.1f;
                                result.HitLeftWall = true;
                            }
                            break;
                        }

                        c += stepX;
                    }
                }

                // Equivalent horizontal Controller.boxes scan from Solid.as.
                if (body.HitsBoxes && boxes != null)
                {
                    for (int i = 0; i < boxes.Count; i++)
                    {
                        PhysicsBoxObstacle obstacle = boxes[i];
                        if (obstacle.Body == null || obstacle.Body == self)
                            continue;
                        PhysicsBodyState box = obstacle.State;
                        if (box.Y - box.TopExtent > body.Y + body.BottomExtent ||
                            box.Y + box.BottomExtent < body.Y - body.TopExtent)
                            continue;

                        float candidateX;
                        if (body.VelocityX > 0f)
                        {
                            if (box.X < body.X || box.X - box.LeftExtent > body.X + body.VelocityX + body.RightExtent)
                                continue;
                            candidateX = box.X - box.LeftExtent - body.RightExtent - 0.1f;
                            if (!hitX || targetX > candidateX)
                            {
                                targetX = candidateX;
                                hitX = true;
                                targetXFromBox = true;
                                result.HitRightWall = true;
                            }
                        }
                        else
                        {
                            if (box.X > body.X || box.X + box.RightExtent < body.X + body.VelocityX - body.LeftExtent)
                                continue;
                            candidateX = box.X + box.RightExtent + body.LeftExtent + 0.1f;
                            if (!hitX || targetX < candidateX)
                            {
                                targetX = candidateX;
                                hitX = true;
                                targetXFromBox = true;
                                result.HitLeftWall = true;
                            }
                        }
                    }
                }

                if (hitX)
                {
                    if (targetXFromBox && body.HitsTiles &&
                        WouldOverlapSolidTerrain(
                            targetX, body.Y, body,
                            terrainGrid, gridWidth, gridHeight))
                    {
                        targetX = startX;
                    }

                    body.VelocityX *= -0.4f; // Fixed Flash wall bounce
                    body.X = targetX;
                }
                else
                {
                    body.X += body.VelocityX;
                }
            }

            result.IsAtRest = body.IsAtRest;
            return result;
        }

        private static bool IsSolidTile(string[,] grid, int c, int r, int w, int h)
        {
            if (c < 0 || c >= w || r < 0 || r >= h)
                return false;

            string tile = grid[r, c];
            if (string.IsNullOrEmpty(tile) || tile == "-")
                return false;

            // Water ripple tiles in row are water visuals, not solid ground
            if (tile.IndexOf("ripple", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        private static bool WouldOverlapSolidTerrain(
            float x,
            float y,
            PhysicsBodyState body,
            string[,] terrainGrid,
            int gridWidth,
            int gridHeight)
        {
            if (terrainGrid == null || gridWidth <= 0 || gridHeight <= 0)
                return false;

            // Treat an exact edge contact as clear. Collision corrections retain a
            // 0.1 px separation, while this smaller inset only avoids assigning an
            // AABB edge to the tile on the far side of that edge.
            const float edgeInset = 0.001f;
            int leftCol = (int)Mathf.Floor((x - body.LeftExtent + edgeInset) / 32f);
            int rightCol = (int)Mathf.Floor((x + body.RightExtent - edgeInset) / 32f);
            int topRow = (int)Mathf.Floor((y - body.TopExtent + edgeInset) / 32f);
            int bottomRow = (int)Mathf.Floor((y + body.BottomExtent - edgeInset) / 32f);

            for (int row = topRow; row <= bottomRow; row++)
            {
                for (int column = leftCol; column <= rightCol; column++)
                {
                    if (IsSolidTile(terrainGrid, column, row, gridWidth, gridHeight))
                        return true;
                }
            }

            return false;
        }

        public static List<Vector2> SimulateTrajectory(PhysicsBodyState initialState, string[,] terrainGrid, int gridWidth, int gridHeight, int maxSteps = 60)
        {
            var points = new List<Vector2>(maxSteps + 1);
            PhysicsBodyState simBody = initialState;
            points.Add(new Vector2(simBody.X, simBody.Y));

            for (int i = 0; i < maxSteps; i++)
            {
                StepResult res = Step(ref simBody, terrainGrid, gridWidth, gridHeight);
                points.Add(new Vector2(simBody.X, simBody.Y));

                if (res.IsAtRest)
                    break;
            }

            return points;
        }
    }

    public static class MutinyRotationRules
    {
        public static float CharacterMotionDelta(float velocityX)
        {
            return -velocityX * 3f;
        }

        public static float CharacterWaterDelta(float velocityX, float velocityY)
        {
            return -(velocityX + velocityY) * 4f;
        }

        public static float SettleCharacterFloorAngle(float unityAngle)
        {
            float angle = Mathf.DeltaAngle(0f, unityAngle) * 0.5f;
            if (angle > -1f && angle < 1f)
                angle = 0f;
            return angle;
        }

        public static float WeaponRotationMultiplier(string weaponType)
        {
            switch (weaponType)
            {
                case "banana":
                case "dynamite":
                case "rumBottle":
                case "voodooDoll":
                    return 2f;
                case "boulder":
                    return 2.5f;
                default:
                    return 0f;
            }
        }

        public static float WeaponMotionDelta(string weaponType, float velocityX)
        {
            return -velocityX * WeaponRotationMultiplier(weaponType);
        }
    }

    /// <summary>
    /// Keeps the authoritative Flash rotation on the 25 Hz simulation timeline
    /// while allowing Unity to present the last tick transition smoothly. Sampling
    /// never feeds back into physics or adds another angular integration step.
    /// </summary>
    public sealed class MutinyRotationState
    {
        private long m_LastPreparedTick = long.MinValue;

        public float PreviousAngle { get; private set; }
        public float LogicalAngle { get; private set; }

        public void Reset(float angle)
        {
            LogicalAngle = Normalize(angle);
            PreviousAngle = LogicalAngle;
            m_LastPreparedTick = long.MinValue;
        }

        public void AddDelta(long simulationTick, float delta)
        {
            PrepareTick(simulationTick);
            LogicalAngle = Normalize(LogicalAngle + delta);
        }

        public void SetAngle(long simulationTick, float angle)
        {
            PrepareTick(simulationTick);
            LogicalAngle = Normalize(angle);
        }

        public float Sample(float alpha)
        {
            return Mathf.LerpAngle(PreviousAngle, LogicalAngle, Mathf.Clamp01(alpha));
        }

        private void PrepareTick(long simulationTick)
        {
            if (m_LastPreparedTick == simulationTick)
                return;

            PreviousAngle = LogicalAngle;
            m_LastPreparedTick = simulationTick;
        }

        private static float Normalize(float angle)
        {
            return Mathf.DeltaAngle(0f, angle);
        }
    }
}
