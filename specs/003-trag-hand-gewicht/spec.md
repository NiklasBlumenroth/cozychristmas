# Feature Specification: F3 – Trag-, Hand- & Gewichtssystem

**Feature Branch**: `003-trag-hand-gewicht` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: 2026-06-01
**Status**: Draft
**Input**: User description: "F3 Trag-, Hand- und Gewichtssystem: Objekte aufnehmen und tragen, Links/Rechts-Stapellogik (zuletzt aufgenommenes Objekt links, Ablegen in umgekehrter Reihenfolge), Gewichtssystem mit Traglastgrenze; baut auf der F2-Interaktion (IInteractable, Interact) auf"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: `CLAUDE.md` (Fortschritt/Status), neue Spec-Artefakte unter `specs/003-trag-hand-gewicht/`. Baut auf F2-Interaktion auf (kein Bruch der F2-Verträge).
- **New Documentation**: spec, plan, research, data-model, quickstart, tasks; PlantUML unter `diagrams/`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German for project communication
- **Merge Coverage Evidence**: Review prüft, dass Aufnehmen, Stapel-/Hand-Logik, Ablegen und Gewichtsgrenze in Plan/Tasks beschrieben und mit Tests/Diagrammen verknüpft sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: State (Trag-Stapel-Lebenszyklus) + Activity (Aufnehmen-Entscheidung inkl. Gewichtsprüfung). Optional Class für die Tragdaten.
- **Diagram Coverage Evidence**: Diagramme unter `specs/003-trag-hand-gewicht/diagrams/`, verknüpft mit den Stapel-/Gewichtstests.

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: F2-Interaktion (`IInteractable.Interact`, `PlayerInteractionController.TryInteract`), F1-Core-Schichtung. Eingabe (neue „Ablegen"-Action). Hand-/Kamera-Transforms am Player.
- **Known Collisions/Conflicts**: `IInteractable.Interact()` ist bisher generisch (DebugInteractable). F3 führt eine konkrete Aufnehmen-Interaktion ein: ein aufnehmbares Objekt reagiert auf `Interact()`, indem es in den Tragstapel wandert. → Keine widersprüchliche Definition; `IInteractable` bleibt unverändert, eine neue Implementierung wird ergänzt.
- **Resolution Before Implementation**: Verantwortlichkeit „wer hält den Tragstapel" wird vorab festgelegt (eine Trag-Komponente am Player, die die reine Core-Stapellogik kapselt). Siehe data-model.md.
- **Open Clarifications**: Keine offen (Annahmen siehe „Assumptions").
- **PlantUML Context Map**: State (Stapel-Lebenszyklus), Activity (Aufnehmen mit Gewichtsprüfung), Class (Tragdaten).

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

- **Activity Diagrams**: `diagrams/aufnehmen-entscheidung.puml` – Aufnehmen-Regel inkl. Gewichts-/Kapazitätsprüfung und Ablehnung bei Überlast.
- **Sequence Diagrams**: Not required: Der Aufnehmen-/Ablegen-Fluss ist über die F2-Sequence (Interact) plus die Activity ausreichend abgedeckt; keine neue systemübergreifende Kette.
- **Class/Domain Diagrams**: `diagrams/tragdaten.puml` – Tragstapel-Eintrag, Gewichtsdaten, Beziehung Core ↔ Runtime.
- **State Diagrams**: `diagrams/trag-stapel-zustand.puml` – Lebenszyklus eines Objekts: in der Welt → in der Hand (links/Stapel) → wieder in der Welt.
- **Mindmap**: Not required: klar abgegrenztes Feature.
- **Diagram Exceptions**: Sequence bewusst ausgelassen (Begründung siehe oben).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Objekt aufnehmen und tragen (Priority: P1)

Der Spieler schaut ein aufnehmbares Objekt an und nimmt es per Interaktionstaste auf. Das Objekt liegt danach sichtbar in seiner (linken) Hand und wird beim Gehen mitgetragen.

**Why this priority**: Aufnehmen/Tragen ist die Grundvoraussetzung für Sortieren (F4) und alle weiteren Objektaufgaben.

**Independent Test**: In der Grey-Box-Szene ein leichtes Objekt anschauen, Interagieren drücken → das Objekt ist nicht mehr am alten Platz, sondern wird in der Hand getragen.

**Acceptance Scenarios**:

1. **Given** ein aufnehmbares Objekt im Fokus und freie Tragkapazität, **When** der Spieler Interagieren drückt, **Then** wird das Objekt aufgenommen und in der linken Hand angezeigt.
2. **Given** der Spieler trägt ein Objekt, **When** er sich bewegt, **Then** bleibt das Objekt sichtbar mitgeführt.

---

### User Story 2 - Stapeln mit Links/Rechts-Hand-Logik (Priority: P1)

Nimmt der Spieler mehrere Objekte auf, liegt das zuletzt aufgenommene links; vorher gehaltene wandern nach rechts bzw. in den Stapel. Beim Ablegen wird die Reihenfolge rückwärts abgearbeitet (zuletzt aufgenommenes zuerst).

**Why this priority**: Die klare, vorhersehbare Stapelordnung ist der Kern des Trag-Designs und Grundlage für den späteren Sortierblick (linke Hand = relevantes Objekt).

**Independent Test**: Drei Objekte nacheinander aufnehmen und prüfen, dass das zuletzt aufgenommene links liegt; dann ablegen und prüfen, dass sie in umgekehrter Reihenfolge zurückkommen.

**Acceptance Scenarios**:

1. **Given** der Spieler nimmt A, dann B, dann C auf, **When** alle aufgenommen sind, **Then** liegt C links (oben), darunter B, darunter A.
2. **Given** der Spieler trägt C/B/A, **When** er Ablegen drückt, **Then** wird zuerst C abgelegt, dann B, dann A (LIFO).
3. **Given** ein leerer Tragstapel, **When** der Spieler Ablegen drückt, **Then** passiert nichts.

---

### User Story 3 - Gewichtsgrenze (Priority: P2)

Jedes Objekt hat ein Gewicht; der Spieler hat eine maximale Traglast. Ein Objekt kann nur aufgenommen werden, wenn die Summe der getragenen Gewichte plus das neue Objekt die Traglast nicht überschreitet.

**Why this priority**: Das Gewichtssystem gibt dem Tragen Bedeutung und ist die Andockstelle für spätere Tragkraft-Upgrades (F6); baut auf US1/US2 auf.

**Independent Test**: Bei kleiner Traglast ein schweres Objekt anschauen und Interagieren → Aufnahme wird abgelehnt (kein Stapelzuwachs); mit ausreichender Traglast → Aufnahme klappt.

**Acceptance Scenarios**:

1. **Given** die getragene Last + Objektgewicht ≤ Traglast, **When** der Spieler aufnimmt, **Then** wird das Objekt aufgenommen.
2. **Given** die getragene Last + Objektgewicht > Traglast, **When** der Spieler aufnimmt, **Then** wird die Aufnahme abgelehnt und das Objekt bleibt in der Welt.

---

### Edge Cases

- Ablegen mit leerem Stapel → keine Aktion.
- Aufnehmen ohne Fokus auf ein aufnehmbares Objekt → keine Aktion.
- Objekt exakt an der Traglastgrenze (Summe == Traglast) → erlaubt (≤).
- Ablegen platziert das Objekt wieder als normales Weltobjekt (mit Collider/Physik), nicht „im Boden".
- Sehr viele/sehr leichte Objekte (z. B. Briefe) → Stapel kann wachsen, solange Gewicht passt (eine optionale Höchstanzahl darf als Sicherheitsnetz dienen).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Der Spieler MUSS ein fokussiertes, aufnehmbares Objekt per Interaktion aufnehmen können (Anbindung an die F2-Interaktion).
- **FR-002**: Aufgenommene Objekte MÜSSEN einem Tragstapel mit LIFO-Ordnung folgen: zuletzt aufgenommen = „links/oben"; Ablegen entnimmt zuerst das zuletzt aufgenommene.
- **FR-003**: Der Spieler MUSS getragene Objekte per „Ablegen"-Eingabe wieder in der Welt platzieren können; das abgelegte Objekt wird wieder ein normales, interagierbares Weltobjekt.
- **FR-004**: Jedes aufnehmbare Objekt MUSS ein Gewicht besitzen; der Spieler hat eine konfigurierbare maximale Traglast (spätere Skill-Anbindung in F6).
- **FR-005**: Eine Aufnahme MUSS abgelehnt werden, wenn die getragene Gesamtlast plus das neue Objektgewicht die Traglast überschreitet; an der Grenze (==) ist die Aufnahme erlaubt.
- **FR-006**: Getragene Objekte MÜSSEN sichtbar mitgeführt werden (an Hand-/Trage-Transforms des Spielers); ihre Welt-Physik/Kollision ruht, solange sie getragen werden.
- **FR-007**: Die Stapel-, Reihenfolge- und Gewichtslogik (Entscheidungen: aufnehmbar?, nächstes Ablege-Objekt) MUSS in der Core-Schicht ohne Unity-Laufzeitabhängigkeit liegen und per EditMode-/Unit-Test prüfbar sein (Prinzip IX); Seiteneffekte (Reparenting, Physik-Toggle, Weltplatzierung) liegen in der Runtime-Schicht.
- **FR-008**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-009**: System definitions/requirements MUST be updated before implementation when conflicts are identified.
- **FR-010**: No UI elements may be generated at runtime by gameplay code; UI MUST be editor-authored. *(F3 nutzt 3D-Hand-Transforms, keine Laufzeit-UI-Erzeugung.)*
- **FR-011**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-012**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-013**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten (Prinzip VII).
- **FR-014**: PlantUML diagrams (State Stapel-Lebenszyklus, Activity Aufnehmen, Class Tragdaten) MUST be created and mapped to tests.
- **FR-015**: Diagram-derived test candidates MUST be reflected in the test plan or documented as explicit exceptions.

### Key Entities *(include if feature involves data)*

- **Tragstapel (Core)**: LIFO-Sammlung getragener Einträge mit Gesamtgewicht, Kapazitätsprüfung (`CanPickUp`), `Push`, `Pop`/`Peek`, `Count`, `TotalWeight`.
- **Trageintrag**: Identität + Gewicht des getragenen Objekts (reine Daten).
- **Aufnehmbares Objekt (Runtime)**: `IInteractable`-Implementierung mit Gewicht; `Interact()` versucht die Aufnahme in den Tragstapel des Spielers.
- **Trage-Komponente (Runtime)**: hält den Core-Tragstapel, Hand-Transforms (links/rechts/Stapel), führt Reparenting + Physik-Toggle + Weltplatzierung aus (Apply).
- **Traglast**: maximale Gewichtskapazität des Spielers (Parameter, Skill-Anbindung später).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Der Spieler kann ein aufnehmbares Objekt anschauen, aufnehmen und sieht es in der Hand; ein zweites und drittes Objekt stapeln sich mit dem zuletzt aufgenommenen „links/oben".
- **SC-002**: Ablegen gibt die Objekte in umgekehrter Aufnahmereihenfolge zurück; jedes abgelegte Objekt ist wieder in der Welt interagierbar.
- **SC-003**: Bei kleiner Traglast wird ein zu schweres Objekt nicht aufgenommen; bei ausreichender Traglast schon (Grenze inklusive).
- **SC-004**: Die Trag-/Stapel-/Gewichtslogik ist durch EditMode-Tests abgedeckt und läuft ohne Szenenstart grün.
- **SC-005**: 100% documentation coverage check passes at merge.
- **SC-006**: Compile check passes with zero errors after implementation.
- **SC-007**: Die geforderten PlantUML-Diagramme (State + Activity + Class) sind vorhanden, aktuell und mit Tests verknüpft.
- **SC-008**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.

## Assumptions

- **Aufnehmen-Eingabe**: nutzt die bestehende F2-Interact-Action (Anschauen + Tippen). Aufnehmbare Objekte sind `IInteractable`.
- **Ablegen-Eingabe**: eine **neue „Drop"-Action** wird der Map „Player" hinzugefügt (Default-Vorschlag `Q` / Gamepad-Ost); finale Tastenbelegung ist laut Konzept offen und leicht änderbar.
- **Tragdarstellung (Links/Rechts-Split, konzeptgetreu)**: Das aufgenommene Objekt wird reparentet, Collider/Rigidbody ruhen.
  - **Linker Anker = aktuelles Objekt**, ist **Kind der Kamera** → bleibt immer an fester Bildschirmposition (immer sichtbar).
  - **Rechter Anker = Stapel der übrigen**, ist **Kind des Spielerkörpers** (nicht der Kamera) → hat eine **feste Höhe**. Das **unterste (älteste)** Objekt sitzt am Anker, weitere stapeln sich **nach oben** (Index-Offset). Weil der Anker dem Blick nicht folgt, kann der Spieler **nach oben schauen, um die oberen Stapelobjekte anzusehen** (wie ein real gehaltener Stapel).
  - Nach jedem Aufnehmen/Ablegen wird neu angeordnet: zuletzt aufgenommenes → links; das vorher linke wandert oben auf den rechten Stapel; beim Ablegen umgekehrt. Beim Ablegen wird das linke Objekt vor dem Spieler platziert.
- **Kapazität**: Begrenzung primär über Gewichtssumme ≤ Traglast; eine optionale Höchstanzahl darf als Performance-/Sicherheitsnetz dienen. Startwert klein (z. B. nur sehr leichte Objekte), Upgrades bis 25 kg folgen in F6.
- **Hand-Modelle/Animation**: nicht Teil von F3 — die Objekte werden an den beiden Anker-Transforms (links/rechts) angezeigt; aufwändige Hand-Animation ist spätere Politur.
- **Engine/Pakete**: Unity 6000.2.7f2, URP 17.2, Input System 1.14.2, Test Framework 1.6 (wie F1/F2); baut auf `CozySanta.Core`/`CozySanta.Runtime`.
