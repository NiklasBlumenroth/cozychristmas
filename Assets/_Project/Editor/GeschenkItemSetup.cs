using System.Collections.Generic;
using System.IO;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Editor
{
    /// <summary>
    /// Stattet die Geschenk-Prefabs beider Klassen mit allem aus, was ein Sortierobjekt braucht
    /// (analog <see cref="BakerySweetItemSetup"/>): <see cref="Rigidbody"/> (Masse 1), <see cref="PickupInteractable"/>,
    /// <see cref="Sortable"/> mit einem einzigen Facetten-Wert = Prefab-Name (eine Sorte je Container),
    /// an die Mesh-Bounds gefitteter <see cref="BoxCollider"/>, <see cref="PrefabId"/> + <see cref="SettlingBody"/>,
    /// Schattenwurf aus. Baut EINEN gemeinsamen Katalog <c>GeschenkehalleCatalog</c> für den einen
    /// Geschenkehalle-Bereich (beide Arten werden dort verteilt/gespawnt): die „Toys" aus <c>Geschenke/</c>
    /// mit Max 70/Variante, die Boxen aus <c>Geschenke/Truhengeschenke/</c> mit Max 100/Variante
    /// (variantenspezifisch über <see cref="ItemCatalog.Entry.maxPerVariant"/>). Das Truhen-Setup zieht
    /// sich daraus nur die Truhen-Varianten (per Prefab-Pfad). Reiner Editor-/Asset-Schritt
    /// (Constitution V: dokumentierte Nicht-Unit-Ausnahme analog BookPrefabSetup).
    /// </summary>
    public static class GeschenkItemSetup
    {
        private const string RegalFolder = "Assets/_Project/Prefabs/Geschenke";
        public const string TruhenFolder = "Assets/_Project/Prefabs/Geschenke/Truhengeschenke";
        private const string DataFolder = "Assets/_Project/Data";
        public const string CatalogPath = DataFolder + "/GeschenkehalleCatalog.asset";

        private const int MaxRegal = 70;   // 2 Regale je Toy-Geschenkart
        private const int MaxTruhe = 100;  // 100 je Truhen-Geschenkart
        private const float SettleDuration = 3f;

        [MenuItem("CozySanta/Geschenkehalle/Geschenke als Sortierobjekte einrichten (Prefabs + Katalog)")]
        public static void SetupGifts()
        {
            // Truhen-Items zuerst stempeln+sammeln, danach die Regal-Items NUR aus dem obersten Ordner
            // (ohne den Unterordner Truhengeschenke). Beide landen in EINEM Katalog (eine Geschenkehalle),
            // jeweils mit eigener Höchstzahl pro Variante.
            var truhen = StampFolder(TruhenFolder, MaxTruhe, recursive: true, exclude: null);
            var regal = StampFolder(RegalFolder, MaxRegal, recursive: false, exclude: TruhenFolder);

            var all = new List<ItemCatalog.Entry>(regal);
            all.AddRange(truhen);
            BuildCatalog(CatalogPath, all);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Geschenkehalle] {all.Count} Items in EINEM Katalog ({CatalogPath}): " +
                      $"{regal.Count} Toys (Max {MaxRegal}) + {truhen.Count} Truhen-Geschenke (Max {MaxTruhe}). " +
                      "Katalog der Geschenkehalle-ItemArea zuweisen, dann das Truhen-Setup ausführen.");
        }

        // Stempelt alle Prefabs eines Ordners und liefert die Katalog-Einträge zurück.
        private static List<ItemCatalog.Entry> StampFolder(string folder, int max, bool recursive, string exclude)
        {
            var entries = new List<ItemCatalog.Entry>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"[Geschenkehalle] Ordner nicht gefunden: {folder}");
                return entries;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!recursive && Path.GetDirectoryName(path).Replace('\\', '/') != folder) continue;
                if (exclude != null && path.Replace('\\', '/').StartsWith(exclude + "/")) continue;

                var key = Path.GetFileNameWithoutExtension(path);
                if (Stamp(path, key))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        entries.Add(new ItemCatalog.Entry { key = key, prefab = prefab, maxPerVariant = max });
                }
            }

            return entries;
        }

        private static bool Stamp(string path, string key)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return false;

            try
            {
                // Unitys überladenes „== null" verwenden (kein ?? – siehe BakerySweetItemSetup).
                var body = root.GetComponent<Rigidbody>();
                if (body == null) body = root.AddComponent<Rigidbody>();
                body.mass = 1f;
                body.useGravity = true;
                body.isKinematic = false;

                if (root.GetComponent<PickupInteractable>() == null)
                    root.AddComponent<PickupInteractable>();

                var sortable = root.GetComponent<Sortable>();
                if (sortable == null) sortable = root.AddComponent<Sortable>();
                SetStringArray(sortable, "facets", new[] { key }); // eine Sorte je Container = Prefab-Name

                FitBoxCollider(root, key);

                var id = root.GetComponent<PrefabId>();
                if (id == null) id = root.AddComponent<PrefabId>();
                id.SetKey(key);

                var settling = root.GetComponent<SettlingBody>();
                if (settling == null) settling = root.AddComponent<SettlingBody>();
                var sso = new SerializedObject(settling);
                sso.FindProperty("settleDuration").floatValue = SettleDuration;
                sso.ApplyModifiedPropertiesWithoutUndo();

                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[Geschenkehalle] {Path.GetFileName(path)} -> SortKey [{key}]");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Fittet einen BoxCollider am Root an die kombinierten Mesh-Bounds im LOKALEN Root-Raum.
        private static void FitBoxCollider(GameObject root, string key)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var bounds = new Bounds();
            var has = false;

            foreach (var mf in filters)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;

                var toRoot = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var c = mesh.bounds.center;
                var e = mesh.bounds.extents;
                for (var i = 0; i < 8; i++)
                {
                    var corner = c + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y,
                        (i & 4) == 0 ? -e.z : e.z);
                    var p = toRoot.MultiplyPoint3x4(corner);
                    if (!has) { bounds = new Bounds(p, Vector3.zero); has = true; }
                    else bounds.Encapsulate(p);
                }
            }

            if (!has)
            {
                Debug.LogWarning($"[Geschenkehalle] Kein Mesh in '{key}'; BoxCollider nicht gefittet.");
                return;
            }

            var box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        private static void BuildCatalog(string path, List<ItemCatalog.Entry> entries)
        {
            EnsureFolder(DataFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
        }

        private static void SetStringArray(Object target, string propName, string[] values)
        {
            var serialized = new SerializedObject(target);
            var prop = serialized.FindProperty(propName);
            if (prop == null) return;
            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
