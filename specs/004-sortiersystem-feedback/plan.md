# Implementation Plan: F4 – Sortiersystem & Sortierfeedback

**Branch**: `004-sortiersystem-feedback` | **Date**: 2026-06-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-sortiersystem-feedback/spec.md`

## Summary

F4 ergänzt das Einsortieren getragener Objekte an Zielorte (Fächer) mit Korrektheits- und
Vollständigkeitsprüfung sowie Lampenfeedback. Die reine Sortierlogik (Klassifizierung
korrekt/falsch über einen generischen `SortKey`, Vollständigkeit gegen eine konfigurierbare
Soll-Menge, nächstes LIFO-Entnahme-Objekt, Abschluss/Sperre) liegt als testbare Core-Komponente
(`SortTarget`) ohne Unity vor. In der Runtime ist ein Fach eine `IInteractable`-Implementierung
(`SortTargetInteractable`), deren Interaktion das oberste getragene Objekt (F3-`PlayerCarry`)
übernimmt bzw. – wenn die Hand leer und das Fach offen befüllt ist – ein Objekt LIFO zurück in die
Hand gibt. Apply reparentet das Objekt an einen Slot-Anker, deaktiviert dessen Interaktions-Collider,
schaltet bei Abschluss die editor-authored Lampe und „schließt" das Fach (Visuals/Collider entfernen).
Anbindung erfolgt additiv an F2-Interaktion und F3-Tragen; keine Vertragsbrüche.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Fortschritt); neue Spec-Artefakte unter `specs/004-sortiersystem-feedback/`
**Diagram Impact**: `diagrams/fach-zustand.puml` (State), `diagrams/sortier-entscheidung.puml` (Activity), `diagrams/sortierdaten.puml` (Class)
**Working Language**: German (mandatory for docs and project communication)

## Technical Context

**Language/Version**: C# (Unity 6000.2.7f2; Core gegen netstandard2.1, `noEngineReferences`)
**Primary Dependencies**: Unity 6.2, URP 17.2, Input System 1.14.2, Unity Test Framework 1.6; baut auf F1/F2/F3 (`CozySanta.Core`/`CozySanta.Runtime`, `IInteractable`, `PlayerInteractionController`, `PlayerCarry`)
**Storage**: N/A (Persistenz erst F14; Zählstände leben zur Laufzeit im Core-`SortTarget`)
**Testing**: EditMode (`SortTarget`: Klassifizierung, Vollständigkeit/Grenze, falsch hält Lampe aus, LIFO-Entnahme, Abschluss/Sperre) + PlayMode (Einsortieren→Lampe und Entnahme E2E mit Fakes)
**Target Platform**: PC (Windows), First-Person
**Project Type**: single (Unity-Spielprojekt)
**Performance Goals**: 60 fps; eingelegte Objekte ohne aktive Interaktions-Collider (schützt `PhysicsInteractionProbe`-Puffer); abgeschlossene Fächer entfernen Visuals
**Constraints**: Core ohne UnityEngine; ≤300 Zeilen je Datei; keine Laufzeit-UI (Lampe/Beschriftung editor-authored); F2/F3-Verträge unverändert
**Scale/Scope**: 1 Core-`SortTarget` (+ `SortKey`, Zustand), 1 Runtime-`SortTargetInteractable`, 1 `Sortable`-Komponente, Erweiterung des Pickup-Routings, Fallback-Prefabs (Fach, sortierbares Objekt, optionaler Rahmen)
**Integration Touchpoints**: F2 `IInteractable`/`PlayerInteractionController` (Interact→Fach), F3 `PlayerCarry` (Hand↔Fach-Übergabe, Kapazitätsprüfung bei Entnahme), `CarryStack` (LIFO, unverändert)
**PlantUML Documentation**: State + Activity + Class (siehe Diagram Impact)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design and before implementation.*

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined for all impacted project areas (Notizen ausgenommen)
- [x] Whole-system impact and collision analysis documented (additive `IInteractable`-Implementierung; `PhysicsInteractionProbe`-Pufferkollision aufgelöst via collider-lose Einlagen + Entnahme über das Fach)
- [x] PlantUML diagram selection completed (State + Activity + Class; Sequence/Mindmap mit Begründung „Not required")
- [x] Definitions/requirements updated where collisions exist (Granularität „1 Fach = 1 Zielort/Lampe/Key/Soll-Menge" und Rahmen/Fach-Rollentrennung vorab festgelegt)
- [x] Compile-validation command(s) defined for this scope (siehe „Compile- & Teststrategie")
- [x] Architekturtrennung geplant: Sortier-/Klassifizierungslogik als Core ohne Unity
- [x] Decide/Apply-Trennung dokumentiert (Decide: `SortTarget.Classify/Place/RemoveTop/Evaluate`; Apply: Reparenting/Collider-Toggle/Lampe/Schließen/Hand↔Fach)
- [x] Teststrategie definiert (EditMode für Sortierregeln, PlayMode für E2E)
- [x] Diagram-derived test candidates documented (siehe data-model.md)
- [x] DoD baseline covers per-story/increment compile-checks, 300-line class-size, Unit-/Regressionstests
- [x] UI approach confirms editor-authored UI only (Lampe/Beschriftung im Prefab; Laufzeitcode toggelt nur)
- [x] Documentation split analysis completed (kein Doc übergroß)
- [x] Diagram sync and freshness checks planned for PR review
- [x] Documentation and project communication are in German

→ **Gate bestanden.** Keine Verletzungen; Complexity Tracking bleibt leer.

## Compile- & Teststrategie

- **Compile-Check**: Build im Unity-Editor bzw. `dotnet build Assembly-CSharp.csproj` nach Reimport; Core eigenständig via `dotnet build` (netstandard2.1).
- **Tests**: EditMode für `SortTarget` (Klassifizierung korrekt/falsch, Vollständigkeit an/unter Grenze, falsch hält Lampe aus, LIFO-Entnahme, leeres/abgeschlossenes Fach, Abschluss-Sperre); PlayMode für Einsortieren→Lampe an + Entnahme zurück in die Hand mit Fake-Fach/Fake-Player.
- **UI-Politik**: keine Laufzeit-UI; Lampe und Beschriftung sind editor-authored, Laufzeitcode schaltet nur.

## Project Structure

### Documentation (this feature)

```text
specs/004-sortiersystem-feedback/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── diagrams/
│   ├── fach-zustand.puml             # State
│   ├── sortier-entscheidung.puml     # Activity
│   └── sortierdaten.puml             # Class
├── checklists/requirements.md        # aus /speckit.specify
└── tasks.md                          # /speckit.tasks
```

### Source Code (repository root)

```text
Assets/_Project/
├── Core/
│   └── Sorting/
│       ├── SortKey.cs                # readonly struct: geordnete Facettenwerte, Wertegleichheit (Decide-Daten)
│       ├── SortTargetState.cs        # enum: Leer/Teilweise/FalschEnthalten/Vollstaendig
│       └── SortTarget.cs             # Klassifizierung, Place/RemoveTop (LIFO), Evaluate, Abschluss/Sperre (Decide)
├── Runtime/
│   └── Sorting/
│       ├── ISortable.cs              # liefert SortKey + Component-Identität (analog IPickup)
│       ├── Sortable.cs               # MonoBehaviour: SortKey im Editor authored; ergänzt Pickup
│       └── SortTargetInteractable.cs # IInteractable am Fach; Hand↔Fach, Reparenting/Collider/Lampe/Schließen (Apply)
├── Editor/
│   └── SortingSetup.cs               # Fallback-Prefabs (Fach + sortierbares Objekt) + Szenen-Setup
└── Tests/
    ├── EditMode/
    │   └── SortTargetTests.cs
    └── PlayMode/
        └── SortingPlayModeTests.cs
```

**Structure Decision**: Fortführung der F1–F3-Schichtung. Klassifizierungs-/Vollständigkeits-/
Entnahme-Entscheidungen kommen als reine Logik nach `Core/Sorting`; Unity-nahe Effekte (Reparenting,
Collider-/Visual-Toggle, Lampe, Schließen, Hand↔Fach-Übergabe) nach `Runtime/Sorting`.
`SortTargetInteractable` implementiert das bestehende `IInteractable` additiv. `Sortable` ergänzt das
F3-Pickup um Sortierdaten, ohne `IPickup` zu brechen. Das Pickup-Routing im `PlayerInteractionController`
wird so erweitert, dass ein fokussiertes Fach die Interaktion erhält (Übergabe statt Aufnehmen).

## Phase 0 – Research

Siehe [research.md](./research.md): Entscheidungen zu (1) `SortKey`-Repräsentation (datengetrieben,
Wertegleichheit, skalierbar), (2) Granularität Fach vs. Kasten + Rahmen/Fach-Rollentrennung,
(3) Auflösung der `PhysicsInteractionProbe`-Pufferkollision (collider-lose Einlagen, Entnahme über das
Fach), (4) Einsortieren/Entnehmen-Eingabe (kontextabhängige Interact-Route), (5) Optimierung bei
Abschluss (Schließen + Visual-/Collider-Entfernung, Core hält Zählstand), (6) Abgrenzung zu F6/F9/F12.
Keine offenen `NEEDS CLARIFICATION`.

## Phase 1 – Design & Contracts

- **data-model.md**: `SortKey`, `SortTargetState`, `SortTarget` (Signaturen/Invarianten), `ISortable`,
  `Sortable`, `SortTargetInteractable`-Verantwortung, Routing-Erweiterung, abgeleitete Testkandidaten.
- **contracts/**: Nicht anwendbar (keine externen API-Verträge; „Verträge" sind die C#-Typen in data-model.md).
- **quickstart.md**: Fach-Prefab (Collider/Slot-Anker/Lampe, Pivot mittig), Rahmen + Fach-Gitter,
  sortierbares Objekt (+ `Sortable`), Einsortieren/Vollständigkeit/Lampe und Entnahme prüfen.
- **Agent-Context-Update**: `CLAUDE.md` „Fortschritt" wird (bei Implementierung) manuell um F4 ergänzt.

## Complexity Tracking

> Keine Constitution-Verletzungen – Tabelle bleibt leer.
