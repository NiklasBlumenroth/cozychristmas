# Notizen – Cozy Santa Factory

> Persönliche Projektnotizen. Laut Constitution (Prinzip I) bewusst **außerhalb**
> der Doku-Pflicht. Hier landen Roadmap, Ideen, offene Entscheidungen.

## Feature-Roadmap (Spec-Kit-Schnitt)

Geschnitten nach **Mechanik-Systemen mit testbarer Core-Logik** (Prinzip IX),
nicht nach Gebäuden. Gebäude/Item-Mengen sind Content/Daten, kein eigenes Feature.

### Phase 0 – Fundament
- **F1 – Core-Architektur & Projektgrundgerüst**: Ordnerstruktur, entkoppelte
  Core-Schicht, Provider-Interfaces (Zeit/Welt/Input), Decide/Apply-Muster,
  Assembly Definitions, EditMode-/PlayMode-Test-Setup.
- **F2 – First-Person-Controller & Interaktionssystem**: WASD, Blick, blick-/
  reichweitenbasierter Interaktions-Raycast, Interaktionshinweis.

### Phase 1 – MVP-Kernloop (Sektor 1 / Poststelle)
- **F3 – Trag-, Hand- & Gewichtssystem**: Aufnehmen, Links/Rechts-Stapellogik,
  Ablegen rückwärts, Traglast/Objektgewicht.
- **F4 – Sortiersystem & Sortierfeedback**: Zielorte, Korrektheits-/
  Vollständigkeitsprüfung, Lampe am Zielort, sanftes Fehlerfeedback.
- **F5 – Schnee-Schmelzsystem**: Lampenkegel, maskenbasiertes Freilegen, Akku +
  Aufladen, Flächen-Fortschritt pro Area. (Größtes technisches Risiko.)
- **F6 – XP- & Skillsystem**: gemeinsamer Pool, Level-up, frei investierbare
  Punkte, ~20 kleinteilige Stufen je Option, kein fester Tree.
- **F7 – Area- & Aufgabensystem + HUD**: Area-Datenmodell, Aufgabentypen,
  Fortschritt (Zähler/Prozent), Anzeige oben rechts.
- **F8 – Schlüssel-, Tor- & Sektorfreischaltung**: Gebäude → Schlüssel → Tore →
  nächster Sektor.

→ Nach F1–F8 ist Sektor 1 durchspielbar = **MVP-Schnitt**.

### Phase 2 – Fähigkeiten & weitere Gebäude
- **F9 – Sortierblick** (Cooldown/Dauer, Bezug zum linken Objekt).
- **F10 – Objektanziehung** (Reichweite, max. Anzahl, erlaubte Arten, Cooldown).
- **F11 – Montagesystem** (Rohre/Zahnräder, Werkstatt).

### Phase 3 – Finale
- **F12 – Geschenkcontainer-Batchsystem** (25er-Batches, Validierung,
  Deckenrohr-Auswurf, 1-min-Cooldown).
- **F13 – Finale & Sammelobjekte** (Christbaumkugeln finden, Baum schmücken,
  Endbedingung/Spielende).

### Querschnitt
- **F14 – Speichern/Laden & Persistenz** (Snapshots, PlantUML-pflichtig).

### Abhängigkeiten
F1→F2 zuerst · F3/F4/F5 bauen auf F2 · F6/F7 quer-koppelnd · F9/F10 brauchen
F3+F4 · F12 braucht F4 · F13 braucht F5+F7 · F14 nach stabilem Domänenmodell.
**Risiko-First:** F5 (Schnee-Maske) und F3 (Stapellogik lesbar) früh als
graue Prototypen.

## Offene technische Entscheidungen

### Boden: Terrain vs. Mesh (Status: offen)
- Du hast `Assets/New Terrain.asset` angelegt. **Nicht falsch**, aber bewusst entscheiden.
- Empfehlung: **Schnee als eigene Schicht** behandeln (Mesh/Quad/Decal mit
  Mask-Shader) – unabhängig davon, ob der Boden Terrain oder Mesh ist. Das
  Schmelzen löscht die Maske der Schneeschicht und legt den Boden darunter frei.
- Damit ist Terrain als Boden ok. ABER: Das Fabrikgelände ist strukturiert/flach
  (Wege, Gebäude-Footprints). Terrain glänzt bei natürlichen Landschaften – hier
  evtl. Overkill. Für Grey-Box-MVP ist eine simple Plane / ProBuilder-Fläche
  schneller und shader-freundlicher.
- → Vorschlag: Terrain vorerst behalten, falls schon Höhen/Form drinstecken.
  Wenn es nur eine flache Grundfläche ist, später leichtgewichtig auf Mesh-
  Module umstellen. Entscheidung spätestens im F5-Spike treffen.

### Controller-Typ (F2): CharacterController vs Rigidbody
- Empfehlung **CharacterController**: cozy, kein Physik-Geschubse, deterministisch,
  testfreundlicher. Rigidbody nur falls echte Physik-Interaktionen nötig werden.

## Technischer Ist-Stand
- Unity 6000.2.7f2 (Unity 6.2), URP 17.2, Input System 1.14.2, Test Framework 1.6.
- AI Navigation + Terrain-Module installiert.
- Vorhanden: `InputSystem_Actions` (Default), `SampleScene`, URP-Settings,
  `New Terrain.asset`.



Wenn das Modell im Spiel ist: Leuchten & Nutzung im cozy Stil

Das Modell ist nur die Hülle — die Magie machst du in Unity über Material, Licht und Partikel. So bleibt es cozy und liest sich klar:

1. Der Funke = Emission, kein Modell-Detail

- Gib der Kristallkugel ein eigenes Material mit Emission (warmes Gold, z. B. #FFB347 → #FFD27F).
- Lege die Emission auf eine Animation/Kurve, die sanft pulsiert („atmet") — Geschwindigkeit ~0.5–1 Hz. Das wirkt lebendig, nicht technisch.
- In URP: Bloom im Post-Processing anschalten → der Funke glüht weich über seine Kanten hinaus. Das ist der wichtigste Cozy-Trick.

2. Echtes Licht für die Welt

- Eine Point Light im Kuppel-Inneren, warm getönt, weiche Reichweite → wirft sanftes Licht auf Hände/Objekte in der Nähe.
- Optional eine Spot Light nach vorne als der eigentliche Schmelz-Kegel (deckt sich mit LampCone). Intensität/Winkel kannst du direkt an die Skill-Werte koppeln.

3. Mechaniken sichtbar koppeln (an deinen bestehenden Code)

- Akku (LampBattery): Emission-Farbe + Lichtintensität per Skript am Ladestand interpolieren. Voll → warmes Gold & hell; leer → kühles Blau & matt. Das Glasröhrchen-Material gleich mitfärben.
- Schmelzen aktiv (MeltController, Taste F): beim Halten Funke heller + Spot-Intensität hoch, beim Loslassen sanft zurück (Lerp über ~0.2 s, nie hart umschalten — Härte killt „cozy").
- Aufladen (Ladestation): Emission langsam von blau zurück nach gold faden lassen → fühlbares „Auftanken".

4. Partikel & Feedback (sparsam!)

- Am Funke: ganz feine, langsam aufsteigende Glitzer-/Glut-Partikel — wenige, klein, warm. Passt zum Glitzer eurer SnowMelt-Optik.
- Am Schmelzpunkt: kleiner warmer Lichtfleck + aufsteigender Dampf dort, wo der Schnee verschwindet.

Cozy-Grundregeln, die alles zusammenhalten

- Warm > kalt, weich > scharf: runde Lichtabfälle, sanfte Übergänge, nichts blinkt hart.
- Bewegung lebt: das leichte Pulsieren des Funkens ist der Unterschied zwischen „Requisite" und „lebendiger magischer Lampe".
- Bloom ist dein Freund, aber dezent — sonst wird's kitschig statt cozy.

Praktisch: Im Material-Setup brauchst du also 2 Materialien (Metall/Glas-Hülle + emittierende Kugel), 1–2 Lichter und 2 kleine Partikelsysteme. Den Rest steuert ein kleines Skript, das an LampBattery/MeltController hängt und  
Emission + Lichtintensität interpoliert.

