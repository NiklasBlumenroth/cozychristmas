---
description: "Task list für F4 – Sortiersystem & Sortierfeedback"
---

# Tasks: F4 – Sortiersystem & Sortierfeedback

**Input**: Design-Dokumente aus `/specs/004-sortiersystem-feedback/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, diagrams/ (vorhanden); contracts/ nicht anwendbar

**Tests**: Verbindlich für Core (`SortTarget`: Klassifizierung, Vollständigkeit/Grenze, falsch hält Lampe
aus, LIFO-Entnahme, Abschluss/Sperre) und kritische E2E-Flows (PlayMode). Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `fach-zustand.puml` (State), `sortier-entscheidung.puml` (Activity), `sortierdaten.puml` (Class)
in der Plan-Phase erstellt; in den Stories nur abgleichen/aktuell halten.

**Organization**: Tasks nach User Stories (US1 Einsortieren, US2 Vollständigkeit/Lampe, US3 sanftes
Fehlerfeedback, US4 Entnahme/Korrektur).

## Format: `[ID] [P?] [Story] Beschreibung`

- **[P]**: parallelisierbar (andere Datei, keine offene Abhängigkeit)
- exakte Dateipfade in jeder Beschreibung

## Path Conventions

Unity-Einzelprojekt, Code additiv unter `Assets/_Project/` (Core/Runtime/Tests/Editor), aufbauend auf F1–F3.

## Umsetzungsstatus (2026-06-02)

- **Erledigt (Code)**: Core `SortKey`/`SortTargetState`/`SortTarget` (US1–US4-Logik inkl. Klassifizierung,
  Vollständigkeit/Grenze, LIFO-Entnahme, Abschluss/Sperre, `JustCompleted`); Runtime `ISortable`/`Sortable`/
  `SortTargetInteractable` (Einsortieren/Entnehmen, Reparenting an Slot, Collider-Toggle, Lampe+Schließen,
  `onCompleted`); `PlayerCarry` additiv (`CanCarry`, `TryHandOverTop`); `PlayerInteractionController`-Routing
  für Fächer; Dev-Tool `DevSpawnMenu` (G/R, IMGUI); Editor `SortingSetup` (verdrahtet Brief/TestPickup/Fach +
  Dev-Spawn). EditMode-Tests `SortTargetTests` (S1–S10).
- **Verifiziert (Compile)**: `dotnet build cozychristmas.sln` – **0 Fehler / 0 Warnungen** (Core, Runtime,
  Tests Edit+Play, Editor, Assembly-CSharp). 300-Zeilen-DoD eingehalten (größte Datei 198 Zeilen).
- **Erledigt (Tests)**: EditMode `SortTargetTests` (S1–S10) – vom Nutzer im Test Runner **grün** ausgeführt.
  PlayMode `SortingPlayModeTests` (E1–E4b: Einsortieren, Vollständigkeit+Lampe+Schließen, Fehlerfeedback,
  Entnahme-LIFO, Überlast-Ablehnung) geschrieben; Compile grün, Testlauf durch den Nutzer.
- **Erledigt (manuell)**: „CozySanta/Setup F4" ausgeführt, Prefabs verdrahtet, Spieltest erfolgreich
  (Einsortieren, Lampe/Abschluss, Entnahme, Stapelrichtung über `stackDirection` einstellbar).
- **Verifiziert (Compile)**: `dotnet build cozychristmas.sln` – **0 Fehler / 0 Warnungen** (inkl. PlayMode-Tests).
  300-Zeilen-DoD eingehalten.
- **Ausstehend**: einmaliger PlayMode-Testlauf von `SortingPlayModeTests` im Test Runner (nur im Editor möglich).
- **Hinweis**: Die generierten `.csproj` wurden für den lokalen Compile-Check temporär um die neuen Dateien
  ergänzt (gitignored); Unity regeneriert sie beim nächsten Editor-Fokus automatisch.

## Doku-Coverage-Matrix (Merge-Readiness)

| Code-Area | Doku/Diagramm | Test |
| --- | --- | --- |
| `Core/Sorting/SortKey` | data-model.md (SortKey), `sortierdaten.puml` | S1 |
| `Core/Sorting/SortTarget` (Place/Remove/Evaluate/Close) | data-model.md, `fach-zustand.puml`, `sortier-entscheidung.puml` | S2–S10 |
| `Runtime/Sorting/Sortable` + `ISortable` | data-model.md, `sortierdaten.puml` | E1/E3 (Klassifizierung) |
| `Runtime/Sorting/SortTargetInteractable` (Einsortieren/Entnehmen/Lampe/Schließen) | data-model.md, `sortier-entscheidung.puml` | E1–E4b |
| `PlayerCarry` (CanCarry/TryHandOverTop) | data-model.md (Routing) | E1/E4b |
| `PlayerInteractionController` (Fach-Routing) | plan.md (Structure Decision), data-model.md | E1 (E2E) |
| `DevTools/DevSpawnMenu` (G/R) | CLAUDE.md (Fortschritt), quickstart.md | manuell |
| `Editor/SortingSetup` | quickstart.md (Setup-Menü) | manuell |

`Notizen.md` ist gemäß Constitution I von der Coverage ausgenommen. Keine Laufzeit-UI (Dev-Tool via IMGUI,
Lampe editor-authored). PlantUML ohne Platzhalter, mit Testkandidaten verknüpft.

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Change-Typ „Planned feature" bestätigt (spec/plan)
- [ ] T002 Doku-Impact prüfen: `CLAUDE.md` (Fortschritt) + Spec-Artefakte; `Notizen.md` ausgenommen
- [ ] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation sicherstellen
- [ ] T004 [P] Diagrammauswahl verifizieren (State + Activity + Class vorhanden; Sequence/Mindmap begründet ausgelassen)

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: blockiert alle Stories

- [ ] T005 Ordner `Assets/_Project/Core/Sorting/`, `Assets/_Project/Runtime/Sorting/` anlegen
- [ ] T006 Testkandidaten S1–S10, E1–E4 aus `data-model.md` als verbindlich markieren
- [ ] T007 Routing-Entscheidung bestätigen: ein fokussiertes `SortTargetInteractable` (kein `IPickup`) durchläuft den bestehenden `IInteractable.Interact()`-Pfad; das Fach greift selbst auf `PlayerCarry` zu (kein Bruch des F2/F3-Vertrags)
- [ ] T008 UI-/Physik-/Probe-Politik bestätigen: keine Laufzeit-UI; eingelegte Objekte ohne aktive Interaktions-Collider (schützt `PhysicsInteractionProbe`-Puffer); Lampe editor-authored, disabled by default

**Checkpoint**: Struktur/Vorgaben stehen; Stories können beginnen

---

## Phase 3: User Story 1 – Objekt korrekt einsortieren (Priority: P1) 🎯 MVP

**Goal**: Oberstes getragenes Objekt per Fach-Interact übergeben; es liegt sichtbar im Fach und wird korrekt gezählt.

**Independent Test**: Passendes Objekt tragen, Fach anschauen + Interact → Objekt im Fach, Hand leer, Korrekt-Zähler +1 (E1).

### Tests for User Story 1 ⚠️

- [ ] T009 [P] [US1] EditMode S1 (`SortKey`-Gleichheit) + S2 (`Classify` korrekt/falsch) in `Assets/_Project/Tests/EditMode/SortTargetTests.cs`
- [ ] T010 [P] [US1] EditMode S3 (`TryPlace` legt oben auf, Zähler konsistent) in `SortTargetTests.cs`
- [ ] T011 [P] [US1] PlayMode E1 (Einsortieren→im Fach, Hand leer) in `Assets/_Project/Tests/PlayMode/SortingPlayModeTests.cs`

### Implementation for User Story 1

- [ ] T012 [P] [US1] `SortKey` (readonly struct, `IEquatable`, `Matches`/Wertegleichheit) in `Assets/_Project/Core/Sorting/SortKey.cs`
- [ ] T013 [P] [US1] `SortTargetState` (enum) in `Assets/_Project/Core/Sorting/SortTargetState.cs`
- [ ] T014 [US1] `SortTarget` (Accepted/RequiredCount, Counts, `Classify`, `TryPlace`, `Evaluate`, Invarianten) in `Assets/_Project/Core/Sorting/SortTarget.cs`
- [ ] T015 [P] [US1] `ISortable` (`SortKey Key`) in `Assets/_Project/Runtime/Sorting/ISortable.cs`
- [ ] T016 [P] [US1] `Sortable : MonoBehaviour, ISortable` (`facets`→`Key`, ergänzt F3-Pickup) in `Assets/_Project/Runtime/Sorting/Sortable.cs`
- [ ] T017 [US1] `SortTargetInteractable : MonoBehaviour, IInteractable` Grundgerüst (Config→`SortTarget` in `Awake`, `slotAnchor`, `Interact()`-Einsortier-Zweig: `TryPlace` + Reparent an `slotAnchor` + Interaktions-Collider aus) in `Assets/_Project/Runtime/Sorting/SortTargetInteractable.cs`
- [ ] T018 [US1] `PlayerInteractionController` (F2) so prüfen/erweitern, dass das fokussierte Fach `Interact()` erhält und das Fach Zugriff auf `PlayerCarry` hat (Editor-Referenz/Resolver)
- [ ] T019 [US1] ⏳ EditMode S1–S3 + PlayMode E1 grün — Test Runner; Core via `dotnet build` grün, 300-Zeilen ok

**Checkpoint**: Einsortieren funktioniert; US1 unabhängig testbar

---

## Phase 4: User Story 2 – Vollständigkeit & Lampenfeedback (Priority: P1)

**Goal**: `RequiredCount` korrekte + 0 falsche → Lampe an, Fach schließt, eingelegte Visuals entfernt; Abschluss-Ereignis.

**Independent Test**: Fach mit `RequiredCount` passenden Objekten füllen → Lampe an, Fach geschlossen, Visuals weg (E2).

### Tests for User Story 2 ⚠️

- [ ] T020 [P] [US2] EditMode S4 (Vollständigkeit an Grenze) + S6 (Teilweise) in `SortTargetTests.cs`
- [ ] T021 [P] [US2] EditMode S9 (Sperre nach Abschluss) + S10 (`JustCompleted` nur im Abschluss-Schritt) in `SortTargetTests.cs`
- [ ] T022 [P] [US2] PlayMode E2 (Lampe an + Fach geschlossen + Visuals entfernt) in `SortingPlayModeTests.cs`

### Implementation for User Story 2

- [ ] T023 [US2] `SortTarget`: Abschlusslogik (`IsClosed`, `JustCompleted`, Sperre in `TryPlace`/`TryRemoveTop`) in `Assets/_Project/Core/Sorting/SortTarget.cs`
- [ ] T024 [US2] `SortTargetInteractable`: Apply bei Abschluss (Lampe an via `Light.enabled`/`GameObject.SetActive`, `onCompleted` auslösen, Fach schließen: eingelegte Visuals entfernen/verstecken + Fach-Collider aus) in `Assets/_Project/Runtime/Sorting/SortTargetInteractable.cs`
- [ ] T025 [US2] `onCompleted : UnityEvent` als Andockpunkt für XP (F6) ergänzen; F4 vergibt selbst keine XP
- [ ] T026 [US2] Diagramm-Abgleich: State-Abschluss/Sperre in `diagrams/fach-zustand.puml` + Activity-Abschluss in `diagrams/sortier-entscheidung.puml` ⇄ S4/S9/S10/E2
- [ ] T027 [US2] ⏳ EditMode S4/S6/S9/S10 + PlayMode E2 grün — Test Runner; 300-Zeilen ok

**Checkpoint**: Vollständigkeit + Lampe + Schließen funktionieren unabhängig

---

## Phase 5: User Story 3 – Sanftes Fehlerfeedback (Priority: P2)

**Goal**: Falsches Objekt bleibt im Fach, wird als falsch gezählt, Lampe bleibt aus; kein Abschluss.

**Independent Test**: Nicht passendes Objekt einsortieren → liegt im Fach, Falsch-Zähler +1, Lampe aus, auch bei sonst erreichter Soll-Menge (E3).

### Tests for User Story 3 ⚠️

- [ ] T028 [P] [US3] EditMode S5 (`RequiredCount` korrekte + ≥1 falsch → `FalschEnthalten`, nicht abgeschlossen) in `SortTargetTests.cs`
- [ ] T029 [P] [US3] PlayMode E3 (falsches Objekt bleibt, Lampe bleibt aus) in `SortingPlayModeTests.cs`

### Implementation for User Story 3

- [ ] T030 [US3] Sicherstellen: `SortTarget.TryPlace` bei falschem Key zählt `WrongCount` hoch, hält `Evaluate` von `Vollstaendig` fern; kein harter Auswurf (Abgrenzung F12)
- [ ] T031 [US3] `SortTargetInteractable`: falsch eingelegtes Objekt bleibt sichtbar im Fach (Reparent an `slotAnchor`, Collider aus), Lampe bleibt aus
- [ ] T032 [US3] Diagramm-Abgleich: `FalschEnthalten`-Pfad in `diagrams/fach-zustand.puml` + Falsch-Zweig in `diagrams/sortier-entscheidung.puml` ⇄ S5/E3
- [ ] T033 [US3] ⏳ EditMode S5 + PlayMode E3 grün — Test Runner; Core via `dotnet build` grün

**Checkpoint**: Sanftes Fehlerfeedback greift; Lampe bleibt bei Fehlern aus

---

## Phase 6: User Story 4 – Entnehmen/Korrigieren (Priority: P2)

**Goal**: Eingelegte Objekte über das Fach LIFO zurück in die Hand entnehmen (Kapazität beachten), um Fehler zu korrigieren.

**Independent Test**: Zwei Objekte einlegen, zweimal entnehmen → kehren in umgekehrter Reihenfolge zurück; bei Überlast abgelehnt (E4).

### Tests for User Story 4 ⚠️

- [ ] T034 [P] [US4] EditMode S7 (LIFO-Entnahme, Zähler sinken) + S8 (Entnahme leeres Fach → false) in `SortTargetTests.cs`
- [ ] T035 [P] [US4] PlayMode E4 (Entnahme zurück in Hand, Überlast abgelehnt) in `SortingPlayModeTests.cs`

### Implementation for User Story 4

- [ ] T036 [US4] `SortTarget.TryRemoveTop(out id)` (LIFO, Sperre wenn `IsClosed`, false bei leer) in `Assets/_Project/Core/Sorting/SortTarget.cs`
- [ ] T037 [US4] `SortTargetInteractable`: `Interact()`-Entnahme-Zweig (Hand leer & Fach offen & gefüllt → `TryRemoveTop` → Objekt zurück an `PlayerCarry` mit Kapazitätsprüfung; bei Überlast abgelehnt, Objekt bleibt; Interaktions-Collider reaktivieren)
- [ ] T038 [US4] Kontextabhängiger `PromptText` (Hand voll → „Einsortieren" / Hand leer & gefüllt → „Entnehmen"); abgeschlossenes Fach ohne Hinweis/gesperrt
- [ ] T039 [US4] Diagramm-Abgleich: Entnahme-Zweig in `diagrams/sortier-entscheidung.puml` + LIFO-Übergänge in `diagrams/fach-zustand.puml` ⇄ S7/S8/E4
- [ ] T040 [US4] ⏳ EditMode S7/S8 + PlayMode E4 grün — Test Runner; 300-Zeilen ok

**Checkpoint**: Entnahme/Korrektur funktioniert; voller Sortier-Korrektur-Loop vollständig

---

## Phase 6b: Test-Tooling (Fallback-Prefabs + Setup)

**Purpose**: schneller manueller Spieltest; Grey-Box-Platzhalter, falls keine eigenen Meshes vorliegen

- [ ] T041 Editor-Menü „CozySanta/Setup F4 (Sortieren + Test-Fach)" in `Assets/_Project/Editor/SortingSetup.cs`: erzeugt via `PrefabUtility` (a) sortierbares Objekt (Cube/flache Box + Rigidbody + Collider + `PickupInteractable` + `Sortable`), (b) Fach-Prefab (Root Pivot Öffnungsmitte + `SortTargetInteractable` + `BoxCollider` + `SlotAnchor` + `Lampe` als Emissive-Mesh disabled), (c) optionales Rahmen-Gitter mit Beispiel-`acceptedFacets`/`requiredCount`
- [ ] T042 ⏳ Prefab-Ordner `Assets/_Project/Prefabs/` um `SortFach.prefab` + `SortItem.prefab` ergänzen — **entsteht beim Ausführen des Menüs** im Editor (oder vom Ersteller manuell gemäß `quickstart.md`)
- [ ] T043 [P] Optionale Editor-Validierung: Hinweis, wenn Rahmen-Beschriftung und Fach-`acceptedFacets` inkonsistent wirken (Content-Schutz)

**Checkpoint**: Fach + sortierbares Objekt manuell testbar (Einsortieren/Lampe/Entnahme)

---

## Phase 7: Documentation Sync & Merge Readiness

- [ ] T044 Doku-Coverage-Matrix: geänderte Code-Areas ⇄ Doku/Diagramme (ohne `Notizen.md`)
- [ ] T045 `CLAUDE.md` „Fortschritt" um F4 ergänzen (F4 von „geplant" auf „in Arbeit/abgeschlossen")
- [ ] T046 [P] Deutsch-Nachweis; PlantUML renderbar/ohne Platzhalter
- [ ] T047 Bestätigen: keine Laufzeit-UI; eingelegte Objekte ohne aktive Interaktions-Collider; Lampe editor-authored
- [ ] T048 ⏳ Finaler Compile-Check (0 Fehler) + 300-Zeilen-DoD + alle EditMode-/PlayMode-Tests grün — Test Runner; Core via `dotnet build` grün

---

## Dependencies & Execution Order

- **Setup (Ph1)** → **Foundational (Ph2)** blockiert Stories.
- **US1 (Ph3)**: nach Ph2; `SortKey`/`SortTarget`(Place)/`SortTargetInteractable`(Einsortieren) + Routing.
- **US2 (Ph4)**: nach US1 (braucht `TryPlace`/`Evaluate`); ergänzt Abschluss/Sperre/Lampe/Schließen.
- **US3 (Ph5)**: nach US1 (Klassifizierung existiert); schärft Falsch-Pfad + Lampe-aus.
- **US4 (Ph6)**: nach US1 (Einlagen existieren); ergänzt `TryRemoveTop` + Entnahme-Apply/Kapazität.
- **Tooling (Ph6b)**: parallel ab US1 nutzbar; finalisiert nach US2/US4.
- **Doc Sync (Ph7)**: nach gewünschten Stories.

### Within each story

- Core (`SortTarget`) vor Runtime-Anwendung; Tests vor/with Implementierung; Diagramm-Abgleich + Compile + 300-Zeilen vor Story-Abschluss.

### Parallel Opportunities

- T009/T010/T011 parallel; T012/T013/T015/T016 parallel; T020/T021/T022 parallel; T028/T029 parallel; T034/T035 parallel.

---

## Implementation Strategy

1. Ph1 Setup → 2. Ph2 Foundational → 3. US1 (Einsortieren, MVP) → validieren → 4. US2 (Vollständigkeit/Lampe)
→ 5. US3 (Fehlerfeedback) → 6. US4 (Entnahme/Korrektur) → 6b Tooling → 7. Merge-Readiness.
Nach jedem Checkpoint unabhängig validieren.

### Notes

- [P] = andere Datei, keine offene Abhängigkeit. Keine Laufzeit-UI; Lampe/Beschriftung editor-authored.
- F1–F3-Verträge nicht brechen; Erweiterungen additiv (`IInteractable` unverändert, `ISortable`/`Sortable`/
  `SortTargetInteractable` neu; `PlayerCarry`/`CarryStack` unverändert, nur konsumiert).
- Eingelegte Objekte: sichtbares Mesh ohne Interaktions-Collider; Entnahme über das Fach (LIFO); abgeschlossene
  Fächer entfernen ihre Visuals (Core hält Zählstand).
