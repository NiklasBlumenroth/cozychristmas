using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Zerlegt ein Sammel-Modell (FBX/DAE/OBJ/BLEND mit mehreren Kind-Objekten) in einzelne Prefabs –
    /// ein Prefab je direktem Kind des Modell-Wurzelknotens. Auswahl im Project-Fenster (ein oder mehrere
    /// Modelle), Ausgabe in einen Ordner „&lt;Name&gt;_Parts" neben dem Modell. Die Prefabs referenzieren die
    /// Meshes des Modells (keine Mesh-Kopien). Teile ohne Renderer (Locator/Gruppen) werden übersprungen.
    /// Editor-only; läuft im offenen Editor (Unity löst die Sub-Meshes auf).
    /// </summary>
    public static class ModelSplitter
    {
        [MenuItem("CozySanta/Modelle/Sammel-Modell in einzelne Prefabs zerlegen")]
        public static void Split()
        {
            var models = 0;
            var parts  = 0;
            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".fbx" && ext != ".dae" && ext != ".obj" && ext != ".blend") continue;

                models++;
                parts += SplitModel(path);
            }

            AssetDatabase.SaveAssets();
            if (models == 0)
                Debug.LogWarning("[ModelSplitter] Kein Modell (.fbx/.dae/.obj/.blend) im Project-Fenster ausgewählt.");
            else
                Debug.Log($"[ModelSplitter] {parts} Teil-Prefab(s) aus {models} Modell(en) erzeugt.");
        }

        private static int SplitModel(string modelPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"[ModelSplitter] Konnte Modell nicht laden: {modelPath}");
                return 0;
            }

            var dir       = Path.GetDirectoryName(modelPath).Replace('\\', '/');
            var baseName  = Path.GetFileNameWithoutExtension(modelPath);
            var outFolder = $"{dir}/{baseName}_Parts";
            EnsureFolder(outFolder);

            // Plain-Instanz (keine Prefab-Verbindung): Kinder referenzieren die FBX-Meshes.
            var instance = (GameObject)Object.Instantiate(model);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            // Direkte Kinder = Teile. Snapshot, da das Loslösen die Hierarchie verändert.
            var children = new List<Transform>();
            foreach (Transform c in instance.transform) children.Add(c);

            var count = 0;
            if (children.Count == 0)
            {
                // Modell ist ein einzelnes Mesh ohne Kinder → als ein Prefab.
                if (instance.GetComponentInChildren<Renderer>() != null && Save(instance, outFolder, baseName)) count++;
                Object.DestroyImmediate(instance);
                return count;
            }

            foreach (var child in children)
            {
                if (child.GetComponentInChildren<Renderer>() == null) continue; // Locator/leere Gruppe
                child.SetParent(null, worldPositionStays: false);               // lokale Pose des Teils beibehalten
                if (Save(child.gameObject, outFolder, child.name)) count++;
            }

            Object.DestroyImmediate(instance);
            return count;
        }

        private static bool Save(GameObject go, string folder, string name)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Sanitize(name)}.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out var ok);
            if (!ok) Debug.LogWarning($"[ModelSplitter] Speichern fehlgeschlagen: {path}");
            return ok && prefab != null;
        }

        private static string Sanitize(string n)
        {
            if (string.IsNullOrEmpty(n)) return "Part";
            foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return n;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf   = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
