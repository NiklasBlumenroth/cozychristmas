# F12 – Magische Sortierhilfen (Auto-Einsortieren & Heranholen)

## Ziel

Zwei freischaltbare „magische" Sortierhilfen, die das Sortieren beschleunigen und sich als Paar
ergänzen (wegschicken ↔ heranholen). Beide brauchen ein **gehaltenes Sortierobjekt** als Vorlage,
besitzen **Ladungen + Cooldown** (stapelbar) und tun nichts (verbrauchen nichts), wenn kein
gültiges Ziel/keine Kopie existiert.

- **A – Auto-Einsortieren** (Taste `2`): das oberste gehaltene Objekt fliegt selbstständig in ein
  passendes Ziel im aktuellen Gebäude und wird dort eingelegt. 1 Ladung = 1 Objekt.
- **B – Heranholen** (Taste `3`): zufällige ruhende Kopien derselben Sorte im aktuellen Gebäude
  fliegen in die Hand. Eine Auslösung holt `min(Ladungen, freie Traglast)` Stück.

Beide ersetzen im Design die entfernten Skills **Lichtkegel** und **Sortierblick** (siehe
`GameKonzept/docs/game-concept/05_systeme.md`).

## A – Auto-Einsortieren

**Vollautomatische Zielwahl** (kein Anvisieren). Vorlage = oberstes getragenes Objekt
(`PlayerCarry.TryPeekTopComponent` → `ISortable.Key`). Gesucht wird **nur im aktuellen Gebäude**
(`ItemArea.Contains(Spieler)`).

1. **Normales Fach** (`SortTargetInteractable`): alle Fächer mit passender Facette (`acceptedFacets`
   + `placeableArts`), die nicht geschlossen sind und eine freie Zelle haben → **zufällig eines**
   wählen → bestehendes `PlaceInColumn(x, y, carry)` übernimmt Flug + Einlegen + ggf. Abschluss.
   - Additive Helfer-API: `SortTargetInteractable.TryFindFreeColumn(SortKey, out x, out y)`
     (respektiert Sperre + Vollstand; keine Änderung an der Einlege-Logik selbst).
2. **Truhe** (`GiftChest`): kein Slot-Raster, sondern Physik-Volumen (**Option C – Einwerfen**):
   - Deckel öffnen, falls zu (additive `GiftChest.EnsureOpen()`).
   - **Voll-Prüfung (Einwurf-Zone):** `Physics.OverlapBox` im oberen Bereich des `InsideVolume`.
     Ist die Zone bereits durch gestapelte Items blockiert → Truhe gilt als voll → Fähigkeit nicht
     möglich (keine Ladung).
   - Sonst: Objekt zum Einwurfpunkt (oben mittig, leicht zufällig versetzt) fliegen lassen und mit
     Physik fallen/stapeln lassen (`CarriedItemFlight.BeginToWorld` → `SettlingBody.BeginSettling`).
   - **Schließen/Validieren bleibt manuell** – die bestehende GiftChest-Logik (alle korrekt → weg /
     eins falsch → Auswurf; Verriegeln bei Soll-Menge) bleibt unangetastet.

**Mehrere Treffer:** Gibt es mehrere passende Fächer/Truhen, wird **bei jeder Auslösung neu
zufällig** entschieden.

**Halten (`HoldRepeatTimer`):** sendet pro Intervall das jeweils oberste passende Objekt, solange
Ladungen + Ziele reichen.

**Grenzen / No-op:** kein gehaltenes Objekt; oberstes Objekt hat kein gültiges Ziel im Gebäude;
keine Ladung. In all diesen Fällen passiert nichts und es wird **keine** Ladung verbraucht.

## B – Heranholen

Vorlage = oberstes getragenes Objekt → dessen `PrefabId.Key`. Quelle = **vorhandene ruhende Items**
(`SettlingBody` ruhend) mit gleichem `PrefabId.Key` unter dem aktuellen Gebäude
(`ItemArea.ItemParent`).

1. Kandidaten sammeln (ruhend, gleicher Key, im aktuellen Gebäude).
2. **Menge** = `min(verfügbare Ladungen, freie Traglast)` – beide Faktoren limitieren.
   Freie Traglast über `PlayerCarry.CanCarry(weight)` (Abbruch, sobald voll).
3. **Zufällige** Auswahl dieser Menge → je Item `PlayerCarry.TryPickup(pickup)` (übernimmt Physik,
   Abmelden vom `InstancedItemRenderer`, Flug an den Hand-Anker über `RelayoutHands`).
4. Je tatsächlich geholtes Item: 1 Ladung verbraucht.

**Halten:** wiederholt das Batch-Heranholen, sobald Ladungen nachgeladen sind / Traglast frei wird.

**Grenzen / No-op:** kein gehaltenes Objekt; keine ruhende Kopie im Gebäude; 0 Ladungen oder 0
freie Traglast.

## Ladungen & Cooldown

Core `ChargeStack` (Decide, testbar): `maxCharges`, `rechargeSeconds`; `Step(deltaSeconds)` lädt bis
`maxCharges` auf, `TryConsume()` zieht eine ab (false bei 0), `Current`, Fortschritt zur nächsten
Ladung. Je Fähigkeit ein eigener `ChargeStack` (getrennte Pools). Werte kommen aus der Skillstufe.

## Skill-Modell & anpassbare Stufen-Datei

Je Fähigkeit **ein** Skill; höhere Stufe senkt den Cooldown **und** erhöht die max. Ladungen
(gekoppelt). Erste Investition = Freischaltung (`IsUnlockable`).

**Enum-Umbau** `SkillId`: `LampCone` **raus**, `SortVision` **raus**, `ObjectPull` → **Heranholen**,
neu **AutoSort**. Ergebnis: **6** Skills (vorher 7).

Folge: `MeltController.MeltRadius` verliert seinen Skill → **fester Wert 1.2** (Lampenkegel als
Mechanik bleibt, nur nicht mehr upgradebar).

**Stufen-Konfiguration als ScriptableObject** (`Assets/_Project/Data/SkillTable.asset`, Typ
`SkillTableConfig`), im Inspector tabellarisch editierbar. Laufzeit baut daraus die Core-
`SkillConfig[]` (Core bleibt rein/testbar; das Asset ist Runtime-Daten). Pro Zeile:
`id, anzeigename, gruppe, freischaltbar, stufen, w1(start/end/einheit), w2(start/end/einheit)`.
Die meisten Skills nutzen nur Wert 1; die zwei Fähigkeiten zusätzlich Wert 2 (Ladungen).

Startentwurf (später frei im Inspector anpassbar):

| id | Anzeige | Gruppe | Frei | Stufen | W1 Start→End | W1 Einheit | W2 Start→End | W2 Einheit |
| --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| LampPower | Schmelzstärke | Lampe | nein | 20 | 1.2 → 3.0 | x | – | – |
| LampBattery | Akku | Lampe | nein | 20 | 12 → 32 | s | – | – |
| CarryCapacity | Tragkraft | Tragen | nein | 20 | 5 → 25 | kg | – | – |
| MoveSpeed | Laufgeschw. | Bewegung | nein | 20 | 3.0 → 6.0 | m/s | – | – |
| ObjectPull | Heranholen | Heranholen | ja | 20 | 10 → 4 | s (Cooldown) | 1 → 5 | Ladungen |
| AutoSort | Auto-Einsortieren | Einsortieren | ja | 20 | 8 → 3 | s (Cooldown) | 1 → 5 | Ladungen |

Ladungen steigen ganzzahlig (≈ +1 alle 5 Stufen), Cooldown linear fallend.

## Tasten / Eingabe

| Taste | Aktion |
| --- | --- |
| `2` | A – Auto-Einsortieren (Hold = mehrfach) |
| `3` | B – Heranholen (Hold = mehrfach) |
| Numpad `7/8/9/0/6` | `DevTeleportPad` Slots 1–5 (von den oberen Ziffern 1–5 verlegt; Reihenfolge anpassbar) |

`PlayerInputRelay` erhält die zwei neuen Tasten (mit `HoldRepeatTimer`); `DevTeleportPad` wechselt
auf die Numpad-Tasten.

## Architektur (Constitution IX)

| Schicht | Typ | Verantwortung |
| --- | --- | --- |
| Core | `ChargeStack` | Ladungen + Cooldown (Decide, testbar) |
| Core | `SkillScaling` (o. ä.) | aus Start/End/Stufe den Stufenwert berechnen (linear + ganzzahlige Ladungen) |
| Runtime | `MagicSortAbility` (A) | Vorlage lesen, Ziel im Gebäude finden (Fach/Truhe), Flug+Einlegen, Ladung |
| Runtime | `MagicGatherAbility` (B) | Vorlage lesen, ruhende Kopien finden, Batch-Aufnahme, Ladung |
| Runtime | `SkillTableConfig` (ScriptableObject) | editierbare Stufen-Daten → Core-`SkillConfig[]` |
| Runtime (additiv) | `SortTargetInteractable.TryFindFreeColumn` | freie Zelle für einen SortKey finden |
| Runtime (additiv) | `GiftChest.EnsureOpen` + Voll-Prüfung/Einwurfpunkt | Auto-Einsortieren in Truhen |
| Erweitert | `PlayerInputRelay` | Tasten 2/3 mit Hold → Abilities |
| Erweitert | `PlayerProgression` | `SkillTableConfig` laden, neue Skills anwenden (Cooldown/Ladungen) |
| Erweitert | `SkillId`, `ProgressionSetup`, `SkillMenuView` | 7 → 6 Skills, neue Namen/Gruppen |
| Erweitert | `DevTeleportPad` | Numpad-Tasten |

- **Decide/Apply getrennt:** Ladungs-/Stufenmathematik in Core (testbar); Welt-/Physik-/UI-Effekte
  in der Runtime.
- **Wiederverwendung:** `FlightProgress` + `CarriedItemFlight` (Flüge), `PlaceInColumn` (Fach),
  `TryPickup`/`RelayoutHands` (Hand), `SettlingBody`/`PrefabId`/`ItemArea` (Ruhe-Items & Gebäude),
  `HoldRepeatTimer` (Halten).

## Datenmodell / Persistenz

Ladungen/Cooldown sind reiner Laufzeitzustand (nicht persistiert). Die Stufen-Daten liegen im
`SkillTableConfig`-Asset.

## Menü (editor-authored, Constitution V)

6 Einträge, Gruppen: **Lampe** (Schmelzstärke, Akku) · **Tragen** · **Bewegung** · **Heranholen** ·
**Einsortieren** (die letzten beiden Freischalt-Skills). Umbau von `ProgressionSetup` (Setup F6),
`SkillMenuView` und der Index-Schleifen in `PlayerProgression`.

## Tests

- EditMode `ChargeStackTests`: Aufladen bis max, Verbrauch, kein Verbrauch bei 0, Cooldown-Takt.
- EditMode `SkillScalingTests`: Stufenwert Start/Mitte/End, ganzzahlige Ladungen, Deckelung.
- EditMode für die Auswahl-Entscheidungen, soweit als reine Core-Funktion herauslösbar
  (z. B. „Menge = min(Ladungen, Traglast)", Kandidatenfilter). Welt-/Physik-/Editor-Teile =
  dokumentierte Nicht-Unit-Ausnahme.

## Diagramme

PlantUML unter `specs/012-magische-sortierhilfen/diagrams/` für: Ladungs-/Cooldown-Zustand und die
zwei Ability-Abläufe (A: Fach vs. Truhe; B: Batch-Heranholen).

## Out of Scope (v1)

- In Fächer bereits einsortierte Items als Heranhol-Quelle (nur ruhende Welt-Items).
- Hallenübergreifendes Wirken (immer nur aktuelles Gebäude).
- A wirkt nur auf das **oberste** gehaltene Objekt (kein Durchsuchen des Stapels).
- Persistenz von Ladungen über Szenen/Sessions.

## Offene Punkte

- Truhen-Voll-Prüfung bei Option C ist heuristisch (Einwurf-Zonen-Overlap) – Feinjustage der
  Zonengröße beim Playtest.
- Konkrete Stufen-Zahlen sind ein Startentwurf (im Asset anpassbar).
