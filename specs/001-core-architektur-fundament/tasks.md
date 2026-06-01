---
description: "Task list für F1 – Core-Architektur & Projektgrundgerüst"
---

# Tasks: F1 – Core-Architektur & Projektgrundgerüst

**Input**: Design-Dokumente aus `/specs/001-core-architektur-fundament/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, diagrams/ (vorhanden); contracts/ nicht anwendbar

**Tests**: Verbindlich für die Fachregel `InteractionSelector.Decide` (EditMode/Unit) und für den
E2E-Flow (PlayMode). Compile-Check und 300-Zeilen-DoD sind verbindlich.

**Diagrams**: `architektur-schichten.puml` (Class) und `decide-apply-flow.puml` (Activity) bereits in
der Plan-Phase erstellt; in den Stories nur abgleichen/aktuell halten.

**Organization**: Tasks gruppiert nach User Stories (US1, US2, US3) aus der Spec.

## Umsetzungsstatus (2026-06-01) — abgeschlossen

- **Erledigt**: Alle Dateien angelegt (4 Assembly Definitions, 6 Core-Typen, 3 Runtime-Klassen,
  EditMode- + PlayMode-Tests, `CLAUDE.md`-Konvention, 2 PlantUML-Diagramme).
- **Verifiziert (Core)**: Core-Schicht eigenständig mit `dotnet build` (netstandard2.1) kompiliert –
  **0 Fehler / 0 Warnungen**. 300-Zeilen-DoD eingehalten (größte Datei < 80 Zeilen).
- **Verifiziert (Unity)**: EditMode- und PlayMode-Tests im Unity Test Runner ausgeführt – **alle grün**
  (durch Entwickler bestätigt). Voller Unity-Compile inkl. Runtime/Tests damit fehlerfrei.

## Format: `[ID] [P?] [Story] Beschreibung`

- **[P]**: parallelisierbar (andere Datei, keine offene Abhängigkeit)
- **[Story]**: zugehörige User Story (US1/US2/US3)
- Exakte Dateipfade in jeder Beschreibung

## Path Conventions

Unity-Einzelprojekt. Quellcode additiv unter `Assets/_Project/` (Core/Runtime/Tests), Doku unter
`specs/001-core-architektur-fundament/`. Vorlagen-Assets außerhalb `_Project/` bleiben unangetastet.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Projekt-/Governance-Grundlage

- [X] T001 Change-Typ als „Planned feature (waterfall)" bestätigt und in `spec.md`/`plan.md` dokumentiert
- [X] T002 Doku-Impact-Plan prüfen: betroffen ist `CLAUDE.md` (Build/Struktur) + Spec-Artefakte; `Notizen.md` ausgenommen
- [X] T003 [P] Sicherstellen, dass alle Feature-Doku/Kommunikation auf Deutsch ist
- [X] T004 [P] Whole-System-Impact bestätigen: keine bestehende Gameplay-Logik betroffen (nur additive Assemblies)
- [X] T005 [P] Diagrammauswahl verifizieren: Class + Activity vorhanden unter `specs/001-core-architektur-fundament/diagrams/`; übrige als „Not required" begründet

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Assembly-Schichtung anlegen – blockiert alle Stories

**⚠️ CRITICAL**: Keine Story-Arbeit vor Abschluss dieser Phase

- [X] T006 Ordnerstruktur `Assets/_Project/{Core,Runtime,Tests/EditMode,Tests/PlayMode}` anlegen
- [X] T007 Core-Assembly `Assets/_Project/Core/CozySanta.Core.asmdef` mit `"noEngineReferences": true`, `"autoReferenced": false` anlegen
- [X] T008 Runtime-Assembly `Assets/_Project/Runtime/CozySanta.Runtime.asmdef` anlegen, referenziert `CozySanta.Core`
- [X] T009 [P] EditMode-Test-Assembly `Assets/_Project/Tests/EditMode/CozySanta.Tests.EditMode.asmdef` (`includePlatforms:["Editor"]`, `defineConstraints:["UNITY_INCLUDE_TESTS"]`, refs Core + TestRunner, `precompiledReferences:["nunit.framework.dll"]`)
- [X] T010 [P] PlayMode-Test-Assembly `Assets/_Project/Tests/PlayMode/CozySanta.Tests.PlayMode.asmdef` (kein Plattform-Filter, `defineConstraints:["UNITY_INCLUDE_TESTS"]`, refs Core + Runtime + TestRunner, `precompiledReferences:["nunit.framework.dll"]`)
- [X] T011 Teststrategie/Decide-vs-Apply-Grenzen aus `plan.md`/`data-model.md` bestätigen; Diagram-Testkandidaten T1–T8 aus `data-model.md` als verbindlich markieren
- [X] T012 UI-Politik bestätigen: F1 enthält keine UI; keine Laufzeit-UI-Generierung

**Checkpoint**: Schichten + Test-Assemblies kompilieren leer; Stories können beginnen

---

## Phase 3: User Story 1 – Fachlogik ohne Unity-Laufzeit testen (Priority: P1) 🎯 MVP

**Goal**: Reine Core-Entscheidung (`InteractionSelector.Decide`) existiert und ist per EditMode-Test ohne Szene prüfbar.

**Independent Test**: EditMode-Tests T1–T6 laufen grün ohne Szenenstart; Core referenziert kein `UnityEngine`.

### Tests for User Story 1 (zuerst schreiben, müssen zunächst FAIL/nicht kompilieren) ⚠️

- [X] T013 [P] [US1] EditMode-Tests T1–T6 in `Assets/_Project/Tests/EditMode/InteractionSelectorTests.cs` (leer→None, 1 gültig, Distanz-Sortierung, Tie-Break Winkel, Winkel-Filter, Reichweiten-Filter) gemäß `data-model.md`
- [X] T014 [P] [US1] Diagramm-Abgleich: Activity-Branches aus `diagrams/decide-apply-flow.puml` ⇄ Tests T1–T6 prüfen

### Implementation for User Story 1

- [X] T015 [P] [US1] `InteractionCandidate` (readonly struct) in `Assets/_Project/Core/Interaction/InteractionCandidate.cs`
- [X] T016 [P] [US1] `SelectionSettings` (readonly struct) in `Assets/_Project/Core/Interaction/SelectionSettings.cs`
- [X] T017 [P] [US1] `InteractionSelection` (readonly struct, inkl. `None`) in `Assets/_Project/Core/Interaction/InteractionSelection.cs`
- [X] T018 [P] [US1] `ITimeProvider` in `Assets/_Project/Core/Time/ITimeProvider.cs`
- [X] T019 [P] [US1] `IInteractionProbe` in `Assets/_Project/Core/Interaction/IInteractionProbe.cs`
- [X] T020 [US1] `InteractionSelector.Decide(...)` in `Assets/_Project/Core/Interaction/InteractionSelector.cs` gemäß Regel (Reichweiten-/Winkel-Filter, kleinste Distanz, Tie-Break Winkel) – hängt von T015–T017 ab
- [X] T021 [US1] EditMode-Tests T1–T6 grün im Test Runner (bestätigt)
- [X] T022 [US1] Compile-Check + 300-Zeilen-DoD für alle US1-Dateien — Core via `dotnet build` grün ✓, Unity-Compile grün ✓, 300-Zeilen ✓

**Checkpoint**: US1 unabhängig funktional und getestet (Kern-Entscheidung)

---

## Phase 4: User Story 2 – Entscheidung von Seiteneffekt trennen (Decide/Apply) (Priority: P1)

**Goal**: Runtime orchestriert Probe→Decide→Apply; E2E im PlayMode bestätigt Konsistenz zum EditMode.

**Independent Test**: PlayMode-Test T7 in Minimalszene mit Fake-Probe setzt `FocusedTargetId` korrekt.

### Tests for User Story 2 ⚠️

- [X] T023 [P] [US2] PlayMode-Test T7 in `Assets/_Project/Tests/PlayMode/InteractionFlowPlayModeTests.cs` (Fake-`IInteractionProbe` + 1 Ziel → `FocusedTargetId` gesetzt; Ergebnis konsistent zu EditMode)

### Implementation for User Story 2

- [X] T024 [P] [US2] `UnityTimeProvider : ITimeProvider` in `Assets/_Project/Runtime/Time/UnityTimeProvider.cs`
- [X] T025 [P] [US2] `PhysicsInteractionProbe : MonoBehaviour, IInteractionProbe` in `Assets/_Project/Runtime/Interaction/PhysicsInteractionProbe.cs`
- [X] T026 [US2] `PlayerInteractionController : MonoBehaviour` in `Assets/_Project/Runtime/Interaction/PlayerInteractionController.cs` (Probe→`Decide`→Apply: `FocusedTargetId`; keine Regel-Duplikation) – hängt von US1 + T025
- [X] T027 [US2] PlayMode-Test T7 grün im Test Runner (bestätigt; Fake-Probe via `Configure(...)`, keine Szene-Datei nötig)
- [X] T028 [US2] Diagramm-Abgleich: Apply-Seiteneffekt in `diagrams/decide-apply-flow.puml` ⇄ T7
- [X] T029 [US2] Compile-Check + 300-Zeilen-DoD für alle US2-Dateien — Unity-Compile grün ✓, 300-Zeilen ✓

**Checkpoint**: US1 + US2 unabhängig funktional; Decide/Apply exemplarisch belegt

---

## Phase 5: User Story 3 – Neues Feature ohne Setup-Reibung starten (Priority: P2)

**Goal**: Ordner-/Assembly-/Namespace-Konvention ist in `CLAUDE.md` dokumentiert und nachvollziehbar.

**Independent Test**: Anhand `CLAUDE.md`/`quickstart.md` lässt sich ein neuer Core-Typ + Test ohne Zusatzkonfiguration anlegen und ausführen.

### Implementation for User Story 3

- [X] T030 [US3] `CLAUDE.md` Abschnitt „Build und Werkzeuge" um die `Assets/_Project/`-Struktur, Assembly-Namen und Namespace-Konvention ergänzen (FR-008)
- [X] T031 [P] [US3] Querverweis auf `specs/001-core-architektur-fundament/quickstart.md` in `CLAUDE.md` aufnehmen
- [X] T032 [US3] SC-004 belegt: die Tests in den neuen Assemblies kompilieren/laufen ohne Zusatzkonfiguration, womit die dokumentierte Struktur als auflösbar bestätigt ist

**Checkpoint**: Konvention dokumentiert und praktisch verifiziert

---

## Phase 6: Documentation Sync & Merge Readiness

- [X] T033 Doku-Coverage-Matrix: jede geänderte Code-Area ⇄ aktualisierte Doku/Diagramme (ohne `Notizen.md`)
- [X] T034 Doku-Split-Analyse: kein Dokument übergroß – bestätigt
- [X] T035 [P] Deutsch-Nachweis für Feature-Doku/Kommunikation
- [X] T036 [P] PlantUML-Dateien renderbar/ohne Platzhalter geprüft (`architektur-schichten.puml`, `decide-apply-flow.puml`)
- [X] T037 Bestätigen: keine Laufzeit-UI eingeführt
- [X] T038 Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + alle EditMode-/PlayMode-Tests grün (Test Runner bestätigt)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: keine Abhängigkeiten
- **Foundational (Phase 2)**: nach Setup; **blockiert alle Stories** (asmdefs nötig)
- **US1 (Phase 3)**: nach Phase 2; keine Story-Abhängigkeit
- **US2 (Phase 4)**: nach Phase 2; nutzt US1-Core-Typen (T020 vor T026)
- **US3 (Phase 5)**: nach Phase 2; inhaltlich nach US1/US2 sinnvoll (dokumentiert reale Struktur)
- **Doc Sync (Phase 6)**: nach gewünschten Stories

### Within Each User Story

- Tests vor Implementierung schreiben (müssen zunächst fehlschlagen/nicht kompilieren)
- Daten-Structs vor `Decide`; `Decide` (Core) vor `Apply` (Runtime)
- Compile-Check + 300-Zeilen-DoD vor Story-Abschluss
- Diagramm-Abgleich vor Story-Abschluss

### Parallel Opportunities

- T003/T004/T005 parallel; T009/T010 parallel
- US1: T015–T019 (verschiedene Dateien) parallel; T013/T014 parallel vor Implementierung
- US2: T024/T025 parallel
- US1 und US2 teilen sich Core-Typen → US2-Apply erst nach T020

---

## Implementation Strategy

### MVP First

1. Phase 1 Setup
2. Phase 2 Foundational (kritisch – blockiert Stories)
3. Phase 3 US1 (reine Core-Entscheidung + EditMode-Tests) → **validieren** (MVP der Architektur)
4. Phase 4 US2 (Decide/Apply + PlayMode-E2E)
5. Phase 5 US3 (Konvention dokumentieren) → Phase 6 Merge-Readiness

### Notes

- [P] = andere Datei, keine offene Abhängigkeit
- Nach jedem Task/Logikblock committen
- An jedem Checkpoint Story unabhängig validieren
- Keine Laufzeit-UI; UI ist editor-authored (in F1 ohnehin nicht vorhanden)
