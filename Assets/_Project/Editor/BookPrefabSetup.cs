using System.IO;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erzeugt aus einem Buch-Mesh (.blend unter <c>Prefabs/post/Books</c>) ein spielbares Prefab –
    /// analog zum bestehenden <c>Buch.prefab</c>: <see cref="Rigidbody"/> (Masse 1, Gravity an),
    /// <see cref="PickupInteractable"/> (Gewicht-Default 0.3) und <see cref="Sortable"/> am Root,
    /// dazu ein <see cref="BoxCollider"/>, der automatisch an die Mesh-Bounds gefittet wird.
    /// Die Facetten sind <c>[Farbe, Form]</c> aus dem Dateinamen (Schema <c>Buch{Farbe}{Form}</c>).
    /// Ergebnis ist ein Prefab-Variant des Modells, damit Mesh-/Material-Referenz korrekt bleiben.
    /// Manipuliert nur Assets im Editor (Constitution V konform).
    /// </summary>
    public static class BookPrefabSetup
    {
        private const string OutputFolder = "Assets/_Project/Prefabs/Books";
        private const string ExampleMesh = "Assets/_Project/Prefabs/post/Books/BuchRotStern.blend";

        // Reihenfolge wichtig: zusammengesetzte Farbe (HellGrün) vor ihrem Bestandteil (Grün) prüfen.
        private static readonly string[] Colors =
        {
            "HellGrün", "Blau", "Braun", "Gelb", "Grün", "Pink", "Rot", "Schwarz"
        };

        [MenuItem("CozySanta/Bücher/Buch-Prefab exemplarisch (BuchRotStern)")]
        public static void CreateExample()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ExampleMesh);
            if (model == null)
            {
                Debug.LogError($"[BuchPrefab] Mesh nicht gefunden: {ExampleMesh}");
                return;
            }

            var path = CreatePrefabFromModel(model, ExampleMesh);
            if (path == null)
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("CozySanta/Bücher/Buch-Prefabs aus Auswahl (.blend) erzeugen")]
        public static void CreateFromSelection()
        {
            var created = 0;
            foreach (var obj in Selection.objects)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".blend"))
                {
                    continue;
                }

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (model != null && CreatePrefabFromModel(model, assetPath) != null)
                {
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuchPrefab] {created} Buch-Prefab(s) erzeugt in {OutputFolder}.");
        }

        private static string CreatePrefabFromModel(GameObject model, string sourcePath)
        {
            EnsureFolder(OutputFolder);

            var baseName = Path.GetFileNameWithoutExtension(sourcePath); // z. B. BuchRotStern
            var facets = ParseFacets(baseName);

            // Modell instanziieren (= Quelle des Prefab-Variants). Ursprung + Identity, damit die
            // Welt-AABB der Renderer achsenparallel zum Root liegt und exakt in lokale Maße umrechenbar ist.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = baseName;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            var body = instance.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = instance.AddComponent<Rigidbody>();
            }

            body.mass = 1f;
            body.useGravity = true;
            body.isKinematic = false;

            if (instance.GetComponent<PickupInteractable>() == null)
            {
                instance.AddComponent<PickupInteractable>(); // promptText/weight aus Feld-Defaults (Aufnehmen / 0.3)
            }

            var sortable = instance.GetComponent<Sortable>();
            if (sortable == null)
            {
                sortable = instance.AddComponent<Sortable>();
            }

            SetStringArray(sortable, "facets", facets);

            FitBoxCollider(instance);

            var outPath = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{baseName}.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, outPath, out var success);
            Object.DestroyImmediate(instance);

            if (!success || prefab == null)
            {
                Debug.LogError($"[BuchPrefab] Speichern fehlgeschlagen: {outPath}");
                return null;
            }

            Debug.Log($"[BuchPrefab] {outPath} erzeugt. Facetten: [{string.Join(", ", facets)}]");
            return outPath;
        }

        /// <summary>Fittet einen <see cref="BoxCollider"/> am Root an die kombinierten Renderer-Bounds.</summary>
        private static void FitBoxCollider(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[BuchPrefab] Kein MeshRenderer in '{root.name}'; BoxCollider nicht gefittet.");
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var box = root.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = root.AddComponent<BoxCollider>();
            }

            // Root ist rotationsfrei -> die Welt-AABB ist achsenparallel zum Root. Center in lokale
            // Koordinaten transformieren, Size komponentenweise um den (lossy) Scale bereinigen.
            var scale = root.transform.lossyScale;
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(
                SafeDiv(bounds.size.x, scale.x),
                SafeDiv(bounds.size.y, scale.y),
                SafeDiv(bounds.size.z, scale.z));
        }

        private static float SafeDiv(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }

        /// <summary>Trennt <c>Buch{Farbe}{Form}</c> in <c>[Farbe, Form]</c> anhand der bekannten Farbliste.</summary>
        private static string[] ParseFacets(string baseName)
        {
            var rest = baseName.StartsWith("Buch") ? baseName.Substring("Buch".Length) : baseName;
            foreach (var color in Colors)
            {
                if (rest.StartsWith(color))
                {
                    return new[] { color, rest.Substring(color.Length) };
                }
            }

            Debug.LogWarning($"[BuchPrefab] Konnte Farbe/Form aus '{baseName}' nicht trennen; nutze Gesamtnamen als einzige Facette.");
            return new[] { rest };
        }

        private static void SetStringArray(Object target, string propName, string[] values)
        {
            var serialized = new SerializedObject(target);
            var prop = serialized.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[BuchPrefab] Feld '{propName}' an {target.GetType().Name} nicht gefunden.");
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
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
