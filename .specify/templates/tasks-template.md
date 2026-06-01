---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, diagrams/, contracts/

**Tests**: The examples below include test tasks. Tests are MANDATORY for code-bearing changes in Fachlogik (Unit-Tests) and for bug fixes (Regressionstests). Compile checks and 300-line class-size DoD checks for code changes are MANDATORY.

**Diagrams**: PlantUML diagrams are MANDATORY when the feature changes Fachlogik, lifecycle states, persisted data/snapshots, or cross-system flows. Use Activity, Sequence, Class/Domain, State, and Mindmap selectively; document exceptions explicitly.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

<!--
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.

  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Required PlantUML diagrams from spec.md and plan.md
  - Endpoints from contracts/

  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment

  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and baseline compliance setup

- [ ] T001 Classify change type (planned feature vs spontaneous extension) and record in docs
- [ ] T002 Identify all impacted documentation files and create update plan
- [ ] T003 [P] Ensure documentation and project communication artifacts are in German
- [ ] T004 [P] Capture whole-system impact map (modules/interfaces/data flows)
- [ ] T005 [P] Determine required PlantUML diagrams (Activity/Sequence/Class-State/Mindmap) and record exceptions
- [ ] T006 [P] Configure linting and formatting tools

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure and governance prerequisites that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

Examples of foundational tasks (adjust based on your project):

- [ ] T007 Resolve requirement/definition collisions found in context analysis
- [ ] T008 [P] Create `specs/[###-feature]/diagrams/` and initial PlantUML files required by spec/plan
- [ ] T009 [P] Setup API routing and middleware structure
- [ ] T010 [P] Setup environment configuration management
- [ ] T011 Create base models/entities that all stories depend on
- [ ] T012 Define compile check command(s), PlantUML diagram check, and 300-line class-size DoD check approach for this feature scope
- [ ] T013 Define test architecture for this feature (Core-vs-Adapter split, Decide-vs-Apply boundaries)
- [ ] T014 Map diagram-derived test candidates (Activity branches, State transitions, Sequence side effects, Domain invariants)
- [ ] T015 Confirm UI approach uses editor-authored assets only (no runtime UI generation)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1 (MANDATORY fuer Fachlogik/Bugfixes) ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T016 [P] [US1] Unit test fuer neue/geaenderte Entscheidungsregeln in tests/unit/test_[name].py
- [ ] T017 [P] [US1] Regressionstest fuer relevanten Bug-/Edge-Case in tests/integration/test_[name].py
- [ ] T018 [P] [US1] Tests aus PlantUML ableiten/abgleichen (Activity-Branches, State-Transitions, Sequence-Side-Effects)

### Implementation for User Story 1

- [ ] T019 [P] [US1] Update required PlantUML diagrams for this story in `specs/[###-feature]/diagrams/`
- [ ] T020 [P] [US1] Create [Entity1] model in src/models/[entity1].py
- [ ] T021 [P] [US1] Create [Entity2] model in src/models/[entity2].py
- [ ] T022 [US1] Implement [Service] in src/services/[service].py (depends on T020, T021)
- [ ] T023 [US1] Implement [endpoint/feature] in src/[location]/[file].py
- [ ] T024 [US1] Add validation and error handling
- [ ] T025 [US1] Run compile check, PlantUML diagram check, class-size DoD check, and mandatory test suite before closing US1

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2 (MANDATORY fuer Fachlogik/Bugfixes) ⚠️

- [ ] T026 [P] [US2] Unit test fuer neue/geaenderte Entscheidungsregeln in tests/unit/test_[name].py
- [ ] T027 [P] [US2] Regressionstest fuer relevanten Bug-/Edge-Case in tests/integration/test_[name].py
- [ ] T028 [P] [US2] Tests aus PlantUML ableiten/abgleichen (Activity-Branches, State-Transitions, Sequence-Side-Effects)

### Implementation for User Story 2

- [ ] T029 [P] [US2] Update required PlantUML diagrams for this story in `specs/[###-feature]/diagrams/`
- [ ] T030 [P] [US2] Create [Entity] model in src/models/[entity].py
- [ ] T031 [US2] Implement [Service] in src/services/[service].py
- [ ] T032 [US2] Implement [endpoint/feature] in src/[location]/[file].py
- [ ] T033 [US2] Integrate with User Story 1 components (if needed)
- [ ] T034 [US2] Run compile check, PlantUML diagram check, class-size DoD check, and mandatory test suite before closing US2

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3 (MANDATORY fuer Fachlogik/Bugfixes) ⚠️

- [ ] T035 [P] [US3] Unit test fuer neue/geaenderte Entscheidungsregeln in tests/unit/test_[name].py
- [ ] T036 [P] [US3] Regressionstest fuer relevanten Bug-/Edge-Case in tests/integration/test_[name].py
- [ ] T037 [P] [US3] Tests aus PlantUML ableiten/abgleichen (Activity-Branches, State-Transitions, Sequence-Side-Effects)

### Implementation for User Story 3

- [ ] T038 [P] [US3] Update required PlantUML diagrams for this story in `specs/[###-feature]/diagrams/`
- [ ] T039 [P] [US3] Create [Entity] model in src/models/[entity].py
- [ ] T040 [US3] Implement [Service] in src/services/[service].py
- [ ] T041 [US3] Implement [endpoint/feature] in src/[location]/[file].py
- [ ] T042 [US3] Run compile check, PlantUML diagram check, class-size DoD check, and mandatory test suite before closing US3

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Documentation Sync & Merge Readiness

**Purpose**: Ensure branch is merge-ready under constitution rules

- [ ] T043 Map every changed code area to updated docs and PlantUML diagrams (documentation coverage matrix; excluding personal management notes such as `Notizen`)
- [ ] T044 Perform documentation split analysis for expanded docs and implement splits if needed
- [ ] T045 Validate docs and project communication evidence are in German
- [ ] T046 Update quickstart/usage docs where behavior changed
- [ ] T047 Validate no runtime-generated UI elements were introduced
- [ ] T048 Validate PlantUML files are renderable or pass the repository diagram check, and contain no stale placeholders
- [ ] T049 Final compile check passes with zero errors, final class-size DoD check passes, diagram check passes, and mandatory tests pass

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] TXXX [P] Documentation updates in docs/
- [ ] TXXX Code cleanup and refactoring
- [ ] TXXX Performance optimization across all stories
- [ ] TXXX [P] Additional unit tests for uncovered edge-cases in tests/unit/
- [ ] TXXX Security hardening
- [ ] TXXX Run quickstart.md validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 -> P2 -> P3)
- **Documentation Sync (Phase 6)**: Depends on completed implementation for included stories
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests for Fachlogik/Bugfixes MUST be written and FAIL before implementation
- Required PlantUML diagrams MUST be created or updated before/with implementation
- Diagram-derived test candidates MUST be checked before closing the story
- Models before services
- Services before endpoints
- Core implementation before integration
- Compile check MUST pass before story completion
- PlantUML diagram check MUST pass before story completion when diagrams are present
- 300-line class-size DoD check MUST pass before story completion
- Mandatory Unit/Regression tests MUST pass before story completion
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch mandatory tests for User Story 1 together:
Task: "Unit test fuer neue/geaenderte Entscheidungsregeln in tests/unit/test_[name].py"
Task: "Regressionstest fuer relevanten Bug-/Edge-Case in tests/integration/test_[name].py"

# Launch all models for User Story 1 together:
Task: "Create [Entity1] model in src/models/[entity1].py"
Task: "Create [Entity2] model in src/models/[entity2].py"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. Run compile check and documentation coverage updates
5. **STOP and VALIDATE**: Test User Story 1 independently
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational -> Foundation ready
2. Add User Story 1 -> Compile + docs check -> Test independently -> Deploy/Demo (MVP!)
3. Add User Story 2 -> Compile + docs check -> Test independently -> Deploy/Demo
4. Add User Story 3 -> Compile + docs check -> Test independently -> Deploy/Demo
5. Complete Phase 6 merge-readiness checks

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently
4. Team performs documentation sync and merge checks together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Compile checks, class-size DoD checks, and mandatory tests are required when code changes
- PlantUML diagrams are required for changed Fachlogik, lifecycle states, snapshots/data, and cross-system flows unless an explicit exception is documented
- UI elements must be editor-authored; do not generate UI elements at runtime in gameplay code
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
