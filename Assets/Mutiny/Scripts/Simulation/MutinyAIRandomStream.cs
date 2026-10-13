using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mutiny.Simulation
{
    [Serializable]
    public sealed class MutinyAIRandomDraw
    {
        public string Kind;
        public float Minimum;
        public float Maximum;
        public float Value;
    }

    [Serializable]
    public sealed class MutinyAICandidateRecord
    {
        public string Character;
        public string Weapon;
        public string MoveType;
        public float Score;
        public Vector2 Velocity;
        public Vector2 Target;
        public string TargetCharacter;
        public float SeagullFlightY;
        public float[] SeagullShotXs;
        public Vector2[] BoxPossibilities;
        public int CannonRotationDegrees;
    }

    [Serializable]
    public sealed class MutinyAIDecisionTrace
    {
        public int DecisionId;
        public int TeamNumber;
        public int Seed;
        public string Phase;
        public string Winner;
        public List<MutinyAIRandomDraw> Draws = new List<MutinyAIRandomDraw>();
        public List<MutinyAICandidateRecord> Candidates = new List<MutinyAICandidateRecord>();
    }

    /// <summary>
    /// Isolates AI draws from Unity's global random stream. A saved trace can be
    /// replayed draw-for-draw; bounds/type mismatches fail rather than silently
    /// changing the decision. The source game does not expose its PRNG state.
    /// </summary>
    public sealed class MutinyAIRandomStream
    {
        private UnityEngine.Random.State m_State;
        private readonly MutinyAIDecisionTrace m_Replay;
        private int m_ReplayIndex;
        private int m_CandidateIndex;

        public MutinyAIDecisionTrace Trace { get; }

        public MutinyAIRandomStream(int seed, int decisionId, int teamNumber, string phase,
            MutinyAIDecisionTrace replay = null)
        {
            UnityEngine.Random.State previous = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            m_State = UnityEngine.Random.state;
            UnityEngine.Random.state = previous;
            m_Replay = replay;
            Trace = new MutinyAIDecisionTrace
            {
                DecisionId = decisionId,
                TeamNumber = teamNumber,
                Seed = seed,
                Phase = phase
            };
        }

        public int NextInt(int minimum, int maximum)
        {
            if (m_Replay != null)
                return Mathf.RoundToInt(ReadReplay("int", minimum, maximum));

            UnityEngine.Random.State previous = UnityEngine.Random.state;
            UnityEngine.Random.state = m_State;
            int value = UnityEngine.Random.Range(minimum, maximum);
            m_State = UnityEngine.Random.state;
            UnityEngine.Random.state = previous;
            Record("int", minimum, maximum, value);
            return value;
        }

        public float NextFloat(float minimum, float maximum)
        {
            if (m_Replay != null)
                return ReadReplay("float", minimum, maximum);

            UnityEngine.Random.State previous = UnityEngine.Random.state;
            UnityEngine.Random.state = m_State;
            float value = UnityEngine.Random.Range(minimum, maximum);
            m_State = UnityEngine.Random.state;
            UnityEngine.Random.state = previous;
            Record("float", minimum, maximum, value);
            return value;
        }

        public void AssertReplayComplete()
        {
            if (m_Replay != null && m_ReplayIndex != m_Replay.Draws.Count)
                throw new InvalidDataException($"AI replay consumed {m_ReplayIndex}/{m_Replay.Draws.Count} draws");
            if (m_Replay != null && m_CandidateIndex != m_Replay.Candidates.Count)
                throw new InvalidDataException($"AI replay consumed {m_CandidateIndex}/{m_Replay.Candidates.Count} candidates");
        }

        public void RecordCandidate(MutinyAICandidateRecord candidate)
        {
            if (m_Replay != null)
            {
                if (m_CandidateIndex >= m_Replay.Candidates.Count)
                    throw new InvalidDataException($"AI replay generated an unexpected candidate at {m_CandidateIndex}");
                MutinyAICandidateRecord expected = m_Replay.Candidates[m_CandidateIndex];
                if (expected.Character != candidate.Character ||
                    (expected.Weapon ?? string.Empty) != (candidate.Weapon ?? string.Empty) ||
                    expected.MoveType != candidate.MoveType ||
                    !Mathf.Approximately(expected.Score, candidate.Score) ||
                    Vector2.Distance(expected.Velocity, candidate.Velocity) > 0.001f ||
                    Vector2.Distance(expected.Target, candidate.Target) > 0.001f ||
                    (expected.TargetCharacter ?? string.Empty) != (candidate.TargetCharacter ?? string.Empty) ||
                    !Mathf.Approximately(expected.SeagullFlightY, candidate.SeagullFlightY) ||
                    expected.CannonRotationDegrees != candidate.CannonRotationDegrees ||
                    !SameFloats(expected.SeagullShotXs, candidate.SeagullShotXs) ||
                    !SameVectors(expected.BoxPossibilities, candidate.BoxPossibilities))
                    throw new InvalidDataException($"AI replay candidate {m_CandidateIndex} diverged");
                m_CandidateIndex++;
            }
            Trace.Candidates.Add(candidate);
        }

        private static bool SameFloats(float[] first, float[] second)
        {
            if (first == null || second == null)
                return (first == null || first.Length == 0) && (second == null || second.Length == 0);
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (!Mathf.Approximately(first[i], second[i])) return false;
            return true;
        }

        private static bool SameVectors(Vector2[] first, Vector2[] second)
        {
            if (first == null || second == null)
                return (first == null || first.Length == 0) && (second == null || second.Length == 0);
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (Vector2.Distance(first[i], second[i]) > 0.001f) return false;
            return true;
        }

        private float ReadReplay(string kind, float minimum, float maximum)
        {
            if (m_ReplayIndex >= m_Replay.Draws.Count)
                throw new InvalidDataException($"AI replay ran out of draws at {m_ReplayIndex}");
            MutinyAIRandomDraw draw = m_Replay.Draws[m_ReplayIndex++];
            if (draw.Kind != kind || !Mathf.Approximately(draw.Minimum, minimum) ||
                !Mathf.Approximately(draw.Maximum, maximum) ||
                draw.Value < minimum || draw.Value > maximum ||
                (kind == "int" && (draw.Value != Mathf.Round(draw.Value) || draw.Value >= maximum)))
                throw new InvalidDataException($"AI replay draw {m_ReplayIndex - 1} does not match {kind}[{minimum},{maximum})");
            Record(kind, minimum, maximum, draw.Value);
            return draw.Value;
        }

        private void Record(string kind, float minimum, float maximum, float value)
        {
            Trace.Draws.Add(new MutinyAIRandomDraw
            {
                Kind = kind, Minimum = minimum, Maximum = maximum, Value = value
            });
        }
    }
}
