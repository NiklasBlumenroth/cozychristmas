# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]` *(globale fortlaufende Feature-Nummer, nicht pro Short-Name neu starten)*
**Created**: [DATE]
**Status**: Draft
**Input**: User description: "$ARGUMENTS"

## Change Classification & Documentation Impact *(mandatory)*

- **Change Type**: [Planned feature (waterfall) / Spontaneous extension]
- **Documentation Scope**: [List all existing docs that MUST be updated]
- **New Documentation**: [List new docs to create, if any]
- **Documentation Language**: [German (mandatory)]
- **Communication Language**: [German for project communication]
- **Merge Coverage Evidence**: [How review can verify all code changes are reflected in docs]
- **Personal Notes Exclusion**: [List personal management files excluded from coverage, e.g. `Notizen`]
- **Documentation Split Analysis**: [State whether any expanded doc must be split and what was done]
- **Diagram Scope**: [Required PlantUML diagrams or "Not required" with reason]
- **Diagram Coverage Evidence**: [How review can verify diagrams cover changed logic/data/flows]

## System Context & Collision Check *(mandatory)*

- **Impacted Existing Systems**: [Modules, data flows, interfaces this feature builds on]
- **Known Collisions/Conflicts**: [None or explicit list]
- **Resolution Before Implementation**: [Definitions/requirements updated before code]
- **Open Clarifications**: [Use NEEDS CLARIFICATION entries if unresolved]
- **PlantUML Context Map**: [Mindmap/Activity/Sequence/Class/State diagrams to create or update under `specs/[###-feature]/diagrams/`]

## Diagram Requirements *(mandatory when feature changes Fachlogik, states, data, snapshots, or cross-system flows)*

<!--
  Select only diagrams that create practical value. Do not create diagram noise for
  trivial copy/text-only changes.

  Required selection rules:
  - Activity: decision rules, validation branches, priorities, fallback/replan logic
  - Sequence: cross-system runtime flow or Apply side effects
  - Class/Domain: entities, relationships, inventories, ledgers, save snapshots
  - State: lifecycle with more than two relevant domain states
  - Mindmap: large feature, refactor, or overview needed for review/KI prompts
-->

- **Activity Diagrams**: [List files or "Not required: ..."]
- **Sequence Diagrams**: [List files or "Not required: ..."]
- **Class/Domain Diagrams**: [List files or "Not required: ..."]
- **State Diagrams**: [List files or "Not required: ..."]
- **Mindmap**: [List files or "Not required: ..."]
- **Diagram Exceptions**: [Any omitted diagram despite selection rule, with reason/owner/due date]

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?
- What happens if this feature collides with an existing workflow/module?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]
- **FR-006**: Feature branch MUST include documentation updates for every impacted behavior and interface
- **FR-007**: System definitions/requirements MUST be updated before implementation when conflicts are identified
- **FR-008**: No UI elements may be generated at runtime by gameplay code; UI MUST be editor-authored
- **FR-009**: Compilation MUST succeed after code changes before merge
- **FR-010**: Documentation and project communication artifacts for this feature MUST be in German
- **FR-011**: PlantUML diagrams MUST be created or updated when this feature changes Fachlogik, state transitions, persisted data/snapshots, or cross-system flows
- **FR-012**: Diagram-derived test candidates MUST be reflected in the test plan or documented as explicit exceptions

*Example of marking unclear requirements:*

- **FR-013**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-014**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
- **SC-005**: [100% documentation coverage check passes at merge]
- **SC-006**: [Compile check passes with zero errors after implementation]
- **SC-007**: [Required PlantUML diagrams are present, current, and mapped to changed code/test areas]
