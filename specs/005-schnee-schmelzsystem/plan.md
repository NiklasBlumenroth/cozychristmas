# Implementation Plan: F5 – Schnee-Schmelzsystem

**Branch**: `005-schnee-schmelzsystem` | **Date**: 2026-06-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-schnee-schmelzsystem/spec.md`

## Summary

F5 trägt eine volumetrische Schneedecke per Taschenlampe ab. Die testbare Core-Schicht hält den Akku
(`LampBattery`) und ein Zell-Höhenfeld (`MeltField`, Höhe 0..1 je Zelle, `Coverage`). Die Runtime
generiert ein Grid-Mesh (`SnowPatch`), das das Höhenfeld spiegelt (Vertex-Höhe + Höhe im Vertex-Color),
und steuert es über `MeltController` (Raycast Blick→Schnee, F = schmelzen mit Akku-Verbrauch, V =
Schnee auftragen als Dev-Helfer). Ein URP-Shader (`SnowMelt`) clippt freigelegte Stellen mit weicher,
noisy Kante und texturiert/beleuchtet den Schnee. Boden = vorhandene Szenen-Plane; Schnee-Textur =
stilisierte `Holiday_Snow_02`. Scope: **ein** Test-Patch.

**Change Classification**: Planned feature via waterfall artifacts
**Documentation Impact**: `CLAUDE.md` (Fortschritt); neue Spec-Artefakte unter `specs/005-schnee-schmelzsystem/`
**Diagram Impact**: `diagrams/schmelz-entscheidung.puml` (Activity), `diagrams/schnee-daten.puml` (Class), `diagrams/akku-zustand.puml` (State)
**Working Language**: German (mandatory)

## Technical Context

**Language/Version**: C# (Unity 6000.2.7f2; Core gegen netstandard2.1, `noEngineReferences`) + ein URP-HLSL-Shader
**Primary Dependencies**: Unity 6.2, URP 17.2, Input System 1.14.2, Test Framework 1.6; baut auf F1 (Core/Provider) und F2 (Kamera/Blick)
**Storage**: N/A (Höhenfeld/Akku leben zur Laufzeit; Persistenz erst F14)
**Testing**: EditMode (`LampBattery`, `MeltField`/Coverage) – B1–B4, M1–M5. Shader/Mesh-Displacement: manuelle/Editor-Smoke-Checks (dokumentierte Ausnahme zu Prinzip IX/X)
**Target Platform**: PC (Windows), First-Person
**Project Type**: single (Unity-Spielprojekt)
**Performance Goals**: 60 fps auf einem begrenzten Test-Patch; Mesh-Sync nur beim Schmelzen/Auftragen
**Constraints**: Core ohne UnityEngine; ≤300 Zeilen je Datei; keine Laufzeit-UI; Chunking/LOD ausgeschlossen
**Scale/Scope**: 1 Core-Akku, 1 Core-Höhenfeld, 1 `SnowPatch`, 1 `MeltController`, 1 Shader, 1 Editor-Setup, neue Eingaben F/V
**Integration Touchpoints**: F2-Kamera (Raycast-Ursprung), `ITimeProvider`-Idee (dt in Runtime skaliert), Input (F/V via Keyboard)
**PlantUML Documentation**: Activity + Class + State (siehe Diagram Impact)

## Constitution Check

- [x] Change path classified (planned waterfall feature)
- [x] Documentation coverage defined (Notizen ausgenommen)
- [x] Whole-system impact & collision analysis documented (GPU-vs-testbare-Core-Auflösung; Boden=eigene Schicht; neue F/V-Eingaben)
- [x] PlantUML diagram selection completed (Activity + Class + State; Sequence/Mindmap „Not required")
- [x] Definitions/requirements updated where collisions exist (Core = Autorität, GPU spiegelt nur)
- [x] Compile-validation command(s) defined (siehe „Compile- & Teststrategie")
- [x] Architekturtrennung geplant: Akku/Höhenfeld/Coverage als Core ohne Unity
- [x] Decide/Apply-Trennung dokumentiert (Decide: `LampBattery`, `MeltField`; Apply: Mesh-Sync, Shader, Raycast, Input)
- [x] Teststrategie definiert (EditMode für Core; Shader/RT als dokumentierte Nicht-Unit-Ausnahme)
- [x] Diagram-derived test candidates documented (data-model.md)
- [x] DoD baseline covers compile-checks, 300-line class-size, Unit-Tests
- [x] UI approach confirms editor-authored UI only (keine Laufzeit-UI; HUD folgt F7)
- [x] Documentation split analysis completed (kein Doc übergroß)
- [x] Diagram sync/freshness checks planned for PR review
- [x] Documentation and project communication are in German

→ **Gate bestanden.** Einzige bewusste Ausnahme: Shader/RenderTexture/Mesh-Displacement sind nicht
unit-testbar (GPU); dokumentiert in spec/research/data-model. Complexity Tracking bleibt leer.

## Compile- & Teststrategie

- **Compile-Check**: `dotnet build cozychristmas.sln` (Core netstandard2.1 + Runtime/Editor/Tests). Shader-Kompilierung erfolgt im Unity-Editor (nicht via dotnet) → Editor-Smoke-Check.
- **Tests**: EditMode `SnowMeltTests` (B1–B4 Akku, M1–M5 Höhenfeld/Coverage). Manueller Spieltest: F schmelzen, V auftragen, Akku leeren/laden.
- **UI-Politik**: keine Laufzeit-UI; spätere Akku-/Fortschrittsanzeige editor-authored (F7).

## Project Structure

```text
Assets/_Project/
├── Core/Snow/
│   ├── LampBattery.cs        # Akku (Decide)
│   └── MeltField.cs          # Zell-Höhenfeld + Coverage (Decide)
├── Runtime/Snow/
│   ├── SnowPatch.cs          # Grid-Mesh, spiegelt MeltField (Apply)
│   └── MeltController.cs      # Raycast + F/V + Akku-Tick (Apply)
├── Shaders/SnowMelt.shader    # URP: Clip melted + Noise-Rand + Textur/Lighting
├── Editor/SnowSetup.cs        # Material + Patch + Controller-Verdrahtung
└── Tests/EditMode/SnowMeltTests.cs
```

**Structure Decision**: Fortführung der F1–F4-Schichtung. Akku/Höhenfeld/Coverage als reine Logik nach
`Core/Snow`; Mesh-Sync, Raycast, Eingabe und der Shader nach `Runtime/Snow` bzw. `Shaders`/`Editor`. Das
Core-`MeltField` ist Autorität über Fortschritt; die GPU-Darstellung spiegelt es nur (Vertex-Displacement
+ Clip). Robuster erster Wurf via **CPU-Vertex-Displacement** (kein Tessellation-Risiko).

## Phase 0 – Research

Siehe [research.md](./research.md): (1) Höhenfeld als Core-Grid + Mesh-Spiegelung, (2) CPU-Vertex-
Displacement statt Tessellation, (3) Reveal über Shader-Clip mit Noise, (4) Raycast auf Patch-Ebene,
(5) Akku/Aufladen, (6) GPU-vs-testbare-Core-Auflösung. Keine offenen `NEEDS CLARIFICATION`.

## Phase 1 – Design & Contracts

- **data-model.md**: `LampBattery`, `MeltField` (Signaturen/Invarianten), `SnowPatch`, `MeltController`, Shader-Schnittstelle, Testkandidaten B1–B4/M1–M5.
- **contracts/**: Nicht anwendbar (keine externen API-Verträge).
- **quickstart.md**: Setup-Menü ausführen bzw. manuell; Schnee-Patch + Material; F/V testen; Look justieren.

## Complexity Tracking

> Keine Constitution-Verletzungen – Tabelle bleibt leer. (Shader-Nicht-Testbarkeit ist dokumentierte Ausnahme, keine Verletzung.)
