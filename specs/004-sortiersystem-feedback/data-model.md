# Phase 1 – Data Model & Contracts: F4

> „Contracts" sind die C#-Typen/Signaturen. F4 erweitert F1–F3 additiv. Abhängigkeit strikt
> Runtime → Core. Signaturen sind Richtwerte für die Implementierung (Namen/Details dürfen sich beim
> Coden geringfügig konkretisieren, solange Verträge/Invarianten erhalten bleiben).

## Core (`CozySanta.Core.Sorting`) – reine, testbare Logik

### `SortKey` (readonly struct, `IEquatable<SortKey>`)

- Kapselt eine **geordnete Folge benannter Facettenwerte** (z. B. `["Europe","Rot","Stern"]`).
- `bool Matches(SortKey other)` / Wertegleichheit (`Equals`/`GetHashCode`) über alle Facetten,
  positionsbezogen.
- Unveränderlich; keine Unity-Abhängigkeit. Leerer/teilbesetzter Key ist zulässig (Grey-Box).
- Invariante: Zwei `SortKey` sind genau dann gleich, wenn Facettenanzahl und alle Facettenwerte gleich sind.

### `SortTargetState` (enum)

- `Leer` – keine Objekte eingelegt.
- `Teilweise` – korrekte Objekte vorhanden, aber Soll-Menge nicht erreicht; keine falschen.
- `FalschEnthalten` – mindestens ein falsch klassifiziertes Objekt eingelegt.
- `Vollstaendig` – `RequiredCount` korrekte UND 0 falsche; abgeschlossen/gesperrt.

### `SortTarget`

- Konstruktor `SortTarget(SortKey accepted, int requiredCount)`.
- `SortKey Accepted { get; }` – akzeptierte Kategorie des Fachs.
- `int RequiredCount { get; }` – Soll-Menge korrekter Objekte für den Abschluss.
- `int CorrectCount { get; }` / `int WrongCount { get; }` – Zählstände der Einlagen.
- `int Count { get; }` – Gesamtzahl eingelegter Objekte.
- `bool IsClosed { get; }` – `true`, sobald einmal `Vollstaendig` erreicht (gesperrt).
- `bool Classify(SortKey itemKey)` – Decide: `true` wenn `itemKey.Matches(Accepted)` (korrekt), sonst falsch.
- `bool TryPlace(int id, SortKey itemKey)` – Decide+Mutation: wenn `IsClosed` → `false` (gesperrt);
  sonst Eintrag (Id + Key + Klassifizierung) **oben** anlegen, Zähler aktualisieren, `true`.
- `bool TryRemoveTop(out int id)` – Decide+Mutation: wenn `IsClosed` oder leer → `false`; sonst das
  **zuletzt eingelegte** Objekt (LIFO) entfernen, Zähler aktualisieren, dessen `id` liefern, `true`.
- `SortTargetState Evaluate()` – Decide (rein): leitet den Zustand aus `CorrectCount`/`WrongCount`/
  `RequiredCount` ab. Abschlussregel: `CorrectCount == RequiredCount && WrongCount == 0` → `Vollstaendig`.
- `bool JustCompleted` / Abschluss-Signal – zeigt der Runtime an, dass der Abschluss **in diesem Schritt**
  erreicht wurde (für einmaliges Ereignis + Schließen). Alternativ über Rückgabewert/Out-Parameter von
  `TryPlace`.
- Invarianten: `CorrectCount + WrongCount == Count`; `CorrectCount <= RequiredCount` ist **nicht**
  erzwungen (überzählige korrekte sind theoretisch möglich, der Abschluss greift aber bei Gleichheit und
  sperrt danach); nach `IsClosed` keine Mutation mehr; LIFO-Reihenfolge der Einlagen.

## Runtime (`CozySanta.Runtime.Sorting`)

### `ISortable`

- `SortKey Key { get; }` – Sortierschlüssel des Objekts.
- Implementierende Komponente ist ein `Component` (für Reparenting/Identität, analog `IPickup`).

### `Sortable : MonoBehaviour, ISortable`

- Editor-authored Facettenwerte (`[SerializeField] string[] facets;`) → bildet `Key`.
- Ergänzt das F3-`PickupInteractable`/`IPickup` desselben GameObjects (Objekt bleibt aufnehmbar **und**
  sortierbar). Bricht `IPickup` nicht.

### `SortTargetInteractable : MonoBehaviour, IInteractable`

- Editor-authored Konfiguration: `[SerializeField] string[] acceptedFacets;`, `[SerializeField] int requiredCount;`
  → bildet das Core-`SortTarget` in `Awake`.
- `[SerializeField] Transform slotAnchor;` – Ablageort für eingelegte Visuals (Fallback: eigener Transform).
- `[SerializeField] GameObject lampOn;` **oder** `[SerializeField] Light lamp;` – editor-authored Lampe,
  disabled by default; Apply schaltet bei Abschluss.
- `[SerializeField] UnityEvent onCompleted;` – einmaliges Abschluss-Ereignis (Andockpunkt F6).
- `PromptText` – kontextabhängig (z. B. „Einsortieren" / „Entnehmen"), oder statisch im Grey-Box.
- `Interact()` (Apply, kontextabhängig, geroutet über den Player – siehe Routing):
  - Hand trägt etwas & Fach offen → **Einsortieren**: oberstes getragenes `ISortable` aus `PlayerCarry`
    übernehmen; `SortTarget.TryPlace(id, key)`; Objekt an `slotAnchor` reparenten, dessen
    Interaktions-Collider deaktivieren, sichtbar lassen; bei `JustCompleted` → Lampe an, `onCompleted`,
    Fach **schließen** (eingelegte Visuals entfernen/verstecken, Fach-Collider aus).
  - Hand leer & Fach hat entnehmbare Objekte & nicht geschlossen → **Entnahme**: `SortTarget.TryRemoveTop`
    → das zugehörige Objekt zurück in die Hand (`PlayerCarry`-Kapazitätsprüfung; bei Überlast abgelehnt,
    Objekt bleibt im Fach), Interaktions-Collider reaktivieren.
- Hält ein `Dictionary<int, Component>` (Id→Objekt) für die eingelegten Visuals (analog `PlayerCarry`).
- `[SerializeField] Vector3 stackDirection` (+ `stackSpacing`): Reihung der Einlagen im **lokalen Raum des
  Slots** (z. B. `(0,0,1)` = nebeneinander entlang der Fachbreite). Versatz = `slot.rotation * (dir * spacing * index)`.
- `void Configure(string[] accepted, int required, Transform slot = null, GameObject lampObject = null)`:
  Laufzeit-Konfiguration (datengetriebene Bestückung „Weg B" und Tests) – baut die Core-`SortTarget` frisch auf.

### Routing-Erweiterung (`PlayerInteractionController`, F2)

- Bestehendes Verhalten: fokussiertes `IPickup` → `PlayerCarry.TryPickup`; sonst `IInteractable.Interact()`.
- **Erweiterung**: ein fokussiertes `SortTargetInteractable` erhält die Interaktion (Übergabe/Entnahme).
  Da das Fach ein `IInteractable` ist, genügt es, dass das Fach **kein** `IPickup` ist → es fällt in den
  bestehenden `Interact()`-Pfad. Das Fach greift im `Interact()` selbst auf den Player-`PlayerCarry` zu
  (Referenz im Editor gesetzt oder via Resolver), um Hand↔Fach umzusetzen. Kein Bruch des F2/F3-Vertrags.

### `SortingSetup` (Editor, Fallback-Prefabs + Szene)

- Editor-Menü „CozySanta/Setup F4 (Sortieren + Test-Fach)": erzeugt via `PrefabUtility`
  - **sortierbares Objekt**-Prefab (Cube/flache Box + Rigidbody + Collider + `PickupInteractable` + `Sortable`),
  - **Fach**-Prefab (Root mit Pivot Öffnungsmitte + `SortTargetInteractable` + `BoxCollider` + `SlotAnchor`
    + `Lampe` als Emissive-Mesh, disabled),
  - optional ein **Rahmen** mit kleinem Fach-Gitter; setzt `acceptedFacets`/`requiredCount` als Beispiel.

## Abhängigkeitsrichtung (unverändert)

```
Runtime ──▶ Core (SortKey, SortTarget, SortTargetState).
Core kennt weder UnityEngine noch ISortable/IInteractable/PlayerCarry.
```

## Abgeleitete Testkandidaten

| ID | Testfall | Art | Quelle |
| --- | --- | --- | --- |
| S1 | `SortKey`-Gleichheit: gleiche Facetten → gleich; abweichende → ungleich | EditMode | Class/Klassifizierung |
| S2 | `Classify`: passender Key → korrekt; abweichender → falsch | EditMode | Activity-Verzweigung |
| S3 | `TryPlace` legt oben auf; `CorrectCount`/`WrongCount`/`Count` konsistent | EditMode | Class-Invariante |
| S4 | Vollständigkeit: genau `RequiredCount` korrekte, 0 falsche → `Vollstaendig` (Grenze inklusive) | EditMode | State-Abschluss |
| S5 | Falsch enthalten: `RequiredCount` korrekte + ≥1 falsche → `FalschEnthalten`, nicht abgeschlossen | EditMode | State/sanftes Feedback |
| S6 | Teilweise: korrekte < `RequiredCount`, 0 falsche → `Teilweise` | EditMode | State |
| S7 | LIFO-Entnahme: A,B einlegen → `TryRemoveTop` liefert B, dann A; Zähler sinken | EditMode | State/Stapelregel |
| S8 | `TryRemoveTop` auf leerem Fach → `false`, kein Fehler | EditMode | Edge Case |
| S9 | Nach Abschluss `IsClosed`: `TryPlace`/`TryRemoveTop` → `false` (gesperrt) | EditMode | State-Sperre |
| S10 | `JustCompleted` nur im Abschluss-Schritt `true`, danach nicht erneut | EditMode | Abschluss-Ereignis |
| E1 | PlayMode: passendes Objekt tragen + Fach-Interact → Objekt im Fach, Hand leer, korrekt gezählt | PlayMode | Routing/Apply |
| E2 | PlayMode: Fach auf `RequiredCount` korrekte füllen → Lampe an, Fach geschlossen, Visuals entfernt | PlayMode | Vollständigkeit/Apply |
| E3 | PlayMode: falsches Objekt einsortieren → bleibt im Fach, Lampe bleibt aus | PlayMode | sanftes Feedback |
| E4 | PlayMode: Entnahme bei leerer Hand → Objekt kehrt zurück (LIFO); bei Überlast abgelehnt | PlayMode | Entnahme/Kapazität |

> S1–S10 decken die Core-Entscheidungen (Klassifizierung, State-Übergänge, LIFO, Sperre, Abschluss-Signal);
> E1–E4 die Apply-Seiteneffekte (Reparenting, Collider-/Visual-/Lampen-Toggle, Hand↔Fach, Kapazität).
