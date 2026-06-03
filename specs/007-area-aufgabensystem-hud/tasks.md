---
description: "Task list für F7 – Area- & Aufgabensystem + HUD"
---

# Tasks: F7 – Area- & Aufgabensystem + HUD

**Input**: Design-Dokumente aus `/specs/007-area-aufgabensystem-hud/`
**Prerequisites**: spec.md, plan.md, research.md, data-model.md, quickstart.md, diagrams/ (vorhanden)

**Tests**: Verbindlich für Core (A1–A3, B1–B2 in `AreaProgressTests.cs`). HUD/Apply per PlayMode/manuell.
Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `area-fortschritt.puml` (Activity), `area-datenmodell.puml` (Class), `area-zustand.puml` (State).

**Status**: Abgeschlossen. EditMode-Tests A1–A3, B1–B2, C1–C2 grün; manuelle Abnahme bestätigt.

---

## Phase 1: Setup

- [x] T001 Change-Typ „Planned feature" bestätigen (spec/plan)
- [x] T002 Doku-Impact: `CLAUDE.md` (Fortschritt) + Spec-Artefakte; `Notizen.md` ausgenommen
- [x] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation bestätigen
- [x] T004 [P] Diagrammauswahl verifizieren (Activity + Class + State; Sequence ausgelassen)

## Phase 2: Foundational

- [x] T005 Andockpunkte bestätigen: `SortTargetInteractable.AddCompletionListener`, `MeltController.Coverage`, `PlayerProgression.AwardXp`
- [x] T006 Testkandidaten A1–A3, B1–B2 aus data-model.md als verbindlich markieren
- [x] T007 UI-Politik bestätigen: HUD editor-authored; TaskEntryUI-Prefab; kein Laufzeit-Erzeugen

## Phase 3: US1 – Aufgabenfortschritt live aktualisiert (P1) 🎯 MVP

- [x] T008 [P] [US1] EditMode A1–A3 in `Assets/_Project/Tests/EditMode/AreaProgressTests.cs`
- [x] T009 [P] [US1] `TaskType` (enum) in `Assets/_Project/Core/Progression/TaskType.cs`
- [x] T010 [US1] `AreaTask` (Book, IsComplete, Clamp) in `Assets/_Project/Core/Progression/AreaTask.cs`
- [x] T011 [US1] `AreaDefinition` (Name, Tasks[], AreaXp) in `Assets/_Project/Core/Progression/AreaDefinition.cs`
- [x] T012 [US1] `AreaProgress` (BookSort/BookMelt, IsComplete, OnCompleted) in `Assets/_Project/Core/Progression/AreaProgress.cs`
- [x] T013 [US1] ⏳ EditMode A1–A3 grün — Test Runner
- [x] T014 [US1] `TaskEntryUI` (NameText, ProgressText, SetTask) in `Assets/_Project/Runtime/Progression/TaskEntryUI.cs`
- [x] T015 [US1] `AreaHudView` (Set*-Methoden, GetEntry, TaskEntryUI[]) in `Assets/_Project/Runtime/Progression/AreaHudView.cs`
- [x] T016 [US1] `AreaTracker` (hält AreaProgress, Subscribe Sort/Melt, Update Coverage-Delta, aktualisiert HudView) in `Assets/_Project/Runtime/Progression/AreaTracker.cs`
- [x] T017 [US1] `AreaSetup.cs` Editor-Skript: HUD-Panel + TaskEntryUI-Prefab erstellen, AreaTracker verdrahten in `Assets/_Project/Editor/AreaSetup.cs`
- [x] T018 [US1] ⏳ Editor-Setup ausführen: „CozySanta/Setup F7 (Area-HUD erstellen)" — Editor
- [x] T019 [US1] ⏳ Manueller Test: Fach abschließen → Task-Zähler steigt im HUD

## Phase 4: US2 – Area-Abschluss erkennen und XP vergeben (P1)

- [x] T020 [P] [US2] EditMode B1–B2 in `AreaProgressTests.cs`
- [x] T021 [US2] ⏳ EditMode B1–B2 grün — Test Runner
- [x] T022 [US2] `AreaTracker`: OnCompleted → `PlayerProgression.AwardXp(config.areaXp)` verdrahten
- [x] T023 [US2] ⏳ Manueller Test: alle Tasks erledigen → XP vergeben, kein zweites Event

## Phase 5: US3 – HUD Lampen-Akku (P2)

- [x] T024 [US3] `AreaHudView`: `batteryBar` (Slider) + `SetBattery(float fraction)` verdrahten
- [x] T025 [US3] `AreaTracker.Update`: `MeltController.BatteryFraction` → `hudView.SetBattery(...)` pro Frame
- [x] T026 [US3] ⏳ Manueller Test: F-Taste halten → Akku-Balken sinkt; loslassen → steigt

## Phase 6: US4 – Ladestation (P2)

- [x] T027 [P] [US4] EditMode C1–C2 (Ladelogik) in `AreaProgressTests.cs`
- [x] T028 [US4] Additiver Setter `MeltController.ChargeFromStation(float amount)` in `MeltController.cs`
- [x] T029 [US4] `LadeStation` (IInteractable, ChargeTick, chargeDuration, HudView-Ref) in `Assets/_Project/Runtime/Progression/LadeStation.cs`
- [x] T030 [US4] Placeholder-Prefab `LadeStation` (Cube + Skript) in `Assets/_Project/Prefabs/LadeStation.prefab` (via AreaSetup oder manuell)
- [x] T031 [US4] `DevSpawnMenu`-Eintrag: LadeStation-Prefab zur Spawn-Liste hinzufügen (via AreaSetup)
- [x] T032 [US4] `AreaHudView`: `chargeBar` (Slider, initial inaktiv) + `SetChargeProgress(float fraction)` + `SetChargeBarVisible(bool)`
- [x] T033 [US4] `PlayerInputRelay.Update`: Rechtsklick (`Mouse.current.rightButton.isPressed`) + fokussierte `LadeStation` → `ChargeTick(dt)` + `hudView.SetChargeProgress`; bei Abbruch `SetChargeBarVisible(false)`
- [x] T034 [US4] ⏳ EditMode C1–C2 grün — Test Runner
- [x] T035 [US4] ⏳ Manueller Test: zur Ladestation gehen, Rechtsklick halten → Ladebalken erscheint, Akku steigt; loslassen/wegschauen → stoppt, Stand bleibt

## Phase 7: US5 – HUD XP/Level (P2)

- [x] T036 [US5] `AreaHudView`: `levelText`, `xpBar` + `SetLevel/SetXp`-Methoden
- [x] T037 [US5] `AreaTracker`: `PlayerProgression.State` auslesen → HUD nach XP-Änderung aktualisieren (via Callback oder `Update`)
- [x] T038 [US5] ⏳ Manueller Test: Fach abschließen → XP-Balken im HUD wächst

## Phase 8: Documentation Sync & Merge Readiness

- [x] T039 Doku-Coverage-Matrix: geänderte Code-Areas ⇄ Doku/Diagramme (ohne `Notizen.md`)
- [x] T040 `CLAUDE.md` „Fortschritt" um F7 ergänzen
- [x] T041 [P] Deutsch-Nachweis; PlantUML ohne Platzhalter
- [x] T042 Bestätigen: keine Laufzeit-UI (HUD + Ladebalken editor-authored)
- [x] T043 ⏳ Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + EditMode A1–A3/B1–B2/C1–C2 grün — Test Runner

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
