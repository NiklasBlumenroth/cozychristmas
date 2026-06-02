# Feature Specification: F6 – XP- & Skillsystem

**Feature Branch**: `006-xp-skillsystem` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: 2026-06-02
**Status**: Draft
**Input**: User description: "F6 XP- und Skillsystem: gemeinsamer XP-Pool aus mehreren Aktionen; Levelaufstieg gibt frei investierbare Skillpunkte ohne festen Skilltree; ~20 kleinteilige Stufen je Skilloption; Skillgruppen Lampe/Tragen/Bewegung + Freischalt-Skills Sortierblick/Objektanziehung; Skillwerte wirken auf bestehende Systeme; testbare Core-Schicht, editor-authored Skillmenü"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: Planned feature (waterfall)
- **Documentation Scope**: `CLAUDE.md` (Fortschritt/Status), neue Spec-Artefakte unter `specs/006-xp-skillsystem/`. Bindet additiv an F3 (`PlayerCarry.Capacity`), F4 (`SortTargetInteractable.onCompleted`), F5 (`MeltController`/`SnowPatch.Coverage`) und F2 (Bewegungsgeschwindigkeit) an, ohne deren Verträge zu brechen.
- **New Documentation**: spec, plan, research, data-model, quickstart, tasks; PlantUML unter `diagrams/`.
- **Documentation Language**: German (mandatory)
- **Communication Language**: German for project communication
- **Merge Coverage Evidence**: Review prüft, dass XP-Sammeln/Level-up, freie Punktevergabe, Skillwert-Wirkung auf bestehende Systeme und die XP-Quellen-Anbindung in Plan/Tasks beschrieben und mit Tests/Diagrammen verknüpft sind.
- **Personal Notes Exclusion**: `Notizen.md` ist von der Doku-Abdeckung ausgenommen.
- **Documentation Split Analysis**: Kein bestehendes Dokument wird übermäßig groß; keine Aufteilung nötig.
- **Diagram Scope**: Activity (Level-up/Investitions-Entscheidung), Class (XP-/Skill-Daten), State (Skill-/Level-Fortschritt). Sequence begründet ausgelassen.
- **Diagram Coverage Evidence**: Diagramme unter `specs/006-xp-skillsystem/diagrams/`, verknüpft mit den Core-Tests (Level-Kurve, Investition, Skillwert).

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: F3 `PlayerCarry.Capacity` (Tragkraft-Upgrade), F5 `MeltController` (Akku-Kapazität, Schmelzradius/-stärke) + `SnowPatch.Coverage` (XP-Quelle Schmelzen), F4 `SortTargetInteractable.onCompleted` (XP-Quelle Sortieren), F2-Bewegung (Speed-Upgrade). Eingabe: neue „SkillMenu"-Aktion (Menü öffnen).
- **Known Collisions/Conflicts**:
  - Die Andockpunkte existieren bereits absichtlich aus F3–F5 (`Capacity`-Properties, `onCompleted`-Event, `Coverage`). F6 liest/setzt diese; es entstehen keine widersprüchlichen Definitionen.
  - **Freischalt-Skills** (Sortierblick = F9, Objektanziehung = F10): F6 führt die **Skill-Optionen** (freischaltbar, danach steigerbar) und einen Freischalt-/Stufen-Status ein; die **Fähigkeiten selbst** werden erst in F9/F10 implementiert. F6 stellt nur den Skill-Status + Andockpunkte bereit.
  - **UI**: Das Skillmenü ist Gameplay-UI → editor-authored (Constitution V). Laufzeitcode bindet/aktualisiert nur. Ein optionales IMGUI-Dev-Tool darf für den Test investieren (kein authored Gameplay-UI).
- **Resolution Before Implementation**: XP-Pool ist gemeinsam (ein Konto); Level-Kurve + Punktevergabe + Skill-Stufen/-Werte liegen testbar im Core; die Runtime wendet die Skillwerte auf die bestehenden Komponenten an (Apply). Siehe data-model.md.
- **Open Clarifications**: Keine offen (Annahmen siehe „Assumptions").
- **PlantUML Context Map**: Activity (XP→Level→Punkte, Investition), Class (XP-/Skill-/Progression-Daten Core↔Runtime), State (Skill-Stufe Gesperrt/Freigeschaltet/Max, Level-Fortschritt).

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

- **Activity Diagrams**: `diagrams/xp-skill-entscheidung.puml` – XP gutschreiben → Level-Schwelle prüfen → Skillpunkte vergeben; Investition: Punkte verfügbar? Skill nicht max? → Stufe erhöhen + Wert neu berechnen.
- **Sequence Diagrams**: Not required: Die XP-Quellen sind Einzel-Events (Sortieren/Schmelzen); Activity + Class decken die Regeln ab; keine neue mehrstufige systemübergreifende Kette.
- **Class/Domain Diagrams**: `diagrams/progression-daten.puml` – `XpLedger`, `Skill`, `SkillSet`, `ProgressionState`, XP-Quellen-Werte; Beziehung Core ↔ Runtime (Apply auf PlayerCarry/MeltController/Bewegung).
- **State Diagrams**: `diagrams/skill-zustand.puml` – Skill-Stufe: (Gesperrt →) Stufe 0..Max; Freischalt-Skills mit Gesperrt→Freigeschaltet.
- **Mindmap**: Not required: klar abgegrenztes Feature.
- **Diagram Exceptions**: Sequence bewusst ausgelassen (Begründung oben).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - XP sammeln & Level-up (Priority: P1)

Der Spieler sammelt XP aus verschiedenen Aktionen auf ein gemeinsames Konto. Bei Erreichen der Level-Schwelle steigt sein Level und er erhält Skillpunkte.

**Why this priority**: XP/Level ist die Grundlage jeder Progression; ohne sie gibt es keine Skillpunkte und keinen spürbaren Fortschritt.

**Independent Test**: In EditMode XP gutschreiben und prüfen, dass das Level an den definierten Schwellen steigt und die korrekte Anzahl Skillpunkte vergeben wird.

**Acceptance Scenarios**:

1. **Given** ein XP-Konto, **When** XP unterhalb der nächsten Schwelle gutgeschrieben wird, **Then** bleibt das Level gleich und es gibt keine neuen Punkte.
2. **Given** XP erreicht/überschreitet die nächste Schwelle, **When** gutgeschrieben wird, **Then** steigt das Level und es werden Skillpunkte vergeben (auch mehrere Level auf einmal bei viel XP).
3. **Given** ein gemeinsames Konto, **When** XP aus unterschiedlichen Aktionen kommt, **Then** zählt alles auf dasselbe Konto.

---

### User Story 2 - Skillpunkte frei investieren (Priority: P1)

Der Spieler investiert Skillpunkte frei in beliebige verfügbare Skilloptionen. Es gibt keinen festen Skilltree; jede Option hat ~20 kleinteilige Stufen.

**Why this priority**: Die freie, kleinteilige Investition ist der Kern des Skilldesigns (kein Tree, häufige kleine Upgrades).

**Independent Test**: Mit verfügbaren Punkten in eine Skilloption investieren → Stufe steigt um 1, ein Punkt wird verbraucht; ohne Punkte oder bei Maximalstufe schlägt die Investition fehl.

**Acceptance Scenarios**:

1. **Given** mindestens ein verfügbarer Punkt und eine Skilloption unter Maximalstufe, **When** investiert wird, **Then** steigt die Stufe um 1 und ein Punkt wird verbraucht.
2. **Given** keine verfügbaren Punkte, **When** investiert wird, **Then** ändert sich nichts.
3. **Given** eine Skilloption auf Maximalstufe (~20), **When** investiert wird, **Then** ändert sich nichts (kein Überschreiten).
4. **Given** ein freischaltbarer Skill (Sortierblick/Objektanziehung) auf Stufe 0/gesperrt, **When** der erste Punkt investiert wird, **Then** gilt er als freigeschaltet und ist danach weiter steigerbar.

---

### User Story 3 - Skillwerte wirken aufs Gameplay (Priority: P1)

Investierte Stufen verändern spürbar die zugehörigen Systemwerte: Tragkraft (F3), Lampen-Akku/-Power/-Kegel (F5) und Bewegungsgeschwindigkeit (F2).

**Why this priority**: Ohne Wirkung sind Skillpunkte bedeutungslos; die spürbare Verbesserung ist die Belohnung.

**Independent Test**: Eine Skilloption hochstufen und prüfen, dass der abgeleitete Wert (z. B. Tragkraft in kg) steigt; die Runtime überträgt diesen Wert auf die zuständige Komponente.

**Acceptance Scenarios**:

1. **Given** die Tragkraft-Option steigt, **When** der Wert berechnet wird, **Then** erhöht sich die Traglast schrittweise bis maximal 25 kg und `PlayerCarry.Capacity` übernimmt ihn.
2. **Given** eine Lampen-Option steigt (Power/Kegel/Akku), **When** der Wert berechnet wird, **Then** übernimmt der `MeltController` den höheren Wert (z. B. Akku-Kapazität, Schmelzstärke/-radius).
3. **Given** die Bewegungs-Option steigt, **When** der Wert berechnet wird, **Then** erhöht sich die Laufgeschwindigkeit.

---

### User Story 4 - XP-Quellen angebunden (Priority: P2)

XP entsteht aus den real existierenden Aktionen: korrektes Einsortieren (F4), Schnee schmelzen/Area-Fortschritt (F5) und weiteren Meilensteinen (Area abgeschlossen, Schlüssel, Christbaumkugel – soweit vorhanden).

**Why this priority**: Die Anbindung macht die Progression im Spiel erlebbar; ohne Quellen bleibt das XP-Konto leer.

**Independent Test**: Ein Fach abschließen (F4-`onCompleted`) → XP-Konto steigt um den definierten Betrag; Schnee schmelzen → XP steigt anhand des Fortschritts.

**Acceptance Scenarios**:

1. **Given** ein Fach wird vollständig korrekt befüllt, **When** `onCompleted` ausgelöst wird, **Then** erhält das Konto die definierte „Sortieren"-XP.
2. **Given** der Spieler legt Schneefläche frei, **When** Fortschritt entsteht, **Then** erhält das Konto „Schmelzen"-XP entsprechend der freigelegten Fläche.

---

### Edge Cases

- Sehr viel XP auf einmal → mehrere Level gleichzeitig, korrekte Summe an Skillpunkten.
- Investition ohne Punkte / auf Maximalstufe → keine Aktion.
- XP-Betrag 0 oder negativ → kein Effekt (nur positive Gutschrift).
- Skillwert an der Grenze (z. B. Tragkraft exakt 25 kg) → Deckelung greift (kein Überschreiten).
- Reihenfolge der Investitionen frei; keine Option blockiert eine andere (kein Tree).
- Freischalt-Skill (Sortierblick/Objektanziehung): in F6 nur Status/Stufe; die Fähigkeit wirkt erst mit F9/F10.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Das System MUSS ein gemeinsames XP-Konto führen, das XP aus mehreren Aktionsarten aufsummiert.
- **FR-002**: Das System MUSS aus dem XP-Stand ein Level über eine definierte Schwellen-/Kurvenfunktion ableiten und bei Level-Aufstieg Skillpunkte vergeben (auch mehrere Level bei großer Gutschrift).
- **FR-003**: Der Spieler MUSS Skillpunkte frei in beliebige verfügbare Skilloptionen investieren können; es gibt KEINEN festen Skilltree mit Abhängigkeiten.
- **FR-004**: Jede Skilloption MUSS eine Maximalstufe (Zielgröße ~20) besitzen; Investitionen über die Maximalstufe hinaus MÜSSEN abgelehnt werden.
- **FR-005**: Eine Investition MUSS abgelehnt werden, wenn keine Skillpunkte verfügbar sind (verfügbare = vergebene − ausgegebene Punkte).
- **FR-006**: Jede Skilloption MUSS aus ihrer Stufe einen abgeleiteten Wert berechnen (kleinteilige Skalierung pro Stufe), inkl. Deckelung (z. B. Tragkraft ≤ 25 kg).
- **FR-007**: Die Runtime MUSS die Skillwerte auf die zuständigen bestehenden Komponenten übertragen: Tragkraft → `PlayerCarry.Capacity`; Lampen-Werte → `MeltController` (Akku-Kapazität, Schmelzstärke/-radius); Bewegung → Laufgeschwindigkeit.
- **FR-008**: Das System MUSS XP-Quellen anbinden: korrektes Einsortieren (F4-`onCompleted`) und Schnee schmelzen/Fortschritt (F5); weitere Meilensteine (Area/Schlüssel/Kugel) MÜSSEN als Andockpunkte vorgesehen sein, soweit die Quellen existieren.
- **FR-009**: Freischalt-Skills (Sortierblick, Objektanziehung) MÜSSEN als investierbare Optionen mit Freischalt-Status modelliert sein; die zugehörigen Fähigkeiten bleiben F9/F10 vorbehalten.
- **FR-010**: Die Progressionslogik (XP→Level→Punkte, Investitionsregeln, Skillwert-Berechnung) MUSS in der Core-Schicht ohne Unity-Laufzeitabhängigkeit liegen und per EditMode-/Unit-Test prüfbar sein (Prinzip IX); Seiteneffekte (Werte auf Komponenten anwenden, Event-Anbindung) liegen in der Runtime.
- **FR-011**: No UI elements may be generated at runtime by gameplay code; das Skillmenü MUSS editor-authored sein (Laufzeitcode bindet/aktualisiert nur). Ein optionales IMGUI-Dev-Tool für Tests ist zulässig.
- **FR-012**: Feature branch MUST include documentation updates for every impacted behavior and interface.
- **FR-013**: System definitions/requirements MUST be updated before implementation when conflicts are identified.
- **FR-014**: Compilation MUST succeed after code changes before merge (Zero-Compile-Error).
- **FR-015**: Documentation and project communication artifacts for this feature MUST be in German.
- **FR-016**: Keine Produktions-/Test-Klassenimplementierungsdatei DARF 300 Zeilen überschreiten (Prinzip VII).
- **FR-017**: PlantUML diagrams (Activity XP/Invest, Class Progression-Daten, State Skill-Stufe) MUST be created and mapped to tests.
- **FR-018**: Diagram-derived test candidates MUST be reflected in the test plan or documented as explicit exceptions.

### Key Entities *(include if feature involves data)*

- **XpLedger (Core)**: gemeinsamer XP-Stand; Level aus Kurve; vergebene Skillpunkte; `Add(xp)`, `Level`, Fortschritt im aktuellen Level, gesamt verdiente Punkte.
- **Skill (Core)**: eine Option mit `Level` (0..MaxLevel), `MaxLevel`, optional Freischalt-Flag; berechnet aus der Stufe einen Wert (`Value`) über eine kleinteilige Skalierung (Basis + Schritt·Stufe, gedeckelt).
- **SkillSet (Core)**: Sammlung aller Skills; `CanInvest(id)`, `Invest(id)`; kennt verfügbare Punkte (über `ProgressionState`).
- **ProgressionState (Core)**: bündelt `XpLedger` + `SkillSet`; verfügbare Punkte = verdient − ausgegeben; Decide-Einstieg für XP-Gutschrift und Investition.
- **Skill-Gruppen/-Optionen**: Lampe (Power, Kegelgröße, Akku), Tragen (Tragkraft ≤ 25 kg), Bewegung (Speed), Sortierblick (Freischaltung, Dauer, Cooldown), Objektanziehung (Freischaltung, Cooldown, Stärke).
- **PlayerProgression (Runtime)**: hält `ProgressionState`, schreibt XP aus F4/F5-Events gut, wendet Skillwerte auf `PlayerCarry`/`MeltController`/Bewegung an (Apply), bindet das editor-authored Skillmenü.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: XP aus mehreren Aktionen sammelt sich auf einem Konto; an den definierten Schwellen steigt das Level und vergibt Skillpunkte (auch mehrere Level bei großer Gutschrift).
- **SC-002**: Skillpunkte lassen sich frei in beliebige Optionen investieren; Investition scheitert bei 0 Punkten oder Maximalstufe; kein Tree-Zwang.
- **SC-003**: Hochgestufte Optionen erhöhen spürbar Tragkraft (≤ 25 kg), Lampenwerte und Bewegungsgeschwindigkeit; die Runtime überträgt die Werte auf die bestehenden Komponenten.
- **SC-004**: XP-Quellen Sortieren (F4) und Schmelzen (F5) speisen das Konto messbar.
- **SC-005**: Die Progressionslogik ist durch EditMode-Tests abgedeckt und läuft ohne Szenenstart grün.
- **SC-006**: 100% documentation coverage check passes at merge.
- **SC-007**: Compile check passes with zero errors after implementation.
- **SC-008**: Die geforderten PlantUML-Diagramme (Activity + Class + State) sind vorhanden, aktuell und mit Tests verknüpft.
- **SC-009**: Keine erstellte Klassen-/Testdatei überschreitet 300 Zeilen.

## Assumptions

- **Gemeinsamer Pool, freie Wahl**: Ein XP-Konto, frei investierbare Punkte, kein fester Skilltree (laut Konzept 03/05/08).
- **~20 Stufen je Option**: Zielgröße; Skalierung kleinteilig (kleine Steigerung pro Stufe). Konkrete Zahlen sind Balancing-Platzhalter und im Editor/Config einstellbar (08: „erste Prototypwerte … später balancen").
- **Level-Kurve**: einfache, monoton steigende Schwellenfunktion (Platzhalterwerte), so dass Level-ups anfangs häufig sind; später balancebar.
- **XP-Quellen**: zunächst Sortieren (F4-`onCompleted`) und Schmelzen (F5-Fortschritt) real angebunden; Area/Schlüssel/Kugel als Andockpunkte (Quellen entstehen in F7/F8/F13).
- **Freischalt-Skills**: Sortierblick (F9) und Objektanziehung (F10) sind in F6 nur Skill-Status/-Stufe; die Fähigkeiten wirken erst mit F9/F10. F6 setzt z. B. ein Unlock-Flag, das diese späteren Features lesen.
- **UI**: Das Skillmenü ist editor-authored (Szene/Prefab), Laufzeitcode bindet nur (Anzeige Level/XP/Punkte, Investitions-Buttons). Für den Test darf ein IMGUI-Dev-Tool (analog `DevSpawnMenu`) Punkte vergeben/investieren.
- **Persistenz**: kein Speichern/Laden in F6 (kommt mit F14); Progression lebt zur Laufzeit.
- **Testbarkeit**: Core (`XpLedger`, `Skill`, `SkillSet`, `ProgressionState`) vollständig unit-testbar; Runtime-Apply (Werte auf Komponenten) per gezielten Tests/manuell.
- **Engine/Pakete**: Unity 6000.2.7f2, URP 17.2, Input System 1.14.2, Test Framework 1.6 (wie F1–F5); baut auf `CozySanta.Core`/`CozySanta.Runtime`.
