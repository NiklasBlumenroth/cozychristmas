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




❯ in diesem spiel gibt es ja eine lampe mit der ich den schnee schmelzen kann als aufgabe. wie könnte diese lampe aussehen? es kann gerne magisch wirken weils ja auch ein magischer sturm war der das chaos in das spiel gebracht  
hat weshalb der spieler alles aufräumen muss

● Schöne Frage — und sie passt gut zu dem, was die Lampe im Spiel schon kann (Akku, Kegel/Cone, Leistung, Aufladen an der Ladestation). Ich orientiere mich an diesen Mechaniken, damit das Aussehen nicht nur hübsch ist, sondern  
das Gameplay lesbar macht.

Grundidee: eine warme Lichtquelle gegen den kalten Sturm

Der magische Sturm war kalt, blau, chaotisch → die Lampe ist sein Gegenpol: warm, golden, ordnend. Das verkauft sofort die Fantasie „ich bringe Wärme & Ordnung zurück".

Konkreter Vorschlag (meine Empfehlung)

Eine altmodische Sturmlaterne / Grubenlampe, in der statt einer Flamme ein Stück gezähmtes Sturm-Licht schwebt.

- Gehäuse: Messing/Kupfer, leicht abgenutzt, runde Glaskuppel — vertraut, „weihnachtlich-nostalgisch", cozy.
- Kern: keine Flamme, sondern ein schwebendes, pulsierendes Licht-Kügelchen (ein „eingefangener Sturmfunke"). Das erklärt die Magie narrativ: Der Spieler nutzt die Energie des Sturms gegen den Sturm.
- Licht: warmes Gold-Orange, das nach vorne in einen sichtbaren Kegel strahlt — deckt sich direkt mit dem LampCone-Skill, der Spieler sieht seine Upgrades.

Wie die Mechaniken sichtbar werden

- Akku (LampBattery): ein Glasröhrchen/Ring an der Seite, der sich wie ein Thermometer leert — voll = goldglühend, leer = mattes Blau (Sturmkälte kriecht zurück). Klares Feedback ohne HUD-Blick.
- Leistung (LampPower): beim Schmelzen wird der Funke heller/größer, kleine Glut-Partikel & Hitzeflimmern am Kegelrand.
- Aufladen (Ladestation): der Funke „atmet" sich wieder voll, Lichtfarbe kippt von blau zurück zu gold.
- Schmelzen selbst: warmer Lichtfleck auf dem Schnee + aufsteigender Dampf/Glitzer (passt zur Glitzer-Note eures SnowMelt-Shaders).

Alternativen, falls die Laterne nicht trifft

- Magischer Stab / Zauberstab mit Kristallspitze: kristalliner Kern (greift euer Kristall-Key-Modell auf → visuelle Sprachfamilie). Wirkt mehr „Fantasy-Magie", weniger „cozy-nostalgisch".
- Glühende Schneekugel/Lampions-Kugel an einem Griff: sehr cozy, runde Christbaumkugel-Form (verbindet thematisch mit dem Endziel Weihnachtsbaum), aber der Lichtkegel ist schwerer glaubhaft zu machen.
- Umgebaute Werkstatt-Lötlampe der Elfen: „Fabrik"-Charme, technisch-bastelig — passt zur Santa-Factory, weniger magisch.

  ---
Wenn du magst, kann ich:
1. das als kurze Konzept-Notiz unter GameKonzept/docs/ oder im Feature-Ordner ablegen (Doku-Regel der Constitution), oder
2. die Idee an die schon vorhandenen MeltController/Shader-Parameter knüpfen und eine konkrete Material-/Partikel-Checkliste für den Editor-Bau erstellen.

Welche Richtung gefällt dir — die Sturmlaterne oder eher der kristalline Stab?

✻ Cooked for 30s

❯ welchen stil sollte die lampe haben? super realistisch oder was?

● Kurze Antwort: nicht super-realistisch. Das würde gegen fast alles arbeiten, was euer Spiel ausmacht.

Warum kein Foto-Realismus

- Genre-Bruch: „Cozy" lebt von Wärme, Weichheit, leichter Überzeichnung. Realistische PBR-Lampe mit echten Materialfehlern, Fingerabdrücken etc. wirkt schnell kalt/technisch — das Gegenteil der Stimmung.
- Magie-Problem: Ein schwebender Sturmfunke in einer fotorealistischen Laterne sieht aus wie ein Fehler, nicht wie Magie. Stilisierung erlaubt das Magische erst.
- Eure eigene Linie: Schnee-Glitzer, Fake-Lighting im SnowMelt-Shader, der „graue Prototyp"-Ansatz — ihr baut ohnehin stilisiert, nicht simuliert.
- Lesbarkeit: Akku-Stand über Farbe (gold→blau), Kegelgröße, Funkengröße — das funktioniert nur, wenn der Stil Klarheit über Detail stellt.
- Performance/Aufwand: Realismus kostet Texeldichte, Normal-Maps, Material-Tuning. Für ein First-Person-Aufräumspiel verschwendet.

Meine Empfehlung: stilisiert-handgemacht, warm

Richtung „Pixar/Overcooked/A Little to the Left meets Weihnachtsmarkt":

- Formen: leicht überzeichnet, runde Kanten, etwas „dicker" als realistisch (liest sich gut in First-Person, nah an der Kamera).
- Materialien: klare, sauber abgesetzte Töne (Messing, Glas, Funke) — eher flächig mit weichen Verläufen als hochdetailliert. Gerne ein Hauch Cel-/Toon-Anteil oder zumindest weiche, gemalte Highlights.
- Farbe trägt die Story: warmes Gold = Funktion/Magie, kühles Blau = Sturm/leer. Sättigung ruhig etwas hochziehen.
- Detail durch Licht, nicht durch Textur: Emission, Glow, Partikel, Bloom machen die Magie — nicht Kratzer-Maps.

Einordnung der Stil-Optionen

┌──────────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                 Stil                                 │                                                       Passt?                                                        │
├──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Stilisiert-realistisch (Pixar-artig, weiche PBR, überzeichnete Form) │ ✅  Empfehlung — cozy + magie-tauglich + lesbar                                                                      │
├──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Cel-Shaded / Toon (harte Kanten)                                     │ 🟡  möglich, aber prüfen ob es zum restlichen Szenen-Look passt (euer Schnee-Shader ist eher weich, nicht hart-toon) │
├──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Low-Poly / Flat                                                      │ 🟡  günstig & charmant, aber das Nahobjekt in First-Person darf ruhig etwas mehr Liebe vertragen                     │
├──────────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Foto-Realismus                                                       │ ❌  Stimmungs- & Magie-Bruch, zu teuer                                                                               │
└──────────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

Wichtig: Den Stil der Lampe nicht isoliert festlegen — sie wird zur visuellen Leitlinie für das ganze Spiel (sie ist permanent im Bild). Was ihr hier entscheidet, sollten Schlüssel, Tore, Kisten usw. später spiegeln. Euer     
Kristall-Key-Modell deutet schon Richtung „stilisiert mit magischem Glow" — die Lampe sollte dazu passen.

Willst du, dass ich daraus eine knappe Art-Direction-Notiz schreibe (Stil, Farb-Codes für gold/blau, Material-Liste), die du als Referenz für den Lampen-Bau und spätere Assets nutzen kannst?

✻ Cooked for 28s

❯ ok das klingt alles gut. kannst du mir einen prompf für eine ki ausgeben damit ich ein 3D modell generieren kann? maximal 990 zeichen.  die ki kann mir nur ein statisches modell ausgeben, ein lichtschein oder so wäre als      
ungeeignet. frage danach: wenn ich das modell der lampe ins spiel importiert habe, wie gestalte ich das leuchten bzw die nutzung im cozy stil?

● Prompt für den 3D-Modell-Generator

Geschrieben auf Englisch (die meisten 3D-Generatoren wie Meshy, Tripo, Rodin liefern damit bessere Ergebnisse) und bewusst ohne Licht/Glow — nur die feste Geometrie. Den Funke im Inneren beschreibe ich als physisches
Glaskugel-Objekt, nicht als Lichtschein, damit das Modell brauchbar bleibt.

Stylized handheld storm lantern, cozy Pixar-like game art style,
slightly exaggerated rounded shapes, clean readable forms.
Brass and copper frame with soft worn edges, warm golden metal.
Rounded glass dome housing, inside a small floating crystalline
orb (solid faceted sphere, like a glass marble) suspended on a
tiny metal mount. Top carry ring and a thick comfortable handle.
A small glass tube gauge along one side (battery indicator shape).
Smooth surfaces, gentle bevels, soft matte materials, subtle
toon-friendly shading, no scratches or grime. Christmas cozy mood,
magical workshop prop. Single object, centered, neutral pose,
game-ready, clean topology, PBR-friendly, white background,
no lighting effects, no glow, no particles.

Das sind ~780 Zeichen — also Luft nach oben, falls du noch etwas ergänzen willst (z. B. Größe oder Detailgrad). Tipp: „solid faceted sphere, like a glass marble" ist wichtig — sonst versuchen manche KIs, ein Leuchten zu       
modellieren, was als Geometrie hässlich wird.

  ---
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

Soll ich dir für Schritt 3 ein konkretes kleines Runtime-Skript skizzieren, das sich an deine vorhandenen MeltController/LampBattery-Werte hängt und Emission + Lichtintensität live setzt?

✻ Worked for 37s

❯ ich muss mir jetzt gedanken machen wo ich die materialien, sowie die lichter und partikelsysteme herbekomme oder kannst du sowas generieren?

● Gute Nachricht: Lichter und Partikelsysteme brauchst du nirgends „herholen" — die sind in Unity eingebaut. Und das Leuchten braucht keine Textur. Du musst also fast nichts extern besorgen. Ich kann den allergrößten Teil davon
generieren.

Was woher kommt

┌──────────────────────────────────────────────────────────────────────┬───────────────────────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────┐
│                               Element                                │                               Woher                               │                      Kann ich generieren?                       │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Light (Point/Spot)                                                   │ Unity-Komponente, kein Asset nötig                                │ ✅  per Editor-Setup-Skript oder du klickst es in 30 Sek. selbst │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Particle System                                                      │ Unity-Komponente (ParticleSystem), eingebaut                      │ ✅  per Skript konfigurierbar                                    │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Emissions-Material (glühende Kugel)                                  │ URP-Lit-Shader, eingebaut — nur eine Emission-Farbe, keine Textur │ ✅  als .mat / Editor-Skript                                     │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Metall/Glas-Material der Hülle                                       │ URP-Lit, eingebaut — Farbe + Metallic/Smoothness-Regler           │ ✅                                                               │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Steuer-Skript (Emission/Licht koppeln an LampBattery/MeltController) │ dein Code                                                         │ ✅  klar mein Job                                                │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Bloom                                                                │ URP Post-Processing Volume, eingebaut                             │ ✅  einrichtbar                                                  │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Das 3D-Mesh                                                          │ KI-Generator (dein Prompt)                                        │ ❌  machst du extern                                             │
├──────────────────────────────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────┤
│ Texturen (optional, Kratzer etc.)                                    │ extern — brauchst du für cozy aber kaum                           │ ❌  (und unnötig)                                                │
└──────────────────────────────────────────────────────────────────────┴───────────────────────────────────────────────────────────────────┴─────────────────────────────────────────────────────────────────┘

Kurz: Mesh kommt von der KI, alles andere kann aus Unity-Bordmitteln + meinem Code entstehen. Texturen sind im cozy-Stil verzichtbar — das Leuchten macht Emission + Bloom, nicht eine Textur.

Wie ich es liefern würde (passend zu eurem Projekt)

Ihr habt schon das Muster CozySanta/Setup F5 …, das Material + Patch erzeugt und verdrahtet. Genau in dem Stil würde ich es bauen:

1. Editor-Setup CozySanta/Setup Lampe … — erzeugt die 2 Materialien, hängt Point/Spot-Light + 2 Partikelsysteme ans Lampen-Prefab und legt das Bloom-Volume an.
2. Runtime-Skript LampVisuals (Core-Decide trennt sich hier kaum, aber die Interpolations-Mathe könnte als reine Funktion in Core testbar sein — passt zu eurer Architektur) — liest LampBattery-Ladestand +
   MeltController-Aktivität und setzt Emission-Farbe/Intensität + Lichtstärke live (gold↔blau, an↔aus mit Lerp).

So bleibt es konform zu eurer Constitution (Editor-authored Setup, Decide/Apply, Tests für die Interpolation) — und du musst extern nur das Mesh besorgen.

Bevor ich baue — zwei Dinge klären

Damit ich nichts ins Blaue baue:

● User answered Claude's questions:
⎿  · Wie weit soll ich jetzt gehen? → Komplettpaket (Empfohlen)
· Hast du das Lampen-Mesh schon importiert (mit benannten Teilen wie Kugel/Hülle)? → Noch nicht

● Komplettpaket, Mesh kommt noch — verstanden. Damit ich exakt an eure vorhandenen Klassen andocke und das Setup im gleichen Stil wie Setup F5 baue, schaue ich mir zuerst den Bestand an.

Searched for 3 patterns, read 2 files (ctrl+o to expand)

● Jetzt die Editor-Setup-Konvention (am F5/Snow-Setup) und das vorhandene Lampen-Material ansehen, damit ich Material-Erstellung und Verdrahtung im gleichen Stil mache.

Read 2 files (ctrl+o to expand)

● Jetzt noch die Core-Struktur (wo die testbare Interpolation hin soll), das Light-Setup-Muster und ob es schon ein Lampen-Objekt am Player gibt.

Searched for 1 pattern, read 1 file, listed 1 directory (ctrl+o to expand)

● Kein Lampen-Objekt in der Szene — das Rig baue ich also self-contained (mit Platzhalter-Funke), damit es schon vor dem Mesh-Import leuchtet und du das Mesh später nur einhängst. Noch kurz die asmdef-Referenzen (wegen
Bloom/URP-API) und ein Test-Stil prüfen.

Read 3 files, recalled 1 memory (ctrl+o to expand)

● Alles klar — ich habe genug Kontext. Ich lege jetzt das Komplettpaket an, konform zu eurer Architektur (Core-Mathe testbar, Runtime Apply, Editor-Setup wie Setup F5). Bloom selbst fasse ich bewusst nicht per Code an
(URP-Bloom-Typ ist im Editor-asmdef nicht garantiert referenziert → Zero-Compile-Error-Risiko); das ist ein 2-Klick-Schritt, den ich dir beschreibe.

Los geht's mit der testbaren Core-Mathe:

● Write(Assets\_Project\Core\Snow\LampVisualMath.cs)
⎿  Wrote 50 lines to Assets\_Project\Core\Snow\LampVisualMath.cs
1 namespace CozySanta.Core.Snow
2 {
3     /// <summary>
4     /// Reine, testbare Mathematik für die Lampen-Optik (Decide). Wandelt Akku-Stand + Schmelz-Zustand in
5     /// Ziel-Helligkeit und Warm/Kalt-Mischung um und glättet Werte frame-rate-unabhängig über die Zeit.
6     /// Keine Unity-Typen (Core, noEngineReferences). Die Runtime (<c>LampVisuals</c>) wendet die Werte an.
7     /// </summary>
8     public static class LampVisualMath
9     {
10         private const float Tau = 6.2831853071795862f;
… +40 lines (ctrl+o to expand)

● Write(Assets\_Project\Tests\EditMode\LampVisualMathTests.cs)
⎿  Wrote 64 lines to Assets\_Project\Tests\EditMode\LampVisualMathTests.cs
1 using CozySanta.Core.Snow;
2 using NUnit.Framework;
3
4 namespace CozySanta.Tests.EditMode
5 {
6     public sealed class LampVisualMathTests
7     {
8         // LV1: Pulse bleibt in [0,1] und liegt bei sin=0 in der Mitte (0.5).
9         [Test]
10         public void Pulse_StaysInRange_AndCentersAtHalf()
… +54 lines (ctrl+o to expand)

● Jetzt IsMelting additiv an MeltController (damit LampVisuals den aktiven Schmelz-Zustand lesen kann):

● Update(Assets\_Project\Runtime\Snow\MeltController.cs)
⎿  Added 4 lines
31          /// <summary>Akku-Ladestand 0..1 (Andockpunkt für eine spätere HUD-Anzeige, F7).</summary>
32          public float BatteryFraction => _battery != null ? _battery.Fraction : 0f;
33  
34 +        /// <summary>True, solange aktiv geschmolzen wird (F gedrückt UND Akku nicht leer).                                                                                                                               
35 +        /// Andockpunkt für die Lampen-Optik (<c>LampVisuals</c>).</summary>                                                                                                                                              
36 +        public bool IsMelting { get; private set; }                                                                                                                                                                       
37 +                                                                                                                                                                                                                          
38          [Tooltip("Empfohlen: Wurzel-Transform der Schnee-Region, die dieser Task zählt. Es werden nur " +
39                   "die SnowPatches UNTER dieser Wurzel aggregiert. Leer = ganze Szene (sehr großer Nenner!).")]
40          [SerializeField] private Transform coverageRoot;

● Update(Assets\_Project\Runtime\Snow\MeltController.cs)
⎿  Added 2 lines, removed 1 line
112              var hasHit = TryAimAtSnow(origin, out var world, out var aimed);
113  
114              // Akku läuft immer wenn F gedrückt, unabhängig ob Schnee getroffen wird
115 -            if (melting && _battery.CanMelt)                                                                                                                                                                             
115 +            IsMelting = melting && _battery.CanMelt;                                                                                                                                                                     
116 +            if (IsMelting)                                                                                                                                                                                               
117              {
118                  _battery.Drain(drainPerSecond * dt);
119                  if (hasHit)

● Jetzt das Runtime-Skript LampVisuals (Apply: setzt Emission, Lichtstärke, Partikel live aus Akku + Schmelz-Zustand):

● Write(Assets\_Project\Runtime\Snow\LampVisuals.cs)
⎿  Wrote 106 lines to Assets\_Project\Runtime\Snow\LampVisuals.cs
1 using CozySanta.Core.Snow;
2 using UnityEngine;
3
4 namespace CozySanta.Runtime.Snow
5 {
6     /// <summary>
7     /// Lampen-Optik (Apply): spiegelt Akku-Stand + Schmelz-Zustand des <see cref="MeltController"/> auf die
8     /// Emission des Funke-Renderers, die Lichtstärke (Kern-/Kegel-Licht) und die Funkel-Partikel. Die Mathe
9     /// (Warm/Kalt, Ziel-Pegel, Glättung, Atmen) liegt testbar in <see cref="LampVisualMath"/>; hier nur die
10     /// Seiteneffekte. Voll = warmes Gold + hell, leer = kühles Blau + matt; beim Schmelzen heller + mehr Funken.
… +96 lines (ctrl+o to expand)

● Jetzt das Editor-Setup CozySanta/Setup Lampe … — baut Materialien, Rig (Platzhalter-Funke + Lichter + Partikel) unter der Kamera und verdrahtet alles:

● Write(Assets\_Project\Editor\LampSetup.cs)
⎿  Wrote 234 lines to Assets\_Project\Editor\LampSetup.cs
1 using CozySanta.Runtime.Snow;
2 using UnityEditor;
3 using UnityEditor.SceneManagement;
4 using UnityEngine;
5
6 namespace CozySanta.Editor
7 {
8     /// <summary>
9     /// Einmal-Setup für die Lampen-Optik: erzeugt zwei Materialien (Gehäuse-Metall + emissiver Funke), baut
10     /// ein Lampen-Rig (Platzhalter-Funke, Kern-/Kegel-Licht, Funkel-Partikel) unter der Kamera, hängt
… +224 lines (ctrl+o to expand)

● Jetzt der Compile-Check. Laut Projektgedächtnis muss ich die neuen .cs erst in die gitignored .csproj eintragen, sonst meldet der Build trügerisch „erfolgreich". Ich prüfe, ob die Projektdateien existieren:

● Bash(cd "C:/Users/Ic3Dr4gon/Desktop/GameDevelopment/cozychristmas" && ls *.sln *.csproj 2>/dev/null; echo "---dotnet---"; which dotnet 2>/dev/null && dotnet --versio…)
⎿  Assembly-CSharp-Editor.csproj
Assembly-CSharp.csproj
CozySanta.Core.csproj
… +8 lines (ctrl+o to expand)

● csproj-Dateien existieren. Ich trage die vier neuen Dateien in die jeweiligen .csproj ein (MeltController ist schon drin). Zuerst das vorhandene Format ansehen:

● Bash(cd "C:/Users/Ic3Dr4gon/Desktop/GameDevelopment/cozychristmas" && grep -m2 "Compile Include" CozySanta.Core.csproj; echo "---count---"; for p in Core Runtime Edi…)
⎿      <Compile Include="Assets\_Project\Core\Progression\AreaProgress.cs" />
<Compile Include="Assets\_Project\Core\Interaction\InteractionCandidate.cs" />
---count---
… +4 lines (ctrl+o to expand)

● Bash(cd "C:/Users/Ic3Dr4gon/Desktop/GameDevelopment/cozychristmas" && \
grep -n "Compile Include" CozySanta.Core.csproj | grep -i "Snow" ; echo "--- check MeltContro…)
⎿  53:    <Compile Include="Assets\_Project\Core\Snow\CoverageProgress.cs" />
73:    <Compile Include="Assets\_Project\Core\Snow\MeltField.cs" />
82:    <Compile Include="Assets\_Project\Core\Snow\LampBattery.cs" />
… +2 lines (ctrl+o to expand)

● MeltController ist drin. Ich füge die vier neuen Dateien ein:

● Update(CozySanta.Core.csproj)
⎿  File must be read first

● Update(CozySanta.Runtime.csproj)
⎿  File must be read first

● Bash(cd "C:/Users/Ic3Dr4gon/Desktop/GameDevelopment/cozychristmas" && \
sed -i 's#<Compile Include="Assets\\_Project\\Core\\Snow\\LampBattery.cs" />#&\n    <Compile…)
⎿  === verify ===

● sed hat nicht gematcht (Backslash-Escaping). Ich prüfe die echten Zeilen und mache es mit PowerShell:

● PowerShell($repo = "C:\Users\Ic3Dr4gon\Desktop\GameDevelopment\cozychristmas"
$edits = @(…)
⎿  CozySanta.Core.csproj: inserted=True
CozySanta.Runtime.csproj: inserted=True
CozySanta.Editor.csproj: inserted=True
CozySanta.Tests.EditMode.csproj: inserted=True

● Alle vier eingetragen. Jetzt der Compile-Check über die Solution:

● Bash(cd "C:/Users/Ic3Dr4gon/Desktop/GameDevelopment/cozychristmas" && dotnet build cozychristmas.sln -nologo -clp:ErrorsOnly 2>&1 | tail -30)
⎿  C:\Users\Ic3Dr4gon\Desktop\GameDevelopment\cozychristmas\Assets\_Project\Runtime\Snow\LampVisuals.cs(64,22): error CS0234: Der Typ- oder Namespacename "deltaTime" ist im Namespace "CozySanta.Runtime.Time" nicht vorhand     
en. (Möglicherweise fehlt ein Assemblyverweis.) [C:\Users\Ic3Dr4gon\Desktop\GameDevelopment\cozychristmas\CozySanta.Runtime.csproj]
C:\Users\Ic3Dr4gon\Desktop\GameDevelopment\cozychristmas\Assets\_Project\Runtime\Snow\LampVisuals.cs(70,38): error CS0234: Der Typ- oder Namespacename "time" ist im Namespace "CozySanta.Runtime.Time" nicht vorhanden. (     
… +7 lines (ctrl+o to expand)
⎿  (timeout 5m)

● Namespenskollision mit CozySanta.Runtime.Time — wie im MeltController muss ich UnityEngine.Time voll qualifizieren:

● Update(Assets\_Project\Runtime\Snow\LampVisuals.cs)
⎿  Added 1 line, removed 1 line
61                  return;
62              }
63  
64 -            var dt = Time.deltaTime;                                                                                                                                                                                      
64 +            var dt = UnityEngine.Time.deltaTime;                                                                                                                                                                          
65              var frac = lamp.BatteryFraction;
66              var melting = lamp.IsMelting;
67

● Update(Assets\_Project\Runtime\Snow\LampVisuals.cs)
⎿  Added 1 line, removed 1 line
67  
68              var color = Color.Lerp(coldColor, warmColor, LampVisualMath.Warmth(frac));
69              var breathing = Mathf.Lerp(1f - pulseDepth, 1f + pulseDepth,
70 -                LampVisualMath.Pulse(Time.time, pulseFrequency));                                                                                                                                                         
70 +                LampVisualMath.Pulse(UnityEngine.Time.time, pulseFrequency));                                                                                                                                             
71  
72              var glowTarget = Mathf.Max(coldFloor,
73                  LampVisualMath.TargetLevel(frac, melting, idleEmission, activeEmission));

● Bash(cd "C:/Users/Ic3Dr4gon/Desktop/GameDevelopment/cozychristmas" && dotnet build cozychristmas.sln -nologo -clp:ErrorsOnly 2>&1 | tail -15)
⎿  Der Buildvorgang wurde erfolgreich ausgeführt.
0 Warnung(en)
0 Fehler
… +2 lines (ctrl+o to expand)
⎿  (timeout 5m)

● Build sauber: 0 Fehler, 0 Warnungen. Jetzt noch die Doku (Constitution: jede Änderung im selben Branch dokumentiert) — ein additiver Fortschritts-Eintrag in CLAUDE.md im Stil der anderen:

● Update(CLAUDE.md)
⎿  Added 11 lines
182    `FirstPersonController` um `jumpHeight` + `RequestJump()` erweitert, `PlayerInputRelay` liest die Leertaste.
183    EditMode-Tests J1–J5 grün.
184  
185 +- **Lampen-Optik (additiv zu F5)**: Core `LampVisualMath` (Decide, testbar) — reine Mathe für die Optik:                                                                                                                 
186 +  `Pulse` (Atmen 0..1), `Warmth` (Warm↔Kalt-Smoothstep aus Akku), `TargetLevel` (akku-skalierter Ziel-Pegel                                                                                                              
187 +  idle/aktiv), `SmoothTowards` (frame-rate-unabhängige Glättung). Runtime `LampVisuals` (Apply): liest                                                                                                                   
188 +  `MeltController.BatteryFraction` + neues `MeltController.IsMelting` und setzt live Emission des Funke-Renderers                                                                                                        
189 +  (MaterialPropertyBlock `_EmissionColor`), Kern-/Kegel-Lichtstärke und Funkel-Partikelrate — voll = warmes Gold                                                                                                         
190 +  & hell, leer = kühles Blau & matt, beim Schmelzen heller + mehr Funken, sanftes Pulsieren. Editor-Setup                                                                                                                
191 +  „CozySanta/Setup Lampe (Material, Lichter, Partikel)" erzeugt Materialien (`M_LampGehaeuse`, `M_LampFunke` mit                                                                                                         
192 +  HDR-Emission), baut ein Rig unter der Kamera (Platzhalter-Funke + Point/Spot-Light + Partikel) und verdrahtet                                                                                                          
193 +  `LampVisuals` mit dem `MeltController`. KI-Mesh wird später unter „Lampe" gehängt, Funke-Renderer auf das echte                                                                                                        
194 +  Kugel-Mesh umgesetzt. Bloom = manueller URP-2-Klick-Schritt (HDR-Emission liegt > 1). EditMode-Tests LV1–LV5 grün.                                                                                                     
195 +                                                                                                                                                                                                                         
196  ## Status / MVP-Fokus
197  
198  Erster Sektor (Eingangsbereich + Poststelle, optional Dekorationsfabrik) als

● Fertig. Hier die Zusammenfassung.

Was ich gebaut habe (Komplettpaket)

Alles konform zu eurer Architektur (Core testbar, Runtime Apply, Editor-Setup wie Setup F5). Build: 0 Fehler, 0 Warnungen.

┌───────────────────────────────────────┬──────────────────────┬──────────────────────────────────────────────────────────────────────────────────────────┐
│                 Datei                 │       Schicht        │                                          Inhalt                                          │
├───────────────────────────────────────┼──────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────┤
│ Core/Snow/LampVisualMath.cs           │ Core (rein, testbar) │ Pulse (Atmen), Warmth (Warm↔Kalt), TargetLevel (akku-skaliert), SmoothTowards (Glättung) │
├───────────────────────────────────────┼──────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────┤
│ Tests/EditMode/LampVisualMathTests.cs │ Tests                │ LV1–LV5                                                                                  │
├───────────────────────────────────────┼──────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────┤
│ Runtime/Snow/LampVisuals.cs           │ Runtime (Apply)      │ setzt Emission, Lichtstärke, Partikelrate live aus Akku + Schmelz-Zustand                │
├───────────────────────────────────────┼──────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────┤
│ Runtime/Snow/MeltController.cs        │ Runtime              │ additiv: IsMelting-Property                                                              │
├───────────────────────────────────────┼──────────────────────┼──────────────────────────────────────────────────────────────────────────────────────────┤
│ Editor/LampSetup.cs                   │ Editor               │ Menü CozySanta/Setup Lampe …                                                             │
└───────────────────────────────────────┴──────────────────────┴──────────────────────────────────────────────────────────────────────────────────────────┘

Dein Workflow danach

1. Setup ausführen: Unity-Menü → CozySanta/Setup Lampe (Material, Lichter, Partikel). Erstellt 2 Materialien (M_LampGehaeuse, M_LampFunke mit HDR-Emission) und ein funktionierendes Rig unter der Kamera — mit Platzhalter-Funke,
   Point-Light, Spot-Light (Kegel) und Funkel-Partikeln. Leuchtet sofort, auch ohne dein Mesh.
2. KI-Mesh importieren → unter das Lampe-Objekt hängen. Gehäuse-Teil bekommt M_LampGehaeuse, die Kristallkugel M_LampFunke.
3. Platzhalter-Funke ausblenden/löschen und im LampVisuals-Inspector orbRenderer auf das echte Kugel-Mesh setzen — fertig.
4. Bloom (das weiche Glühen): einmalig im URP-Profil ein Global Volume + Bloom-Override anschalten. Das mache ich bewusst nicht per Code (URP-Bloom-Typ ist im Editor-asmdef nicht garantiert referenziert → hätte den
   Zero-Compile-Error-Anspruch gefährdet). Die HDR-Emission liegt schon über 1, Bloom greift also sofort.

Verhalten zur Laufzeit (automatisch gekoppelt)

- Akku voll → warmes Gold, hell. Leer → kühles Blau, matt (mit kleinem coldFloor, damit das Blau sichtbar bleibt statt schwarz).
- F gedrückt (schmelzen) → Funke + Kegel heller, mehr Funken.
- Sanftes „Atmen" über den Puls; alle Übergänge weich (kein hartes Umschalten).

Alle Tuning-Werte (Farben, Intensitäten, Puls-Tempo, Glättung) sind im LampVisuals-Inspector einstellbar.

Soll ich dir noch eine kurze Klick-Anleitung speziell für den Bloom-Schritt schreiben (welches Asset, welche Werte)?

