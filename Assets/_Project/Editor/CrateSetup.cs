using System.Collections.Generic;
using System.IO;
using CozySanta.Runtime.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Editor
{
    /// <summary>
    /// Richtet die 48 Lagerhallen-Crates ein (Constitution V: nur Editor-/Asset-Manipulation):
    /// setzt die Sortier-Facetten auf den 3er-Code <c>[Typ, Farbe, Symbol]</c> (Typ = „Box"/„Kiste"
    /// aus dem Ordner, Farbe/Symbol aus dem Namen), stattet sie analog zu den Büchern mit
    /// <see cref="PrefabId"/> + <see cref="SettlingBody"/> aus, schaltet ihren Schattenwurf ab und baut
    /// einen eigenen <see cref="ItemCatalog"/> (CrateCatalog) für das Spawnen/Persistieren.
    /// </summary>
    public static class CrateSetup
    {
        private const string BoxFolder = "Assets/_Project/Prefabs/Lagerhalle/Box";
        private const string KisteFolder = "Assets/_Project/Prefabs/Lagerhalle/Kiste";
        private const string DataFolder = "Assets/_Project/Data";
        private const string CatalogPath = DataFolder + "/CrateCatalog.asset";
        private const float SettleDuration = 3f;

        [MenuItem("CozySanta/Lagerhalle/1 - Crates einrichten (Facetten + Persistenz + Katalog)")]
        public static void SetupCrates()
        {
            var entries = new List<ItemCatalog.Entry>();
            var done = SetupFolder(BoxFolder, "Box", entries) + SetupFolder(KisteFolder, "Kiste", entries);

            BuildCatalog(entries);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CrateSetup] {done} Crate-Prefab(s) eingerichtet, CrateCatalog mit {entries.Count} Einträgen unter {CatalogPath}.");
        }

        private static int SetupFolder(string folder, string typ, List<ItemCatalog.Entry> entries)
        {
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = Path.GetFileNameWithoutExtension(path);
                if (StampCrate(path, typ, key)) count++;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) entries.Add(new ItemCatalog.Entry { key = key, prefab = prefab });
            }

            return count;
        }

        private static bool StampCrate(string path, string typ, string key)
        {
            var code = BuildCode(typ, key);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return false;

            try
            {
                var sortable = root.GetComponentInChildren<CozySanta.Runtime.Sorting.Sortable>(true);
                if (sortable != null) SetStringArray(sortable, "facets", code);

                var id = root.GetComponent<PrefabId>() ?? root.AddComponent<PrefabId>();
                id.SetKey(key);

                var settling = root.GetComponent<SettlingBody>() ?? root.AddComponent<SettlingBody>();
                var sso = new SerializedObject(settling);
                sso.FindProperty("settleDuration").floatValue = SettleDuration;
                sso.ApplyModifiedPropertiesWithoutUndo();

                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Code aus Typ + Name: <c>[Typ, Farbe, Symbol]</c>. Symbol/Farbe = letzte zwei
        /// Namens-Token (Box: Crate_01_Symbol_Farbe, Kiste: crateDark_Symbol_Farbe).</summary>
        public static string[] BuildCode(string typ, string prefabName)
        {
            var parts = prefabName.Split('_');
            var farbe = parts.Length >= 1 ? parts[parts.Length - 1] : "";
            var symbol = parts.Length >= 2 ? parts[parts.Length - 2] : "";
            return new[] { typ, farbe, symbol };
        }

        private static void BuildCatalog(List<ItemCatalog.Entry> entries)
        {
            if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/_Project", "Data");

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
        }

        private static void SetStringArray(Object target, string propName, string[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null) return;

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
