# Phase 1 – Data Model & Contracts: F2

> „Contracts" sind die C#-Interfaces/Signaturen. F2 erweitert die F1-Schichtung additiv.

## Core (`CozySanta.Core`) – reine, testbare Logik

### `CozySanta.Core.Player.LookMath` (static)

- `float ClampPitch(float currentPitch, float deltaPitch, float minPitch, float maxPitch)`
  - Regel: `clamp(currentPitch + deltaPitch, minPitch, maxPitch)`.
  - Invariante: Ergebnis liegt immer in `[minPitch, maxPitch]`.

### `CozySanta.Core.Player.MovementCalculator` (static)

- `System.Numerics.Vector2 ComputeLocalVelocity(System.Numerics.Vector2 input, float speed)`
  - `input`: x = Strafe, y = Vorwärts (Rohwert, Betrag kann > 1 sein bei Diagonale).
  - Regel: Eingabe auf Betrag ≤ 1 normalisieren (Diagonale nicht schneller), dann `* speed`.
  - Liefert die lokale Planar-Geschwindigkeit (ohne Schwerkraft, ohne dt). Runtime mappt auf
    Welt-Achsen (`right * v.x + forward * v.y`) und multipliziert mit `deltaTime`.

### `CozySanta.Core.Interaction.InteractionTrigger` (static)

- `bool ShouldInteract(bool hasFocus, bool interactPressed)`
  - Regel: `hasFocus && interactPressed`. Gate für die Auslösung (FR-009).

### Bestehend (F1, unverändert genutzt)

- `InteractionSelector.Decide(IReadOnlyList<InteractionCandidate>, SelectionSettings)` → `InteractionSelection`.
- `IInteractionProbe.QueryCandidates()` → `IReadOnlyList<InteractionCandidate>` (Core bleibt pur).

## Runtime (`CozySanta.Runtime`)

### `IInteractable`

- `void Interact();` – Seiteneffekt einer Interaktion (von F3/F4 konkretisiert).
- Optional (für Hinweis-Text): `string PromptText { get; }`.
- Nur Objekte mit `IInteractable` sind Fokuskandidaten (FR-006).

### `IInteractableResolver`

- `bool TryResolve(int targetId, out IInteractable interactable);`
- Wird vom Probe implementiert; löst das von Core gewählte `TargetId` auf das Objekt auf.

### `PhysicsInteractionProbe` (erweitert F1)

- Baut Kandidaten nur aus überlappten Collidern mit `IInteractable` (via `GetComponentInParent`).
- `TargetId = interactableComponent.GetInstanceID()`; hält pro Frame `TargetId → IInteractable`.
- Implementiert `IInteractionProbe` **und** `IInteractableResolver`.

### `PlayerInteractionController` (erweitert F1)

- Pro Frame: Kandidaten holen → `InteractionSelector.Decide` → `FocusedTargetId`/`HasFocus` (F1) →
  Auflösung via `IInteractableResolver` → aktuelles `IInteractable` halten.
- Bei `InteractionTrigger.ShouldInteract(HasFocus, interactPressed)` → `Interact()` auf das aufgelöste Objekt (Apply).
- Meldet Fokuszustand an den `InteractionPromptPresenter`.

### `FirstPersonController`

- Liest Move/Look (Input System), berechnet via `MovementCalculator`/`LookMath`, wendet auf
  `CharacterController` (Move + Schwerkraft) und Kamera (Pitch) sowie Körper (Yaw) an.

### `InteractionPromptPresenter`

- `[SerializeField] GameObject promptRoot;` (editor-authored) – nur `SetActive`/optional Text.
- Methode `Show(string text)` / `Hide()`; keine Laufzeit-Erzeugung von UI (FR-008/SC-009).

## Abhängigkeitsrichtung (unverändert)

```
Runtime ──▶ Core (System.Numerics, InteractionSelector). Core kennt weder UnityEngine noch IInteractable.
```

## Abgeleitete Testkandidaten

| ID | Testfall | Art |
| --- | --- | --- |
| L1 | `ClampPitch` innerhalb Bereich → unverändert addiert | EditMode |
| L2 | `ClampPitch` über max → auf max begrenzt | EditMode |
| L3 | `ClampPitch` unter min → auf min begrenzt | EditMode |
| M1 | `ComputeLocalVelocity` Nulleingabe → Nullvektor | EditMode |
| M2 | `ComputeLocalVelocity` Diagonale → Betrag == speed (nicht schneller) | EditMode |
| M3 | `ComputeLocalVelocity` Geradeaus → Betrag == speed | EditMode |
| G1 | `ShouldInteract(true,true)` → true | EditMode |
| G2 | `ShouldInteract(false,true)`/`(true,false)` → false | EditMode |
| E1 | PlayMode: Bewegung gegen Wand → keine Durchdringung | PlayMode |
| E2 | PlayMode: Interactable in Reichweite/Winkel → HasFocus + Prompt sichtbar | PlayMode |
| E3 | PlayMode: Wegschauen/zu weit → kein Fokus, Prompt versteckt | PlayMode |
| E4 | PlayMode: Fokus + Interact-Taste → `Interact()` genau am fokussierten Fake-Objekt | PlayMode |
| E5 | PlayMode: kein Fokus + Interact-Taste → keine Auslösung | PlayMode |

> L*/M*/G* decken die Core-Entscheidungen ab; E2/E4 decken die Sequence-Seiteneffekte aus
> `diagrams/interaktion-ausloesen.puml`.
