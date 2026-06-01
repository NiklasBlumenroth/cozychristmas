# Feature Specification: F2 – First-Person-Controller & Interaktionssystem

**Feature Branch**: `002-first-person-controller` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: 2026-06-01
**Status**: Draft
**Input**: User description: "F2 First-Person-Controller und Interaktionssystem: WASD-Bewegung mit CharacterController, Blicksteuerung (Maus), blick- und reichweitenbasierte Interaktion mit dezentem Hinweis und Ausloesen auf das fokussierte Objekt, aufbauend auf der F1-Core-Zielauswahl (InteractionSelector/IInteractionProbe)"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: `CLAUDE.md` (Abschnitt Status/MVP-Fokus aktualisieren), neue Spec-Artefakte unter `specs/002-first-person-controller/`. Baut auf F1-Architektur auf (keine Änderung an F1-Verträgen).
- **New Documentation**: spec, plan, research, data-model, quickstart, tasks; PlantUML unter `diagrams/`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German for project communication
- **Merge Coverage Evidence**: Review prüft, dass Bewegung, Blick, Interaktionserkennung und -auslösung in Plan/Tasks beschrieben und mit Tests/Diagrammen verknüpft sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: Sequence (Interaktions-Auslösung, systemübergreifend) + Class (neue Komponenten + `IInteractable` im Bezug zu F1-Typen).
- **Diagram Coverage Evidence**: Diagramme unter `specs/002-first-person-controller/diagrams/`, verknüpft mit den Interaktions-Tests und der Core-Mathematik.

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: F1-Schichtung (`CozySanta.Core`/`CozySanta.Runtime`), `InteractionSelector.Decide`, `IInteractionProbe`/`PhysicsInteractionProbe`, `PlayerInteractionController` (Fokusziel). Input System (`InputSystem_Actions`, Map „Player": Move/Look/Interact). URP-Kamera.
- **Known Collisions/Conflicts**: `PlayerInteractionController` aus F1 hält bereits `FocusedTargetId`. F2 erweitert/ergänzt diesen Fokus um eine Auflösung auf ein konkretes interagierbares Objekt und die Auslösung. → Keine widersprüchliche Definition; F2 baut additiv auf der F1-Schnittstelle auf.
- **Resolution Before Implementation**: Auflösung „TargetId → interagierbares Objekt" wird über den Probe definiert (Probe liefert das fokussierbare Objekt mit). Festgelegt vor der Umsetzung (siehe data-model.md).
- **Open Clarifications**: Keine offen (Annahmen siehe „Assumptions").
- **PlantUML Context Map**: Sequence (Interaktions-Auslösung), Class (Komponenten + `IInteractable`).

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

- **Activity Diagrams**: Not required: Bewegung/Blick sind kontinuierlich; die wenige Entscheidungslogik (Pitch-Clamp, Interaktions-Gating) ist klein und wird direkt per Unit-Test abgedeckt – begründete Auslassung.
- **Sequence Diagrams**: `diagrams/interaktion-ausloesen.puml` – systemübergreifender Fluss Input → Controller → Probe/`InteractionSelector` (F1) → `IInteractable.Interact`.
- **Class/Domain Diagrams**: `diagrams/interaktion-komponenten.puml` – neue Komponenten (`FirstPersonController`, `InteractionPromptPresenter`, `IInteractable`) im Bezug zu F1-Typen.
- **State Diagrams**: Not required: kein Lifecycle mit mehr als zwei fachlichen Zuständen (Fokus an/aus ist binär und im Sequence/Code abgedeckt).
- **Mindmap**: Not required: Feature klar abgegrenzt; Übersicht in `Notizen.md`.
- **Diagram Exceptions**: Activity bewusst ausgelassen (Begründung siehe oben).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - In First-Person bewegen und umsehen (Priority: P1)

Der Spieler bewegt sich mit WASD über das Gelände und schaut sich mit der Maus um. Die Kamera ist First-Person; der Blick lässt sich frei drehen, ist aber im Auf-/Ab-Winkel begrenzt. Wände und Hindernisse stoppen die Bewegung.

**Why this priority**: Bewegung und Blick sind die Grundlage jeder weiteren Interaktion. Ohne sie ist nichts spielbar.

**Independent Test**: In einer Grey-Box-Szene mit Boden und Wänden kann man mit WASD laufen, mit der Maus umsehen (Pitch begrenzt) und läuft nicht durch Wände.

**Acceptance Scenarios**:

1. **Given** der Spieler steht in der Szene, **When** er WASD drückt, **Then** bewegt er sich in Blickrichtung relativ zur Kamera mit definierter Geschwindigkeit.
2. **Given** der Spieler bewegt die Maus nach oben/unten, **When** der Pitch die Grenze erreicht, **Then** wird der Blickwinkel begrenzt (kein Überschlagen).
3. **Given** der Spieler läuft gegen eine Wand, **When** er weiter drückt, **Then** gleitet/stoppt er an der Wand, ohne hindurchzugehen.
4. **Given** der Spieler steht am Rand einer Fläche, **When** kein Boden ist, **Then** bleibt er durch Schwerkraft am Boden (kein Schweben).

---

### User Story 2 - Interagierbare Objekte erkennen (Blick + Reichweite) (Priority: P1)

Der Spieler schaut ein Objekt an. Liegt es in Reichweite und im Blickwinkel und ist interagierbar, erscheint ein dezenter Interaktionshinweis. Schaut er weg oder ist es zu weit entfernt, verschwindet der Hinweis.

**Why this priority**: Die Erkennung ist Voraussetzung für jede gezielte Interaktion (Aufnehmen, Sortieren, Lampe laden …) und nutzt die in F1 etablierte Zielauswahl.

**Independent Test**: Mit einem interagierbaren Test-Objekt in der Szene erscheint der Hinweis beim Anschauen innerhalb Reichweite/Winkel und verschwindet beim Wegschauen/zu großer Distanz.

**Acceptance Scenarios**:

1. **Given** ein interagierbares Objekt in Reichweite und Blickwinkel, **When** der Spieler es anschaut, **Then** wird es als Fokusziel gewählt und der Hinweis erscheint.
2. **Given** mehrere interagierbare Objekte im Blick, **When** der Spieler schaut, **Then** wird das nächstgelegene/zentralste gewählt (Regel aus F1 `InteractionSelector`).
3. **Given** ein Objekt außerhalb Reichweite oder Blickwinkel, **When** der Spieler es anschaut, **Then** erscheint kein Hinweis und es ist kein Fokusziel.
4. **Given** ein fokussiertes Objekt, **When** der Spieler wegschaut, **Then** verschwindet der Hinweis.

---

### User Story 3 - Interaktion auslösen (Priority: P2)

Hat der Spieler ein Objekt im Fokus und drückt die Interaktionstaste, wird die Interaktion auf genau dieses Objekt ausgelöst.

**Why this priority**: Schließt den Grundloop „erkennen → handeln" ab und liefert den Andockpunkt für spätere Aktionen (F3 Aufnehmen, F4 Sortieren). Niedriger als US1/US2, da es darauf aufbaut.

**Independent Test**: Bei sichtbarem Hinweis löst der Tastendruck die Reaktion des fokussierten Test-Objekts aus (z. B. beobachtbarer Zustand wechselt); ohne Fokus passiert nichts.

**Acceptance Scenarios**:

1. **Given** ein Objekt ist fokussiert (Hinweis sichtbar), **When** der Spieler die Interaktionstaste drückt, **Then** wird `Interact` genau auf dieses Objekt ausgelöst.
2. **Given** kein Objekt ist fokussiert, **When** der Spieler die Interaktionstaste drückt, **Then** passiert nichts.

---

### Edge Cases

- Kein interagierbares Objekt im Blick → kein Hinweis, Interaktionstaste ohne Wirkung.
- Fokussiertes Objekt wird ungültig/verschwindet im selben Frame → keine Auslösung auf ungültiges Ziel.
- Mehrere überlappende Kandidaten → eindeutige Auswahl über F1-Regel (kleinste Distanz, Tie-Break Winkel).
- Spieler schaut direkt auf eine nicht-interagierbare Wand → kein Hinweis.
- Sehr schnelles Drehen der Maus → Pitch bleibt begrenzt, keine Sprünge über die Grenze.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Der Spieler MUSS sich mit WASD relativ zur Kameraausrichtung über einen `CharacterController` bewegen können; die Bewegungsgeschwindigkeit MUSS als Parameter konfigurierbar sein (spätere Skill-Anbindung in F6).
- **FR-002**: Der Spieler MUSS sich per Maus umsehen können (Yaw am Körper, Pitch an der Kamera); der Pitch MUSS auf einen konfigurierbaren Bereich begrenzt sein (kein Überschlagen).
- **FR-003**: Bewegung MUSS an Collidern stoppen/gleiten (keine Durchdringung) und durch Schwerkraft am Boden bleiben.
- **FR-004**: Eingaben MÜSSEN über das vorhandene Input System (Map „Player": Move, Look, Interact) gelesen werden; nicht benötigte Default-Actions dürfen entfernt/ignoriert werden.
- **FR-005**: Das System MUSS interagierbare Objekte über die F1-Zielauswahl (`InteractionSelector.Decide` mit Kandidaten aus einem `IInteractionProbe`) ermitteln; F2 fügt keine zweite, abweichende Auswahlregel hinzu.
- **FR-006**: Nur Objekte, die als interagierbar markiert sind (`IInteractable`), DÜRFEN als Fokusziel/Hinweis berücksichtigt werden.
- **FR-007**: Bei vorhandenem Fokusziel MUSS ein dezenter Interaktionshinweis angezeigt werden; entfällt der Fokus, MUSS der Hinweis verschwinden.
- **FR-008**: Der Interaktionshinweis (UI) MUSS editor-authored sein. Laufzeitcode DARF das vorab erstellte UI-Element nur ein-/ausblenden und binden, NICHT zur Laufzeit erzeugen.
- **FR-009**: Bei vorhandenem Fokusziel und gedrückter Interaktionstaste MUSS `Interact` genau auf das fokussierte `IInteractable` ausgelöst werden; ohne Fokus DARF keine Auslösung erfolgen.
- **FR-010**: Entscheidungslogik (Pitch-Clamp, Bewegungsvektor-Berechnung, Interaktions-Gating) MUSS in der Core-Schicht ohne Unity-Laufzeitabhängigkeit liegen und per EditMode-/Unit-Test prüfbar sein (Prinzip IX); Seiteneffekte (CharacterController-Move, Kamera-Rotation, UI-Toggle, `Interact`) liegen in der Runtime-Schicht.
- **FR-011**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-012**: System definitions/requirements MUST be updated before implementation when conflicts are identified.
- **FR-013**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-014**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-015**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten (Prinzip VII).
- **FR-016**: PlantUML diagrams (Sequence Interaktions-Auslösung, Class Komponenten) MUST be created and mapped to tests.
- **FR-017**: Diagram-derived test candidates MUST be reflected in the test plan or documented as explicit exceptions.

### Key Entities *(include if feature involves data)*

- **`IInteractable`**: Vertrag eines interagierbaren Weltobjekts; bietet eine `Interact`-Aktion (Seiteneffekt) und ist als Fokuskandidat zugelassen.
- **`FirstPersonController`** (Runtime): liest Eingaben, berechnet Bewegung/Blick über Core-Mathematik und wendet sie auf `CharacterController`/Kamera an (Apply).
- **`InteractionPromptPresenter`** (Runtime): bindet den Fokuszustand an ein vorab im Editor erstelltes Hinweis-UI (nur Ein-/Ausblenden + Binden).
- **Core-Mathematik (Decide)**: `LookMath.ClampPitch`, `MovementCalculator.ComputeDisplacement`, `InteractionTrigger.ShouldInteract` – reine, testbare Funktionen.
- **Fokus-Auflösung**: Erweiterung des Probe-Ergebnisses, sodass das gewählte `TargetId` eindeutig auf ein `IInteractable` zurückgeführt werden kann.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In einer Grey-Box-Szene kann ein Tester ohne Anleitung mit WASD laufen und sich umsehen; der Pitch bleibt innerhalb der Grenze und die Bewegung durchdringt keine Wände.
- **SC-002**: Schaut der Tester ein interagierbares Objekt in Reichweite/Winkel an, erscheint der Hinweis in unter 1 Sekunde; beim Wegschauen verschwindet er.
- **SC-003**: Bei sichtbarem Hinweis löst der Tastendruck zuverlässig die Reaktion genau des fokussierten Objekts aus; ohne Fokus erfolgt keine Reaktion.
- **SC-004**: Die Core-Mathematik (Pitch-Clamp, Bewegungsvektor, Interaktions-Gating) ist durch EditMode-Tests abgedeckt und läuft ohne Szenenstart grün.
- **SC-005**: 100% documentation coverage check passes at merge.
- **SC-006**: Compile check passes with zero errors after implementation.
- **SC-007**: Die geforderten PlantUML-Diagramme (Sequence + Class) sind vorhanden, aktuell und mit Tests verknüpft.
- **SC-008**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.
- **SC-009**: Es wird kein UI-Element zur Laufzeit erzeugt (nur vorab erstellte UI wird ein-/ausgeblendet).

## Assumptions

- **Controller-Typ**: `CharacterController` (cozy, deterministisch, testfreundlich) statt Rigidbody – wie in `Notizen.md` festgehalten.
- **Bewegungsumfang im MVP**: Laufen + Umsehen + Interagieren. Kein Springen/Ducken/Sprint im MVP (cozy, flaches Fabrikgelände); Default-Actions Jump/Crouch/Sprint/Attack werden nicht verdrahtet.
- **Hinweis-UI**: Screen-Space-HUD-Hinweis. Das konkrete Canvas/Text-Element wird im Editor erstellt (editor-authored) und dem `InteractionPromptPresenter` per Referenz zugewiesen; der Code blendet nur ein/aus und setzt ggf. den Text.
- **Interagierbarkeit**: F2 liefert ein einfaches Test-`IInteractable` (beobachtbarer Zustandswechsel) als Nachweis; konkrete Interaktionen (Aufnehmen/Sortieren) folgen in F3/F4.
- **Fokus-Auflösung**: Der `IInteractionProbe` liefert je Kandidat eine Referenz auf das zugehörige `IInteractable`, sodass das gewählte `TargetId` ohne erneute Weltabfrage aufgelöst werden kann.
- **Engine/Pakete**: Unity 6000.2.7f2, URP 17.2, Input System 1.14.2, Test Framework 1.6 (wie F1).
- **Szene**: Eine eigene Grey-Box-Testszene unter `Assets/_Project/` (die vorhandene `SampleScene` bleibt unangetastet oder dient nur als Spielwiese).
