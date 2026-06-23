using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Aktiviert GPU-Instancing (<see cref="Material.enableInstancing"/>) auf allen Material-Assets im
    /// Projekt. Hintergrund: Der Playtest-Profiler zeigte beim Betreten dichter Gebäude ~24.000 Draw Calls
    /// bei nur ~110 SetPass-Calls und <c>batches == draw_calls</c> – d. h. zehntausende identische Renderer
    /// werden ungebatcht einzeln gezeichnet, obwohl sie sich nur ~110 Materialien teilen. Mit aktivem
    /// Instancing fasst die GPU gleiche Material+Mesh-Kombinationen zu wenigen Draws zusammen.
    ///
    /// Reiner Editor-/Asset-Schritt (Constitution V konform, keine neue Core-Fachlogik). Setzt das Flag
    /// über die Unity-API (korrekte Serialisierung) statt per YAML-Textmanipulation. Idempotent: bereits
    /// gesetzte Materialien werden übersprungen, das Asset wird nur bei echter Änderung als „dirty" markiert.
    /// </summary>
    public static class GpuInstancingSetup
    {
        [MenuItem("CozySanta/Performance/GPU-Instancing auf allen Materialien aktivieren")]
        public static void EnableInstancingOnAllMaterials()
        {
            var guids = AssetDatabase.FindAssets("t:Material");
            var changed = new List<string>();
            var alreadyOn = 0;

            try
            {
                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar(
                        "GPU-Instancing", path, (float)i / Mathf.Max(1, guids.Length));

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        continue;
                    }

                    if (mat.enableInstancing)
                    {
                        alreadyOn++;
                        continue;
                    }

                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    changed.Add(path);
                }

                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[GpuInstancing] Fertig. {changed.Count} Materialien umgestellt, " +
                      $"{alreadyOn} waren bereits aktiv, {guids.Length} gesamt geprüft." +
                      (changed.Count > 0 ? "\nGeändert:\n  " + string.Join("\n  ", changed) : ""));
        }
    }
}
