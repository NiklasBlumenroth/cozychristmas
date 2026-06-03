# Implementation Plan: F7 – Area- & Aufgabensystem + HUD

**Branch**: `007-area-aufgabensystem-hud` | **Date**: 2026-06-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification aus `/specs/007-area-aufgabensystem-hud/spec.md`

## Summary

Area-basierter Fortschritt mit editor-authored HUD. Core modelliert `AreaTask`/`AreaDefinition`/`AreaProgress` (reine Logik, testbar). Runtime-`AreaTracker` verdrahtet F4/F5-Events auf den Core. `AreaHudView` bindet das editor-authored HUD-Panel. HUD zeigt Area-Aufgaben, Akku-Balken und XP/Level.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Fortschritt), neue Spec-Artefakte, `specs/007-area-aufgabensystem-hud/`
**Diagram Impact**: Activity (Fortschritt/Abschluss), Class (Datenmodell), State (Area-Lebenszyklus)
**Working Language**: German

## Technical Context

**Language/Version**: C# / Unity 6000.2.7f2
**Primary Dependencies**: Unity URP 17.2, Input System 1.14.2, TextMeshPro (Unity.TextMeshPro), UnityEngine.UI
**Storage**: Kein Speichern in F7 (Laufzeit); F14 bringt Persistenz
**Testing**: Unity Test Framework 1.6 – EditMode für Core-Logik (A1–A3, B1–B2); PlayMode/manuell für HUD-Anzeige
**Target Platform**: PC / Unity Editor (Linux, Windows)
**Performance Goals**: Fortschritts-Update im selben Frame wie das auslösende Event; kein spürbarer Overhead
**Constraints**: Keine Laufzeit-UI-Erzeugung; max. 300 Zeilen pro Klassendatei; Core ohne UnityEngine
**Scale/Scope**: 1 aktive Area (MVP); erweiterbar auf mehrere Areas ab F8
**Integration Touchpoints**: F4 `SortTargetInteractable.AddCompletionListener`, F5 `MeltController.Coverage`, F6 `PlayerProgression.AwardXp`, F6 `SkillMenuView` (XP/Level-Anzeige im HUD teilt Daten aus F6)
**PlantUML Documentation**: `diagrams/area-fortschritt.puml`, `diagrams/area-datenmodell.puml`, `diagrams/area-zustand.puml`

## Constitution Check

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined für alle betroffenen Bereiche (ohne `Notizen.md`)
- [x] Whole-system impact und Kollisions-Analyse dokumentiert (F4/F5/F6-Andockpunkte)
- [x] PlantUML-Diagrammauswahl abgeschlossen (Activity + Class + State; Sequence ausgelassen)
- [x] Definitionen/Requirements vor Implementierung aktualisiert (keine Kollisionen offen)
- [x] Compile-Validierung: `dotnet build CozySanta.Core.csproj`, `CozySanta.Runtime.csproj`, `CozySanta.Tests.EditMode.csproj`
- [x] Architekturtrennung: Core (`AreaTask`, `AreaDefinition`, `AreaProgress`) ohne Unity; Runtime (`AreaTracker`, `AreaHudView`) als Apply-Schicht
- [x] Decide/Apply-Trennung: `AreaProgress.BookSort/BookMelt` = Decide; `AreaTracker.Update` + Event-Anbindung = Apply
- [x] Teststrategie: EditMode für Fortschritts-/Abschlusslogik; PlayMode/manuell für HUD-Anzeige
- [x] Diagram-derived test candidates: Activity-Branches (Fortschritt unter/über Grenze, Abschluss einmalig), State-Übergang (Aktiv → Abgeschlossen), Class-Invarianten (IsComplete abgeleitet)
- [x] DoD: Compile-Check, 300-Zeilen, Unit-Tests für Fachregeln
- [x] UI: editor-authored (AreaHudView + TaskEntryUI-Prefab); keine Laufzeit-Erzeugung
- [x] Documentation split: nicht nötig
- [x] Diagram sync beim PR-Review geplant
- [x] Dokumentation auf Deutsch

## Project Structure

### Documentation (this feature)

```
specs/007-area-aufgabensystem-hud/
├── spec.md
├── plan.md              ← diese Datei
├── research.md
├── data-model.md
├── quickstart.md
├── checklists/requirements.md
├── diagrams/
│   ├── area-fortschritt.puml
│   ├── area-datenmodell.puml
│   └── area-zustand.puml
└── tasks.md
```

### Source Code

```
Assets/_Project/
├── Core/Progression/          ← AreaTask.cs, AreaDefinition.cs, AreaProgress.cs (F7)
├── Runtime/Progression/       ← AreaTracker.cs, AreaHudView.cs, TaskEntryUI.cs (F7)
├── Prefabs/UI/                ← TaskEntryUI.prefab (F7, analog SkillEntryUI)
├── Editor/                    ← AreaSetup.cs (F7 Editor-Setup)
└── Tests/EditMode/            ← AreaProgressTests.cs (F7)
```

**Structure Decision**: Additive Erweiterung von `Core/Progression/` und `Runtime/Progression/` (aus F6); keine neuen Assemblies nötig.

## Phase 0 – Research

Ergebnisse in `research.md`. Wesentliche Entscheidungen:

- **Task-Fortschritt**: Zähler (Sort: ganze Zahl) und Prozentwert (Melt: float 0..100) sind unterschiedliche Skalen → `AreaTask` hat `float Current` + `float Required` für beide Typen einheitlich.
- **Area-Trigger**: Aktive Area wird per `[SerializeField]` in `AreaTracker` konfiguriert (kein automatisches Proximity-System in F7).
- **HUD-Struktur**: Festes Panel oben rechts (analog SkillMenuPanel); TaskEntryUI-Prefab für wiederholte Zeilen.
- **XP-Anbindung**: `AreaTracker` ruft `PlayerProgression.AwardXp` direkt auf (analog F6-Pattern).

## Phase 1 – Design & Contracts

Ergebnisse in `data-model.md` und `quickstart.md`.

### Core-Typen

| Typ | Verantwortung |
|---|---|
| `TaskType` (enum) | Sort, Melt, Custom |
| `AreaTask` | Id, Typ, Beschreibung, Required, Current, IsComplete; `Book(float delta)` |
| `AreaDefinition` | Name, Tasks[], AreaXp (readonly config) |
| `AreaProgress` | Hält AreaDefinition + Mutable-Task-Stände; BookSort/BookMelt; IsComplete; onCompleted (einmalig) |

### Runtime-Typen

| Typ | Verantwortung |
|---|---|
| `AreaTracker` | MonoBehaviour; hält AreaProgress; abonniert F4-Events + trackt F5-Coverage; Apply auf PlayerProgression |
| `AreaHudView` | MonoBehaviour; editor-authored HUD-Panel; Set*-Methoden; bindet TaskEntryUI[] |
| `TaskEntryUI` | MonoBehaviour (Prefab-Zeile); TaskNameText, ProgressText |

### Testkandidaten

| ID | Testfall | Art |
|---|---|---|
| A1 | Sort-Task: `BookSort(1)` → Current steigt, IsComplete wenn Grenze erreicht | EditMode |
| A2 | Melt-Task: `BookMelt(delta)` → Prozentwert steigt korrekt | EditMode |
| A3 | Task nach Abschluss: weitere `Book`-Aufrufe → keine Änderung | EditMode |
| B1 | Area-Abschluss: alle Tasks erledigt → `IsComplete` true, Event einmalig | EditMode |
| B2 | Area bereits abgeschlossen: weiterer Fortschritt → `IsComplete` bleibt true, kein zweites Event | EditMode |
