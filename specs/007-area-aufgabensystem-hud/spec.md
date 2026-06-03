# Feature Specification: F7 – Area- & Aufgabensystem + HUD

**Feature Branch**: `007-area-aufgabensystem-hud`
**Created**: 2026-06-03
**Status**: Draft
**Input**: User description: "F7 – Area- & Aufgabensystem + HUD: Area-Datenmodell, Aufgabentypen, Fortschritt (Zähler/Prozent), Anzeige oben rechts."

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: `CLAUDE.md` (Fortschritt/Status), neue Spec-Artefakte unter `specs/007-area-aufgabensystem-hud/`. Bindet additiv an F4 (`SortTargetInteractable.onCompleted`), F5 (`SnowPatch.Coverage`/`MeltController`) und F6 (`PlayerProgression.AwardXp`) an, ohne deren Verträge zu brechen.
- **New Documentation**: spec, plan, research, data-model, quickstart, tasks; PlantUML unter `diagrams/`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German für Projektkommunikation
- **Merge Coverage Evidence**: Review prüft, dass Area-Datenmodell, Aufgaben-Fortschritt, Area-Abschluss, HUD-Binding und XP-Anbindung in Plan/Tasks beschrieben und mit Tests/Diagrammen verknüpft sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: Class (Area/Task-Datenmodell), State (Area-Zustand Aktiv/Abgeschlossen), Activity (Fortschritts-Update + Abschluss-Flow).
- **Diagram Coverage Evidence**: Diagramme unter `specs/007-area-aufgabensystem-hud/diagrams/`, verknüpft mit Core-Tests (Fortschritts-Update, Abschluss-Erkennung).

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: F4 `SortTargetInteractable.onCompleted` (Sortier-Task-Fortschritt), F5 `SnowPatch.Coverage` + `MeltController` (Schmelz-Task-Fortschritt), F6 `PlayerProgression.AwardXp` (XP bei Area-Abschluss). F2-Interaktionssystem liefert den Spieler-Kontext (aktive Area via Proximity oder Trigger).
- **Known Collisions/Conflicts**:
  - F6 hat bereits `AwardXp`-Andockpunkte für Area-Abschluss vorgesehen (`PlayerProgression.AwardSortXp` etc.). F7 ergänzt `AwardAreaXp` und ruft diesen Pfad auf – kein Widerspruch.
  - HUD ist editor-authored (Constitution V); `AreaHudView` bindet/aktualisiert nur, erzeugt keine Objekte zur Laufzeit.
  - `SortTargetInteractable.AddCompletionListener` ist aus F6 bereits vorhanden.
- **Resolution Before Implementation**: Area-/Task-Datenmodell im Core, Runtime nur Apply. HUD editor-authored. Fortschritts-Events strikt über die bestehenden Andockpunkte. Keine Änderung an F4/F5-Kernverträgen.
- **Open Clarifications**: Keine offenen Klärungspunkte (Annahmen siehe „Assumptions").
- **PlantUML Context Map**: Class (AreaDefinition/AreaTask/AreaProgress), State (Area-Lebenszyklus), Activity (Task-Update + Area-Abschluss-Entscheidung).

## Diagram Requirements *(mandatory)*

- **Activity Diagrams**: `diagrams/area-fortschritt.puml` – Task-Fortschritt aktualisieren → alle Tasks erledigt? → Area abschließen → XP vergeben.
- **Sequence Diagrams**: Not required: Die Quellen-Events kommen aus F4/F5; Activity + Class decken die Regeln ab.
- **Class/Domain Diagrams**: `diagrams/area-datenmodell.puml` – `AreaTask`, `AreaDefinition`, `AreaProgress`; Beziehung Core ↔ Runtime (AreaTracker, AreaHudView).
- **State Diagrams**: `diagrams/area-zustand.puml` – Area: Aktiv → Abgeschlossen (einmaliger Übergang, nicht rückgängig).
- **Mindmap**: Not required: klar abgegrenztes Feature.
- **Diagram Exceptions**: Sequence bewusst ausgelassen (Begründung oben).

## User Scenarios & Testing *(mandatory)*

### User Story 1 – Aufgabenfortschritt wird live aktualisiert (Priority: P1)

Der Spieler sieht oben rechts die Aufgaben seiner aktuellen Area und beobachtet, wie sich Zähler und Prozentwerte in Echtzeit ändern, wenn er Fächer befüllt oder Schnee schmilzt.

**Why this priority**: Ohne sichtbaren Fortschritt hat der Spieler keine Orientierung; das ist die Kernfunktion des Area-Systems.

**Independent Test**: EditMode: AreaProgress mit konfigurierten Tasks erstellen, Fortschritts-Methoden aufrufen und prüfen, dass Zähler/Prozentwerte korrekt berechnet werden.

**Acceptance Scenarios**:

1. **Given** eine Area mit einem Sortier-Task (z. B. „3 Fächer sortieren"), **When** ein Fach abgeschlossen wird, **Then** steigt der Zähler um 1 und der Fortschritt wird korrekt berechnet.
2. **Given** eine Area mit einem Schmelz-Task (z. B. „80 % Schnee schmelzen"), **When** Coverage steigt, **Then** aktualisiert sich der Prozentwert des Tasks entsprechend.
3. **Given** das HUD ist editor-authored und geöffnet, **When** sich ein Task-Wert ändert, **Then** aktualisiert der `AreaHudView` die entsprechenden Texte ohne Laufzeit-UI-Erzeugung.

---

### User Story 2 – Area-Abschluss erkennen und XP vergeben (Priority: P1)

Sobald alle Pflicht-Tasks einer Area erfüllt sind, gilt die Area als abgeschlossen, der Spieler erhält XP und das System meldet den Abschluss (Andockpunkt für Schlüssel/Tor in F8).

**Why this priority**: Der Area-Abschluss ist das zentrale Fortschritts-Event; ohne ihn ist F7 nur eine Anzeige ohne Konsequenz.

**Independent Test**: EditMode: AreaProgress bis zur Abschluss-Bedingung vorantreiben und prüfen, dass `IsComplete` true wird, XP-Event ausgelöst und kein weiterer Fortschritt gebucht wird.

**Acceptance Scenarios**:

1. **Given** alle Tasks einer Area sind erfüllt, **When** der letzte Task-Fortschritt gebucht wird, **Then** ist `AreaProgress.IsComplete` true und das Abschluss-Event wird einmalig ausgelöst.
2. **Given** eine abgeschlossene Area, **When** erneut Fortschritt gebucht wird (z. B. weiteres Sortieren), **Then** ändert sich `IsComplete` nicht und kein weiteres XP-Event wird ausgelöst.
3. **Given** `PlayerProgression` ist verdrahtet, **When** die Area abgeschlossen wird, **Then** erhält das XP-Konto den definierten Area-Abschluss-XP-Betrag.

---

### User Story 3 – HUD zeigt Lampen-Akku an (Priority: P2)

Der Spieler sieht im HUD jederzeit den aktuellen Ladestand des Lampen-Akkus, damit er weiß, wann er schmelzen kann und wann er warten muss.

**Why this priority**: Spielrelevante Information; ohne Akku-Anzeige ist das Schmelzen schwer einzuschätzen. Technisch einfach da `BatteryFraction` bereits in F5 existiert.

**Independent Test**: Im Play-Mode: HUD-Akku-Balken beobachten während F-Taste gehalten wird → Balken sinkt; loslassen → Balken steigt.

**Acceptance Scenarios**:

1. **Given** das HUD ist aktiv, **When** der Spieler die Lampe benutzt, **Then** sinkt der Akku-Balken sichtbar.
2. **Given** der Akku ist leer, **When** der Spieler nicht schmilzt, **Then** steigt der Balken wieder auf vollen Stand.

---

### User Story 4 – Ladestation lädt die Lampe auf (Priority: P2)

Der Spieler läuft zu einer Ladestation in der Welt, schaut sie an und hält Rechtsklick gedrückt. Solange Blickkontakt und Rechtsklick gleichzeitig gehalten werden, lädt der Lampen-Akku innerhalb von 10 Sekunden vollständig auf. Ein Ladebalken zeigt den Fortschritt. Bricht eine der beiden Bedingungen ab (Rechtsklick losgelassen oder Blickkontakt unterbrochen), stoppt der Ladevorgang – der bereits erreichte Ladestand bleibt erhalten.

**Why this priority**: Gibt dem Spieler eine aktive Strategie für den Akku-Management-Loop; die passive Nachladung aus F5 reicht bei großen Schneeflächen nicht aus.

**Independent Test**: Play-Mode: Akku leer machen (langes Schmelzen), zur Ladestation gehen, Rechtsklick 10 s halten → Akku voll; Rechtsklick vorzeitig loslassen → Zwischenstand bleibt.

**Acceptance Scenarios**:

1. **Given** Spieler steht vor der Ladestation und schaut sie an, **When** Rechtsklick gehalten wird, **Then** steigt der Akku-Ladestand kontinuierlich und der Ladebalken im HUD ist sichtbar.
2. **Given** Ladevorgang läuft, **When** Spieler wegschaut oder Rechtsklick loslässt, **Then** stoppt das Laden sofort; Ladestand bleibt auf dem aktuellen Wert.
3. **Given** Ladevorgang läuft, **When** 10 Sekunden gehalten (von 0 %), **Then** ist der Akku vollständig aufgeladen; Ladebalken verschwindet.
4. **Given** Spieler schaut eine Ladestation an, **When** Rechtsklick noch nicht gedrückt, **Then** wird ein Interaktionshinweis angezeigt („Rechtsklick: Lampe aufladen").

---

### User Story 5 – HUD zeigt Level und XP-Fortschritt an (Priority: P2)

Der Spieler sieht im HUD sein aktuelles Level sowie den Fortschrittsbalken zum nächsten Level, damit er den Effekt seiner Aktionen direkt wahrnimmt.

**Why this priority**: Schließt die Rückkopplungsschleife aus F6; ohne Anzeige bleibt XP unsichtbar. Editor-authored, kein neuer Core-Code nötig.

**Independent Test**: Im Play-Mode: Fach abschließen → XP-Balken wächst; Level-up → Level-Zahl erhöht sich.

**Acceptance Scenarios**:

1. **Given** das HUD ist aktiv, **When** XP gutgeschrieben wird, **Then** aktualisiert sich der XP-Balken im HUD sichtbar.
2. **Given** ein Level-up tritt ein, **When** XP-Grenze überschritten wird, **Then** zeigt das HUD das neue Level.

---

### Edge Cases

- Ladestation: Spieler unterbricht mehrfach → Ladestand akkumuliert sich korrekt über mehrere Ladevorgänge.
- Ladestation: Akku bereits voll → Ladebalken erscheint nicht / Vorgang wird sofort beendet.
- Ladestation: Spieler steht sehr nah (LoS-Raycast trifft nicht) → kein Laden.
- Area ohne Tasks → `IsComplete` sofort true (oder Konfigurationsfehler loggen).
- Task mit `required = 0` → sofort erfüllt (Sonderfall: optionaler Task).
- Sehr schneller Fortschritt (mehrere Events im selben Frame) → kein doppelter Area-Abschluss.
- HUD ohne verdrahteten `AreaTracker` → zeigt Platzhalterwerte, kein Absturz.
- Akku-Balken bei `BatteryFraction = 0` → Balken leer, kein negativer Wert.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Das System MUSS eine Area als Datensatz mit Name und einer konfigurierbaren Liste von Tasks modellieren, ohne Unity-Laufzeitabhängigkeiten (Core-Schicht).
- **FR-002**: Jeder Task MUSS einen Typ (Sort/Melt/Custom), einen Soll-Wert und einen aktuellen Wert besitzen; `IsComplete` MUSS abgeleitet sein (`current >= required`).
- **FR-003**: Das System MUSS `AreaProgress` führen, das alle Tasks einer Area bündelt und `IsComplete` der Area als `alle Pflicht-Tasks erledigt` ableitet.
- **FR-004**: `AreaProgress` MUSS eine Methode zum Buchen von Sortier-Fortschritt (`BookSort`) und Schmelz-Fortschritt (`BookMelt`) bereitstellen; Fortschritt auf abgeschlossenen Tasks MUSS ignoriert werden.
- **FR-005**: Bei Area-Abschluss MUSS ein einmaliges Ereignis ausgelöst werden, das `PlayerProgression.AwardXp` aufruft (F6-Andockpunkt).
- **FR-006**: Die Runtime-Komponente `AreaTracker` MUSS Area-Fortschritt aus F4-`onCompleted` und F5-Coverage-Delta ableiten und auf den Core-`AreaProgress` buchen.
- **FR-007**: Das HUD (`AreaHudView`) MUSS editor-authored sein; Laufzeitcode darf UI nur binden/aktualisieren, nicht erzeugen (Constitution V).
- **FR-008**: Das HUD MUSS die aktuelle Area (Name, Tasks mit Fortschritt), den Lampen-Akku und das XP-Level anzeigen.
- **FR-009**: Die Fortschrittslogik (Task-Update, Area-Abschluss) MUSS in der Core-Schicht ohne Unity-Abhängigkeit liegen und per EditMode-Tests prüfbar sein.
- **FR-010**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-011**: System definitions/requirements MUST be updated before implementation when conflicts are identified.
- **FR-012**: Keine UI-Elemente dürfen zur Laufzeit durch Gameplay-Code erzeugt werden; das HUD MUSS editor-authored sein.
- **FR-013**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-014**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-015**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten.
- **FR-016**: PlantUML-Diagramme (Activity Fortschritt/Abschluss + Ladevorgang, Class Area-Datenmodell, State Area-Zustand + Ladezustand) MÜSSEN erstellt und mit Tests verknüpft werden.
- **FR-017**: Der Ladevorgang MUSS sowohl gehaltenen Rechtsklick als auch ununterbrochenen Blickkontakt (Raycast-Treffer auf die Ladestation) erfordern; Unterbrechung einer der beiden Bedingungen stoppt das Laden sofort.
- **FR-018**: Der Ladestand MUSS bei Unterbrechung erhalten bleiben und beim nächsten Ladevorgang fortgesetzt werden können.
- **FR-019**: Ein Ladebalken im HUD MUSS den aktuellen Ladefortschritt (0–100 %) während des Ladevorgangs anzeigen; er MUSS editor-authored sein.
- **FR-020**: Die Ladestation MUSS als Placeholder-Prefab unter `Assets/_Project/Prefabs/LadeStation.prefab` vorliegen und in die `DevSpawnMenu`-Spawn-Liste aufgenommen sein.
- **FR-021**: Die Ladedauer für einen vollständigen Ladevorgang (0 → 100 %) MUSS konfigurierbar sein (Platzhalterwert: 10 Sekunden).

### Key Entities

- **AreaTask (Core)**: `TaskId`, `TaskType` (Sort/Melt/Custom), `Description`, `Required` (Soll), `Current` (Ist), `IsComplete` (abgeleitet). Fortschritt wird über `Book(delta)` gebucht.
- **AreaDefinition (Core)**: Unveränderliche Konfiguration einer Area: `Name`, `Tasks[]`, `AreaXp` (XP bei Abschluss).
- **AreaProgress (Core)**: Laufzeitstatus einer Area: hält `AreaDefinition` + aktuelle Task-Stände; `IsComplete` = alle Tasks erledigt; `BookSort(count)`, `BookMelt(coveragePercent)`; `onCompleted`-Event (einmalig).
- **AreaTracker (Runtime)**: MonoBehaviour; hält `AreaProgress`, abonniert F4-Events + tracked F5-Coverage-Delta in Update; ruft bei Abschluss `PlayerProgression.AwardXp` auf.
- **AreaHudView (Runtime)**: MonoBehaviour; editor-authored Panel; zeigt Area-Name, Task-Liste, Akku-Balken, Ladebalken, XP/Level. Wird von `AreaTracker`, `LadeStation` + `PlayerProgression` aktualisiert.
- **TaskEntryUI (Runtime/Prefab)**: Wiederholte UI-Zeile pro Task (Name + Fortschrittstext); analoges Prefab-Muster wie `SkillEntryUI` (F6).
- **LadeStation (Runtime)**: MonoBehaviour + Placeholder-Prefab; implementiert `IInteractable` für Hinweis-Text; `ChargeTick(float dt)` lädt den Akku wenn Rechtsklick + LoS aktiv; konfigurierbare Ladedauer (10 s Standard). Wird in `DevSpawnMenu` aufgenommen.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Sortier- und Schmelzfortschritt schlägt sich in Echtzeit in den HUD-Task-Anzeigen nieder.
- **SC-002**: Area-Abschluss wird korrekt erkannt; XP wird einmalig und korrekt vergeben.
- **SC-003**: HUD zeigt Akku-Ladestand und XP/Level-Fortschritt ohne Fehler an.
- **SC-004**: EditMode-Tests für Task-Fortschritt (A1–A3), Area-Abschluss (B1–B2) und Ladelogik (C1–C2) laufen grün.
- **SC-004b**: Ladestation-Prefab existiert unter `Assets/_Project/Prefabs/LadeStation.prefab` und ist in DevSpawnMenu eingetragen.
- **SC-005**: 100 % Doku-Coverage-Check besteht beim Merge.
- **SC-006**: Compile-Check: 0 Fehler nach der Implementierung.
- **SC-007**: Die geforderten PlantUML-Diagramme (Activity + Class + State) sind vorhanden, aktuell und mit Tests verknüpft.
- **SC-008**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.

## Assumptions

- **Area-Konfiguration**: Eine Area ist zur Laufzeit fix; kein dynamisches Hinzufügen von Tasks während des Spiels.
- **Aktive Area**: In F7 gibt es genau eine aktive Area (erste Sektor / Poststelle). Multi-Area-Verwaltung folgt mit F8.
- **Task-Typen**: Sort + Melt decken den MVP ab; Custom-Task als Erweiterungspunkt ohne Implementierungsaufwand in F7.
- **Keine Persistenz**: Kein Speichern/Laden in F7 (kommt mit F14); Fortschritt lebt zur Laufzeit.
- **HUD immer sichtbar**: Das Area-HUD ist dauerhaft eingeblendet (kein Ein-/Ausschalten in F7).
- **Akku-Anzeige**: Einfacher Slider/Balken; keine Zahl; visuelle Abschätzung reicht für MVP.
- **XP/Level-Anzeige**: Kleines Label + Balken im HUD; keine doppelte Anzeige neben dem Skill-Menü (F6) nötig.
