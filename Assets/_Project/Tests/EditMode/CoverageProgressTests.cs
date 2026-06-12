using CozySanta.Core.Snow;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// Regressionstests für <see cref="CoverageProgress"/>. Sichert ab, dass sich auch sehr kleine
    /// Zuwächse vollständig aufsummieren (Bug: Schwellen-Prüfung verlor Mini-Pro-Frame-Deltas, während
    /// der Stand bedingungslos fortgeschrieben wurde → Fortschritt blieb bei 0).
    /// </summary>
    public sealed class CoverageProgressTests
    {
        [Test]
        public void Advance_liefert_positiven_Zuwachs_und_schreibt_fort()
        {
            var p = new CoverageProgress();
            Assert.AreEqual(0.25f, p.Advance(0.25f), 1e-6f);
            Assert.AreEqual(0f,    p.Advance(0.25f), 1e-6f); // kein neuer Zuwachs
            Assert.AreEqual(0.25f, p.Last, 1e-6f);
        }

        [Test]
        public void Advance_ignoriert_Rueckgang()
        {
            var p = new CoverageProgress(0.5f);
            Assert.AreEqual(0f, p.Advance(0.4f), 1e-6f); // Coverage gesunken → nichts buchen
            Assert.AreEqual(0.5f, p.Last, 1e-6f);        // Stand bleibt Hochwassermarke
        }

        [Test]
        public void Winzige_Schritte_summieren_sich_vollstaendig_auf()
        {
            var p = new CoverageProgress();
            var total = 0f;
            var current = 0f;
            // 10.000 winzige Schritte (je 0,00001) – einzeln weit unter jeder sinnvollen Schwelle.
            for (var i = 0; i < 10000; i++)
            {
                current += 0.00001f;
                total += p.Advance(current);
            }

            Assert.AreEqual(0.1f, total, 1e-3f); // ~10 % Gesamt-Zuwachs darf nicht verloren gehen
            Assert.AreEqual(current, p.Last, 1e-6f);
        }
    }
}
