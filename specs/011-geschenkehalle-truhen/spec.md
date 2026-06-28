# F11 – Geschenkehalle: Truhen-Sortierung

## Ziel

In der Geschenkehalle sortiert der Spieler Geschenke. Zwei Item-Klassen:

- **Regal-Geschenke** (`Prefabs/Geschenke/`, „Toys"): werden in die Regale (`gabinet`)
  einsortiert – über das bestehende Fach-System (`SortTargetInteractable`, F4). 70 je Variante
  (2 Regale pro Geschenkart).
- **Truhen-Geschenke** (`Prefabs/Geschenke/Truhengeschenke/`): werden in **Truhen** (`chest`)
  einsortiert – über das neue `GiftChest`-System dieser Feature. 100 je Variante, 20 Truhen
  (eine Sorte je Truhe).

Beide Arten gehören zu **einem** Bereich (eine Geschenkehalle, kein zweiter Raum): sie liegen in
**einem** gemeinsamen Katalog (`GeschenkehalleCatalog`) und werden über die eine Geschenkehalle-
`ItemArea` verteilt/gespawnt. Unterschiedlich ist nur die Höchstzahl je Variante (Toys 70,
Truhen-Geschenke 100, variantenspezifisch im Katalog) und das Sortierziel (Regal vs. Truhe).

## Truhen-Mechanik

1. **Öffnen/Schließen**: Rechtsklick auf die fokussierte Truhe schaltet den Deckel (`chest_top`)
   auf/zu. Offener Winkel > 90°, damit oben Platz zum Einwerfen ist.
2. **Befüllen**: Der Spieler wirft Geschenke physisch in die offene Truhe (Q = ablegen, Items
   fallen in die Truhen-Schale).
3. **Validierung beim Schließen** (alles-oder-nichts):
   - **Alle** enthaltenen Items = akzeptierte Sorte → Items verschwinden, Zählstand += Anzahl.
   - **Mind. ein** falsches Item → **gesamter** Inhalt fliegt zur `RöhrenPosition` zurück.
4. **Verriegelung**: Erreicht der Zählstand die Soll-Menge (100), verriegelt die Truhe dauerhaft
   (`PromptText` = „Abgeschlossen", Interaktion gesperrt). Der Spieler weiß: diese Geschenkart
   ist fertig.

## Architektur (Constitution IX)

| Schicht | Typ | Verantwortung |
| --- | --- | --- |
| Core | `GiftChestValidation.Decide` | Reine Entscheidung: alles-korrekt? Annahme-Anzahl? verriegelt? |
| Runtime | `GiftChest` (`.cs` + `.Validate.cs`) | `IInteractable`, Deckel-Slerp, OverlapBox-Inhalt, Annehmen (Destroy)/Auswerfen (zur Röhre), Verriegeln |
| Runtime | `AreaTracker.ChestGroup` | bindet jede verriegelte Truhe → `BookSort(taskId, +1)`; Auto-Soll-Menge = Anzahl Truhen |
| Input | `PlayerInputRelay` | diskreter Rechtsklick auf fokussierte `GiftChest` → `Interact()` |
| Editor | `GeschenkItemSetup` | beide Ordner → Sortierobjekte (Rigidbody/Pickup/Sortable/Collider/PrefabId/SettlingBody) + EIN gemeinsamer Katalog (Max je Variante) |
| Editor | `GiftChestSetup` | Truhen-Instanzen → `GiftChest` + Innenvolumen + Deckel/Röhre verdrahten + Sorte aus Katalog (nur Truhen-Ordner-Varianten, Reihenfolge) |

- **Decide/Apply getrennt**: Validierungsregel in Core (testbar), Seiteneffekte (Deckel, Destroy,
  Auswurf) in Runtime.
- **Wiederverwendung**: SortKey/Matches (F4), Deckel-Slerp (Muster `GateController`), Persistenz-
  Andockpunkte (`SettlingBody.BeginSettling`, `InstancedItemRenderer.Unregister`), Area-Buchung
  (`AreaProgress.BookSort`, F7).

## SortKey-Schema

Eine Facette = Prefab-Name (z. B. `["GiftBox_Square_TypeA"]`). „Eine Sorte je Truhe" = exakter
SortKey-Match (`SortKey.Matches` ist positional-strikt). Die Truhe liest ihren akzeptierten Key
aus dem Katalog (Reihenfolge), deckungsgleich mit den Prefab-`Sortable`.

## Tests

EditMode `GiftChestValidationTests` (GC1–GC6): leerer Inhalt, alle korrekt, ein falsches weist
alles ab, Verriegeln bei/über Soll-Menge, kein required → nie verriegelt. Setup-Tools = Editor-
Authoring (dokumentierte Nicht-Unit-Ausnahme analog `BookPrefabSetup`/`BakerySweetItemSetup`).

## Manuelle Schritte (User)

1. Truhen-Geschenke auf 20 Varianten reduzieren (Soll-Menge je 100).
2. Truhen (20) und Regale platzieren; `RöhrenPosition` existiert bereits.
3. Setup ausführen:
   - „CozySanta/Geschenkehalle/Geschenke als Sortierobjekte einrichten (Prefabs + Katalog)"
   - „CozySanta/Geschenkehalle/Truhen einrichten (GiftChest + Innenvolumen + Sorte)"
4. Am Geschenkehalle-`AreaTracker` eine Truhen-Gruppe (root = gemeinsamer Eltern der Truhen,
   taskId) hinzufügen; den gemeinsamen `GeschenkehalleCatalog` der Geschenkehalle-`ItemArea` zuweisen.
5. Pro Truhe: Innenvolumen-Größe und Deckel-Öffnungswinkel feintunen.

## Out of Scope (v1)

Regal-Zuweisung der Toys (bestehendes `SortTargetInteractable`-System, separat wie Dekohalle/
Bäckerei). Truhen-spezifische Einlage-Pose (Items fallen frei hinein, kein Slot-Ghost).
