using System.Collections.Generic;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MutinyTrajectoryRenderer : MonoBehaviour
    {
        private const float OriginalDashPixels = 8f;
        private const int OriginalPredictionSteps = 15;

        private LineRenderer m_PullLine;
        private readonly List<LineRenderer> m_DashLines = new List<LineRenderer>();
        private Material m_LineMaterial;

        private void Awake()
        {
            m_PullLine = GetComponent<LineRenderer>();
            m_LineMaterial = new Material(Shader.Find("Sprites/Default"));
            ConfigureLine(m_PullLine);
            HideTrajectory();
        }

        private void ConfigureLine(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.loop = false;
            line.startWidth = 2f / MutinyPhysics.PixelsPerUnit;
            line.endWidth = 2f / MutinyPhysics.PixelsPerUnit;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.sortingOrder = 28;
            line.material = m_LineMaterial;
            line.startColor = Color.white;
            line.endColor = Color.white;
        }

        public void ShowTrajectory(
            Vector2 startPosPx,
            Vector2 dragPosPx,
            string[,] terrainGrid,
            int gridW,
            int gridH,
            float maxForce = 20f,
            float weightPerTick = MutinyPhysics.Gravity,
            string weaponType = null)
        {
            DrawPullLine(startPosPx, dragPosPx, maxForce);

            Vector2 launchVelocity = MutinyPhysics.CalculateTwangVelocity(startPosPx, dragPosPx, maxForce);
            // Solid.drawTwangLine predicts exactly 15 unconstrained ticks. It adds
            // weight before each point and deliberately does not test terrain.
            var pixelPoints = new List<Vector2>(OriginalPredictionSteps + 1) { startPosPx };
            Vector2 predictionPosition = startPosPx;
            Vector2 predictionVelocity = launchVelocity;
            for (int i = 0; i < OriginalPredictionSteps; i++)
            {
                predictionVelocity = PredictVelocityTick(weaponType, predictionVelocity, weightPerTick);
                predictionPosition += predictionVelocity;
                pixelPoints.Add(predictionPosition);
            }

            DrawDashedPath(pixelPoints);
        }

        // Solid.drawTwangLine invokes the selected Solid.twangPrediction once before
        // every point. Keep special weapon prediction here so the input preview uses
        // the same original per-tick rules as the production projectile.
        public static Vector2 PredictVelocityTick(string weaponType, Vector2 velocity, float weightPerTick)
        {
            if (string.Equals(weaponType, "parachuteBomb", System.StringComparison.OrdinalIgnoreCase))
            {
                if (velocity.y > 1f)
                {
                    velocity.y -= 2f;
                    if (velocity.y < 1f)
                        velocity.y = 1f;
                }
                velocity.x *= MutinyParachuteBomb.HorizontalDragPerTick;
            }

            velocity.y += weightPerTick;
            return velocity;
        }
        private void DrawPullLine(Vector2 startPixels, Vector2 dragPixels, float maxForce)
        {
            Vector2 pull = dragPixels - startPixels;
            float maximumPull = maxForce * 4f;
            if (pull.sqrMagnitude > maximumPull * maximumPull)
                pull = pull.normalized * maximumPull;

            m_PullLine.positionCount = 2;
            m_PullLine.SetPosition(0, MutinyPhysics.PixelToUnity(startPixels.x, startPixels.y));
            Vector2 end = startPixels + pull;
            m_PullLine.SetPosition(1, MutinyPhysics.PixelToUnity(end.x, end.y));
            m_PullLine.startColor = Color.white;
            m_PullLine.endColor = Color.white;
            m_PullLine.enabled = true;
        }

        private void DrawDashedPath(List<Vector2> pixelPoints)
        {
            int used = 0;
            if (pixelPoints == null || pixelPoints.Count < 2)
            {
                DisableUnusedDashLines(used);
                return;
            }

            float dashRemaining = OriginalDashPixels;
            bool drawing = true;
            float travelled = 0f;
            float totalLength = CalculateLength(pixelPoints);

            for (int i = 1; i < pixelPoints.Count; i++)
            {
                Vector2 cursor = pixelPoints[i - 1];
                Vector2 target = pixelPoints[i];
                Vector2 segment = target - cursor;
                float segmentRemaining = segment.magnitude;
                if (segmentRemaining <= Mathf.Epsilon)
                    continue;

                Vector2 direction = segment / segmentRemaining;
                while (segmentRemaining > Mathf.Epsilon)
                {
                    float step = Mathf.Min(segmentRemaining, dashRemaining);
                    Vector2 next = cursor + direction * step;

                    if (drawing)
                    {
                        float progressStart = totalLength > 0f ? travelled / totalLength : 0f;
                        float progressEnd = totalLength > 0f ? (travelled + step) / totalLength : 0f;
                        LineRenderer dash = GetDashLine(used++);
                        dash.positionCount = 2;
                        dash.SetPosition(0, MutinyPhysics.PixelToUnity(cursor.x, cursor.y));
                        dash.SetPosition(1, MutinyPhysics.PixelToUnity(next.x, next.y));
                        dash.startColor = new Color(1f, 1f, 1f, 1f - progressStart);
                        dash.endColor = new Color(1f, 1f, 1f, 1f - progressEnd);
                        dash.enabled = true;
                    }

                    cursor = next;
                    segmentRemaining -= step;
                    dashRemaining -= step;
                    travelled += step;

                    if (dashRemaining <= Mathf.Epsilon)
                    {
                        drawing = !drawing;
                        dashRemaining = OriginalDashPixels;
                    }
                }
            }

            DisableUnusedDashLines(used);
        }

        private LineRenderer GetDashLine(int index)
        {
            while (m_DashLines.Count <= index)
            {
                GameObject dashObject = new GameObject($"TrajectoryDash_{m_DashLines.Count:D2}");
                dashObject.transform.SetParent(transform, false);
                LineRenderer line = dashObject.AddComponent<LineRenderer>();
                ConfigureLine(line);
                m_DashLines.Add(line);
            }

            return m_DashLines[index];
        }

        private static float CalculateLength(List<Vector2> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
                length += Vector2.Distance(points[i - 1], points[i]);
            return length;
        }

        private void DisableUnusedDashLines(int firstUnused)
        {
            for (int i = firstUnused; i < m_DashLines.Count; i++)
                m_DashLines[i].enabled = false;
        }

        public void HideTrajectory()
        {
            if (m_PullLine != null)
            {
                m_PullLine.positionCount = 0;
                m_PullLine.enabled = false;
            }

            DisableUnusedDashLines(0);
        }

        private void OnDestroy()
        {
            if (m_LineMaterial != null)
                Destroy(m_LineMaterial);
        }
    }
}
