using System.Collections.Generic;
using System.Text;
using CozySanta.Runtime.Items;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Macht alle Spawn-Katalog-Objekte emissiv, damit sie trotz warmer/dunkler Cozy-Beleuchtung erkennbar
    /// bleiben (#3). Quelle der Wahrheit sind die <see cref="ItemCatalog"/>-Assets: alles, was aus einem
    /// Katalog gespawnt wird, leuchtet – inklusive der Deko-Abbilder in Schildern (teilen dasselbe
    /// Prefab-Material), Nicht-Katalog-Objekte (Schildrahmen, Wände …) bleiben dunkel. Die Basis-Textur wird
    /// als Emissions-Textur übernommen, sodass das Glühen die echten Farben trägt; ab Stärke &gt; 1 greift
    /// euer vorhandener URP-Bloom (#5 = bloom-ready; den Bloom-Threshold justiert man danach von Hand).
    /// Reines Editor-Authoring (keine Core-Logik/Tests – dokumentierte Nicht-Unit-Ausnahme analog
    /// <c>BookPrefabSetup</c>/<c>BakerySweetItemSetup</c>).
    /// </summary>
    public sealed class SortableEmissionTool : EditorWindow
    {
        private const string EmissionKeyword = "_EMISSION";
        private const string EmissionColorProp = "_EmissionColor";
        private const string EmissionMapProp = "_EmissionMap";
        private static readonly string[] BaseColorProps = { "_BaseColor", "_Color" };
        private static readonly string[] BaseMapProps = { "_BaseMap", "_MainTex" };

        // Stärke skaliert die Emission. Mit Emissions-Textur (= Basis-Textur) trägt das Glühen die echten
        // Farben; 0.6 ist ein dezenter, gut lesbarer Start. > 1 lässt helle Stellen über 1 steigen → Bloom.
        [SerializeField] private float strength = 0.6f;

        [MenuItem("CozySanta/Sortierobjekte/Emission (Sichtbarkeit) …")]
        public static void Open()
        {
            var window = GetWindow<SortableEmissionTool>(true, "Sortierobjekt-Emission");
            window.minSize = new Vector2(360, 170);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Setzt Emission auf die Materialien aller Objekte aus den Spawn-Katalogen (ItemCatalog), " +
                "damit sie in warmem/dunklem Licht sichtbar bleiben. Nur Katalog-Objekte leuchten – " +
                "Schildrahmen, Wände usw. bleiben dunkel; Deko-Abbilder in Schildern glühen mit, weil sie " +
                "dasselbe Prefab-Material teilen. Die Basis-Textur wird als Emissions-Textur übernommen → " +
                "das Glühen trägt die echten Farben (kein flaches Weiß). Emission = Basis-Textur × Basisfarbe × Stärke.\n\n" +
                "Für Bloom Stärke > 1 wählen und danach den Bloom-Threshold im Global Volume justieren.",
                MessageType.Info);

            strength = EditorGUILayout.Slider(
                new GUIContent("Emissions-Stärke", "Skaliert die Emission. 0.4–0.8 = dezent lesbar, > 1 = bloom-fähig."),
                strength, 0f, 3f);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Emission anwenden", GUILayout.Height(28)))
                {
                    Apply(strength);
                }

                if (GUILayout.Button("Zurücksetzen", GUILayout.Height(28)))
                {
                    Apply(0f, reset: true);
                }
            }
        }

        private static void Apply(float strength, bool reset = false)
        {
            var materials = CollectCatalogMaterials();
            if (materials.Count == 0)
            {
                Debug.LogWarning("[Emission] Keine Katalog-Materialien gefunden (keine ItemCatalog-Assets " +
                                 "oder leere Kataloge). Zuerst die Katalog-Setups ausführen.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(reset ? "Sortier-Emission zurücksetzen" : "Sortier-Emission anwenden");
            var group = Undo.GetCurrentGroup();

            var log = new StringBuilder();
            var changed = 0;
            foreach (var mat in materials)
            {
                if (mat == null || !mat.HasProperty(EmissionColorProp)) continue;

                Undo.RecordObject(mat, "Sortier-Emission");
                if (reset)
                {
                    mat.DisableKeyword(EmissionKeyword);
                    mat.SetColor(EmissionColorProp, Color.black);
                    if (mat.HasProperty(EmissionMapProp)) mat.SetTexture(EmissionMapProp, null);
                }
                else
                {
                    mat.EnableKeyword(EmissionKeyword);
                    // Nicht ins Lightmap backen – die Emission dient nur der Lesbarkeit/dem Bloom.
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    // Basis-Textur als Emissions-Textur übernehmen → das Glühen trägt die echten Farben
                    // statt flachem Weiß. _EmissionColor bleibt dann nur Tönung × Stärke.
                    var baseMap = BaseMapOf(mat);
                    if (baseMap != null && mat.HasProperty(EmissionMapProp))
                    {
                        mat.SetTexture(EmissionMapProp, baseMap);
                    }

                    mat.SetColor(EmissionColorProp, BaseColorOf(mat) * strength);
                }

                EditorUtility.SetDirty(mat);
                log.AppendLine($"  {mat.name}");
                changed++;
            }

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Emission] {(reset ? "Zurückgesetzt" : $"Aktiviert (Stärke {strength:0.00})")}: " +
                      $"{changed} Material(ien).\n{log}");
        }

        /// <summary>Basisfarbe des Materials (für die Emissions-Tönung); Fallback Weiß.</summary>
        private static Color BaseColorOf(Material mat)
        {
            foreach (var prop in BaseColorProps)
            {
                if (mat.HasProperty(prop)) return mat.GetColor(prop);
            }

            return Color.white;
        }

        /// <summary>Basis-/Albedo-Textur des Materials (wird als Emissions-Textur übernommen); kann null sein.</summary>
        private static Texture BaseMapOf(Material mat)
        {
            foreach (var prop in BaseMapProps)
            {
                if (mat.HasProperty(prop) && mat.GetTexture(prop) != null) return mat.GetTexture(prop);
            }

            return null;
        }

        // Sammelt die geteilten Materialien aller Prefabs, die in einem ItemCatalog stehen (dedupliziert,
        // damit jedes Material nur einmal bearbeitet wird). Katalog = einzige Quelle der Wahrheit fürs Glühen.
        private static HashSet<Material> CollectCatalogMaterials()
        {
            var result = new HashSet<Material>();

            foreach (var guid in AssetDatabase.FindAssets("t:ItemCatalog"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(path);
                if (catalog == null) continue;

                foreach (var key in catalog.Keys)
                {
                    var prefab = catalog.Get(key);
                    if (prefab != null) AddMaterials(prefab, result);
                }
            }

            return result;
        }

        private static void AddMaterials(GameObject root, HashSet<Material> into)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null) into.Add(mat);
                }
            }
        }
    }
}
