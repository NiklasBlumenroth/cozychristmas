using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Rüstet die Truhen-Instanzen (<c>chest</c>-Prefab, erkannt am Deckel-Child <c>chest_top</c>) in der
    /// offenen Szene mit <see cref="GiftChest"/> aus (Constitution V: nur Szene). Pro Truhe: Deckel
    /// verdrahten, ein Innenvolumen-Trigger („InsideVolume") anlegen, den Auswurfpunkt
    /// (<c>RöhrenPosition</c>) setzen und der Reihe nach genau eine Sorte aus dem
    /// <c>TruhengeschenkeCatalog</c> zuweisen (Soll-Menge = Katalog-Max bzw. 100). Reihenfolge der Truhen =
    /// letzte Ziffer im Namen. Die SortKeys kommen direkt aus den Prefabs (deren <see cref="Sortable"/>),
    /// sind also deckungsgleich mit den Items.
    /// </summary>
    public static class GiftChestSetup
    {
        private const string CatalogPath = GeschenkItemSetup.CatalogPath;
        private const string LidChildName = "chest_top";
        private const string EjectTargetName = "RöhrenPosition";
        private const int DefaultRequired = 100;

        [MenuItem("CozySanta/Geschenkehalle/Truhen einrichten (GiftChest + Innenvolumen + Sorte)")]
        public static void Setup()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null || catalog.Keys.Count == 0)
            {
                Debug.LogError($"[Truhen] Kein/leerer GeschenkehalleCatalog ({CatalogPath}). " +
                               "Zuerst 'Geschenke als Sortierobjekte einrichten' ausführen.");
                return;
            }

            // Nur die Truhen-Varianten (Prefabs im Truhengeschenke-Ordner) aus dem gemeinsamen Katalog.
            var variants = LoadCatalogFacets(catalog);
            if (variants.Count == 0)
            {
                Debug.LogError($"[Truhen] Keine Truhen-Varianten im Katalog (Ordner {GeschenkItemSetup.TruhenFolder}).");
                return;
            }
            var eject = FindByName(EjectTargetName);
            if (eject == null)
                Debug.LogWarning($"[Truhen] Kein '{EjectTargetName}' in der Szene gefunden – Auswurfpunkt bleibt leer.");

            // Auswahlbasiert: nur die in der Hierarchie SELEKTIERTEN Truhen (bzw. Truhen unter selektierten
            // Roots) werden eingerichtet. So bleiben Deko-Truhen anderer Hallen unberührt. Ohne Auswahl
            // werden alle Truhen der Szene genommen (mit Warnung) – nur fallbackweise.
            var fromSelection = true;
            var chests = CollectSelectedChests();
            if (chests.Count == 0)
            {
                fromSelection = false;
                chests = FindAllChests();
                Debug.LogWarning("[Truhen] Keine Truhen in der Auswahl – verwende ALLE Truhen der Szene. " +
                                 "Besser: nur die Geschenkehalle-Truhen (oder ihren Eltern-Root) selektieren, " +
                                 "damit Deko-Truhen anderer Hallen nicht erfasst werden.");
            }
            chests = chests.OrderBy(NumberOf).ThenBy(t => t.name).ToList();
            if (chests.Count == 0)
            {
                Debug.LogError("[Truhen] Keine Truhen (chest mit Child 'chest_top') gefunden.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            var log = new StringBuilder();

            // 1:1-Zuweisung (KEIN Modulo): jede Sorte genau einmal. Überzählige Truhen/Varianten bleiben offen.
            var pairs = Mathf.Min(chests.Count, variants.Count);
            for (var i = 0; i < pairs; i++)
            {
                var facets = variants[i];
                var max = MaxFor(catalog, facets);
                ConfigureChest(chests[i], facets, max, eject);
                log.AppendLine($"  {chests[i].name} -> [{string.Join(", ", facets)}] (Soll {max})");
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(chests[0].gameObject.scene);

            if (chests.Count != variants.Count)
            {
                Debug.LogWarning($"[Truhen] {chests.Count} Truhen, aber {variants.Count} Truhen-Varianten – " +
                                 $"{pairs} 1:1 zugewiesen. " +
                                 (chests.Count > variants.Count
                                     ? $"{chests.Count - pairs} Truhe(n) bleiben OHNE Sorte (kein GiftChest). " +
                                       "Truhenzahl auf die Variantenzahl bringen oder nur passende selektieren."
                                     : $"{variants.Count - pairs} Variante(n) haben KEINE Truhe."));
            }

            Debug.Log($"[Truhen] {pairs} Truhe(n) eingerichtet (Quelle: {(fromSelection ? "Auswahl" : "ganze Szene")}). " +
                      "Szene speichern (Strg+S). Danach am Geschenkehalle-AreaTracker eine Truhen-Gruppe " +
                      $"(root = gemeinsamer Eltern, taskId) hinzufügen.\n{log}");
        }

        private static void ConfigureChest(Transform chest, string[] facets, int required, Transform eject)
        {
            var giftChest = chest.GetComponent<GiftChest>();
            if (giftChest == null) giftChest = Undo.AddComponent<GiftChest>(chest.gameObject);

            var lid = FindChild(chest, LidChildName);
            if (lid == null)
                Debug.LogWarning($"[Truhen] '{chest.name}': kein '{LidChildName}' gefunden – Deckel nicht verdrahtet.");

            var inside = EnsureInsideVolume(chest);

            var so = new SerializedObject(giftChest);
            SetStringArray(so, "acceptedFacets", facets);
            so.FindProperty("required").intValue = required;
            if (lid != null) so.FindProperty("lid").objectReferenceValue = lid;
            if (inside != null) so.FindProperty("insideVolume").objectReferenceValue = inside;
            if (eject != null) so.FindProperty("ejectTarget").objectReferenceValue = eject;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(giftChest);
        }

        // Legt (oder findet) ein Trigger-BoxCollider-Child „InsideVolume", grob an den Innenraum gefittet.
        private static BoxCollider EnsureInsideVolume(Transform chest)
        {
            var existing = FindChild(chest, "InsideVolume");
            BoxCollider box;
            if (existing != null)
            {
                box = existing.GetComponent<BoxCollider>();
                if (box == null) box = Undo.AddComponent<BoxCollider>(existing.gameObject);
            }
            else
            {
                var go = new GameObject("InsideVolume");
                Undo.RegisterCreatedObjectUndo(go, "InsideVolume");
                go.transform.SetParent(chest, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                box = go.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;

            // Startwerte aus den Body-Bounds (Root-MeshFilter) im lokalen Raum – innen ~60 %, leicht angehoben.
            var mf = chest.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var b = mf.sharedMesh.bounds;
                box.center = new Vector3(b.center.x, b.center.y + b.extents.y * 0.25f, b.center.z);
                box.size = new Vector3(b.size.x * 0.7f, b.size.y * 0.6f, b.size.z * 0.7f);
            }

            return box;
        }

        private static int MaxFor(ItemCatalog catalog, string[] facets)
        {
            var map = catalog.MaxByKey();
            var key = facets != null && facets.Length > 0 ? facets[0] : null;
            return key != null && map.TryGetValue(key, out var m) && m > 0 ? m : DefaultRequired;
        }

        // Liest aus den TRUHEN-Varianten (Prefabs im Truhengeschenke-Ordner) die Sortable-Facetten,
        // in Katalog-Reihenfolge. Toys (Regal-Geschenke) werden übersprungen.
        private static List<string[]> LoadCatalogFacets(ItemCatalog catalog)
        {
            var truhenFolder = GeschenkItemSetup.TruhenFolder + "/";
            var result = new List<string[]>();
            foreach (var key in catalog.Keys)
            {
                var prefab = catalog.Get(key);
                if (prefab == null) continue;

                var path = AssetDatabase.GetAssetPath(prefab).Replace('\\', '/');
                if (!path.StartsWith(truhenFolder)) continue; // nur Truhen-Geschenke

                var sortable = prefab.GetComponent<Sortable>();
                if (sortable == null)
                {
                    Debug.LogWarning($"[Truhen] '{key}': kein Sortable am Prefab – übersprungen.");
                    continue;
                }

                var so = new SerializedObject(sortable);
                var prop = so.FindProperty("facets");
                if (prop == null || prop.arraySize == 0) continue;

                var facets = new string[prop.arraySize];
                for (var i = 0; i < prop.arraySize; i++)
                    facets[i] = prop.GetArrayElementAtIndex(i).stringValue;
                result.Add(facets);
            }

            return result;
        }

        // Truhen aus der Hierarchie-Auswahl: jede selektierte Truhe selbst + alle Truhen unter selektierten Roots.
        private static List<Transform> CollectSelectedChests()
        {
            var result = new List<Transform>();
            foreach (var root in Selection.transforms)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (FindChild(t, LidChildName) != null && !result.Contains(t)) result.Add(t);
                }
            }

            return result;
        }

        // Alle Truhen der Szene = Transforms mit einem direkten Child namens „chest_top" (Fallback).
        private static List<Transform> FindAllChests()
        {
            var result = new List<Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (FindChild(t, LidChildName) != null) result.Add(t);
            }

            return result;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            }

            return null;
        }

        private static Transform FindByName(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
        }

        private static void SetStringArray(SerializedObject so, string name, string[] values)
        {
            var prop = so.FindProperty(name);
            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static int NumberOf(Transform t)
        {
            var matches = Regex.Matches(t.name, @"\d+");
            return matches.Count > 0 ? int.Parse(matches[matches.Count - 1].Value) : 0;
        }
    }
}
