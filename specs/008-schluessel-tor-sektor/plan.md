# Implementierungsplan: F8 – Schlüssel-, Tor- & Sektorfreischaltung

**Branch**: `008-schluessel-tor-sektor` | **Datum**: 2026-06-04 | **Spec**: [spec.md](spec.md)

## Zusammenfassung

F8 schließt den MVP-Loop für Sektor 1: Gebäude-Abschluss spawnt einen physischen Schlüssel,
der Spieler hebt ihn auf (F2-Interaktionssystem), sieht ihn als Icon im HUD, und nähert sich
einem Tor → automatische Öffnung bei richtigen Schlüsseln. Parallel erweitert F8 das
Area-HUD-System (F7) um räumliche Area-Zones (BoxCollider-Trigger), damit das HUD dynamisch
die Aufgabenliste der aktuellen Area(s) anzeigt.

**Änderungsklassifikation**: Geplantes Feature via Spec-Kit-Wasserfall  
**Dokumentationsauswirkung**: CLAUDE.md, `04_welt_und_narrative.md`, `05_systeme.md`, `06_content_progression.md`  
**Diagrammauswirkung**: 4 PlantUML-Diagramme (Activity, State, Sequence, Class/Domain)  
**Arbeitssprache**: Deutsch (verpflichtend)

## Technischer Kontext

**Sprache/Version**: C# 9, Unity 6000.2.7f2 (Unity 6.2), URP 17.2  
**Primäre Abhängigkeiten**: Unity Input System 1.14.2, Test Framework 1.6  
**Storage**: Nicht persistent in F8 (Schlüsselzustand nur Session-Daten; F14 kümmert sich um Persistenz)  
**Testing**: Unity EditMode-Tests (schnelle Regelprüfung), PlayMode gezielt für Gate-Proximity  
**Zielplattform**: PC (Windows/Linux/macOS)  
**Performance-Ziele**: Gate-Öffnung < 0,5 s nach Trigger; HUD-Update < 1 Frame Verzögerung  
**Constraints**: Klassen ≤ 300 Zeilen; Core ohne UnityEngine-Abhängigkeiten  
**Integration**: F2 IInteractable, F3 PlayerCarry (nicht genutzt, aber Kollisionsprüfung nötig), F4 onCompleted, F7 AreaTracker / AreaHudView

## Constitution-Check

- [x] Änderungspfad klassifiziert: geplantes Feature via Spec-Kit-Wasserfall
- [x] Dokumentationsabdeckung definiert für alle betroffenen Bereiche (CLAUDE.md, 04/05/06)
- [x] Gesamtsystem-Impact und Kollisionsanalyse dokumentiert (spec.md Systemkontext)
- [x] PlantUML-Diagrammauswahl abgeschlossen: Activity, State, Sequence, Class/Domain
- [x] Definitionen/Anforderungen dort aktualisiert, wo Kollisionen bestehen (AreaHudView F7-Erweiterung)
- [x] Compile-Validierungsbefehl definiert: `dotnet build Assembly-CSharp.csproj`
- [x] Architekturtrennung geplant: Core `KeyInventory` + `GateLockData` ohne UnityEngine; Runtime übernimmt Apply
- [x] Decide/Apply-Trennung für Gate-Flow dokumentiert (GateLockData.CanOpen → GateController.Apply)
- [x] Teststrategie definiert: EditMode K1–K4, G1–G2, Z1–Z2; PlayMode für Gate-Proximity optional
- [x] Diagram-derived Testkandidaten dokumentiert (data-model.md Testkandidaten-Spalten)
- [x] DoD-Baseline: Compile-Check, 300-Zeilen-Check, Unit-Tests für Fachregeln
- [x] UI-Ansatz bestätigt: editor-authored Slots, kein Laufzeit-UI-Generieren
- [x] Dokumentations-Split-Analyse: 05_systeme.md erhält neuen Abschnitt, kein Split nötig
- [x] Diagramm-Sync für PR-Review geplant
- [x] Dokumentation und Kommunikation auf Deutsch

## Projektstruktur

### Dokumentation (dieses Feature)

```
specs/008-schluessel-tor-sektor/
├── spec.md
├── plan.md             ← diese Datei
├── research.md
├── data-model.md
├── tasks.md
├── checklists/
│   └── requirements.md
└── diagrams/
    ├── activity-schluessel-pickup.puml
    ├── state-gate.puml
    ├── sequence-areazone-wechsel.puml
    └── class-domain-f8.puml
```

### Quellcode (Repository-Root)

```
Assets/_Project/
├── Core/
│   └── Keys/
│       ├── KeyInventory.cs          (HashSet, AddKey, RemoveKeys, HasKeys)
│       ├── GateState.cs             (enum: Closed, Opening, Open)
│       └── GateLockData.cs          (RequiredKeyIds[], CanOpen(KeyInventory))
│
└── Runtime/
    ├── Keys/
    │   ├── KeyPickup.cs             (IInteractable, meldet an KeyInventoryManager)
    │   ├── KeySpawnBinding.cs       (abonniert AreaTracker.OnCompleted, spawnt Prefab)
    │   ├── KeyInventoryManager.cs   (Apply-Seite: CollectKey, ConsumeKeys, Event)
    │   ├── GateController.cs        (OnTriggerEnter, Animation, Apply)
    │   └── KeyHudView.cs            (Icon-Slots binden)
    └── Areas/
        ├── AreaZone.cs              (BoxCollider-Trigger, Register/Unregister)
        └── AreaManager.cs           (HashSet<AreaZone>, OnActiveZonesChanged)

Assets/_Project/Tests/EditMode/
└── Keys/
    ├── KeyInventoryTests.cs         (K1–K4)
    ├── GateLockDataTests.cs         (G1–G2)
    └── AreaManagerTests.cs          (Z1–Z2)
```

### Assembly Definitions

- `CozySanta.Core` bekommt `Keys/`-Namespace (noEngineReferences: true)
- `CozySanta.Runtime` bekommt `Keys/` und `Areas/`-Namespaces
- `CozySanta.Tests.EditMode` referenziert Core für K1–K4, G1–G2, Z1–Z2 (soweit testbar ohne Unity)

## Decide/Apply-Trennung (Gate-Flow)

```
Decide (Core):
  GateLockData.CanOpen(inventory) → bool

Apply (Runtime):
  GateController.OnTriggerEnter
    → prüft Player-Tag + GateLockData.CanOpen(KeyInventoryManager.Inventory)
    → falls true: state = Opening, startet Coroutine (Animation)
    → nach Animation: state = Open, KeyInventoryManager.ConsumeKeys(requiredKeyIds)
```

## Teststrategie

| Test-ID | Typ | Regel |
|---------|-----|-------|
| K1 | EditMode | `KeyInventory.AddKey("a")` → `HasKeys(["a"])` = true |
| K2 | EditMode | `HasKeys(["a","b"])` wenn nur "a" vorhanden → false |
| K3 | EditMode | `HasKeys(["a","b"])` wenn beide vorhanden → true |
| K4 | EditMode | `RemoveKeys(["a"])` → "a" nicht mehr in Inventory |
| G1 | EditMode | `GateLockData(["a","b"]).CanOpen(inventory mit nur "a")` → false |
| G2 | EditMode | `GateLockData(["a","b"]).CanOpen(inventory mit "a"+"b")` → true |
| Z1 | EditMode | `AreaManager` mit 2 registrierten Zones → `ActiveTrackers` enthält beide |
| Z2 | EditMode | Nach `UnregisterZone(zoneA)` → nur Zone B aktiv |

## Komplexitäts-Tracking

Keine Constitution-Verletzungen. Alle Klassen bleiben deutlich unter 300 Zeilen.
