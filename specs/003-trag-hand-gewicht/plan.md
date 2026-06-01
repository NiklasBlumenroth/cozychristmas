# Implementation Plan: F3 – Trag-, Hand- & Gewichtssystem

**Branch**: `003-trag-hand-gewicht` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-trag-hand-gewicht/spec.md`

## Summary

F3 ergänzt das Aufnehmen und Tragen von Objekten mit einem LIFO-Tragstapel und einer
Gewichtsgrenze. Die reine Stapel-/Gewichtslogik (aufnehmbar?, nächstes Ablege-Objekt,
Gesamtgewicht) liegt als testbare Core-Komponente (`CarryStack`) ohne Unity vor. In der Runtime
ist ein aufnehmbares Objekt eine `IInteractable`-Implementierung, deren `Interact()` den Player-
Tragstapel füttert; eine Trage-Komponente reparentet das Objekt an Hand-/Stapel-Transforms,
deaktiviert dessen Physik und platziert es beim Ablegen wieder in der Welt. Anbindung erfolgt an
die F2-Interaktion; eine neue „Drop"-Action wird ergänzt.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Fortschritt); neue Spec-Artefakte unter `specs/003-trag-hand-gewicht/`
**Diagram Impact**: `diagrams/trag-stapel-zustand.puml` (State), `diagrams/aufnehmen-entscheidung.puml` (Activity), `diagrams/tragdaten.puml` (Class)
**Working Language**: German (mandatory for docs and project communication)

## Technical Context

**Language/Version**: C# (Unity 6000.2.7f2; Core gegen netstandard2.1, `noEngineReferences`)
**Primary Dependencies**: Unity 6.2, URP 17.2, Input System 1.14.2, Unity Test Framework 1.6; baut auf F1/F2 (`CozySanta.Core`/`CozySanta.Runtime`, `IInteractable`, `PlayerInteractionController`)
**Storage**: N/A (Persistenz erst F14)
**Testing**: EditMode (CarryStack: LIFO, Gewichtskapazität, Ablege-Reihenfolge) + PlayMode (Aufnehmen→Tragen→Ablegen E2E mit Fakes)
**Target Platform**: PC (Windows), First-Person
**Project Type**: single (Unity-Spielprojekt)
**Performance Goals**: 60 fps; Aufnehmen/Ablegen ohne Allokationen im Hotpath (Tick fragt nur)
**Constraints**: Core ohne UnityEngine; ≤300 Zeilen je Datei; keine Laufzeit-UI; getragene Objekte mit ruhender Physik
**Scale/Scope**: 1 Core-Stapel, 1 Trage-Komponente, 1 aufnehmbares Objekt, neue Drop-Action, 1 Drop-Anbindung im Relay
**Integration Touchpoints**: F2 `IInteractable`/`PlayerInteractionController` (Interact→Aufnehmen), `PlayerInputRelay` (Drop), Hand-Transforms am Player
**PlantUML Documentation**: State + Activity + Class (siehe Diagram Impact)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design and before implementation.*

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined for all impacted project areas (Notizen ausgenommen)
- [x] Whole-system impact and collision analysis documented (additive `IInteractable`-Implementierung, kein Vertragsbruch)
- [x] PlantUML diagram selection completed (State + Activity + Class; Sequence/Mindmap mit Begründung „Not required")
- [x] Definitions/requirements updated where collisions exist (Verantwortung „Tragstapel am Player" vorab geklärt)
- [x] Compile-validation command(s) defined for this scope (siehe „Compile- & Teststrategie")
- [x] Architekturtrennung geplant: Stapel-/Gewichtslogik als Core ohne Unity
- [x] Decide/Apply-Trennung dokumentiert (Decide: CarryStack.CanPickUp/Push/Pop; Apply: Reparenting/Physik/Weltplatzierung)
- [x] Teststrategie definiert (EditMode für Stapel/Gewicht, PlayMode für E2E)
- [x] Diagram-derived test candidates documented (siehe data-model.md)
- [x] DoD baseline covers per-story/increment compile-checks, 300-line class-size, Unit-/Regressionstests
- [x] UI approach confirms editor-authored UI only (F3 nutzt 3D-Hand-Transforms, keine Laufzeit-UI)
- [x] Documentation split analysis completed (kein Doc übergroß)
- [x] Diagram sync and freshness checks planned for PR review
- [x] Documentation and project communication are in German

→ **Gate bestanden.** Keine Verletzungen; Complexity Tracking bleibt leer.

## Compile- & Teststrategie

- **Compile-Check**: Build im Unity-Editor bzw. `dotnet build Assembly-CSharp.csproj` nach Reimport; Core eigenständig via `dotnet build` (netstandard2.1).
- **Tests**: EditMode für `CarryStack` (Push/Pop-LIFO, `CanPickUp` an/über/unter Grenze, `TotalWeight`, leeres Pop); PlayMode für Aufnehmen→Tragen→Ablegen mit Fake-Aufnehmbar/Fake-Player.
- **UI-Politik**: keine UI; getragene Objekte sind 3D-GameObjects an Hand-Transforms.

## Project Structure

### Documentation (this feature)

```text
specs/003-trag-hand-gewicht/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── diagrams/
│   ├── trag-stapel-zustand.puml      # State
│   ├── aufnehmen-entscheidung.puml   # Activity
│   └── tragdaten.puml                # Class
├── checklists/requirements.md        # aus /speckit.specify
└── tasks.md                          # /speckit.tasks
```

### Source Code (repository root)

```text
Assets/_Project/
├── Core/
│   └── Carry/
│       ├── CarryItem.cs            # readonly struct: Id, Weight
│       ├── CarryStack.cs           # LIFO-Stapel + Gewichtskapazität (Decide)
│       └── PickupResult.cs         # Ergebnis-/Ablehnungstyp (optional enum)
├── Runtime/
│   ├── Carry/
│   │   ├── PlayerCarry.cs          # hält CarryStack; Reparenting/Physik/Weltplatzierung (Apply)
│   │   └── PickupInteractable.cs   # IInteractable mit Gewicht; Interact() -> PlayerCarry.TryPickup
│   └── Player/
│       └── PlayerInputRelay.cs     # (F2) – ergänzt OnDrop -> PlayerCarry.Drop
└── Tests/
    ├── EditMode/
    │   └── CarryStackTests.cs
    └── PlayMode/
        └── CarryPlayModeTests.cs
```

**Structure Decision**: Fortführung der F1/F2-Schichtung. Die LIFO-/Gewichtsentscheidungen kommen
als reine Logik nach `Core/Carry`; Unity-nahe Effekte (Reparenting, Physik-Toggle, Weltplatzierung,
Hand-Transforms) nach `Runtime/Carry`. `PickupInteractable` implementiert das bestehende
`IInteractable` additiv. Der `PlayerInputRelay` (F2) wird um eine `OnDrop`-Nachricht erweitert.

## Phase 0 – Research

Siehe [research.md](./research.md): Entscheidungen zu (1) Core-Stapel-Datenstruktur & Kapazität,
(2) Aufnehmen-Anbindung an F2-Interact, (3) Tragdarstellung via Reparenting + Physik-Toggle,
(4) Ablegen-Eingabe (neue Drop-Action) und Weltplatzierung. Keine offenen `NEEDS CLARIFICATION`.

## Phase 1 – Design & Contracts

- **data-model.md**: `CarryItem`, `CarryStack` (Signaturen/Invarianten), `IInteractable`-Aufnahme,
  `PlayerCarry`-Verantwortung, abgeleitete Testkandidaten.
- **contracts/**: Nicht anwendbar (keine externen API-Verträge; „Verträge" sind die C#-Typen in data-model.md).
- **quickstart.md**: aufnehmbares Objekt + Hand-Transforms einrichten, Drop-Action ergänzen, Aufnehmen/Stapeln/Ablegen prüfen.
- **Agent-Context-Update**: `CLAUDE.md` „Fortschritt" wird (bei Implementierung) manuell um F3 ergänzt.

## Complexity Tracking

> Keine Constitution-Verletzungen – Tabelle bleibt leer.
