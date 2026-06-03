---
description: "Task list für F7 – Area- & Aufgabensystem + HUD"
---

# Tasks: F7 – Area- & Aufgabensystem + HUD

**Input**: Design-Dokumente aus `/specs/007-area-aufgabensystem-hud/`
**Prerequisites**: spec.md, plan.md, research.md, data-model.md, quickstart.md, diagrams/ (vorhanden)

**Tests**: Verbindlich für Core (A1–A3, B1–B2 in `AreaProgressTests.cs`). HUD/Apply per PlayMode/manuell.
Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `area-fortschritt.puml` (Activity), `area-datenmodell.puml` (Class), `area-zustand.puml` (State).

**Status**: Planung abgeschlossen. **Code noch nicht begonnen.**

---

## Phase 1: Setup

- [ ] T001 Change-Typ „Planned feature" bestätigen (spec/plan)
- [ ] T002 Doku-Impact: `CLAUDE.md` (Fortschritt) + Spec-Artefakte; `Notizen.md` ausgenommen
- [ ] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation bestätigen
- [ ] T004 [P] Diagrammauswahl verifizieren (Activity + Class + State; Sequence ausgelassen)

## Phase 2: Foundational

- [ ] T005 Andockpunkte bestätigen: `SortTargetInteractable.AddCompletionListener`, `MeltController.Coverage`, `PlayerProgression.AwardXp`
- [ ] T006 Testkandidaten A1–A3, B1–B2 aus data-model.md als verbindlich markieren
- [ ] T007 UI-Politik bestätigen: HUD editor-authored; TaskEntryUI-Prefab; kein Laufzeit-Erzeugen

## Phase 3: US1 – Aufgabenfortschritt live aktualisiert (P1) 🎯 MVP

- [ ] T008 [P] [US1] EditMode A1–A3 in `Assets/_Project/Tests/EditMode/AreaProgressTests.cs`
- [ ] T009 [P] [US1] `TaskType` (enum) in `Assets/_Project/Core/Progression/TaskType.cs`
- [ ] T010 [US1] `AreaTask` (Book, IsComplete, Clamp) in `Assets/_Project/Core/Progression/AreaTask.cs`
- [ ] T011 [US1] `AreaDefinition` (Name, Tasks[], AreaXp) in `Assets/_Project/Core/Progression/AreaDefinition.cs`
- [ ] T012 [US1] `AreaProgress` (BookSort/BookMelt, IsComplete, OnCompleted) in `Assets/_Project/Core/Progression/AreaProgress.cs`
- [ ] T013 [US1] ⏳ EditMode A1–A3 grün — Test Runner
- [ ] T014 [US1] `TaskEntryUI` (NameText, ProgressText, SetTask) in `Assets/_Project/Runtime/Progression/TaskEntryUI.cs`
- [ ] T015 [US1] `AreaHudView` (Set*-Methoden, GetEntry, TaskEntryUI[]) in `Assets/_Project/Runtime/Progression/AreaHudView.cs`
- [ ] T016 [US1] `AreaTracker` (hält AreaProgress, Subscribe Sort/Melt, Update Coverage-Delta, aktualisiert HudView) in `Assets/_Project/Runtime/Progression/AreaTracker.cs`
- [ ] T017 [US1] `AreaSetup.cs` Editor-Skript: HUD-Panel + TaskEntryUI-Prefab erstellen, AreaTracker verdrahten in `Assets/_Project/Editor/AreaSetup.cs`
- [ ] T018 [US1] ⏳ Editor-Setup ausführen: „CozySanta/Setup F7 (Area-HUD erstellen)" — Editor
- [ ] T019 [US1] ⏳ Manueller Test: Fach abschließen → Task-Zähler steigt im HUD

## Phase 4: US2 – Area-Abschluss erkennen und XP vergeben (P1)

- [ ] T020 [P] [US2] EditMode B1–B2 in `AreaProgressTests.cs`
- [ ] T021 [US2] ⏳ EditMode B1–B2 grün — Test Runner
- [ ] T022 [US2] `AreaTracker`: OnCompleted → `PlayerProgression.AwardXp(config.areaXp)` verdrahten
- [ ] T023 [US2] ⏳ Manueller Test: alle Tasks erledigen → XP vergeben, kein zweites Event

## Phase 5: US3 – HUD Lampen-Akku (P2)

- [ ] T024 [US3] `AreaHudView`: `batteryBar` (Slider) + `SetBattery(float fraction)` verdrahten
- [ ] T025 [US3] `AreaTracker.Update`: `MeltController.BatteryFraction` → `hudView.SetBattery(...)` pro Frame
- [ ] T026 [US3] ⏳ Manueller Test: F-Taste halten → Akku-Balken sinkt; loslassen → steigt

## Phase 6: US4 – HUD XP/Level (P2)

- [ ] T027 [US4] `AreaHudView`: `levelText`, `xpBar` + `SetLevel/SetXp`-Methoden
- [ ] T028 [US4] `AreaTracker`: `PlayerProgression.State` auslesen → HUD nach XP-Änderung aktualisieren (via Callback oder `Update`)
- [ ] T029 [US4] ⏳ Manueller Test: Fach abschließen → XP-Balken im HUD wächst

## Phase 7: Documentation Sync & Merge Readiness

- [ ] T030 Doku-Coverage-Matrix: geänderte Code-Areas ⇄ Doku/Diagramme (ohne `Notizen.md`)
- [ ] T031 `CLAUDE.md` „Fortschritt" um F7 ergänzen
- [ ] T032 [P] Deutsch-Nachweis; PlantUML ohne Platzhalter
- [ ] T033 Bestätigen: keine Laufzeit-UI (HUD editor-authored)
- [ ] T034 ⏳ Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + EditMode A1–A3/B1–B2 grün — Test Runner

---

## Dependencies & Execution Order

- Setup→Foundational blockiert Stories. US1 (Fortschritt/Anzeige) → US2 (Abschluss, braucht vollständigen AreaProgress) → US3/US4 (HUD-Erweiterungen, unabhängig voneinander).

### Within each story

- Core vor Runtime-Anwendung; Tests vor/with Implementierung; Compile + 300-Zeilen vor Story-Abschluss.

### Parallel Opportunities

- T008/T009 parallel zu Core-Start; T014/T015 teils parallel; T020 parallel zu T016/T017.

## Notes

- [P] = andere Datei, keine offene Abhängigkeit.
- F4/F5/F6-Verträge nicht brechen; Erweiterungen additiv.
- `AreaTrackerConfig` kann als `[Serializable]`-Struct in `AreaTracker` inline definiert werden (kein eigenes ScriptableObject für MVP).
- Balancingwerte (AreaXp, Task-Soll-Mengen) sind Platzhalter → später tunen.
