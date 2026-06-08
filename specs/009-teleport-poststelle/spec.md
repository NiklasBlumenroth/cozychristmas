# F9 – Teleport (Eingang ↔ Innenraum)

## Ziel
Der Spieler soll Gebäude (zunächst die Poststelle) betreten/verlassen, indem er einen
Trigger-Collider berührt und an eine zugeordnete Zielposition versetzt wird. Konfiguration
als Liste beliebig vieler Paare „Trigger-Collider → Ziel-Transform".

## Warum Komponente statt ScriptableObject
Ein ScriptableObject (Projekt-Asset) kann **keine Szenen-Objekte** (Collider, Transform)
referenzieren. Da sowohl Trigger als auch Ziele Szenen-Objekte sind, lebt die Konfiguration
in einer Komponente (`TeleportRouter`) im jeweiligen Gebäude-Prefab.

## Architektur (Decide/Apply, Constitution-Prinzip VIII/IX)
- **Core – `CozySanta.Core.Teleport.TeleportArbiter`** (rein, ohne Unity): Re-Entry-Schutz
  gegen das „Bounce"-Problem (Spieler landet im Ziel-Trigger → würde sofort wieder ausgelöst).
  Modell: ein Trigger ist „belegt", solange der Spieler darin steht; belegte Trigger feuern
  nicht. `ShouldTeleport(id)` entscheidet, `MarkOccupied(id)` belegt Ziel-Trigger vor,
  `NotifyExit(id)` gibt frei.
- **Runtime – `CozySanta.Runtime.Teleport.TeleportRouter`** (Apply): hält die Paar-Liste,
  erkennt den Spieler über `FirstPersonController`, versetzt CharacterController-sicher
  (deaktivieren → Position/Rotation setzen → reaktivieren), setzt Fallgeschwindigkeit zurück
  (`FirstPersonController.ResetVerticalVelocity`) und meldet ziel-überlappende Trigger als belegt.
- **Runtime – `TeleportTriggerForwarder`**: wird beim Start automatisch an jeden Trigger
  gehängt; leitet `OnTriggerEnter/Exit` mit dem Paar-Index an den Router (Unity feuert
  Trigger-Events nur am Collider-Objekt selbst).

## Konfiguration im Editor
Komponente `TeleportRouter` am Gebäude-Prefab. Pro Listeneintrag:
- **Trigger**: Collider, dessen Berührung teleportiert (isTrigger wird erzwungen).
- **Destination**: leeres GameObject; Position = Ziel. Bei `faceDestination` zusätzlich dessen
  Y-Drehung als neue Blickrichtung.

## Tests
EditMode `TeleportArbiterTests` (TP1–TP4): erstes Betreten löst aus; belegter Trigger nicht;
nach Verlassen erneut; vorbelegtes Ziel verschluckt das Lande-Enter und re-armt nach Verlassen.
Apply-Schicht (CharacterController-Versatz, Trigger-Forwarding) = Editor-/PlayMode-Iteration,
dokumentierte Nicht-Unit-Ausnahme.

## Status
Core + Test grün, Runtime baut fehlerfrei. Editor-Verdrahtung (Trigger/Ziele am Poststellen-
Prefab) erfolgt manuell im Editor.
