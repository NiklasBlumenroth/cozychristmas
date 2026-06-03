using CozySanta.Runtime.Carry;
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
    /// Einmal-Setup F6: erstellt das editor-authored Skillmenü-Prefab (SkillEntryUI) und die
    /// vollständige SkillMenuPanel-Hierarchie unter dem vorhandenen Canvas in der SampleScene.
    /// Laufzeitcode (PlayerProgression) bindet später an die fertige Hierarchie.
    /// Jeder Aufruf löscht und ersetzt Prefab + Panel vollständig.
    /// </summary>
    public static class ProgressionSetup
    {
        private const string EntryPrefabPath = "Assets/_Project/Prefabs/UI/SkillEntryUI.prefab";

        private static readonly string[] SkillNames =
            { "Schmelzstärke", "Kegelgröße", "Akku", "Tragkraft", "Laufgeschw.", "Sortierblick", "Objektanz." };

        private static readonly bool[] Unlockable =
            { false, false, false, false, false, true, true };

        [MenuItem("CozySanta/Setup F6 (Skill-Menü erstellen)")]
        public static void Setup()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[F6Setup] Kein Canvas in der Szene."); return; }

            var prefab = CreateEntryPrefab();
            BuildMenuPanel(canvas.transform, prefab);
            WireProgression(canvas);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F6Setup] SkillMenuPanel + PlayerProgression verdrahtet. Szene speichern (Strg+S).");
        }

        // ── PlayerProgression verdrahten ─────────────────────────────────────────

        private static void WireProgression(Canvas canvas)
        {
            var relay = Object.FindFirstObjectByType<PlayerInputRelay>();
            if (relay == null) { Debug.LogWarning("[F6Setup] Kein PlayerInputRelay in der Szene."); return; }

            var player = relay.gameObject;
            var view   = canvas.GetComponentInChildren<SkillMenuView>(includeInactive: true);

            var prog   = player.GetComponent<PlayerProgression>()  ?? player.AddComponent<PlayerProgression>();
            var dev    = player.GetComponent<SkillMenuDevTool>()   ?? player.AddComponent<SkillMenuDevTool>();

            var soP = new SerializedObject(prog);
            ObjProp(soP, "carry",         player.GetComponent<PlayerCarry>());
            ObjProp(soP, "melt",          Object.FindFirstObjectByType<MeltController>());
            ObjProp(soP, "movement",      player.GetComponent<FirstPersonController>());
            ObjProp(soP, "skillMenuView", view);
            soP.ApplyModifiedPropertiesWithoutUndo();

            var soD = new SerializedObject(dev);
            ObjProp(soD, "progression", prog);
            soD.ApplyModifiedPropertiesWithoutUndo();

            var soR = new SerializedObject(relay);
            ObjProp(soR, "skillMenu", view);
            soR.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[F6Setup] PlayerProgression, SkillMenuDevTool und PlayerInputRelay verdrahtet.");
        }

        // ── SkillEntryUI-Prefab ──────────────────────────────────────────────────

        private static GameObject CreateEntryPrefab()
        {
            // Vorhandenes Prefab immer ersetzen damit Layout-Fixes ankommen
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath) != null)
                AssetDatabase.DeleteAsset(EntryPrefabPath);

            var root = UI("SkillEntryUI", null);
            SetSize(root, 440, 32);

            var hl = root.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = true;
            hl.spacing = 6;

            var entry = root.AddComponent<SkillEntryUI>();

            var nameT = AddTMP(UI("SkillNameText",  root.transform), "Name");
            LE(nameT.gameObject, flexible: true);
            var lvlT  = AddTMP(UI("SkillLevelText", root.transform), "0 / 20");
            LE(lvlT.gameObject, width: 66);
            var valT  = AddTMP(UI("SkillValueText", root.transform), "—");
            LE(valT.gameObject, width: 80);
            var badge = UI("UnlockBadge", root.transform);
            AddTMP(badge, "✓");
            LE(badge, width: 24);
            badge.SetActive(false);
            var btn = MakeButton("InvestButton", root.transform, "+1", 44, 0);

            Wire(entry, "skillNameText",  nameT);
            Wire(entry, "skillLevelText", lvlT);
            Wire(entry, "skillValueText", valT);
            Wire(entry, "unlockBadge",    badge);
            Wire(entry, "investButton",   btn);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, EntryPrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ── SkillMenuPanel ───────────────────────────────────────────────────────

        private static void BuildMenuPanel(Transform canvas, GameObject entryPrefab)
        {
            var old = canvas.Find("SkillMenuPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var panel = UI("SkillMenuPanel", canvas);
            panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.93f);
            Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(460, 620));
            panel.SetActive(false);

            var vl = panel.AddComponent<VerticalLayoutGroup>();
            vl.padding                = new RectOffset(12, 12, 12, 12);
            vl.spacing                = 5;
            vl.childControlWidth      = true;
            vl.childControlHeight     = true;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            var view = panel.AddComponent<SkillMenuView>();
            var so   = new SerializedObject(view);

            // Header
            var lvlText    = AddTMP(UI("LevelText",           panel.transform), "Level 1");
            LE(lvlText.gameObject, height: 26);
            var xpBarGo    = UI("XpBar", panel.transform);
            LE(xpBarGo, height: 14);
            var xpBar      = xpBarGo.AddComponent<Slider>();
            xpBar.interactable = false;
            var xpText     = AddTMP(UI("XpText",              panel.transform), "0 / 100 XP");
            LE(xpText.gameObject, height: 20);
            var pointsText = AddTMP(UI("AvailablePointsText", panel.transform), "Skillpunkte: 0");
            LE(pointsText.gameObject, height: 20);

            ObjProp(so, "levelText",           lvlText);
            ObjProp(so, "xpBar",               xpBar);
            ObjProp(so, "xpText",              xpText);
            ObjProp(so, "availablePointsText", pointsText);

            // Skill-Einträge (Gruppen + Zeilen)
            var arr = so.FindProperty("skillEntries");
            arr.arraySize = 7;

            GroupHeader(panel.transform, "Lampe");
            for (var i = 0; i < 3; i++) AddEntry(i, panel.transform, entryPrefab, arr);
            GroupHeader(panel.transform, "Tragen");
            AddEntry(3, panel.transform, entryPrefab, arr);
            GroupHeader(panel.transform, "Bewegung");
            AddEntry(4, panel.transform, entryPrefab, arr);
            GroupHeader(panel.transform, "Freischalt-Skills");
            AddEntry(5, panel.transform, entryPrefab, arr);
            AddEntry(6, panel.transform, entryPrefab, arr);

            var close = MakeButton("CloseButton", panel.transform, "X", 0, 28);
            ObjProp(so, "closeButton", close);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddEntry(int idx, Transform parent, GameObject prefab, SerializedProperty arr)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"SkillEntry_{SkillNames[idx]}";
            LE(go, height: 32);

            var nameT = go.transform.Find("SkillNameText")?.GetComponent<TMP_Text>();
            if (nameT != null) nameT.text = SkillNames[idx];
            if (!Unlockable[idx]) go.transform.Find("UnlockBadge")?.gameObject.SetActive(false);

            arr.GetArrayElementAtIndex(idx).objectReferenceValue = go.GetComponent<SkillEntryUI>();
        }

        private static void GroupHeader(Transform parent, string label)
        {
            var t = AddTMP(UI($"Header_{label}", parent), label);
            t.fontStyle = FontStyles.Bold;
            LE(t.gameObject, height: 22);
        }

        // ── UI-Hilfsmethoden ─────────────────────────────────────────────────────

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
            t.text            = text;
            t.fontSize        = 13;
            t.color           = Color.white;
            t.enableAutoSizing = false;
            t.overflowMode    = TextOverflowModes.Ellipsis;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, float w, float h)
        {
            var go = UI(name, parent);
            go.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f, 1f);
            var btn = go.AddComponent<Button>();
            if (w > 0 || h > 0) LE(go, w > 0 ? w : -1, h > 0 ? h : -1);
            var tGo = UI("Text", go.transform);
            var rt  = tGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            AddTMP(tGo, label);
            return btn;
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

        private static void Anchor(GameObject go, Vector2 anchor, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
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
            else Debug.LogWarning($"[F6Setup] Feld '{field}' nicht gefunden.");
        }
    }
}
