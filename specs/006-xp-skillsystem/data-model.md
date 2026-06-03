# Phase 1 – Data Model & Contracts: F6

> „Contracts" = die C#-Typen/Signaturen. F6 erweitert F2–F5 additiv. Abhängigkeit strikt Runtime → Core.

## Core (`CozySanta.Core.Progression`) – reine, testbare Logik

### `XpLedger`

- `void Add(int xp)` – nur positive Beträge; summiert auf das gemeinsame Konto.
- `int TotalXp`, `int Level`, `int XpIntoLevel`, `int XpForNextLevel`.
- `int EarnedSkillPoints` – aus dem Level abgeleitet (z. B. `pointsPerLevel · Level`).
- Level-Kurve: `XpForLevel(int level)` (Platzhalter, monoton steigend). Mehrfacher Level-up bei großer Gutschrift.
- Invariante: `Level` konsistent mit `TotalXp`; XP nie negativ.

### `SkillId` (enum)

- `LampPower`, `LampCone`, `LampBattery`, `CarryCapacity`, `MoveSpeed`, `SortVision`, `ObjectPull` (Freischalt: `SortVision`, `ObjectPull`).

### `Skill`

- `SkillId Id`, `int Level (0..MaxLevel)`, `int MaxLevel`, `bool Unlockable`, `bool Unlocked`.
- `float Value` – `baseValue + step·Level`, optional gedeckelt (`maxValue`).
- `bool CanRaise` – `Level < MaxLevel`.
- `void Raise()` – `Level++` (clamp Max); erste Erhöhung setzt `Unlocked = true` bei Freischalt-Skills.

### `SkillSet`

- Hält alle `Skill` (per `SkillId`). `Skill Get(SkillId id)`, `float ValueOf(SkillId id)`, `int SpentPoints`.
- `bool CanInvest(SkillId id, int availablePoints)` – `availablePoints > 0 && Get(id).CanRaise`.
- `bool TryInvest(SkillId id, int availablePoints)` – wenn erlaubt: `Raise()`, `SpentPoints++`, `true`.

### `ProgressionState`

- Bündelt `XpLedger` + `SkillSet`. `int AvailablePoints => XpLedger.EarnedSkillPoints − SkillSet.SpentPoints`.
- `void AwardXp(int amount)` → `XpLedger.Add`. `bool Invest(SkillId id)` → `SkillSet.TryInvest(id, AvailablePoints)`.
- Invariante: `AvailablePoints >= 0`; ausgegebene Punkte nie > verdiente.

## Runtime (`CozySanta.Runtime.Progression`)

### `PlayerProgression : MonoBehaviour`

- Hält `ProgressionState` (mit konfigurierten Skill-Parametern aus SerializeFields/Config).
- `[SerializeField] PlayerCarry carry; MeltController melt; FirstPersonController move;` (Apply-Ziele).
- `void AwardXp(int amount)` / `void AwardSortXp()` / `void AwardMeltXp(float coverageDelta)` – XP-Anbindung.
- `bool Invest(SkillId id)` → `ProgressionState.Invest` + `ApplySkills()`.
- `ApplySkills()` (Apply): setzt `carry.Capacity = ValueOf(CarryCapacity)`; `melt`-Werte (Akku/Power/Kegel) aus den Lampen-Skills; Bewegungsgeschwindigkeit aus `MoveSpeed`.
- XP-Anbindung: abonniert `SortTargetInteractable.onCompleted` (→ `AwardSortXp`); liest `SnowPatch.Coverage`-Delta (→ `AwardMeltXp`). Andockpunkte für Area/Schlüssel/Kugel als Methoden.
- Eingabe „SkillMenu": öffnet das editor-authored Menü (bind/aktualisieren); kein Runtime-UI-Erzeugen.

### `SkillMenuDevTool : MonoBehaviour` (optional, IMGUI)

- Debug-Overlay (analog `DevSpawnMenu`): zeigt Level/XP/verfügbare Punkte, Buttons je `SkillId` zum Investieren, Taste zum XP-Gutschreiben. Nur Test-Hilfe, kein authored Gameplay-UI.

## Andockpunkte an bestehende Systeme (unverändert/additiv)

- F3 `PlayerCarry.Capacity` (Setter existiert) – Tragkraft.
- F5 `MeltController` – Akku-Kapazität + Schmelzstärke/-radius (ggf. kleine öffentliche Setter additiv).
- F2-Bewegungs-Controller – Laufgeschwindigkeit (ggf. öffentliches Speed-Property additiv).
- F4 `SortTargetInteractable.onCompleted` – XP-Quelle Sortieren.

## Abhängigkeitsrichtung

```
Runtime ──▶ Core (XpLedger, Skill, SkillSet, ProgressionState). Core kennt kein UnityEngine.
```

## Abgeleitete Testkandidaten

| ID | Testfall | Art | Quelle |
| --- | --- | --- | --- |
| X1 | XP unter Schwelle → Level/Punkte unverändert | EditMode | Activity/State |
| X2 | XP überschreitet Schwelle → Level +1, Punkte vergeben | EditMode | Activity |
| X3 | Sehr viel XP → mehrere Level auf einmal, korrekte Punktsumme | EditMode | Edge Case |
| X4 | XP 0/negativ → kein Effekt | EditMode | Edge Case |
| S1 | Invest mit verfügbarem Punkt → Stufe +1, Punkt verbraucht | EditMode | Activity |
| S2 | Invest ohne Punkte → keine Änderung | EditMode | Activity-Ablehnung |
| S3 | Invest auf Maximalstufe → keine Änderung | EditMode | State-Grenze |
| S4 | Freischalt-Skill: erste Investition → `Unlocked` true, danach steigerbar | EditMode | State |
| S5 | `AvailablePoints` = verdient − ausgegeben (nie negativ) | EditMode | Invariante |
| V1 | Skillwert: `Value = base + step·Level`, Deckelung (z. B. ≤ 25 kg) | EditMode | Wertmodell |
| A1 | Runtime: nach Invest übernimmt `PlayerCarry.Capacity` den neuen Tragkraftwert | PlayMode/manuell | Apply |

> X1–X4, S1–S5, V1 decken die Core-Entscheidungen; A1 die Apply-Übertragung auf bestehende Komponenten.
