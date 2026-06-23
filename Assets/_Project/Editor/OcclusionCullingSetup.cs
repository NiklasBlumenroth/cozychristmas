using System.Collections.Generic;
using CozySanta.Runtime.Keys;
using CozySanta.Runtime.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CozySanta.Editor
{
    /// <summary>
    /// Richtet Occlusion Culling für die Gebäude ein. Hintergrund: Der Playtest-Bisect zeigte, dass die
    /// geladenen Item-Haufen (Bücher/Süßes/Kisten-Inhalte) ~14.000 der ~18.000 Draw Calls ausmachen –
    /// jedes Item ein eigener Renderer. Occlusion Culling lässt Renderer, die hinter solider Geometrie
    /// (Wände, Regale, Böden) liegen, gar nicht erst zeichnen. Damit das greift, muss diese Struktur-
    /// Geometrie als <see cref="StaticEditorFlags.OccluderStatic"/> markiert und das Culling gebacken
    /// werden – aktuell ist nichts davon static, also kann das Culling noch nichts tun.
    ///
    /// Die zur Laufzeit gespawnten Items selbst werden NICHT markiert (sie existieren im Editor nicht);
    /// sie sind per Default „Dynamic Occludee" und werden gegen die gebackenen Occluder automatisch
    /// gecullt. Reiner Editor-/Szenen-Schritt (Constitution V konform, keine neue Core-Fachlogik).
    ///
    /// Bewegliche Objekte (Tore <see cref="GateController"/>, Schranktüren <see cref="CabinetDoorController"/>,
    /// dynamische Rigidbodies) werden bewusst ausgelassen – ein bewegtes Objekt als statischer Occluder/
    /// Occludee würde zu Fehl-Cullings führen.
    /// </summary>
    public static class OcclusionCullingSetup
    {
        // Bereichs-Roots (Gebäude-Innenräume + Außenwelt), deren statische Geometrie markiert wird.
        // Inaktive Roots werden über die Transform-Hierarchie ebenfalls gefunden (Traversal inkl. inaktiv).
        private static readonly string[] AreaRootNames =
        {
            "Außenwelt", "Außenwelt1", "BibliothekInnen", "BäckereiInnen",
            "DekoInnen", "PostInnen", "Lagerhalle", "Dekohalle",
        };

        // Nur Occluder + Occludee: das Item-Problem sind die dynamischen Occludees, die hinter dieser
        // Geometrie verschwinden. BatchingStatic (Static Batching) bewusst NICHT, um die Messung sauber
        // auf den Occlusion-Effekt zu beschränken – das ist der dokumentierte nächste Hebel.
        private const StaticEditorFlags OcclusionFlags =
            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        [MenuItem("CozySanta/Performance/Occlusion 1 - statische Geometrie markieren")]
        public static void MarkStatic()
        {
            var roots = FindAreaRoots();
            if (roots.Count == 0)
            {
                Debug.LogWarning("[Occlusion] Keine Area-Roots in der aktiven Szene gefunden " +
                                 "(erwartet z. B. 'BibliothekInnen', 'Lagerhalle').");
                return;
            }

            int marked = 0, skipped = 0;
            foreach (var root in roots)
            {
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                {
                    if (IsDynamic(mr.transform, root.transform))
                    {
                        skipped++;
                        continue;
                    }

                    var go = mr.gameObject;
                    var cur = GameObjectUtility.GetStaticEditorFlags(go);
                    if ((cur & OcclusionFlags) != OcclusionFlags)
                    {
                        GameObjectUtility.SetStaticEditorFlags(go, cur | OcclusionFlags);
                        marked++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[Occlusion] {marked} Renderer als Occluder+Occludee-Static markiert, " +
                      $"{skipped} bewegliche übersprungen, {roots.Count} Bereiche geprüft.\n" +
                      "Szene speichern, dann 'CozySanta/Performance/Occlusion 2 - Culling backen' ausführen.");
        }

        // Bewegliches Objekt? Ab dem Renderer bis zum Bereichs-Root hochlaufen und auf Tor/Schranktür/
        // dynamischen Rigidbody prüfen. stopAt begrenzt den Aufstieg auf den jeweiligen Bereich.
        private static bool IsDynamic(Transform t, Transform stopAt)
        {
            for (var c = t; c != null; c = c.parent)
            {
                if (c.GetComponent<GateController>() != null) return true;
                if (c.GetComponent<CabinetDoorController>() != null) return true;

                var rb = c.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic) return true;

                if (c == stopAt) break;
            }

            return false;
        }

        private static List<GameObject> FindAreaRoots()
        {
            var wanted = new HashSet<string>(AreaRootNames);
            var found = new List<GameObject>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Collect(root.transform, wanted, found);
            }

            return found;
        }

        private static void Collect(Transform t, HashSet<string> wanted, List<GameObject> found)
        {
            if (wanted.Contains(t.name))
            {
                found.Add(t.gameObject);
            }

            for (var i = 0; i < t.childCount; i++)
            {
                Collect(t.GetChild(i), wanted, found);
            }
        }

        [MenuItem("CozySanta/Performance/Occlusion 2 - Culling backen")]
        public static void Bake()
        {
            Debug.Log("[Occlusion] Bake gestartet – das kann je nach Szenengröße dauern …");
            StaticOcclusionCulling.Compute();
            Debug.Log("[Occlusion] Bake fertig. Im Play-Mode greift das Culling jetzt; " +
                      "im Profiler an der Stelle messen, wo es vorher auf ~27 FPS einbrach.");
        }
    }
}
