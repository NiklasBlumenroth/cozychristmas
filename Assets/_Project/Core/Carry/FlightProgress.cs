namespace CozySanta.Core.Carry
{
    /// <summary>
    /// Reiner, testbarer Fortschritt einer Trag-Flugbewegung (Decide): akkumuliert die verstrichene Zeit
    /// gegen eine feste Dauer und liefert linearen wie geglätteten (Smoothstep) Fortschritt 0..1. Keine
    /// Unity-Typen (Core, noEngineReferences); Position/Rotation interpoliert die Runtime
    /// (<c>CarriedItemFlight</c>) anhand von <see cref="Eased"/>.
    /// </summary>
    public sealed class FlightProgress
    {
        private readonly float _duration;

        /// <param name="durationSeconds">Dauer der Bewegung; ≤ 0 wird auf einen Minimalwert geklemmt.</param>
        public FlightProgress(float durationSeconds)
        {
            _duration = durationSeconds <= 0f ? 0.0001f : durationSeconds;
        }

        /// <summary>Bisher verstrichene Zeit (Sekunden), bei der Dauer gedeckelt nicht nötig – siehe Linear01.</summary>
        public float Elapsed { get; private set; }

        /// <summary>True, sobald die Dauer erreicht/überschritten ist.</summary>
        public bool IsDone => Elapsed >= _duration;

        /// <summary>Linearer Fortschritt 0..1 (geklemmt).</summary>
        public float Linear01 => Clamp01(Elapsed / _duration);

        /// <summary>Geglätteter Fortschritt 0..1 (Smoothstep, ease-in-out). Endpunkte exakt 0 bzw. 1.</summary>
        public float Eased
        {
            get
            {
                var t = Linear01;
                return t * t * (3f - (2f * t));
            }
        }

        /// <summary>Treibt die Zeit um <paramref name="deltaSeconds"/> (≤ 0 = unverändert).</summary>
        public void Step(float deltaSeconds)
        {
            if (deltaSeconds > 0f)
            {
                Elapsed += deltaSeconds;
            }
        }

        /// <summary>Setzt den Fortschritt auf Anfang zurück (für Wiederverwendung).</summary>
        public void Reset()
        {
            Elapsed = 0f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
