# Performance-Untersuchung — FPS-Einbruch (Bibliothek / Bäckerei)

Arbeits- und Tagebuch-Datei. Oben die offenen Untersuchungspunkte, darunter chronologisch,
was wir geprüft haben. Schritt für Schritt abarbeiten.

## Problem & Baseline
Beim Betreten der Bibliothek (~1920 Bücher, 96×20) brechen die FPS ein.
Messung mit `ProfilerSampler` → `ProfilerLogs/profile_20260618_204501.csv`:

- **Ruhig** (Items nicht im Blick): ~60–67 FPS, **544 Draw Calls**, **0,32 Mio Tris**, GC ~85 KB/Frame.
- **Last**: 15–52 FPS, **2.000–8.174 Draw Calls**, bis **20,6 Mio Tris**, GC-Spitzen bis 880 KB/Frame.
- `main_thread_ms ≈ frame_ms` → **CPU-/Main-Thread-gebunden** (Draw-Call-Einreichen, Culling).
- `SettlingBody.FixedUpdate = 0 ms` → Items/Physik sind **NICHT** der Treiber (Einfrieren funktioniert).
- **Buch ≈ 110 Tris** → 1920 Bücher ≈ 0,2 Mio Tris → Bücher sind **NICHT** die Tris-Quelle.

## WICHTIG: Profiler-Zahlen sind Summen über ALLE Render-Pässe
Tris/Draw Calls werden **pro Pass** gezählt: Schatten-Cascades (4) + Hauptpass (+ ggf. Depth/Transparenz).
Geometrie wird also 5×+ gezählt. „20 Mio Tris" ≈ z. B. ~4 Mio echte Geometrie × 5 Pässe.

## Offene Untersuchungspunkte (Priorität oben)
1. [ ] **Terrain (21 Instanzen, 24 TerrainData)** — Heightmap-Tris, Schattenwurf, Tree/Foliage. **Höchster Verdacht** für die Tris. Rendert auch verdeckt/unter Gebäuden.
2. [ ] **Schatten: 4 Cascades + Main 2048 + Additional-Light-Shadows** — multipliziert alle Schatten-Caster. Test: Cascades 4→1 bzw. Shadow-Distance senken → Sampler-Delta.
3. [ ] **Deko-Lichter** — Anzahl Additional Lights mit Schatten (jedes = zusätzlicher Geometrie-Pass).
4. [ ] **Draw Calls der Bücher** — fehlendes GPU-Instancing → viele Einzel-Draws (CPU). Getrennt vom Tris-Thema.
5. [ ] **Lade-Spike vs Dauerlast** — 20M nur beim ersten Betreten (Item-Aktivierung) oder dauerhaft? Steady-State isolieren.
6. [ ] **Occlusion Culling** — rendern verdeckte Bücher/Terrain mit? (kein Bake vorhanden)
7. [ ] **Overdraw/Transparenz** — Schnee-Shader (Clip/Noise), transparente Layer.
8. [ ] **Bisektion** — Subsysteme einzeln aus (Terrain, Deko-Lichter, Bücher-Parent, Schatten global) und je Sampler-Delta.

## Tagebuch

### 2026-06-18 — Setup + erste statische Analyse
- `ProfilerSampler` gebaut (CSV in `ProfilerLogs/`), Lauf in der Bibliothek. Baseline siehe oben.
- ✅ **Ausgeschlossen:** `SettlingBody.FixedUpdate = 0` → Items/Physik nicht der Treiber.
- ✅ **Ausgeschlossen:** Bücher als Tris-Quelle (110 Tris/Buch).
- URP `PC_RPAsset`: SRP-Batcher **AN**, Dynamic Batching AUS, **4 Shadow-Cascades**, Main-Light-Shadow 2048,
  Shadow-Distance 50, Additional Lights per-object 8 + Additional-Shadows **AN**.
- Szene: **21 Terrains** (24 TerrainData), 1 Kamera, 0 Reflection Probes.
  Terrain: HeightmapPixelError 5, DrawTreesAndFoliage AN.
- Keine LODGroups im Projekt; GPU-Instancing AUS auf Item-Materialien; Buch-Materialien eingebettet (`.blend`).
- **Zwischenfazit:** Tris-Quelle ist nicht „Bücher", sondern mit hoher Wahrscheinlichkeit **Terrain × Schatten-Cascades**.
  Draw-Call-Last (CPU) zusätzlich durch fehlendes Instancing der Bücher.
- **Nächster Schritt:** Punkt 1+2 per Bisektion verifizieren — Terrain-Schattenwurf aus / Cascades 4→1 → Sampler vergleichen.

### 2026-06-18 — Bisect-Tool gebaut, wartet auf Messlauf
- `PerfBisectTool` gebaut: schaltet zur Laufzeit per Taste um; Zustand wird in `ProfilerSampler.Note` →
  neue CSV-Spalte `note` geschrieben (jede Zeile selbst-getaggt).
- **Tasten:** `T`=Terrains an/aus · `H`=Haupt-Schatten (Cascades) an/aus · `G`=Terrain-Schattenwurf an/aus ·
  `L`=Extra-Lichter an/aus · `B`=Bücher/Items-Renderer an/aus.
- **Testablauf (TODO Niklas):** In die Bibliothek laufen, dann nacheinander je ~10–15 s: alles an →
  `T` → wieder `T` → `H` → `H` → `G` → `G` → `L` → `L` → `B` → `B`. Sampler läuft mit (F8=Pause).
  CSV schicken → Delta je `note`-Zustand zeigt den Schuldigen.
- **Nächster Schritt:** CSV auswerten, teuersten Posten zuerst optimieren.

### 2026-06-18 — Bisect-Lauf ausgewertet: Rendering WIDERLEGT
- CSV `profile_20260618_211851.csv`, gleiche Stelle, je ~20 Zeilen pro Zustand:
  T0/H0/G0/L0/B0 → **FPS bleibt überall ~22–25, Main-Thread konstant ~43 ms.**
- ❌ **Ausgeschlossen:** Terrain, Haupt-Schatten (halbiert Tris 3,3→1,7 Mio → keine FPS-Änderung!),
  Terrain-Schatten, Extra-Lichter, Bücher (−2.500 Draw Calls → keine FPS-Änderung). → **NICHT Rendering.**
- **Befund:** konstante ~43 ms/Frame Main-Thread-Last, unabhängig von allem Visuellen.
- Statisch gefunden: **6 OnGUI-Dev-Tools** laufen jeden Frame (AreaInventoryHud, DevSpawnMenu, FpsDisplay,
  ItemSaveDevTool, SortPlacementRotationDevTool, SkillMenuDevTool) → passt zu **244 KB/Frame GC**.
  Diese wurden im ersten Bisect NIE getestet. OnDrawGizmos in SnowPatch/SortTarget/SortPlacementRotation
  = nur Editor-Scene-View-Last.
- **Neue Hauptverdächtige:** (1) IMGUI-Dev-Tools, (2) reiner **Editor-/Scene-View-Overhead** (tausende
  GameObjects + Gizmos + 2. Kamera) — existiert im echten Build evtl. gar nicht.
- **Tools:** `PerfBisectTool` um `U` (Dev-HUDs an/aus) erweitert.
- **Nächste Schritte (TODO Niklas):**
  1. Neuer Bisect-Lauf, diesmal `U` testen (HUDs aus ~15 s).
  2. Free-Checks ohne Code: im Game-View **Gizmos AUS**; **Game-View maximieren**; idealerweise einen
     **Development Build** starten und FPS (FpsDisplay) anschauen → wenn Build flüssig = reines Editor-Problem.

### 2026-06-18 — Track A Ergebnis: es sind die GIZMOS (Editor-only)
- **Game-View maximieren (Scene-View aus): brachte NICHTS.**
- **Gizmos deaktivieren (Game-View): deutlicher FPS-Gewinn.** → Hauptkostenpunkt = `OnDrawGizmos`.
- Ursache: `SortTargetInteractable.OnDrawGizmos` zeichnet **pro Fach pro Slot** Drahtwürfel+Kugel+Linie.
  Bäckerei: 32 Regale × 21 Slots + 4 Crates × 75 ≈ **1000+ Slots** → tausende Gizmo-Primitive je Repaint,
  unabhängig vom Blick. (Auch SnowPatch/SortPlacementRotation zeichnen Gizmos.)
- **Wichtig:** Gizmos existieren **im Build nicht** → das ist **kein echtes Spiel-Performance-Problem**,
  nur Editor-Komfort.
- **Offen:** Build-Test (Test 3) als Bestätigung. Danach optional Gizmo-Kosten senken (nur bei Auswahl
  zeichnen / Slot-Zahl deckeln), damit das Editor-Arbeiten flüssig bleibt.

## Vorgehen / Werkzeuge
- **ProfilerSampler** (Taste F8 = Pause): schreibt CSV, ich werte aus. Für **Bisektion**: ein Subsystem aus,
  ~20 s messen, Datei schicken, Delta vergleichen.
- **Frame Debugger** (Window > Analysis > Frame Debugger) — *DU*: zeigt jeden Draw + Pass.
  Screenshot der Gesamtzahl je Sektion (Shadows / Opaque / Transparent) + der teuersten Einträge.
- **Game-View „Stats"** — *DU*: Screenshot (Batches/SetPass/Tris/Verts) an ruhigem vs. teurem Blickwinkel.
- Ich kann den Sampler erweitern (z. B. „Shadow Casters Count", per-Kamera) für gezieltere Zahlen.
