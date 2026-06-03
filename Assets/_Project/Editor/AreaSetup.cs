using CozySanta.Runtime.Carry;
using CozySanta.Runtime.DevTools;
using CozySanta.Runtime.Player;
using CozySanta.Runtime.Progression;
using CozySanta.Runtime.Snow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup F7: erstellt TaskEntryUI-Prefab, LadeStation-Prefab, das editor-authored
    /// Area-HUD-Panel (oben rechts) und verdrahtet AreaTracker + PlayerInputRelay am Player.
    /// </summary>
    public static class AreaSetup
    {
        private const string EntryPrefabPath   = "Assets/_Project/Prefabs/UI/TaskEntryUI.prefab";
        private const string StationPrefabPath = "Assets/_Project/Prefabs/LadeStation.prefab";

        [MenuItem("CozySanta/Setup F7 (Area-HUD erstellen)")]
        public static void Setup()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[F7Setup] Kein Canvas in der Szene."); return; }

            var entryPrefab = CreateTaskEntryPrefab();
            BuildHudPanel(canvas.transform, entryPrefab);
            var stationPrefab = CreateLadeStationPrefab();
            WirePlayer(canvas, stationPrefab);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F7Setup] Area-HUD + LadeStation erstellt. Szene speichern (Strg+S).");
        }

        // ── TaskEntryUI-Prefab ───────────────────────────────────────────────────

        private static GameObject CreateTaskEntryPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath) != null)
                AssetDatabase.DeleteAsset(EntryPrefabPath);

            var root = UI("TaskEntryUI", null);
            SetSize(root, 260, 22);
            var hl = root.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth  = false;
            hl.spacing = 4;

            var entry = root.AddComponent<TaskEntryUI>();
            var nameT = AddTMP(UI("TaskNameText",  root.transform), "Aufgabe");
            LE(nameT.gameObject, flexible: true);
            var progT = AddTMP(UI("ProgressText", root.transform), "0 / 1");
            progT.alignment = TextAlignmentOptions.Right;
            LE(progT.gameObject, width: 72);

            Wire(entry, "taskNameText", nameT);
            Wire(entry, "progressText", progT);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, EntryPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ── Area-HUD-Panel ───────────────────────────────────────────────────────

        private static void BuildHudPanel(Transform canvas, GameObject entryPrefab)
        {
            var old = canvas.Find("AreaHudPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var panel = UI("AreaHudPanel", canvas);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.80f);
            AnchorTopRight(panel, 280, 260);

            var vl = panel.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(10, 10, 10, 10);
            vl.spacing = 4;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            var view = panel.AddComponent<AreaHudView>();
            var so   = new SerializedObject(view);

            var areaName = AddTMP(UI("AreaNameText", panel.transform), "Area");
            areaName.fontStyle = TMPro.FontStyles.Bold;
            LE(areaName.gameObject, height: 22);
            ObjProp(so, "areaNameText", areaName);

            // 4 Task-Einträge (überzählige werden zur Laufzeit ausgeblendet)
            var entries = so.FindProperty("taskEntries");
            entries.arraySize = 4;
            for (var i = 0; i < 4; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(entryPrefab, panel.transform);
                go.name = $"TaskEntry_{i}";
                LE(go, height: 22);
                entries.GetArrayElementAtIndex(i).objectReferenceValue = go.GetComponent<TaskEntryUI>();
            }

            MakeSeparator(panel.transform);

            var batteryBar = MakeSlider("BatteryBar", panel.transform, 14);
            ObjProp(so, "batteryBar", batteryBar);

            var chargeSection = UI("ChargeSection", panel.transform);
            LE(chargeSection, height: 18);
            chargeSection.SetActive(false);
            var chargeSectionVl = chargeSection.AddComponent<VerticalLayoutGroup>();
            chargeSectionVl.childControlWidth = chargeSectionVl.childControlHeight = true;
            chargeSectionVl.childForceExpandWidth = true; chargeSectionVl.childForceExpandHeight = false;
            var chargeLabel = AddTMP(UI("ChargeLabel", chargeSection.transform), "Laden...");
            LE(chargeLabel.gameObject, height: 16);
            var chargeBar = MakeSlider("ChargeBar", chargeSection.transform, 10);
            ObjProp(so, "chargeSection", chargeSection);
            ObjProp(so, "chargeBar",     chargeBar);

            MakeSeparator(panel.transform);

            var levelText = AddTMP(UI("LevelText", panel.transform), "Level 0");
            LE(levelText.gameObject, height: 20);
            var xpBar = MakeSlider("XpBar", panel.transform, 10);
            ObjProp(so, "levelText", levelText);
            ObjProp(so, "xpBar",     xpBar);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── LadeStation-Prefab ───────────────────────────────────────────────────

        private static GameObject CreateLadeStationPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(StationPrefabPath) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(StationPrefabPath);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LadeStation";
            go.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            var col = go.GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = false;
            go.AddComponent<LadeStation>();

            var saved = PrefabUtility.SaveAsPrefabAsset(go, StationPrefabPath);
            Object.DestroyImmediate(go);
            return saved;
        }

        // ── Player verdrahten ────────────────────────────────────────────────────

        private static void WirePlayer(Canvas canvas, GameObject stationPrefab)
        {
            var relay = Object.FindFirstObjectByType<PlayerInputRelay>();
            if (relay == null) { Debug.LogWarning("[F7Setup] Kein PlayerInputRelay gefunden."); return; }

            var player = relay.gameObject;
            var view   = canvas.GetComponentInChildren<AreaHudView>(includeInactive: true);

            var tracker = player.GetComponent<AreaTracker>() ?? player.AddComponent<AreaTracker>();
            var soT     = new SerializedObject(tracker);
            ObjProp(soT, "melt",         Object.FindFirstObjectByType<MeltController>());
            ObjProp(soT, "progression",  player.GetComponent<PlayerProgression>());
            ObjProp(soT, "hudView",      view);
            soT.ApplyModifiedPropertiesWithoutUndo();

            var soR = new SerializedObject(relay);
            ObjProp(soR, "areaHud", view);
            soR.ApplyModifiedPropertiesWithoutUndo();

            var spawn = player.GetComponent<DevSpawnMenu>();
            if (spawn != null && stationPrefab != null)
            {
                var soS  = new SerializedObject(spawn);
                var list = soS.FindProperty("prefabs");
                if (list != null)
                {
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = stationPrefab;
                    soS.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[F7Setup] AreaTracker verdrahtet; LadeStation in DevSpawnMenu eingetragen.");
        }

        // ── Hilfsmethoden ────────────────────────────────────────────────────────

        private static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI AddTMP(GameObject go, string text)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = 13; t.color = Color.white;
            t.enableAutoSizing = false;
            t.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            return t;
        }

        private static Slider MakeSlider(string name, Transform parent, float height)
        {
            var go = UI(name, parent);
            LE(go, height: height);
            var s = go.AddComponent<Slider>();
            s.interactable = false;
            s.value = 0f;
            return s;
        }

        private static void MakeSeparator(Transform parent)
        {
            var go = UI("Separator", parent);
            LE(go, height: 6);
        }

        private static void LE(GameObject go, float width = -1, float height = -1, bool flexible = false)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (width  > 0) { le.minWidth  = width;  le.preferredWidth  = width; }
            if (height > 0) { le.minHeight = height; le.preferredHeight = height; }
            if (flexible)   { le.flexibleWidth = 1; }
        }

        private static void SetSize(GameObject go, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void AnchorTopRight(GameObject go, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.one;
            rt.pivot     = Vector2.one;
            rt.anchoredPosition = new Vector2(-10f, -10f);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            ObjProp(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ObjProp(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[F7Setup] Feld '{field}' nicht gefunden.");
        }
    }
}
