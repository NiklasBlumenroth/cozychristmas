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
    /// Stattet die Süßigkeiten-Prefabs (8 Zuckerstangen, 8 Lebkuchen, 4 Kekse unter
    /// <c>Prefabs/Süßigkeiten</c>) mit allem aus, was ein Sortierobjekt braucht – analog zu den
    /// Büchern (<see cref="BookPrefabSetup"/> + <see cref="ItemPersistenceSetup"/>): <see cref="Rigidbody"/>
    /// (Masse 1, Gravity an), <see cref="PickupInteractable"/> (F3), <see cref="Sortable"/> mit dem
    /// SortKey <c>[Art, Farbe]</c> (z. B. <c>[Zuckerstange, Rot]</c>), ein an die Mesh-Bounds gefitteter
    /// <see cref="BoxCollider"/> sowie <see cref="PrefabId"/> + <see cref="SettlingBody"/> für die
    /// Persistenz. Schattenwurf wird abgeschaltet (Perf bei vielen Items) und ein
    /// <see cref="ItemCatalog"/> gebaut. Reiner Editor-/Asset-Schritt (Constitution V konform).
    /// </summary>
    public static class BakerySweetItemSetup
    {
        private const string SweetsFolder = "Assets/_Project/Prefabs/Süßigkeiten";
        private const string DataFolder = "Assets/_Project/Data";
        private const string CatalogPath = DataFolder + "/SuessigkeitenCatalog.asset";
        private const float SettleDuration = 3f;

        // Spawn-Höchstzahlen je Variante (im Katalog hinterlegt): Kekse 75 (= 1 Crate à 75),
        // Zuckerstangen/Lebkuchen je 42 (= 2 Fächer à 21).
        private const int MaxKeks = 75;
        private const int MaxSuessigkeit = 42;

        // Item-eigener Dreh-Offset fürs Einsortieren (SortPlacementRotation): gilt in JEDEM Fach gleich –
        // Ghost + Einlage identisch. Zuckerstangen UND Kekse liegen als FBX anders als die Lebkuchen.
        // Werte mit dem SortPlacementRotationDevTool ermitteln und hier eintragen, dann ersten Befehl erneut
        // ausführen. Lebkuchen brauchen keinen Offset (0).
        private static readonly Vector3 ZuckerstangePlacedEuler = Vector3.zero;
        private static readonly Vector3 KeksPlacedEuler = Vector3.zero;

        [MenuItem("CozySanta/Bäckerei/Süßigkeiten als Sortierobjekte einrichten (Prefabs + Katalog)")]
        public static void SetupSweets()
        {
            if (!AssetDatabase.IsValidFolder(SweetsFolder))
            {
                Debug.LogError($"[Süßigkeiten] Ordner nicht gefunden: {SweetsFolder}");
                return;
            }

            var entries = new List<ItemCatalog.Entry>();
            var stamped = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { SweetsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = Path.GetFileNameWithoutExtension(path);
                var facets = ParseFacets(key);
                if (StampPrefab(path, key, facets)) stamped++;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    entries.Add(new ItemCatalog.Entry { key = key, prefab = prefab, maxPerVariant = MaxFor(facets[0]) });
                }
            }

            BuildCatalog(entries);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Süßigkeiten] {stamped} Prefab(s) als Sortierobjekt ausgestattet, " +
                      $"Katalog mit {entries.Count} Einträgen unter {CatalogPath} " +
                      $"(Kekse Max {MaxKeks}, Zuckerstangen/Lebkuchen Max {MaxSuessigkeit} je Variante). " +
                      "Den Katalog der Bäckerei-ItemArea zuweisen.");
        }

        private static bool StampPrefab(string path, string key, string[] facets)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return false;

            try
            {
                // Hinweis: KEIN „?? AddComponent" – das C#-Null-Coalescing umgeht Unitys überladenes
                // „== null" und kann eine Fake-Null-Komponente zurückgeben (MissingComponentException).
                // Daher überall explizit mit Unity-`== null` prüfen.
                var body = root.GetComponent<Rigidbody>();
                if (body == null) body = root.AddComponent<Rigidbody>();
                body.mass = 1f;
                body.useGravity = true;
                body.isKinematic = false;

                if (root.GetComponent<PickupInteractable>() == null)
                {
                    root.AddComponent<PickupInteractable>(); // promptText/weight aus Feld-Defaults (Aufnehmen / 0.3)
                }

                var sortable = root.GetComponent<Sortable>();
                if (sortable == null) sortable = root.AddComponent<Sortable>();
                SetStringArray(sortable, "facets", facets);

                // Item-eigener Einlage-Dreh-Offset (Zuckerstangen + Kekse liegen als Mesh anders);
                // Lebkuchen bekommen die Komponente mit 0 → einheitlicher Knopf fürs Dev-Tool.
                var rot = root.GetComponent<SortPlacementRotation>();
                if (rot == null) rot = root.AddComponent<SortPlacementRotation>();
                rot.PlacedEuler = PlacedEulerFor(facets[0]);

                FitBoxCollider(root);

                var id = root.GetComponent<PrefabId>();
                if (id == null) id = root.AddComponent<PrefabId>();
                id.SetKey(key);

                var settling = root.GetComponent<SettlingBody>();
                if (settling == null) settling = root.AddComponent<SettlingBody>();
                var sso = new SerializedObject(settling);
                sso.FindProperty("settleDuration").floatValue = SettleDuration;
                sso.ApplyModifiedPropertiesWithoutUndo();

                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[Süßigkeiten] {Path.GetFileName(path)} -> SortKey [{string.Join(", ", facets)}]");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Fittet einen <see cref="BoxCollider"/> am Root an die kombinierten Mesh-Bounds – direkt im
        /// LOKALEN Raum des Roots, damit es auch bei gedrehtem/skaliertem Root (FBX-Achsenkorrektur)
        /// korrekt ist (Welt-AABB wäre dann nicht achsenparallel zum Root).
        /// </summary>
        private static void FitBoxCollider(GameObject root)
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
                Debug.LogWarning($"[Süßigkeiten] Kein Mesh in '{root.name}'; BoxCollider nicht gefittet.");
                return;
            }

            var box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        /// <summary>
        /// Leitet aus dem Prefab-Namen den SortKey <c>[Art, Farbe]</c> ab: Art aus dem Präfix
        /// (zuckerstange/gingerbre…/coockie), Farbe aus dem Rest nach dem ersten „_" (zweifarbige wie
        /// <c>pink_grün</c> bleiben EIN Farbwert <c>Pink_Grün</c>).
        /// </summary>
        private static string[] ParseFacets(string baseName)
        {
            var lower = baseName.ToLowerInvariant();
            string art;
            if (lower.StartsWith("zuckerstange")) art = "Zuckerstange";
            else if (lower.StartsWith("gingerbre")) art = "Lebkuchen";
            else if (lower.StartsWith("coockie") || lower.StartsWith("cookie")) art = "Keks";
            else art = Capitalize(baseName);

            var underscore = baseName.IndexOf('_');
            var color = underscore >= 0 && underscore < baseName.Length - 1
                ? Capitalize(baseName.Substring(underscore + 1))
                : "Standard";

            return new[] { art, color };
        }

        // Spawn-Höchstzahl je Variante anhand der Art (Kekse 75, sonst 42).
        private static int MaxFor(string art) => art == "Keks" ? MaxKeks : MaxSuessigkeit;

        // Item-eigener Einlage-Dreh-Offset je Art.
        private static Vector3 PlacedEulerFor(string art)
        {
            if (art == "Zuckerstange") return ZuckerstangePlacedEuler;
            if (art == "Keks") return KeksPlacedEuler;
            return Vector3.zero;
        }

        // Kapitalisiert jeden „_"-getrennten Teil (pink_grün -> Pink_Grün), Trenner bleibt erhalten.
        private static string Capitalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var parts = value.Split('_');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
                }
            }

            return string.Join("_", parts);
        }

        private static void BuildCatalog(List<ItemCatalog.Entry> entries)
        {
            EnsureFolder(DataFolder);

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
            var serialized = new SerializedObject(target);
            var prop = serialized.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[Süßigkeiten] Feld '{propName}' an {target.GetType().Name} nicht gefunden.");
                return;
            }

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }

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
