# Phase 0 – Research: F5 – Schnee-Schmelzsystem

Entscheidungen aus Konzeptdurchsicht + Konversation. Keine offenen `NEEDS CLARIFICATION`.

## R1 – Höhenfeld als Core-Grid + Mesh-Spiegelung

- **Entscheidung**: Die Schneehöhe lebt als reines Zell-Grid (`MeltField`, Höhe 0..1) in der Core-Schicht;
  die Runtime spiegelt es in ein Grid-Mesh (Vertex-Höhe = Schneehöhe). Coverage (% freigelegt) wird im Core
  inkrementell mitgeführt → O(1)-Abfrage, unit-testbar.
- **Begründung**: Erfüllt Prinzip IX (Fortschritts-/Akku-Logik ohne Unity testbar) und hält die GPU-
  Darstellung als reine Spiegelung. Coverage ist die Andockstelle für XP/Aufgaben (F6/F7).
- **Alternativen verworfen**: GPU-RenderTexture als alleinige Wahrheit + Readback (teurer, nicht unit-testbar).

## R2 – CPU-Vertex-Displacement statt Tessellation

- **Entscheidung**: Volumen über **CPU-Vertex-Displacement** eines fein unterteilten Grid-Mesh (Vertex-Y aus
  dem Höhenfeld, `RecalculateNormals`). Tessellation/Height-RT-Displacement ist spätere Politur.
- **Begründung**: Tessellation-Shader sind blind (ohne sichtbares Editor-Feedback) schwer korrekt zu schreiben
  und der dokumentierte Hauptperformance-Risikopunkt. CPU-Displacement auf einem **begrenzten Patch** ist robust,
  liefert echte Mulden und braucht keinen komplexen Shader. Mesh-Sync nur beim Schmelzen/Auftragen.
- **Konsequenz**: Skalierung auf große Flächen (Chunking/LOD) ist bewusst späterer Scope.

## R3 – Freilegen über Shader-Clip mit Noise

- **Entscheidung**: Der Schnee-Shader (`SnowMelt`, URP) bekommt die Schneehöhe pro Vertex über den Vertex-Color
  (r-Kanal) und **clippt** Fragmente, wo die Höhe unter einer noisy Schwelle liegt → freigelegte Stellen zeigen
  den Boden, der Rand franst weich/unregelmäßig aus. Textur = stilisierte Snow-Map, Glitzer + Fake-Lighting prozedural.
- **Begründung**: Clip + Value-Noise ist ein kleiner, robust schreibbarer Shader (nur `Core.hlsl`), erfüllt den
  „sinnvollen Übergang" ohne zusätzliche Textur. `Cull Off` + `abs(dot)`-Lighting machen ihn robust gegen
  Mesh-Normalen-/Winding-Fehler (blind nicht verifizierbar).
- **Offen (Editor-Iteration)**: Randweichheit, Höhe, Glitzer-Stärke werden mit Test-Feedback justiert.

## R4 – Zielen: Raycast auf die Patch-Ebene

- **Entscheidung**: `MeltController` schießt einen Strahl aus der Blickrichtung und schneidet die **Ebene** des
  Patches (kein Mesh-Collider, da das Mesh sich jeden Frame ändert). Trefferpunkt → lokale UV → Core-Pinsel.
- **Begründung**: Mesh-Collider-Updates pro Frame wären teuer; eine Ebenen-Schnittrechnung ist günstig und genügt
  fürs gezielte Schmelzen einer „Säule". Feinheiten (Treffer auf die tatsächliche Schneeoberfläche) sind spätere Politur.

## R5 – Akku & Aufladen

- **Entscheidung**: `LampBattery` (Core) verbraucht beim tatsächlichen Abtrag (F gehalten + Treffer), sperrt bei
  Stand 0 und lädt langsam nach, wenn nicht geschmolzen wird (für den Test). Gebäude-Aufladezonen folgen mit dem
  Sektor-Content. Parameter sind Andockpunkt für F6-Upgrades.
- **Begründung**: Erfüllt US3 testbar; Auto-Regen hält den Test-Loop flüssig ohne zusätzliche UI.

## R6 – GPU vs. testbare Core (Constitution IX/X)

- **Entscheidung**: Akku-/Höhenfeld-/Coverage-**Entscheidungen** liegen testbar im Core; der **Shader, die
  RenderTexture-/Mesh-Darstellung** sind nicht unit-testbar und werden über EditMode-Smoke + manuelle Editor-
  Iteration abgedeckt. Diese Nicht-Testbarkeit ist als **dokumentierte Ausnahme** festgehalten (Prinzip IX/X).
- **Begründung**: Schmelzen ist inhärent GPU; die Constitution erlaubt dokumentierte, begründete Ausnahmen für
  reine Darstellungsanteile, solange die Fachlogik testbar gekapselt bleibt.

## R7 – Dev-Helfer „Schnee auftragen" (V)

- **Entscheidung**: Taste V hebt die Schneehöhe wieder an (bis Max) – reines Test-Werkzeug für beliebige
  Schmelz-Wiederholung, klar gekapselt im `MeltController`. Kein Konzept-Gameplay; eine spätere „Schneekanone"
  als echtes Feature wäre ein kleiner Umbau.
- **Begründung**: Vom Nutzer gewünscht; beschleunigt das Testen ohne Szenen-Neustart.
