# Implementation Plan: F2 – First-Person-Controller & Interaktionssystem

**Branch**: `002-first-person-controller` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-first-person-controller/spec.md`

## Summary

F2 macht das Spiel erstmals begehbar: First-Person-Bewegung (WASD über `CharacterController`),
Maus-Blick (Yaw am Körper, Pitch begrenzt an der Kamera) und blick-/reichweitenbasierte Interaktion.
Die Zielerkennung nutzt unverändert die F1-Entscheidung `InteractionSelector.Decide` mit Kandidaten
aus einem `IInteractionProbe`; F2 ergänzt die Auflösung des Fokusziels auf ein konkretes
`IInteractable`, einen dezenten (editor-authored) Hinweis und das Auslösen von `Interact`. Neue
Entscheidungslogik (Pitch-Clamp, Bewegungsvektor, Interaktions-Gating) liegt als reine, getestete
Core-Mathematik vor; Unity wendet nur die Seiteneffekte an.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Status/MVP-Fokus); neue Spec-Artefakte unter `specs/002-first-person-controller/`
**Diagram Impact**: `diagrams/interaktion-ausloesen.puml` (Sequence), `diagrams/interaktion-komponenten.puml` (Class)
**Working Language**: German (mandatory for docs and project communication)

## Technical Context

**Language/Version**: C# (Unity 6000.2.7f2; Core gegen netstandard2.1, `noEngineReferences`)
**Primary Dependencies**: Unity 6.2, URP 17.2, Input System 1.14.2, Unity Test Framework 1.6; baut auf `CozySanta.Core`/`CozySanta.Runtime` (F1)
**Storage**: N/A (Persistenz erst F14)
**Testing**: EditMode (Core-Mathematik, Interaktions-Gating, Decide) + PlayMode (Bewegung/Blick/Interaktion E2E in Grey-Box-Szene)
**Target Platform**: PC (Windows), First-Person
**Project Type**: single (Unity-Spielprojekt)
**Performance Goals**: 60 fps; Eingabe-/Bewegungslogik pro Frame ohne Allokationen im Hotpath
**Constraints**: Core ohne UnityEngine; ≤300 Zeilen je Datei; UI nur editor-authored (keine Laufzeit-Erzeugung)
**Scale/Scope**: 1 Controller, 1 Interaktions-Orchestrierung, 1 Prompt-Presenter, 3 reine Core-Funktionen, 1 Test-Interactable
**Integration Touchpoints**: F1 `InteractionSelector`/`IInteractionProbe`/`PhysicsInteractionProbe`/`PlayerInteractionController`; Input System Map „Player"; URP-Kamera
**PlantUML Documentation**: `diagrams/interaktion-ausloesen.puml` (Sequence), `diagrams/interaktion-komponenten.puml` (Class)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design and before implementation.*

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined for all impacted project areas (Notizen ausgenommen)
- [x] Whole-system impact and collision analysis documented (Erweiterung der F1-Fokus-Schnittstelle, keine widersprüchliche Definition)
- [x] PlantUML diagram selection completed (Sequence + Class; Activity/State/Mindmap mit Begründung „Not required")
- [x] Definitions/requirements updated where collisions exist (Fokus-Auflösung vorab definiert)
- [x] Compile-validation command(s) defined for this scope (siehe „Compile- & Teststrategie")
- [x] Architekturtrennung geplant: Pitch-Clamp/Bewegung/Gating als Core-Logik ohne Unity
- [x] Decide/Apply-Trennung dokumentiert (Decide: Core-Mathematik + Gating; Apply: Move/Rotate/UI-Toggle/Interact)
- [x] Teststrategie definiert (EditMode für Mathematik/Gating, PlayMode für E2E)
- [x] Diagram-derived test candidates documented (siehe data-model.md)
- [x] DoD baseline covers per-story/increment compile-checks, 300-line class-size, Unit-/Regressionstests
- [x] UI approach confirms editor-authored UI only (Prompt-Presenter blendet vorab erstelltes UI nur ein/aus)
- [x] Documentation split analysis completed (kein Doc übergroß)
- [x] Diagram sync and freshness checks planned for PR review
- [x] Documentation and project communication are in German

→ **Gate bestanden.** Keine Verletzungen; Complexity Tracking bleibt leer.

## Compile- & Teststrategie

- **Compile-Check**: Build im Unity-Editor bzw. `dotnet build Assembly-CSharp.csproj` nach Reimport; Core zusätzlich eigenständig via `dotnet build` (netstandard2.1).
- **Tests**: EditMode für `LookMath.ClampPitch`, `MovementCalculator.ComputeDisplacement`, `InteractionTrigger.ShouldInteract`; PlayMode für Bewegung gegen Collider, Fokuswechsel + Prompt-Zustand, Interaktionsauslösung mit Fake-Interactable.
- **UI-Politik**: Kein `new GameObject`/`AddComponent` für UI im Gameplay-Code. Der `InteractionPromptPresenter` referenziert ein per Editor erstelltes Hinweis-Objekt (SerializeField) und ruft nur `SetActive`/Textbindung.

## Project Structure

### Documentation (this feature)

```text
specs/002-first-person-controller/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── diagrams/
│   ├── interaktion-ausloesen.puml      # Sequence
│   └── interaktion-komponenten.puml    # Class
├── checklists/requirements.md          # aus /speckit.specify
└── tasks.md                            # /speckit.tasks
```

### Source Code (repository root)

```text
Assets/_Project/
├── Core/
│   ├── Player/
│   │   ├── LookMath.cs                  # Pitch-Clamp (pure)
│   │   └── MovementCalculator.cs        # Bewegungsvektor aus Input/Basisvektoren (pure)
│   └── Interaction/
│       ├── InteractionTrigger.cs        # ShouldInteract(hasFocus, pressed) (pure)
│       └── IInteractionProbe.cs         # (F1) – wird um Interactable-Referenz erweitert (siehe data-model)
├── Runtime/
│   ├── Player/
│   │   └── FirstPersonController.cs      # Input lesen, Core rufen, CharacterController/Kamera anwenden
│   └── Interaction/
│       ├── IInteractable.cs              # Vertrag interagierbarer Objekte (Runtime, da Unity-nah)
│       ├── InteractionPromptPresenter.cs # bindet Fokus an editor-authored UI (nur Toggle/Bind)
│       ├── PhysicsInteractionProbe.cs    # (F1) – liefert Interactable je Kandidat
│       └── PlayerInteractionController.cs# (F1) – ergänzt Auflösung TargetId→IInteractable + Interact
└── Tests/
    ├── EditMode/
    │   ├── LookMathTests.cs
    │   ├── MovementCalculatorTests.cs
    │   └── InteractionTriggerTests.cs
    └── PlayMode/
        └── FirstPersonInteractionPlayModeTests.cs
```

**Structure Decision**: Fortführung der F1-Schichtung. Reine Steuer-/Gating-Mathematik kommt nach
`Core/Player` bzw. `Core/Interaction`; Unity-nahe Orchestrierung (CharacterController, Kamera, UI,
`Interact`) nach `Runtime`. `IInteractable` liegt in der Runtime-Schicht, weil es konkrete
Weltobjekte/Seiteneffekte beschreibt; die Auswahlregel bleibt in Core (F1). Bestehende F1-Klassen
werden additiv erweitert, ihre Verträge nicht gebrochen.

## Phase 0 – Research

Siehe [research.md](./research.md): Entscheidungen zu (1) CharacterController vs Rigidbody,
(2) Yaw/Pitch-Aufteilung + Clamp, (3) Input-System-Anbindung (Default-Map trimmen),
(4) Fokus-Auflösung TargetId→IInteractable, (5) editor-authored Prompt-Binding. Keine offenen
`NEEDS CLARIFICATION`.

## Phase 1 – Design & Contracts

- **data-model.md**: Core-Funktionssignaturen (Pitch-Clamp, Bewegung, Gating), `IInteractable`,
  erweiterter Probe-Kandidat (Interactable-Referenz), abgeleitete Testkandidaten.
- **contracts/**: Nicht anwendbar (keine externen API-Verträge; „Verträge" sind die C#-Interfaces in data-model.md).
- **quickstart.md**: Grey-Box-Szene aufsetzen, Player-Prefab, Prompt-UI authoren, Test-Interactable platzieren, Steuerung prüfen.
- **Agent-Context-Update**: `CLAUDE.md` wird im Abschnitt „Status / MVP-Fokus" manuell aktualisiert
  (wie in F1 begründet; das Skript passt nicht zur hand-gepflegten Datei).

## Complexity Tracking

> Keine Constitution-Verletzungen – Tabelle bleibt leer.
