using CozySanta.Runtime.Carry;
using CozySanta.Runtime.DevTools;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für F4: hängt <see cref="Sortable"/> an die vorhandenen Objekt-Prefabs (Brief,
    /// TestPickup), ergänzt <see cref="SortTargetInteractable"/> am Fach-Prefab und verdrahtet
    /// SlotAnker + Lampe per Namen, und richtet am Player das Dev-Spawn-Tool (Tasten G/R) ein.
    /// Manipuliert nur Assets/Szene im Editor (Constitution V konform).
    /// </summary>
    public static class SortingSetup
    {
        private const string BriefPath = "Assets/_Project/Prefabs/Brief.prefab";
        private const string TestPickupPath = "Assets/_Project/Prefabs/TestPickup.prefab";
        private const string FachPath = "Assets/_Project/Prefabs/Fach.prefab";

        // Sortier-Vokabular ab Buch-Variante: 2 Facetten [Farbe, Symbol] (kein Kontinent mehr).
        // Gültige Farben:  Tannenrot, Tannengruen, Mitternachtsblau, Salbeigruen, Beere, Anthrazit, Karamell, Schokobraun
        // Gültige Symbole: Stern, Tannenbaum, Schneeflocke, Geschenk, Christbaumkugel, Herz, Glocke, Kerze, Schneemann, Schluessel
        private static readonly string[] BriefFacets = { "Tannenrot", "Stern" };
        private static readonly string[] TestPickupFacets = { "Mitternachtsblau", "Schneeflocke" };
        private static readonly string[] FachAccepts = { "Tannenrot", "Stern" };
        private const int FachRequiredCount = 3;

        [MenuItem("CozySanta/Setup F4 (Sortieren + Dev-Spawn)")]
        public static void Setup()
        {
            ConfigureSortable(BriefPath, BriefFacets);
            ConfigureSortable(TestPickupPath, TestPickupFacets);
            ConfigureFach(FachPath);
            ConfigureDevSpawner();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F4Setup] Sortable an Brief/TestPickup, SortTargetInteractable am Fach (SlotAnker+Lampe verdrahtet) " +
                      "und Dev-Spawn am Player eingerichtet. Szene speichern (Strg+S), dann Play. 'G' Liste, 'R' spawnen, 'E' interagieren.");
        }

        private static void ConfigureSortable(string path, string[] facets)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogWarning($"[F4Setup] Prefab nicht gefunden: {path}");
                return;
            }

            var sortable = root.GetComponent<Sortable>();
            if (sortable == null)
            {
                sortable = root.AddComponent<Sortable>();
            }

            SetStringArray(sortable, "facets", facets);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[F4Setup] Sortable gesetzt: {path} -> [{string.Join("/", facets)}]");
        }

        private static void ConfigureFach(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogWarning($"[F4Setup] Fach-Prefab nicht gefunden: {path}");
                return;
            }

            var fach = root.GetComponent<SortTargetInteractable>();
            if (fach == null)
            {
                fach = root.AddComponent<SortTargetInteractable>();
            }

            var serialized = new SerializedObject(fach);
            SetStringArrayProperty(serialized, "acceptedFacets", FachAccepts);
            var required = serialized.FindProperty("requiredCount");
            if (required != null)
            {
                required.intValue = FachRequiredCount;
            }

            var slot = FindChild(root.transform, "SlotAnker") ?? FindChild(root.transform, "SlotAnchor");
            SetObjectProperty(serialized, "slotAnchor", slot != null ? slot : null);

            var lamp = FindChild(root.transform, "Lampe");
            SetObjectProperty(serialized, "lamp", lamp != null ? lamp.gameObject : null);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[F4Setup] Fach konfiguriert: accepts=[{string.Join("/", FachAccepts)}], required={FachRequiredCount}, " +
                      $"SlotAnker={(slot != null ? "ok" : "FEHLT")}, Lampe={(lamp != null ? "ok" : "FEHLT")}");
        }

        private static void ConfigureDevSpawner()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[F4Setup] Kein 'Player' in der Szene; Dev-Spawn nicht eingerichtet.");
                return;
            }

            // Alten festen O-Spawner entfernen (wird durch das G/R-Menü ersetzt).
            var oldSpawner = player.GetComponent<TestPickupSpawner>();
            if (oldSpawner != null)
            {
                Object.DestroyImmediate(oldSpawner);
            }

            var menu = player.GetComponent<DevSpawnMenu>();
            if (menu == null)
            {
                menu = player.AddComponent<DevSpawnMenu>();
            }

            var brief = AssetDatabase.LoadAssetAtPath<GameObject>(BriefPath);
            var testPickup = AssetDatabase.LoadAssetAtPath<GameObject>(TestPickupPath);

            var serialized = new SerializedObject(menu);
            var list = serialized.FindProperty("prefabs");
            if (list != null)
            {
                list.ClearArray();
                AppendObject(list, brief);
                AppendObject(list, testPickup);
            }

            var origin = serialized.FindProperty("spawnOrigin");
            if (origin != null)
            {
                origin.objectReferenceValue = Camera.main != null ? Camera.main.transform : player.transform;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[F4Setup] DevSpawnMenu am Player: Liste [Brief, TestPickup], O-Spawner entfernt.");
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static void SetStringArray(Object target, string propName, string[] values)
        {
            var serialized = new SerializedObject(target);
            SetStringArrayProperty(serialized, propName, values);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringArrayProperty(SerializedObject serialized, string propName, string[] values)
        {
            var prop = serialized.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[F4Setup] Feld '{propName}' nicht gefunden an {serialized.targetObject.GetType().Name}.");
                return;
            }

            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void SetObjectProperty(SerializedObject serialized, string propName, Object value)
        {
            var prop = serialized.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"[F4Setup] Feld '{propName}' nicht gefunden an {serialized.targetObject.GetType().Name}.");
            }
        }

        private static void AppendObject(SerializedProperty arrayProp, Object value)
        {
            arrayProp.arraySize++;
            arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1).objectReferenceValue = value;
        }
    }
}
