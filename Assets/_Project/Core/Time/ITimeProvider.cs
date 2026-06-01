namespace CozySanta.Core.Time
{
    /// <summary>
    /// Provider-Abstraktion für Zeitzugriffe ohne direkte Bindung an <c>UnityEngine.Time</c>.
    /// Ermöglicht deterministische Tests von zeitabhängiger Fachlogik (z. B. spätere Cooldowns).
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>Vergangene Zeit seit dem letzten Frame in Sekunden.</summary>
        float DeltaTime { get; }

        /// <summary>Monotone Spielzeit in Sekunden.</summary>
        double Now { get; }
    }
}
