# Phase 0 – Research: F3 Trag-, Hand- & Gewichtssystem

Keine offenen `NEEDS CLARIFICATION`. Entscheidungen:

## 1. Core-Stapel & Kapazität

- **Decision**: `CarryStack` (reine C#-Klasse) mit interner `List<CarryItem>` als LIFO (Top =
  letztes Element = „links/oben"). `Capacity` als setzbare Eigenschaft (für spätere Skill-Upgrades),
  `TotalWeight`, `Count`, `CanPickUp(weight)`, `TryPush(item)`, `TryPop(out item)`, `TryPeek(out item)`.
- **Rationale**: List erlaubt Gesamtgewicht und Indexzugriff (für versetzte Stapel-Darstellung), bleibt
  rein und unit-testbar. Kapazität setzbar → F6 kann sie nur erhöhen, ohne F3 zu ändern.
- **Alternatives considered**: `Stack<T>` – weniger flexibel (kein Indexzugriff/Gewichtssumme ohne Zusatzpflege).

## 2. Anbindung des Aufnehmens an die F2-Interaktion

- **Decision**: Weltobjekte bleiben „dumm": ein aufnehmbares Objekt implementiert ein schlankes
  `IPickup` (Gewicht; ist ein `Component`). Der `PlayerInteractionController` (F2) routet beim
  Interagieren: ist das fokussierte Objekt ein `IPickup`, wird es an `PlayerCarry.TryPickup` übergeben;
  sonst wie bisher `Interact()`. `PlayerCarry` ist die einzige Stelle, die den Tragstapel hält.
- **Rationale**: Spielerseitige Logik gehört zum Spieler, nicht ins Weltobjekt. Spiegelt das
  F2-Resolver-Muster (Id→Objekt) und vermeidet, dass Weltobjekte den Spieler kennen müssen.
- **Alternatives considered**: (a) `PickupInteractable.Interact()` ruft `PlayerCarry` über eine
  gefundene Referenz – verworfen (Weltobjekt müsste den Spieler/Global-Lookup kennen). (b) statisches
  Carrier-Singleton – verworfen (globale Kopplung, schlecht testbar).

## 3. Tragdarstellung (Apply)

- **Decision**: Das aufgenommene Welt-GameObject wird an ein Hand-/Stapel-Transform reparentet,
  `Collider` deaktiviert und `Rigidbody` (falls vorhanden) auf `isKinematic = true` gesetzt; die Position
  im Stapel ergibt sich aus dem Index. Beim Ablegen umgekehrt: reparent in die Welt, Physik wieder aktiv,
  Platzierung vor dem Spieler (an der Blickposition / leicht davor).
- **Rationale**: Erhält Objektidentität/-zustand; kein Spawnen/Zerstören nötig. `PlayerCarry` löst die
  Stapel-Id über ein Dictionary `Id→IPickup` auf (analog F2-Resolver).
- **Alternatives considered**: Welt-Objekt zerstören und „Trage-Prefab" spawnen – verworfen (Identitäts-/
  Zustandsverlust, mehr Komplexität).

## 4. Ablegen-Eingabe & Weltplatzierung

- **Decision**: Im Prototyp wird **Ablegen direkt über die Taste `Q`** gelesen (`Keyboard.current.qKey`
  im `PlayerInputRelay`), `PlayerCarry.Drop()` platziert das zuletzt aufgenommene Objekt (LIFO) vor
  dem Spieler. Eine formale „Drop"-Action in der Map folgt mit der finalen Tastenbelegung.
- **Rationale**: Vermeidet fragile JSON-Edits am `InputSystem_Actions`-Asset; die Tastenbelegung ist
  laut Konzept ohnehin noch offen. Eigene Taste vermeidet Konflikt mit dem Aufnehmen (Interact).
- **Alternatives considered**: (a) Drop-Action ins Asset einpflegen – später, wenn Bindings final
  sind. (b) Interact für Aufnehmen UND Ablegen doppelt belegen – verworfen (mehrdeutig).

## 5. Anker-Parenting (Links/Rechts)

- **Decision**: `leftHandAnchor` ist **Kind der Kamera** (screen-fix, aktuelles Objekt immer sichtbar);
  `rightHandAnchor` ist **Kind des Spielerkörpers** (feste Höhe). `RelayoutHands()` legt das oberste
  Stack-Element (zuletzt aufgenommen) auf links, die übrigen ab Index 0 (ältestes = feste Basis) nach
  oben gestapelt auf rechts.
- **Rationale**: Erfüllt die gewünschte Haptik „Stapel in der Hand halten und durch Hochschauen ansehen";
  das aktuelle Objekt bleibt griffbereit im Blick.
- **Alternatives considered**: beide Anker an der Kamera – verworfen (Hochschauen würde den Stapel nicht
  freigeben, da er mit dem Blick mitliefe).

## 6. Test-Spawner & Prefab

- **Decision**: Ein `TestPickup`-Prefab (Cube + Rigidbody mit Schwerkraft + Collider + `PickupInteractable`)
  wird per Editor-Menü erzeugt; ein `TestPickupSpawner` spawnt es bei `O` (`Keyboard.current.oKey`) vor
  dem Spieler, sodass es per Schwerkraft zu Boden fällt. Reine Test-/Prototyping-Hilfe.
- **Rationale**: Schneller manueller Test von Aufnehmen/Stapeln/Ablegen/Gewicht ohne manuelles Platzieren.
  Prefab wird via `PrefabUtility` zuverlässig im Editor erzeugt (kein YAML-Handediting).
- **Alternatives considered**: Laufzeit-Primitive ohne Prefab – verworfen (es wurde explizit ein Prefab gewünscht).

## Offene Punkte

- Keine. Tastenbelegung (Q/O im Prototyp direkt gelesen), Kapazität-Startwert und Stapel-Darstellung sind
  als Defaults festgehalten und leicht justierbar.
