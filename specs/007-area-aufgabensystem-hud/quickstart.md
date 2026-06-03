# Quickstart: F7 – Area- & Aufgabensystem + HUD

## Neue Dateien

| Pfad | Inhalt |
|---|---|
| `Assets/_Project/Core/Progression/TaskType.cs` | Enum Sort/Melt/Custom |
| `Assets/_Project/Core/Progression/AreaTask.cs` | Task-Logik (Book, IsComplete) |
| `Assets/_Project/Core/Progression/AreaDefinition.cs` | Unveränderliche Area-Konfiguration |
| `Assets/_Project/Core/Progression/AreaProgress.cs` | Laufzeit-Fortschritt + OnCompleted |
| `Assets/_Project/Runtime/Progression/AreaTracker.cs` | Apply: Events binden, HUD aktualisieren |
| `Assets/_Project/Runtime/Progression/AreaHudView.cs` | View-Binding für HUD-Panel |
| `Assets/_Project/Runtime/Progression/TaskEntryUI.cs` | Prefab-Skript für HUD-Task-Zeile |
| `Assets/_Project/Editor/AreaSetup.cs` | „CozySanta/Setup F7 …" – erstellt HUD-Panel + verdrahtet AreaTracker |
| `Assets/_Project/Tests/EditMode/AreaProgressTests.cs` | EditMode-Tests A1–A3, B1–B2 |

## Schritte

1. Core-Typen schreiben und EditMode-Tests A1–A3, B1–B2 grün machen.
2. Runtime `AreaTracker` + `AreaHudView` + `TaskEntryUI` schreiben.
3. `AreaSetup.cs` Editor-Skript: HUD-Panel unter Canvas erstellen (analog `ProgressionSetup.cs`), `AreaTracker` am Player verdrahten.
4. **CozySanta/Setup F7 (Area-HUD erstellen)** ausführen → Szene speichern.
5. Manuelle Verifikation: Fach abschließen → Task-Zähler steigt; Schnee schmelzen → Prozent steigt; Area abschließen → XP vergeben.

## Compile-Check

```bash
~/.dotnet/dotnet build CozySanta.Core.csproj
~/.dotnet/dotnet build CozySanta.Runtime.csproj
~/.dotnet/dotnet build CozySanta.Tests.EditMode.csproj
```
