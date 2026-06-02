# Phase 0 – Research: F6 – XP- & Skillsystem

Entscheidungen aus Konzept (03/05/08) + bisherigen Features. Keine offenen `NEEDS CLARIFICATION`.

## R1 – XP/Level als Core mit einfacher Kurve

- **Entscheidung**: `XpLedger` (Core) hält den gemeinsamen XP-Stand und leitet das Level über eine monoton
  steigende Schwellenfunktion ab (Platzhalter, z. B. `xpForLevel(n) = base * n` oder leicht progressiv). Bei
  Gutschrift werden ggf. mehrere Level auf einmal überschritten; pro Level wird eine feste Zahl Skillpunkte vergeben.
- **Begründung**: Erfüllt Prinzip IX (testbar) und das Konzept (gemeinsamer Pool, häufige kleine Level-ups). Die
  Kurve ist als Platzhalter klar austauschbar fürs spätere Balancing (08).
- **Alternativen verworfen**: feste XP-Tabelle pro Level (unflexibler beim Balancing) – kann später ergänzt werden.

## R2 – Skill-Wertmodell (Basis + Schritt, gedeckelt)

- **Entscheidung**: `Skill` hält `Level` (0..`MaxLevel` ~20) und berechnet `Value = Basis + Schritt·Level`, optional
  gedeckelt (z. B. Tragkraft ≤ 25 kg). Werte/Schritte sind Konfigurationsparameter.
- **Begründung**: Kleinteilige, vorhersagbare Steigerung pro Stufe (Konzept: ~20 Stufen, kleine Schritte). Deckelung
  fängt harte Grenzen ab (Tragkraft 25 kg).
- **Offen (Balancing)**: konkrete Basis/Schritt/Max je Option sind Platzhalter, später zu tunen.

## R3 – Freie Investition ohne Tree + Freischalt-Skills

- **Entscheidung**: `SkillSet` erlaubt `Invest(id)`, solange verfügbare Punkte > 0 und `Level < MaxLevel`; keine
  Abhängigkeiten zwischen Optionen. Freischalt-Skills (Sortierblick, Objektanziehung) starten „gesperrt/Stufe 0";
  die erste Investition schaltet frei (`Unlocked`), danach normal steigerbar.
- **Begründung**: Konzept: kein fester Skilltree, freie Wahl; Freischaltung als erste Stufe ist die einfachste,
  konsistente Modellierung.
- **Abgrenzung**: Die Fähigkeiten selbst (Sortierblick-Anzeige F9, Objektanziehung F10) sind NICHT Teil von F6; F6
  liefert nur Stufe/Unlock-Status als Andockpunkt.

## R4 – XP-Quellen-Anbindung

- **Entscheidung**: Runtime `PlayerProgression` abonniert vorhandene Quellen:
  - **Sortieren (F4)**: `SortTargetInteractable.onCompleted` → feste „Sortieren"-XP.
  - **Schmelzen (F5)**: aus dem Zuwachs der `SnowPatch.Coverage` (Delta) → „Schmelzen"-XP proportional zur neu
    freigelegten Fläche.
  - Weitere (Area/Schlüssel/Kugel) als Methoden-Andockpunkte (`AwardXp(source, amount)`), Quellen folgen F7/F8/F13.
- **Begründung**: Nutzt die in F4/F5 bewusst angelegten Haken; keine Änderung an deren Verträgen.

## R5 – Skillwerte auf Komponenten anwenden (Apply)

- **Entscheidung**: `PlayerProgression` berechnet bei jeder Investition (oder on-demand) die Skillwerte und setzt sie:
  - Tragkraft → `PlayerCarry.Capacity`.
  - Lampe → `MeltController` (Akku-Kapazität; Schmelzstärke/-radius als „Power/Kegel").
  - Bewegung → Laufgeschwindigkeit (F2-Controller; ggf. kleines Setter-Property ergänzen, falls nicht vorhanden).
- **Begründung**: Hält die Wertquelle (Core) und die Anwendung (Runtime) getrennt; die Komponenten bleiben Eigentümer
  ihres Laufzeitverhaltens, F6 setzt nur Parameter.
- **Hinweis**: Falls der F2-Bewegungs-Controller keinen öffentlichen Speed-Setter hat, wird er additiv ergänzt
  (kleiner, nicht-brechender Zusatz).

## R6 – UI editor-authored + Dev-Tool

- **Entscheidung**: Das Skillmenü (Anzeige Level/XP/Punkte, Investitions-Buttons) wird **editor-authored** als
  Szene/Prefab erstellt; Laufzeitcode bindet/aktualisiert nur (Constitution V). Für den Test gibt es ein **IMGUI-Dev-Tool**
  (analog `DevSpawnMenu`), das XP gutschreibt und investiert.
- **Begründung**: Constitution-konform; das Dev-Tool ermöglicht Testen, bevor das finale Menü gebaut ist.
