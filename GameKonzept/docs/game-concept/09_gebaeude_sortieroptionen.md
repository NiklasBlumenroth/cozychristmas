# Gebaeude und Sortieroptionen

Diese Datei sammelt die Sortierkategorien pro Gebaeude. Mengen sind aktuelle Konzeptwerte und muessen spaeter gegen Zielspielzeit, Laufwege, Tragkraft und Skillprogression gebalanced werden.

## Designregeln fuer Sortierung

- Sortierkategorien muessen in First-Person schnell lesbar sein.
- Kategorien sollten ueber Form, Farbe, Symbol oder Beschriftung eindeutig erkennbar sein.
- Jedes Regal, Lager, Fach, jeder Container oder aehnliche Zielort besitzt eine Lampe.
- Die Lampe leuchtet auf, sobald der Zielort vollstaendig und korrekt befuellt ist.
- Sortierung soll pro Sektor komplexer werden, aber nicht stressig.

## Poststelle

Objekte:

- Briefe.

Kategorien:

- Kontinent: America, Europe, Asia, Africa, Australia.
- Farbe: rot, gelb, gruen, blau.
- Symbol: Teddy, Eisenbahn, Buch, Gitarre, Stern.

Kombinationsregel:

- Jeder Brief besitzt genau eine Auspraegung aus jeder Kategorie.
- Beispiel: blauer Brief mit Africa als Anschrift und Teddy-Symbol.
- Jede Kombination kommt 25-mal vor.

Mengen:

- 5 Kontinente x 4 Farben x 5 Symbole = 100 Kombinationen.
- 100 Kombinationen x 25 Briefe = 2.500 Briefe.

Sortierlogik:

- Der Spieler sortiert Briefe nach der vollen Kombination aus Kontinent, Farbe und Symbol.
- Sortierfaecher oder Briefkaesten muessen die Kombination klar lesbar anzeigen.
- Sobald ein Ziel vollstaendig und korrekt befuellt ist, leuchtet die zugehoerige Lampe.

## Dekorationsfabrik

Objekte:

- Christbaumkugeln,
- Zuckerstangen,
- Kraenze.

Kategorien und Mengen:

| Objekt | Varianten | Menge je Variante | Gesamt |
| --- | --- | ---: | ---: |
| Christbaumkugel | rot, gruen, gelb, blau, lila, pink, orange | 100 | 700 |
| Zuckerstange | rot-weiss, weiss-blau, weiss-gruen, weiss-orange | 100 | 400 |
| Kranz | 3 blaue Schleifen, 4 gruene Schleifen, 5 orangene Schleifen, 6 weisse Schleifen | 100 | 400 |

Gesamtmenge:

- 1.500 Dekoobjekte.

Sortierlogik:

- Christbaumkugeln werden nach Farbe sortiert.
- Zuckerstangen werden nach Farbkombination sortiert.
- Kraenze werden nach Anzahl und Farbe der Schleifen sortiert.
- Die finalen Christbaumkugeln auf dem Endplatz sind davon getrennt und bleiben die einzigen finalen Sammelobjekte.

## Werkstatt

Objekte:

- Rohre,
- Werkzeuge,
- Zahnraeder.

Rohr-Kategorien:

- 5 unterschiedliche Durchmesser.
- Je Durchmesser 100 Rohre.
- Gesamt: 500 Rohre.

Rohr-Montage:

- Rohre muessen an einer Wand angebracht werden.
- Ziel ist, Anfang und Ende der Rohrstrecke zu verbinden.
- Der korrekte Durchmesser muss zur jeweiligen Leitung passen.
- Eine abgeschlossene Verbindung kann Licht oder Maschinenfeedback aktivieren.

Werkzeug-Kategorien:

- kleiner Hammer,
- grosser Hammer,
- Schraubenzieher,
- 3 unterschiedliche Saegen,
- 3 unterschiedliche Zangen,
- 3 unterschiedliche Schraubenschluessel.

Werkzeug-Mengen:

- 12 Werkzeugvarianten.
- Jede Werkzeugvariante ist 50-mal vorhanden.
- Gesamt: 600 Werkzeuge.

Werkzeug-Sortierlogik:

- Es gibt viele Werkzeugwaende.
- Die Silhouetten an den Werkzeugwaenden zeigen, welches Werkzeug dort hingehoert.
- Fertige Werkzeugbereiche leuchten ueber die jeweilige Lampe auf.

Zahnraeder:

- 100 Zahnraeder.
- Zahnraeder werden an einer Wand angebracht.
- Die Zahnradwand kann als eigener Fortschrittsblock zaehlen.

Gesamtmenge Werkstatt:

- 500 Rohre,
- 600 Werkzeuge,
- 100 Zahnraeder,
- insgesamt 1.200 Objekte.

## Lagerhalle

Objekte:

- Lagerboxen.

Kategorien:

- 5 unterschiedliche Boxformen.
- Symbole: Brief, Kranz, Christbaumkugel, Zahnrad, Hammer.

Kombinationsregel:

- Jede Box besitzt eine Form und ein Symbol.
- Jede Kombination kommt 75-mal vor.

Mengen:

- 5 Formen x 5 Symbole = 25 Kombinationen.
- 25 Kombinationen x 75 Boxen = 1.875 Boxen.

Sortierlogik:

- Boxen werden nach Kombination aus Form und Symbol einsortiert.
- Regale muessen Form und Symbol klar anzeigen.
- Eine Lampe am Regal leuchtet, sobald die korrekte Kombination vollstaendig ist.

## Geschenksortierung

Objekte:

- Geschenke.

Kategorien:

- 5 Farben.
- Jede Farbe hat 500 Geschenke.

Mengen:

- 5 Farben x 500 = 2.500 Geschenke.

Container-Logik:

- Der Spieler kann immer 25 Geschenke in einen Container packen.
- Danach kann der Container abgeschlossen werden.
- Wenn der Container nur richtige Geschenke enthaelt, verschwinden diese Geschenke und der Container oeffnet sich wieder.
- Wenn der Container falsche Geschenke enthaelt, fallen die Geschenke ueber ein Rohr an der Decke wieder heraus und muessen neu eingesammelt werden.
- Das Abschliessen eines Containers hat einen Cooldown von 1 Minute.

Designfunktion:

- Der Container ist ein pruefbarer Batch von 25 Geschenken.
- Falsche Sortierung bestraft nicht hart, kostet aber einen neuen Sammelweg.
- Der 1-Minuten-Cooldown verhindert zu schnelles Wiederholen und gibt der Sortierung Rhythmus.

## Finaler Platz

Objekte:

- Christbaumkugeln unter Schnee,
- Weihnachtsbaum im Zentrum.

Funktion:

- Die Kugeln sind finale Sammel- und Einsetzobjekte, keine normalen Sortierobjekte.
- Jede Kugel wird gefunden, getragen und am Baum angebracht.
- Der Baum zeigt den Sammlungsfortschritt dauerhaft sichtbar.
- Es gibt keine weiteren Sammelobjekte ausser diesen finalen Christbaumkugeln.

Kategorien:

- mehrere unterschiedliche Kugelfarben.
- Farben dienen der visuellen Abwechslung, nicht als Sortierregel.

Finale Logik:

- Kugeln muessen nicht einsortiert werden.
- Jede Kugel wird nur zum Baum gebracht und dort angebracht.
- Jede Kugel sollte einen sichtbaren Licht-/Klangmoment am Baum ausloesen.
- Die letzte Kugel loest zusammen mit dem vollstaendig freigeschmolzenen Platz das Spielende aus.

## Mengenuebersicht

| Bereich | Menge |
| --- | ---: |
| Poststelle | 2.500 Briefe |
| Dekorationsfabrik | 1.500 Dekoobjekte |
| Werkstatt | 1.200 Objekte |
| Lagerhalle | 1.875 Boxen |
| Geschenksortierung | 2.500 Geschenke |

Hinweis: Diese Mengen ergeben einen sehr grossen Sortierumfang. Damit 4-8 Stunden Zielspielzeit realistisch bleiben, muessen Tragkraft, Objektanziehung, Laufwege, Containergroesse, XP-Rhythmus und Sortierblick stark auf diese Mengen abgestimmt werden.
