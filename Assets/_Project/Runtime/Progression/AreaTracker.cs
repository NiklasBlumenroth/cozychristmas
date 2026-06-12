using System;
using System.Collections.Generic;
using CozySanta.Core.Progression;
using CozySanta.Core.Snow;
using CozySanta.Runtime.Snow;
using CozySanta.Runtime.Sorting;
using UnityEngine;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// Apply-Schicht des Area-Systems. Baut <see cref="AreaProgress"/> aus der Inspector-Konfiguration,
    /// verdrahtet F4-onCompleted + F5-Coverage-Delta und überträgt den Fortschritt ins HUD.
    /// </summary>
    public sealed class AreaTracker : MonoBehaviour
    {
        [Serializable]
        public sealed class TaskEntry
        {
            public string   taskId      = "";
            public TaskType taskType    = TaskType.Sort;
            public string   description = "";
            public float    required    = 1f;
        }

        [Serializable]
        public sealed class SortBinding
        {
            public SortTargetInteractable target;
            public string                 taskId = "";
        }

        [Serializable]
        public sealed class SortGroup
        {
            [Tooltip("Alle SortTargetInteractable (Fächer) unterhalb von 'root' werden auf diese Task-ID gebucht.")]
            public string    taskId = "";
            public Transform root;
            [Tooltip("Setzt die Soll-Menge der Aufgabe automatisch = Anzahl gefundener Fächer.")]
            public bool      autoRequired = true;
        }

        [Header("Area-Konfiguration")]
        [SerializeField] private string      areaName = "Area";
        [SerializeField] private int         areaXp   = 100;
        [SerializeField] private TaskEntry[] taskEntries = new TaskEntry[0];

        [Header("Sort-Anbindung (Fach → Task-ID)")]
        [SerializeField] private SortBinding[] sortBindings = new SortBinding[0];

        [Header("Sort-Gruppen (alle Fächer unter root → eine Task-ID)")]
        [SerializeField] private SortGroup[] sortGroups = new SortGroup[0];

        [Header("Melt-Anbindung")]
        [SerializeField] private string        meltTaskId = "";
        [Tooltip("Wurzel der Schnee-Region DIESES Sektors: alle SnowPatches darunter zählen für den " +
                 "Melt-Task (zellgewichtet). Pro Sektor eine eigene Wurzel → mehrere Schnee-Sektoren " +
                 "sind unabhängig. Leer = Fallback auf melt.Coverage (eine globale Lampe).")]
        [SerializeField] private Transform     meltRoot;
        [Tooltip("Die Lampe (für die Akku-Anzeige; eine pro Spiel). Coverage kommt aus meltRoot, " +
                 "falls gesetzt.")]
        [SerializeField] private MeltController melt;

        [Header("Referenzen")]
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private AreaHudView       hudView;

        private AreaProgress     _progress;
        private CoverageProgress _coverage = new CoverageProgress();
        private SnowPatch[]      _meltPatches;
        private bool             _hudActive;

        /// <summary>Für Tests und Diagnose.</summary>
        public AreaProgress Progress => _progress;

        /// <summary>Wird vom <see cref="AreaManager"/> beim Betreten/Verlassen der zugehörigen Zone
        /// gesetzt: nur die gerade aktive Area beschreibt das (geteilte) HUD-Panel. Fortschritt
        /// (Sortieren/Schmelzen) wird unabhängig davon weiter gebucht.</summary>
        public void SetHudActive(bool active)
        {
            _hudActive = active;
            if (active && hudView != null)
            {
                hudView.SetAreaName(areaName);
                RefreshHud();
            }
        }

        private void Awake()
        {
            // Schnee-Region dieses Sektors einsammeln (für den Melt-Task).
            if (meltRoot != null)
                _meltPatches = meltRoot.GetComponentsInChildren<SnowPatch>(includeInactive: true);

            // Sort-Gruppen einsammeln: alle Fächer unter root, dedupliziert je Task-ID.
            var grouped = CollectSortGroups();

            var tasks = new AreaTask[taskEntries.Length];
            for (var i = 0; i < taskEntries.Length; i++)
            {
                var e = taskEntries[i];
                var required = AutoRequired(e.taskId, grouped) ?? e.required;
                tasks[i] = new AreaTask(e.taskId, e.taskType, e.description, required);
            }
            _progress = new AreaProgress(new AreaDefinition(areaName, tasks, areaXp));
            _progress.OnCompleted += HandleCompletion;

            // Explizite Einzel-Bindings (weiterhin unterstützt).
            foreach (var b in sortBindings)
            {
                if (b.target == null) continue;
                var id = b.taskId;
                b.target.AddCompletionListener(() => _progress.BookSort(id));
            }

            // Gruppen-Bindings: jedes gefundene Fach bucht +1 auf seine Task-ID.
            foreach (var kv in grouped)
            {
                var id = kv.Key;
                foreach (var fach in kv.Value)
                {
                    if (fach != null) fach.AddCompletionListener(() => _progress.BookSort(id));
                }
            }
        }

        private Dictionary<string, HashSet<SortTargetInteractable>> CollectSortGroups()
        {
            var grouped = new Dictionary<string, HashSet<SortTargetInteractable>>();
            foreach (var g in sortGroups)
            {
                if (g == null || g.root == null || string.IsNullOrEmpty(g.taskId)) continue;
                if (!grouped.TryGetValue(g.taskId, out var set))
                {
                    set = new HashSet<SortTargetInteractable>();
                    grouped[g.taskId] = set;
                }
                foreach (var fach in g.root.GetComponentsInChildren<SortTargetInteractable>(includeInactive: true))
                {
                    set.Add(fach);
                }
            }
            return grouped;
        }

        /// <summary>Liefert die auto-ermittelte Soll-Menge (Anzahl Fächer) für eine Task-ID, sofern eine
        /// zugehörige Gruppe autoRequired gesetzt hat und Fächer gefunden wurden; sonst null.</summary>
        private float? AutoRequired(string taskId, Dictionary<string, HashSet<SortTargetInteractable>> grouped)
        {
            var wants = false;
            foreach (var g in sortGroups)
            {
                if (g != null && g.autoRequired && g.taskId == taskId && g.root != null) wants = true;
            }
            if (!wants || !grouped.TryGetValue(taskId, out var set) || set.Count == 0) return null;
            return set.Count;
        }

        private void Start()
        {
            _coverage = new CoverageProgress(CurrentMeltCoverage());
        }

        private void Update()
        {
            TrackMelt();                       // Fortschritt immer buchen …
            if (_hudActive) RefreshHud();      // … aber nur die aktive Area beschreibt das HUD.
        }

        private void TrackMelt()
        {
            if (string.IsNullOrEmpty(meltTaskId)) return;
            // Jeden positiven Zuwachs buchen; CoverageProgress schreibt den Stand nur dann fort, sodass
            // sich auch winzige Pro-Frame-Deltas (großer Nenner) korrekt aufsummieren.
            var inc = _coverage.Advance(CurrentMeltCoverage());
            if (inc > 0f) _progress.BookMelt(meltTaskId, inc * 100f);
        }

        /// <summary>Flächen-Fortschritt 0..1 für den Melt-Task: zellgewichtet über die Schnee-Region
        /// dieses Sektors (<see cref="meltRoot"/>); sonst Fallback auf die Lampe (<see cref="melt"/>).</summary>
        private float CurrentMeltCoverage()
        {
            if (_meltPatches != null && _meltPatches.Length > 0)
            {
                float cleared = 0f, total = 0f;
                foreach (var p in _meltPatches)
                {
                    if (p == null) continue;
                    var cells = p.CellCount;
                    cleared += p.Coverage * cells;
                    total   += cells;
                }
                return total > 0f ? cleared / total : 0f;
            }

            return melt != null ? melt.Coverage : 0f;
        }

        private void RefreshHud()
        {
            if (hudView == null || _progress == null) return;

            var tasks = _progress.Tasks;
            for (var i = 0; i < hudView.EntryCount; i++)
            {
                var entry = hudView.GetEntry(i);
                if (entry == null) continue;
                if (i < tasks.Length)
                {
                    entry.SetVisible(true);
                    entry.SetTask(tasks[i].Description, tasks[i].Current, tasks[i].Required, tasks[i].Type);
                }
                else
                {
                    entry.SetVisible(false);
                }
            }

            if (melt != null) hudView.SetBattery(melt.BatteryFraction);

            if (progression?.State != null)
            {
                var l = progression.State.Ledger;
                hudView.SetLevel(l.Level);
                hudView.SetXp(l.XpIntoLevel, l.XpForNextLevel);
            }
        }

        /// <summary>Wird einmalig gefeuert wenn alle Area-Tasks abgeschlossen sind (z. B. für F8 KeySpawnBinding).</summary>
        public event System.Action OnAreaCompleted;

        private void HandleCompletion()
        {
            if (progression != null) progression.AwardXp(_progress.AreaXp);
            OnAreaCompleted?.Invoke();
        }
    }
}
