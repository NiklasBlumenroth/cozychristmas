# Gameplay

## Grundstruktur

Der Spieler bewegt sich in First-Person durch ein Fabrikgelaende mit mehreren Sektoren. Jeder Sektor besteht aus einer Aussenflaeche und Gebaeuden. Draussen wird Schnee mit einer magischen Lampe geschmolzen. In den Gebaeuden werden Objekte aufgenommen, getragen, sortiert oder montiert.

Der Fortschritt ist area-basiert. Oben rechts sieht der Spieler die Aufgaben der aktuellen Area und ihren Erledigungsstand.

## Core Loop

1. Area betreten.
2. Aufgaben oben rechts pruefen.
3. Schnee schmelzen oder Objekte suchen.
4. Objekte korrekt sortieren, montieren oder ablegen.
5. XP durch erledigte Aktionen sammeln.
6. Levelaufstieg erreichen und Skill verbessern.
7. Area-Aufgaben abschliessen.
8. Schluessel, Zugang oder naechstes Ziel erhalten.

## Meta Loop

1. Sektor freiraeumen.
2. Gebaeude im Sektor abschliessen.
3. Zwei Schluessel erhalten.
4. Tore zwischen den Gebaeuden oeffnen.
5. Naechsten Sektor freischalten.
6. Staerkere Skills nutzen, um groessere Aufgaben effizienter zu erledigen.

## Hauptaktionen

### Bewegen

Der Spieler laeuft mit WASD ueber das Fabrikgelaende und durch Innenraeume. Die Kamera ist First-Person, wodurch Lampe, linke Hand, rechte Hand und Sortierziele direkt im Blickfeld des Spielers liegen. Die Bewegungsgeschwindigkeit kann durch Skillpunkte verbessert werden.

Wichtige Designfunktion:

- Langsamer Start fuer cozy Tempo.
- Spaetere Upgrades reduzieren Wiederholungsfrust.
- Breitere und laengere Wege in Sektor 2 machen Speed-Upgrades wertvoll.
- First-Person macht das Aufnehmen, Tragen und Platzieren unmittelbarer.

### Blick und Interaktion

Da das Spiel in First-Person gespielt wird, sollten Interaktionen ueber Blickrichtung und Reichweite funktionieren.

Grundprinzip:

- Der Spieler schaut ein Objekt oder Ziel an.
- Ein dezenter Interaktionshinweis erscheint.
- Per Interaktionstaste wird aufgenommen, abgelegt, eingesetzt, sortiert oder montiert.

Offene Tastenbelegung:

- Interaktion,
- Ablegen,
- Lampe aktivieren,
- Sortierblick,
- Objektanziehung,
- Skill-/Menueoeffnung.

Designfunktion:

- First-Person erlaubt praezises Platzieren.
- Die Hand-/Stapelanzeige kann direkt im HUD oder als sichtbare Haende umgesetzt werden.
- Sortierziele muessen aus Spielersicht gut lesbar sein, ohne das Bild zu ueberladen.

### Schnee schmelzen

Der Spieler erhaelt zu Beginn eine magische Lampe. Diese schmilzt Schnee in einem Lichtkegel vor dem Spieler.

Die angestrebte technische Richtung ist ein maskenbasiertes Schmelzsystem: Der Bereich im Lampenkegel wird in einer Maske freigelegt, sodass genau dort Schnee verschwindet, wo der Spieler hinleuchtet. Die Raender sollen nicht hart ausgeschnitten wirken, sondern ueber weiche Uebergaenge, Noise oder aehnliche Shader-/Maskeneffekte organisch ausfransen.

Startzustand:

- kleiner Lichtkegel,
- geringe Schmelzleistung,
- begrenzter Akku,
- langsames Freilegen einzelner Flaechen.

Verbesserungen:

- mehr Lampen-Power,
- groesserer Lichtkegel,
- groesserer Akku.

Der Akku kann an jedem Gebaeude in kurzer Zeit wieder aufgeladen werden.

Designfunktion:

- Schnee ist ein sichtbares Fortschrittsmedium.
- Die Lampe macht Fortschritt direkt erfahrbar.
- Upgrades sind sofort spuerbar.
- Der freigelegte Bereich folgt sichtbar dem Lampenkegel.
- Weiche/noisy Raender verhindern einen technischen, scharfkantigen Look.
- Akku erzeugt leichte Routenplanung, ohne das Spiel hart zu bestrafen.

Status: Die konkrete Unity-Umsetzung des Maskensystems muss noch ausgearbeitet und prototypisch geprueft werden.

### Objekte aufnehmen

Der Spieler kann Objekte aufnehmen und tragen. Zu Beginn kann er nur sehr leichte Dinge sinnvoll bewegen, etwa einzelne Briefe. Durch Skillpunkte steigt die maximale Traglast bis auf 25 kg.

Die Anzahl tragbarer Objekte ergibt sich aus:

- maximaler Traglast des Spielers,
- Gewicht des einzelnen Objekts,
- aktuellen Stapelregeln,
- moeglichen Skill-Upgrades.

Beispiele:

| Objektart | Rolle | Gewichtseindruck |
| --- | --- | --- |
| Briefe | leichteste Sortierobjekte | sehr leicht |
| Dekoartikel | leichte bis mittlere Sortierobjekte | leicht |
| Werkzeuge | mittlere Objekte | mittel |
| Rohre | Montageobjekte | mittel bis schwer |
| Lagerboxen | schwere Sortierobjekte | schwer |
| Geschenke | finale Massenobjekte | variabel |

### Stapel- und Handlogik

Das Tragen folgt einer klaren visuellen Logik mit linker und rechter Hand.

Grundregel:

- Das zuletzt aufgenommene Objekt liegt in der linken Hand.
- Das zuvor gehaltene Objekt wandert nach rechts oder in den Stapel.
- Beim Ablegen wird die Reihenfolge rueckwaerts abgearbeitet.

Beispiel mit drei Objekten:

1. Spieler hebt Objekt A auf.
   - A ist links sichtbar.
2. Spieler hebt Objekt B auf.
   - A wandert in die rechte Hand.
   - B liegt links.
3. Spieler hebt Objekt C auf.
   - B wandert unter A in den rechten Stapel.
   - C liegt links.

Beim Ablegen:

1. C wird zuerst abgelegt.
2. Danach rutscht B aus dem Stapel in die linke Hand.
3. Danach kann B abgelegt werden.
4. Danach rutscht A nach.

Designfunktion:

- Der Spieler sieht immer klar, welches Objekt als naechstes relevant ist.
- Die linke Hand bestimmt Sortierblick und Zielanzeige.
- Das System erlaubt spaeter groessere Stapel, ohne die Grundregel zu aendern.

### Sortieren

In Gebaeuden muessen Objekte an korrekte Zielorte gebracht werden. Der Sortierblick kann freigeschaltet werden und zeigt fuer das Objekt in der linken Hand visuell an, wo es einsortiert werden muss.

Jedes Lager, Regal, jeder Container oder vergleichbare Sortierort nutzt eine Lampe als klares Abschlussfeedback. Sobald alle zugehoerigen Objekte vollstaendig und richtig einsortiert sind, leuchtet diese Lampe auf.

Falsch einsortierte Objekte werden ausserhalb der Geschenkcontainer nicht hart verhindert und nicht automatisch zurueckgeworfen. Sie duerfen am Zielort liegen bleiben, aber der Zielort gilt nicht als korrekt abgeschlossen und die Lampe bleibt aus.

Der Sortierblick:

- betrifft das aktuell linke Objekt,
- zeigt ein visuelles Zeichen am Zielort,
- hat einen Cooldown,
- hat eine begrenzte Dauer,
- kann per Skill verbessert werden.

Skill-Verbesserungen:

- Cooldown senken,
- Dauer verlaengern.

Designfunktion:

- Der Spieler kann ohne harte Questmarker suchen.
- Der Sortierblick dient als Hilfsmittel, nicht als Dauer-Autopilot.
- Die linke-Hand-Regel gibt dem System Klarheit.
- Lampen an Sortierorten geben positives, raeumliches Feedback fuer korrekte Vollstaendigkeit.
- Falsche Sortierung wird sanft ueber ausbleibendes Lampenfeedback kommuniziert.

### Objekte anziehen lassen

Der Spieler kann eine Faehigkeit freischalten, die eine gewisse Anzahl passender Objekte zu ihm fliegen laesst. Grundlage ist das Objekt in seiner linken Hand.

Beispiel:

- Spieler haelt einen bestimmten Brieftyp links.
- Faehigkeit wird aktiviert.
- Eine begrenzte Anzahl weiterer Briefe dieser Art fliegt zum Spieler.

Die Faehigkeit:

- hat einen Cooldown,
- kann per Skill im Cooldown verbessert werden,
- muss vermutlich eine maximale Reichweite und maximale Objektanzahl haben,
- sollte nur Objekte erfassen, die fuer die aktuelle Aufgabe sinnvoll sind.

Designfunktion:

- Reduziert spaeteres Kleinteile-Sammeln.
- Macht Skillfortschritt deutlich.
- Gibt dem Spieler ein magisches Werkzeug passend zum Weihnachtssetting.

### Montieren

In der Werkstatt sortiert der Spieler Werkzeuge und montiert Rohre. Die Rohrmontage ist eine eigene Interaktionsform und sollte sich leicht vom reinen Sortieren unterscheiden.

Moegliche Ausgestaltung:

- Rohre werden an markierte Anschlussstellen gebracht.
- Rohrtypen muessen zu Form, Farbe oder Symbol passen.
- Ein Rohr wird nach dem Platzieren sichtbar verbunden.
- Eine abgeschlossene Rohrstrecke zaehlt als Area-Aufgabe.

Status: Mechanik noch zu konkretisieren.

## Aufgabenanzeige

Oben rechts sieht der Spieler fuer die aktuelle Area:

- aktuelle Aufgaben,
- Anzahl erledigter Sortierziele,
- Fortschritt beim Schneefeld,
- erhaltene oder fehlende Schluessel,
- moegliche finale Area-Ziele.

Beispiel:

```text
Poststelle
- Briefe sortieren: 18 / 30
- Verlorene Pakete finden: 1 / 2
- Bereich aufgeraeumt: 63%
```

Die Anzeige sollte knapp bleiben und immer nur die relevante Area abbilden.

## Erfahrung und Levelaufstieg

Der Spieler sammelt XP durch:

- Schnee schmelzen,
- Objekte korrekt sortieren,
- Montageziele abschliessen,
- Area-Aufgaben beenden,
- Schluessel erhalten,
- Christbaumkugeln finden oder anbringen.

Alle XP gehen auf ein gemeinsames Konto. Bei jedem Levelaufstieg erhaelt der Spieler Skillpunkte und kann eine beliebige verfuegbare Faehigkeit verbessern. Es gibt keinen Skilltree mit festen Abhaengigkeiten.

Die Progression soll aus vielen kleinen Upgrades bestehen. Pro Skilloption sind als Zielgroesse etwa 20 Upgrade-Stufen vorgesehen. Dadurch bekommt der Spieler haeufig Fortschritt, ohne dass einzelne Upgrades das Balancing sprunghaft veraendern.

## Skillrichtungen

### Lampe

- Power: Schnee schmilzt schneller.
- Kegelgroesse: mehr Flaeche wird gleichzeitig getroffen.
- Akku: laengere Nutzung vor dem Aufladen.

### Tragen

- maximale Traglast bis 25 kg.
- indirekt mehr Objekte pro Lauf.
- wichtig fuer schwere Objekte wie Boxen.

### Sortierblick

- Freischaltung.
- laengere Dauer.
- geringerer Cooldown.

### Objektanziehung

- Freischaltung.
- geringerer Cooldown.
- eventuell hoehere Objektanzahl.
- eventuell groessere Reichweite.

### Bewegung

- hoehere Laufgeschwindigkeit.
- wichtig fuer groessere Areale und spaetere Lager-/Geschenkaufgaben.

## Erfolg und Spielende

Das Spiel endet, wenn:

- der finale Platz komplett vom Schnee befreit wurde,
- alle Christbaumkugeln unter dem Schnee gefunden wurden,
- alle Kugeln am Weihnachtsbaum im Zentrum angebracht wurden.

Das finale Bild sollte klar zeigen, dass die Fabrik gerettet ist.
