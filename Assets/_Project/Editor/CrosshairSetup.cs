using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup: baut ein zentriertes Fadenkreuz (editor-authored UI) in den vorhandenen Canvas.
    /// Markiert die Bildschirmmitte und damit die Kamera-/Raycast-Richtung (Interaktions-Probe und
    /// Melt-Raycast zielen entlang der Blickrichtung). Vier dünne Arme mit Mittellücke + Mittelpunkt;
    /// rein statisch, daher kein Laufzeit-Skript nötig.
    /// </summary>
    public static class CrosshairSetup
    {
        private const string RootName = "Crosshair";

        [MenuItem("CozySanta/Setup Fadenkreuz")]
        public static void Setup()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[FadenkreuzSetup] Kein Canvas in der Szene gefunden.");
                return;
            }

            var existing = canvas.transform.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // Look: weiße, leicht transparente Arme.
            var color = new Color(1f, 1f, 1f, 0.85f);
            const float thickness = 2f;   // Armdicke
            const float length    = 8f;   // Armlänge
            const float gap       = 5f;   // Lücke vom Zentrum bis zum Armanfang
            const float dot       = 2f;   // Mittelpunkt-Größe

            var root = NewUI(RootName, canvas.transform);
            CenterAnchor(root, Vector2.zero);
            root.transform.SetAsLastSibling(); // über den HUD-Panels

            var offset = gap + (length * 0.5f);
            MakeArm(root.transform, "Up",    new Vector2(0f,  offset), new Vector2(thickness, length), color);
            MakeArm(root.transform, "Down",  new Vector2(0f, -offset), new Vector2(thickness, length), color);
            MakeArm(root.transform, "Left",  new Vector2(-offset, 0f), new Vector2(length, thickness), color);
            MakeArm(root.transform, "Right", new Vector2( offset, 0f), new Vector2(length, thickness), color);
            MakeArm(root.transform, "Dot",   Vector2.zero,             new Vector2(dot, dot),          color);

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[FadenkreuzSetup] Fadenkreuz im Canvas erstellt. Szene speichern (Strg+S).");
        }

        private static void MakeArm(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = NewUI(name, parent);
            CenterAnchor(go, anchoredPos);
            go.GetComponent<RectTransform>().sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false; // darf keine Klicks/Pointer abfangen
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        private static void CenterAnchor(GameObject go, Vector2 anchoredPos)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
        }
    }
}
