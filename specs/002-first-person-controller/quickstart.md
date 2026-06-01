# Quickstart: F2 First-Person-Controller in der Grey-Box-Szene

So bringst du den Controller in einer Testszene zum Laufen (nach der Implementierung).

## Szene & Player aufsetzen

1. Neue Szene unter `Assets/_Project/Scenes/Greybox.unity` (oder bestehende `SampleScene` als
   Spielwiese). Boden + ein paar Wände mit Collidern (Grey-Box, z. B. Cubes).
2. Player-Hierarchie:
   - `Player` (leeres GameObject) mit `CharacterController` + `FirstPersonController`.
   - Child `Camera` (Main Camera) auf Augenhöhe – wird für Pitch genutzt.
   - `PhysicsInteractionProbe` auf `Player` (oder Camera), `view` = Kamera-Transform.
   - `PlayerInteractionController` auf `Player`, referenziert Probe + `InteractionPromptPresenter`.
3. Input: das vorhandene `InputSystem_Actions` nutzen (Map „Player": Move/Look/Interact). Per
   `PlayerInput` oder direktem Action-Binding im `FirstPersonController` verdrahten.

## Interaktionshinweis (editor-authored, Pflicht!)

4. Ein **im Editor erstelltes** Canvas mit einem Hinweis-Element (z. B. Text „[E] Interagieren")
   anlegen. Das Hinweis-Objekt dem `InteractionPromptPresenter.promptRoot` zuweisen.
   - **Wichtig**: Der Code erzeugt dieses UI NICHT zur Laufzeit (Constitution V). Er blendet es nur
     ein/aus. Standardmäßig deaktiviert lassen.

## Test-Interactable platzieren

5. Ein Cube mit Collider + einer einfachen `IInteractable`-Implementierung (Test-Komponente, die
   bei `Interact()` einen sichtbaren Zustand wechselt, z. B. Farbe/Log) in Reichweite platzieren.

## Prüfen (entspricht den Acceptance-Szenarien)

- WASD bewegt relativ zur Blickrichtung; Maus dreht Blick, Pitch begrenzt; keine Wanddurchdringung.
- Anschauen des Test-Cubes in Reichweite/Winkel → Hinweis erscheint; Wegschauen → Hinweis weg.
- Interact-Taste bei sichtbarem Hinweis → Cube reagiert; ohne Fokus → keine Reaktion.

## Tests

- **EditMode**: `LookMath`, `MovementCalculator`, `InteractionTrigger` (Test Runner → EditMode).
- **PlayMode**: `FirstPersonInteractionPlayModeTests` (Fake-`IInteractable`, Minimalszene).
- **Compile**: Build im Editor bzw. `dotnet build` für die Core-Schicht.
