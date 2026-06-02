# Phase 0 – Research: F4 – Sortiersystem & Sortierfeedback

Alle Entscheidungen wurden vorab in der Konzeptdurchsicht + Konversation geklärt. Keine offenen
`NEEDS CLARIFICATION`.

## R1 – `SortKey`-Repräsentation (datengetrieben, skalierbar)

- **Entscheidung**: `SortKey` ist ein reiner Wertetyp (Core), der eine **geordnete Menge benannter
  Facettenwerte** kapselt (z. B. Poststelle: {Kontinent, Farbe, Symbol}; Lager: {Form, Symbol}).
  Gleichheit erfolgt **wertbasiert** über alle Facetten. Im Grey-Box werden nur wenige Facettenwerte
  bestückt; die volle Konzeptmenge (100 bzw. 25 Kombinationen) entsteht später als reiner Content,
  ohne Code-Änderung.
- **Begründung**: Vom Nutzer gewählt („Generischer SortKey, reduzierter Content"). Ein generischer
  Schlüssel erlaubt Poststelle und Lager mit identischem Code/Prefab-Muster; nur Facettenwerte und
  Soll-Menge unterscheiden sich.
- **Alternativen verworfen**: Einzel-Enum (zu eng für Kombinationen); harte 100-Ziel-Poststelle direkt
  (zu viel Content für den ersten Prototyp).
- **Umsetzungshinweis**: Repräsentation als unveränderliche Facettenwert-Folge (z. B. `string[]`/`int[]`
  hinter einem `readonly struct` mit `IEquatable`); deterministische Gleichheit/HashCode. Reihenfolge der
  Facetten ist Teil des Schlüssels (positionsbezogen), damit kein Mehrdeutigkeitsrisiko entsteht.

## R2 – Granularität: Fach vs. Kasten, Rahmen/Fach-Rollentrennung

- **Entscheidung**: **1 Fach = 1 Zielort = 1 Lampe = 1 `SortKey` + 1 `RequiredCount`.** Der „Kasten"
  (Poststelle) bzw. „Regal" (Lager) ist nur ein editor-authored **Rahmen-Mesh** plus ein **Gitter aus
  Fach-Instanzen** (nested Prefabs). Der Rahmen liefert Sichtbarkeit + Beschriftung (Koordinate/Farbe/
  Symbol); das **Fach** trägt nur die **klickbare Fläche** (Collider in der Öffnung, Pivot mittig auf der
  Öffnung) und die **Korrektheits-Lampe**.
- **Begründung**: Konzept fordert eine Lampe je Fach (09/05). Die Trennung hält das Fach-Prefab minimal
  und wiederverwendbar; der Content-Aufwand ist Duplizieren + Inspector-Werte, kein Code.
- **Konsequenz für Selektion**: `PhysicsInteractionProbe` wählt nach Winkel/Distanz zur
  `transform.position`; deshalb **Pivot des Fach-Roots auf das Öffnungszentrum** legen, Fächer für den
  Grey-Box bewusst aim-freundlich groß, mit Stegen/Lücken (keine überlappenden Collider).

## R3 – Auflösung der `PhysicsInteractionProbe`-Pufferkollision

- **Problem**: Die Probe scannt per `OverlapSphereNonAlloc` mit **festem Puffer 32**
  (`PhysicsInteractionProbe.cs:20,28`). 25 einzeln kollidierbare Briefe je Fach würden den Puffer sättigen
  und echte Kandidaten verschlucken.
- **Entscheidung**: **Eingelegte Objekte tragen keine aktiven Interaktions-Collider** (Collider beim
  Einsortieren deaktiviert; das Objekt wird zum reinen sichtbaren Mesh). **Entnahme läuft über das Fach**
  (genau ein Interactable je Fach), nicht über einzelne Objekte. Die Objekt-Identität/Sortierdaten bleiben
  für die Entnahme im Fach erhalten.
- **Begründung**: Vom Nutzer gewählt; löst Sichtbarkeit (Fehler erkennen) und Performance/Selektion in
  einem. Spätere „gezielt das falsche Objekt greifen"-Politur bleibt möglich (z. B. eigener kurzer Raycast),
  ist aber nicht F4-Scope.

## R4 – Einsortieren / Entnehmen – Eingabe

- **Entscheidung**: Beides läuft über die bestehende **F2-Interact-Action** auf das fokussierte Fach,
  **kontextabhängig**: trägt der Spieler etwas und ist das Fach offen → **Einsortieren** (oberstes
  getragenes Objekt übergeben); trägt er nichts und das Fach enthält entnehmbare Objekte → **Entnahme**
  (LIFO) zurück in die Hand. Abgeschlossene Fächer sind gesperrt (keine Aktion).
- **Begründung**: Minimiert neue Tastenbelegung im offenen Konzept; der Kontext (Hand voll/leer) ist
  eindeutig. Finale Belegung ist laut Konzept offen und leicht änderbar; eine spätere dedizierte
  Entnahme-Action bleibt möglich.
- **Kapazität**: Entnahme nutzt `PlayerCarry.TryPickup`-Pfad (bzw. dieselbe Kapazitätsprüfung) → bei
  Überlast wird die Entnahme abgelehnt, das Objekt bleibt im Fach (F3-`CarryStack.CanPickUp`).

## R5 – Optimierung bei Abschluss (Schließen)

- **Entscheidung**: Sobald `RequiredCount` korrekte **und** 0 falsche Objekte erreicht sind: Lampe an,
  Fach **schließt** (gesperrt), eingelegte Objekt-Visuals werden entfernt/versteckt, Fach-Collider
  deaktiviert. Der **autoritative Zählstand bleibt im Core-`SortTarget`**; die Visuals sind entbehrlich.
  Offene Fächer behalten ihre Einzel-Visuals (Fehlererkennung).
- **Begründung**: Vom Nutzer gewählt; entlastet Rendering/Selektion bei großen Konzeptmengen, ohne
  Spielzustand zu verlieren. Narrativ stimmig („erledigtes Fach").

## R6 – Abgrenzung zu Nachbarsystemen

- **F6 (XP/Skill)**: F4 vergibt keine XP. Bei Fach-Abschluss wird **ein einmaliges Abschluss-Ereignis**
  bereitgestellt (Andockpunkt). Korrekturen (Entnahme) vergeben keine XP.
- **F9 (Sortierblick)**: F4 zeigt **nicht** an, wo ein Objekt hingehört. Orientierung liefert allein die
  Rahmen-Beschriftung. Der Sortierblick ist ein späterer eigener Skill.
- **F12 (Geschenkcontainer)**: Das **harte** Auswerfen falscher Objekte über ein Deckenrohr + 25er-Batch
  + 1-min-Cooldown ist explizit **nicht** F4. F4 implementiert das **sanfte** Fach-Feedback (Lampe bleibt
  aus, Objekt bleibt liegen, korrigierbar).

## R7 – Fallback-Prefabs

- **Entscheidung**: Grey-Box-Platzhalter via Editor-Tool und/oder manuell:
  - **Sortierbares Objekt**: auf `TestPickup`-Basis (Cube/flache Box + Rigidbody + Collider +
    `PickupInteractable`) **plus** `Sortable` (SortKey).
  - **Fach**: leeres Root (Pivot Öffnungsmitte) + `SortTargetInteractable` + `BoxCollider` (Öffnung) +
    `SlotAnchor` (leeres Kind) + `Lampe` (Emissive-Mesh oder `Light`, disabled by default). Optionales
    Fach-Mesh.
  - **Rahmen (optional)**: ein Mesh + Gitter aus Fach-Instanzen; trägt Beschriftung.
- **Begründung**: Meshes fehlen; data-model/quickstart legen verbindliche Anker-/Lampen-/Pivot-Konventionen
  fest, damit selbst gebaute Platzhalter und Code zusammenpassen.
- **Offen (Lampen-Variante)**: Emissive-Mesh (`SetActive`/Emission-Toggle) **oder** `Light`-Komponente
  (`enabled`-Toggle) – beide werden vom Runtime-Apply unterstützt; finale Wahl trifft der Ersteller im
  Prefab (Standard: Emissive-Mesh, einfachstes Grey-Box-Setup).
