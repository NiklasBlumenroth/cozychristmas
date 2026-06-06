using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für den Außen-Untergrund: erzeugt aus der abgelegten Gras-Textur eine
    /// <see cref="TerrainLayer"/>, ein flaches <see cref="TerrainData"/> und ein Terrain-GameObject
    /// (zentriert auf den Ursprung). Die alte Boden-Plane wird deaktiviert (reversibel). Läuft im
    /// offenen Editor über das Menü „CozySanta" – kein Hand-Editieren der Szene.
    ///
    /// Hinweis: Schnee-Schmelz-Zonen (F5 <c>SnowPatch</c>/<c>MeltController</c>) bleiben eigene Planes
    /// leicht über dem Terrain, damit das Vertex-Displacement/Raycast-Verhalten unverändert bleibt.
    /// </summary>
    public static class TerrainSetup
    {
        // Quelle: die vom Nutzer unter Materials abgelegte Gras-Textur.
        private const string GrassTexturePath = "Assets/_Project/Materials/Vegetation (47).png";
        private const string TerrainLayerPath = "Assets/_Project/Materials/TL_Gras.terrainlayer";
        private const string TerrainFolder = "Assets/_Project/Terrain";
        private const string TerrainDataPath = "Assets/_Project/Terrain/TD_Wiese.asset";

        private const float SizeMeters = 100f;     // Kantenlänge der Wiese (100 × 100 m)
        private const float MaxHeight = 50f;        // erlaubte Höhe (flach genutzt, Reserve für später)
        private const int HeightmapResolution = 129; // muss 2^n+1 sein; flach -> niedrige Auflösung reicht
        private const float GrasTileSizeMeters = 4f;  // Kachelung der Gras-Textur in Metern

        [MenuItem("CozySanta/Setup Terrain (Wiese)")]
        public static void Setup()
        {
            // 1) Gras-Textur laden
            var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            if (grass == null)
            {
                Debug.LogError($"[TerrainSetup] Gras-Textur nicht gefunden: {GrassTexturePath}");
                return;
            }

            // 2) TerrainLayer erzeugen/aktualisieren (Terrains nutzen Layer statt normaler Materials)
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, TerrainLayerPath);
            }
            layer.diffuseTexture = grass;
            layer.tileSize = new Vector2(GrasTileSizeMeters, GrasTileSizeMeters);
            layer.tileOffset = Vector2.zero;
            EditorUtility.SetDirty(layer);

            // 3) Flaches TerrainData erzeugen/aktualisieren
            EnsureFolder(TerrainFolder);
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            data.heightmapResolution = HeightmapResolution; // setzt Höhen implizit auf 0 (flach)
            data.size = new Vector3(SizeMeters, MaxHeight, SizeMeters);
            data.SetHeights(0, 0, new float[HeightmapResolution, HeightmapResolution]); // explizit flach
            data.terrainLayers = new[] { layer };
            EditorUtility.SetDirty(data);

            // 4) Terrain-GameObject erstellen/finden (inkl. TerrainCollider) und zentrieren
            var terrainGo = GameObject.Find("Terrain_Wiese");
            if (terrainGo == null)
            {
                terrainGo = Terrain.CreateTerrainGameObject(data);
                terrainGo.name = "Terrain_Wiese";
            }

            var terrain = terrainGo.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.terrainData = data;
            }

            var collider = terrainGo.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = data;
            }

            // Terrain-Ursprung liegt in der Ecke -> auf den Welt-Ursprung zentrieren.
            terrainGo.transform.position = new Vector3(-SizeMeters / 2f, 0f, -SizeMeters / 2f);
            terrainGo.transform.rotation = Quaternion.identity;

            // 5) Alte Boden-Plane deaktivieren (reversibel; Terrain bringt eigenen Collider mit)
            var plane = GameObject.Find("Plane");
            if (plane != null)
            {
                plane.SetActive(false);
                Debug.Log("[TerrainSetup] 'Plane' deaktiviert – Terrain ist jetzt der Boden.");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[TerrainSetup] Terrain '{terrainGo.name}' ({SizeMeters}×{SizeMeters} m, flach) erstellt " +
                      "und mit Gras-Layer belegt. Bitte Szene speichern (Strg+S).");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
