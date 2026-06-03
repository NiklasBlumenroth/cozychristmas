namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Laufzeit-Fortschritt einer Area. Bündelt die Tasks aus <see cref="AreaDefinition"/> und
    /// leitet <see cref="IsComplete"/> ab. <see cref="OnCompleted"/> wird einmalig ausgelöst sobald
    /// alle Tasks erledigt sind; weitere Buchungen danach werden ignoriert.
    /// </summary>
    public sealed class AreaProgress
    {
        private readonly AreaDefinition _def;

        public AreaProgress(AreaDefinition definition)
        {
            _def = definition;
        }

        public string     AreaName  => _def.AreaName;
        public AreaTask[] Tasks     => _def.Tasks;
        public int        AreaXp    => _def.AreaXp;
        public bool       Completed { get; private set; }

        public bool IsComplete
        {
            get
            {
                foreach (var t in _def.Tasks) if (!t.IsComplete) return false;
                return _def.Tasks.Length > 0;
            }
        }

        /// <summary>Einmalig bei Übergang zu IsComplete.</summary>
        public event System.Action OnCompleted;

        /// <summary>Bucht Sortier-Fortschritt (+count) auf den Task mit passender ID.</summary>
        public void BookSort(string taskId, float count = 1f)
            => BookTask(taskId, TaskType.Sort, count);

        /// <summary>Bucht Schmelz-Fortschritt (Coverage-Delta in Prozent, 0–100) auf den Melt-Task.</summary>
        public void BookMelt(string taskId, float deltaPercent)
            => BookTask(taskId, TaskType.Melt, deltaPercent);

        private void BookTask(string taskId, TaskType type, float delta)
        {
            if (Completed) return;
            foreach (var t in _def.Tasks)
            {
                if (t.TaskId == taskId && t.Type == type)
                {
                    t.Book(delta);
                    break;
                }
            }
            TryComplete();
        }

        private void TryComplete()
        {
            if (!Completed && IsComplete)
            {
                Completed = true;
                OnCompleted?.Invoke();
            }
        }
    }
}
