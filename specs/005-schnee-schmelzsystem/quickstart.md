# Quickstart: F5 Schnee schmelzen testen

## Einrichten (Editor-Menü)

1. Unity fokussieren (kompiliert die neuen Skripte + den Shader; Konsole auf Shader-Fehler prüfen).
2. Menü **„CozySanta/Setup F5 (Schnee-Patch + Lampe)"** ausführen. Das:
   - erzeugt `Assets/_Project/Materials/M_SnowMelt.mat` (Shader `CozySanta/SnowMelt` + Textur `Holiday_Snow_02`),
   - legt ein **`SnowPatch`**-Objekt ~4 m vor dem Player auf `y = 0` an (mit dem Material),
   - ergänzt **`MeltController`** am `Player` (Kamera als Leuchtursprung).
3. **Patch positionieren**: Falls der Schnee nicht auf deiner Boden-Plane liegt, das `SnowPatch`-Objekt in der
   Szene auf Bodenhöhe schieben/skalieren (`size`, `maxHeight`, `resolution` am `SnowPatch` einstellbar).
4. Szene speichern (Strg+S), **Play**.

## Steuerung

- **F (halten)** = Taschenlampe/Schmelzen → an der Blickstelle sinkt der Schnee, bis der Boden (die Plane) erscheint.
- **V (halten)** = Schnee wieder auftragen (Dev-Helfer) → beliebig oft erneut schmelzen, ohne Neustart.
- Akku leert sich beim Schmelzen und lädt langsam nach, wenn du **nicht** schmilzt (leerer Akku = kein Abtrag).

## Look justieren (am Material `M_SnowMelt`)

- `Randweichheit` / `Rand-Noise` → weicher, unregelmäßiger Schmelzrand.
- `Tiling` → Wiederholung der Schnee-Textur.
- `Glitzer`, `Grundhelligkeit`, `Lichtrichtung` → Stimmung.
- Schneedicke = `maxHeight` am `SnowPatch`; Detailgrad = `resolution`.

## Erwartung / Iteration

- Erster Lauf beweist die **Mechanik** (gezieltes Abtragen, Volumen, weicher Rand, Akku, Fortschritt).
- Da ich den Shader **nicht** selbst sehen kann: Falls etwas „falsch" aussieht (Schnee zu dunkel, Rand zu hart,
  Höhe zu flach, Boden nicht sichtbar), melde es kurz zurück – ich justiere Shader/Parameter in 1–2 Runden.

## Tests

- **EditMode**: `SnowMeltTests` (B1–B4 Akku, M1–M5 Höhenfeld/Coverage) – im Test Runner ausführen.
- **Compile**: `dotnet build cozychristmas.sln` (C#); der **Shader** wird nur im Unity-Editor kompiliert (Konsole prüfen).

## Bekannte Grenzen (bewusst späterer Scope)

- Nur **ein** Test-Patch; große Flächen brauchen Chunking/LOD (nicht F5).
- Zielen trifft die Patch-Ebene (gezielte „Säule"), nicht die exakte Schneeoberfläche – Politur später.
- Akku-/Fortschrittsanzeige (HUD) kommt mit F7; XP-Vergabe mit F6.
