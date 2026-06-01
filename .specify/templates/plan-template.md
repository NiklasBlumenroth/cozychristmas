# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

**Change Classification**: [Planned feature via waterfall artifacts / Spontaneous extension]
**Documentation Impact**: [List docs updated in this branch]
**Diagram Impact**: [PlantUML diagrams to create/update or "None" with reason]
**Working Language**: [German (mandatory for docs and project communication)]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION]
**Primary Dependencies**: [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]
**Testing**: [e.g., pytest, XCTest, cargo test or NEEDS CLARIFICATION]
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]
**Project Type**: [single/web/mobile - determines source structure]
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]
**Integration Touchpoints**: [existing systems/modules the change builds on]
**PlantUML Documentation**: [Activity/Sequence/Class-State/Mindmap files planned under `specs/[###-feature]/diagrams/`]

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design and before implementation.*

- [ ] Change path classified (planned waterfall feature vs. spontaneous extension)
- [ ] Documentation coverage defined for all impacted project areas (excluding personal management notes, e.g. `Notizen`)
- [ ] Whole-system impact and collision analysis documented
- [ ] PlantUML diagram selection completed (Activity/Sequence/Class-State/Mindmap or documented exception)
- [ ] Definitions/requirements updated where collisions exist
- [ ] Compile-validation command(s) defined for this scope
- [ ] Architekturtrennung geplant: Core-Entscheidungslogik testbar ohne direkte Unity-Abhaengigkeiten
- [ ] Decide/Apply-Trennung fuer kritische Flows dokumentiert
- [ ] Teststrategie definiert (EditMode/Unit als Standard, PlayMode gezielt fuer E2E)
- [ ] Diagram-derived test candidates documented (Activity branches, State transitions, Sequence side effects, Domain invariants)
- [ ] DoD baseline covers per-story/increment compile-checks, 300-line class-size compliance, Unit-Tests fuer Fachregeln und Regressionstests fuer Bugfixes
- [ ] UI approach confirms editor-authored UI only (no runtime-generated UI elements)
- [ ] Documentation split analysis completed for expanded docs
- [ ] Diagram sync and freshness checks planned for PR review
- [ ] Documentation and project communication are in German

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── diagrams/            # PlantUML diagrams: Activity/Sequence/Class/State/Mindmap as needed
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., Runtime-generated UI request] | [specific reason] | [why editor-authored UI could not satisfy, with approval reference] |
| [e.g., Deferred doc split] | [constraint] | [why immediate split not feasible + tracked follow-up] |
| [e.g., Diagram exception] | [why a required PlantUML diagram is omitted] | [why a lighter documentation form is sufficient + owner/due date] |
