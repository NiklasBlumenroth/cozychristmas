using System;
using System.Collections.Generic;
using CozySanta.Core.Progression;
using UnityEngine;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// Editierbare Stufen-Tabelle aller Skills (Balancing in einer Datei, im Inspector pflegbar).
    /// Eine Zeile je Skill: Anzeige (Name/Gruppe/Einheit) + Werteverlauf. Die meisten Skills nutzen
    /// nur Wert 1 (Start→Ende); Fähigkeiten zusätzlich Wert 2 = Ladungen (Wert 1 = Cooldown).
    /// Die Laufzeit baut daraus die reine Core-<see cref="SkillConfig"/>; das editor-authored Menü
    /// (ProgressionSetup) liest Namen/Gruppen aus denselben Zeilen.
    /// </summary>
    [CreateAssetMenu(menuName = "CozySanta/Skill Table", fileName = "SkillTable")]
    public sealed class SkillTableConfig : ScriptableObject
    {
        [Serializable]
        public struct Row
        {
            public SkillId id;
            [Tooltip("Anzeigename im Skill-Menü.")] public string displayName;
            [Tooltip("Gruppen-Überschrift im Menü (gleiche aufeinanderfolgende Werte werden gebündelt).")]
            public string group;
            [Tooltip("Freischalt-Skill (erste Investition schaltet frei).")] public bool unlockable;
            [Tooltip("Maximale Stufe.")] public int maxLevel;
            [Tooltip("Wert 1 bei Stufe 0 (Fähigkeiten: Cooldown-Start in s).")] public float startValue;
            [Tooltip("Wert 1 bei Maximalstufe (Fähigkeiten: Cooldown-Ende in s).")] public float endValue;
            [Tooltip("Anzeige-Einheit für Wert 1 (z. B. kg, s, m/s, x).")] public string unit;
            [Tooltip("Hat zweite Kurve = Ladungen (nur Fähigkeiten).")] public bool hasCharges;
            [Tooltip("Ladungen bei Stufe 0.")] public float chargesStart;
            [Tooltip("Ladungen bei Maximalstufe.")] public float chargesEnd;
        }

        [SerializeField] private Row[] rows = DefaultRows();

        /// <summary>Alle Zeilen in Eingabe-Reihenfolge (= Menü-Reihenfolge).</summary>
        public IReadOnlyList<Row> Rows => rows ?? Array.Empty<Row>();

        /// <summary>Baut die Core-Konfiguration je SkillId (Index = Enum-Wert). Fehlende Zeilen = neutral.</summary>
        public SkillConfig[] BuildConfigs()
        {
            var count = Enum.GetValues(typeof(SkillId)).Length;
            var result = new SkillConfig[count];
            for (var i = 0; i < count; i++) result[i] = new SkillConfig(0f, 0f, 1);

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    var idx = (int)r.id;
                    if (idx < 0 || idx >= count) continue;
                    result[idx] = new SkillConfig(r.startValue, r.endValue, r.maxLevel, r.unlockable,
                        r.hasCharges, r.chargesStart, r.chargesEnd);
                }
            }

            return result;
        }

        /// <summary>Liefert die Zeile zu einer SkillId (z. B. für Einheit/Name im Menü).</summary>
        public bool TryGetRow(SkillId id, out Row row)
        {
            if (rows != null)
            {
                foreach (var r in rows)
                {
                    if (r.id == id) { row = r; return true; }
                }
            }

            row = default;
            return false;
        }

        /// <summary>Setzt die Zeilen auf den Startentwurf zurück (Editor-Setup).</summary>
        public void ResetToDefaults() => rows = DefaultRows();

        // Startentwurf (Konzept F12); im Inspector frei anpassbar.
        private static Row[] DefaultRows() => new[]
        {
            Row1(SkillId.LampPower,     "Schmelzstärke",     "Lampe",        false, 20, 1.2f, 3.0f, "x"),
            Row1(SkillId.LampBattery,   "Akku",              "Lampe",        false, 20, 12f,  32f,  "s"),
            Row1(SkillId.CarryCapacity, "Tragkraft",         "Tragen",       false, 20, 5f,   25f,  "kg"),
            Row1(SkillId.MoveSpeed,     "Laufgeschw.",       "Bewegung",     false, 20, 3f,   6f,   "m/s"),
            Row2(SkillId.ObjectPull,    "Heranholen",        "Heranholen",   20, 10f, 4f, 1f, 5f),
            Row2(SkillId.AutoSort,      "Auto-Einsortieren", "Einsortieren", 20, 8f,  3f, 1f, 5f),
        };

        private static Row Row1(SkillId id, string name, string group, bool unlockable, int maxLevel,
            float start, float end, string unit) => new Row
        {
            id = id, displayName = name, group = group, unlockable = unlockable, maxLevel = maxLevel,
            startValue = start, endValue = end, unit = unit, hasCharges = false,
        };

        private static Row Row2(SkillId id, string name, string group, int maxLevel,
            float cdStart, float cdEnd, float chargesStart, float chargesEnd) => new Row
        {
            id = id, displayName = name, group = group, unlockable = true, maxLevel = maxLevel,
            startValue = cdStart, endValue = cdEnd, unit = "s", hasCharges = true,
            chargesStart = chargesStart, chargesEnd = chargesEnd,
        };
    }
}
