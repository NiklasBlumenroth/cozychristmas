# Quickstart: F4 Sortieren & Sortierfeedback testen

Nach der Implementierung in der Grey-Box-Szene. Baut auf dem F3-Setup auf (`PlayerCarry` mit
Hand-Ankern, `PlayerInteractionController.Carry` gesetzt).

## Fach-Prefab bauen (Konventionen – wichtig für Code-Kompatibilität)

Ein **Fach** ist die kleinste Sortier-Einheit (1 Fach = 1 Lampe = 1 `SortKey` + 1 Soll-Menge):

```text
SortFach            ← Root: SortTargetInteractable (+ acceptedFacets, requiredCount)
│                     Pivot = ÖFFNUNGSZENTRUM (Selektion zielt auf transform.position!)
├── Collider        ← BoxCollider in der Öffnung, Layer = Interactable-/Probe-Maske
├── SlotAnchor      ← leeres Transform: hierhin reparenten eingelegte Objekte
└── Lampe           ← Emissive-Mesh ODER Light, DISABLED by default
```

- **Pivot mittig** auf die Öffnung legen (sonst springt die Auswahl bei eng stehenden Fächern).
- **Collider** füllt die Öffnung, **keine** Überlappung mit Nachbarfächern (Stege/Lücken lassen).
- **Lampe** standardmäßig aus; der Code schaltet sie nur beim Abschluss.
- Ein eigenes Fach-Mesh ist optional – die Sichtbarkeit liefert der Rahmen (s. u.).

## Kasten/Regal (Rahmen + Fach-Gitter)

1. Ein **Rahmen-Mesh** (Poststellen-Kasten bzw. Lager-Regal) anlegen; es trägt die **Beschriftung**
   (Koordinate/Farbe/Symbol je Fach) – editor-authored, reine Orientierung.
2. Pro Fach eine **Instanz** des `SortFach`-Prefabs in das Gitter setzen.
3. Je Instanz im Inspector `acceptedFacets` (z. B. `["Europe","Rot","Stern"]`) und `requiredCount`
   (Grey-Box klein, z. B. `2`–`3` statt 25) setzen.
4. **Konsistenz beachten**: Die Rahmen-Beschriftung an dieser Stelle muss zum `acceptedFacets` der
   dort sitzenden Instanz passen (sonst wirkt das Fach „nie befüllbar"). Editor-Validierung hilft.

## Sortierbares Objekt

5. Auf `TestPickup`-Basis ein Objekt mit `Pickup Interactable` (F3) **plus** `Sortable` versehen;
   `facets` passend setzen (z. B. ein „Brief" `["Europe","Rot","Stern"]`, ein falscher `["Asia","Blau","Teddy"]`).
6. Mehrere passende + mindestens ein nicht passendes Objekt platzieren, um korrekt/falsch zu testen.

## Prüfen (entspricht den Acceptance-Szenarien)

- Passendes Objekt aufnehmen, Fach anschauen, Interact → Objekt liegt sichtbar im Fach, Hand ist leer (US1).
- Fach mit `requiredCount` passenden Objekten füllen → Lampe geht an, Fach schließt, eingelegte Objekte
  verschwinden (US2).
- Ein **nicht** passendes Objekt einsortieren → bleibt im Fach, Lampe bleibt aus, Fach gilt nicht als
  abgeschlossen (US3).
- Mit leerer Hand das offene Fach anschauen + Interact → zuletzt eingelegtes Objekt kehrt in die Hand
  zurück (LIFO); bei voller Tragkapazität wird die Entnahme abgelehnt (US4).
- Falsches Objekt entnehmen, korrektes nachlegen → Fach kann jetzt doch abschließen.

## Tests

- **EditMode**: `SortTargetTests` (S1–S10: `SortKey`-Gleichheit, Klassifizierung, Vollständigkeit/Grenze,
  falsch hält Lampe aus, LIFO-Entnahme, leeres/abgeschlossenes Fach, Abschluss-Signal).
- **PlayMode**: `SortingPlayModeTests` (E1–E4: Einsortieren→Lampe, sanftes Fehlerfeedback, Entnahme/Kapazität).
- **Compile**: Build im Editor bzw. `dotnet build` für die Core-Schicht.

## Optional: Editor-Setup-Menü

- „CozySanta/Setup F4 (Sortieren + Test-Fach)" erzeugt Fallback-Prefabs (sortierbares Objekt + Fach
  mit Slot-Anker + Emissive-Lampe) und ein kleines Beispiel-Gitter, falls keine eigenen Platzhalter
  vorliegen.
