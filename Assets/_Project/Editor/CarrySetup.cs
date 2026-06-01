using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für F3: ergänzt <see cref="PlayerCarry"/> am Player, legt Links/Rechts-Anker an
    /// (links unter der Kamera, rechts unter dem Spielerkörper), erzeugt ein aufnehmbares
    /// TestPickup-Prefab (mit Schwerkraft) und einen Spawner (Taste „O"). Läuft im offenen Editor.
    /// </summary>
    public static class CarrySetup
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string PrefabPath = "Assets/_Project/Prefabs/TestPickup.prefab";

        [MenuItem("CozySanta/Setup F3 (Carry + Test Pickup)")]
        public static void Setup()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[F3Setup] Kein 'Player' gefunden. Bitte zuerst 'CozySanta/Setup Greybox Player' ausführen.");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[F3Setup] Keine Main Camera (als Player-Child) gefunden.");
                return;
            }

            var carry = EnsureComponent<PlayerCarry>(player);

            var left = GetOrCreate("LeftHandAnchor");
            left.transform.SetParent(cam.transform, false);
            left.transform.localPosition = new Vector3(-0.35f, -0.3f, 0.7f); // links unten im Blickfeld
            left.transform.localRotation = Quaternion.identity;

            var right = GetOrCreate("RightHandAnchor");
            right.transform.SetParent(player.transform, false); // unter Player-Body -> feste Höhe
            right.transform.localPosition = new Vector3(0.4f, 0.1f, 0.7f); // rechts, Hüfthöhe als Stapelbasis (gut sichtbar beim Blick nach unten)
            right.transform.localRotation = Quaternion.identity;

            SetRef(carry, "leftHandAnchor", left.transform);
            SetRef(carry, "rightHandAnchor", right.transform);

            var interaction = player.GetComponent<PlayerInteractionController>();
            if (interaction != null)
            {
                SetRef(interaction, "carry", carry);
            }

            var prefab = CreateTestPickupPrefab();

            var spawner = EnsureComponent<TestPickupSpawner>(player);
            SetRef(spawner, "prefab", prefab);
            SetRef(spawner, "spawnOrigin", cam.transform);

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F3Setup] Carry + Anker + TestPickup-Prefab + Spawner eingerichtet. Szene speichern (Strg+S), dann Play. 'O' spawnt, 'E' aufnehmen, 'Q' ablegen.");
        }

        private static GameObject CreateTestPickupPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.name = "TestPickup";
            temp.transform.localScale = Vector3.one * 0.4f;
            var body = temp.AddComponent<Rigidbody>();
            body.useGravity = true;
            temp.AddComponent<PickupInteractable>(); // Gewicht-Default 0.3 kg aus dem Feld-Initializer

            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            Object.DestroyImmediate(temp);
            Debug.Log($"[F3Setup] Prefab erzeugt: {PrefabPath}");
            return prefab;
        }

        private static GameObject GetOrCreate(string name)
        {
            var existing = GameObject.Find(name);
            return existing != null ? existing : new GameObject(name);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetRef(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[F3Setup] Feld '{fieldName}' an {target.GetType().Name} nicht gefunden.");
            }
        }
    }
}
