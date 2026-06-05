# Datenmodell: F8 – Schlüssel-, Tor- & Sektorfreischaltung

## Schicht-Übersicht

```
CozySanta.Core.Keys          → KeyInventory, GateState (reine C#-Logik)
CozySanta.Runtime.Keys       → KeyPickup, KeySpawnBinding, GateController, KeyHudView
CozySanta.Runtime.Areas      → AreaZone, AreaManager  (erweitert F7)
CozySanta.Runtime.UI         → KeyHudView, AreaHudView-Erweiterung
```

---

## Core-Entitäten

### KeyInventory (Core)

```
KeyInventory
├── _heldKeys : HashSet<string>
├── AddKey(id: string) : void
├── RemoveKeys(ids: IEnumerable<string>) : void
├── HasKeys(ids: IEnumerable<string>) : bool
└── GetAllKeys() : IReadOnlyCollection<string>
```

- Entscheidungslogik ohne Unity-Abhängigkeit
- `HasKeys` prüft ob **alle** übergebenen IDs im Set vorhanden sind
- Testkandidaten: AddKey, HasKeys(subset), HasKeys(vollständig), RemoveKeys

### GateState (Core-Enum)

```
GateState { Closed, Opening, Open }
```

- Zustandsübergänge: Closed → Opening (wenn HasKeys = true & Proximity) → Open
- Open ist terminal (kein Zurück)

### GateLockData (Core-Value-Object)

```
GateLockData
├── RequiredKeyIds : string[]   (1–3 Einträge, Editor-konfiguriert)
└── CanOpen(inventory: KeyInventory) : bool
```

---

## Runtime-Entitäten

### KeyPickup (Runtime, IInteractable)

```
KeyPickup : MonoBehaviour, IInteractable
├── [SerializeField] keyId : string
├── [SerializeField] keyIcon : Sprite
└── Interact() → meldet id+icon an KeyInventoryManager, zerstört sich selbst
```

- Nutzt F2-Interaktionssystem (Blick + Reichweite)
- KEIN Eintrag in PlayerCarry

### KeySpawnBinding (Runtime)

```
KeySpawnBinding : MonoBehaviour
├── [SerializeField] targetArea : AreaTracker
├── [SerializeField] keyPrefab : GameObject
└── [SerializeField] spawnTransform : Transform
```

- Abonniert `AreaTracker.OnCompleted`; spawnt `keyPrefab` an `spawnTransform` einmalig

### KeyInventoryManager (Runtime)

```
KeyInventoryManager : MonoBehaviour
├── _inventory : KeyInventory         (Core)
├── CollectKey(id, icon) : void       (Apply-Seite von AddKey)
├── ConsumeKeys(ids) : void           (Apply-Seite von RemoveKeys)
├── HasKeys(ids) : bool               (Delegation an Core)
└── OnInventoryChanged : event        (HUD abonniert)
```

### GateController (Runtime)

```
GateController : MonoBehaviour
├── [SerializeField] requiredKeyIds : string[]   (1–3)
├── [SerializeField] openDuration : float
├── _state : GateState                           (Core-Enum)
├── _lockData : GateLockData                     (Core-Value-Object)
├── OnTriggerEnter(Collider) → prüft Player + HasKeys → startet Opening
└── Apply: Öffnungs-Animation + ConsumeKeys + state = Open
```

### AreaZone (Runtime)

```
AreaZone : MonoBehaviour
├── [SerializeField] areaTracker : AreaTracker
└── [SerializeField] areaManager : AreaManager
```

- `OnTriggerEnter` → `areaManager.RegisterZone(this)`
- `OnTriggerExit` → `areaManager.UnregisterZone(this)`

### AreaManager (Runtime)

```
AreaManager : MonoBehaviour
├── _activeZones : HashSet<AreaZone>
├── RegisterZone(zone) : void
├── UnregisterZone(zone) : void
└── OnActiveZonesChanged : event       (AreaHudView abonniert)
```

- Liefert `IEnumerable<AreaTracker>` aller aktiver Zones
- Kein Stack, kein Prioritätssystem

---

## HUD-Erweiterung (UI – editor-authored)

### KeyHudView (Runtime)

```
KeyHudView : MonoBehaviour
├── [SerializeField] iconSlots : Image[]   (feste Anzahl pre-authored Slots, z. B. 5)
└── Refresh(keys: IReadOnlyCollection<string>, icons: ...) : void
```

- Editor-authored Slots; Laufzeitcode aktualisiert nur Sprite + active-Flag

### AreaHudView-Erweiterung (F7-Komponente)

Die bestehende `AreaHudView` wird erweitert:
- Bisher: zeigt eine statische Area
- Neu: abonniert `AreaManager.OnActiveZonesChanged`; zeigt eine `ScrollRect` / `VerticalLayoutGroup` mit einem Eintrag pro aktiver Area
- Editor: Prefab-Slot für `TaskEntrySection`-Prefab (bereits F7); AreaHudView klont Sections nicht zur Laufzeit – stattdessen wird eine feste Anzahl vorbereiteter Sektionen an/abgeschaltet
- **Limit**: maximal 3 gleichzeitig angezeigte Areas (entspricht Sektor-Design; höhere Zahl = Konfigurationsfehler im Level)

---

## Beziehungsdiagramm (textuell)

```
AreaTracker (F7) ←── KeySpawnBinding ──→ KeyPickup (spawnt)
                                              ↓ Interact()
                                         KeyInventoryManager
                                              ↑ HasKeys()
GateLockData ────────────────────────────────┘
GateController ──OnTriggerEnter──→ GateLockData.CanOpen() → Opening → Open
                                        ↓ ConsumeKeys()
                                   KeyInventoryManager

AreaZone ──OnTriggerEnter/Exit──→ AreaManager ──OnChanged──→ AreaHudView
```
