using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Editor
{
    /// <summary>
    /// Richtet einen prozeduralen Nachthimmel ein – OHNE externe Assets: erzeugt ein Material aus dem
    /// <c>CozySanta/NightSky</c>-Shader (Vollmond + Sternenhimmel), setzt es als Szenen-Skybox, stellt ein
    /// stimmiges Nacht-Ambient ein und richtet das vorhandene Directional Light als kühles, gedämpftes
    /// Mondlicht aus (passend zur Mond-Richtung im Shader). Reiner Editor-/Optik-Schritt (keine Fachlogik,
    /// dokumentierte Nicht-Unit-Ausnahme analog SnowMelt). Mehrfach ausführbar (idempotent).
    /// </summary>
    public static class NightSkySetup
    {
        private const string ShaderName = "CozySanta/NightSky";
        private const string MatFolder = "Assets/_Project/Materials";
        private const string MatPath = MatFolder + "/M_NightSky.mat";

        // Mond-Richtung am Himmel (muss zur Skybox passen). Licht kommt aus dieser Richtung.
        private static readonly Vector3 MoonDir = new Vector3(0.35f, 0.55f, -0.75f).normalized;

        [MenuItem("CozySanta/Umgebung/Nachthimmel einrichten (Mond + Sterne)")]
        public static void Setup()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[Nachthimmel] Shader '{ShaderName}' nicht gefunden. Erst von Unity kompilieren lassen.");
                return;
            }

            var mat = CreateOrUpdateMaterial(shader);

            // Skybox + Ambient (Szenen-/Lighting-Settings).
            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.11f, 1f);

            var moon = SetupMoonLight();
            var cam = SetupCameraSkybox();

            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();

            Debug.Log($"[Nachthimmel] Skybox '{Path.GetFileName(MatPath)}' gesetzt, Ambient = Nacht, " +
                      $"Mondlicht {(moon ? "eingerichtet" : "NICHT gefunden")}, " +
                      $"Kamera-Hintergrund {(cam ? "= Skybox" : "NICHT gefunden")}. " +
                      "Szene speichern (Strg+S). Optik im Material M_NightSky feinjustierbar.");
        }

        private static Material CreateOrUpdateMaterial(Shader shader)
        {
            EnsureFolder(MatFolder);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            else
            {
                mat.shader = shader;
            }

            // Vollmond-Look (die Shader-Defaults sind dieselben – hier explizit für Klarheit/Reproduzierbarkeit).
            mat.SetColor("_TopColor", new Color(0.008f, 0.015f, 0.045f, 1f));
            mat.SetColor("_BottomColor", new Color(0.03f, 0.05f, 0.10f, 1f));
            mat.SetFloat("_StarDensity", 0.55f);
            mat.SetFloat("_StarBrightness", 1.8f);
            mat.SetFloat("_StarGlow", 0.5f);
            mat.SetFloat("_StarSharp", 9f);
            mat.SetFloat("_Twinkle", 0.6f);
            mat.SetVector("_MoonDir", new Vector4(MoonDir.x, MoonDir.y, MoonDir.z, 0f));
            mat.SetFloat("_MoonSize", 0.055f);
            mat.SetColor("_MoonColor", new Color(1f, 0.97f, 0.90f, 1f));
            mat.SetFloat("_MoonGlow", 0.5f);
            mat.SetFloat("_MilkyWay", 0.18f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Stellt das (erste) Directional Light als kühles, gedämpftes Mondlicht ein und richtet es so aus,
        // dass das Licht aus der Mond-Richtung kommt.
        private static bool SetupMoonLight()
        {
            var light = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (light == null)
            {
                return false;
            }

            Undo.RecordObject(light, "Mondlicht einrichten");
            Undo.RecordObject(light.transform, "Mondlicht ausrichten");

            light.color = new Color(0.55f, 0.65f, 0.95f, 1f);
            light.intensity = 0.45f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            // Lichtrichtung (forward) = aus der Mondrichtung Richtung Szene.
            light.transform.rotation = Quaternion.LookRotation(-MoonDir);

            EditorUtility.SetDirty(light);
            return true;
        }

        // Setzt den Hintergrund der Hauptkamera auf Skybox (URP „Background Type = Skybox"), sonst zeigt
        // die Spielkamera nur eine Solid-Color statt des Himmels (Scene-View zeigt die Skybox immer).
        private static bool SetupCameraSkybox()
        {
            var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var cam = cams.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? cams.FirstOrDefault();
            if (cam == null)
            {
                return false;
            }

            Undo.RecordObject(cam, "Kamera-Hintergrund auf Skybox");
            cam.clearFlags = CameraClearFlags.Skybox;
            EditorUtility.SetDirty(cam);
            return true;
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
