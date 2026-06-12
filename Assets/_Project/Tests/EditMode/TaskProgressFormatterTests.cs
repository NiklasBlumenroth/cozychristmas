using CozySanta.Core.Progression;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>Regeltests für die Aufgaben-Fortschritts-Formatierung (Bruch + Prozent, Komma, 2 NK).</summary>
    public sealed class TaskProgressFormatterTests
    {
        [Test]
        public void Sort_zeigt_Bruch_und_Prozent_mit_zwei_Nachkommastellen()
        {
            Assert.AreEqual("12 / 96 (12,50 %)", TaskProgressFormatter.FormatSort(12, 96));
        }

        [Test]
        public void Sort_bei_required_null_meldet_null_prozent()
        {
            Assert.AreEqual("0 / 0 (0,00 %)", TaskProgressFormatter.FormatSort(0, 0));
        }

        [Test]
        public void Sort_vollstaendig_ist_hundert_prozent()
        {
            Assert.AreEqual("96 / 96 (100,00 %)", TaskProgressFormatter.FormatSort(96, 96));
        }

        [Test]
        public void Melt_zeigt_zwei_Nachkommastellen()
        {
            Assert.AreEqual("57,34 / 100 %", TaskProgressFormatter.FormatMelt(57.337f, 100));
        }

        [Test]
        public void Format_routet_Melt_auf_Prozentform()
        {
            Assert.AreEqual("57,00 / 100 %", TaskProgressFormatter.Format(57, 100, TaskType.Melt));
        }
    }
}
