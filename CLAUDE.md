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
- **F4 (abgeschlossen)**: Sortiersystem & Sortierfeedback — Core-`SortKey`/`SortTarget` (Klassifizierung
  korrekt/falsch, konfigurierbare Soll-Menge, LIFO-Entnahme, Abschluss/Sperre, `JustCompleted`),
  Runtime `ISortable`/`Sortable` + `SortTargetInteractable` (Fach: Einsortieren/Entnehmen kontextabhängig
  über das fokussierte Fach, Reparenting an Slot, eingelegte Objekte ohne aktive Interaktions-Collider,
  Lampe + Schließen bei Vollständigkeit, `onCompleted` als XP-Andockpunkt/F6). `PlayerCarry` additiv um
  `CanCarry`/`TryHandOverTop` erweitert; `PlayerInteractionController` routet fokussierte Fächer.
  Dev-Tool `DevSpawnMenu` (IMGUI): „G" öffnet Auswahlliste, „R" spawnt das selektierte Prefab
  (ersetzt den festen „O"-Spawner). Editor-Setup „CozySanta/Setup F4 …" verdrahtet Prefabs.
- **F5 (abgeschlossen)**: Schnee-Schmelzsystem — Core `LampBattery` (Akku) + `MeltField` (Zell-Höhenfeld,
  Coverage %); Runtime `SnowPatch` (Grid-Mesh spiegelt das Höhenfeld via Vertex-Displacement, Höhe im
  Vertex-Color) + `MeltController` (Raycast auf die Patch-Ebene, „F" schmelzen mit Akku-Verbrauch, „V"
  Schnee auftragen als Dev-Helfer). URP-Shader `CozySanta/SnowMelt` clippt freigelegte Stellen mit
  weicher Noise-Kante (Boden erscheint), Textur/Fake-Lighting/Glitzer. Editor-Setup
  „CozySanta/Setup F5 …" (Material + Patch + Verdrahtung). Core unit-getestet; Shader/Look = Editor-
  Iteration (dokumentierte Nicht-Unit-Ausnahme). Boden = Szenen-Plane, Schnee-Textur `Holiday_Snow_02`.
- **F6 (abgeschlossen)**: XP- & Skillsystem — Core `XpLedger` (gemeinsamer XP-Pool, Level-Kurve n×100,
  1 Skillpunkt/Level), `Skill`/`SkillConfig` (Basis+Schritt×Stufe, Deckelung, Freischalt-Flag),
  `SkillSet` (7 Optionen: LampPower/LampCone/LampBattery/CarryCapacity/MoveSpeed/SortVision/ObjectPull,
  freie Investition ohne Tree), `ProgressionState` (bündelt Ledger+Skills, AvailablePoints). Runtime
  `PlayerProgression` (Apply: schreibt XP aus F4-`onCompleted` + F5-Coverage-Delta gut, überträgt
  Skillwerte auf `PlayerCarry.Capacity`, `MeltController`-Felder, `FirstPersonController.MoveSpeed`);
  editor-authored Skillmenü (`SkillMenuView`/`SkillEntryUI`-Prefab, „X" öffnen/schließen, Invest-Buttons).
  IMGUI-Dev-Tool `SkillMenuDevTool` (F2). Editor-Setup „CozySanta/Setup F6 …" erstellt Panel+Prefab
  und verdrahtet alle Referenzen. EditMode-Tests X1–X4, S1–S5, V1 grün.

- **F7 (abgeschlossen)**: Area- & Aufgabensystem + HUD — Core `TaskType`/`AreaTask`/`AreaDefinition`/`AreaProgress`
  (BookSort/BookMelt, IsComplete, OnCompleted einmalig); Runtime `AreaTracker` (Inspector-Konfiguration: Area-Name,
  Tasks[], SortBindings, MeltTaskId; verdrahtet F4-onCompleted + F5-Coverage-Delta; XP bei Abschluss über F6-Andockpunkt),
  `AreaHudView` (editor-authored HUD oben rechts: Area-Name, Task-Zeilen, Akku-Balken, XP/Level), `TaskEntryUI`-Prefab,
  `LadeStation` (IInteractable, Rechtsklick+LoS, 10s-Ladedauer, auto-wire MeltController).
  `MeltController`: Akku läuft immer bei gedrücktem F (nicht nur bei Schnee-Treffer); passives Nachladen entfernt.
  Editor-Setup „CozySanta/Setup F7 …". EditMode-Tests A1–A3, B1–B2, C1–C2 grün.

- **F8 (abgeschlossen)**: Schlüssel-, Tor- & Sektorfreischaltung — Core `KeyInventory` (HashSet gehaltener
  Schlüssel-IDs: AddKey/RemoveKeys/HasKeys, kein Duplikat), `GateLockData` (RequiredKeyIds[], `CanOpen(KeyInventory)`),
  `GateState` (Closed/Opening/Open). Runtime `KeyInventoryManager` (hält `KeyInventory` + Icon-Map, `OnInventoryChanged`,
  CollectKey/ConsumeKeys; auf GameManager-Objekt), `KeyPickup` (IInteractable, geht NICHT in den Carry-Stack →
  `CollectKey` + Destroy), `KeyHudView` (editor-authored Icon-Slots, nur Binding/Refresh), `GateController`
  (Proximity-Trigger, öffnet automatisch bei `CanOpen`, verbraucht Schlüssel, Slerp-Rotation um `doorPivot`, bleibt
  dauerhaft offen), `KeySpawnBinding` (spawnt Schlüssel-Prefab einmalig bei `AreaTracker.OnAreaCompleted` an
  `spawnTransform`). Runtime `AreaManager` + `AreaZone` (BoxCollider-Trigger, Player-Erkennung über
  `FirstPersonController`; Overlap zeigt mehrere HUD-Sektionen gleichzeitig, kein Stack/Fallback). Editor-Setup
  „CozySanta/Setup F8 (Schlüssel, Tore, Zones)" erstellt GameManager, Key-HUD und Prefabs (`KeyItem`, `Gate`,
  `AreaZone`). EditMode-Tests K1–K4, G1–G2, Z1–Z2 grün. Diagramme unter `specs/008-schluessel-tor-sektor/diagrams/`.

- **F9 (in Arbeit)**: Teleport (Eingang ↔ Innenraum) — Core `TeleportArbiter` (Re-Entry-Schutz
  über Belegungs-Set: belegte Trigger feuern nicht, Ziel-Trigger werden beim Teleport vorbelegt →
  kein Bounce); Runtime `TeleportRouter` (Inspector-Liste aus Paaren „Trigger-Collider → Ziel-Transform",
  Spieler-Erkennung über `FirstPersonController`, CharacterController-sicherer Versatz + optionale
  Ziel-Blickrichtung) + auto-angehängter `TeleportTriggerForwarder` (leitet `OnTriggerEnter/Exit` an den
  Router). `FirstPersonController.ResetVerticalVelocity` additiv. EditMode-Tests TP1–TP4 grün. Doku/Diagramm
  unter `specs/009-teleport-poststelle/`. Editor-Verdrahtung (Trigger/Ziele am Prefab) manuell.

- **Item-Persistenz & Ruhezustand (additiv zu F3/F7)**: Core `SettleTimer` (Decide, testbar) —
  meldet „ruhend", sobald lineare/Winkel-Geschwindigkeit lang genug unter Schwelle liegen. Runtime
  `SettlingBody` (Apply: friert das Item bei Ruhe ein → `isKinematic=true` + Controller aus, Collider
  bleibt an → aufhebbar; `BeginSettling` weckt es beim Ablegen, von `PlayerCarry.Drop` aufgerufen),
  `PrefabId` (Wiedererkennungs-Schlüssel), `ItemArea` (benannter Bereich über Collider-Bounds, z. B.
  „Bibliothek"), `ItemCatalog` (Schlüssel→Prefab), `AreaItemData`/`AreaItemStore` (JSON pro Bereich
  unter `StreamingAssets`, `items_<Bereich>.json`), `ItemPersistence` (Speichern/Laden je Bereich,
  Auto-Laden beim Start → geladene Items starten ruhend, kein Settle-Spike). Dev-Menü
  `ItemSaveDevTool` (IMGUI, „F4": Bereichsliste mit Speichern/Laden). Editor-Setup
  „CozySanta/Items/…" stattet die 96 Buch-Prefabs mit `PrefabId`+`SettlingBody` aus, schaltet ihren
  Schattenwurf ab und baut den Katalog; zweiter Befehl legt Szenen-Objekte + Bereiche aus AreaZones an.
  `AreaTracker.AreaName` additiv öffentlich. EditMode-Tests ST1–ST5 grün. Persistierte Daten: JSON je
  Bereich (Prefab-Schlüssel + Pose). Workflow: im Play-Mode spawnen/verteilen, ruhen lassen, Bereich
  speichern → beim Start wird der Haufen kinematisch geladen.
  Erweiterung (pro Gebäude): `ItemArea` trägt einen eigenen `ItemCatalog` + `maxPerVariant` (Bibliothek
  = 96 Bücher × 20); `ItemPersistence.LoadArea` nutzt den Bereichs-Katalog (Fallback global) und
  `CountByKey` zählt je Variante im Bereich. Core `SpawnQuota` (Decide, testbar, EditMode-Tests SQ1–SQ4): wählt eine
  zufällige, noch nicht gedeckelte Variante / meldet „alle voll". Runtime `AreaSpawner` („R": spawnt im
  betretenen Bereich ein erlaubtes Zufalls-Item mit zufälliger XYZ-Rotation; ersetzt das `DevSpawnMenu`-R
  in diesem Bereich). Overlay `AreaInventoryHud` („F6"): Gesamtzahl + alle Varianten x/Max + Buttons
  „Speichern" (= Start-Standard des Gebäudes) und „Reset" (alle Items des Bereichs entfernen). Editor-Setup
  weist Bibliotheks-Bereichen den Bücher-Katalog (Max 20) zu und verdrahtet Spawner/HUD.

- **Halten-zum-Wiederholen (additiv zu F3/F4)**: Core `HoldRepeatTimer` (Decide, testbar) — aus
  gedrückt/los + `deltaTime` entscheidet `Tick`, ob ausgelöst wird: einmal sofort beim Druck, dann
  nach `holdInitialDelay` im Takt `holdRepeatInterval`, solange gehalten. `PlayerInputRelay` nutzt je
  einen Timer für Linksklick (TryTake), Rechtsklick (TryPlace) und „Q" (TryDrop); `DevSpawnMenu`
  für „R" (Spawn). Delays im Inspector einstellbar. EditMode-Tests HR1–HR5 grün.

- **Spielersprung (additiv zu F2)**: Core `JumpCalculator` (Decide, testbar) — `ComputeJumpVelocity`
  (v0 = √(2·g·h)) + `StepVerticalVelocity` (Boden-Anpressdruck, Absprung, Schwerkraft-Integration);
  `FirstPersonController` um `jumpHeight` + `RequestJump()` erweitert, `PlayerInputRelay` liest die Leertaste.
  EditMode-Tests J1–J5 grün.

## Status / MVP-Fokus

Erster Sektor (Eingangsbereich + Poststelle, optional Dekorationsfabrik) als
grauer Prototyp: Schnee schmelzen, XP, einfache Skill-Upgrades, Aufnehmen/
Sortieren, Lampenfeedback, ein Schluessel, ein Tor. Erst wenn Schnee, Tragen,
Sortieren und Aufgabenfortschritt Spass machen, wird visuell ausgebaut.
Offene Punkte siehe `08_offene_fragen.md`.
