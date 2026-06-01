---
description: "Task list für F3 – Trag-, Hand- & Gewichtssystem"
---

# Tasks: F3 – Trag-, Hand- & Gewichtssystem

**Input**: Design-Dokumente aus `/specs/003-trag-hand-gewicht/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, diagrams/ (vorhanden); contracts/ nicht anwendbar

**Tests**: Verbindlich für Core (`CarryStack`: LIFO, Kapazität, Ablege-Reihenfolge) und kritische
E2E-Flows (PlayMode). Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `trag-stapel-zustand.puml` (State), `aufnehmen-entscheidung.puml` (Activity),
`tragdaten.puml` (Class) in der Plan-Phase erstellt; in den Stories nur abgleichen/aktuell halten.

**Organization**: Tasks nach User Stories (US1 Aufnehmen/Tragen, US2 Stapel-LIFO, US3 Gewicht).

## Format: `[ID] [P?] [Story] Beschreibung`

- **[P]**: parallelisierbar (andere Datei, keine offene Abhängigkeit)
- exakte Dateipfade in jeder Beschreibung

## Path Conventions

Unity-Einzelprojekt, Code additiv unter `Assets/_Project/` (Core/Runtime/Tests), aufbauend auf F1/F2.

## Umsetzungsstatus (2026-06-01)

- **Erledigt (Code)**: Core `CarryItem`/`CarryStack`; Runtime `IPickup`, `PickupInteractable`,
  `PlayerCarry` (Links/Rechts-Anker + `RelayoutHands`, `Capacity`/`CarriedCount`/`SetAnchors`),
  `TestPickupSpawner`; `PlayerInteractionController` (Pickup-Routing) + `PlayerInputRelay`
  (`Q`-Ablegen) erweitert; Editor-Tool `CarrySetup`; EditMode- (C1–C6) + PlayMode-Tests (E1–E4).
- **Verifiziert (Core)**: `dotnet build` (netstandard2.1) – **0 Fehler / 0 Warnungen**, 300-Zeilen-DoD ok.
- **Ausstehend (offener Editor / nur im Editor möglich)**: EditMode-/PlayMode-Testlauf, das
  `TestPickup.prefab` (entsteht beim Ausführen des Menüs „CozySanta/Setup F3 (Carry + Test Pickup)"),
  manueller Spieltest (O/E/Q). `[~]` = teilweise verifiziert.

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Change-Typ „Planned feature" bestätigt (spec/plan)
- [X] T002 Doku-Impact prüfen: `CLAUDE.md` (Fortschritt) + Spec-Artefakte; `Notizen.md` ausgenommen
- [X] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation sicherstellen
- [X] T004 [P] Diagrammauswahl verifizieren (State + Activity + Class vorhanden; Sequence/Mindmap begründet ausgelassen)

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: blockiert alle Stories

- [X] T005 Ordner `Assets/_Project/Core/Carry/`, `Assets/_Project/Runtime/Carry/` anlegen
- [X] T006 Testkandidaten C1–C6, E1–E4 aus `data-model.md` als verbindlich markieren
- [X] T007 Entscheidung bestätigen: `PlayerInteractionController` (F2) routet fokussierte `IPickup` an `PlayerCarry` (kein Bruch des F2-Interact-Verhaltens für sonstige `IInteractable`)
- [X] T008 UI-/Physik-Politik bestätigen: keine Laufzeit-UI; getragene Objekte mit ruhender Physik (Collider aus, Rigidbody kinematic)

**Checkpoint**: Struktur/Vorgaben stehen; Stories können beginnen

---

## Phase 3: User Story 1 – Aufnehmen & Tragen (Priority: P1) 🎯 MVP

**Goal**: Fokussiertes `IPickup` per Interact aufnehmen; Objekt liegt sichtbar in der Hand und wird mitgeführt.

**Independent Test**: Leichtes Objekt anschauen + Interact → Objekt in Hand, nicht mehr am alten Platz (E1).

### Tests for User Story 1 ⚠️

- [X] T009 [P] [US1] EditMode C1 (Push unter Kapazität) in `Assets/_Project/Tests/EditMode/CarryStackTests.cs`
- [X] T010 [P] [US1] PlayMode E1 (Aufnehmen→in Hand) in `Assets/_Project/Tests/PlayMode/CarryPlayModeTests.cs`

### Implementation for User Story 1

- [X] T011 [P] [US1] `CarryItem` (readonly struct) in `Assets/_Project/Core/Carry/CarryItem.cs`
- [X] T012 [US1] `CarryStack` (Capacity, TotalWeight, Count, CanPickUp, TryPush/TryPop/TryPeek) in `Assets/_Project/Core/Carry/CarryStack.cs`
- [X] T013 [P] [US1] `IPickup` in `Assets/_Project/Runtime/Carry/IPickup.cs`
- [X] T014 [P] [US1] `PickupInteractable : MonoBehaviour, IInteractable, IPickup` in `Assets/_Project/Runtime/Carry/PickupInteractable.cs`
- [X] T015 [US1] `PlayerCarry` (CarryStack-Halter, `leftHandAnchor`/`rightHandAnchor`, `TryPickup`: Physik aus + Id→Objekt + `RelayoutHands`) in `Assets/_Project/Runtime/Carry/PlayerCarry.cs`
- [X] T016 [US1] `PlayerInteractionController` (F2) um `carry`-Feld + Routing fokussierter `IPickup` an `PlayerCarry.TryPickup` erweitert
- [ ] T017 [US1] ⏳ EditMode C1 + PlayMode E1 grün — **ausstehend** (Test Runner); Core via `dotnet build` grün, 300-Zeilen ok

**Checkpoint**: Aufnehmen/Tragen funktioniert; US1 unabhängig testbar

---

## Phase 4: User Story 2 – Stapel-LIFO & Ablegen (Priority: P1)

**Goal**: Mehrere Objekte stapeln (zuletzt aufgenommen oben); Ablegen entnimmt in umgekehrter Reihenfolge.

**Independent Test**: A,B,C aufnehmen → C oben; ablegen → C,B,A zurück (E2/E3).

### Tests for User Story 2 ⚠️

- [X] T018 [P] [US2] EditMode C4 (LIFO Peek/Pop-Reihenfolge) + C5 (leeres Pop) in `CarryStackTests.cs`
- [X] T019 [P] [US2] PlayMode E2 (Stapeln) + E3 (Ablegen LIFO, wieder in Welt) in `CarryPlayModeTests.cs`

### Implementation for User Story 2

- [X] T020 [US2] `PlayerCarry.Drop()` (TryPop → Objekt aus Dictionary, reparent in Welt vor Spieler, Physik aktiv) in `Assets/_Project/Runtime/Carry/PlayerCarry.cs`
- [X] T021 [US2] `PlayerInputRelay` (F2) um `carry`-Referenz + `Keyboard.current.qKey`-Lese (Update) → `PlayerCarry.Drop()` erweitert in `Assets/_Project/Runtime/Player/PlayerInputRelay.cs` (formale Drop-Action später)
- [X] T023 [US2] Links/Rechts-Layout (`RelayoutHands`): zuletzt aufgenommenes am `leftHandAnchor`, übrige gestapelt am `rightHandAnchor` (Index-Offset); Re-Layout nach jedem Pickup/Drop
- [X] T024 [US2] Diagramm-Abgleich: State-Übergänge in `diagrams/trag-stapel-zustand.puml` ⇄ C4/E2/E3
- [ ] T025 [US2] ⏳ EditMode C4/C5 + PlayMode E2/E3 grün — **ausstehend** (Test Runner); 300-Zeilen ok

**Checkpoint**: Stapeln und Ablegen (LIFO) funktionieren unabhängig

---

## Phase 5: User Story 3 – Gewichtsgrenze (Priority: P2)

**Goal**: Aufnahme nur, wenn getragene Last + Objektgewicht ≤ Traglast (Grenze inklusive).

**Independent Test**: Schweres Objekt bei kleiner Kapazität → keine Aufnahme (E4); mit großer Kapazität → Aufnahme.

### Tests for User Story 3 ⚠️

- [X] T026 [P] [US3] EditMode C2 (==Grenze erlaubt), C3 (über → abgelehnt), C6 (Capacity erhöht → aufnehmbar) in `CarryStackTests.cs`
- [X] T027 [P] [US3] PlayMode E4 (Überlast bleibt in Welt) in `CarryPlayModeTests.cs`

### Implementation for User Story 3

- [X] T028 [US3] Sicherstellen, dass `PlayerCarry.TryPickup` die Ablehnung sauber behandelt (Objekt bleibt in der Welt, kein Reparent/keine Dictionary-Eintragung)
- [X] T029 [US3] `Capacity` als SerializeField + öffentliches Property am `PlayerCarry` (Startwert klein; Andockpunkt für F6-Tragkraft)
- [X] T030 [US3] Diagramm-Abgleich: Gewichts-Branch in `diagrams/aufnehmen-entscheidung.puml` ⇄ C2/C3/C6/E4
- [ ] T031 [US3] ⏳ EditMode C2/C3/C6 + PlayMode E4 grün — **ausstehend** (Test Runner); Core via `dotnet build` grün

**Checkpoint**: Gewichtssystem greift; Grundloop Aufnehmen/Stapeln/Ablegen/Überlast vollständig

---

## Phase 5b: Test-Tooling (Prefab + Spawner)

**Purpose**: schneller manueller Spieltest

- [X] T032 `TestPickupSpawner` (`Keyboard.current.oKey` → Prefab vor dem Spieler instanziieren) in `Assets/_Project/Runtime/Carry/TestPickupSpawner.cs`
- [X] T033 Editor-Menü „CozySanta/Setup F3 (Carry + Test Pickup)" in `Assets/_Project/Editor/CarrySetup.cs`: PlayerCarry + LeftHandAnchor (unter Kamera) + RightHandAnchor (unter Player) + carry an Controller; TestPickup-Prefab (Cube + Rigidbody[Schwerkraft] + Collider + PickupInteractable) via `PrefabUtility` erzeugen; Spawner ergänzen
- [ ] T034 ⏳ Prefab-Ordner `Assets/_Project/Prefabs/` + `TestPickup.prefab` — **entsteht beim Ausführen des Menüs** im Editor

**Checkpoint**: „O" spawnt ein fallendes Pickup; Aufnehmen/Stapeln/Ablegen manuell testbar

---

## Phase 6: Documentation Sync & Merge Readiness

- [X] T035 Doku-Coverage-Matrix: geänderte Code-Areas ⇄ Doku/Diagramme (ohne `Notizen.md`)
- [X] T036 `CLAUDE.md` „Fortschritt" um F3 ergänzt
- [X] T037 [P] Deutsch-Nachweis; PlantUML renderbar/ohne Platzhalter
- [X] T038 Bestätigen: keine Laufzeit-UI; getragene Objekte mit ruhender Physik
- [ ] T039 ⏳ Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + alle EditMode-/PlayMode-Tests grün — **ausstehend** (Test Runner; Core bereits via `dotnet build` grün)

---

## Dependencies & Execution Order

- **Setup (Ph1)** → **Foundational (Ph2)** blockiert Stories.
- **US1 (Ph3)**: nach Ph2; baut auf F2-Interact-Routing.
- **US2 (Ph4)**: nach US1 (braucht `PlayerCarry`/Stapel); Drop-Action + Relay.
- **US3 (Ph5)**: Core-Gewicht (`CanPickUp`) ist Teil von `CarryStack` (US1), die Story prüft/verschärft die Ablehnung; nach US1.
- **Doc Sync (Ph6)**: nach gewünschten Stories.

### Within each story

- Core (`CarryStack`) vor Runtime-Anwendung; Tests vor/with Implementierung; Diagramm-Abgleich + Compile + 300-Zeilen vor Story-Abschluss.

### Parallel Opportunities

- T009/T010 parallel; T011/T013/T014 parallel; T018/T019 parallel; T026/T027 parallel.

---

## Implementation Strategy

1. Ph1 Setup → 2. Ph2 Foundational → 3. US1 (Aufnehmen/Tragen, MVP) → validieren → 4. US2 (Stapel/Ablegen)
→ 5. US3 (Gewicht) → 6. Merge-Readiness. Nach jedem Checkpoint unabhängig validieren.

### Notes

- [P] = andere Datei, keine offene Abhängigkeit. Keine Laufzeit-UI; getragene Objekte = 3D an HandAnchor.
- F1/F2-Verträge nicht brechen; Erweiterungen additiv (`IInteractable` unverändert, `IPickup` neu).
