# Feature Specification: F1 – Core-Architektur & Projektgrundgerüst

**Feature Branch**: `001-core-architektur-fundament` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: 2026-06-01
**Status**: Draft
**Input**: User description: "F1 Core-Architektur und Projektgrundgeruest: entkoppelte testbare Core-Schicht, Provider-Interfaces fuer Zeit/Welt/Input, Decide/Apply-Muster, Assembly Definitions, EditMode- und PlayMode-Test-Setup als Fundament fuer alle weiteren Gameplay-Features"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: Neues Feature, keine bestehende Fachdoku betroffen. `CLAUDE.md` (Abschnitt „Build und Werkzeuge") wird um die etablierte Assembly-/Ordnerstruktur ergänzt.
- **New Documentation**: `specs/001-core-architektur-fundament/` (spec, plan, tasks); Architektur-Kurzbeschreibung als Teil von `plan.md`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German for project communication
- **Merge Coverage Evidence**: Review prüft, dass die angelegten Assemblies, Provider-Interfaces und das Decide/Apply-Beispiel in `plan.md`/`tasks.md` beschrieben und in `CLAUDE.md` referenziert sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: Class/Domain-Diagramm der Architekturschichten und ein Activity-Diagramm des Decide/Apply-Beispielflusses (siehe Diagram Requirements).
- **Diagram Coverage Evidence**: Diagramme liegen unter `specs/001-core-architektur-fundament/diagrams/` und werden mit dem Beispiel-Interaktionsflow und dessen Tests verknüpft.

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: Keine bestehende Gameplay-Logik vorhanden. Betroffen sind ausschließlich Projekt-Setup-Artefakte (`Assets/`, Assembly Definitions, Test-Setup). Bestehende URP-/Input-System-/Test-Framework-Pakete werden genutzt, nicht verändert.
- **Known Collisions/Conflicts**: None. Es existiert nur die Unity-Vorlage (`SampleScene`, `InputSystem_Actions`, `New Terrain.asset`); keine eigenen Skripte bisher.
- **Resolution Before Implementation**: Namens-/Ordnerkonvention und Schichtgrenzen sind in diesem Spec und im Plan vor der Umsetzung festgelegt.
- **Open Clarifications**: Keine offen (Annahmen siehe Abschnitt „Assumptions").
- **PlantUML Context Map**: Class-Diagramm (Schichten + Provider), Activity-Diagramm (Decide/Apply-Beispielflow).

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

- **Activity Diagrams**: `diagrams/decide-apply-flow.puml` – Ablauf einer Beispiel-Entscheidung (Decide) und deren Anwendung (Apply) über einen Provider.
- **Sequence Diagrams**: Not required: Der einzelne Beispielflow ist als Activity ausreichend abgebildet; keine echte systemübergreifende Laufzeitinteraktion vorhanden.
- **Class/Domain Diagrams**: `diagrams/architektur-schichten.puml` – Schichten (Core / Runtime / Tests), Provider-Interfaces und deren Abhängigkeitsrichtung.
- **State Diagrams**: Not required: Kein Lifecycle-Modell mit mehr als zwei fachlichen Zuständen in F1.
- **Mindmap**: Not required: Feature ist klar abgegrenzt und klein; Übersicht steht in `Notizen.md` (Roadmap).
- **Diagram Exceptions**: Keine.

## User Scenarios & Testing *(mandatory)*

> Hinweis: F1 ist ein Enabling-Feature. „Nutzer" sind hier die Entwickelnden des Projekts. Der Wert besteht darin, dass alle folgenden Gameplay-Features (F2–F14) auf einer testbaren, entkoppelten Architektur aufsetzen können.

### User Story 1 - Fachlogik ohne Unity-Laufzeit testen (Priority: P1)

Ein Entwickler schreibt Entscheidungslogik (z. B. Auswahl eines Interaktionsziels, später Trag-/Sortierregeln) in der Core-Schicht und kann sie mit schnellen EditMode-/Unit-Tests prüfen, ohne eine Szene zu starten oder Unity-Laufzeit-APIs zu benötigen.

**Why this priority**: Dies ist der Kern von Constitution-Prinzip IX und die Grundvoraussetzung dafür, dass jedes weitere Feature seine Fachregeln testen kann. Ohne dieses Fundament entsteht sofort untestbarer, an Unity gekoppelter Code.

**Independent Test**: Ein Beispiel-Entscheidungstyp in der Core-Schicht wird über einen EditMode-Test mit gemockten Providern geprüft; der Test läuft grün, ohne dass `UnityEngine`-Laufzeit-APIs aufgerufen werden.

**Acceptance Scenarios**:

1. **Given** eine Entscheidungsklasse in der Core-Schicht und ein gemockter Provider, **When** der EditMode-Test ausgeführt wird, **Then** liefert `Decide` das erwartete Ergebnis ohne Szenenstart.
2. **Given** die Core-Assembly, **When** versucht wird, `UnityEngine`-Typen (z. B. `MonoBehaviour`, `Time`) in der Core-Schicht zu verwenden, **Then** schlägt die Kompilierung fehl bzw. ist durch die Assembly-Definition nicht referenzierbar.

---

### User Story 2 - Entscheidung von Seiteneffekt trennen (Decide/Apply) (Priority: P1)

Ein Entwickler trennt für ein Beispielverhalten die reine Entscheidung (`Decide`) von der Anwendung des Effekts (`Apply`). Die Unity-Schicht orchestriert nur: sie holt Eingaben über Provider, ruft `Decide` in der Core-Schicht und wendet das Ergebnis über `Apply` an.

**Why this priority**: Die saubere Trennung ist verbindliche Architekturvorgabe (Prinzip IX) und Muster-Vorlage für alle Folgefeatures. Sie muss am ersten Feature exemplarisch existieren.

**Independent Test**: Ein vertikaler Beispiel-Slice (z. B. „Blick-auf-Objekt-Zielauswahl") wird einmal als reine Core-Entscheidung getestet und einmal als PlayMode-Test end-to-end in einer minimalen Szene geprüft.

**Acceptance Scenarios**:

1. **Given** ein Decide-Ergebnis aus der Core-Schicht, **When** die Runtime-Schicht `Apply` aufruft, **Then** wird genau der beschriebene Seiteneffekt ausgeführt und keine Entscheidungslogik in der Runtime dupliziert.
2. **Given** ein PlayMode-Test mit einer minimalen Szene, **When** der Beispielflow ausgeführt wird, **Then** ist das beobachtbare Ergebnis konsistent mit dem EditMode-Test der gleichen Entscheidung.

---

### User Story 3 - Neues Feature ohne Setup-Reibung starten (Priority: P2)

Ein Entwickler beginnt ein neues Gameplay-Feature und findet eine klare, dokumentierte Ordner- und Assembly-Struktur vor, in die Core-Logik, Runtime-Komponenten und Tests eindeutig einsortiert werden.

**Why this priority**: Reduziert wiederkehrende Setup-Kosten und verhindert Architektur-Drift, ist aber erst nach den P1-Garantien wertvoll.

**Independent Test**: Anhand der dokumentierten Struktur kann ein neuer Beispiel-Namespace/Ordner angelegt werden, der ohne zusätzliche Konfiguration kompiliert und testbar ist.

**Acceptance Scenarios**:

1. **Given** die dokumentierte Struktur in `CLAUDE.md`, **When** ein Entwickler einen neuen Core-Typ und zugehörigen Test anlegt, **Then** werden Assembly-Zugehörigkeit und Testlauf ohne weitere Konfiguration aufgelöst.

---

### Edge Cases

- Was passiert, wenn versehentlich eine Unity-Abhängigkeit in die Core-Schicht gezogen wird? → Muss durch fehlende Assembly-Referenz zu einem Kompilierfehler führen (nicht erst zur Laufzeit auffallen).
- Wie verhält sich das Test-Setup, wenn keine PlayMode-Tests vorhanden sind? → EditMode-Tests müssen unabhängig lauffähig bleiben.
- Was passiert bei zirkulären Abhängigkeiten zwischen Runtime und Core? → Abhängigkeit ist strikt einseitig (Runtime → Core); eine umgekehrte Referenz darf nicht möglich sein.
- Kollidiert die Struktur mit der bestehenden Unity-Vorlage (`SampleScene`, `TutorialInfo`)? → Vorlagen-Assets bleiben unangetastet; das Fundament wird additiv angelegt.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Das Projekt MUSS eine Core-Schicht bereitstellen, in der Fachentscheidungslogik ohne Abhängigkeit auf `MonoBehaviour`, `UnityEngine`, `FindObjectsOfType(...)` oder `Time.time` formuliert wird.
- **FR-002**: Die Core-Schicht MUSS über eine eigene Assembly Definition von der Unity-Runtime-Schicht getrennt sein, sodass Unity-Laufzeittypen in der Core-Schicht nicht referenzierbar sind.
- **FR-003**: Zeit-, Welt- und Eingabezugriffe MÜSSEN in der Core-Schicht über explizite Provider-/Resolver-Schnittstellen erfolgen (z. B. Zeitquelle, Welt-/Raycast-Abfrage, Eingabezustand).
- **FR-004**: Das Fundament MUSS das Decide/Apply-Muster an mindestens einem Beispiel-Slice demonstrieren: reine Entscheidung in der Core-Schicht, Anwendung des Effekts in der Runtime-Schicht.
- **FR-005**: Die Abhängigkeitsrichtung MUSS einseitig sein (Runtime referenziert Core, nicht umgekehrt); zirkuläre Referenzen MÜSSEN ausgeschlossen sein.
- **FR-006**: Das Projekt MUSS ein lauffähiges Test-Setup mit getrennten EditMode- und PlayMode-Test-Assemblies bereitstellen, das auf das Unity Test Framework aufsetzt.
- **FR-007**: Es MUSS mindestens ein EditMode-/Unit-Test existieren, der die Beispiel-Core-Entscheidung mit gemockten Providern prüft, und mindestens ein PlayMode-Test, der den Beispielflow end-to-end prüft.
- **FR-008**: Die Ordner-, Namespace- und Assembly-Konvention MUSS in `CLAUDE.md` dokumentiert sein, sodass Folgefeatures sie ohne Rückfragen anwenden können.
- **FR-009**: Bestehende Unity-Vorlagen-Assets (`SampleScene`, `TutorialInfo`, URP-Settings, `InputSystem_Actions`, `New Terrain.asset`) MÜSSEN unverändert bleiben; das Fundament wird additiv ergänzt.
- **FR-010**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-011**: System definitions/requirements MUST be updated before implementation when conflicts are identified.
- **FR-012**: No UI elements may be generated at runtime by gameplay code; UI MUST be editor-authored. *(In F1 nicht relevant, da keine UI; Regel bleibt verbindlich.)*
- **FR-013**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-014**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-015**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten (Prinzip VII).
- **FR-016**: PlantUML diagrams MUST be created or updated for the architecture layers and the Decide/Apply example flow.
- **FR-017**: Diagram-derived test candidates MUST be reflected in the test plan (Decide/Apply-Beispiel als Testfall) or documented as explicit exceptions.

### Key Entities *(include if feature involves data)*

- **Core-Schicht (Assembly)**: Reine C#-Fachlogik ohne Unity-Laufzeitabhängigkeiten; enthält Entscheidungstypen und Provider-Schnittstellen.
- **Runtime-Schicht (Assembly)**: Unity-`MonoBehaviour`-Komponenten, die Eingaben sammeln, Core-Entscheidungen aufrufen und Effekte anwenden (Provider-Implementierungen).
- **Provider-Schnittstelle**: Abstraktion für Zeit-, Welt- und Eingabezugriffe; in Tests durch Mock/Fake ersetzbar.
- **Entscheidung (Decide)**: Pure Funktion/Methode der Core-Schicht, die aus Eingaben ein Ergebnis ohne Seiteneffekt berechnet.
- **Effektanwendung (Apply)**: Runtime-seitige Anwendung eines Decide-Ergebnisses auf die Welt.
- **Test-Assemblies (EditMode/PlayMode)**: Getrennte Testprojekte, die Core- bzw. End-to-End-Verhalten prüfen.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Die Core-Assembly enthält keine Referenz auf `UnityEngine`/`MonoBehaviour`; ein Versuch, solche Typen zu nutzen, ist nicht kompilierbar (überprüfbar an der Assembly-Definition und einem dokumentierten Negativfall).
- **SC-002**: Mindestens ein EditMode-/Unit-Test für die Beispiel-Core-Entscheidung läuft grün und benötigt keinen Szenenstart.
- **SC-003**: Mindestens ein PlayMode-Test für den Beispielflow läuft grün und liefert ein zum EditMode-Test konsistentes Ergebnis.
- **SC-004**: Ein neues Beispiel-Feature kann allein anhand der in `CLAUDE.md` dokumentierten Struktur angelegt werden, ohne zusätzliche Assembly-/Test-Konfiguration.
- **SC-005**: 100% documentation coverage check passes at merge (Struktur in `CLAUDE.md`, Architektur in `plan.md`).
- **SC-006**: Compile check passes with zero errors after implementation.
- **SC-007**: Die geforderten PlantUML-Diagramme (Schichten + Decide/Apply-Flow) sind vorhanden, aktuell und mit dem Beispiel-Slice und dessen Tests verknüpft.
- **SC-008**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.

## Assumptions

- **Namespace-Wurzel**: `CozySanta` mit Unterschichten `CozySanta.Core`, `CozySanta.Runtime`, Tests `CozySanta.Tests.EditMode` / `CozySanta.Tests.PlayMode`. (Informierte Annahme; leicht änderbar vor der Umsetzung.)
- **Beispiel-Slice**: Als Demonstrations-Slice dient die spätere F2-Interaktions-Zielauswahl in minimaler Form (Auswahl des besten Kandidaten aus einer Liste anhand Distanz/Winkel über einen Welt-/Ray-Provider). F1 implementiert davon nur so viel, wie zur Demonstration des Musters und der Tests nötig ist; die vollständige F2-Funktionalität bleibt F2 vorbehalten.
- **Engine/Pakete**: Unity 6000.2.7f2, URP 17.2, Input System 1.14.2, Test Framework 1.6 sind gesetzt und werden genutzt, nicht ausgetauscht.
- **Keine Laufzeit-UI** in F1; HUD/UI entsteht erst in späteren Features (editor-authored).
- **Persistenz** ist nicht Teil von F1 (kommt in F14); F1 legt nur die testbare Schichtung an.
