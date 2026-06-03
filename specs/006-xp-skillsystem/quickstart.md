# Quickstart: F6 XP & Skills testen

## Einrichten

1. Am `Player` die Komponente **`PlayerProgression`** ergänzen und die Apply-Ziele zuweisen:
   `PlayerCarry` (Tragkraft), `MeltController` (Lampe/Akku), Bewegungs-Controller (Speed).
2. XP-Quellen verbinden:
   - F4: das/die `SortTargetInteractable`-`onCompleted`-Event auf `PlayerProgression.AwardSortXp` legen
     (Inspector-Event) – oder die Verdrahtung über ein Setup-Menü.
   - F5: `PlayerProgression` liest den Zuwachs von `SnowPatch.Coverage` (Referenz setzen).
3. *(Optional fürs Testen)* **`SkillMenuDevTool`** am Player ergänzen (IMGUI-Overlay).

## Testen (Dev-Tool)

- XP gutschreiben (Dev-Taste) → Level steigt an den Schwellen, verfügbare Punkte erscheinen.
- In Skilloptionen investieren (Buttons) → Stufe steigt, Punkte sinken; bei 0 Punkten / Maximalstufe passiert nichts.
- Wirkung prüfen:
  - **Tragkraft** hoch → schwerere Objekte / mehr Stapel tragbar (F3).
  - **Lampe** hoch → schnelleres/größeres Schmelzen bzw. mehr Akku (F5).
  - **Bewegung** hoch → schnelleres Laufen.
- Freischalt-Skills (Sortierblick/Objektanziehung): investierbar; gelten als freigeschaltet — die Fähigkeit
  selbst kommt mit F9/F10.

## Editor-authored Skillmenü (finales UI)

- Das richtige Skillmenü wird als Szene/Prefab im Editor gebaut (Panels, Buttons, Labels). Der Laufzeitcode
  (`PlayerProgression`) **bindet** nur: zeigt Level/XP/Punkte/Stufen an und ruft `Invest(id)` auf.
- Kein Laufzeit-Erzeugen von UI (Constitution V). Das IMGUI-Dev-Tool ist nur Test-Hilfe.

## Tests

- **EditMode** `ProgressionTests`: X1–X4 (XP/Level), S1–S5 (Investition/Freischaltung), V1 (Skillwert/Deckelung).
- **Compile**: `dotnet build cozychristmas.sln`.

## Bewusst späterer Scope

- **Balancing** (Level-Kurve, XP-Beträge, Skill-Skalierung) – Platzhalter, später tunen.
- **Fähigkeiten** Sortierblick (F9) / Objektanziehung (F10).
- **HUD**-Anzeige von XP/Level dauerhaft (F7) und **Speichern/Laden** (F14).
