using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für einen Mond am Nachthimmel: erzeugt eine unbeleuchtete Kugel (URP-Unlit),
    /// die unabhängig vom dunklen Nacht-Ambiente sichtbar leuchtet, plus ein sehr schwaches
    /// „Mondlicht" als Glow-Andeutung. Läuft im offenen Editor über das Menü „CozySanta".
    /// Position/Größe danach frei im Inspector justierbar.
    /// </summary>
    public static class MoonSetup
    {
        private const string MaterialPath = "Assets/_Project/Materials/M_Mond.mat";

        [MenuItem("CozySanta/Setup Mond (Nachthimmel)")]
        public static void Setup()
        {
            // 1) Unlit-Material: der Mond leuchtet unabhängig von der Szenenbeleuchtung.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    Debug.LogError("[MoonSetup] Shader 'Universal Render Pipeline/Unlit' nicht gefunden.");
                    return;
                }

                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            mat.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.86f)); // blasses, warmes Mondweiß
            EditorUtility.SetDirty(mat);

            // 2) Mond-Kugel erstellen/finden (rein visuell, ohne Collider/Schatten).
            var moon = GameObject.Find("Mond");
            if (moon == null)
            {
                moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moon.name = "Mond";
            }

            var col = moon.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }

            var mr = moon.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Weit weg und hoch am Himmel platzieren, groß genug zum Lesen als Mondscheibe.
            moon.transform.position = new Vector3(60f, 55f, 95f);
            moon.transform.localScale = Vector3.one * 18f;

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[MoonSetup] 'Mond' erstellt/aktualisiert. Position/Größe im Inspector anpassbar.");
        }
    }
}
