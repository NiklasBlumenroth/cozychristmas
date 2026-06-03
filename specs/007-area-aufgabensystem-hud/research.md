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

## Entscheidung 5 – Area-Abgrenzung im MVP: immer aktiv, kein Trigger

**Entscheidung**: In F7 gibt es genau eine Area, die immer aktiv ist. Kein Trigger-Collider nötig. F8 bringt Trigger-Collider wenn mehrere Areas sequenziell freigeschaltet werden.

**Begründung**: MVP hat einen Sektor. Spatial-Trigger-Logik ist overhead der erst relevant wird wenn es mehrere Areas gibt. Einfachste korrekte Lösung für jetzt.

## Entscheidung 6 – Ladestation: Rechtsklick + LoS-Raycast im PlayerInputRelay

**Entscheidung**: `PlayerInputRelay.Update()` prüft `Mouse.current.rightButton.isPressed` + ob der Interaktions-Raycast die fokussierte `LadeStation` trifft. Wenn beides zutrifft, wird `focusedStation.ChargeTick(dt)` aufgerufen.

**Begründung**: Konsistent mit dem bestehenden Interaktionsmuster (Raycast läuft bereits für F2); kein neues Input-System nötig. LoS-Prüfung ist bereits im `PhysicsInteractionProbe` vorhanden.

**Verworfene Alternative**: Eigener `ChargingController` MonoBehaviour – unnötige Trennung für eine einfache Held-Interaction.

## Entscheidung 7 – Ladebalken im HUD (editor-authored), nicht World-Space

**Entscheidung**: Der Ladebalken ist ein Slider im `AreaHudView`-Panel (HUD, Screen-Space). Er ist standardmäßig inaktiv und wird nur während des Ladevorgangs eingeblendet.

**Begründung**: Konsistent mit dem restlichen HUD-Ansatz; einfacher zu implementieren als World-Space Canvas am Prefab. World-Space wäre ansprechender aber Mehraufwand für den Prototyp.

## Entscheidung 8 – AreaDefinition als serialisierbares C#-Objekt

**Entscheidung**: `AreaDefinition` ist ein reines C#-Objekt (kein ScriptableObject) mit `[Serializable]`-Unterobjekten. `AreaTracker` hält die Konfiguration direkt via `[SerializeField]`.

**Begründung**: Einfacher, kein Asset-Management nötig für MVP. ScriptableObject lohnt sich erst wenn mehrere Areas dynamisch geladen werden (F8+).
