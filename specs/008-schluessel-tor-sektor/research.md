# Research: F8 – Schlüssel-, Tor- & Sektorfreischaltung

## Entscheidung 1: Schlüssel – physisches Objekt vs. automatisches Inventar-Flag

**Entscheidung**: Physisches Objekt (Prefab in der Szene), aufhebbar über F2-IInteractable  
**Begründung**: Passt zur Spielphantasie (Finden, Aufheben); nutzt das bestehende Interaktionssystem; gibt dem Spieler Kontrolle über Zeitpunkt des Aufhebens  
**Verworfene Alternative**: Automatisches Hinzufügen zum Inventar bei Area-Abschluss – zu abstrakt, kein physischer Fundmoment

## Entscheidung 2: Schlüssel im Carry-Stack oder eigenes Inventar?

**Entscheidung**: Eigenes `KeyInventory`; Schlüssel gehen NICHT in den Carry-Stack  
**Begründung**: Schlüssel sollen dauerhaft gehalten werden ohne Tragkraft zu verbrauchen; Icons am Bildschirmrand statt Objekte in der Hand; Carry-Stack bleibt für Spielobjekte reserviert  
**Verworfene Alternative**: Schlüssel als normales Carry-Objekt – würde Tragkraft verbrauchen und visuell ablenken

## Entscheidung 3: Tor-Öffnung – automatisch bei Annäherung vs. explizite Interaktion

**Entscheidung**: Automatisch bei Betreten der Proximity-Zone (BoxCollider-Trigger am Tor)  
**Begründung**: Flusspfreier; der Spieler muss nicht explizit mit dem Tor interagieren; Schlüsselprüfung läuft im Hintergrund; eindeutiges Feedback  
**Verworfene Alternative**: Explizite Interaktion (Taste drücken am Tor) – zusätzlicher Schritt ohne Spielwert

## Entscheidung 4: Area-Zonen – Volumen, Flächen oder Objekt-Tags?

**Entscheidung**: BoxCollider-Trigger pro Area-Zone, im Editor aufziehbar; separates `AreaZone`-Component  
**Begründung**: Visuell im Scene-View direkt erkennbar und anpassbar; Standard-Unity-Workflow; kein Custom-Tool nötig  
**Verworfene Alternative**: Per-Objekt-Tags auf jedem Interactable → keine räumliche Logik, nur statische Zuordnung; Flächen (2D) → schlechter bei mehrgeschossigen Bereichen

## Entscheidung 5: Overlap-Verhalten bei mehreren aktiven Area-Zones

**Entscheidung**: Alle aktiven Zones werden gleichzeitig angezeigt (Set-basiert, kein Stack, keine Priorität)  
**Begründung**: Designentscheidung des Users; der Spieler sieht beide Aufgabenlisten bei Überlappung; beim Verlassen wird nur die verlassene Zone entfernt  
**Verworfene Alternative**: Prioritätssystem oder Stack (letzte gewinnt) → versteckt Information für den Spieler

## Entscheidung 6: Schlüssel-Spawn – direkt am AreaTracker oder separater SpawnPoint?

**Entscheidung**: Separater `KeySpawnBinding`-Component, der an denselben GameObject-Baum wie der AreaTracker gehängt wird; hat Referenz auf AreaTracker und auf Key-Prefab + SpawnTransform  
**Begründung**: Hält `AreaTracker` schmal (single responsibility); SpawnPoint kann frei im Level positioniert werden; keine Kopplung AreaTracker → Key  
**Verworfene Alternative**: AreaTracker bekommt Key-Spawn-Felder direkt → verletzt Single-Responsibility, macht AreaTracker größer

## Entscheidung 7: GateController – State-Machine oder einfaches bool?

**Entscheidung**: Einfache drei-Phasen-Logik: `Geschlossen → Öffnend → Offen` als enum in Core (`GateState`); `GateController` in Runtime führt die Öffnungs-Animation aus  
**Begründung**: Drei Zustände sind klar; Decide/Apply-Trennung ist einfach umsetzbar; kein vollwertiger State-Machine-Overhead nötig  
**Verworfene Alternative**: Vollständige StateMachine-Klasse → Overengineering für drei Zustände

## Entscheidung 8: AreaManager – Singleton oder Component?

**Entscheidung**: `AreaManager` als MonoBehaviour-Component in der Szene; `AreaZone` hält eine direkte Inspector-Referenz darauf  
**Begründung**: Kein Singleton-Anti-Pattern; explizite Verdrahtung im Editor; einfach testbar durch Dependency-Injection  
**Verworfene Alternative**: Singleton/ServiceLocator → globaler Zustand, schwer testbar
