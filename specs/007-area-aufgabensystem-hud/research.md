# Research: F7 – Area- & Aufgabensystem + HUD

**Branch**: `007-area-aufgabensystem-hud` | **Date**: 2026-06-03

## Entscheidung 1 – Einheitliche Fortschritts-Skala

**Entscheidung**: `AreaTask.Current` und `AreaTask.Required` sind beide `float`. Sort-Tasks nutzen ganzzahlige Werte (z. B. 3.0f / 3.0f), Melt-Tasks Prozentwerte (z. B. 63.5f / 80.0f).

**Begründung**: Vermeidet zwei verschiedene Datentypen (int/float) und erlaubt Custom-Tasks beliebiger Granularität. `IsComplete = Current >= Required` funktioniert für beide.

**Verworfene Alternative**: Separate `SortTask : int` und `MeltTask : float` – mehr Typen, kein Mehrwert.

## Entscheidung 2 – Aktive Area per SerializeField

**Entscheidung**: `AreaTracker` hat ein `[SerializeField] private AreaDefinitionConfig areaConfig` (ScriptableObject oder serialisierbares Struct) das die Area-Konfiguration enthält. In F7 gibt es genau eine aktive Area.

**Begründung**: Einfachste Lösung für MVP; kein Proximity-System nötig. F8 führt die Tor/Schlüssel-Logik ein, die Areas dann sequenziell freischaltet.

**Verworfene Alternative**: Automatische Erkennung via Trigger-Collider – Overhead für MVP mit einer Area nicht gerechtfertigt.

## Entscheidung 3 – TaskEntryUI-Prefab (analog SkillEntryUI)

**Entscheidung**: Wiederholte HUD-Zeilen als Prefab `TaskEntryUI` (NameText + ProgressText). `AreaHudView` hält ein `TaskEntryUI[]`-Array (Reihenfolge = Task-Reihenfolge in AreaDefinition).

**Begründung**: Konsistent mit F6-Pattern (SkillEntryUI). Editor-authored, Runtime bindet nur.

**Verworfene Alternative**: Dynamisches Instantiieren der Zeilen zur Laufzeit – verstößt gegen Constitution V.

## Entscheidung 4 – XP-Anbindung über PlayerProgression.AwardXp

**Entscheidung**: `AreaTracker` ruft direkt `playerProgression.AwardXp(areaXp)` bei Abschluss auf. `AwardXp` ist bereits public in F6.

**Begründung**: Direkteste Verbindung; kein neues Event-System nötig. Andockpunkt war in F6 explizit vorgesehen.

## Entscheidung 5 – AreaDefinition als serialisierbares C#-Objekt

**Entscheidung**: `AreaDefinition` ist ein reines C#-Objekt (kein ScriptableObject) mit `[Serializable]`-Unterobjekten. `AreaTracker` hält die Konfiguration direkt via `[SerializeField]`.

**Begründung**: Einfacher, kein Asset-Management nötig für MVP. ScriptableObject lohnt sich erst wenn mehrere Areas dynamisch geladen werden (F8+).
