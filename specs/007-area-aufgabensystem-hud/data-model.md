# Data Model & Contracts: F7 – Area- & Aufgabensystem + HUD

## Core (`CozySanta.Core.Progression`)

### `TaskType` (enum)
- `Sort`, `Melt`, `Custom`

### `AreaTask`
- `string TaskId`, `TaskType Type`, `string Description`
- `float Required` (Soll; Sort: Anzahl, Melt: Prozent 0..100)
- `float Current` (Ist, nie > Required)
- `bool IsComplete => Current >= Required`
- `void Book(float delta)` – addiert delta auf Current (clamp bei Required); ignoriert wenn IsComplete

### `AreaDefinition`
- `string AreaName`, `AreaTask[] Tasks`, `int AreaXp`
- Unveränderliche Konfiguration; wird aus SerializeFields in `AreaTracker` gebaut

### `AreaProgress`
- Hält `AreaDefinition` + mutable Task-Zustände (Dictionary<string, float> oder paralleles Array)
- `bool IsComplete` – alle Tasks erfüllt
- `bool Completed` – Flag: Abschluss-Event wurde bereits ausgelöst
- `void BookSort(string taskId, float count)` – delegiert an passenden Sort-Task
- `void BookMelt(string taskId, float coverageDelta)` – delegiert an passenden Melt-Task
- `event System.Action OnCompleted` – einmalig bei Übergang → IsComplete

## Runtime (`CozySanta.Runtime.Progression`)

### `AreaTracker : MonoBehaviour`
- `[SerializeField] AreaTrackerConfig config` (Name, Tasks[], AreaXp, Sort/Melt-TaskId-Mapping)
- `[SerializeField] PlayerProgression progression`
- `[SerializeField] MeltController melt`
- `[SerializeField] SortTargetInteractable[] sortTargets`
- `[SerializeField] AreaHudView hudView`
- Awake: baut `AreaProgress` aus config; abonniert SortTargets + OnCompleted
- Update: trackt Coverage-Delta → `BookMelt`; aktualisiert HudView
- `AreaProgress Progress { get; }` (für Tests/Diagnose)

### `AreaHudView : MonoBehaviour`
- `[SerializeField] TMP_Text areaNameText`
- `[SerializeField] TaskEntryUI[] taskEntries`
- `[SerializeField] Slider batteryBar`
- `[SerializeField] TMP_Text levelText`
- `[SerializeField] Slider xpBar`
- `SetAreaName(string)`, `SetBattery(float fraction)`, `SetLevel(int)`, `SetXp(int into, int forNext)`
- `TaskEntryUI GetEntry(int index)`

### `TaskEntryUI : MonoBehaviour` (Prefab)
- `[SerializeField] TMP_Text taskNameText`
- `[SerializeField] TMP_Text progressText`
- `void SetTask(string name, float current, float required, TaskType type)`

## Abgeleitete Testkandidaten

| ID | Testfall | Art | Quelle |
|---|---|---|---|
| A1 | `BookSort`: Current steigt; IsComplete bei Grenze | EditMode | Activity |
| A2 | `BookMelt`: Prozentwert steigt korrekt | EditMode | Activity |
| A3 | Task nach Abschluss: weitere Buchungen ignoriert | EditMode | State-Grenze |
| B1 | Area: alle Tasks erledigt → IsComplete true, OnCompleted einmalig | EditMode | State-Übergang |
| B2 | Area bereits abgeschlossen: weiterer Fortschritt ohne zweites Event | EditMode | State-Invariante |
