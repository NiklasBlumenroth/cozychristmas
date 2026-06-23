# F10 – Instanziertes Rendern ruhender Items (Draw-Call-Kollaps)

## Ziel
Die geladenen Item-Massen eines Gebäudes (Bücher/Süßes/Kisten-Inhalte) sollen nicht mehr je
ein Draw Call kosten. Playtest-Profiling zeigte beim Betreten eines Gebäudes ~14.000 der
~18.000 Draw Calls allein aus diesen Items – jedes Item = ein eigener `MeshRenderer`. Die
Mengen bleiben unangetastet (Spiel-Kern = Masse); gesenkt werden nur die Render-Kosten.

## Warum GPU-Instancing per Code
Die Items sind massenhaft Duplikate (96 Buch-Varianten × bis 20, 20 Süßes-Varianten …).
`Graphics.RenderMeshInstanced` zeichnet alle Kopien einer (Mesh, Material)-Gruppe in einem Draw,
egal ob 1 oder 1.000. Das Material-Flag `enableInstancing` reicht in URP nicht, weil der
**SRP Batcher** Vorrang hat und es ignoriert; die explizite API umgeht das. Ziel: 1.920 Bücher
→ ~96 Draws.

## Architektur (Decide/Apply, Constitution-Prinzip VIII/IX)
- **Core – `CozySanta.Core.Rendering.InstanceSlots`** (rein, ohne Unity): dichte Slot-Verwaltung
  je Render-Gruppe. `Add(ownerId) → index`, `RemoveAt(index)` via Swap-with-last (Array bleibt
  lückenlos; meldet den Quell-Index des nachgerückten Elements, damit die Runtime ihre parallelen
  Matrix-/Owner-Arrays gleich nachzieht), `ChunkRanges(total, max)` für die 1023er-Grenze.
- **Runtime – `CozySanta.Runtime.Rendering.InstancedItemRenderer`** (Apply, partial:
  `…cs` + `…Draw.cs`): je Gebäude auf dem `ItemArea.ItemParent`. Hält pro (Mesh, Submesh,
  Material, Schatten, Empfang, Layer)-Gruppe `RenderParams` + dichte `Matrix4x4[]`/`Transform[]`
  + eine `InstanceSlots`. `Register(itemRoot)` cacht die Weltmatrizen und schaltet die Einzel-
  Renderer ab; `Unregister` macht es rückgängig. `Update` zeichnet je Gruppe je Chunk; eine
  billige Kompaktierung entfernt Slots zerstörter Items (Reset/ClearArea → keine Geister).
  **Kein Per-Instanz-Culling:** Profiling zeigte die Szene als CPU-/Main-Thread-gebunden (GPU idle,
  `ms ≈ main_thread_ms`). Per-Instanz-Frustum-Culling spart nur GPU-Vertexlast (nicht nötig) und kostet
  CPU – kontraproduktiv. Darum werden alle Instanzen einer Gruppe gezeichnet; das grobe Culling ganzer
  Gruppen erledigt Unity günstig über `worldBounds`. Die Owner-null-Aufräumung (zerstörte Items) läuft
  gedrosselt (alle ~120 Frames), nicht je Frame. Ein nicht instanzierbares Material legt seine Gruppe
  einmalig still (`Broken`) und rendert ihre Items wieder einzeln (kein Per-Frame-Fehlerflut).
  Materialien mit fehlendem `enableInstancing` werden zur Laufzeit in-memory aktiviert.

## Andockpunkte (additiv)
- `SettlingBody.EnterRest()`: meldet das ruhende Item beim `InstancedItemRenderer` des Gebäudes an
  (deckt geladene Items **und** abgelegte Items ab – beide enden in `EnterRest`).
- `PlayerCarry.TryPickup()`: meldet das aufgenommene Item ab → eigener Renderer wieder sichtbar.
  (Aufwecken beim Ablegen läuft über `OnDropLanded → BeginSettling → … → EnterRest` von selbst.)
- `AreaActivator` deaktiviert den Gebäude-Root beim Verlassen → `Update` (und alle Draws) laufen
  nur im betretenen Gebäude. Keine zusätzliche Gattung nötig.

## Editor-Setup
`CozySanta/Performance/Instanced Item Rendering einrichten` (`InstancedRenderingSetup`): hängt je
`ItemArea.ItemParent` einen `InstancedItemRenderer` an (idempotent). Sonst keine Verdrahtung –
Items registrieren sich selbst beim Ruhen.

## Annahmen / Grenzen (v1)
- Gebäude-Roots bewegen sich zur Laufzeit nicht (gecachte Weltmatrizen bleiben gültig).
- In `SortTargetInteractable`-Fächer einsortierte Items bleiben einzeln gerendert (anderer Pfad,
  kleinere Mengen, meist occludiert) – dokumentierter Out-of-Scope.
- Materialien müssen instancing-fähige Shader nutzen (URP Lit/SimpleLit erfüllen das).
- **Culling für Items grob auf Gruppenebene** (Unity über `worldBounds`): ist eine ganze (Mesh,
  Material)-Gruppe komplett außerhalb des Sichtkegels, entfällt ihr Call; sonst werden alle ihre
  Instanzen gezeichnet (GPU hat Reserve). Occlusion (hinter Wänden) gilt für instanzierte Draws nicht.
- Der nach dem Item-Instancing verbleibende Engpass ist die **statische Hülle/Deko** (~6.300 Prefab-
  Instanzen, eigene Draws): nächster Hebel Static Batching / Deko-Instancing, nicht Teil von F10.

## Tests
EditMode `InstanceSlotsTests` (IS1–IS5): Add liefert fortlaufende Indizes; RemoveAt in der Mitte
swappt das letzte Element herein und meldet dessen Quell-Index; RemoveAt des letzten = kein Swap;
ungültiger Index = No-Op; ChunkRanges respektiert 0/1/1023/1024/2046. Die Rendering-Apply-Schicht
(`Graphics.RenderMeshInstanced`, Material/Schatten/Look) = Editor-/PlayMode-Iteration, dokumentierte
Nicht-Unit-Ausnahme analog Shader/`BookPrefabSetup`.

## Status
Core + Tests grün (standalone validiert). Runtime/Editor gegen Unity-6-API geschrieben, Compile-
Check maßgeblich im Unity-Editor. Verifikation: Setup-Befehl ausführen, im Play-Mode am Item-Haufen
den `ProfilerSampler` vergleichen – Draws sollen von ~21k auf wenige hundert fallen.
