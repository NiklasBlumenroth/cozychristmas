# Cozy Santa Factory – Arbeitsleitfaden

> Hinweis: Projektdokumentation und Kommunikation in diesem Repository sind
> **auf Deutsch** zu halten (siehe Constitution, Prinzip VI). Technische
> Bezeichner, API-Namen und direkte externe Zitate duerfen im Original bleiben.

## Worum geht es

Cozy Santa Factory ist ein cozy 3D-Aufraeumspiel in **Unity** (First-Person, PC).
Ein magischer Sturm hat die Fabrik des Weihnachtsmannes verwuestet. Der Spieler
schmilzt mit einer magischen Lampe Schnee, sortiert durcheinandergeratene Objekte,
schaltet Sektoren ueber Schluessel und Tore frei und vollendet am Ende den
zentralen Weihnachtsbaum mit gefundenen Christbaumkugeln.

Kernmechaniken: Schnee schmelzen (maskenbasiert), Objekte aufnehmen/tragen
(Gewichtssystem bis 25 kg, Links/Rechts-Hand-Stapellogik), Sortieren mit
Lampenfeedback, Montieren (Rohre/Zahnraeder), XP/Skill-Progression (~20 Stufen
je Skilloption, kein fester Skilltree), Geschenkcontainer (25er-Batches).

## Wo steht was

| Pfad | Inhalt |
| --- | --- |
| `GameKonzept/docs/game-concept/00_uebersicht.md` | Einstieg + Doku-Schichten |
| `GameKonzept/docs/game-concept/01_vision.md` | Vision, Genre, Plattform |
| `GameKonzept/docs/game-concept/03_gameplay.md` | Regeln, Aktionen, Loops, Skills (Hauptreferenz) |
| `GameKonzept/docs/game-concept/04_welt_und_narrative.md` | Sektoren, Gebaeude, narrativer Verlauf |
| `GameKonzept/docs/game-concept/05_systeme.md` | XP, Skills, Gewicht, Cooldowns, Maskensystem |
| `GameKonzept/docs/game-concept/06_content_progression.md` | Content-Mengen, Freischaltlogik, Balancing |
| `GameKonzept/docs/game-concept/07_scope_mvp.md` | MVP-Schnitt und technische Risiken |
| `GameKonzept/docs/game-concept/08_offene_fragen.md` | Offene Klaerungspunkte |
| `.specify/memory/constitution.md` | **Verbindliche Projektregeln** (siehe unten) |

## Verbindliche Regeln (Constitution-Kurzfassung)

Vor Code-Aenderungen die Constitution lesen. Die wichtigsten Pflichten:

- **Doku im selben Branch:** Jede Aenderung wird dokumentiert – entweder ueber
  Spec-Kit-Artefakte (`spec.md`, `plan.md`, `tasks.md`) oder als direkte
  Erweiterung der bestehenden Doku. Kein Merge ohne verifizierte Doku-Abdeckung.
- **Editor-authored UI:** UI wird in Unity-Szenen/Prefabs im Editor erstellt.
  Laufzeitcode darf vorhandene UI nur binden/aktualisieren, nicht generieren.
- **Zero-Compile-Error:** Bei jeder Code-Aenderung Compile-Check ausfuehren,
  alle Fehler im selben Branch beheben.
- **300-Zeilen-Limit:** Keine Produktions-Klassendatei > 300 Zeilen (gilt auch
  pro `partial`-Datei, Aufteilung entlang fachlicher Verantwortung).
- **Testbare Architektur:** Fachlogik in einer entkoppelten Core-Schicht ohne
  `MonoBehaviour`/`UnityEngine`/`FindObjectsOfType`/`Time.time`. Zeit/Welt ueber
  Provider-Interfaces. `Decide` (Entscheidung) und `Apply` (Seiteneffekt) trennen.
  Regeltests als schnelle EditMode-/Unit-Tests, PlayMode nur fuer kritische Flows.
- **Tests:** Neue/geaenderte Fachregel → mind. ein Unit-Test. Bugfix →
  Regressionstest. Testnachweise im Feature-Artefakt dokumentieren.
- **PlantUML:** Bei geaenderter Fachlogik, Zustaenden, persistierten Daten oder
  systemuebergreifenden Flows Diagramme unter `specs/[###-feature]/diagrams/`
  anlegen/aktualisieren (pragmatische Diagrammwahl, mit Testkandidaten abgleichen).

## Build und Werkzeuge

- Engine: **Unity** (Projekt-Root mit `Assets/`, `Packages/`, `ProjectSettings/`).
- Skript-Build/Diagnose ueber die generierten C#-Projekte:
  `dotnet build Assembly-CSharp.csproj` bzw. `dotnet build cozychristmas.sln`.
- Spec-Kit-Tooling liegt unter `.specify/scripts/powershell/` (z. B.
  `create-new-feature.ps1`, `setup-plan.ps1`, `update-agent-context.ps1`),
  Prompts unter `.codex/prompts/`.
- `.csproj`/`.sln` werden von Unity generiert und sind nicht versioniert
  (siehe `.gitignore`).

## Code-Architektur & Konvention (ab F1)

Quellcode liegt additiv unter `Assets/_Project/`, getrennt über Assembly Definitions
(setzt Constitution-Prinzip IX technisch durch):

```
Assets/_Project/
├── Core/      → CozySanta.Core      (reines C#, noEngineReferences: true – keine UnityEngine-Typen!)
├── Runtime/   → CozySanta.Runtime   (MonoBehaviours, Provider-Impl., referenziert Core)
└── Tests/
    ├── EditMode/ → CozySanta.Tests.EditMode (refs Core; schnelle Regel-/Unit-Tests, kein Szenenstart)
    └── PlayMode/ → CozySanta.Tests.PlayMode (refs Core + Runtime; gezielte E2E-Flows)
```

- **Namespaces folgen dem Ordner**: `CozySanta.Core.<Bereich>`, `CozySanta.Runtime.<Bereich>`.
- **Decide/Apply**: Entscheidungslogik als reine Methode in Core (`Decide`), Seiteneffekt in der
  Runtime (`Apply`). Zeit-/Welt-/Eingabezugriffe nur über Provider-Interfaces (in Tests gemockt).
- **Abhängigkeit strikt einseitig**: Runtime → Core, niemals umgekehrt.
- Schritt-für-Schritt-Anleitung zum Anlegen eines neuen Features:
  `specs/001-core-architektur-fundament/quickstart.md`.

## Fortschritt

- **F1 (abgeschlossen)**: Testbares Architekturfundament (Core/Runtime/Tests, Decide/Apply, Provider).
- **F2 (abgeschlossen)**: First-Person-Controller (WASD + Maus-Blick, CharacterController)
  und Interaktionssystem (blick-/reichweitenbasierte Erkennung über F1-`InteractionSelector`,
  editor-authored Hinweis, `Interact`-Auslösung auf das fokussierte `IInteractable`).
- **F3 (abgeschlossen)**: Trag-, Hand- & Gewichtssystem — Core-`CarryStack` (LIFO + Traglast),
  `IPickup`/`PickupInteractable`, `PlayerCarry` (Links/Rechts-Anker: links an der Kamera = aktuelles
  Objekt, rechts am Körper = Stapel mit fester Basis), Ablegen via „Q", Test-Spawner + Prefab.
- **F4 (in Arbeit)**: Sortiersystem & Sortierfeedback — Core-`SortKey`/`SortTarget` (Klassifizierung
  korrekt/falsch, konfigurierbare Soll-Menge, LIFO-Entnahme, Abschluss/Sperre, `JustCompleted`),
  Runtime `ISortable`/`Sortable` + `SortTargetInteractable` (Fach: Einsortieren/Entnehmen kontextabhängig
  über das fokussierte Fach, Reparenting an Slot, eingelegte Objekte ohne aktive Interaktions-Collider,
  Lampe + Schließen bei Vollständigkeit, `onCompleted` als XP-Andockpunkt/F6). `PlayerCarry` additiv um
  `CanCarry`/`TryHandOverTop` erweitert; `PlayerInteractionController` routet fokussierte Fächer.
  Dev-Tool `DevSpawnMenu` (IMGUI): „G" öffnet Auswahlliste, „R" spawnt das selektierte Prefab
  (ersetzt den festen „O"-Spawner). Editor-Setup „CozySanta/Setup F4 …" verdrahtet Prefabs.

## Status / MVP-Fokus

Erster Sektor (Eingangsbereich + Poststelle, optional Dekorationsfabrik) als
grauer Prototyp: Schnee schmelzen, XP, einfache Skill-Upgrades, Aufnehmen/
Sortieren, Lampenfeedback, ein Schluessel, ein Tor. Erst wenn Schnee, Tragen,
Sortieren und Aufgabenfortschritt Spass machen, wird visuell ausgebaut.
Offene Punkte siehe `08_offene_fragen.md`.
