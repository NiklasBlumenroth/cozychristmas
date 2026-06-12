namespace CozySanta.Core.Snow
{
    /// <summary>
    /// Reine Buchungs-Hilfe (Decide) für monotonen Flächen-Fortschritt: liefert aus dem aktuellen
    /// Coverage-Wert den noch nicht gebuchten, positiven Zuwachs und schreibt den internen Stand
    /// <b>nur bei positivem Zuwachs</b> fort. Damit summieren sich auch winzige Pro-Frame-Deltas
    /// korrekt auf – im Gegensatz zu einer Schwellen-Prüfung, die den Stand bedingungslos fortschreibt
    /// und kleine Zuwächse verliert. Keine Unity-Abhängigkeit.
    /// </summary>
    public sealed class CoverageProgress
    {
        private float _last;

        public CoverageProgress(float start = 0f)
        {
            _last = start < 0f ? 0f : start;
        }

        /// <summary>Zuletzt gebuchter Stand (0..1).</summary>
        public float Last => _last;

        /// <summary>
        /// Liefert den positiven Zuwachs seit dem letzten gebuchten Stand (0 bei keinem/negativem
        /// Zuwachs) und schreibt den Stand nur bei positivem Zuwachs fort. So geht kein Fortschritt
        /// verloren, egal wie klein die einzelnen Schritte sind.
        /// </summary>
        public float Advance(float current)
        {
            var delta = current - _last;
            if (delta <= 0f)
            {
                return 0f;
            }

            _last = current;
            return delta;
        }
    }
}
