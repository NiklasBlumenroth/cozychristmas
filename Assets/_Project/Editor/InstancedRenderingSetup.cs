using System.Collections.Generic;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CozySanta.Editor
{
    /// <summary>
    /// Hängt je Gebäude einen <see cref="InstancedItemRenderer"/> an den <c>ItemArea.ItemParent</c>
    /// (Gebäude-Root). Damit zeichnet das Gebäude seine ruhenden Item-Massen per GPU-Instancing statt
    /// einzeln – die Items melden sich beim Ruhen selbst an (über <c>SettlingBody.EnterRest</c>), es ist
    /// keine weitere Verdrahtung nötig. Idempotent; Bereiche ohne eigenen ItemParent werden übersprungen
    /// (deren Items rendern weiter einzeln). Reiner Editor-/Szenen-Schritt (Constitution V konform).
    /// </summary>
    public static class InstancedRenderingSetup
    {
        [MenuItem("CozySanta/Performance/Instanced Item Rendering einrichten")]
        public static void Setup()
        {
            var areas = Object.FindObjectsByType<ItemArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (areas.Length == 0)
            {
                Debug.LogWarning("[InstancedRendering] Keine ItemArea in der Szene gefunden.");
                return;
            }

            var added = 0;
            var existing = 0;
            var skipped = 0;
            var seen = new HashSet<int>();

            foreach (var area in areas)
            {
                var parent = area.ItemParent;
                if (parent == null)
                {
                    skipped++;
                    continue;
                }

                // Mehrere Bereiche können denselben Gebäude-Root teilen – nur einmal bestücken.
                if (!seen.Add(parent.GetInstanceID()))
                {
                    continue;
                }

                if (parent.GetComponent<InstancedItemRenderer>() != null)
                {
                    existing++;
                    continue;
                }

                Undo.AddComponent<InstancedItemRenderer>(parent.gameObject);
                added++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[InstancedRendering] Fertig. {added} Renderer hinzugefügt, {existing} bereits vorhanden, " +
                      $"{skipped} Bereiche ohne ItemParent übersprungen ({areas.Length} ItemAreas gesamt).\n" +
                      "Szene speichern, dann im Play-Mode am Item-Haufen den Profiler vergleichen.");
        }
    }
}
