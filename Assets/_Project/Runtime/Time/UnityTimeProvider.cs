using CozySanta.Core.Time;

namespace CozySanta.Runtime.Time
{
    /// <summary>
    /// Runtime-Implementierung von <see cref="ITimeProvider"/> auf Basis von <c>UnityEngine.Time</c>.
    /// </summary>
    public sealed class UnityTimeProvider : ITimeProvider
    {
        public float DeltaTime => UnityEngine.Time.deltaTime;

        public double Now => UnityEngine.Time.timeAsDouble;
    }
}
