# Quickstart: F3 Tragen & Ablegen testen

Nach der Implementierung in der Grey-Box-Szene.

## Player vorbereiten

1. Am `Player` die Komponente `Player Carry` ergänzen; `Capacity` z. B. auf `1.0` (kg) setzen
   (klein, damit die Gewichtsgrenze testbar ist).
2. Zwei Kind-Transforms unter der Kamera anlegen (Augen-/Brusthöhe, leicht vorne): `LeftHandAnchor`
   (etwas links) und `RightHandAnchor` (etwas rechts). Beide am `Player Carry` zuweisen
   (`Left Hand Anchor` / `Right Hand Anchor`). Links = aktuelles Objekt, rechts = Stapel der übrigen.
3. Am `Player Interaction Controller` das Feld `Carry` = die `Player Carry`-Komponente zuweisen.

## Drop-Action ergänzen

4. Im `InputSystem_Actions`-Asset der Map „Player" eine **Button-Action „Drop"** hinzufügen,
   Binding z. B. `<Keyboard>/q`. (Der `PlayerInputRelay` ruft `OnDrop` → `PlayerCarry.Drop`.)

## Aufnehmbare Objekte

5. Mehrere kleine Würfel mit Collider + `Pickup Interactable` platzieren; `Weight` unterschiedlich
   setzen (z. B. 0.1 / 0.5 / 2.0 kg), um Stapel und Gewichtsgrenze zu testen.

## Prüfen (entspricht den Acceptance-Szenarien)

- Leichtes Objekt anschauen + Interact → liegt in der Hand, ist nicht mehr am alten Platz.
- Zweites/drittes leichtes Objekt → stapeln; zuletzt aufgenommenes „oben/links".
- `Q` (Drop) → zuletzt aufgenommenes Objekt kommt zuerst zurück in die Welt (LIFO) und ist wieder aufnehmbar.
- Schweres Objekt bei kleiner `Capacity` → wird nicht aufgenommen, bleibt in der Welt.

## Tests

- **EditMode**: `CarryStackTests` (LIFO, Kapazität an/über/unter Grenze, leeres Pop, Capacity-Erhöhung).
- **PlayMode**: `CarryPlayModeTests` (Aufnehmen/Stapeln/Ablegen/Überlast mit Fakes).
- **Compile**: Build im Editor bzw. `dotnet build` für die Core-Schicht.
