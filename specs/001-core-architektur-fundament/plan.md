# Implementation Plan: F1 – Core-Architektur & Projektgrundgerüst

**Branch**: `001-core-architektur-fundament` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-core-architektur-fundament/spec.md`

## Summary

F1 legt das testbare Architekturfundament für alle weiteren Gameplay-Features. Es etabliert
drei Schichten über Unity Assembly Definitions: eine **Core-Schicht** (`CozySanta.Core`) ohne
Unity-Laufzeitabhängigkeiten (`noEngineReferences`), eine **Runtime-Schicht**
(`CozySanta.Runtime`) für `MonoBehaviour`-Orchestrierung und Provider-Implementierungen, sowie
getrennte **EditMode-/PlayMode-Test-Assemblies**. Das Decide/Apply-Muster und die
Provider-Abstraktion werden an einem minimalen Beispiel-Slice (Interaktions-Zielauswahl)
demonstriert und mit EditMode- und PlayMode-Tests abgesichert.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Abschnitt „Build und Werkzeuge" + neue Struktur/Konvention); neue Spec-Artefakte unter `specs/001-core-architektur-fundament/`
**Diagram Impact**: `diagrams/architektur-schichten.puml` (Class), `diagrams/decide-apply-flow.puml` (Activity)
**Working Language**: German (mandatory for docs and project communication)

## Technical Context

**Language/Version**: C# (Unity 6000.2.7f2; Core gegen .NET Standard 2.1, `noEngineReferences`)
**Primary Dependencies**: Unity 6.2, URP 17.2, Input System 1.14.2, Unity Test Framework 1.6 (NUnit)
**Storage**: N/A (Persistenz erst in F14)
**Testing**: Unity Test Framework – EditMode (Core/Unit) und PlayMode (E2E)
**Target Platform**: PC (Windows), First-Person
**Project Type**: single (Unity-Spielprojekt)
**Performance Goals**: Für F1 nicht relevant (Fundament, kein Laufzeit-Hotpath); Projektziel allgemein 60 fps
**Constraints**: Core-Assembly ohne `UnityEngine`-Referenz; einseitige Abhängigkeit Runtime→Core; ≤300 Zeilen je Klassendatei
**Scale/Scope**: Wenige Fundament-Typen + 1 Beispiel-Slice (Interaktions-Zielauswahl)
**Integration Touchpoints**: Bestehende Unity-Vorlage (`SampleScene`, `TutorialInfo`, URP-Settings, `InputSystem_Actions`, `New Terrain.asset`) bleibt unverändert; additive Ergänzung unter `Assets/_Project/`
**PlantUML Documentation**: `diagrams/architektur-schichten.puml` (Class), `diagrams/decide-apply-flow.puml` (Activity)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design and before implementation.*

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined for all impacted project areas (Notizen ausgenommen)
- [x] Whole-system impact and collision analysis documented (Spec: System Context – keine Kollision)
- [x] PlantUML diagram selection completed (Class + Activity; übrige mit Begründung „Not required")
- [x] Definitions/requirements updated where collisions exist (keine Kollision vorhanden)
- [x] Compile-validation command(s) defined for this scope (siehe „Compile- & Teststrategie")
- [x] Architekturtrennung geplant: Core-Entscheidungslogik testbar ohne direkte Unity-Abhängigkeiten (`noEngineReferences`)
- [x] Decide/Apply-Trennung für kritische Flows dokumentiert (Beispiel-Slice)
- [x] Teststrategie definiert (EditMode/Unit als Standard, PlayMode gezielt für E2E)
- [x] Diagram-derived test candidates documented (siehe data-model.md / Activity-Branches)
- [x] DoD baseline covers per-story/increment compile-checks, 300-line class-size compliance, Unit-Tests für Fachregeln und Regressionstests für Bugfixes
- [x] UI approach confirms editor-authored UI only (F1 hat keine UI; Regel bleibt verbindlich)
- [x] Documentation split analysis completed (kein Doc wird übergroß)
- [x] Diagram sync and freshness checks planned for PR review
- [x] Documentation and project communication are in German

→ **Gate bestanden.** Keine Verletzungen, Complexity Tracking bleibt leer.

## Compile- & Teststrategie

- **Compile-Check**: `dotnet build Assembly-CSharp.csproj` (von Unity generiert) bzw. Build über den Unity-Editor. Die neuen Assemblies erscheinen nach Re-Import als eigene `.csproj`.
- **Tests**: Unity Test Framework über Test Runner (Editor) bzw. CLI
  `Unity -runTests -testPlatform EditMode` / `PlayMode`. EditMode ist der primäre Regel-Testpfad.
- **Negativnachweis FR-002/SC-001**: Dokumentierter (nicht eingecheckter) Versuch, in der Core-Schicht `UnityEngine.MonoBehaviour` zu nutzen → Kompilierfehler, da `noEngineReferences` aktiv. Wird in `quickstart.md` beschrieben.

## Project Structure

### Documentation (this feature)

```text
specs/001-core-architektur-fundament/
├── plan.md              # Diese Datei
├── research.md          # Phase 0: Entscheidungen (Assembly-Layout, Provider, Tests)
├── data-model.md        # Phase 1: Typen/Interfaces des Fundaments + Beispiel-Slice
├── quickstart.md        # Phase 1: „Wie lege ich ein neues Feature/Testpaar an"
├── diagrams/
│   ├── architektur-schichten.puml
│   └── decide-apply-flow.puml
├── checklists/
│   └── requirements.md  # bereits aus /speckit.specify
└── tasks.md             # Phase 2 (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/_Project/
├── Core/                                  # CozySanta.Core (noEngineReferences: true)
│   ├── CozySanta.Core.asmdef
│   ├── Time/
│   │   └── ITimeProvider.cs               # Provider-Abstraktion (Beispiel)
│   └── Interaction/
│       ├── InteractionCandidate.cs        # readonly struct (Daten)
│       ├── SelectionSettings.cs           # readonly struct (maxRange, maxAngle)
│       ├── InteractionSelection.cs        # readonly struct (Ergebnis: HasTarget, TargetId)
│       ├── IInteractionProbe.cs           # Provider: liefert Kandidaten aus der Welt
│       └── InteractionSelector.cs         # Decide(...) – reine Entscheidung
├── Runtime/                               # CozySanta.Runtime (refs Core)
│   ├── CozySanta.Runtime.asmdef
│   ├── Time/
│   │   └── UnityTimeProvider.cs           # ITimeProvider via UnityEngine.Time
│   └── Interaction/
│       ├── PhysicsInteractionProbe.cs     # IInteractionProbe via Physics.Raycast
│       └── PlayerInteractionController.cs # Orchestrierung: Probe → Decide → Apply
└── Tests/
    ├── EditMode/
    │   ├── CozySanta.Tests.EditMode.asmdef
    │   └── InteractionSelectorTests.cs    # Decide mit Fake-Daten
    └── PlayMode/
        ├── CozySanta.Tests.PlayMode.asmdef
        └── InteractionFlowPlayModeTests.cs# E2E mit Fake-Probe in Minimalszene
```

**Structure Decision**: Single Unity-Projekt mit additivem `Assets/_Project/`-Wurzelordner, der
die drei Schichten (Core/Runtime/Tests) über Assembly Definitions trennt. Die Trennung ist die
technische Durchsetzung von Constitution-Prinzip IX und FR-001/FR-002/FR-005. Bestehende
Vorlagen-Assets bleiben außerhalb von `_Project/` unangetastet (FR-009).

## Phase 0 – Research

Siehe [research.md](./research.md): Entscheidungen zu (1) Assembly-Layout & `noEngineReferences`,
(2) Provider-Design, (3) Test-Assembly-Konfiguration EditMode/PlayMode, (4) Wahl des
Beispiel-Slice. Keine offenen `NEEDS CLARIFICATION`.

## Phase 1 – Design & Contracts

- **data-model.md**: definiert die Fundament-Typen (Provider-Interfaces, Daten-Structs,
  Decide-Signatur, Apply-Verantwortung) und leitet Testkandidaten ab.
- **contracts/**: **Nicht anwendbar.** F1 stellt keine externen API-/Netzwerk-Verträge bereit;
  die „Verträge" sind die C#-Interfaces, dokumentiert in `data-model.md`. Bewusst kein
  `contracts/`-Ordner.
- **quickstart.md**: Schritt-für-Schritt „Neues Feature + EditMode-Test anlegen" anhand der Struktur.
- **Agent-Context-Update**: Statt `update-agent-context.ps1` (erwartet das auto-generierte
  Template-Format mit `## Active Technologies`) wird die hand-gepflegte `CLAUDE.md` direkt im
  Abschnitt „Build und Werkzeuge" um Ordner-/Assembly-/Namespace-Konvention ergänzt. Begründung:
  Das Skript würde die individuell verfasste `CLAUDE.md` nicht sinnvoll aktualisieren. Diese
  Abweichung ist hier dokumentiert.

## Complexity Tracking

> Keine Constitution-Verletzungen – Tabelle bleibt leer.
