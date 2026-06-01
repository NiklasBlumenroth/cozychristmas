# Phase 1 – Data Model & Contracts: F3

> „Contracts" sind die C#-Typen/Signaturen. F3 erweitert F1/F2 additiv.

## Core (`CozySanta.Core.Carry`) – reine, testbare Logik

### `CarryItem` (readonly struct)

- `int Id` – stabile Kennung des getragenen Objekts (Runtime: Instanz-Id des Pickups).
- `float Weight` – Gewicht in kg (≥ 0).

### `CarryStack`

- Konstruktor `CarryStack(float capacity)`.
- `float Capacity { get; set; }` – maximale Traglast (kg); setzbar für spätere Skill-Upgrades (F6).
- `float TotalWeight { get; }` – Summe der Gewichte der getragenen Einträge.
- `int Count { get; }`.
- `bool CanPickUp(float weight)` – Regel: `TotalWeight + weight <= Capacity` (Grenze inklusive).
- `bool TryPush(CarryItem item)` – wenn `CanPickUp(item.Weight)`: oben auflegen, `true`; sonst `false`.
- `bool TryPop(out CarryItem item)` – entnimmt das zuletzt aufgelegte (LIFO); `false` bei leerem Stapel.
- `bool TryPeek(out CarryItem item)` – liest das oberste ohne Entnahme.
- Invariante: LIFO-Reihenfolge; `TotalWeight` stets konsistent mit Inhalt; nie `> Capacity`.

## Runtime (`CozySanta.Runtime.Carry`)

### `IPickup`

- `float Weight { get; }` – Gewicht des Objekts.
- Implementierende Komponente ist ein `Component` (für Reparenting/Identität).

### `PickupInteractable : MonoBehaviour, IInteractable, IPickup`

- `[SerializeField] float weight;` → `Weight`.
- `PromptText => "Aufnehmen"` (oder serialisiert).
- `Interact()` bleibt leer/optional – das Aufnehmen wird spielerseitig geroutet (siehe unten).

### `PlayerCarry : MonoBehaviour`

- Hält `CarryStack` (Kapazität als SerializeField → Stack.Capacity) und `Dictionary<int, IPickup>`.
- `[SerializeField] Transform leftHandAnchor;` – **aktuelles** Objekt; **Kind der Kamera** (screen-fix).
- `[SerializeField] Transform rightHandAnchor;` – **Stapel der übrigen**; **Kind des Spielerkörpers**
  (feste Höhe), unterstes (ältestes) am Anker, weitere nach oben (Index-Offset `up * spacing * i`).
- `bool TryPickup(IPickup pickup)` – baut `CarryItem(Id, Weight)`; bei `CarryStack.TryPush`:
  Collider aus / `Rigidbody.isKinematic = true`, `Id→IPickup` merken, danach `RelayoutHands()`; `true`.
  Sonst `false` (Überlast/abgelehnt – kein Reparent, kein Dictionary-Eintrag).
- `void Drop()` – `CarryStack.TryPop`: zugehöriges `IPickup` aus dem Dictionary nehmen, in die Welt
  reparenten (vor dem Spieler platzieren), Physik reaktivieren, danach `RelayoutHands()`.
- `RelayoutHands()` (privat, Apply) – ordnet anhand des `CarryStack` neu an: oberstes Element
  (zuletzt aufgenommen) an `leftHandAnchor`; alle übrigen gestapelt an `rightHandAnchor` (Offset pro Index).
  Die Reihenfolge stammt ausschließlich aus dem Core-`CarryStack` (Single Source of Truth).

### `PlayerInteractionController` (F2, erweitert)

- `[SerializeField] PlayerCarry carry;`
- `TryInteract()`: bei Fokus → ist `_focused` ein `IPickup` und `carry` gesetzt → `carry.TryPickup(pickup)`;
  sonst `_focused.Interact()` (unverändertes F2-Verhalten, z. B. DebugInteractable).

### `PlayerInputRelay` (F2, erweitert)

- `[SerializeField] PlayerCarry carry;` (sonst via `GetComponent`).
- Liest im Prototyp `Keyboard.current.qKey.wasPressedThisFrame` → `PlayerCarry.Drop()` (formale Action später).

### `TestPickupSpawner` (Runtime, Test-/Debug-Hilfe)

- `[SerializeField] GameObject prefab;` (TestPickup), `[SerializeField] Transform spawnOrigin;` (Kamera).
- Liest `Keyboard.current.oKey.wasPressedThisFrame` → instanziiert das Prefab vor dem Spieler; das Prefab
  hat einen `Rigidbody` mit Schwerkraft und fällt zu Boden.

### `TestPickup` (Prefab, via Editor-Menü erzeugt)

- Cube + `BoxCollider` + `Rigidbody` (Schwerkraft) + `PickupInteractable` (Gewicht ~0,3 kg).

## Abhängigkeitsrichtung (unverändert)

```
Runtime ──▶ Core (CarryStack, CarryItem). Core kennt weder UnityEngine noch IPickup/IInteractable.
```

## Abgeleitete Testkandidaten

| ID | Testfall | Art | Quelle |
| --- | --- | --- | --- |
| C1 | Push unter Kapazität → erfolgreich, Count/TotalWeight stimmen | EditMode | Activity/Class |
| C2 | Push genau an Grenze (Summe == Capacity) → erlaubt | EditMode | CanPickUp-Grenze |
| C3 | Push über Kapazität → abgelehnt, Stapel unverändert | EditMode | Activity-Ablehnung |
| C4 | LIFO: A,B,C pushen → Peek = C; Pop-Reihenfolge C,B,A | EditMode | State/Stapelregel |
| C5 | Pop auf leerem Stapel → false, kein Fehler | EditMode | Edge Case |
| C6 | Capacity erhöhen → vorher abgelehntes Objekt jetzt aufnehmbar | EditMode | Skill-Andockung |
| E1 | PlayMode: Pickup fokussieren + Interact → Objekt in Hand, nicht mehr in Welt | PlayMode | F2-Interact-Routing |
| E2 | PlayMode: zweites Pickup → stapelt, zuletzt aufgenommenes „oben" | PlayMode | State |
| E3 | PlayMode: Drop → zuletzt aufgenommenes landet wieder in der Welt, interagierbar | PlayMode | State/Apply |
| E4 | PlayMode: zu schweres Pickup bei kleiner Kapazität → bleibt in der Welt | PlayMode | Gewichtsregel |

> C1–C6 decken die Core-Entscheidungen (Activity-Branches, State-Übergänge); E1–E4 die Apply-Seiteneffekte.
