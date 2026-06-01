# Phase 0 – Research: F1 Core-Architektur & Projektgrundgerüst

Keine offenen `NEEDS CLARIFICATION`. Folgende Entscheidungen wurden getroffen.

## 1. Assembly-Layout & Durchsetzung der Unity-Entkopplung

- **Decision**: Drei Assembly Definitions unter `Assets/_Project/`: `CozySanta.Core`,
  `CozySanta.Runtime`, sowie Test-Assemblies. Die Core-asmdef setzt `"noEngineReferences": true`
  und `"autoReferenced": false`.
- **Rationale**: `noEngineReferences` entfernt die implizite `UnityEngine`/`UnityEditor`-Referenz,
  wodurch ein Zugriff auf `MonoBehaviour`, `Time` etc. in der Core-Schicht zum Kompilierfehler
  führt (FR-002, SC-001). Das macht Prinzip IX **erzwungen** statt nur empfohlen. `autoReferenced:false`
  verhindert, dass beliebiger Assembly-CSharp-Code unbeabsichtigt an Core hängt.
- **Alternatives considered**: (a) Alles in `Assembly-CSharp` mit Namens-Konvention – verworfen,
  da nicht erzwingbar. (b) Reines .csproj außerhalb Unity – verworfen, da Tooling/Import-Bruch.

## 2. Provider-Design (Zeit/Welt/Eingabe)

- **Decision**: Provider als schmale Interfaces in der Core-Schicht; Implementierungen in der
  Runtime-Schicht. Für F1 konkret: `ITimeProvider` (Zeitabstraktion) und `IInteractionProbe`
  (Weltabfrage liefert Kandidatenliste). Eingabe-Provider wird erst in F2 ausmodelliert.
- **Rationale**: Minimal, aber repräsentativ. Tests injizieren Fakes ohne Unity-Laufzeit
  (FR-003, FR-007). Eingabe bewusst auf F2 verschoben, um F1 klein zu halten (Spec-Assumption).
- **Alternatives considered**: Service-Locator/Singletons – verworfen (schlecht testbar, globale
  Kopplung, kollidiert mit Prinzip IX-Verbot von Global-Lookups).

## 3. Test-Assembly-Konfiguration

- **Decision**: Getrennte EditMode- und PlayMode-Test-asmdefs mit
  `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`, Referenzen auf `UnityEngine.TestRunner` /
  `UnityEditor.TestRunner`, `"precompiledReferences": ["nunit.framework.dll"]`. EditMode mit
  `"includePlatforms": ["Editor"]`; PlayMode ohne Plattform-Einschränkung. Beide referenzieren
  `CozySanta.Core`, PlayMode zusätzlich `CozySanta.Runtime`.
- **Rationale**: Standardmuster des Unity Test Framework 1.6; EditMode bleibt schnell und
  szenenfrei (primärer Regel-Testpfad), PlayMode nur für den E2E-Beispielflow (FR-006, FR-007).
- **Alternatives considered**: Ein gemeinsames Test-Assembly – verworfen, da EditMode/PlayMode
  unterschiedliche Plattform-/Laufzeitanforderungen haben.

## 4. Wahl des Beispiel-Slice

- **Decision**: Minimaler „Interaktions-Zielauswahl"-Slice: `InteractionSelector.Decide` wählt aus
  einer Kandidatenliste den besten Treffer (kleinste Distanz innerhalb `maxRange`/`maxAngle`).
  Runtime orchestriert Probe→Decide→Apply.
- **Rationale**: Demonstriert Decide (pure) + Apply (Seiteneffekt) + Provider (Weltabfrage) an
  echtem, später in F2 wiederverwendbarem Verhalten – ohne F2 vorwegzunehmen (nur Auswahl, keine
  vollständige Bewegung/Input/HUD). Liefert klare Testkandidaten.
- **Alternatives considered**: Künstliches Wegwerf-Beispiel (z. B. Taschenrechner) – verworfen,
  da es keinen Projektwert hat und die Konvention nicht realistisch zeigt.

## Offene Punkte

- Keine. Namespace-Wurzel `CozySanta` und Slice-Wahl sind in der Spec als Assumptions fixiert.
