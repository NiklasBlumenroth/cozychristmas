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




Assets generieren:
Gemeinsamer Stil- + Technik-Block (vor jeden Prompt setzen)

cozy stylized 3D Santa's workshop interior building kit piece, whimsical handcrafted
low-poly game art, soft warm festive palette, realistic sensible materials only
(timber, plaster, stone, iron). Single isolated modular asset, centered, fully visible,
flat-on orthographic, seamless tileable edges, clean straight square borders.
Pure white background, flat even studio lighting, no cast shadows, no people, no text.

Die Kit-Teile (für ALLE Gebäude gleich)

1. Boden – Holzdiele
   [Stil-Block] A square wooden plank floor tile, warm aged timber boards, subtle grain,
   slightly worn cozy look, tileable on all four edges, top-down flat view.

2. Wand – schlicht
   [Stil-Block] A straight interior wall panel segment, lower timber wainscot, upper
   warm plaster, simple wood corner studs, flat front view, tileable left/right edges.

3. Wand – mit Fenster
   [Stil-Block] A straight interior wall segment with one cozy square window, wooden frame
   and muntins, warm glowing glass, timber wainscot below, plaster above, flat front view,
   tileable left/right edges.

4. Wand – mit Türöffnung
   [Stil-Block] A straight interior wall segment with a wooden doorway opening (arched top),
   timber frame, wainscot and plaster, flat front view, tileable left/right edges.

5. Decke – mit Balken
   [Stil-Block] A square ceiling tile with exposed dark timber rafter beams and warm plaster
   between, rustic workshop look, bottom-up flat view, tileable on all four edges.

6. Eck-/Stützpfeiler
   [Stil-Block] A vertical corner support post, sturdy dark timber beam with simple iron
   brackets, standalone, front view, matching the wall height.

7. Sockelleiste / Zierleiste (optional)
   [Stil-Block] A simple wooden skirting board / baseboard trim strip, warm timber,
   straight long piece, front view, tileable along its length.

