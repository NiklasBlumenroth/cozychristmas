using CozySanta.Runtime.Abilities;
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
    /// Einmal-Setup F6/F12: erstellt das editor-authored Skillmenü-Prefab (SkillEntryUI) und die
    /// SkillMenuPanel-Hierarchie unter dem Canvas – Einträge/Gruppen kommen aus der editierbaren
    /// <see cref="SkillTableConfig"/> (wird angelegt, falls fehlend). Verdrahtet PlayerProgression inkl.
    /// der magischen Sortierhilfen (MagicSortAbility/MagicGatherAbility) und der Eingabe. Jeder Aufruf
    /// ersetzt Prefab + Panel vollständig.
    /// </summary>
    public static class ProgressionSetup
    {
        private const string EntryPrefabPath = "Assets/_Project/Prefabs/UI/SkillEntryUI.prefab";
        private const string SkillTablePath  = "Assets/_Project/Data/SkillTable.asset";

        [MenuItem("CozySanta/Setup F6 (Skill-Menü erstellen)")]
        public static void Setup()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[F6Setup] Kein Canvas in der Szene."); return; }

            var table  = EnsureSkillTable();
            var prefab = CreateEntryPrefab();
            BuildMenuPanel(canvas.transform, prefab, table);
            WireProgression(canvas, table);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F6Setup] SkillTable + SkillMenuPanel + PlayerProgression/Abilities verdrahtet. " +
                      "Szene speichern (Strg+S).");
        }

        // Lädt die Stufen-Tabelle oder legt sie mit dem Startentwurf an.
        private static SkillTableConfig EnsureSkillTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<SkillTableConfig>(SkillTablePath);
            if (table != null) return table;

            var dir = System.IO.Path.GetDirectoryName(SkillTablePath);
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/_Project", "Data");

            table = ScriptableObject.CreateInstance<SkillTableConfig>();
            table.ResetToDefaults();
            AssetDatabase.CreateAsset(table, SkillTablePath);
            Debug.Log($"[F6Setup] SkillTable angelegt: {SkillTablePath}");
            return table;
        }

        // ── PlayerProgression verdrahten ─────────────────────────────────────────

        private static void WireProgression(Canvas canvas, SkillTableConfig table)
        {
            var relay = Object.FindFirstObjectByType<PlayerInputRelay>();
            if (relay == null) { Debug.LogWarning("[F6Setup] Kein PlayerInputRelay in der Szene."); return; }

            var player = relay.gameObject;
            var view   = canvas.GetComponentInChildren<SkillMenuView>(includeInactive: true);

            var prog   = player.GetComponent<PlayerProgression>()  ?? player.AddComponent<PlayerProgression>();
            var dev    = player.GetComponent<SkillMenuDevTool>()   ?? player.AddComponent<SkillMenuDevTool>();
            var sortAb = player.GetComponent<MagicSortAbility>()   ?? player.AddComponent<MagicSortAbility>();
            var gathAb = player.GetComponent<MagicGatherAbility>() ?? player.AddComponent<MagicGatherAbility>();

            var soP = new SerializedObject(prog);
            ObjProp(soP, "carry",         player.GetComponent<PlayerCarry>());
            ObjProp(soP, "melt",          Object.FindFirstObjectByType<MeltController>());
            ObjProp(soP, "movement",      player.GetComponent<FirstPersonController>());
            ObjProp(soP, "autoSort",      sortAb);
            ObjProp(soP, "gather",        gathAb);
            ObjProp(soP, "skillTable",    table);
            ObjProp(soP, "skillMenuView", view);
            soP.ApplyModifiedPropertiesWithoutUndo();

            var soD = new SerializedObject(dev);
            ObjProp(soD, "progression", prog);
            soD.ApplyModifiedPropertiesWithoutUndo();

            var soR = new SerializedObject(relay);
            ObjProp(soR, "skillMenu",       view);
            ObjProp(soR, "autoSortAbility", sortAb);
            ObjProp(soR, "gatherAbility",   gathAb);
            soR.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[F6Setup] PlayerProgression, Abilities, SkillMenuDevTool und PlayerInputRelay verdrahtet.");
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

        private static void BuildMenuPanel(Transform canvas, GameObject entryPrefab, SkillTableConfig table)
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

            // Skill-Einträge aus der Stufen-Tabelle (Gruppen-Header bei Gruppenwechsel; Eintrag-Index = SkillId).
            var arr = so.FindProperty("skillEntries");
            arr.arraySize = System.Enum.GetValues(typeof(CozySanta.Core.Progression.SkillId)).Length;

            var lastGroup = string.Empty;
            foreach (var row in table.Rows)
            {
                if (row.group != lastGroup)
                {
                    GroupHeader(panel.transform, string.IsNullOrEmpty(row.group) ? "Sonstige" : row.group);
                    lastGroup = row.group;
                }

                AddEntry(row, panel.transform, entryPrefab, arr);
            }

            var close = MakeButton("CloseButton", panel.transform, "X", 0, 28);
            ObjProp(so, "closeButton", close);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddEntry(SkillTableConfig.Row row, Transform parent, GameObject prefab, SerializedProperty arr)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = $"SkillEntry_{row.displayName}";
            LE(go, height: 32);

            var nameT = go.transform.Find("SkillNameText")?.GetComponent<TMP_Text>();
            if (nameT != null) nameT.text = row.displayName;
            if (!row.unlockable) go.transform.Find("UnlockBadge")?.gameObject.SetActive(false);

            arr.GetArrayElementAtIndex((int)row.id).objectReferenceValue = go.GetComponent<SkillEntryUI>();
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
