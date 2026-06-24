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
  Trage-Sperre (additiv): `TeleportRouter` hält eine `PlayerCarry`-Referenz (Auto-Suche beim Start);
  solange der Spieler etwas trägt (`CarriedCount > 0`), wird in `HandleEnter` NICHT teleportiert –
  Gebäude lassen sich weder verlassen noch betreten, während man Objekte hält (Arbiter bleibt
  unberührt → nach dem Ablegen löst erneutes Betreten den Teleport regulär aus).

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
  Gebäude-Parenting (additiv, Leistung): `ItemArea` trägt optional einen `ItemParent` (Gebäude-Root,
  den der `AreaActivator` beim Verlassen per `SetActive(false)` deaktiviert). `ItemPersistence.LoadArea`
  und `AreaSpawner.TrySpawn` hängen geladene/gespawnte Items unter `area.ItemParent` (Fallback: globaler
  `spawnParent`) → die Items eines Gebäudes werden mit dem Gebäude mit-deaktiviert (kein Rendern/Culling/
  Broadphase außerhalb). Speichern/Reset/Spawn nur im aktiven (betretenen) Gebäude nutzen, da
  `FindObjectsByType<PrefabId>` inaktive Objekte ausschließt.

- **Halten-zum-Wiederholen (additiv zu F3/F4)**: Core `HoldRepeatTimer` (Decide, testbar) — aus
  gedrückt/los + `deltaTime` entscheidet `Tick`, ob ausgelöst wird: einmal sofort beim Druck, dann
  nach `holdInitialDelay` im Takt `holdRepeatInterval`, solange gehalten. `PlayerInputRelay` nutzt je
  einen Timer für Linksklick (TryTake), Rechtsklick (TryPlace) und „Q" (TryDrop); `DevSpawnMenu`
  für „R" (Spawn). Delays im Inspector einstellbar. EditMode-Tests HR1–HR5 grün.

- **Spielersprung (additiv zu F2)**: Core `JumpCalculator` (Decide, testbar) — `ComputeJumpVelocity`
  (v0 = √(2·g·h)) + `StepVerticalVelocity` (Boden-Anpressdruck, Absprung, Schwerkraft-Integration);
  `FirstPersonController` um `jumpHeight` + `RequestJump()` erweitert, `PlayerInputRelay` liest die Leertaste.
  EditMode-Tests J1–J5 grün.

- **Lampen-Optik (additiv zu F5)**: Core `LampVisualMath` (Decide, testbar) — reine Mathe für die Optik:
  `Pulse` (Atmen 0..1), `Warmth` (Warm↔Kalt-Smoothstep aus Akku), `TargetLevel` (akku-skalierter Ziel-Pegel
  idle/aktiv), `SmoothTowards` (frame-rate-unabhängige Glättung). Runtime `LampVisuals` (Apply): liest
  `MeltController.BatteryFraction` + neues `MeltController.IsMelting` und setzt live Emission des Funke-Renderers
  (MaterialPropertyBlock `_EmissionColor`), Kern-/Kegel-Lichtstärke und Funkel-Partikelrate — voll = warmes Gold
  & hell, leer = kühles Blau & matt, beim Schmelzen heller + mehr Funken, sanftes Pulsieren. Editor-Setup
  „CozySanta/Setup Lampe (Material, Lichter, Partikel)" erzeugt Materialien (`M_LampGehaeuse`, `M_LampFunke` mit
  HDR-Emission), baut ein Rig unter der Kamera (Platzhalter-Funke + Point/Spot-Light + Partikel) und verdrahtet
  `LampVisuals` mit dem `MeltController`. KI-Mesh wird später unter „Lampe" gehängt, Funke-Renderer auf das echte
  Kugel-Mesh umgesetzt. Bloom = manueller URP-2-Klick-Schritt (HDR-Emission liegt > 1). EditMode-Tests LV1–LV5 grün.

- **Bäckerei-Sortierobjekte (additiv zu F3/F4 + Item-Persistenz)**: Die 20 Süßigkeiten-Prefabs unter
  `Prefabs/Süßigkeiten` (8 Zuckerstangen, 8 Lebkuchen, 4 Kekse) werden zu Sortierobjekten gemacht.
  Editor-Setup `CozySanta/Bäckerei/Süßigkeiten als Sortierobjekte einrichten (Prefabs + Katalog)`
  (`BakerySweetItemSetup`) gibt jedem Prefab `Rigidbody` (Masse 1), `PickupInteractable`, `Sortable`
  mit SortKey `[Art, Farbe]` (z. B. `[Zuckerstange, Rot]`; zweifarbig `pink_grün` → ein Farbwert
  `Pink_Grün`), einen lokal-raumkorrekt gefitteten `BoxCollider`, `PrefabId` + `SettlingBody`, schaltet
  den Schattenwurf ab und baut den `SuessigkeitenCatalog.asset` (für die Bäckerei-`ItemArea`/Spawn).
  Zweiter Befehl `CozySanta/Bäckerei/Fächer & Crates belegen (Szene)` (`BakerySortAssignmentSetup`)
  liest die SortKeys aus den Prefabs und schreibt sie in die `SortTargetInteractable`-Fächer unter
  `BäckereiInnen`: die ersten 4 Warenregale → Zuckerstangen, die anderen 4 → Lebkuchen (8 Farben auf
  je 16 Fächer = jede Farbe doppelt), Crates → je eine Keks-Variante (1:1). Soll-Mengen/Raster bleiben
  unangetastet (bereits eingestellt: Regal 7×3=21, Crate 5×5×3=75). Editor-Authoring-Tools (keine neue
  Core-Fachlogik → dokumentierte Nicht-Unit-Ausnahme analog `BookPrefabSetup`).
  Variantenspezifische Spawn-Höchstzahlen (additiv): `ItemCatalog.Entry` trägt optional `maxPerVariant`
  (0 = Bereichs-Default der `ItemArea`); `ItemCatalog.MaxByKey()` baut die Übersteuerungs-Map. Core
  `SpawnQuota.TryPick`/`IsFull` haben Overloads mit `maxByKey` + `defaultMax` (alte Signaturen delegieren,
  bleiben grün). `AreaSpawner` + `AreaInventoryHud` nutzen die Map. `BakerySweetItemSetup` setzt im Katalog
  Kekse = 75, Zuckerstangen/Lebkuchen = 42 (2×21) je Variante. EditMode-Tests SQ5–SQ7 ergänzt.
  Einlage-Dreh-Offset pro Item (additiv): Runtime `SortPlacementRotation` (Euler-Offset) am Item; das Fach
  wendet ihn in `CellRotation(itemOffset)` GLEICH auf Ghost (`TryGetGhostCellPose`) und tatsächliche Einlage
  (`PlaceVisual`) an – so liegt ein „schief" orientiertes Mesh (Zuckerstangen, Kekse) in JEDEM Fach korrekt,
  ohne die Fächer item-spezifisch zu machen (jedes Item bleibt in jedes Fach legbar). `BakerySweetItemSetup`
  hängt die Komponente an alle Süßigkeiten (Euler je Art: `ZuckerstangePlacedEuler`/`KeksPlacedEuler`,
  Lebkuchen 0). Edit-Mode-Gizmo (grau = Grund, grün = mit Offset) + Dev-Tool `SortPlacementRotationDevTool`
  (Tasten I/K J/L U/O drehen den Offset des GETRAGENEN Items live im Fach-Ghost, P = loggen) zum Ermitteln
  der Winkel; gefundene Werte in die `…PlacedEuler`-Konstanten eintragen und den Einricht-Befehl erneut ausführen.
  Einlege-Sperre nach Art (additiv, opt-in): Core `SortPlacementRule.IsPlaceable(SortKey, allowedArts)` (leer =
  jedes Item einlegbar → Standardverhalten überall sonst unberührt). `SortTargetInteractable` hat ein Feld
  `placeableArts`; `PlaceInColumn` und `TryGetGhostCellPose` sperren das Einlegen UND den Ghost, wenn die Art
  (erste Facette) nicht gelistet ist (acceptedFacets/korrekt-falsch bleibt unberührt). `BakerySortAssignmentSetup`
  setzt es (Variante A): Crates = `["Keks"]`, Warenregale = `["Zuckerstange","Lebkuchen"]` → Kekse nur in Crates,
  Zuckerstangen/Lebkuchen nur in den Regalen (untereinander im Regal weiter vertauschbar). EditMode-Tests SP1–SP4.

- **F10 (Core/Runtime grün, Unity-Compile maßgeblich)**: Instanziertes Rendern ruhender Items —
  Performance-Fix gegen den Playtest-FPS-Einbruch (geladene Item-Massen = ~14k der ~18k Draw Calls,
  jedes Item ein eigener `MeshRenderer`; URP-SRP-Batcher fasst die Anzahl nicht zusammen). Da die Items
  Duplikate sind, kollabiert `Graphics.RenderMeshInstanced` jede (Mesh, Material)-Gruppe auf einen Draw.
  Core `InstanceSlots` (dichte Slot-Verwaltung, Swap-with-last, `ChunkRanges` für die 1023er-Grenze;
  rein, EditMode-Tests IS1–IS5). Runtime `InstancedItemRenderer` (partial `…cs`/`…Draw.cs`, je Gebäude
  auf `ItemArea.ItemParent`): cacht beim Ruhen die Weltmatrizen je (Mesh, Submesh, Material, Schatten,
  Layer)-Gruppe, schaltet den Einzel-Renderer ab (Collider bleibt → aufhebbar) und zeichnet je Gruppe je
  Chunk; Kompaktierung räumt zerstörte Items (Reset/ClearArea). Andockpunkte additiv: `SettlingBody.EnterRest`
  → `Register`, `PlayerCarry.TryPickup` → `Unregister`; der `AreaActivator` deaktiviert den Root beim
  Verlassen → Draws laufen nur im betretenen Gebäude. Editor-Setup
  „CozySanta/Performance/Instanced Item Rendering einrichten". In Fächer einsortierte Items + bewegte
  Gebäude-Roots: Out-of-Scope v1. Doku/Diagramm unter `specs/010-instanced-item-rendering/`.
  Begleitende Perf-Tools (Editor): „CozySanta/Performance/GPU-Instancing auf allen Materialien aktivieren"
  und „Occlusion 1/2" (statische Geometrie markieren + Culling backen).

- **Einlage-Justage erweitert (additiv zu Sortieren)**: `SortPlacementRotation` trägt zusätzlich
  `PlacedScale` (Größenfaktor pro Item, multipliziert auf den Fach-Wert) und `PlacedOffset`
  (Positions-Offset, v. a. Höhe) — fürs Hinlegen von Items, die in der Höhe nicht ins Fach passen.
  `SortTargetInteractable` wendet beides identisch auf Ghost + Einlage an (`ItemPlacementScale`/
  `ItemPlacementPositionOffset`, `CombinedScale`). `SortPlacementRotationDevTool` erweitert: `,`/`.` =
  Größe, Bild↑/↓ = Höhe (zusätzlich zu I/K J/L U/O = drehen). `DekohalleSortItemSetup` reduziert auf
  „macht Katalog-Items drehbar" (hängt `SortPlacementRotation` an + ein DevTool in die Szene).

- **Dekohalle-Fächer-Belegung (Editor, Szene)**: `DekohalleSortAssignmentSetup`
  („CozySanta/Dekohalle/Fächer in Regalen belegen") weist den 18 `gabinet`-Regalen unter `DekoInnen`
  der Reihe nach je eine Deko-Variante aus dem Katalog zu und vereinheitlicht alle Fächer eines Regals
  auf diese SortKey (liest die Facetten aus den Prefabs; nur `acceptedFacets`, Raster/Mengen unberührt).

- **Nachthimmel (additiv, Optik)**: prozeduraler Skybox-Shader `CozySanta/NightSky` (Nacht-Verlauf,
  zweilagiger Sternenhimmel mit Funkeln + Glüh-Halo, Vollmond mit Halo/Kratern, dezente Milchstraße —
  texturfrei). Editor-Setup „CozySanta/Umgebung/Nachthimmel einrichten" erzeugt `M_NightSky`, setzt es
  als Skybox, stellt Nacht-Ambient ein, richtet das Directional Light als Mondlicht aus und setzt die
  Kamera auf Background-Type Skybox. Reine Optik (dokumentierte Nicht-Unit-Ausnahme analog SnowMelt).

- **Hocke (additiv zu F2)**: Core `CrouchMotion` (Decide, testbar) — `StepHeight` (sanftes Anfahren
  Steh-/Hockhöhe), `CenterY` (Füße bleiben am Boden) und `EyeHeight` (Kamera senkt mit).
  `FirstPersonController` um `SetCrouch(bool)` + `ApplyCrouch` (CharacterController-Höhe/-Mitte + Kamera)
  erweitert, `PlayerInputRelay` liest Shift gehalten. EditMode-Tests CR1–CR5.

## Status / MVP-Fokus

Erster Sektor (Eingangsbereich + Poststelle, optional Dekorationsfabrik) als
grauer Prototyp: Schnee schmelzen, XP, einfache Skill-Upgrades, Aufnehmen/
Sortieren, Lampenfeedback, ein Schluessel, ein Tor. Erst wenn Schnee, Tragen,
Sortieren und Aufgabenfortschritt Spass machen, wird visuell ausgebaut.
Offene Punkte siehe `08_offene_fragen.md`.
