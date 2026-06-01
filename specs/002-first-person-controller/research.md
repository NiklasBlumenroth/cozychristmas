# Phase 0 – Research: F2 First-Person-Controller & Interaktionssystem

Keine offenen `NEEDS CLARIFICATION`. Entscheidungen:

## 1. Controller-Typ: CharacterController vs Rigidbody

- **Decision**: `CharacterController`.
- **Rationale**: Cozy, kein Physik-Geschubse, deterministische Bewegung, einfache Wand-Kollision/
  Gleiten, testfreundlich. Entspricht der in `Notizen.md` festgehaltenen Vorab-Entscheidung.
- **Alternatives considered**: Rigidbody-Controller – verworfen (mehr Tuning, unruhigeres Gefühl,
  schlechtere Vorhersagbarkeit für ein ruhiges Aufräumspiel).

## 2. Blick: Yaw/Pitch-Aufteilung + Clamp

- **Decision**: Yaw (Links/Rechts) dreht den Körper (Player-Root); Pitch (Hoch/Runter) dreht die
  Kamera lokal und wird über `LookMath.ClampPitch` auf einen konfigurierbaren Bereich begrenzt.
- **Rationale**: Standardmuster für FP-Controller; Bewegung folgt dem Körper-Yaw, Pitch ohne
  Überschlagen. Clamp ist reine, testbare Logik (Prinzip IX).
- **Alternatives considered**: Pitch am Körper – verworfen (kippt den CharacterController).

## 3. Reine Mathematik ohne UnityEngine

- **Decision**: Core-Funktionen nutzen `System.Numerics.Vector2/Vector3` (in netstandard2.1
  verfügbar, **kein** UnityEngine-Typ). Runtime konvertiert zu/von `UnityEngine.Vector*`.
- **Rationale**: Erlaubt Vektor-Mathematik in der `noEngineReferences`-Core-Schicht, ohne einen
  eigenen Vektortyp einzuführen.
- **Alternatives considered**: Eigener `Vec2/Vec3`-Typ – unnötig; nur skalare Übergabe – weniger
  ausdrucksstark.

## 4. Eingabe über Input System

- **Decision**: Vorhandene `InputSystem_Actions`, Map „Player", Actions Move/Look/Interact nutzen.
  Jump/Crouch/Sprint/Attack werden im MVP nicht verdrahtet.
- **Rationale**: Assets sind bereits vorhanden; minimaler, cozy Steuerungsumfang (`Notizen.md`).
- **Alternatives considered**: Eigene Action-Map neu anlegen – unnötiger Mehraufwand für MVP.

## 5. Fokus-Auflösung TargetId → IInteractable

- **Decision**: Core bleibt unverändert pur: `InteractionSelector.Decide` liefert ein `TargetId`.
  Die Auflösung auf das konkrete `IInteractable` passiert in der Runtime: `PhysicsInteractionProbe`
  baut Kandidaten nur aus Collidern mit einem `IInteractable` und hält pro Frame eine Zuordnung
  `TargetId → IInteractable` (Runtime-Interface `IInteractableResolver`). `TargetId` ist die
  `GetInstanceID()` der Interactable-Komponente.
- **Rationale**: Hält Core frei von Runtime-Typen (FR-010), erfüllt FR-005/FR-006 ohne zweite
  Auswahlregel und ohne erneute Weltabfrage.
- **Alternatives considered**: Objektreferenz in `InteractionCandidate` – verworfen (würde Core an
  Runtime/`IInteractable` koppeln). Zweite Raycast-Abfrage zur Auflösung – verworfen (redundant,
  Inkonsistenzrisiko).

## 6. Editor-authored Interaktionshinweis (UI)

- **Decision**: `InteractionPromptPresenter` referenziert ein im Editor erstelltes Hinweis-Objekt
  (SerializeField, z. B. Canvas-Child) und ruft nur `SetActive(true/false)` (optional Textbindung).
  Kein UI wird zur Laufzeit erzeugt (Prinzip V, FR-008).
- **Rationale**: Erfüllt die Editor-authored-UI-Regel; headless testbar über den Fokuszustand des
  Controllers statt über das echte Canvas.
- **Alternatives considered**: UI im Code instanziieren – durch Constitution verboten.

## Offene Punkte

- Keine. Alle Designfragen sind oben oder in den Spec-Assumptions aufgelöst.
