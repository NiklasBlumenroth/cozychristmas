using CozySanta.Runtime.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Entwickler-Menü (kein Gameplay-UI im Sinne der Constitution V) zum Authoren der Item-Haufen:
    /// Taste „F4" öffnet/schließt eine IMGUI-Liste aller Bereiche (<see cref="ItemArea"/>). Pro Bereich
    /// gibt es „Speichern" (schreibt die aktuelle Pose aller aufnehmbaren Items des Bereichs nach
    /// StreamingAssets) und „Laden". Typischer Ablauf: im Play-Mode Bücher spawnen/verteilen, ruhen
    /// lassen, Bereich speichern – beim nächsten Start lädt <see cref="ItemPersistence"/> den Stand.
    /// </summary>
    public sealed class ItemSaveDevTool : MonoBehaviour
    {
        [SerializeField] private ItemPersistence persistence;

        private bool _open;
        private string _status = "";

        private void Awake()
        {
            if (persistence == null) persistence = FindAnyObjectByType<ItemPersistence>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f4Key.wasPressedThisFrame)
            {
                SetOpen(!_open);
            }
        }

        private void SetOpen(bool open)
        {
            _open = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        private void OnGUI()
        {
            if (!_open) return;

            const float width = 320f;
            var area = new Rect(12f, 12f, width, 320f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("Item-Bereiche  (F4 schließen)");

            if (persistence == null)
            {
                GUILayout.Label("Kein ItemPersistence in der Szene.");
                GUILayout.EndArea();
                return;
            }

            var areas = persistence.FindAreas();
            if (areas.Length == 0)
            {
                GUILayout.Label("Keine ItemArea gefunden.");
            }

            foreach (var itemArea in areas)
            {
                if (itemArea == null) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(itemArea.AreaName, GUILayout.Width(150f));
                if (GUILayout.Button("Speichern"))
                {
                    var n = persistence.SaveArea(itemArea);
                    _status = $"{itemArea.AreaName}: {n} Items gespeichert.";
                }

                if (GUILayout.Button("Laden"))
                {
                    var n = persistence.LoadArea(itemArea);
                    _status = $"{itemArea.AreaName}: {n} Items geladen.";
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);

            GUILayout.EndArea();
        }
    }
}
