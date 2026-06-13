using System.Collections.Generic;
using CozySanta.Runtime.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Authoring-/Debug-Overlay (kein Gameplay-UI im Sinne der Constitution V): zeigt für den aktuell
    /// betretenen Bereich die Gesamtzahl der Items und darunter alle Varianten mit „Anzahl/Max" (z. B.
    /// die 96 Bücher der Bibliothek, je x/20), plus Buttons „Speichern" und „Reset".
    ///
    /// Bedienung: Das Panel erscheint passiv, sobald man einen Bereich betritt (Cursor bleibt gesperrt,
    /// Umschauen normal). <b>ESC</b> schaltet in den Bedien-Modus (Cursor frei/sichtbar, Maus-Blick
    /// pausiert). Ein <b>Klick außerhalb des Panels</b> (oder Verlassen des Bereichs) kehrt zurück ins
    /// Spiel (Cursor wieder gesperrt). Liste ist scrollbar; Zählung wird gedrosselt aktualisiert.
    /// </summary>
    public sealed class AreaInventoryHud : MonoBehaviour
    {
        [SerializeField] private ItemPersistence persistence;
        [Tooltip("Spieler-Transform zur Bestimmung des aktuellen Bereichs.")]
        [SerializeField] private Transform player;
        [Tooltip("Aktualisierungsintervall der Zählung (Sekunden).")]
        [SerializeField] private float refreshInterval = 0.25f;

        [Header("Darstellung")]
        [Tooltip("Schriftgröße der Liste/Buttons.")]
        [SerializeField] private int fontSize = 16;
        [Tooltip("Panel oben links statt oben rechts (oben rechts kollidiert mit dem Area-HUD).")]
        [SerializeField] private bool anchorLeft = true;
        [SerializeField] private float panelWidth = 340f;
        [SerializeField] private float panelHeight = 520f;

        private bool _interact;     // Bedien-Modus: Cursor frei, Buttons/Scroll nutzbar
        private bool _wasInteract;
        private float _timer;
        private string _status = "";
        private Vector2 _scroll;
        private ItemArea _area;
        private Dictionary<string, int> _counts = new Dictionary<string, int>();
        private int _total;
        private GUIStyle _label;
        private GUIStyle _header;
        private GUIStyle _button;

        private void Awake()
        {
            if (persistence == null) persistence = FindAnyObjectByType<ItemPersistence>();
        }

        private void Update()
        {
            _timer -= UnityEngine.Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                Refresh();
                _timer = Mathf.Max(0.05f, refreshInterval);
            }

            // ESC: Bedien-Modus an/aus. Nur sinnvoll, wenn ein Panel (Bereich) da ist.
            if (_area != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _interact = !_interact;
            }

            // Bereich verlassen -> Bedien-Modus beenden.
            if (_area == null) _interact = false;

            ApplyCursor();
        }

        // Im Bedien-Modus den Cursor jeden Frame freigeben (sonst sperrt ihn der FP-Controller wieder);
        // beim Verlassen einmalig zurück auf gesperrt/unsichtbar.
        private void ApplyCursor()
        {
            if (_interact)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_wasInteract)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            _wasInteract = _interact;
        }

        private void Refresh()
        {
            _area = CurrentArea();
            if (_area == null)
            {
                _counts.Clear();
                _total = 0;
                return;
            }

            _counts = persistence.CountByKey(_area);
            _total = 0;
            foreach (var c in _counts.Values) _total += c;
        }

        private ItemArea CurrentArea()
        {
            if (persistence == null || player == null) return null;

            ItemArea fallback = null;
            foreach (var area in persistence.FindAreas())
            {
                if (area == null || !area.Contains(player.position)) continue;
                if (area.Catalog != null) return area;       // Bereich mit Inventar bevorzugen
                fallback ??= area;                           // sonst wenigstens Namen zeigen
            }

            return fallback;
        }

        private void EnsureStyles()
        {
            if (_label != null && _label.fontSize == fontSize) return;

            _label = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
            _header = new GUIStyle(GUI.skin.label) { fontSize = fontSize + 2, fontStyle = FontStyle.Bold };
            _button = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        }

        private void OnGUI()
        {
            if (_area == null) return;

            EnsureStyles();

            var x = anchorLeft ? 12f : Screen.width - panelWidth - 12f;
            var rect = new Rect(x, 12f, panelWidth, panelHeight);

            // Klick außerhalb des Panels beendet den Bedien-Modus (zurück ins Spiel).
            var e = Event.current;
            if (_interact && e.type == EventType.MouseDown && !rect.Contains(e.mousePosition))
            {
                _interact = false;
            }

            GUILayout.BeginArea(rect, GUI.skin.box);

            var catalog = _area.Catalog;
            var max = _area.MaxPerVariant;
            var variants = catalog != null ? catalog.Keys.Count : 0;
            var hint = _interact ? "Klick außerhalb = zurück" : "ESC = bedienen";
            GUILayout.Label($"Bereich: {_area.AreaName}   ({hint})", _header);
            GUILayout.Label($"Gesamt: {_total} / {variants * max}", _header);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Speichern", _button))
            {
                var n = persistence.SaveArea(_area);
                _status = $"{n} Items gespeichert.";
            }

            if (GUILayout.Button("Reset (alle entfernen)", _button))
            {
                persistence.ClearArea(_area);
                _status = "Bereich geleert. (Speichern, um den Start-Standard zu setzen.)";
            }

            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status, _label);

            GUILayout.Space(4f);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            if (catalog != null)
            {
                foreach (var key in catalog.Keys)
                {
                    _counts.TryGetValue(key, out var c);
                    GUILayout.Label($"{key}: {c}/{max}", _label);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
