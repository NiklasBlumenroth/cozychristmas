# Quickstart: Neues Feature im Cozy-Santa-Fundament anlegen

Gilt nach Abschluss von F1. Zeigt, wie man die Schichtung nutzt (FR-008, SC-004).

## Ordner- & Assembly-Konvention

```
Assets/_Project/
├── Core/        → CozySanta.Core      (reines C#, noEngineReferences: true)
├── Runtime/     → CozySanta.Runtime   (MonoBehaviours, Provider-Impl., refs Core)
└── Tests/
    ├── EditMode/ → CozySanta.Tests.EditMode (refs Core)
    └── PlayMode/ → CozySanta.Tests.PlayMode (refs Core, Runtime)
```

Namespaces folgen dem Ordner: `CozySanta.Core.<Bereich>`, `CozySanta.Runtime.<Bereich>`.

## Eine neue Fachregel + Test anlegen

1. **Entscheidung in Core**: Lege in `Assets/_Project/Core/<Bereich>/` einen Datentyp
   (Eingabe), ggf. ein Provider-Interface und eine `Decide`-Methode an. Keine `UnityEngine`-Typen.
2. **EditMode-Test**: In `Assets/_Project/Tests/EditMode/` einen `[Test]` schreiben, der `Decide`
   mit Fake-Eingaben/-Providern prüft. Läuft ohne Szene.
3. **Apply in Runtime**: In `Assets/_Project/Runtime/<Bereich>/` ein `MonoBehaviour`, das den
   Provider füllt, `Decide` ruft und das Ergebnis anwendet (einziger Seiteneffekt-Ort).
4. **PlayMode-Test (nur bei kritischem E2E-Flow)**: In `Assets/_Project/Tests/PlayMode/` einen
   `[UnityTest]` mit Minimalszene + Fake-Provider schreiben.
5. **Diagramme**: Bei geänderter Fachlogik/Zuständen/Daten/Flows PlantUML unter
   `specs/<feature>/diagrams/` ergänzen (Prinzip X).

## Tests ausführen

- **Editor**: `Window > General > Test Runner` → EditMode/PlayMode → „Run All".
- **CLI** (Beispiel): `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml`.

## Negativnachweis der Core-Entkopplung (FR-002 / SC-001)

Zur Verifikation (nicht eingecheckt lassen): In einer Core-Datei testweise
`using UnityEngine;` + `class X : MonoBehaviour {}` einfügen → Kompilierung muss fehlschlagen,
weil `CozySanta.Core` mit `noEngineReferences: true` keine `UnityEngine`-Referenz besitzt.
Anschließend wieder entfernen.

## Compile-Check

`dotnet build Assembly-CSharp.csproj` bzw. Build im Unity-Editor; vor jedem Merge fehlerfrei
(Zero-Compile-Error, Prinzip IV).
