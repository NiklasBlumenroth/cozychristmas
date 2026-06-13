using UnityEngine;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Entwickler-/Debug-Hilfe (kein Gameplay-UI im Sinne der Constitution V): zeigt die aktuelle
    /// Bildrate unten links als IMGUI-Overlay, darüber zu Testzwecken die Anzahl der Buch-Objekte in
    /// der Szene (gezählt über die <see cref="Sortable"/>-Komponente, die jedes Buch trägt – so sieht
    /// man, wie viele per „R" gespawnt wurden). Der FPS-Wert wird geglättet (gleitender Mittelwert über
    /// <see cref="smoothing"/> Sekunden), die Buchzahl im Intervall <see cref="countRefreshInterval"/>
    /// aktualisiert. Taste „F3" blendet die Anzeige ein/aus. Analog zu <see cref="DevSpawnMenu"/>
    /// bewusst per Code gezeichnet.
    /// </summary>
    public sealed class FpsDisplay : MonoBehaviour
    {
        [Tooltip("Glättungsfenster in Sekunden (größer = ruhiger, träger).")]
        [SerializeField] private float smoothing = 0.5f;
        [Tooltip("Schriftgröße der Anzeige.")]
        [SerializeField] private int fontSize = 18;
        [Tooltip("Abstand zur unteren linken Bildschirmecke (Pixel).")]
        [SerializeField] private float margin = 10f;
        [Tooltip("Beim Start sichtbar?")]
        [SerializeField] private bool visibleOnStart = true;

        [Header("Buch-Zähler (Testanzeige über FPS)")]
        [Tooltip("Anzahl der Buch-Objekte über der FPS-Zeile anzeigen.")]
        [SerializeField] private bool showBookCount = true;
        [Tooltip("Wie oft die Buchzahl neu gezählt wird (Sekunden).")]
        [SerializeField] private float countRefreshInterval = 0.25f;

        private bool _visible;
        private float _smoothedFps;
        private int _bookCount;
        private float _countTimer;
        private GUIStyle _style;

        private void Awake()
        {
            _visible = visibleOnStart;
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }

            // Gleitender Mittelwert: dt der letzten Frames exponentiell glätten.
            var dt = UnityEngine.Time.unscaledDeltaTime;
            if (dt <= 0f)
            {
                return;
            }

            // Buchzahl gedrosselt neu zählen (FindObjectsByType ist nicht gratis).
            if (showBookCount)
            {
                _countTimer -= dt;
                if (_countTimer <= 0f)
                {
                    _bookCount = FindObjectsByType<CozySanta.Runtime.Sorting.Sortable>(FindObjectsSortMode.None).Length;
                    _countTimer = Mathf.Max(0.05f, countRefreshInterval);
                }
            }

            var instant = 1f / dt;
            if (_smoothedFps <= 0f)
            {
                _smoothedFps = instant;
            }
            else
            {
                var t = smoothing > 0f ? Mathf.Clamp01(dt / smoothing) : 1f;
                _smoothedFps = Mathf.Lerp(_smoothedFps, instant, t);
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };
            }

            var fps = Mathf.RoundToInt(_smoothedFps);
            var ms = _smoothedFps > 0f ? 1000f / _smoothedFps : 0f;

            // Zeile 0 = unten (FPS), Zeile 1 = darüber (Buch-Zähler, optional).
            DrawLine(0, string.Format("{0} FPS ({1:0.0} ms)", fps, ms), ColorFor(fps));
            if (showBookCount)
            {
                DrawLine(1, $"Bücher: {_bookCount}", Color.white);
            }
        }

        // Zeichnet eine Textzeile unten links, lineFromBottom Zeilen über dem Bildschirmrand.
        private void DrawLine(int lineFromBottom, string text, Color color)
        {
            var height = fontSize + 8f;
            var y = Screen.height - (height * (lineFromBottom + 1)) - margin;
            var rect = new Rect(margin, y, 260f, height);

            // Schatten für Lesbarkeit auf hellem Hintergrund, dann der eigentliche Text.
            var prev = _style.normal.textColor;
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _style);
            _style.normal.textColor = color;
            GUI.Label(rect, text, _style);
            _style.normal.textColor = prev;
        }

        // Grün ab 50, Gelb ab 30, sonst Rot – schnelle Sichtkontrolle der Performance.
        private static Color ColorFor(int fps)
        {
            if (fps >= 50) return new Color(0.45f, 1f, 0.45f);
            if (fps >= 30) return new Color(1f, 0.9f, 0.4f);
            return new Color(1f, 0.45f, 0.45f);
        }
    }
}
