# Phase 1 – Data Model & Contracts: F1

> „Contracts" sind in diesem Unity-Feature die C#-Interfaces der Core-Schicht (keine externen
> API-Verträge). Sie sind hier dokumentiert.

## Core-Typen (`CozySanta.Core`)

### Provider-Interfaces

- **`ITimeProvider`**
  - `float DeltaTime { get; }` – Frame-Delta (Abstraktion über `Time.deltaTime`).
  - `double Now { get; }` – monotone Zeit in Sekunden (Abstraktion über `Time.timeAsDouble`).
  - Zweck: Zeitzugriff ohne `UnityEngine.Time` in der Core-Schicht (FR-003).

- **`IInteractionProbe`**
  - `IReadOnlyList<InteractionCandidate> QueryCandidates();`
  - Zweck: Weltabfrage als Abstraktion (Runtime via `Physics.Raycast`/`OverlapSphere`); in Tests durch Fake ersetzbar.

### Daten-Structs (immutabel)

- **`InteractionCandidate`** (`readonly struct`)
  - `int TargetId` – stabile Kennung des Kandidaten.
  - `float Distance` – Distanz zum Spieler (≥ 0).
  - `float AngleToView` – Winkel zur Blickrichtung in Grad (≥ 0).
  - Invariante: `Distance >= 0`, `AngleToView >= 0`.

- **`SelectionSettings`** (`readonly struct`)
  - `float MaxRange` – maximale Interaktionsdistanz (> 0).
  - `float MaxAngle` – maximaler Blickwinkel in Grad (> 0).

- **`InteractionSelection`** (`readonly struct`)
  - `bool HasTarget`
  - `int TargetId` (nur gültig, wenn `HasTarget`)
  - Statisch: `InteractionSelection.None` (HasTarget = false).

### Entscheidung (Decide)

- **`InteractionSelector`** (statisch oder zustandslose Instanz)
  - `InteractionSelection Decide(IReadOnlyList<InteractionCandidate> candidates, SelectionSettings settings);`
  - Reine Funktion, keine Seiteneffekte, keine Unity-Abhängigkeit.
  - Regel: Aus allen Kandidaten mit `Distance <= MaxRange` **und** `AngleToView <= MaxAngle`
    wird der mit der **kleinsten Distanz** gewählt. Bei Gleichstand entscheidet der kleinere
    `AngleToView`. Gibt es keinen gültigen Kandidaten → `InteractionSelection.None`.

## Runtime-Typen (`CozySanta.Runtime`)

- **`UnityTimeProvider : ITimeProvider`** – liefert `Time.deltaTime` / `Time.timeAsDouble`.
- **`PhysicsInteractionProbe : MonoBehaviour, IInteractionProbe`** – baut Kandidaten aus
  `Physics`-Abfragen relativ zu Kamera/Spieler.
- **`PlayerInteractionController : MonoBehaviour`** – Orchestrierung pro Frame:
  1. `var candidates = _probe.QueryCandidates();` (Provider)
  2. `var selection = InteractionSelector.Decide(candidates, _settings);` (Decide, Core)
  3. **Apply**: setzt das fokussierte Ziel (z. B. Event/Property `FocusedTargetId`) – der einzige
     Ort mit Seiteneffekt. Keine Auswahlregel-Duplikation in der Runtime (FR-004, FR-005).

## Abhängigkeitsrichtung

```
CozySanta.Runtime  ──referenziert──▶  CozySanta.Core
CozySanta.Tests.EditMode  ──▶  CozySanta.Core
CozySanta.Tests.PlayMode  ──▶  CozySanta.Core, CozySanta.Runtime
```
Eine Referenz von Core auf Runtime ist ausgeschlossen (FR-005).

## Abgeleitete Testkandidaten (aus Decide-Regel & Activity-Diagramm)

| ID | Testfall | Art | Quelle |
| --- | --- | --- | --- |
| T1 | Kein Kandidat in Reichweite → `None` | EditMode | Decide-Branch „leer" |
| T2 | Ein gültiger Kandidat → wird gewählt | EditMode | Happy Path |
| T3 | Mehrere gültige → kleinste Distanz gewinnt | EditMode | Decide-Sortierregel |
| T4 | Distanzgleichstand → kleinerer Winkel gewinnt | EditMode | Tie-Break-Regel |
| T5 | Kandidat außerhalb `MaxAngle` wird ignoriert | EditMode | Winkel-Filter |
| T6 | Kandidat außerhalb `MaxRange` wird ignoriert | EditMode | Reichweiten-Filter |
| T7 | E2E: Fake-Probe + 1 Ziel in Minimalszene → `FocusedTargetId` gesetzt | PlayMode | Decide/Apply-Flow |
| T8 | Negativnachweis: `UnityEngine`-Typ in Core ⇒ Kompilierfehler | manuell/dokumentiert | FR-002/SC-001 |

> T1–T6 decken die Activity-Branches aus `diagrams/decide-apply-flow.puml` ab; T7 den
> Apply-Seiteneffekt; T8 die Architekturinvariante.
