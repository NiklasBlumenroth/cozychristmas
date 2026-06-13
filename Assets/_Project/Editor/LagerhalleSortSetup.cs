using System.Collections.Generic;
using System.IO;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Verteilt die Sortier-Codes auf die Fächer der Lagerhalle und richtet den Lagerhallen-Bereich ein.
    /// Box-Codes gehen an die Bündel unter Regal..Regal(4), Kiste-Codes an Regal(5)..Regal(9); jeweils
    /// zufällig 1:1 (24 Codes ↔ 24 Bündel). Alle Fächer eines Bündels (4 Racks × 4 Fächer = 16) erhalten
    /// denselben Code. Nur Editor-/Szenen-Manipulation (Constitution V).
    /// </summary>
    public static class LagerhalleSortSetup
    {
        private const string BoxFolder = "Assets/_Project/Prefabs/Lagerhalle/Box";
        private const string KisteFolder = "Assets/_Project/Prefabs/Lagerhalle/Kiste";
        private const string CrateCatalogPath = "Assets/_Project/Data/CrateCatalog.asset";
        private const int MaxPerVariant = 48;   // 16 Fächer × 3 requiredCount

        [MenuItem("CozySanta/Lagerhalle/2 - Fach-Codes verteilen")]
        public static void DistributeCodes()
        {
            var regale = FindRegalClusters();
            if (regale.Count < 10)
            {
                Debug.LogError($"[Lagerhalle] Nur {regale.Count} Regal-Cluster gefunden (erwartet 10). Abbruch.");
                return;
            }

            var boxBundles = CollectBundles(regale.GetRange(0, 5));
            var kisteBundles = CollectBundles(regale.GetRange(5, 5));
            var boxCodes = CodesFromFolder(BoxFolder, "Box");
            var kisteCodes = CodesFromFolder(KisteFolder, "Kiste");

            if (!Check("Box", boxBundles.Count, boxCodes.Count) || !Check("Kiste", kisteBundles.Count, kisteCodes.Count))
            {
                return; // Abbruch bei Anzahl-Mismatch
            }

            var faecher = Assign(boxBundles, boxCodes) + Assign(kisteBundles, kisteCodes);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Lagerhalle] Codes verteilt: {boxBundles.Count} Box- + {kisteBundles.Count} Kiste-Bündel, {faecher} Fächer gesetzt.");
        }

        private static bool Check(string label, int bundles, int codes)
        {
            if (bundles == codes) return true;
            Debug.LogError($"[Lagerhalle] {label}: {bundles} Bündel, aber {codes} Codes – erwartet gleich viele. Abbruch (nichts geändert).");
            return false;
        }

        // Weist jedem Bündel zufällig genau einen Code zu; alle Fächer des Bündels bekommen ihn.
        private static int Assign(List<Transform> bundles, List<string[]> codes)
        {
            Shuffle(codes);
            var faecher = 0;
            for (var i = 0; i < bundles.Count; i++)
            {
                var code = codes[i];
                var targets = bundles[i].GetComponentsInChildren<SortTargetInteractable>(true);
                if (targets.Length != 16)
                {
                    Debug.LogWarning($"[Lagerhalle] Bündel '{bundles[i].name}' hat {targets.Length} Fächer (erwartet 16).");
                }

                foreach (var target in targets)
                {
                    SetCode(target, code);
                    faecher++;
                }
            }

            return faecher;
        }

        private static void SetCode(SortTargetInteractable target, string[] code)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty("acceptedFacets");
            prop.arraySize = code.Length;
            for (var i = 0; i < code.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = code[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Regal-Cluster unter „Regale", sortiert nach Index (Regal=0, Regal (1)=1, …).
        private static List<Transform> FindRegalClusters()
        {
            var result = new List<Transform>();
            var regale = GameObject.Find("Regale");
            if (regale == null) return result;

            foreach (Transform child in regale.transform)
            {
                if (child.name == "Regal" || child.name.StartsWith("Regal (")) result.Add(child);
            }

            result.Sort((a, b) => RegalIndex(a.name).CompareTo(RegalIndex(b.name)));
            return result;
        }

        private static int RegalIndex(string name)
        {
            var open = name.IndexOf('(');
            if (open < 0) return 0;
            var close = name.IndexOf(')', open);
            return int.TryParse(name.Substring(open + 1, close - open - 1), out var n) ? n : 0;
        }

        private static List<Transform> CollectBundles(List<Transform> regale)
        {
            var bundles = new List<Transform>();
            foreach (var regal in regale)
            {
                foreach (var t in regal.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("Bündel")) bundles.Add(t);
                }
            }

            return bundles;
        }

        private static List<string[]> CodesFromFolder(string folder, string typ)
        {
            var codes = new List<string[]>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                codes.Add(CrateSetup.BuildCode(typ, name));
            }

            return codes;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        [MenuItem("CozySanta/Lagerhalle/3 - Bereich + Spawn/HUD einrichten")]
        public static void SetupArea()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CrateCatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[Lagerhalle] Kein CrateCatalog unter {CrateCatalogPath} – zuerst Schritt 1 ausführen.");
                return;
            }

            var zone = FindLagerhalleZone();
            if (zone == null)
            {
                Debug.LogError("[Lagerhalle] Kein 'Lagerhalle'-Objekt mit Collider gefunden. " +
                               "Lege ein GameObject 'Lagerhalle' mit BoxCollider (Raumgröße) an und führe Schritt 3 erneut aus.");
                return;
            }

            var area = zone.GetComponent<ItemArea>() ?? zone.AddComponent<ItemArea>();
            var so = new SerializedObject(area);
            so.FindProperty("areaName").stringValue = "Lagerhalle";
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("maxPerVariant").intValue = MaxPerVariant;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Lagerhalle] Bereich '{zone.name}' = Lagerhalle (Katalog {catalog.name}, Max {MaxPerVariant}). " +
                      "Spawner/HUD/Persistenz nutzen ihn automatisch (R spawnen, F6 Inventar). " +
                      "Falls noch nicht geschehen: 'CozySanta/Items/Szenen-Objekte anlegen' ausführen.");
        }

        private static GameObject FindLagerhalleZone()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name.ToLowerInvariant().Contains("lagerhalle") && t.GetComponent<Collider>() != null)
                {
                    return t.gameObject;
                }
            }

            return null;
        }
    }
}
