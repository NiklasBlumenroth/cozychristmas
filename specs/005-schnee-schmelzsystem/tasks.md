---
description: "Task list für F5 – Schnee-Schmelzsystem"
---

# Tasks: F5 – Schnee-Schmelzsystem

**Input**: Design-Dokumente aus `/specs/005-schnee-schmelzsystem/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, diagrams/ (vorhanden); contracts/ nicht anwendbar

**Tests**: Verbindlich für Core (`LampBattery` B1–B4, `MeltField`/Coverage M1–M5). Shader/Mesh-Displacement =
dokumentierte Nicht-Unit-Ausnahme (Editor-Smoke + manuelle Iteration). Compile-Check + 300-Zeilen-DoD verbindlich.

**Diagrams**: `schmelz-entscheidung.puml` (Activity), `schnee-daten.puml` (Class), `akku-zustand.puml` (State).

## Umsetzungsstatus (2026-06-02)

- **Erledigt (Code)**: Core `LampBattery`, `MeltField` (Coverage); Runtime `SnowPatch` (Grid-Mesh + Höhenfeld-
  Spiegelung), `MeltController` (Raycast, F/V, Akku-Tick); URP-Shader `SnowMelt` (Clip-Reveal + Noise-Rand +
  Textur/Lighting/Glitzer); Editor `SnowSetup` (Material + Patch + Verdrahtung). EditMode-Tests `SnowMeltTests` (B1–B4, M1–M5).
- **Verifiziert (Compile)**: `dotnet build cozychristmas.sln` – **0 Fehler / 0 Warnungen** (Core/Runtime/Editor/Tests).
  300-Zeilen-DoD eingehalten (größte Datei 141 Zeilen).
- **Erledigt (Tests/manuell)**: EditMode `SnowMeltTests` (B1–B4, M1–M5) vom Nutzer **grün** ausgeführt;
  Shader kompiliert, Schmelzen/Volumen/Rand visuell abgenommen; F/V-Spieltest erfolgreich.
- **Nach-Test-Fixes**: (1) Akku-Deadlock behoben – Nachladen erfolgt nun in jedem Frame ohne aktiven Abtrag
  (auch bei gehaltenem F); (2) Zielen via Trigger-`BoxCollider` am Patch + `Physics.RaycastAll` (trifft das
  Schnee-Volumen, Fallback Ebene auf halber Höhe); (3) Edit-Modus-Gizmo zeigt die Schnee-Ausdehnung
  (Fläche + Höhe) beim Szenenbau.
- **Hinweis**: Generierte `.csproj` temporär um F5-Dateien ergänzt (gitignored); Unity regeneriert beim Fokus.
- **Bewusst späterer Scope**: Schnee-Maske (Schnee nur in definierter Form), `[ExecuteAlways]`-Mesh-Preview,
  Chunking/LOD, Wertung/XP (F6/F7).

---

## Phase 1: Setup

- [X] T001 Change-Typ „Planned feature" bestätigt (spec/plan)
- [X] T002 Doku-Impact: `CLAUDE.md` (Fortschritt) + Spec-Artefakte; `Notizen.md` ausgenommen
- [X] T003 [P] Deutsch-Pflicht für Feature-Doku/Kommunikation
- [X] T004 [P] Diagrammauswahl (Activity + Class + State; Sequence/Mindmap begründet ausgelassen) + GPU-Nicht-Test-Ausnahme dokumentiert

## Phase 2: Foundational

- [X] T005 Ordner `Assets/_Project/Core/Snow/`, `Assets/_Project/Runtime/Snow/`, `Assets/_Project/Shaders/` anlegen
- [X] T006 Testkandidaten B1–B4, M1–M5 aus data-model.md als verbindlich markiert
- [X] T007 Architektur bestätigt: Core = Autorität (Akku/Höhenfeld/Coverage); GPU spiegelt nur (Vertex-Displacement + Clip)

## Phase 3: US1 – Schmelzen an der Leuchtstelle (P1) 🎯 MVP

- [X] T008 [P] [US1] EditMode M1/M2/M5 in `Assets/_Project/Tests/EditMode/SnowMeltTests.cs`
- [X] T009 [US1] Core `MeltField` (Höhenfeld, Melt, HeightAt, Pinsel-Falloff) in `Assets/_Project/Core/Snow/MeltField.cs`
- [X] T010 [US1] Runtime `SnowPatch` (Grid-Mesh, Weltpunkt→UV, Melt + SyncMesh) in `Assets/_Project/Runtime/Snow/SnowPatch.cs`
- [X] T011 [US1] `MeltController` (Raycast Patch-Ebene, F gehalten → Melt) in `Assets/_Project/Runtime/Snow/MeltController.cs`
- [X] T012 [US1] EditMode M1/M2/M5 grün + manueller F-Test (Abtrag sichtbar) — Nutzer bestätigt

## Phase 4: US2 – Volumen & weicher Übergang (P1)

- [X] T013 [US2] Vertex-Displacement (Höhe→Vertex-Y) + Vertex-Color.r = Höhe in `SnowPatch.SyncMesh`
- [X] T014 [US2] Shader `CozySanta/SnowMelt`: `clip(height − noisySchwelle)` (Reveal + weicher Rand), Textur/Lighting/Glitzer in `Assets/_Project/Shaders/SnowMelt.shader`
- [X] T015 [US2] Editor-Smoke: Decke hat sichtbare Höhe, Rand weich/noisy, Boden erscheint — visuell abgenommen

## Phase 5: US3 – Akku (P2)

- [X] T016 [P] [US3] EditMode B1–B4 in `SnowMeltTests.cs`
- [X] T017 [US3] Core `LampBattery` (CanMelt/Drain/Recharge/Refill) in `Assets/_Project/Core/Snow/LampBattery.cs`
- [X] T018 [US3] `MeltController`: Verbrauch beim Abtrag, Sperre bei leer, Auto-Recharge wenn nicht schmelzend
- [X] T019 [US3] EditMode B1–B4 grün + manueller Test (Akku leeren/laden, Deadlock-Fix) — Nutzer bestätigt

## Phase 6: US4 – Flächen-Fortschritt (P2)

- [X] T020 [P] [US4] EditMode M3 (100 %) in `SnowMeltTests.cs`
- [X] T021 [US4] Core `MeltField.Coverage`/`CoveragePercent` (inkrementell), `SnowPatch.Coverage`, `MeltController.Coverage` (Andock F6/F7)
- [X] T022 [US4] EditMode M3 grün — Nutzer bestätigt

## Phase 7: US5 – Dev-Auftrag (P3)

- [X] T023 [P] [US5] EditMode M4 (Add hebt, clamp 1) in `SnowMeltTests.cs`
- [X] T024 [US5] `MeltField.Add` + `SnowPatch.AddSnow` + `MeltController` (V gehalten) 
- [X] T025 [US5] manueller Test (V trägt auf, erneut schmelzbar) — Nutzer bestätigt

## Phase 7b: Tooling

- [X] T026 Editor-Menü „CozySanta/Setup F5 (Schnee-Patch + Lampe)" in `Assets/_Project/Editor/SnowSetup.cs` (Material + Patch + Controller-Verdrahtung)
- [X] T027 „Setup F5" ausführen, Patch positionieren, Material zuweisen — Nutzer bestätigt

## Phase 8: Documentation Sync & Merge Readiness

- [X] T028 Doku-Coverage-Matrix (siehe unten)
- [X] T029 `CLAUDE.md` „Fortschritt" um F5 ergänzt
- [X] T030 [P] Deutsch-Nachweis; PlantUML ohne Platzhalter
- [X] T031 Bestätigt: keine Laufzeit-UI; GPU-Nicht-Test-Ausnahme dokumentiert
- [X] T032 Finaler EditMode-Lauf (B1–B4, M1–M5) grün + Shader-Smoke + Look-Iteration abgenommen — Nutzer bestätigt

---

## Doku-Coverage-Matrix (Merge-Readiness)

| Code-Area | Doku/Diagramm | Test |
| --- | --- | --- |
| `Core/Snow/LampBattery` | data-model.md, `akku-zustand.puml` | B1–B4 |
| `Core/Snow/MeltField` (+Coverage) | data-model.md, `schmelz-entscheidung.puml`, `akku-zustand.puml` | M1–M5 |
| `Runtime/Snow/SnowPatch` (Mesh-Spiegelung) | data-model.md, `schnee-daten.puml` | manuell/Smoke |
| `Runtime/Snow/MeltController` (Raycast/F/V/Akku) | data-model.md, `schmelz-entscheidung.puml` | manuell/Smoke |
| `Shaders/SnowMelt` (Clip-Reveal/Rand) | research.md (R3), data-model.md | Editor-Smoke (dokumentierte Ausnahme) |
| `Editor/SnowSetup` | quickstart.md | manuell |

`Notizen.md` ausgenommen (Constitution I). Keine Laufzeit-UI. Shader/Mesh-Displacement = dokumentierte
Nicht-Unit-Test-Ausnahme (Prinzip IX/X), abgedeckt über Editor-Smoke + Look-Iteration.

## Dependencies

- Setup→Foundational blockiert Stories. US1 (Melt-Kern) → US2 (Volumen/Shader) → US3 (Akku) / US4 (Coverage) / US5 (Dev-Auftrag) bauen darauf auf. Tooling parallel. Doc-Sync zuletzt.
