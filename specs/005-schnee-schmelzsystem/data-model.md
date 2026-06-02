# Phase 1 – Data Model & Contracts: F5

> „Contracts" = die C#-Typen/Signaturen. F5 erweitert F1/F2 additiv. Abhängigkeit strikt Runtime → Core.

## Core (`CozySanta.Core.Snow`) – reine, testbare Logik

### `LampBattery`

- Konstruktor `LampBattery(float capacity)` (startet voll).
- `float Capacity { get; set; }`, `float Level { get; }`, `bool CanMelt => Level > 0`, `float Fraction`.
- `void Drain(float amount)` (clamp 0), `void Recharge(float amount)` (clamp Capacity), `void Refill()`.
- Invariante: `0 <= Level <= Capacity`.

### `MeltField`

- Konstruktor `MeltField(int resolution)` – Grid `Resolution × Resolution`, alle Höhen = 1.
- `int Resolution`, `int CellCount`, `float HeightAt(int x, int y)` (0..1).
- `float Coverage` (0..1, Anteil Zellen mit Höhe ≈ 0), `float CoveragePercent`.
- `void Melt(float u, float v, float radius, float strength)` – senkt Höhe im weichen Pinsel (u,v normiert, radius normiert).
- `void Add(float u, float v, float radius, float strength)` – hebt Höhe (Dev-Auftrag), clamp 1.
- Invarianten: Höhe je Zelle in [0,1]; `Coverage` konsistent mit Zahl freigelegter Zellen (inkrementell geführt);
  Pinsel-Falloff weich (Mitte stark, Rand 0); außerhalb des Radius keine Änderung.

## Runtime (`CozySanta.Runtime.Snow`)

### `SnowPatch : MonoBehaviour` (RequireComponent MeshFilter/MeshRenderer)

- `[SerializeField] int resolution`, `float size`, `float maxHeight`.
- `Awake` generiert das Grid-Mesh (Vertices `R×R`, UV 0..1, Start-Höhe `maxHeight`, Vertex-Color.r = 1) und das `MeltField`.
- `bool Melt(Vector3 world, float radiusMeters, float strength)` – Weltpunkt→UV; false außerhalb; sonst `MeltField.Melt` + `SyncMesh`.
- `bool AddSnow(Vector3 world, float radiusMeters, float strength)` – analog mit `MeltField.Add`.
- `float Coverage => MeltField.Coverage`; `float AimHeight => maxHeight` (Zielhöhe).
- `SyncMesh` (Apply): Vertex-Y = Höhe·`maxHeight`, Vertex-Color.r = Höhe, `RecalculateNormals/Bounds`.
- Mapping Weltpunkt→UV über `transform.InverseTransformPoint` (lokale XZ → u,v).
- `EnsureAimCollider`: Trigger-`BoxCollider` (Footprint × `maxHeight`) als Ziel-Volumen für den Raycast (blockiert die Bewegung nicht).
- `OnDrawGizmos`: zeigt die Schnee-Ausdehnung (Fläche + Höhe) im Edit-Modus, da das Mesh erst zur Laufzeit entsteht.

### `MeltController : MonoBehaviour`

- `[SerializeField] Transform viewOrigin; SnowPatch patch;` + Lampe-/Akku-Parameter.
- Hält `LampBattery`; `float BatteryFraction`, `float Coverage` (für spätere HUD-Bindung, F7).
- `Update`: F gehalten + `CanMelt` + Treffer → `patch.Melt(...)` + `battery.Drain(rate·dt)`; V gehalten + Treffer →
  `patch.AddSnow(...)`. **Nachladen, wenn in diesem Frame nicht aktiv geschmolzen wurde** (`!didMelt`) – verhindert
  den Akku-Deadlock bei gehaltenem F mit leerem Akku.
- Zielen: `Physics.RaycastAll` gegen den Trigger-Collider des Patches (trifft das Schnee-Volumen); Fallback =
  Schnitt mit der Patch-Ebene auf halber Schneehöhe (`AimHeight·0.5`), `enter <= maxRange`.
- Eingabe: `Keyboard.current.fKey/vKey` (formale Input-Actions später). `UnityEngine.Time.deltaTime` (Namenskonflikt mit `CozySanta.Runtime.Time` explizit aufgelöst).

### Shader `CozySanta/SnowMelt` (URP)

- Properties: `_BaseMap`, `_BaseColor`, `_Tiling`, `_EdgeWidth`, `_NoiseScale`, `_LightDir`, `_Ambient`, `_Sparkle`.
- Vertex: Standard-Transform; reicht UV + Vertex-Color.r (Höhe) + WorldNormal weiter.
- Fragment: `clip(height − noisySchwelle)` (Freilegen + weicher Rand via Value-Noise), Textur·Fake-Lighting (`abs(dot)`), Glitzer. `Cull Off`, Fallback URP/Unlit.

### `SnowSetup` (Editor)

- Menü „CozySanta/Setup F5 (Schnee-Patch + Lampe)": erzeugt `M_SnowMelt` (Shader + `Holiday_Snow_02`), legt einen `SnowPatch` 4 m vor dem Player (y=0) an, verdrahtet `MeltController` am Player (Kamera als `viewOrigin`).

## Abhängigkeitsrichtung

```
Runtime ──▶ Core (LampBattery, MeltField). Core kennt weder UnityEngine noch SnowPatch/Shader.
```

## Abgeleitete Testkandidaten

| ID | Testfall | Art | Quelle |
| --- | --- | --- | --- |
| B1 | Akku startet voll, `CanMelt`, `Fraction`==1 | EditMode | Class/State |
| B2 | Drain senkt, clamp 0, leer → `CanMelt`==false | EditMode | State-Sperre |
| B3 | Recharge entsperrt, clamp Capacity | EditMode | State |
| B4 | Refill setzt auf voll | EditMode | State |
| M1 | Feld startet voll, Coverage 0 | EditMode | Class |
| M2 | Melt senkt Mitte, clamp 0, Coverage > 0 | EditMode | Activity-Abtrag |
| M3 | Volle Freilegung → Coverage 100 % | EditMode | Coverage-Invariante |
| M4 | Add hebt Höhe, clamp 1, Coverage zurück auf 0 | EditMode | Dev-Auftrag |
| M5 | Melt außerhalb des Radius → unverändert | EditMode | Pinsel-Grenze |
| (Smoke) | F schmilzt sichtbar, V trägt auf, Akku leert/lädt, Boden erscheint | manuell/Editor | Shader/Apply (dokumentierte Nicht-Unit-Ausnahme) |

> B1–B4 + M1–M5 decken die Core-Entscheidungen; der visuelle Schmelz-/Reveal-/Rand-Look wird per Editor-Smoke + Iteration abgenommen.
