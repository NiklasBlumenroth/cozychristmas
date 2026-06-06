# Feature-Spezifikation: Schlüssel-, Tor- & Sektorfreischaltung

**Feature-Branch**: `008-schluessel-tor-sektor`
**Erstellt**: 2026-06-04
**Status**: Draft

## Änderungsklassifikation & Dokumentationsauswirkung

- **Änderungstyp**: Geplantes Feature (Wasserfall via Spec-Kit)
- **Dokumentationsumfang**: CLAUDE.md (F8-Abschnitt), `04_welt_und_narrative.md` (Tor-/Schlüsselmechanik), `06_content_progression.md` (Freischaltlogik), `05_systeme.md` (KeyInventory)
- **Neue Dokumentation**: `specs/008-schluessel-tor-sektor/` (plan.md, research.md, data-model.md, tasks.md, diagrams/)
- **Dokumentationssprache**: Deutsch (verpflichtend)
- **Kommunikationssprache**: Deutsch
- **Merge-Abdeckungsnachweis**: Jeder neue Core-Typ und jede Runtime-Komponente ist in CLAUDE.md gelistet; Diagramme decken alle Zustandsübergänge und Cross-System-Flows ab
- **Ausschluss persönlicher Notizen**: `Notizen.md`
- **Dokumentations-Split-Analyse**: `05_systeme.md` erhält neuen Abschnitt KeyInventory; kein Split nötig
- **Diagrammumfang**: Activity (Schlüssel-Pickup-Flow), State (Tor-Lebenszyklus), Sequence (Area-Zone-Wechsel → HUD), Class/Domain (Entitäten-Überblick)
- **Diagramm-Abdeckungsnachweis**: Jeder Diagramm-Zweig ist als Testkandidat in der Teststrategie gelistet

## Systemkontext & Kollisionsprüfung

- **Betroffene bestehende Systeme**:
  - F2 `IInteractable` / Interaktions-Raycast: Schlüssel-Pickup nutzt dieselbe Erkennung
  - F3 `PlayerCarry`: Schlüssel gehen NICHT in den Carry-Stack; eigenes `KeyInventory` nötig
  - F4 `SortTargetInteractable.onCompleted`: Andockpunkt für Schlüssel-Spawn nach Area-Abschluss
  - F7 `AreaTracker` / `AreaHudView`: AreaHudView muss mehrstufige/Überlappungs-Anzeige unterstützen; `AreaManager` als neuer Koordinator nötig
- **Bekannte Kollisionen / Konflikte**:
  - F7 `AreaHudView` ist aktuell auf eine einzige Area ausgelegt; muss für dynamisches Switching und Overlap erweitert werden
- **Auflösung vor Implementierung**: HUD-Erweiterungsstrategie ist in `data-model.md` festgelegt, bevor Code entsteht
- **Offene Klärungen**: keine
- **PlantUML-Kontextkarte**: Activity, State, Sequence, Class/Domain (siehe Diagrammumfang)

## Diagrammanforderungen

- **Activity-Diagramme**: `specs/008-schluessel-tor-sektor/diagrams/activity-schluessel-pickup.puml`
- **Sequence-Diagramme**: `specs/008-schluessel-tor-sektor/diagrams/sequence-areazone-wechsel.puml`
- **Class/Domain-Diagramme**: `specs/008-schluessel-tor-sektor/diagrams/class-domain-f8.puml`
- **State-Diagramme**: `specs/008-schluessel-tor-sektor/diagrams/state-gate.puml`
- **Mindmap**: Nicht erforderlich – Feature ist klar in drei unabhängige Stories unterteilt
- **Diagramm-Ausnahmen**: keine

## User-Szenarien & Testing

### User Story 1 – Schlüssel aufheben & im HUD sehen (Priorität: P1)

Der Spieler schließt eine Area ab (z. B. die Poststelle). An einer vordefinierten Position
spawnt ein Schlüssel-Objekt. Der Spieler läuft hin, schaut es an und interagiert – der
Schlüssel verschwindet aus der Welt und erscheint als Icon am Bildschirmrand. Der
Carry-Stack bleibt unverändert; der Spieler kann weiterhin Objekte tragen.

**Warum diese Priorität**: Ohne Schlüssel-Pickup gibt es keine F8-Mechanik.
Dieser Schritt ist das Fundament aller weiteren Stories.

**Unabhängiger Test**: Schlüssel-Prefab in Szene platzieren, interagieren → Icon erscheint
im HUD, Carry-Stack unberührt. Testbar ohne Tore und ohne Area-Zones.

**Akzeptanzszenarien**:

1. **Gegeben** ein Schlüssel-Prefab liegt in der Welt, **wenn** der Spieler es anschaut und interagiert, **dann** verschwindet das Objekt und sein Icon erscheint in der Schlüssel-HUD-Liste
2. **Gegeben** der Spieler trägt bereits 3 Objekte im Carry-Stack, **wenn** er einen Schlüssel aufhebt, **dann** bleibt der Stack unverändert und der Schlüssel landet nur im KeyInventory
3. **Gegeben** der Spieler hat Schlüssel A bereits aufgehoben, **wenn** ein zweites identisches Schlüssel-A-Prefab interagiert wird, **dann** erscheint Schlüssel A weiterhin nur einmal im HUD (kein Duplikat)

---

### User Story 2 – Tor automatisch öffnen (Priorität: P2)

Der Spieler besitzt die nötige Anzahl Schlüssel und nähert sich einem Tor. Das Tor öffnet
sich automatisch. Die verwendeten Schlüssel werden aus dem Inventar entfernt und die Icons
verschwinden aus dem HUD. Das Tor bleibt danach dauerhaft offen.

**Warum diese Priorität**: Das Tor ist der Spielfortschritt-Gate. Ohne es ist die
Schlüssel-Mechanik kein vollständiger Loop.

**Unabhängiger Test**: Tor mit `requiredKeyIds: ["post"]` konfigurieren, Schlüssel „post"
ins KeyInventory setzen, Spieler in Proximity-Zone bewegen → Tor öffnet, Key-Icon
verschwindet. Testbar ohne Area-Zones.

**Akzeptanzszenarien**:

1. **Gegeben** ein Tor braucht Schlüssel A und B, **wenn** der Spieler nur A hat und die Proximity-Zone betritt, **dann** öffnet das Tor nicht
2. **Gegeben** ein Tor braucht Schlüssel A und B, **wenn** der Spieler beide hat und die Zone betritt, **dann** öffnet das Tor automatisch und beide Key-Icons verschwinden
3. **Gegeben** ein Tor mit 1 Schlüssel ist bereits offen, **wenn** der Spieler erneut die Zone betritt, **dann** passiert nichts (Tor bleibt offen, kein weiterer Schlüsselverbrauch)
4. **Gegeben** ein Tor braucht 3 Schlüssel, **wenn** der Spieler 3 verschiedene Schlüssel besitzt, **dann** öffnet das Tor und alle 3 Icons verschwinden

---

### User Story 3 – Area-Zone: HUD-Wechsel & Overlap (Priorität: P3)

Der Level-Designer zieht im Editor einen BoxCollider-Trigger als Area-Zone um jeden
räumlichen Bereich (Gebäude, Außenbereich). Betritt der Spieler eine Zone, ergänzt das
HUD die Aufgabenliste dieser Area. Verlässt er eine Zone, verschwindet deren Aufgabenliste.
Überlappen sich zwei Zonen, werden beide Listen gleichzeitig angezeigt. Ist keine Zone
aktiv, sind keine Aufgaben sichtbar.

**Warum diese Priorität**: Ohne Area-Zones funktioniert das HUD nur statisch. Für den MVP
(Sektor 1 mit zwei Gebäuden + Außenbereich) wird diese Mechanik benötigt, damit der
Spieler immer die richtigen Aufgaben sieht.

**Unabhängiger Test**: Zwei Area-Zones im Editor anlegen, Spieler bewegt sich zwischen
ihnen → HUD zeigt jeweils die korrekte(n) Aufgabenliste(n). Testbar ohne Schlüssel und Tore.

**Akzeptanzszenarien**:

1. **Gegeben** der Spieler befindet sich in keiner Area-Zone, **dann** zeigt das HUD keine Aufgabenliste an
2. **Gegeben** der Spieler betritt Area-Zone A, **dann** zeigt das HUD die Aufgaben von Area A
3. **Gegeben** der Spieler steht in Zone A und betritt Zone B (Overlap), **dann** zeigt das HUD beide Aufgabenlisten gleichzeitig
4. **Gegeben** der Spieler verlässt Zone A, bleibt aber in Zone B, **dann** zeigt das HUD nur noch die Aufgaben von Zone B
5. **Gegeben** der Spieler verlässt alle Zones, **dann** zeigt das HUD keine Aufgaben mehr

---

### Edge Cases

- Was passiert, wenn der Area-Abschluss ausgelöst wird, bevor der Schlüssel-Spawn-Punkt aktiv ist (z. B. noch nicht im Sichtfeld geladen)?
- Was passiert, wenn ein Schlüssel-Prefab in ein Trigger-Volumen eines Tores fällt, bevor der Spieler ihn aufhebt?
- Was passiert, wenn zwei Tore dieselbe Key-ID fordern und der Spieler nur einen Schlüssel hat (erster Treffer verbraucht ihn)?
- Was passiert, wenn der Spieler eine Area-Zone besteht, aber der `AreaTracker` noch keine Tasks hat?

## Anforderungen

### Funktionale Anforderungen

- **FA-001**: Wenn eine Area abgeschlossen wird, MUSS an einer definierten Szenenposition ein Schlüssel-Objekt gespawnt werden
- **FA-002**: Der Spieler MUSS Schlüssel-Objekte über das F2-Interaktionssystem aufheben können
- **FA-003**: Aufgehobene Schlüssel MÜSSEN in einem separaten `KeyInventory` gespeichert werden, nicht im `PlayerCarry`-Stack
- **FA-004**: Das HUD MUSS aufgehobene Schlüssel als Icons anzeigen (editor-authored UI, nur Binding/Update durch Laufzeitcode)
- **FA-005**: Jedes Tor MUSS eine konfigurierbare Liste von 1–3 erforderlichen Schlüssel-IDs im Inspector haben
- **FA-006**: Tore MÜSSEN sich automatisch öffnen, wenn der Spieler die Proximity-Zone betritt und alle erforderlichen Schlüssel besitzt
- **FA-007**: Beim Öffnen eines Tores MÜSSEN die verbrauchten Schlüssel aus dem `KeyInventory` entfernt und ihre Icons aus dem HUD gelöscht werden
- **FA-008**: Geöffnete Tore MÜSSEN dauerhaft offen bleiben
- **FA-009**: Area-Zones MÜSSEN als BoxCollider-Trigger im Editor definiert werden können
- **FA-010**: Betritt der Spieler eine Area-Zone, MUSS das HUD deren Aufgabenliste anzeigen
- **FA-011**: Bei überlappenden Area-Zones MÜSSEN beide Aufgabenlisten gleichzeitig im HUD sichtbar sein
- **FA-012**: Verlässt der Spieler eine Area-Zone, MUSS deren Aufgabenliste aus dem HUD entfernt werden – ohne Fallback auf eine vorherige Zone
- **FA-013**: Ist der Spieler in keiner Area-Zone, MUSS das HUD keine Aufgabenliste anzeigen
- **FA-014**: UI-Elemente DÜRFEN NICHT zur Laufzeit durch Gameplay-Code generiert werden; alle UI-Strukturen sind editor-authored
- **FA-015**: Nach Code-Änderungen MUSS der Compile-Check ohne Fehler durchlaufen
- **FA-016**: Dokumentation und Projektkommunikation MÜSSEN auf Deutsch sein
- **FA-017**: PlantUML-Diagramme MÜSSEN für alle geänderten Fachlogik-/Zustandsbereiche erstellt werden

### Schlüssel-Entitäten

- **Schlüssel (KeyItem)**: Physisches Weltobjekt mit eindeutiger ID; wird durch Interaktion aufgehoben; verschwindet aus der Welt
- **KeyInventory**: Menge gehaltener Schlüssel-IDs; Entscheidungslogik (HasKeys, AddKey, RemoveKeys) ohne Unity-Abhängigkeiten in Core
- **GateController**: Tor-Objekt mit Proximity-Trigger und Liste erforderlicher Key-IDs; Zustandsautomat: Geschlossen → Öffnend → Offen
- **AreaZone**: BoxCollider-Trigger mit Referenz auf einen AreaTracker; meldet Betreten/Verlassen an den AreaManager
- **AreaManager**: Koordiniert aktive Zones und liefert der AreaHudView die kombinierte Aufgabenliste

## Erfolgskriterien

### Messbare Ergebnisse

- **EK-001**: Spieler kann innerhalb von 3 Sekunden nach Area-Abschluss den gespawnten Schlüssel aufheben
- **EK-002**: HUD zeigt Schlüssel-Icons unverzüglich nach dem Aufheben an; kein sichtbares Delay
- **EK-003**: Tor öffnet sich innerhalb von 0,5 Sekunden nach Betreten der Proximity-Zone bei korrekten Schlüsseln
- **EK-004**: Beim Verlassen einer Area-Zone verschwindet deren Aufgabenliste sofort; kein Flackern
- **EK-005**: Alle EditMode-Unit-Tests (K1–K4, G1–G2, Z1–Z2) laufen grün
- **EK-006**: Compile-Check läuft nach Implementierung ohne Fehler durch
- **EK-007**: Alle PlantUML-Diagramme sind vorhanden und mit Testkandidaten verknüpft
- **EK-008**: 100 % der geänderten Code-Bereiche sind in der Dokumentation abgedeckt
