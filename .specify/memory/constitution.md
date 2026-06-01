<!--
Sync Impact Report
- Version change: 1.5.0 -> 1.5.1
- Change type: PATCH (Projektidentitaet angepasst, keine Prinzipienaenderung)
- Modified principles:
  - None (Prinzipien I-X inhaltlich unveraendert; nur Projektbezug umbenannt)
- Renamed:
  - Projekt "FarmingAutomation" -> "Cozy Santa Factory" (Unity-Aufraeumspiel,
    siehe GameKonzept/docs/game-concept/)
- Added sections:
  - None
- Removed sections:
  - None
- Templates requiring updates:
  - ⚪ not required: .specify/templates/* (generisch, kein Projektname enthalten)
  - ⚪ not required: .codex/prompts/* (generische Spec-Kit-Prompts)
- Follow-up TODOs:
  - Bei Bedarf .github/pull_request_template.md und scripts/qa/ aus dem
    Vorgaengerprojekt fuer Cozy Santa Factory neu anlegen (existieren hier
    aktuell nicht).
-->

# Cozy Santa Factory Constitution

## Core Principles

### I. Documentation as a Mandatory Deliverable
Every implementation change MUST be reflected in project documentation within the same
branch before merge. Each change MUST follow one explicit path before coding starts:
(1) a planned feature via Spec Kit waterfall artifacts (`spec.md`, `plan.md`, `tasks.md`
and related docs), or (2) a spontaneous extension without a new feature, where the
existing documentation is directly extended. Documentation MUST include behavior,
constraints, dependencies, and integration impact.
Personal management files that are not project artifacts (for example `Notizen`) are
explicitly excluded from this documentation obligation.
Rationale: Undocumented changes create regressions in team understanding and block safe
future extensions.

### II. Documentation Merge Gate and Coverage Verification
A feature branch MUST NOT be merged until documentation coverage is verified. The merge
review MUST include a documentation coverage check that maps all changed code areas to
updated docs. Extended documentation MUST be assessed for structural split potential; if
a document has become too broad, it MUST be split in the same branch or an approved,
tracked follow-up MUST be created before merge.
Rationale: Documentation quality must scale with feature growth and remain maintainable.

### III. Whole-System Assessment Before Extension
Before implementing any feature that builds on or docks into existing systems,
developers MUST create a whole-system view of impacted modules, interfaces, data flow,
and existing definitions. If collisions, ambiguity, or requirement gaps are found,
definitions and requirements MUST be clarified and documented before implementation
continues.
Rationale: Early conflict resolution prevents incompatible design decisions and costly
rework.

### IV. Zero-Compile-Error Rule for New Code
Whenever production code is added or changed, a compilation check appropriate to the
change scope MUST be executed, and all compile errors MUST be fixed in the same branch.
No change may be merged while compile errors remain.
Rationale: Continuous build integrity prevents unstable integration and hidden breakage.

### V. Editor-Authored UI Only
UI elements MUST NOT be generated at runtime by gameplay code. UI layout and elements
MUST be authored in Unity scenes/prefabs via the Editor. Runtime code MAY only bind,
update, or toggle pre-authored UI elements. Editor scripts that generate assets in edit
mode are allowed.
Rationale: Editor-authored UI ensures consistent structure, predictable behavior, and
maintainable workflows.

### VI. German as Mandatory Documentation and Communication Language
Project documentation and project communication MUST be in German. This includes feature
specifications, plans, task lists, architecture notes, PR descriptions, review comments,
and agent communication in this repository. Technical identifiers, API names, and direct
external quotes MAY remain in their original language.
Rationale: A single working language prevents ambiguity and improves review speed.

### VII. Klassenkomplexität und Größenlimit
Produktionsklassen MUESSEN so strukturiert werden, dass keine einzelne
Klassenimplementierungsdatei mehr als 300 Zeilen umfasst. Wenn eine Klasse mittels
`partial` aufgeteilt wird, gilt das 300-Zeilen-Limit fuer jede Partial-Datei, und die
Aufteilung MUSS entlang fachlicher Verantwortungen erfolgen (nicht nur kosmetisch nach
Zeilenbloecken). Ausnahmen MUESSEN explizit dokumentiert, begruendet und mit Owner sowie
Faelligkeitsdatum fuer den Refactor versehen werden.
Rationale: Kleine, klar getrennte Klassenbereiche reduzieren Debugging-Kosten und
verhindern regressionsanfaellige Aenderungen in grossen Monolithen.

### VIII. Definition of Done (DoD) Baseline
Die folgenden Kriterien sind verbindlicher Bestandteil der Definition of Done fuer jedes
Feature und jede spontane Erweiterung mit Codeaenderungen; sie duerfen nicht als optionaler
"Polish"-Schritt behandelt werden:
1. Compile-Check fuer den geaenderten Scope ist erfolgreich und ohne neue Fehler.
2. Das 300-Zeilen-Limit aus Prinzip VII ist fuer alle neu erstellten oder geaenderten
   Produktionsklassen eingehalten (oder eine genehmigte, dokumentierte Ausnahme liegt vor).
3. Geforderte Dokumentationsaktualisierung ist im selben Branch enthalten.
4. Fuer jede neue oder geaenderte Fachregel in der Kernlogik existiert mindestens ein
   automatisierter Unit-Test.
5. Jeder reproduzierbare Bugfix enthaelt mindestens einen Regressionstest.
6. Testnachweise (ausgefuehrte Tests + Ergebnis) sind im Feature-Artefakt dokumentiert.
Rationale: Ein einheitlicher DoD-Standard verhindert Qualitaetsdrift zwischen Features.

### IX. Testbare Architektur als Standard
Neue Features MUESSEN so entworfen werden, dass Kernentscheidungslogik ohne Unity-Laufzeit
automatisiert testbar ist. Fachlogik ist in einer entkoppelten Core-Schicht zu halten; die
Unity-Schicht darf diese Logik nur orchestrieren und Seiteneffekte anwenden.
Verbindliche Mindestregeln:
1. Kernlogik enthaelt keine direkten Abhaengigkeiten auf `MonoBehaviour`, `UnityEngine`,
   `FindObjectsOfType(...)` oder `Time.time`.
2. Zeit-, Welt- und Infrastrukturzugriffe erfolgen ueber explizite Provider/Resolver-
   Schnittstellen.
3. Entscheidungslogik (`Decide`) und Seiteneffekte (`Apply`) sind getrennt modelliert.
4. PlayMode-Tests werden fokussiert fuer kritische End-to-End-Flows eingesetzt; der
   primaere Testfokus fuer Regeln liegt auf schnellen EditMode-/Unit-Tests.
Rationale: Testbarkeit muss Architekturvorgabe sein, nicht nachtraegliche Refactor-Aufgabe.

### X. PlantUML-Diagramme fuer fachliche Aenderungen
Features und spontane Erweiterungen MUESSEN PlantUML-Diagramme erstellen oder aktualisieren,
wenn sie Fachlogik, Zustandsuebergaenge, persistierte Daten/Snapshots oder
systemuebergreifende Runtime-Flows veraendern. Die Diagrammauswahl MUSS pragmatisch sein:
Activity fuer Entscheidungsregeln, Sequence fuer systemuebergreifende Interaktionen,
Class/Domain fuer Entitaeten und Snapshots, State fuer Lifecycle-Modelle mit mehr als zwei
fachlichen Zustaenden und Mindmap fuer Ueberblick bei groesseren Features oder Refactors.
Triviale Text-, Copy- oder reine UI-Bindungsanpassungen duerfen Diagramme auslassen, wenn
die Ausnahme im Feature-/PR-Kontext kurz begruendet ist.
Diagramme MUESSEN unter `specs/[###-feature]/diagrams/` liegen, klein genug fuer Review
bleiben und mit Tests verknuepft werden: Activity-Branches, State-Transitions,
Sequence-Seiteneffekte und Domain-Invarianten sind als Testkandidaten zu pruefen.
Rationale: Diagramme erhoehen Verstaendlichkeit und Stabilitaet nur, wenn sie fachliche
Regeln verdichten und als Review-/Testgrundlage dienen, nicht wenn sie Code mechanisch
duplizieren.

## Operational Constraints

- Every change MUST include documentation updates in the same branch/changeset.
- Documentation split analysis is mandatory when docs expand significantly or combine
  multiple domains.
- Planned and spontaneous changes follow the same merge gate: no merge without verified
  documentation coverage.
- Runtime UI generation in gameplay scripts is forbidden.
- Compile checks are mandatory for all code-bearing changes.
- Documentation and project communication are mandatory in German.
- Klassenimplementierungsdateien in Produktionscode sind auf 300 Zeilen begrenzt
  (Partial-Regelung siehe Prinzip VII).
- Compile-Check und 300-Zeilen-Regel sind verpflichtende DoD-Kriterien je Story/Increment
  und fuer den finalen Merge-Stand.
- Neue/geaenderte Fachregeln benoetigen Unit-Tests; Bugfixes benoetigen Regressionstests.
- Kernlogik darf keine direkten Unity- und Global-Lookup-Abhaengigkeiten enthalten;
  Zugriff erfolgt ueber Adapter/Provider.
- PlantUML-Diagramme sind fuer geaenderte Fachlogik, Zustaende, persistierte
  Daten/Snapshots und systemuebergreifende Runtime-Flows verpflichtend oder mit
  dokumentierter Ausnahme auszulassen.
- Diagramme muessen mit Testkandidaten abgeglichen werden; fehlende Tests oder bewusst
  ausgelassene Testfaelle sind zu dokumentieren.
- Personal management notes (for example `Notizen`) are excluded from mandatory
  documentation coverage.

## Development Workflow

1. Capture whole-system context before implementing dependent functionality.
2. Classify the change path: planned feature (waterfall via Spec Kit) or spontaneous
   extension.
3. Update requirements/definitions first when collisions or ambiguities are found.
4. Implement incrementally while updating docs continuously in parallel and in German.
5. Split oversized classes before or during implementation so the 300-line limit remains
   enforced (or document a tracked exception).
6. Run compile checks whenever code is added/changed and resolve all errors immediately.
7. Implement logic in testbarer Schichtung (Core-Entscheidung + Adapter/Apply).
8. Create/update PlantUML diagrams for changed Fachlogik, states, snapshots/data, or
   cross-system flows and derive/check test candidates from them.
9. Verify DoD for each implemented increment (including class-size, diagram, and test compliance).
10. Execute merge compliance review: documentation coverage, split analysis outcome,
   collision resolution evidence, compile-clean state, class-size compliance, test
   compliance, diagram coverage, and
   UI-policy compliance.

## Governance

This constitution is authoritative over project process for implementation and
documentation quality.

Amendment procedure:
1. Propose changes through a documented pull request including rationale and affected
   principle/section mapping.
2. Update dependent templates and guidance documents in the same change.
3. Record a Sync Impact Report at the top of this constitution.

Versioning policy (Semantic Versioning):
- MAJOR: incompatible governance changes or principle removals/redefinitions.
- MINOR: new principle/section or materially expanded guidance.
- PATCH: clarifications, wording improvements, and non-semantic refinements.

Compliance review expectations:
- Every feature merge MUST verify constitution compliance.
- Reviewers MUST reject merges lacking documentation coverage verification.
- Reviewers MUST reject merges with non-German project documentation or communication.
- Reviewers MUST reject merges introducing production class files over 300 lines without a
  documented, approved exception.
- Reviewers MUST reject work where per-story/increment DoD checks (compile + class-size)
  are missing or skipped.
- Reviewers MUST reject merges where neue/geaenderte Fachregeln ohne Unit-Tests
  eingefuehrt werden.
- Reviewers MUST reject bugfix merges without a regression test unless a documented,
  approved Ausnahme mit Owner und Faelligkeitsdatum vorliegt.
- Reviewers MUST reject architecture changes that keep core decision logic directly
  coupled to Unity runtime APIs without documented, approved exception.
- Reviewers MUST reject merges that change Fachlogik, Zustandsuebergaenge, persistierte
  Daten/Snapshots oder systemuebergreifende Runtime-Flows without updated PlantUML
  diagrams or a documented, approved exception.
- Reviewers MUST reject diagram changes that contain stale placeholders or are not mapped
  to changed code/test areas when they are part of the merge evidence.
- Reviewers MUST ignore personal management note files during documentation coverage
  verification.
- Exceptions MUST be explicitly approved and tracked with owner and due date.

**Version**: 1.5.1 | **Ratified**: 2026-02-13 | **Last Amended**: 2026-06-01
