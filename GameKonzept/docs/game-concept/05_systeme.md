# Systeme

## Area-System

Das Spiel ist in Areas unterteilt. Jede Area verwaltet eigene Aufgaben und Fortschrittswerte.

Moegliche Area-Daten:

- Name,
- Sektor,
- Schnee-Fortschritt,
- Sortierziele,
- Montageziele,
- Belohnung,
- Schluesselstatus,
- Freischaltbedingungen.

## Aufgaben-System

Jede Area hat eine Aufgabenliste, die oben rechts angezeigt wird. Aufgaben koennen prozentual oder als Zaehler dargestellt werden.

Es gibt keine zeitbasierten Pflichtziele. Die Aufgabenliste zeigt Fortschritt und Orientierung, erzeugt aber keinen Timerdruck.

Aufgabentypen:

- Schnee schmelzen,
- Objekte sortieren,
- Objekte montieren,
- Schluessel erhalten,
- Tor oeffnen,
- Kugeln finden,
- Kugeln anbringen.

Die aktuellen Content-Mengen sind bewusst gross und muessen spaeter als Balancingwerte behandelt werden. Der Umfang muss zur Zielspielzeit von ca. 4-8 Stunden passen.

## Sortierfeedback-System

Sortierorte wie Lager, Regale, Faechern, Container oder aehnliche Zielbereiche besitzen eine sichtbare Lampe. Diese Lampe zeigt an, ob der jeweilige Sortierort vollstaendig und korrekt befuellt ist.

Regel:

- Solange Objekte fehlen oder falsch einsortiert sind, bleibt die Lampe aus oder neutral.
- Sobald der Zielort vollstaendig und korrekt ist, leuchtet die Lampe auf.
- Dieses Feedback zaehlt als lokaler Abschlussmoment und unterstuetzt die Aufgabenanzeige.
- Falsch einsortierte Objekte werden ausserhalb der Geschenkcontainer nicht automatisch entfernt.
- Der Spieler erkennt Fehler daran, dass die Lampe aus bleibt und der Zielort nicht als abgeschlossen zaehlt.

Designfunktion:

- Der Spieler bekommt direkte Rueckmeldung im Raum.
- Fertige Bereiche werden heller und belebter.
- Das System passt zur allgemeinen Fortschrittsidee mit warmem Licht.
- Fehler bleiben korrigierbar, ohne harte Strafe oder Zeitdruck.

## XP-System

XP wird fuer unterschiedliche Aktionen vergeben und geht auf ein gemeinsames Konto. Levelaufstiege geben Skillpunkte.

Moegliche XP-Quellen:

| Aktion | XP-Rolle |
| --- | --- |
| Schnee schmelzen | stetiger kleiner Fortschritt |
| Objekt korrekt einsortieren | regelmaessige Belohnung |
| Montage abschliessen | mittlere Belohnung |
| Area abschliessen | grosse Belohnung |
| Schluessel erhalten | Meilenstein |
| Christbaumkugel anbringen | finale Belohnung |

## Sammelobjekt-System

Die finalen Christbaumkugeln sind die einzigen Sammelobjekte. Sie werden unter dem Schnee auf dem finalen Platz gefunden, einzeln entdeckt und dauerhaft sichtbar am zentralen Weihnachtsbaum angebracht. Sie sind damit nicht nur Sortierware, sondern finale Fund- und Abschlussobjekte.

Unterschied zu Geschenken:

- Geschenke sind viele funktionale Sortierobjekte in der Produktionskette.
- Christbaumkugeln sind begrenzte finale Fundobjekte.
- Geschenke verschwinden oder gelten nach dem Einsortieren als erledigt.
- Christbaumkugeln bleiben am Baum sichtbar und zeigen den Endfortschritt.
- Die letzte Kugel markiert zusammen mit dem freien Platz das Spielende.

## Geschenkcontainer-System

Die Geschenksortierung nutzt Container als Batch-System.

Regeln:

- Ein Container fasst 25 Geschenke.
- Der Spieler fuellt den Container mit Geschenken.
- Danach kann der Container abgeschlossen werden.
- Wenn alle Geschenke im Container korrekt sind, verschwinden sie und der Container oeffnet sich wieder.
- Wenn mindestens ein falsches Geschenk enthalten ist, werden die Geschenke ueber ein Rohr an der Decke wieder ausgespuckt.
- Ausgespuckte Geschenke muessen erneut eingesammelt und korrekt sortiert werden.
- Das Abschliessen eines Containers hat einen Cooldown von 1 Minute.

Designfunktion:

- Der Spieler arbeitet in klaren 25er-Paketen.
- Fehler erzeugen sanftes, sichtbares Feedback statt Game Over oder Zeitstrafe.
- Der Cooldown verhindert hektisches Testen und haelt die Sortierentscheidung relevant.

## Skill-System

Alle Skills nutzen denselben Fortschrittspool. Der Spieler entscheidet bei jedem Levelaufstieg frei, welche verfuegbare Faehigkeit verbessert wird. Es gibt keinen Skilltree mit festen Abhaengigkeiten.

Die Skillprogression ist kleinteilig. Jede Skilloption soll ungefaehr 20 Upgrade-Stufen haben. Einzelne Stufen sind entsprechend eher kleine Verbesserungen, die sich ueber mehrere Investitionen hinweg deutlich aufsummieren.

Designregeln:

- Skillpunkte duerfen frei investiert werden.
- Upgrades sollen nicht gegenseitig blockieren.
- Neue Faehigkeiten wie Sortierblick oder Objektanziehung koennen als einzelne Skills freischaltbar sein.
- Nach der Freischaltung koennen diese Faehigkeiten frei weiter verbessert werden.
- Keine Skillwahl darf einen spaeteren Fortschritt unmoeglich machen.
- Jede Stufe soll spuerbar genug sein, um Belohnung zu erzeugen, aber klein genug, um haeufige Levelaufstiege zu erlauben.

Skillgruppen:

- Lampe,
- Tragen,
- Sortierblick,
- Objektanziehung,
- Bewegung.

Vorlaeufige Upgrade-Skalierung:

| Skilloption | Zielanzahl Stufen | Skalierungsart |
| --- | ---: | --- |
| Lampen-Power | ca. 20 | kleine Steigerung der Schmelzgeschwindigkeit pro Stufe |
| Lichtkegel | ca. 20 | kleine Verbreiterung/Verlaengerung pro Stufe |
| Akku | ca. 20 | kleine Erhoehung der Nutzungsdauer pro Stufe |
| Tragkraft | ca. 20 | schrittweise Erhoehung bis maximal 25 kg |
| Bewegung | ca. 20 | kleine Speed-Steigerung pro Stufe |
| Sortierblick-Dauer | ca. 20 | laengere Anzeigezeit pro Stufe |
| Sortierblick-Cooldown | ca. 20 | kuerzerer Cooldown pro Stufe |
| Objektanziehung-Cooldown | ca. 20 | kuerzerer Cooldown pro Stufe |
| Objektanziehung-Staerke | ca. 20 | mehr Reichweite oder mehr Objekte pro Stufe |

## Gewichtssystem

Objekte haben ein Gewicht. Der Spieler hat eine maximale Traglast, die durch Skills bis 25 kg steigt. Die Anzahl tragbarer Objekte ergibt sich aus der Summe der Objektgewichte.

Beispielhafte Entwurfswerte:

| Objekt | Gewicht |
| --- | ---: |
| Brief | 0.1 kg |
| kleiner Dekoartikel | 0.5 kg |
| grosser Dekoartikel | 1.0 kg |
| Werkzeug | 2.0 kg |
| Rohrsegment | 4.0 kg |
| Geschenk | 1.0-5.0 kg |
| Lagerbox | 8.0 kg |

Diese Werte sind Platzhalter und muessen spaeter gebalanced werden.

## Cooldown-System

Sortierblick und Objektanziehung haben Cooldowns.

Cooldowns dienen dem Rhythmus und Balancing der Faehigkeiten, nicht als Stressmechanik. Sie sollen kurz genug sein, um cozy zu bleiben, aber lang genug, damit Upgrades spuerbar sind.

Sortierblick-Parameter:

- Dauer,
- Cooldown,
- Zielanzeige fuer linkes Objekt.

Objektanziehung-Parameter:

- Cooldown,
- maximale Anzahl,
- Reichweite,
- erlaubte Objektarten,
- Bezug zum linken Objekt.

## Akku-System

Die Lampe hat einen Akku. Der Akku sinkt bei Nutzung und kann an jedem Gebaeude in kurzer Zeit aufgeladen werden.

Designziel:

- Der Akku soll leichte Routenplanung erzeugen.
- Aufladen soll nicht lange genug dauern, um das cozy Tempo zu brechen.
- Upgrades sollen die Unterbrechungen reduzieren.

## Schnee-Maskensystem

Das Schneeschmelzen soll voraussichtlich ueber eine Maske umgesetzt werden. Der Lampenkegel schreibt in diese Maske und legt die beschienene Flaeche frei.

Zielverhalten:

- Schnee verschwindet nur im Bereich des Lampenkegels.
- Bereits freigelegte Flaechen bleiben frei.
- Der Schmelzrand ist weich und organisch statt hartkantig.
- Noise oder aehnliche visuelle Modulation kann den Rand natuerlicher machen.
- Das System muss pro Area Fortschritt auswerten koennen.

Noch zu pruefen:

- ob die Maske pro Schneeflaeche, pro Area oder global verwaltet wird,
- wie XP aus geschmolzener Flaeche berechnet wird,
- wie Performance bei grossen Flaechen stabil bleibt,
- wie gefundene Objekte unter Schnee durch die Maske freigelegt werden.

## Schluessel- und Tor-System

Jeder der ersten beiden Sektoren hat zwei Gebaeude, die je einen Schluessel geben. Zwei Schluessel oeffnen zwei Tore zwischen den Gebaeuden und schalten den naechsten Sektor frei.

Systemregeln:

- Gebaeude abgeschlossen -> Schluessel erhalten.
- Schluessel passt zu einem konkreten Tor.
- Beide Tore muessen offen sein, um weiterzukommen.

Offen:

- Ob die Reihenfolge der Tore frei waehlbar ist.
- Ob Schluessel automatisch genutzt oder manuell eingesetzt werden.
