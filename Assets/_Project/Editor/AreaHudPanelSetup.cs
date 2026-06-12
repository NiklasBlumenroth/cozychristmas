using CozySanta.Runtime.Progression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Editor
{
    /// <summary>
    /// Schlankes Tool: erzeugt nur das editor-authored <see cref="AreaHudView"/>-Panel (oben rechts)
    /// unter dem vorhandenen Canvas plus das TaskEntryUI-Prefab – und startet das Panel INAKTIV
    /// (eine <c>AreaZone</c> blendet es ein). Hängt nichts an Player/AreaTracker an; die Referenzen
    /// (AreaTracker.hudView, AreaZone.hudSection) verdrahtest du anschließend selbst im Inspector.
    /// </summary>
    public static class AreaHudPanelSetup
    {
        private const string EntryPrefabPath = "Assets/_Project/Prefabs/UI/TaskEntryUI.prefab";
        private const int    TaskEntryCount  = 4; // überzählige werden zur Laufzeit ausgeblendet

        [MenuItem("CozySanta/Bücher/Area-HUD-Panel erstellen")]
        public static void Setup()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[HUD-Panel] Kein Canvas in der Szene gefunden.");
                return;
            }

            var entryPrefab = CreateTaskEntryPrefab();
            var panel       = BuildHudPanel(canvas.transform, entryPrefab);
            panel.SetActive(false); // Zone blendet ein/aus

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);
            Debug.Log("[HUD-Panel] 'AreaHudPanel' (inaktiv) erstellt. Jetzt verdrahten: " +
                      "AreaTracker.hudView → Panel und AreaZone.hudSection → Panel. Szene speichern (Strg+S).");
        }

        private static GameObject CreateTaskEntryPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath) != null)
                return AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath);

            EnsureFolder("Assets/_Project/Prefabs/UI");

            var root = UI("TaskEntryUI", null);
            SetSize(root, 260, 22);
            var hl = root.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth  = false;
            hl.spacing = 4;

            var entry = root.AddComponent<TaskEntryUI>();
            var nameT = AddTMP(UI("TaskNameText", root.transform), "Aufgabe");
            LE(nameT.gameObject, flexible: true);
            var progT = AddTMP(UI("ProgressText", root.transform), "0 / 1");
            progT.alignment = TextAlignmentOptions.Right;
            LE(progT.gameObject, width: 120);

            Wire(entry, "taskNameText", nameT);
            Wire(entry, "progressText", progT);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, EntryPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildHudPanel(Transform canvas, GameObject entryPrefab)
        {
            // Vorhandene Panels NICHT löschen (jede Area hat ihr eigenes) -> eindeutigen Namen wählen.
            var name = "AreaHudPanel";
            for (var i = 1; canvas.Find(name) != null; i++) name = $"AreaHudPanel ({i})";

            var panel = UI(name, canvas);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.80f);
            AnchorTopRight(panel, 300, 240);

            var vl = panel.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(10, 10, 10, 10);
            vl.spacing = 4;
            vl.childControlWidth = vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            var view = panel.AddComponent<AreaHudView>();
            var so   = new SerializedObject(view);

            var areaName = AddTMP(UI("AreaNameText", panel.transform), "Area");
            areaName.fontStyle = FontStyles.Bold;
            LE(areaName.gameObject, height: 22);
            ObjProp(so, "areaNameText", areaName);

            var entries = so.FindProperty("taskEntries");
            entries.arraySize = TaskEntryCount;
            for (var i = 0; i < TaskEntryCount; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(entryPrefab, panel.transform);
                go.name = $"TaskEntry_{i}";
                LE(go, height: 22);
                entries.GetArrayElementAtIndex(i).objectReferenceValue = go.GetComponent<TaskEntryUI>();
            }

            MakeSeparator(panel.transform);
            var batteryBar = MakeSlider("BatteryBar", panel.transform, 14, new Color(1f, 0.75f, 0.1f, 1f));
            ObjProp(so, "batteryBar", batteryBar);

            MakeSeparator(panel.transform);
            var levelText = AddTMP(UI("LevelText", panel.transform), "Level 0");
            LE(levelText.gameObject, height: 20);
            var xpBar = MakeSlider("XpBar", panel.transform, 10, new Color(0.7f, 0.3f, 1f, 1f));
            ObjProp(so, "levelText", levelText);
            ObjProp(so, "xpBar",     xpBar);

            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
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
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private static Slider MakeSlider(string name, Transform parent, float height, Color fillColor)
        {
            var go = UI(name, parent);
            LE(go, height: height);
            go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);

            var fillArea = UI("Fill Area", go.transform);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = new Vector2(-4f, 0f);

            var fill = UI("Fill", fillArea.transform);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

            var s = go.AddComponent<Slider>();
            s.interactable = false;
            s.value        = 1f;
            s.fillRect     = fillRT;
            return s;
        }

        private static void MakeSeparator(Transform parent) => LE(UI("Separator", parent), height: 6);

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
            else Debug.LogWarning($"[HUD-Panel] Feld '{field}' nicht gefunden.");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf   = System.IO.Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
