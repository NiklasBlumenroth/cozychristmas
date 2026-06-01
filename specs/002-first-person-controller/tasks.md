---
description: "Task list für F2 – First-Person-Controller & Interaktionssystem"
---

# Tasks: F2 – First-Person-Controller & Interaktionssystem

**Input**: Design-Dokumente aus `/specs/002-first-person-controller/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, diagrams/ (vorhanden); contracts/ nicht anwendbar

**Tests**: Verbindlich für Core-Mathematik (`LookMath`, `MovementCalculator`, `InteractionTrigger`)
und für die kritischen E2E-Flows (PlayMode). Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `interaktion-ausloesen.puml` (Sequence) + `interaktion-komponenten.puml` (Class) in der
Plan-Phase erstellt; in den Stories nur abgleichen/aktuell halten.

**Organization**: Tasks nach User Stories (US1 Bewegung/Blick, US2 Erkennung, US3 Auslösung).

## Umsetzungsstatus (2026-06-01)

- **Erledigt**: Alle Dateien angelegt – Core `LookMath`/`MovementCalculator`/`InteractionTrigger`;
  Runtime `FirstPersonController`, `IInteractable`, `IInteractableResolver`, `InteractionPromptPresenter`,
  erweiterte `PhysicsInteractionProbe` + `PlayerInteractionController`; EditMode-Tests (L/M/G) +
  PlayMode-Tests (E1–E5); 2 PlantUML-Diagramme; `CLAUDE.md` Fortschritt aktualisiert.
- **Verifiziert (Core)**: `dotnet build` (netstandard2.1) der Core-Schicht – **0 Fehler / 0 Warnungen**.
  300-Zeilen-DoD eingehalten. F1-Verträge nicht gebrochen (HasFocus/FocusedTargetId-Semantik erhalten).
- **Ausstehend (Unity-Editor hält Projekt-Lock)**: voller Unity-Compile inkl. Runtime + EditMode-/
  PlayMode-Testlauf (T015, T016 voll, T024, T030, T035). → Im **Test Runner** ausführen.
- **Nachtrag (Spielbarkeit, FR-004)**: `PlayerInputRelay` (Input-Adapter: Player-Map → Controller-
  Setter, Behavior „Send Messages"), `DebugInteractable` (Test-Interactable) ergänzt; Runtime-asmdef
  um `Unity.InputSystem` erweitert. Damit ist F2 im Play-Mode begehbar (manuelles Setup siehe quickstart.md).

## Format: `[ID] [P?] [Story] Beschreibung`

- **[P]**: parallelisierbar (andere Datei, keine offene Abhängigkeit)
- exakte Dateipfade in jeder Beschreibung

## Path Conventions

Unity-Einzelprojekt, Code additiv unter `Assets/_Project/` (Core/Runtime/Tests), aufbauend auf F1.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Change-Typ „Planned feature" bestätigt (spec/plan)
- [X] T002 Doku-Impact prüfen: `CLAUDE.md` (Status/MVP) + Spec-Artefakte; `Notizen.md` ausgenommen
- [X] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation sicherstellen
- [X] T004 [P] Diagrammauswahl verifizieren (Sequence + Class vorhanden; Activity/State/Mindmap begründet ausgelassen)

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: blockiert alle Stories

- [X] T005 Ordner `Assets/_Project/Core/Player/`, `Assets/_Project/Runtime/Player/` anlegen
- [X] T006 Input-Anbindung geklärt: Map „Player" (Move/Look/Interact); Eingaben per Setter injiziert (PlayerInput-UnityEvents → SetMoveInput/SetLookInput); Jump/Crouch/Sprint/Attack nicht verdrahtet
- [X] T007 Testkandidaten L1–L3, M1–M3, G1–G2, E1–E5 aus `data-model.md` als verbindlich markiert
- [X] T008 UI-Politik bestätigt: Hinweis-UI editor-authored, nur Toggle/Bind im Code (FR-008)

**Checkpoint**: Struktur/Vorgaben stehen; Stories können beginnen

---

## Phase 3: User Story 1 – Bewegen & Umsehen (Priority: P1) 🎯 MVP

**Goal**: First-Person-Bewegung (WASD) + Maus-Blick (Pitch begrenzt) + Wandkollision/Schwerkraft.

**Independent Test**: In Grey-Box-Szene laufen/umsehen, Pitch begrenzt, keine Wanddurchdringung (E1).

### Tests for User Story 1 ⚠️

- [X] T009 [P] [US1] EditMode-Tests L1–L3 in `Assets/_Project/Tests/EditMode/LookMathTests.cs`
- [X] T010 [P] [US1] EditMode-Tests M1–M3 in `Assets/_Project/Tests/EditMode/MovementCalculatorTests.cs`
- [X] T011 [P] [US1] PlayMode-Test E1 (Bewegung gegen Wand) in `Assets/_Project/Tests/PlayMode/FirstPersonInteractionPlayModeTests.cs`

### Implementation for User Story 1

- [X] T012 [P] [US1] `LookMath.ClampPitch` in `Assets/_Project/Core/Player/LookMath.cs`
- [X] T013 [P] [US1] `MovementCalculator.ComputeLocalVelocity` in `Assets/_Project/Core/Player/MovementCalculator.cs`
- [X] T014 [US1] `FirstPersonController` in `Assets/_Project/Runtime/Player/FirstPersonController.cs` (Input per Setter, Core rufen, CharacterController-Move + Schwerkraft + Yaw/Pitch)
- [ ] T015 [US1] ⏳ EditMode L1–L3/M1–M3 + PlayMode E1 grün — **ausstehend** (Test Runner)
- [~] T016 [US1] Compile-Check + 300-Zeilen-DoD US1 — Core via `dotnet build` grün ✓, 300-Zeilen ✓; voller Unity-Compile ausstehend

**Checkpoint**: Spieler begehbar; US1 unabhängig testbar

---

## Phase 4: User Story 2 – Interagierbare Objekte erkennen (Priority: P1)

**Goal**: Fokus über F1-`Decide` auf `IInteractable`-Objekte; dezenter (editor-authored) Hinweis an/aus.

**Independent Test**: Anschauen eines Test-Interactable in Reichweite/Winkel → Hinweis (E2); wegschauen/zu weit → kein Fokus (E3).

### Tests for User Story 2 ⚠️

- [X] T017 [P] [US2] PlayMode-Tests E2/E3 (Fokus + Prompt-Zustand) in `FirstPersonInteractionPlayModeTests.cs`

### Implementation for User Story 2

- [X] T018 [P] [US2] `IInteractable` in `Assets/_Project/Runtime/Interaction/IInteractable.cs` (`Interact()`, `PromptText`)
- [X] T019 [P] [US2] `IInteractableResolver` in `Assets/_Project/Runtime/Interaction/IInteractableResolver.cs`
- [X] T020 [US2] `PhysicsInteractionProbe` erweitert: nur `IInteractable`-Collider, Frame-Map `TargetId→IInteractable`, `IInteractableResolver` implementiert
- [X] T021 [US2] `InteractionPromptPresenter` (SerializeField promptRoot, `Show/Hide`, `IsShown`, keine UI-Erzeugung)
- [X] T022 [US2] `PlayerInteractionController` erweitert: Fokus auflösen (Resolver) + Prompt an/aus
- [X] T023 [US2] Diagramm-Abgleich: Fokus-/Prompt-Pfad in `diagrams/interaktion-ausloesen.puml` ⇄ E2/E3
- [ ] T024 [US2] ⏳ PlayMode E2/E3 grün; voller Unity-Compile — **ausstehend** (Test Runner); 300-Zeilen ✓

**Checkpoint**: Erkennung + Hinweis funktionieren unabhängig

---

## Phase 5: User Story 3 – Interaktion auslösen (Priority: P2)

**Goal**: Bei Fokus + Interact-Taste `Interact()` genau am fokussierten Objekt; ohne Fokus nichts.

**Independent Test**: Fokus + Taste → Fake-Interactable reagiert (E4); kein Fokus + Taste → keine Reaktion (E5).

### Tests for User Story 3 ⚠️

- [X] T025 [P] [US3] EditMode G1/G2 in `Assets/_Project/Tests/EditMode/InteractionTriggerTests.cs`
- [X] T026 [P] [US3] PlayMode E4/E5 in `FirstPersonInteractionPlayModeTests.cs`

### Implementation for User Story 3

- [X] T027 [P] [US3] `InteractionTrigger.ShouldInteract` in `Assets/_Project/Core/Interaction/InteractionTrigger.cs`
- [X] T028 [US3] `PlayerInteractionController.TryInteract()` (Interact-Input + Gate → `Interact()` am fokussierten Objekt)
- [X] T029 [US3] Diagramm-Abgleich: Auslöse-Pfad in `diagrams/interaktion-ausloesen.puml` ⇄ E4/E5
- [ ] T030 [US3] ⏳ EditMode G1/G2 + PlayMode E4/E5 grün — **ausstehend** (Test Runner); 300-Zeilen ✓

**Checkpoint**: Grundloop „erkennen → handeln" geschlossen

---

## Phase 6: Documentation Sync & Merge Readiness

- [X] T031 Doku-Coverage-Matrix: geänderte Code-Areas ⇄ Doku/Diagramme (ohne `Notizen.md`)
- [X] T032 `CLAUDE.md` Abschnitt „Fortschritt" ergänzt (Spieler begehbar + Interaktion vorhanden)
- [X] T033 [P] Deutsch-Nachweis; PlantUML renderbar/ohne Platzhalter
- [X] T034 Bestätigen: keine Laufzeit-UI erzeugt (FR-008/SC-009) — Presenter blendet nur ein/aus
- [ ] T035 ⏳ Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + alle EditMode-/PlayMode-Tests grün — **ausstehend** (Test Runner; Core bereits via `dotnet build` grün)

---

## Dependencies & Execution Order

- **Setup (Ph1)** → **Foundational (Ph2)** blockiert Stories.
- **US1 (Ph3)**: nach Ph2; unabhängig.
- **US2 (Ph4)**: nach Ph2; nutzt F1-Probe/Selector; `PlayerInteractionController`-Erweiterung baut auf F1.
- **US3 (Ph5)**: nach US2 (braucht aufgelösten Fokus für `Interact()`); `InteractionTrigger` unabhängig.
- **Doc Sync (Ph6)**: nach gewünschten Stories.

### Within each story

- Core-Mathematik vor Runtime-Anwendung; Tests vor/with Implementierung; Diagramm-Abgleich + Compile + 300-Zeilen vor Story-Abschluss.

### Parallel Opportunities

- T009/T010 parallel; T012/T013 parallel; T018/T019 parallel; T025/T027 parallel.

---

## Implementation Strategy

1. Ph1 Setup → 2. Ph2 Foundational → 3. US1 (begehbar, MVP) → validieren → 4. US2 (Erkennung+Hinweis)
→ 5. US3 (Auslösung) → 6. Merge-Readiness. Nach jedem Checkpoint unabhängig validieren.

### Notes

- [P] = andere Datei, keine offene Abhängigkeit. UI editor-authored (kein Laufzeit-`new` für UI).
- F1-Verträge nicht brechen; Erweiterungen additiv.
