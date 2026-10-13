using System;

namespace Mutiny.Simulation
{
    /// <summary>
    /// Central random source for rolls that change simulation state.
    /// Online matches seed both peers with the same value so deterministic
    /// lockstep replay stays identical. UnityEngine.Random is deliberately NOT
    /// used here: presentation code (audio picks, UI flourishes) shares that
    /// global stream at frame-rate-dependent moments, which would desync the
    /// draw order between two machines.
    /// Semantics mirror UnityEngine.Random: value is [0,1],
    /// Range(int) is [min, max-1], Range(float) is [min, max).
    /// </summary>
    public static class MutinyMatchRandom
    {
        private static Random s_Random;

        public static void SetSeed(int seed)
        {
            s_Random = new Random(seed);
        }

        public static float NextValue()
        {
            return (float)RandomInstance().NextDouble();
        }

        public static int NextInt(int minimumInclusive, int maximumExclusive)
        {
            return RandomInstance().Next(minimumInclusive, maximumExclusive);
        }

        public static float NextFloat(float minimumInclusive, float maximumExclusive)
        {
            double t = RandomInstance().NextDouble();
            return minimumInclusive + (float)((maximumExclusive - minimumInclusive) * t);
        }

        private static Random RandomInstance()
        {
            // Local games never receive a match seed; lazily seed from the clock
            // so behaviour keeps its previous "random every run" character.
            if (s_Random == null)
                s_Random = new Random(Environment.TickCount);
            return s_Random;
        }
    }
}
