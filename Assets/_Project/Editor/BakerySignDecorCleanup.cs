using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Entschärft die Süßigkeiten-Deko an den Erklär-Schildern (Constitution V: nur Szene). Auf den beiden
    /// Bildern <c>lebkuchenBild</c> und <c>ZuckerstangeBild</c> unter <c>BäckereiInnen</c> hängen Lebkuchen-/
    /// Zuckerstangen-Prefab-Instanzen als reine Anschauungsobjekte. Da sie noch <see cref="PrefabId"/> &amp; Co.
    /// tragen, würden sie beim Spielstart (Item-Persistenz) entfernt. Dieses Tool macht sie zu purer Optik:
    /// es löscht von allen Kindobjekten der beiden Bilder sämtliche MonoBehaviour-Skripte, <see cref="Rigidbody"/>
    /// und <see cref="Collider"/> – Mesh/Renderer bleiben erhalten.
    /// </summary>
    public static class BakerySignDecorCleanup
    {
        // Süßigkeiten-Stichwörter; ein Schild ist jedes Objekt, dessen Name "bild" UND eines dieser Wörter
        // enthält (robust gegen Singular/Plural & Groß-/Kleinschreibung, z. B. "ZuckerstangenBild").
        private static readonly string[] SweetKeywords = { "zucker", "lebkuchen" };

        [MenuItem("CozySanta/Bäckerei/Schild-Deko entschärfen (Skripte/Collider/Rigidbody entfernen)")]
        public static void Cleanup()
        {
            var bakery = FindBakeryRoot();
            if (bakery == null)
            {
                Debug.LogError("[Bäckerei] Kein 'BäckereiInnen' in der offenen Szene gefunden.");
                return;
            }

            var signs = FindSigns(bakery);
            if (signs.Count == 0)
            {
                Debug.LogError($"[Bäckerei] Keine Schild-Bilder (Name enthält 'bild' + {string.Join("/", SweetKeywords)}) " +
                               $"unter '{bakery.name}' gefunden.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bäckerei-Schild-Deko entschärfen");

            var log = new StringBuilder();
            var removed = 0;
            foreach (var sign in signs)
            {
                var signRemoved = 0;
                // Nur die Kindobjekte (die eingesetzten Süßigkeiten) bereinigen, nicht das Bild selbst.
                foreach (var child in sign.GetComponentsInChildren<Transform>(true))
                {
                    if (child == sign) continue;
                    signRemoved += StripComponents(child.gameObject);
                }

                log.AppendLine($"  {sign.name}: {signRemoved} Komponente(n) entfernt");
                removed += signRemoved;
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(bakery.gameObject.scene);

            Debug.Log($"[Bäckerei] Schild-Deko entschärft: {removed} Komponente(n) auf {signs.Count} Bild(ern) " +
                      $"entfernt. Szene speichern (Strg+S).\n{log}");
        }

        // Entfernt Rigidbody, alle Collider und alle MonoBehaviour-Skripte vom Objekt; Mesh/Renderer/Transform
        // bleiben. Reihenfolge: erst Skripte (könnten Collider/Rigidbody per RequireComponent halten), dann
        // Collider, zuletzt Rigidbody.
        private static int StripComponents(GameObject go)
        {
            var count = 0;
            foreach (var script in go.GetComponents<MonoBehaviour>())
            {
                if (script == null) continue;
                Undo.DestroyObjectImmediate(script);
                count++;
            }

            foreach (var collider in go.GetComponents<Collider>())
            {
                Undo.DestroyObjectImmediate(collider);
                count++;
            }

            foreach (var body in go.GetComponents<Rigidbody>())
            {
                Undo.DestroyObjectImmediate(body);
                count++;
            }

            return count;
        }

        private static List<Transform> FindSigns(Transform bakery)
        {
            return bakery.GetComponentsInChildren<Transform>(true)
                .Where(IsSign)
                .ToList();
        }

        private static bool IsSign(Transform t)
        {
            if (t.name.IndexOf("bild", System.StringComparison.OrdinalIgnoreCase) < 0) return false;
            return SweetKeywords.Any(k => t.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Transform FindBakeryRoot()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.IndexOf("ckereiInnen", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
