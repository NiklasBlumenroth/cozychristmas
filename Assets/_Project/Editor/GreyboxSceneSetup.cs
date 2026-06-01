using CozySanta.Runtime.Interaction;
using CozySanta.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für die manuelle Spielwiese: verdrahtet den Player-Rig (Komponenten, Kamera als
    /// Child, Referenzen, PlayerInput), ergänzt das Test-Interactable und entfernt das überflüssige
    /// Terrain. Läuft im offenen Editor über das Menü „CozySanta" – kein Hand-Editieren der Szene.
    /// </summary>
    public static class GreyboxSceneSetup
    {
        private const string ActionsAssetPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("CozySanta/Setup Greybox Player")]
        public static void Setup()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[GreyboxSetup] Kein GameObject 'Player' in der Szene gefunden.");
                return;
            }

            EnsureComponent<CharacterController>(player);
            var controller = EnsureComponent<FirstPersonController>(player);
            EnsureComponent<PlayerInteractionController>(player);
            var probe = EnsureComponent<PhysicsInteractionProbe>(player);
            EnsureComponent<PlayerInputRelay>(player);

            // Kamera als Child auf Augenhöhe
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("PlayerCamera");
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                camGo.tag = "MainCamera";
            }

            cam.transform.SetParent(player.transform, worldPositionStays: false);
            cam.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cam.transform.localRotation = Quaternion.identity;

            SetReference(controller, "cameraPivot", cam.transform);
            SetReference(probe, "view", cam.transform);

            // PlayerInput (Send Messages -> OnMove/OnLook/OnInteract im Relay)
            var input = EnsureComponent<PlayerInput>(player);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsAssetPath);
            if (actions != null)
            {
                input.actions = actions;
                input.defaultActionMap = "Player";
                input.notificationBehavior = PlayerNotifications.SendMessages;
            }
            else
            {
                Debug.LogWarning($"[GreyboxSetup] InputActionAsset nicht gefunden: {ActionsAssetPath}");
            }

            // Test-Interactable
            var test = GameObject.Find("TestObject");
            if (test != null)
            {
                EnsureComponent<DebugInteractable>(test);
            }
            else
            {
                Debug.LogWarning("[GreyboxSetup] Kein 'TestObject' gefunden – Debug-Interactable nicht ergänzt.");
            }

            // Überflüssiges Terrain entfernen (Plane ist der Boden)
            var terrain = GameObject.Find("Terrain");
            if (terrain != null)
            {
                Object.DestroyImmediate(terrain);
                Debug.Log("[GreyboxSetup] 'Terrain' aus der Szene entfernt.");
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[GreyboxSetup] Player-Rig eingerichtet. Bitte Szene speichern (Strg+S) und Play drücken.");
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetReference(Object target, string fieldName, Object value)
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
                Debug.LogWarning($"[GreyboxSetup] Feld '{fieldName}' an {target.GetType().Name} nicht gefunden.");
            }
        }
    }
}
