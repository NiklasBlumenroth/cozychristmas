# Feature Specification: F4 – Sortiersystem & Sortierfeedback

**Feature Branch**: `004-sortiersystem-feedback` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: 2026-06-02
**Status**: Draft
**Input**: User description: "F4 Sortiersystem und Sortierfeedback: Objekte aus der Hand an Zielorte (Fächer) einsortieren, generischer SortKey, konfigurierbare Soll-Menge je Fach, Lampe als Korrektheits-/Vollständigkeitsfeedback, sanftes Fehlerfeedback ohne harte Strafe, Entnahme über das Fach"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: `CLAUDE.md` (Fortschritt/Status), neue Spec-Artefakte unter `specs/004-sortiersystem-feedback/`. Baut additiv auf F2-Interaktion (`IInteractable`/Routing) und F3-Tragen (`PlayerCarry`/`CarryStack`) auf, ohne deren Verträge zu brechen.
- **New Documentation**: spec, plan, research, data-model, quickstart, tasks; PlantUML unter `diagrams/`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German for project communication
- **Merge Coverage Evidence**: Review prüft, dass Einsortieren, Klassifizierung (korrekt/falsch), Vollständigkeit + Lampe, sanftes Fehlerfeedback und Entnahme in Plan/Tasks beschrieben und mit Tests/Diagrammen verknüpft sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: State (Fach-Lebenszyklus) + Activity (Einsortier-/Entnahme-Entscheidung) + Class (Sortierdaten). Sequence begründet ausgelassen.
- **Diagram Coverage Evidence**: Diagramme unter `specs/004-sortiersystem-feedback/diagrams/`, verknüpft mit den Klassifizierungs-/Vollständigkeits-/Entnahme-Tests.

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: F2-Interaktion (`IInteractable.Interact`, `PlayerInteractionController`-Routing, `PhysicsInteractionProbe`), F3-Tragen (`PlayerCarry.TryPickup`/`Drop`, `CarryStack` LIFO, `IPickup`), F1-Core-Schichtung. Eingabe nutzt die bestehende F2-Interact-Action.
- **Known Collisions/Conflicts**:
  - `PhysicsInteractionProbe` scannt per `OverlapSphereNonAlloc` mit **fester Puffergröße 32** im Radius. Viele einzeln kollidierbare Objekte in einem Fach würden den Puffer sättigen und Kandidaten verschlucken. → Auflösung: **eingelegte Objekte tragen keine aktiven Interaktions-Collider**; Entnahme läuft über das Fach (genau ein Interactable je Fach).
  - `IInteractable.Interact()` ist generisch. F4 ergänzt eine neue Implementierung (`SortTargetInteractable`), bestehendes Verhalten anderer `IInteractable` bleibt unverändert.
  - F3 `PlayerCarry` ist bislang die Autorität über getragene Objekte. F4 ergänzt Übergaben Hand→Fach (Einsortieren) und Fach→Hand (Entnehmen) additiv, ohne die LIFO-Stapellogik zu ändern.
- **Resolution Before Implementation**: Granularität „1 Fach = 1 Zielort = 1 Lampe = 1 SortKey + 1 Soll-Menge" festgelegt; Rahmen liefert Sichtbarkeit/Beschriftung, das Fach nur klickbare Fläche + Lampe. Siehe data-model.md.
- **Open Clarifications**: Keine offen (Annahmen siehe „Assumptions").
- **PlantUML Context Map**: State (Fach-Lebenszyklus), Activity (Einsortier-/Entnahme-Entscheidung inkl. Klassifizierung & Vollständigkeit), Class (Sortierdaten Core ↔ Runtime).

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

- **Activity Diagrams**: `diagrams/sortier-entscheidung.puml` – Einsortieren (Klassifizierung korrekt/falsch, Vollständigkeitsprüfung, Lampe/Schließen) und Entnahme (LIFO, gesperrt wenn geschlossen).
- **Sequence Diagrams**: Not required: Der Hand↔Fach-Fluss ist über die F2-Interact-Sequence plus die Activity ausreichend abgedeckt; keine neue mehrstufige systemübergreifende Kette.
- **Class/Domain Diagrams**: `diagrams/sortierdaten.puml` – `SortKey`, `SortTarget`, Platzierungs-Eintrag, Zustand; Beziehung Core ↔ Runtime.
- **State Diagrams**: `diagrams/fach-zustand.puml` – Lebenszyklus eines Fachs: Leer → Teilweise → (FalschEnthalten) → Vollständig/Geschlossen.
- **Mindmap**: Not required: klar abgegrenztes Feature.
- **Diagram Exceptions**: Sequence bewusst ausgelassen (Begründung siehe oben).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Objekt korrekt einsortieren (Priority: P1)

Der Spieler trägt ein passendes Objekt (linke Hand / oberster Stapel) und schaut ein Fach an. Per Interaktion legt er das Objekt in genau dieses Fach; es wird dort sichtbar abgelegt und vom Fach als korrekt gezählt.

**Why this priority**: Das Übergeben Hand→Fach mit Korrektheitsprüfung ist der Kern von F4 und Voraussetzung für jedes Sortier-Gameplay (Poststelle, Lager) sowie für F9 (Sortierblick) und F12 (Geschenkcontainer).

**Independent Test**: In der Grey-Box-Szene ein zum Fach passendes Objekt aufnehmen, das Fach anschauen, Interagieren → Objekt verlässt die Hand, liegt sichtbar im Fach, Zähler „korrekt" steigt um 1.

**Acceptance Scenarios**:

1. **Given** der Spieler trägt mindestens ein Objekt und schaut ein nicht geschlossenes Fach an, **When** er Interagieren drückt, **Then** wird das oberste getragene Objekt in das Fach übergeben und dort sichtbar abgelegt.
2. **Given** das übergebene Objekt passt zum `SortKey` des Fachs, **When** es abgelegt wird, **Then** zählt das Fach es als korrekt (Korrekt-Zähler +1).
3. **Given** der Spieler trägt nichts, **When** er ein Fach anschaut und Interagieren drückt, **Then** passiert nichts (kein Effekt).

---

### User Story 2 - Vollständigkeit & Lampenfeedback (Priority: P1)

Sobald ein Fach die konfigurierte Soll-Menge korrekter Objekte enthält und kein falsches Objekt enthält, leuchtet seine Lampe auf, das Fach gilt als abgeschlossen, schließt sich und die eingelegten Objekte werden aus Leistungsgründen entfernt (der Zählstand bleibt autoritativ erhalten).

**Why this priority**: Die Lampe ist das im Konzept geforderte räumliche Abschluss-Feedback und der lokale Belohnungsmoment; ohne sie hat Sortieren keinen sichtbaren Erfolg.

**Independent Test**: Ein Fach mit Soll-Menge = N und ausschließlich korrekten Objekten bis N füllen → Lampe geht an, Fach schließt, eingelegte Objekte verschwinden, Zähler meldet „vollständig".

**Acceptance Scenarios**:

1. **Given** ein Fach enthält genau `RequiredCount` korrekte und 0 falsche Objekte, **When** das letzte korrekte Objekt abgelegt wird, **Then** schaltet die Lampe an und das Fach gilt als abgeschlossen.
2. **Given** ein Fach ist abgeschlossen, **When** der Spieler es anschaut und Interagieren drückt, **Then** passiert nichts (gesperrt: kein weiteres Ablegen, keine Entnahme).
3. **Given** ein Fach wird abgeschlossen, **When** der Abschluss eintritt, **Then** wird ein einmaliges „Fach abgeschlossen"-Ereignis ausgelöst (Andockpunkt für XP in F6) und die eingelegten Objekt-Visuals werden entfernt.

---

### User Story 3 - Sanftes Fehlerfeedback bei falscher Sortierung (Priority: P2)

Legt der Spieler ein nicht passendes Objekt in ein Fach, wird das nicht hart verhindert. Das Objekt bleibt im Fach liegen, wird als falsch gezählt, und die Lampe bleibt aus – das Fach gilt nicht als abgeschlossen.

**Why this priority**: Das konzeptkonforme, straffreie Fehlerfeedback (Lampe bleibt aus statt Game Over / Zurückwerfen) unterscheidet F4 vom späteren harten Geschenkcontainer (F12) und macht das System cozy.

**Independent Test**: Ein zum Fach **nicht** passendes Objekt einsortieren → Objekt liegt im Fach, Falsch-Zähler steigt, Lampe bleibt aus, auch wenn die Soll-Menge an korrekten Objekten sonst erreicht wäre.

**Acceptance Scenarios**:

1. **Given** der Spieler übergibt ein zum `SortKey` **nicht** passendes Objekt, **When** es abgelegt wird, **Then** bleibt es im Fach liegen und wird als falsch gezählt (Falsch-Zähler +1).
2. **Given** ein Fach enthält `RequiredCount` korrekte **und** mindestens ein falsches Objekt, **When** der Zustand ausgewertet wird, **Then** bleibt die Lampe aus und das Fach gilt nicht als abgeschlossen.

---

### User Story 4 - Falsch einsortiertes Objekt entnehmen/korrigieren (Priority: P2)

Der Spieler kann eingelegte Objekte über das Fach wieder herausnehmen (zurück in die Hand), um falsch einsortierte Objekte zu korrigieren. Die Entnahme erfolgt in umgekehrter Ablagereihenfolge (LIFO) über das Fach selbst, nicht über einzeln anklickbare Objekte.

**Why this priority**: Ohne Korrekturweg wäre ein einmaliger Fehler dauerhaft blockierend. Die Entnahme über das Fach (statt 25 einzelne Collider) ist zugleich die Auflösung der `PhysicsInteractionProbe`-Pufferkollision.

**Independent Test**: In ein offenes Fach zwei Objekte legen, dann zweimal entnehmen → die Objekte kehren in umgekehrter Reihenfolge in die Hand zurück; das Fach ist danach leer.

**Acceptance Scenarios**:

1. **Given** ein offenes (nicht abgeschlossenes) Fach enthält mindestens ein Objekt und der Spieler hat freie Tragkapazität, **When** er das Fach anschaut und die Entnahme auslöst, **Then** kehrt das zuletzt eingelegte Objekt (LIFO) in die Hand zurück und der zugehörige Zähler sinkt.
2. **Given** ein leeres Fach, **When** der Spieler die Entnahme auslöst, **Then** passiert nichts.
3. **Given** der Spieler hat keine freie Tragkapazität (Überlast), **When** er zu entnehmen versucht, **Then** wird die Entnahme abgelehnt und das Objekt bleibt im Fach.

---

### Edge Cases

- Einsortieren mit leerer Hand → keine Aktion.
- Interagieren auf ein Fach, das weder offen befüllbar noch entnehmbar ist (abgeschlossen) → keine Aktion.
- Falsches Objekt verhindert Abschluss selbst bei erreichter Soll-Menge korrekter Objekte; erst nach Entnahme des falschen Objekts kann das Fach abschließen.
- Grenze: genau `RequiredCount` korrekte **und** 0 falsche → vollständig (Gleichheit zählt als erreicht).
- Entnehmen aus leerem oder abgeschlossenem Fach → keine Aktion.
- Entnehmen bei voller Tragkapazität → abgelehnt, Objekt bleibt im Fach.
- Rahmen-Beschriftung und Fach-`SortKey` müssen konsistent sein; eine Inkonsistenz ist ein Content-Fehler (Fach würde nie korrekt befüllbar wirken) und wird über eine Editor-Validierung adressiert.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Der Spieler MUSS ein fokussiertes Fach per Interaktion (F2-Interact) mit dem obersten getragenen Objekt befüllen können; das Objekt verlässt den Tragstapel und wird im Fach sichtbar abgelegt.
- **FR-002**: Jedes Fach MUSS einen `SortKey` (akzeptierte Kategorie) und eine konfigurierbare Soll-Menge (`RequiredCount`) besitzen; beide werden im Editor je Fach-Instanz gesetzt (kein Code je Fach).
- **FR-003**: Ein eingelegtes Objekt MUSS gegen den `SortKey` des Fachs klassifiziert werden als korrekt (Wertegleichheit) oder falsch (Abweichung).
- **FR-004**: Ein Fach MUSS als abgeschlossen gelten, sobald es genau `RequiredCount` korrekte UND 0 falsche Objekte enthält (Gleichheit erreicht den Abschluss).
- **FR-005**: Bei Abschluss MUSS die editor-authored Lampe des Fachs aktiviert werden; vorher MUSS sie aus/neutral bleiben. Falsch einsortierte oder fehlende Objekte halten die Lampe aus.
- **FR-006**: Falsch einsortierte Objekte DÜRFEN NICHT hart verhindert oder automatisch zurückgeworfen werden (Abgrenzung zu F12); sie bleiben im Fach und werden als falsch gezählt.
- **FR-007**: Der Spieler MUSS eingelegte Objekte über das Fach in umgekehrter Ablagereihenfolge (LIFO) zurück in die Hand entnehmen können, solange das Fach nicht abgeschlossen ist; die Entnahme respektiert die F3-Tragkapazität (Ablehnung bei Überlast).
- **FR-008**: Eingelegte Objekte DÜRFEN KEINE aktiven Interaktions-Collider tragen, solange sie im Fach liegen (Schutz der `PhysicsInteractionProbe`-Kapazität); ihre Identität/Sortierdaten bleiben für die Entnahme erhalten.
- **FR-009**: Bei Abschluss MUSS das Fach „schließen": eingelegte Objekt-Visuals werden entfernt/versteckt und der Fach-Collider deaktiviert; der autoritative Zählstand bleibt in der Core-Schicht erhalten.
- **FR-010**: Bei Abschluss MUSS ein einmaliges Abschluss-Ereignis bereitgestellt werden (Andockpunkt für XP-Vergabe in F6); F4 vergibt selbst keine XP.
- **FR-011**: Die Sortierlogik (Klassifizierung korrekt/falsch, Vollständigkeit, nächstes Entnahme-Objekt, Abschluss/Sperre) MUSS in der Core-Schicht ohne Unity-Laufzeitabhängigkeit liegen und per EditMode-/Unit-Test prüfbar sein (Prinzip IX); Seiteneffekte (Reparenting, Collider/Visual-Toggle, Lampe, Hand↔Fach-Übergabe) liegen in der Runtime-Schicht.
- **FR-012**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-013**: System definitions/requirements MUST be updated before implementation when conflicts are identified.
- **FR-014**: No UI elements may be generated at runtime by gameplay code; UI/Lampe/Beschriftung MUST be editor-authored (Laufzeitcode bindet/toggelt nur).
- **FR-015**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-016**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-017**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten (Prinzip VII).
- **FR-018**: PlantUML diagrams (State Fach-Lebenszyklus, Activity Einsortieren/Entnahme, Class Sortierdaten) MUST be created and mapped to tests.
- **FR-019**: Diagram-derived test candidates MUST be reflected in the test plan or documented as explicit exceptions.

### Key Entities *(include if feature involves data)*

- **SortKey (Core)**: Datengetriebener Kategorie-Deskriptor als geordnete Menge benannter Facettenwerte (z. B. {Kontinent, Farbe, Symbol}); Wertetyp mit Wertegleichheit; skaliert von wenigen Grey-Box-Kategorien bis zu den vollen Konzeptmengen ohne Code-Änderung.
- **Platzierungs-Eintrag (Core)**: Identität (Id) + `SortKey` eines eingelegten Objekts (reine Daten), in Ablagereihenfolge gehalten.
- **SortTarget / Fach (Core)**: hält akzeptierten `SortKey` + `RequiredCount` + Platzierungen; Entscheidungen: `Classify`, `Place`, `RemoveTop` (LIFO), `Evaluate` → Zustand, Abschluss/Sperre. Reine Logik.
- **SortTargetState**: `Leer`, `Teilweise`, `FalschEnthalten`, `Vollstaendig` (abgeschlossen/gesperrt).
- **Sortable (Runtime)**: Komponente am Objekt, liefert dessen `SortKey` (im Editor authored).
- **SortTargetInteractable (Runtime)**: `IInteractable` am Fach-Root; hält das Core-`SortTarget`, `SlotAnchor`, Lampen-Referenz; führt Hand↔Fach-Übergabe, Reparenting, Collider/Visual-Toggle, Lampe und Abschluss-Schließen aus (Apply).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Der Spieler kann ein passendes Objekt aufnehmen, ein Fach anschauen und einsortieren; das Objekt liegt sichtbar im Fach und wird als korrekt gezählt.
- **SC-002**: Ein Fach mit genau `RequiredCount` korrekten und 0 falschen Objekten schaltet seine Lampe an, schließt sich und entfernt die eingelegten Visuals; der Zählstand bleibt korrekt.
- **SC-003**: Ein falsch einsortiertes Objekt bleibt im Fach, wird als falsch gezählt und hält die Lampe aus; nach Entnahme (LIFO) und korrekter Auffüllung kann das Fach doch noch abschließen.
- **SC-004**: Entnahme über das Fach gibt Objekte in umgekehrter Ablagereihenfolge in die Hand zurück und respektiert die Tragkapazität (Ablehnung bei Überlast).
- **SC-005**: Die Sortier-/Klassifizierungs-/Vollständigkeits-/Entnahme-Logik ist durch EditMode-Tests abgedeckt und läuft ohne Szenenstart grün.
- **SC-006**: 100% documentation coverage check passes at merge.
- **SC-007**: Compile check passes with zero errors after implementation.
- **SC-008**: Die geforderten PlantUML-Diagramme (State + Activity + Class) sind vorhanden, aktuell und mit Tests verknüpft.
- **SC-009**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.

## Assumptions

- **Granularität**: 1 Fach = 1 Zielort = 1 Lampe = 1 `SortKey` + 1 `RequiredCount`. Ein „Kasten" (Poststelle) bzw. „Regal" (Lager) ist ein editor-authored Rahmen-Mesh plus ein Gitter aus Fach-Instanzen (nested Prefabs). Poststelle und Lager teilen sich Prefab- und Code-Muster; Unterschied nur in `SortKey`-Facetten und `RequiredCount`.
- **Sichtbarkeit/Rollen**: Der Rahmen liefert Sichtbarkeit und Beschriftung (Koordinate/Farbe/Symbol). Das Fach bringt nur die klickbare Fläche (Collider in der Öffnung, Pivot mittig) und die Korrektheits-Lampe; ein eigenes Fach-Mesh ist optional.
- **Eingabe**: Einsortieren nutzt die bestehende F2-Interact-Action auf das fokussierte Fach. Entnahme im Grey-Box ebenfalls über das Fach; ob Einsortieren/Entnehmen kontextabhängig dieselbe Taste nutzt (Hand voll → einsortieren, sonst → entnehmen) oder die F3-„Q"-Logik, wird im Plan konkretisiert. Finale Tastenbelegung ist laut Konzept offen und leicht änderbar.
- **Sichtbares Füll-Feedback**: Eingelegte Objekte bleiben sichtbar (als Mesh ohne Interaktions-Collider), damit der Spieler falsche erkennen kann; es werden nicht zwingend alle `RequiredCount` Objekte gerendert – der Core zählt autoritativ, die Anzahl sichtbarer Visuals darf gedeckelt werden.
- **Optimierung bei Abschluss**: Abgeschlossene Fächer „schließen" und entfernen ihre eingelegten Visuals/Collider (Leistung); der Zählstand bleibt im Core. Nur abgeschlossene Fächer werden so optimiert; offene Fächer behalten ihre Einzel-Visuals zur Fehlererkennung.
- **Entnahme-Reihenfolge**: LIFO (oberstes/zuletzt eingelegtes zuerst), visuell wie ein realer Stapel. „Gezielt genau das falsche Objekt greifen" ist spätere Politur, falls die LIFO-Entnahme sich zu zäh anfühlt.
- **Abgrenzung**: Geschenkcontainer-Batch mit hartem Auswurf + 1-min-Cooldown ist **F12**, nicht F4. Sortierblick-Hilfe ist **F9**. XP-Vergabe für korrektes Sortieren ist **F6**; F4 stellt nur das Abschluss-Ereignis als Andockpunkt bereit.
- **Fallback-Prefabs**: Da Meshes fehlen, werden Grey-Box-Platzhalter erstellt (sortierbares Objekt auf `TestPickup`-Basis + `Sortable`; Fach mit Öffnungs-Collider + Slot-Anker + Lampe; optionaler Rahmen). Der Ersteller kann diese Platzhalter selbst bauen; data-model/quickstart legen die verbindlichen Anker-/Lampen-/Pivot-Konventionen fest.
- **Engine/Pakete**: Unity 6000.2.7f2, URP 17.2, Input System 1.14.2, Test Framework 1.6 (wie F1–F3); baut auf `CozySanta.Core`/`CozySanta.Runtime`.
