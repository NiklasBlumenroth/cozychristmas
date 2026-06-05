using CozySanta.Runtime.Areas;
using CozySanta.Runtime.DevTools;
using CozySanta.Runtime.Keys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup F8: erstellt GameManager (KeyInventoryManager + AreaManager), Schlüssel-Icon-HUD,
    /// Schlüssel-Prefab und Tor-Prefab. AreaZone-Prefab zum manuellen Platzieren im Level.
    /// </summary>
    public static class KeyGateSetup
    {
        private const string KeyPrefabPath      = "Assets/_Project/Prefabs/Keys/KeyItem.prefab";
        private const string GatePrefabPath     = "Assets/_Project/Prefabs/Gates/Gate.prefab";
        private const string AreaZonePrefabPath = "Assets/_Project/Prefabs/Gates/AreaZone.prefab";

        [MenuItem("CozySanta/Setup F8 (Schlüssel, Tore, Zones)")]
        public static void Setup()
        {
            CreateGameManager();
            CreateKeyHudSlots();
            var keyPrefab  = CreateKeyPrefab();
            var gatePrefab = CreateGatePrefab();
            CreateAreaZonePrefab();
            AddToSpawnMenu(keyPrefab, gatePrefab);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F8Setup] GameManager, Prefabs und HUD-Slots erstellt. Szene speichern (Strg+S).");
        }

        // ── GameManager ──────────────────────────────────────────────────────────

        private static void CreateGameManager()
        {
            var existing = GameObject.Find("GameManager");
            if (existing == null)
            {
                existing = new GameObject("GameManager");
                Debug.Log("[F8Setup] GameManager-Objekt erstellt.");
            }

            if (existing.GetComponent<KeyInventoryManager>() == null)
                existing.AddComponent<KeyInventoryManager>();

            if (existing.GetComponent<AreaManager>() == null)
                existing.AddComponent<AreaManager>();
        }

        // ── Schlüssel-Icon-Slots im HUD ──────────────────────────────────────────

        private static void CreateKeyHudSlots()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogWarning("[F8Setup] Kein Canvas – KeyHudPanel übersprungen."); return; }

            var old = canvas.transform.Find("KeyHudPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var panel = new GameObject("KeyHudPanel");
            panel.AddComponent<RectTransform>();
            panel.transform.SetParent(canvas.transform, false);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.75f);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -10f);
            rt.sizeDelta = new Vector2(180f, 44f);

            var hl = panel.AddComponent<HorizontalLayoutGroup>();
            hl.padding  = new RectOffset(6, 6, 6, 6);
            hl.spacing  = 6;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;

            var view    = panel.AddComponent<KeyHudView>();
            var so      = new SerializedObject(view);
            var slots   = so.FindProperty("iconSlots");
            const int   SlotCount = 5;
            slots.arraySize = SlotCount;

            for (var i = 0; i < SlotCount; i++)
            {
                var slot = new GameObject($"KeySlot_{i}");
                slot.AddComponent<RectTransform>();
                slot.transform.SetParent(panel.transform, false);
                var le = slot.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 32f;
                le.minHeight = le.preferredHeight = 32f;
                var img = slot.AddComponent<Image>();
                img.color = new Color(0.9f, 0.85f, 0.2f, 1f);
                slot.SetActive(false);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = img;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var mgr = Object.FindFirstObjectByType<KeyInventoryManager>();
            if (mgr != null)
            {
                var soM = new SerializedObject(mgr);
                var p   = soM.FindProperty("hudView");
                if (p != null) p.objectReferenceValue = view;
                soM.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Schlüssel-Prefab ─────────────────────────────────────────────────────

        private static GameObject CreateKeyPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(KeyPrefabPath);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "KeyItem";
            go.transform.localScale = Vector3.one * 0.2f;
            var col = go.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = false;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.useGravity = true;

            go.AddComponent<KeyPickup>();
            go.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            var saved = PrefabUtility.SaveAsPrefabAsset(go, KeyPrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[F8Setup] Schlüssel-Prefab erstellt: {KeyPrefabPath}");
            return saved;
        }

        // ── Tor-Prefab ───────────────────────────────────────────────────────────

        private static GameObject CreateGatePrefab()
        {
            // Immer neu erstellen damit Fixes am Prefab ankommen.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefabPath) != null)
                AssetDatabase.DeleteAsset(GatePrefabPath);

            var root = new GameObject("Gate");

            // Proximity-Trigger direkt am Root-Objekt – OnTriggerEnter muss auf
            // demselben GameObject liegen wie der GateController.
            var bc = root.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size      = new Vector3(2f, 2.5f, 2f);
            bc.center    = new Vector3(0f, 1f, 0f);

            // Sichtbarer Türflügel (dreht sich)
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "DoorPivot";
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = new Vector3(0.5f, 1f, 0f);
            door.transform.localScale    = new Vector3(1f, 2f, 0.1f);
            Object.DestroyImmediate(door.GetComponent<BoxCollider>());

            // Türrahmen (statisch, nur visuell)
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 1f, 0f);
            frame.transform.localScale    = new Vector3(1.15f, 2.1f, 0.12f);
            Object.DestroyImmediate(frame.GetComponent<BoxCollider>());

            var ctrl = root.AddComponent<GateController>();
            var so   = new SerializedObject(ctrl);
            var pivP = so.FindProperty("doorPivot");
            if (pivP != null) pivP.objectReferenceValue = door.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, GatePrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[F8Setup] Tor-Prefab (neu) erstellt: {GatePrefabPath}");
            return saved;
        }

        // ── AreaZone-Prefab ──────────────────────────────────────────────────────

        private static void CreateAreaZonePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AreaZonePrefabPath) != null) return;

            var go = new GameObject("AreaZone");
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size      = new Vector3(10f, 4f, 10f);
            go.AddComponent<AreaZone>();

            PrefabUtility.SaveAsPrefabAsset(go, AreaZonePrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[F8Setup] AreaZone-Prefab erstellt: {AreaZonePrefabPath}");
        }

        // ── DevSpawnMenu verdrahten ──────────────────────────────────────────────

        private static void AddToSpawnMenu(params GameObject[] newPrefabs)
        {
            var spawnMenu = Object.FindFirstObjectByType<DevSpawnMenu>();
            if (spawnMenu == null) { Debug.LogWarning("[F8Setup] Kein DevSpawnMenu gefunden."); return; }

            var so   = new SerializedObject(spawnMenu);
            var list = so.FindProperty("prefabs");
            if (list == null) return;

            foreach (var prefab in newPrefabs)
            {
                if (prefab == null) continue;

                // Duplikate vermeiden
                var alreadyIn = false;
                for (var i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
                    { alreadyIn = true; break; }
                }
                if (alreadyIn) continue;

                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = prefab;
                Debug.Log($"[F8Setup] '{prefab.name}' zu DevSpawnMenu hinzugefügt.");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
