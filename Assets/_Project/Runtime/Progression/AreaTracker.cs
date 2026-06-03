using System;
using CozySanta.Core.Progression;
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

        [Header("Area-Konfiguration")]
        [SerializeField] private string      areaName = "Area";
        [SerializeField] private int         areaXp   = 100;
        [SerializeField] private TaskEntry[] taskEntries = new TaskEntry[0];

        [Header("Sort-Anbindung (Fach → Task-ID)")]
        [SerializeField] private SortBinding[] sortBindings = new SortBinding[0];

        [Header("Melt-Anbindung")]
        [SerializeField] private string        meltTaskId = "";
        [SerializeField] private MeltController melt;

        [Header("Referenzen")]
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private AreaHudView       hudView;

        private AreaProgress _progress;
        private float        _lastCoverage;

        /// <summary>Für Tests und Diagnose.</summary>
        public AreaProgress Progress => _progress;

        private void Awake()
        {
            var tasks = new AreaTask[taskEntries.Length];
            for (var i = 0; i < taskEntries.Length; i++)
            {
                var e = taskEntries[i];
                tasks[i] = new AreaTask(e.taskId, e.taskType, e.description, e.required);
            }
            _progress = new AreaProgress(new AreaDefinition(areaName, tasks, areaXp));
            _progress.OnCompleted += HandleCompletion;

            foreach (var b in sortBindings)
            {
                if (b.target == null) continue;
                var id = b.taskId;
                b.target.AddCompletionListener(() => _progress.BookSort(id));
            }
        }

        private void Start()
        {
            if (melt != null) _lastCoverage = melt.Coverage;
            if (hudView != null) hudView.SetAreaName(areaName);
            RefreshHud();
        }

        private void Update()
        {
            TrackMelt();
            RefreshHud();
        }

        private void TrackMelt()
        {
            if (melt == null || string.IsNullOrEmpty(meltTaskId)) return;
            var current = melt.Coverage;
            var delta   = current - _lastCoverage;
            if (delta > 0.0001f) _progress.BookMelt(meltTaskId, delta * 100f);
            _lastCoverage = current;
        }

        private void RefreshHud()
        {
            if (hudView == null) return;

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

        private void HandleCompletion()
        {
            if (progression != null) progression.AwardXp(_progress.AreaXp);
        }
    }
}
