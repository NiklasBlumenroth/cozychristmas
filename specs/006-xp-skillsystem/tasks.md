---
description: "Task list für F6 – XP- & Skillsystem"
---

# Tasks: F6 – XP- & Skillsystem

**Input**: Design-Dokumente aus `/specs/006-xp-skillsystem/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, diagrams/ (vorhanden); contracts/ nicht anwendbar

**Tests**: Verbindlich für Core (`XpLedger` X1–X4, `SkillSet`/`Skill` S1–S5/V1). Runtime-Apply (A1) per PlayMode/manuell.
Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `xp-skill-entscheidung.puml` (Activity), `progression-daten.puml` (Class), `skill-zustand.puml` (State).

**Status**: Planung abgeschlossen (spec/plan/research/data-model/quickstart/diagrams). **Code noch nicht begonnen.**

---

## Phase 1: Setup

- [ ] T001 Change-Typ „Planned feature" bestätigen (spec/plan)
- [ ] T002 Doku-Impact: `CLAUDE.md` (Fortschritt) + Spec-Artefakte; `Notizen.md` ausgenommen
- [ ] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation
- [ ] T004 [P] Diagrammauswahl verifizieren (Activity + Class + State; Sequence/Mindmap begründet ausgelassen)

## Phase 2: Foundational

- [ ] T005 Ordner `Assets/_Project/Core/Progression/`, `Assets/_Project/Runtime/Progression/` anlegen
- [ ] T006 Testkandidaten X1–X4, S1–S5, V1, A1 aus data-model.md als verbindlich markieren
- [ ] T007 Andockpunkte bestätigen: `PlayerCarry.Capacity`, `MeltController` (Setter additiv ergänzen falls nötig), F2-Speed (Property additiv falls nötig), `SortTargetInteractable.onCompleted`, `SnowPatch.Coverage`
- [ ] T008 UI-Politik bestätigen: Skillmenü editor-authored; IMGUI nur Dev-Tool

## Phase 3: US1 – XP sammeln & Level-up (P1) 🎯 MVP

- [ ] T009 [P] [US1] EditMode X1–X4 in `Assets/_Project/Tests/EditMode/ProgressionTests.cs`
- [ ] T010 [US1] Core `XpLedger` (Add, Level-Kurve, EarnedSkillPoints, mehrfacher Level-up) in `Assets/_Project/Core/Progression/XpLedger.cs`
- [ ] T011 [US1] ⏳ EditMode X1–X4 grün — Test Runner

## Phase 4: US2 – Skillpunkte frei investieren (P1)

- [ ] T012 [P] [US2] EditMode S1–S5 in `ProgressionTests.cs`
- [ ] T013 [P] [US2] `SkillId` (enum) in `Assets/_Project/Core/Progression/SkillId.cs`
- [ ] T014 [US2] `Skill` (Level/MaxLevel/Unlock/Value/Raise) in `Assets/_Project/Core/Progression/Skill.cs`
- [ ] T015 [US2] `SkillSet` (CanInvest/TryInvest/SpentPoints) in `Assets/_Project/Core/Progression/SkillSet.cs`
- [ ] T016 [US2] `ProgressionState` (XpLedger+SkillSet, AvailablePoints, AwardXp, Invest) in `Assets/_Project/Core/Progression/ProgressionState.cs`
- [ ] T017 [US2] ⏳ EditMode S1–S5 grün — Test Runner

## Phase 5: US3 – Skillwerte wirken aufs Gameplay (P1)

- [ ] T018 [P] [US3] EditMode V1 (Wert/Deckelung, z. B. Tragkraft ≤ 25 kg) in `ProgressionTests.cs`
- [ ] T019 [US3] Runtime `PlayerProgression` (hält ProgressionState; `ApplySkills` → `PlayerCarry.Capacity`, `MeltController`-Werte, Bewegung) in `Assets/_Project/Runtime/Progression/PlayerProgression.cs`
- [ ] T020 [US3] Falls nötig: kleine öffentliche Setter additiv an `MeltController` (Akku/Power/Kegel) und F2-Speed-Property
- [ ] T021 [US3] ⏳ EditMode V1 grün + manueller Test (Invest → Tragkraft/Lampe/Speed ändern sich, A1)

## Phase 6: US4 – XP-Quellen angebunden (P2)

- [ ] T022 [US4] `PlayerProgression`: `AwardSortXp` an `SortTargetInteractable.onCompleted` binden; `AwardMeltXp` aus `SnowPatch.Coverage`-Delta; Andockmethoden für Area/Schlüssel/Kugel
- [ ] T023 [US4] ⏳ manueller Test (Fach abschließen → XP; Schnee schmelzen → XP)

## Phase 6b: Dev-Tool & Menü-Bindung

- [ ] T024 `SkillMenuDevTool` (IMGUI: XP gutschreiben, je SkillId investieren, Level/Punkte anzeigen) in `Assets/_Project/Runtime/Progression/SkillMenuDevTool.cs`
- [ ] T025 Bindungs-Schnittstelle für das editor-authored Skillmenü (Anzeige + `Invest(id)`), kein Laufzeit-UI-Erzeugen
- [ ] T026 ⏳ Editor-Setup/Menü-Prefab anlegen (editor-authored) — Editor

## Phase 7: Documentation Sync & Merge Readiness

- [ ] T027 Doku-Coverage-Matrix: geänderte Code-Areas ⇄ Doku/Diagramme (ohne `Notizen.md`)
- [ ] T028 `CLAUDE.md` „Fortschritt" um F6 ergänzen
- [ ] T029 [P] Deutsch-Nachweis; PlantUML ohne Platzhalter
- [ ] T030 Bestätigen: keine Laufzeit-UI (Skillmenü editor-authored)
- [ ] T031 ⏳ Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + EditMode X1–X4/S1–S5/V1 grün — Test Runner

---

## Dependencies & Execution Order

- Setup→Foundational blockiert Stories. US1 (XP/Level) → US2 (Investition, braucht AvailablePoints) →
  US3 (Wirkung, braucht Skillwerte) → US4 (Quellen) bauen aufeinander auf. Dev-Tool/Menü parallel ab US2.
  Doc-Sync zuletzt.

### Within each story

- Core vor Runtime-Anwendung; Tests vor/with Implementierung; Diagramm-Abgleich + Compile + 300-Zeilen vor Story-Abschluss.

### Parallel Opportunities

- T009 parallel zu Core-Start; T013/T014/T015 teils parallel; T012/T018 parallel.

## Notes

- [P] = andere Datei, keine offene Abhängigkeit. Keine Laufzeit-UI (Skillmenü editor-authored, IMGUI nur Dev).
- F2–F5-Verträge nicht brechen; Erweiterungen additiv (`Capacity` existiert; ggf. kleine Setter additiv).
- Balancingwerte (Kurve, XP-Beträge, Skill-Skalierung) sind Platzhalter → später tunen (Konzept 08).
