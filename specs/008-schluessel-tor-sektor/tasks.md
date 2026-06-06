# Tasks: F8 – Schlüssel-, Tor- & Sektorfreischaltung

**Input**: `specs/008-schluessel-tor-sektor/` (spec.md, plan.md, research.md, data-model.md)  
**Compile-Check**: `dotnet build Assembly-CSharp.csproj`  
**300-Zeilen-DoD**: alle neuen/geänderten Produktionsklassen prüfen

---

## Phase 1: Setup

**Zweck**: Verzeichnisstruktur, Diagramme anlegen, Dokumentationspfad sichern

- [x] T001 Ordner `Assets/_Project/Core/Keys/` und `Assets/_Project/Runtime/Keys/` anlegen; Assembly-Definition-Dateien um neue Namespaces ergänzen
- [x] T002 [P] Ordner `Assets/_Project/Tests/EditMode/Keys/` anlegen und in EditMode-Assembly-Definition aufnehmen
- [x] T003 [P] PlantUML-Diagramm anlegen: `specs/008-schluessel-tor-sektor/diagrams/activity-schluessel-pickup.puml`
- [x] T004 [P] PlantUML-Diagramm anlegen: `specs/008-schluessel-tor-sektor/diagrams/state-gate.puml`
- [x] T005 [P] PlantUML-Diagramm anlegen: `specs/008-schluessel-tor-sektor/diagrams/sequence-areazone-wechsel.puml`
- [x] T006 [P] PlantUML-Diagramm anlegen: `specs/008-schluessel-tor-sektor/diagrams/class-domain-f8.puml`

---

## Phase 2: Fundament (blockiert alle User Stories)

**Zweck**: Core-Typen, die alle Stories gemeinsam nutzen

**Checkpoint**: Core kompiliert; EditMode-Testdateien skelettiert und rot

- [x] T007 Core-Enum `GateState` (Closed/Opening/Open) anlegen in `Assets/_Project/Core/Keys/GateState.cs`
- [x] T008 [P] Core-Klasse `KeyInventory` anlegen in `Assets/_Project/Core/Keys/KeyInventory.cs` (AddKey, RemoveKeys, HasKeys, GetAllKeys; HashSet<string>)
- [x] T009 [P] Core-Klasse `GateLockData` anlegen in `Assets/_Project/Core/Keys/GateLockData.cs` (RequiredKeyIds[], CanOpen(KeyInventory))
- [x] T010 Compile-Check: `dotnet build Assembly-CSharp.csproj` – null Fehler
- [x] T011 Testskelett anlegen in `Assets/_Project/Tests/EditMode/Keys/KeyInventoryTests.cs` (K1–K4 als leere Tests, alle rot)
- [x] T012 [P] Testskelett anlegen in `Assets/_Project/Tests/EditMode/Keys/GateLockDataTests.cs` (G1–G2 als leere Tests, alle rot)

---

## Phase 3: User Story 1 – Schlüssel aufheben & im HUD sehen (Priorität: P1) 🎯 MVP

**Ziel**: Spieler hebt physischen Schlüssel auf; Icon erscheint im HUD; Carry-Stack unberührt

**Unabhängiger Test**: Schlüssel-Prefab in Testszene platzieren, interagieren → Icon im HUD, Stack unverändert

### Tests für US1 (vor Implementierung schreiben, rot lassen)

- [x] T013 [US1] Unit-Tests K1–K4 in `Assets/_Project/Tests/EditMode/Keys/KeyInventoryTests.cs` ausformulieren und sicherstellen, dass sie rot sind (TDD)

### Implementierung US1

- [x] T014 [US1] PlantUML `activity-schluessel-pickup.puml` fertigstellen (Pickup-Entscheidungsbaum + Testkandidaten markieren)
- [x] T015 [US1] Runtime `KeyInventoryManager.cs` anlegen in `Assets/_Project/Runtime/Keys/` (hält `KeyInventory`; CollectKey, ConsumeKeys, HasKeys; `OnInventoryChanged`-Event)
- [x] T016 [US1] Runtime `KeyPickup.cs` anlegen in `Assets/_Project/Runtime/Keys/` (IInteractable; `[SerializeField] keyId`, `keyIcon`; Interact() → KeyInventoryManager.CollectKey, Destroy(gameObject))
- [x] T017 [US1] Editor-authored `KeyHudView.cs` anlegen in `Assets/_Project/Runtime/Keys/` (bindet `Image[]`-Slots; abonniert `KeyInventoryManager.OnInventoryChanged`; zeigt Icons an/aus)
- [x] T018 [US1] Key-Prefab im Editor erstellen (Mesh + Collider + KeyPickup-Component + Interaktion-Hint-Label); unter `Assets/_Project/Prefabs/Keys/` speichern
- [x] T019 [US1] HUD-Slot-Panel im Editor erstellen (max. 5 Icon-Slots als Image-GameObjects, editor-authored; KeyHudView binden)
- [x] T020 [US1] K1–K4 grün laufen lassen; Testergebnis in `specs/008-schluessel-tor-sektor/tasks.md` notieren
- [x] T021 [US1] Compile-Check + 300-Zeilen-Check für alle US1-Klassen

**Checkpoint**: Schlüssel aufhebbar, Icon im HUD sichtbar, K1–K4 grün ✓

---

## Phase 4: User Story 2 – Tor automatisch öffnen (Priorität: P2)

**Ziel**: Spieler nähert sich Tor mit passenden Schlüsseln → Tor öffnet automatisch, Keys verbraucht

**Unabhängiger Test**: GateController-Prefab mit `requiredKeyIds: ["post"]`; KeyInventoryManager mit "post" füllen; Spieler betritt Trigger → Tor öffnet, Icon weg

### Tests für US2 (vor Implementierung schreiben, rot lassen)

- [x] T022 [US2] Unit-Tests G1–G2 in `Assets/_Project/Tests/EditMode/Keys/GateLockDataTests.cs` ausformulieren und rot lassen

### Implementierung US2

- [x] T023 [US2] PlantUML `state-gate.puml` fertigstellen (Closed→Opening→Open; Testkandidaten markieren)
- [x] T024 [US2] Runtime `GateController.cs` anlegen in `Assets/_Project/Runtime/Keys/` (`[SerializeField] string[] requiredKeyIds`; `[SerializeField] float openDuration`; OnTriggerEnter → GateLockData.CanOpen → Coroutine Animation → ConsumeKeys; state = Open; Tor bleibt dauerhaft offen)
- [x] T025 [US2] Gate-Prefab im Editor erstellen (Tor-Mesh + Hinge/Slide-Animation-Platzhalter + BoxCollider isTrigger als Proximity-Zone + GateController-Component); unter `Assets/_Project/Prefabs/Gates/` speichern
- [x] T026 [US2] Runtime `KeySpawnBinding.cs` anlegen in `Assets/_Project/Runtime/Keys/` (`[SerializeField] AreaTracker targetArea`; `[SerializeField] GameObject keyPrefab`; `[SerializeField] Transform spawnTransform`; abonniert OnCompleted einmalig → Instantiate)
- [x] T027 [US2] G1–G2 grün laufen lassen; Testergebnis notieren
- [x] T028 [US2] Compile-Check + 300-Zeilen-Check für alle US2-Klassen

**Checkpoint**: Tor öffnet bei korrekten Schlüsseln, Keys verbraucht, G1–G2 grün ✓

---

## Phase 5: User Story 3 – Area-Zone: HUD-Wechsel & Overlap (Priorität: P3)

**Ziel**: BoxCollider-Trigger als Area-Zone im Editor; HUD zeigt aktive Area(s); Overlap = beide sichtbar; keine aktive Zone = keine Anzeige

**Unabhängiger Test**: Zwei AreaZone-GameObjects im Editor konfigurieren; Spieler bewegen → HUD wechselt korrekt; Overlap → beide Listen; keine Zone → leer

### Tests für US3 (vor Implementierung schreiben, rot lassen)

- [x] T029 [US3] Unit-Tests Z1–Z2 in `Assets/_Project/Tests/EditMode/Keys/AreaManagerTests.cs` ausformulieren und rot lassen (AreaManager mit Mock-Zones testen, ohne Unity-Laufzeit soweit möglich)

### Implementierung US3

- [x] T030 [US3] PlantUML `sequence-areazone-wechsel.puml` fertigstellen (OnTriggerEnter/Exit → AreaManager → AreaHudView; Testkandidaten markieren)
- [x] T031 [US3] [P] PlantUML `class-domain-f8.puml` fertigstellen (alle F8-Entitäten + Beziehungen)
- [x] T032 [US3] Runtime `AreaManager.cs` anlegen in `Assets/_Project/Runtime/Areas/` (HashSet<AreaZone> _activeZones; RegisterZone; UnregisterZone; `OnActiveZonesChanged`-Event; liefert IEnumerable<AreaTracker>)
- [x] T033 [US3] Runtime `AreaZone.cs` anlegen in `Assets/_Project/Runtime/Areas/` (`[SerializeField] AreaTracker areaTracker`; `[SerializeField] AreaManager areaManager`; OnTriggerEnter/Exit → Register/Unregister; erfordert BoxCollider isTrigger)
- [x] T034 [US3] `AreaHudView` (F7) erweitern: abonniert `AreaManager.OnActiveZonesChanged`; zeigt bis zu 3 editor-authored Task-Sektionen an/aus (keine Laufzeit-Instanziierung); leere Aktiv-Menge → alle Sektionen aus
- [x] T035 [US3] Editor-Setup: AreaZone-Prefab anlegen (leerer GameObject + BoxCollider isTrigger + AreaZone-Component); unter `Assets/_Project/Prefabs/Areas/` speichern
- [x] T036 [US3] Z1–Z2 grün laufen lassen; Testergebnis notieren
- [x] T037 [US3] Compile-Check + 300-Zeilen-Check für alle US3-Klassen

**Checkpoint**: Area-Zone-Wechsel funktioniert, Overlap korrekt, Z1–Z2 grün ✓

---

## Phase 6: Dokumentations-Sync & Merge-Bereitschaft

- [x] T038 CLAUDE.md aktualisieren: F8-Abschnitt mit allen neuen Komponenten und Testnachweis (K1–K4, G1–G2, Z1–Z2 grün)
- [x] T039 [P] `05_systeme.md` erweitern: KeyInventory-Abschnitt (Regeln, Felder, Events)
- [x] T040 [P] `04_welt_und_narrative.md` prüfen/ergänzen: Tor-/Schlüsselmechanik konsistent mit Implementierung
- [x] T041 [P] `06_content_progression.md` prüfen/ergänzen: Freischaltlogik Schlüssel→Tore aktuell
- [x] T042 Diagramm-Abdeckungsmatrix: jeden geänderten Code-Bereich auf Diagramm + Test gemappt
- [x] T043 Dokumentations-Split-Analyse abschließen (05_systeme.md: kein Split nötig, dokumentieren)
- [x] T044 Validieren: keine Laufzeit-generierten UI-Elemente eingeführt
- [x] T045 Finaler Compile-Check + alle Tests grün + Diagramme vorhanden und kein Platzhalter mehr

---

## Abhängigkeiten & Ausführungsreihenfolge

- **Phase 1** (Setup): sofort startbar
- **Phase 2** (Fundament): nach Phase 1 – blockiert alle User Stories
- **Phase 3–5** (User Stories): nach Phase 2; in Prioritätsreihenfolge (P1 → P2 → P3)
- **Phase 6** (Merge): nach Phase 3–5 komplett

## Testergebnisse (nach Implementierung ausfüllen)

| Test-ID | Status | Datum |
|---------|--------|-------|
| K1 | ✅ grün | 2026-06-04 |
| K2 | ✅ grün | 2026-06-04 |
| K3 | ✅ grün | 2026-06-04 |
| K4 | ✅ grün | 2026-06-04 |
| G1 | ✅ grün | 2026-06-04 |
| G2 | ✅ grün | 2026-06-04 |
| Z1 | ✅ grün | 2026-06-04 |
| Z2 | ✅ grün | 2026-06-04 |

## Abnahme & Merge-Bereitschaft (2026-06-06)

- Code vollständig, Compile-Check `dotnet build cozychristmas.sln` ohne Fehler.
- EditMode-Tests K1–K4, G1–G2, Z1–Z2 grün.
- Manuelle Abnahme: Schlüssel-Spawn bei Area-Abschluss verifiziert (Spawn-Kette
  `AreaTracker.OnAreaCompleted → KeySpawnBinding → Instantiate` per Log bestätigt);
  Schlüssel→Tor-Interaktion zuvor getestet (Tor öffnet, Schlüssel verbraucht).
- PlantUML-Diagramme vorhanden: `state-gate`, `activity-schluessel-pickup`,
  `sequence-areazone-wechsel`, `class-domain-f8` (jeweils mit Testkandidaten annotiert).
- CLAUDE.md F8-Abschnitt ergänzt; Konzeptdoku `05_systeme.md` deckt Schlüssel-/Tor-System ab.
- Temporäre F8-Debug-Logs entfernt.
