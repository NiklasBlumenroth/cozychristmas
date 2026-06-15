# Drittanbieter-Assets – Cozy Santa Factory

> Herkunfts- und Lizenznachweis für alle extern bezogenen Assets (Unity Asset
> Store, andere Marktplätze, Free-Quellen). Pflicht laut Constitution (Doku im
> selben Branch) und Absicherung bei Veröffentlichung.

## Pflege-Hinweise

- **Jedes** neu eingebundene Asset hier eintragen, bevor es in `Assets/` landet.
- Bei Unity-Asset-Store-Käufen die **Order-ID** und das **Kaufdatum** festhalten
  (Nachweis bei Lizenz-/Audit-Fragen).
- **Lizenztyp** prüfen: Standard-EULA vs. **Restricted Asset** (eigene Terms in
  den Begleitdateien – diese gehen vor!) vs. enthält **Open-Source**-Anteile
  (z. B. MIT/Apache/GPL – eigene Lizenzpflichten).
- **Seat-Lizenz beachten** bei „Editor Extension" / „Scripting" / „Services":
  max. 2 Rechner pro Lizenz. Jede mitarbeitende Person braucht eine eigene Lizenz.
- Begleitende Lizenzdateien (LICENSE.txt, EULA, Attribution) **mit ins Repo**
  bzw. unter `Assets/ThirdParty/<Asset>/` ablegen, nicht löschen.
- **Verboten** (laut Asset-Store-EULA): Roh-Assets weitergeben/verkaufen,
  „forum pooling" (Kosten teilen + gemeinsam nutzen), Nutzung zum Training von
  KI-/ML-Modellen.

## Asset-Register

| Asset / Pack | Quelle | Anbieter | Lizenztyp | Order-ID | Kaufdatum | Verwendung im Projekt | Attribution nötig? | Anmerkungen |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| _Beispiel: Holiday Snow Texturen_ | Unity Asset Store | _Anbieter_ | Standard-EULA | _z. B. 1234567_ | _JJJJ-MM-TT_ | Schnee-Material F5 | nein | — |
|  |  |  |  |  |  |  |  |  |

## Lizenztyp-Legende

- **Standard-EULA** – Unity Asset Store Standard End User License Agreement
  (Stand 2024-12-04). Einbetten ins Spiel + Monetarisierung erlaubt; Roh-Asset
  nicht weitergeben.
- **Restricted Asset** – abweichende, mitgelieferte Lizenzbedingungen. Immer den
  beigelegten Lizenztext lesen; diese Bedingungen haben Vorrang.
- **Enthält OSS** – Asset enthält Open-Source-Komponenten mit eigener Lizenz
  (Lizenz + ggf. Attribution dokumentieren; bei GPL-Anteilen Verträglichkeit
  mit kommerzieller Veröffentlichung prüfen).
- **CC / Free** – freie Quelle (z. B. CC0, CC-BY). Attribution-Pflicht prüfen.
