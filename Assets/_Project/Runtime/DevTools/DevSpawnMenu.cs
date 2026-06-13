using CozySanta.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Entwickler-/Debug-Hilfe (kein Gameplay-UI im Sinne der Constitution V): ein IMGUI-Overlay
    /// zur Auswahl eines Spawn-Objekts. Taste „G" öffnet/schließt die Liste, ein Klick wählt das
    /// aktive Objekt, Taste „R" spawnt das aktuell selektierte Prefab vor dem Spieler. Ab 6 Einträgen
    /// wird die Liste scrollbar. Ersetzt den festen „O"-Spawner aus F3.
    /// </summary>
    public sealed class DevSpawnMenu : MonoBehaviour
    {
        [Tooltip("Auswählbare Spawn-Prefabs (per Name gelistet).")]
        [SerializeField] private GameObject[] prefabs = new GameObject[0];
        [Tooltip("Ursprung für den Spawn (z. B. die Kamera). Fallback: dieses Transform.")]
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private float distance = 1.5f;
        [SerializeField] private float heightOffset = 0.5f;

        [Tooltip("Sichtbare Zeilen, bevor die Liste scrollt.")]
        [SerializeField] private int visibleRows = 5;

        [Tooltip("Wartezeit, bevor die R-Taste gehalten wiederholt spawnt (Sekunden).")]
        [SerializeField] private float holdInitialDelay = 0.4f;
        [Tooltip("Abstand zwischen den Spawns beim Halten der R-Taste (Sekunden).")]
        [SerializeField] private float holdRepeatInterval = 0.18f;

        private bool _open;
        private int _selected;
        private Vector2 _scroll;
        private HoldRepeatTimer _spawnRepeat;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                SetOpen(!_open);
            }

            // „R" gedrückt halten = wiederholt spawnen (zügig mehrere Test-Objekte setzen).
            if (_spawnRepeat.Tick(keyboard.rKey.isPressed, UnityEngine.Time.deltaTime, holdInitialDelay, holdRepeatInterval))
            {
                Spawn();
            }
        }

        private void SetOpen(bool open)
        {
            _open = open;
            // Cursor freigeben, damit die Liste anklickbar ist; beim Schließen wieder sperren.
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        private void Spawn()
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            _selected = Mathf.Clamp(_selected, 0, prefabs.Length - 1);
            var prefab = prefabs[_selected];
            if (prefab == null)
            {
                return;
            }

            var origin = spawnOrigin != null ? spawnOrigin : transform;
            var position = origin.position + (origin.forward * distance) + (Vector3.up * heightOffset);
            Instantiate(prefab, position, Quaternion.identity);
        }

        private void OnGUI()
        {
            if (!_open || prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            const float width = 260f;
            const float rowHeight = 26f;
            var rows = Mathf.Max(1, Mathf.Min(prefabs.Length, Mathf.Max(1, visibleRows)));
            var listHeight = rows * rowHeight;

            var area = new Rect(12f, 12f, width, listHeight + 78f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("Dev-Spawn  (G schließen · R spawnen)");
            GUILayout.Label("Aktiv: " + NameAt(_selected));

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(listHeight));
            for (var i = 0; i < prefabs.Length; i++)
            {
                var isSelected = i == _selected;
                var previous = GUI.color;
                GUI.color = isSelected ? Color.green : previous;
                var label = (isSelected ? "> " : "   ") + NameAt(i);
                if (GUILayout.Button(label, GUILayout.Height(rowHeight - 4f)))
                {
                    _selected = i;
                }

                GUI.color = previous;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private string NameAt(int index)
        {
            if (prefabs == null || index < 0 || index >= prefabs.Length || prefabs[index] == null)
            {
                return "<leer>";
            }

            return prefabs[index].name;
        }
    }
}
