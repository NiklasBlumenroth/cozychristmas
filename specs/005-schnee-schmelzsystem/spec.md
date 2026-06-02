# Feature Specification: F5 – Schnee-Schmelzsystem

**Feature Branch**: `005-schnee-schmelzsystem` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: 2026-06-02
**Status**: Draft
**Input**: User description: "F5 Schnee-Schmelzsystem: volumetrische Schneedecke (eigene unterteilte Mesh-Schicht über dem Boden), die per Taschenlampe (Taste F) an der Leuchtstelle abgetragen wird bis der Boden sichtbar ist; Höhenfeld-Maske steuert die Schneedicke, weicher noisy Übergang, schon-geschmolzene Stellen; Dev-Helfer Taste V trägt Schnee wieder auf; Akku + Flächen-Fortschritt in testbarer Core-Schicht; stilisierte Snow-Textur, ein Test-Patch (Chunking/LOD später)"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: `CLAUDE.md` (Fortschritt/Status), neue Spec-Artefakte unter `specs/005-schnee-schmelzsystem/`. Baut additiv auf F1 (Core/Provider, Decide/Apply) und F2 (Kamera/Blick als Leuchtursprung) auf.
- **New Documentation**: spec, plan, research, data-model, quickstart, tasks; PlantUML unter `diagrams/`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German for project communication
- **Merge Coverage Evidence**: Review prüft, dass Schmelzen (Höhenabtrag im Lampenkegel), Akku, Flächen-Fortschritt, Dev-Auftrag (V) und die Core/GPU-Trennung in Plan/Tasks beschrieben und mit Tests/Diagrammen verknüpft sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: Activity (Schmelz-/Auftrag-Entscheidung inkl. Akku), Class (Schnee-/Akku-/Fortschrittsdaten), State (Akku-/Area-Fortschritt). Sequence begründet ausgelassen.
- **Diagram Coverage Evidence**: Diagramme unter `specs/005-schnee-schmelzsystem/diagrams/`, verknüpft mit den Core-Tests (Akku, Höhenabtrag, Coverage).

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: F1-Core-Schichtung + `ITimeProvider` (Schmelzrate/Akku pro Zeit), F2-Kamera/Blick (Leuchtursprung & Richtung für den Raycast), `PlayerInputRelay` (neue Eingaben F/V). Keine Änderung an F3/F4.
- **Known Collisions/Conflicts**:
  - **Constitution IX (testbare Core) vs. GPU-Natur des Schmelzens**: Das visuelle Schmelzen ist inhärent GPU/Shader. → Auflösung: Die **Entscheidungslogik** (Akku-Verbrauch/-Stand, Höhenabtrag pro Zelle, Flächen-Coverage %, XP-Andock) liegt als **reine Core-Schicht** (Zell-Höhenfeld) und ist unit-testbar; die **visuelle Maske** (Höhen-RenderTexture + Shader + Mesh-Displacement) ist die parallele Apply-Spiegelung, gespeist aus denselben Treffer-Events. Shader/RT sind nicht unit-testbar und werden über manuelle/PlayMode-Smoke-Checks abgedeckt (dokumentierte Ausnahme).
  - **Boden/Untergrund (offene Notiz aus `Notizen.md`)**: Schnee wird als **eigene Schicht** über dem Boden behandelt (unabhängig von Mesh/Terrain). Für den Test ist der Boden die vorhandene Szenen-Plane (keine Textur nötig).
  - **Eingabe**: Neue Aktionen „MeltLamp" (F, gedrückt-halten) und „DevAddSnow" (V) ergänzen die Map „Player"; bestehende F2/F3/F4-Eingaben unverändert.
- **Resolution Before Implementation**: Schnee = separate, fein unterteilte Mesh-Schicht über dem Boden; Schneedicke aus einem Höhenfeld; F senkt, V hebt die Höhe am Trefferpunkt. Core-Höhenfeld (Zell-Grid) ist Autorität über Fortschritt/Akku; die GPU-Maske dient nur der Darstellung. Siehe data-model.md.
- **Open Clarifications**: Keine offen (Annahmen siehe „Assumptions").
- **PlantUML Context Map**: Activity (Schmelzen/Auftragen + Akku-Gate), Class (Snow-/Akku-/MeltField-Daten Core↔Runtime), State (Akku leer/geladen, Area-Fortschritt).

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

- **Activity Diagrams**: `diagrams/schmelz-entscheidung.puml` – pro Tick: Leuchtet F? Akku > 0? → Höhe am Trefferpunkt senken, Akku verbrauchen, Coverage aktualisieren; V → Höhe anheben (Dev).
- **Sequence Diagrams**: Not required: Der Leucht→Treffer→Abtrag-Fluss ist über Activity + Class ausreichend abgedeckt; keine neue mehrstufige systemübergreifende Kette.
- **Class/Domain Diagrams**: `diagrams/schnee-daten.puml` – `LampBattery`, `MeltField` (Zell-Höhen, Coverage), Provider-Anbindung, Beziehung Core ↔ Runtime (Shader/RT/Mesh).
- **State Diagrams**: `diagrams/akku-zustand.puml` – Akku: Geladen → Entladen (Schmelzen gesperrt) → Aufgeladen; Area-Fortschritt Leer→Teilweise→Frei.
- **Mindmap**: Not required: klar abgegrenztes Feature.
- **Diagram Exceptions**: Sequence ausgelassen (Begründung oben). Shader/RT-Interna werden nicht diagrammiert (GPU-Detail, kein Fachregel-Mehrwert).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Schnee an der Leuchtstelle wegschmelzen (Priority: P1)

Der Spieler hält die Taschenlampe (F) und leuchtet auf die Schneedecke. Genau dort wird der Schnee abgetragen – es entsteht eine Mulde, die tiefer wird, bis der Boden darunter sichtbar ist.

**Why this priority**: Das gezielte Freilegen im Lampenkegel ist die Kernmechanik von F5 und der zentrale Fortschritts-/Belohnungsmoment des Spiels.

**Independent Test**: In der Test-Szene F halten und auf den Schnee-Patch zielen → an der Leuchtstelle sinkt die Schneehöhe sichtbar, nach genug Zeit ist der Boden frei; daneben bleibt der Schnee unverändert.

**Acceptance Scenarios**:

1. **Given** ein Schnee-Patch mit voller Höhe, **When** der Spieler F hält und auf eine Stelle leuchtet, **Then** sinkt dort die Schneehöhe fortlaufend, bis der Boden sichtbar ist.
2. **Given** der Spieler leuchtet nicht auf den Schnee (kein Treffer), **When** F gehalten wird, **Then** ändert sich nichts an der Schneedecke.
3. **Given** eine bereits freigelegte Stelle, **When** der Spieler weiter darauf leuchtet, **Then** bleibt sie frei (kein „Untergraben" des Bodens).

---

### User Story 2 - Volumetrische Decke mit sinnvollem Übergang (Priority: P1)

Die Schneedecke hat sichtbare Dicke (3D), nicht nur eine flache Fläche. An den Schmelzrändern und zu bereits freigelegten Stellen gibt es einen weichen, leicht unregelmäßigen Übergang statt einer harten Kante.

**Why this priority**: „Visuell befriedigend" ist laut Konzept das halbe Risiko von F5; ohne glaubhafte Dicke und weiche Ränder wirkt das Schmelzen technisch/billig.

**Independent Test**: Eine Mulde schmelzen und den Rand betrachten → die Decke hat Höhe, der Übergang franst weich/noisy aus; schon freigelegte Bereiche fügen sich nahtlos an.

**Acceptance Scenarios**:

1. **Given** der Schnee-Patch, **When** er gerendert wird, **Then** hat die Decke eine sichtbare, einstellbare Höhe über dem Boden.
2. **Given** eine geschmolzene Mulde, **When** der Rand betrachtet wird, **Then** ist der Übergang weich/noisy (keine harte kreisrunde Kante).

---

### User Story 3 - Akku begrenzt das Schmelzen (Priority: P2)

Die Lampe hat einen Akku, der beim Schmelzen sinkt. Ist er leer, schmilzt nichts mehr, bis wieder Energie da ist.

**Why this priority**: Der Akku erzeugt laut Konzept leichte Routenplanung und ist die Andockstelle für spätere Akku-Upgrades (F6); baut auf US1 auf.

**Independent Test**: F halten, bis der Akku leer ist → das Schmelzen stoppt; nach Aufladen (Dev/Trigger) schmilzt es wieder.

**Acceptance Scenarios**:

1. **Given** ein Akku > 0, **When** der Spieler schmilzt, **Then** sinkt der Akku über die Zeit und das Schmelzen funktioniert.
2. **Given** ein leerer Akku, **When** der Spieler F hält, **Then** wird nichts abgetragen.
3. **Given** ein leerer Akku, **When** Energie zugeführt wird (Aufladen), **Then** ist das Schmelzen wieder möglich.

---

### User Story 4 - Flächen-Fortschritt als Core-Wert (Priority: P2)

Das System kann auswerten, wie viel Prozent eines Schnee-Bereichs bereits freigelegt sind. Dieser Wert ist die spätere Grundlage für XP und Aufgabenfortschritt.

**Why this priority**: Ohne messbaren Fortschritt liefert Schmelzen kein XP/Aufgaben-Signal (F6/F7); der Wert muss testbar in der Core-Schicht liegen.

**Independent Test**: In EditMode das Core-Höhenfeld an Zellen abtragen und prüfen, dass die Coverage (% freigelegt) korrekt steigt; volle Freilegung ergibt 100 %.

**Acceptance Scenarios**:

1. **Given** ein Core-Höhenfeld, **When** Zellen auf Höhe 0 abgetragen werden, **Then** steigt die Coverage entsprechend dem Anteil freigelegter Zellen.
2. **Given** alle Zellen freigelegt, **When** die Coverage abgefragt wird, **Then** ist sie 100 % (Abschluss-Signal für die Area).

---

### User Story 5 - Schnee wieder auftragen (Dev-/Test-Helfer) (Priority: P3)

Mit Taste V kann der Spieler/Entwickler Schnee an der Zielstelle wieder auftragen, um das Schmelzen beliebig oft zu testen, ohne neu zu starten.

**Why this priority**: Reine Test-Beschleunigung; keine Konzept-Spielmechanik. Sauber als Dev-Werkzeug gekapselt, leicht entfernbar/umbaubar (spätere „Schneekanone" optional).

**Independent Test**: Eine Stelle freischmelzen, dann V darauf halten → die Schneehöhe steigt dort wieder; anschließend erneut schmelzbar.

**Acceptance Scenarios**:

1. **Given** eine freigelegte Stelle, **When** der Spieler V hält und darauf zielt, **Then** steigt die Schneehöhe dort wieder (bis zur Maximalhöhe).
2. **Given** wieder aufgetragener Schnee, **When** der Spieler F hält, **Then** lässt er sich erneut abtragen.

---

### Edge Cases

- Leuchten ohne Schnee-Treffer (Himmel/Boden/Wand) → keine Höhenänderung, kein Akku-Verbrauch (nur Verbrauch bei tatsächlichem Abtrag, Detail im Plan).
- Akku exakt 0 → Schmelzen gesperrt (Gleichheit zählt als leer).
- Höhe an einer Zelle bereits 0 → bleibt 0 (kein negativer/Unter-Boden-Abtrag).
- V hebt nur bis zur konfigurierten Maximalhöhe (kein „Schnee-Turm").
- Sehr schräger Leuchtwinkel / Rand des Patches → Abtrag nur innerhalb der Schneefläche (UV-Grenzen).
- Performance: ein begrenzter Test-Patch; große Flächen (Chunking/LOD) sind ausdrücklich **nicht** Teil von F5.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Der Spieler MUSS mit gehaltener „MeltLamp"-Eingabe (F) Schnee an der vom Blick getroffenen Stelle fortlaufend abtragen können, bis der darunterliegende Boden sichtbar ist.
- **FR-002**: Die Schneedecke MUSS als eigene Schicht mit sichtbarer, konfigurierbarer Höhe über dem Boden dargestellt werden (volumetrischer Eindruck), gespeist aus einem Höhenfeld.
- **FR-003**: Der Abtrag MUSS lokal auf den Lampenkegel/Trefferbereich begrenzt sein; bereits freigelegte Stellen bleiben frei und werden nicht „untergraben".
- **FR-004**: Schmelzränder und Übergänge zu freigelegten Stellen MÜSSEN weich/unregelmäßig (noisy) wirken, nicht hartkantig.
- **FR-005**: Die Lampe MUSS einen Akku besitzen, der beim Abtragen über die Zeit sinkt; bei leerem Akku (== 0) ist kein Abtrag möglich; Aufladen MUSS das Schmelzen wieder ermöglichen.
- **FR-006**: Das System MUSS den Flächen-Fortschritt eines Schnee-Bereichs als Anteil freigelegter Fläche (Coverage %, 0–100) bereitstellen (Andockpunkt für XP/Aufgaben in F6/F7).
- **FR-007**: Mit der „DevAddSnow"-Eingabe (V) MUSS Schnee an der Zielstelle wieder bis zur Maximalhöhe aufgetragen werden können (Dev-/Test-Helfer, klar als solcher gekapselt).
- **FR-008**: Die Entscheidungslogik (Akku-Stand/-Verbrauch, Höhenabtrag/-auftrag pro Zelle, Coverage-Berechnung) MUSS in der Core-Schicht ohne Unity-Laufzeitabhängigkeit liegen und per EditMode-/Unit-Test prüfbar sein (Prinzip IX); Zeitzugriffe über `ITimeProvider`. Die visuelle Darstellung (Höhen-RenderTexture, Shader, Mesh-Displacement) ist die Apply-Schicht.
- **FR-009**: Die GPU-/Shader-Darstellung DARF die Core-Fortschritts-/Akku-Werte nur spiegeln, nicht ersetzen; Core bleibt Autorität über Fortschritt und Akku.
- **FR-010**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-011**: No UI elements may be generated at runtime by gameplay code; etwaige Akku-Anzeige MUSS editor-authored sein (Laufzeitcode bindet/aktualisiert nur). F5 liefert zunächst keine HUD-Anzeige (folgt mit F7).
- **FR-012**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-013**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-014**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten (Prinzip VII).
- **FR-015**: PlantUML diagrams (Activity Schmelzen/Akku, Class Schnee-/Akku-Daten, State Akku/Fortschritt) MUST be created and mapped to tests.
- **FR-016**: Diagram-derived test candidates MUST be reflected in the test plan or documented as explicit exceptions (Shader/RT als dokumentierte Nicht-Unit-Test-Ausnahme).

### Key Entities *(include if feature involves data)*

- **LampBattery (Core)**: Akku-Stand + Kapazität; Decide: `CanMelt` (Stand > 0), Verbrauch pro Zeit/Abtrag, Aufladen. Parameter für spätere F6-Upgrades.
- **MeltField (Core)**: Zell-Höhenfeld eines Schnee-Bereichs (Höhe 0..1 je Zelle); Decide: Abtrag/Auftrag an einer Zellposition mit Radius/Stärke, `Coverage` (% Zellen auf Höhe 0). Reine, testbare Logik; Autorität über Fortschritt.
- **SnowSurface (Runtime)**: die Schnee-Mesh-Schicht + Material (stilisierte Snow-Textur) + Höhen-RenderTexture; spiegelt das `MeltField` visuell via Shader-Displacement und Clip/Blend; weiche Ränder via Noise.
- **MeltController (Runtime)**: Apply – Raycast Blick→Schnee (Trefferpunkt/UV), treibt pro Tick `LampBattery` + `MeltField` (Decide) und malt parallel in die Höhen-RenderTexture (Apply); Eingaben F (schmelzen) / V (auftragen) über das Input-Relay.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Der Spieler kann mit F gezielt Schnee an der Leuchtstelle abtragen, bis der Boden sichtbar ist; unbeleuchtete Stellen bleiben unverändert.
- **SC-002**: Die Schneedecke wirkt volumetrisch (sichtbare Höhe) und die Schmelz-/Freilege-Übergänge sind weich/noisy statt hartkantig.
- **SC-003**: Bei leerem Akku stoppt das Schmelzen; nach Aufladen funktioniert es wieder.
- **SC-004**: Der Flächen-Fortschritt (Coverage %) ist durch EditMode-Tests abgedeckt: Abtrag erhöht die Coverage korrekt, volle Freilegung = 100 %.
- **SC-005**: Mit V lässt sich Schnee wieder auftragen und erneut abschmelzen (Test-Loop ohne Neustart).
- **SC-006**: Die Akku-/Höhenfeld-/Coverage-Logik (Core) läuft ohne Szenenstart grün (EditMode).
- **SC-007**: 100% documentation coverage check passes at merge.
- **SC-008**: Compile check passes with zero errors after implementation.
- **SC-009**: Die geforderten PlantUML-Diagramme (Activity + Class + State) sind vorhanden, aktuell und mit Tests verknüpft.
- **SC-010**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.

## Assumptions

- **Schnee-Technik**: Schnee = eigene, fein unterteilte **Mesh-Schicht** über dem Boden; die Höhe pro Stelle kommt aus einem **Höhenfeld** (RenderTexture für die Darstellung, Zell-Grid in Core für die Logik). Darstellung über **Vertex-Displacement** des Schnee-Mesh (robuster als Tessellation für den ersten Wurf); Tessellation/feinere Verfahren sind spätere Politur. Das Schnee-Mesh wird per Editor-Tool generiert (Spieler muss nicht subdividen).
- **Boden**: für den Test die vorhandene Szenen-Plane mit ihrem Material; keine Boden-Textur nötig. Schnee-Textur: stilisierte `Holiday_Snow_02` (Base Color; Glitzer/Detail prozedural im Shader).
- **Eingabe**: F = Schmelzen (gedrückt halten, echte Spielmechanik). V = Schnee auftragen (Dev-/Test-Helfer, kein Konzept-Gameplay). Finale Tastenbelegung laut Konzept offen/änderbar.
- **Leuchtursprung**: Blickrichtung der F2-Kamera (Raycast aus der Kamera); ein sichtbares Lampenmodell/Lichtkegel ist optionale Politur, nicht Abtrag-relevant.
- **Akku/Aufladen**: Akku-Verbrauch beim Abtragen; Aufladen im Test über einen einfachen Trigger/Dev-Taste oder Auto-Regeneration (Detail im Plan). Gebäude-Aufladezonen folgen mit dem Sektor-Content.
- **Fortschritt/XP**: F5 liefert nur den Coverage-Wert; die XP-Vergabe ist F6, die HUD-Anzeige F7.
- **Testbarkeit**: Core (`LampBattery`, `MeltField`) ist unit-testbar; Shader/RenderTexture/Mesh-Displacement sind nicht unit-testbar und werden über manuelle/PlayMode-Smoke-Checks abgedeckt (dokumentierte Ausnahme nach Prinzip IX/X).
- **Scope**: **ein** begrenzter Test-Patch. Performance-Skalierung über Chunking/LOD und das gesamte Sektor-Gelände sind ausdrücklich **nicht** Teil von F5.
- **Look-Iteration**: Der visuelle Feinschliff (Höhe, Randweichheit, Slush-Rand, Sparkle, Bloom) wird in 1–2 Editor-Runden mit Test-Feedback justiert, da Shader nicht außerhalb des Editors sichtbar sind.
- **Engine/Pakete**: Unity 6000.2.7f2, URP 17.2, Input System 1.14.2, Test Framework 1.6 (wie F1–F4); baut auf `CozySanta.Core`/`CozySanta.Runtime`.
