# Implementation Plan: F6 – XP- & Skillsystem

**Branch**: `006-xp-skillsystem` | **Date**: 2026-06-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-xp-skillsystem/spec.md`

## Summary

F6 ergänzt ein XP- & Skillsystem mit gemeinsamem Pool und freier, kleinteiliger Punktevergabe ohne
Skilltree. Die testbare Core-Schicht hält XP/Level (`XpLedger`), die Skill-Optionen mit Stufe/Wert
(`Skill`/`SkillSet`) und bündelt sie (`ProgressionState`): XP→Level→Punkte, Investitionsregeln und die
Skillwert-Berechnung (gedeckelt). Die Runtime (`PlayerProgression`) schreibt XP aus F4-/F5-Events gut und
überträgt die Skillwerte auf die bestehenden Komponenten (`PlayerCarry.Capacity`, `MeltController`,
Bewegungsgeschwindigkeit). Das Skillmenü ist editor-authored; ein optionales IMGUI-Dev-Tool dient dem Test.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Fortschritt); neue Spec-Artefakte unter `specs/006-xp-skillsystem/`
**Diagram Impact**: `diagrams/xp-skill-entscheidung.puml` (Activity), `diagrams/progression-daten.puml` (Class), `diagrams/skill-zustand.puml` (State)
**Working Language**: German (mandatory)

## Technical Context

**Language/Version**: C# (Unity 6000.2.7f2; Core gegen netstandard2.1, `noEngineReferences`)
**Primary Dependencies**: Unity 6.2, URP 17.2, Input System 1.14.2, Test Framework 1.6; baut auf F2–F5 (`PlayerCarry`, `MeltController`, `SortTargetInteractable`, Bewegung)
**Storage**: N/A (Persistenz erst F14; Progression lebt zur Laufzeit)
**Testing**: EditMode (XpLedger/Level-Kurve, SkillSet-Investition, Skillwert/Deckelung). Runtime-Apply + UI-Bindung: gezielte Tests/manuell
**Target Platform**: PC (Windows), First-Person
**Project Type**: single (Unity-Spielprojekt)
**Performance Goals**: vernachlässigbar (Ereignis-getrieben; keine Hotpath-Last)
**Constraints**: Core ohne UnityEngine; ≤300 Zeilen je Datei; keine Laufzeit-UI (Skillmenü editor-authored); F2–F5-Verträge unverändert
**Scale/Scope**: 1 XpLedger, ~11 Skill-Optionen, 1 ProgressionState, 1 PlayerProgression, XP-Anbindung F4/F5, editor-authored Menü + optionales Dev-Tool
**Integration Touchpoints**: F3 `PlayerCarry.Capacity`, F5 `MeltController` (Akku/Schmelzwerte) + `SnowPatch.Coverage`, F4 `SortTargetInteractable.onCompleted`, F2-Bewegung
**PlantUML Documentation**: Activity + Class + State (siehe Diagram Impact)

## Constitution Check

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined (Notizen ausgenommen)
- [x] Whole-system impact & collision analysis documented (Andockpunkte aus F3–F5; Freischalt-Skills nur Status; UI editor-authored)
- [x] PlantUML diagram selection completed (Activity + Class + State; Sequence/Mindmap „Not required")
- [x] Definitions/requirements updated where collisions exist (gemeinsamer Pool/freie Wahl; Werte→Komponenten)
- [x] Compile-validation command(s) defined (siehe „Compile- & Teststrategie")
- [x] Architekturtrennung geplant: XP/Skill-Logik als Core ohne Unity
- [x] Decide/Apply-Trennung dokumentiert (Decide: XpLedger/SkillSet/ProgressionState; Apply: Werte auf Komponenten, Event-Anbindung, UI-Bindung)
- [x] Teststrategie definiert (EditMode für Progressionsregeln; Runtime-Apply gezielt/manuell)
- [x] Diagram-derived test candidates documented (data-model.md)
- [x] DoD baseline covers compile-checks, 300-line class-size, Unit-Tests
- [x] UI approach confirms editor-authored UI only (Skillmenü editor-authored; IMGUI nur Dev)
- [x] Documentation split analysis completed (kein Doc übergroß)
- [x] Diagram sync/freshness checks planned for PR review
- [x] Documentation and project communication are in German

→ **Gate bestanden.** Keine Verletzungen; Complexity Tracking bleibt leer.

## Compile- & Teststrategie

- **Compile-Check**: `dotnet build cozychristmas.sln` (Core netstandard2.1 + Runtime/Editor/Tests).
- **Tests**: EditMode für `XpLedger` (Schwellen/mehrfacher Level-up), `SkillSet` (Investition/Punkte/Max/Freischaltung), `Skill` (Wert/Deckelung). Manuell: Dev-Tool investiert → Tragkraft/Lampe/Speed ändern sich.
- **UI-Politik**: keine Laufzeit-UI; Skillmenü editor-authored; IMGUI nur als Dev-Test-Tool.

## Project Structure

```text
Assets/_Project/
├── Core/Progression/
│   ├── XpLedger.cs           # XP/Level/Punkte (Decide)
│   ├── Skill.cs              # Stufe + Wertberechnung (Decide)
│   ├── SkillId.cs            # Enum der Skill-Optionen
│   ├── SkillSet.cs           # Investition/Verfügbarkeit (Decide)
│   └── ProgressionState.cs   # XpLedger + SkillSet bündeln (Decide)
├── Runtime/Progression/
│   ├── PlayerProgression.cs  # hält ProgressionState; XP-Anbindung; Werte→Komponenten (Apply)
│   └── SkillMenuDevTool.cs   # optionales IMGUI-Dev-Tool (Punkte/Investition testen)
└── Tests/EditMode/ProgressionTests.cs
```

**Structure Decision**: Fortführung der F1–F5-Schichtung. XP/Skill-Entscheidungen als reine Logik nach
`Core/Progression`; die Werte-Anwendung auf bestehende Komponenten, Event-Anbindung und UI-Bindung nach
`Runtime/Progression`. Die Andockpunkte (`Capacity`, `onCompleted`, `Coverage`, Bewegungsspeed) existieren
bereits aus F3–F5 und werden nur konsumiert/gesetzt.

## Phase 0 – Research

Siehe [research.md](./research.md): (1) XP/Level-Kurve als Core, (2) Skill-Wertmodell (Basis+Schritt, gedeckelt),
(3) freie Investition ohne Tree + Freischalt-Skills, (4) XP-Quellen-Anbindung (F4-Event, F5-Coverage-Delta),
(5) Werte→Komponenten (Apply-Strategie), (6) UI editor-authored + Dev-Tool. Keine offenen `NEEDS CLARIFICATION`.

## Phase 1 – Design & Contracts

- **data-model.md**: `XpLedger`, `Skill`, `SkillId`, `SkillSet`, `ProgressionState` (Signaturen/Invarianten), `PlayerProgression`, Testkandidaten.
- **contracts/**: Nicht anwendbar (keine externen API-Verträge).
- **quickstart.md**: Progression am Player einrichten, XP-Quellen verbinden, Dev-Tool investieren, Wirkung prüfen; editor-authored Menü binden.

## Complexity Tracking

> Keine Constitution-Verletzungen – Tabelle bleibt leer.
