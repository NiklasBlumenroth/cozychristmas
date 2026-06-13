using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Setzt gezielt NUR den <c>gridSize</c>-Override an allen Fächern
    /// (<see cref="SortTargetInteractable"/>) der aktiven Szene zurück, damit der aktuelle Prefab-Wert
    /// greift (z. B. nachträglich von 1 auf 20 Slots geändert). Position, acceptedFacets und alle
    /// anderen Overrides bleiben erhalten – im Gegensatz zu „Revert All". Nur Editor-Manipulation.
    /// </summary>
    public static class FachGridRevertTool
    {
        [MenuItem("CozySanta/Regale/Fach gridSize-Override zurücksetzen (Szene)")]
        public static void RevertGridSize()
        {
            var targets = Object.FindObjectsByType<SortTargetInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var reverted = 0;

            foreach (var target in targets)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(target)) continue;

                var so = new SerializedObject(target);
                var gx = so.FindProperty("gridSize.x");
                var gy = so.FindProperty("gridSize.y");
                var gz = so.FindProperty("gridSize.z");

                var overridden = (gx != null && gx.prefabOverride)
                                 || (gy != null && gy.prefabOverride)
                                 || (gz != null && gz.prefabOverride);
                if (!overridden) continue;

                var grid = so.FindProperty("gridSize");
                PrefabUtility.RevertPropertyOverride(grid, InteractionMode.AutomatedAction);
                reverted++;
            }

            if (reverted > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Debug.Log($"[FachGridRevert] gridSize-Override an {reverted} von {targets.Length} Fächern zurückgesetzt " +
                      "(Position/Codes unverändert). Szene speichern nicht vergessen.");
        }
    }
}
