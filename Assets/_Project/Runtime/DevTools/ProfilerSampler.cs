using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Schreibt periodisch Profiler-Kennzahlen als CSV nach <c>&lt;Projekt&gt;/ProfilerLogs/profile_&lt;zeit&gt;.csv</c>,
    /// damit sie ohne das Profiler-Fenster (Datei-basiert) auswertbar sind. Erfasst Frame-Zeit/FPS, Main-Thread,
    /// GC-Allokationen, Speicher und Render-Lasten (Draw Calls, SetPass, Batches, Tris/Verts) – jeweils
    /// Durchschnitt UND Maximum je Intervall (Spitzen = Ruckler). Eigene <see cref="ProfilerMarker"/> (z. B.
    /// <c>SettlingBody.FixedUpdate</c>) erscheinen als zusätzliche Spalten, sobald sie im Code gesetzt sind.
    ///
    /// Bedienung: Komponente auf ein Szenen-Objekt legen (z. B. ItemPersistence). Auto-Start beim Spielstart;
    /// <b>F8</b> pausiert/fortsetzt. Datei wird laufend geschrieben (überlebt auch einen harten Play-Stopp).
    /// Kein OnGUI, um die GUI-Messung nicht selbst zu verfälschen.
    /// </summary>
    public sealed class ProfilerSampler : MonoBehaviour
    {
        [Tooltip("Wie oft eine CSV-Zeile geschrieben wird (Sekunden, gemittelt über das Intervall).")]
        [SerializeField] private float intervalSeconds = 0.5f;
        [Tooltip("Beim Start sofort mitschreiben (F8 schaltet um).")]
        [SerializeField] private bool logging = true;

        /// <summary>Freitext-Tag, das in jede CSV-Zeile (Spalte „note") geschrieben wird – z. B. vom
        /// <see cref="PerfBisectTool"/> der aktive Schalter-Zustand. Selbst-dokumentierende Bisektion.</summary>
        public static string Note = "";

        private sealed class Stat
        {
            public string Label;
            public ProfilerRecorder Recorder;
            public double Scale;   // Roh-Wert × Scale = Ausgabewert (ns→ms, Bytes→KB/MB, Count→1)
            public double Sum;
            public double Max;
        }

        private readonly List<Stat> _stats = new List<Stat>();
        private StreamWriter _writer;
        private string _path;

        private float _timer;
        private int _frames;
        private double _dtSum;     // unskalierte Frame-Zeit (s) summiert
        private double _dtMax;
        private double _elapsed;
        private bool _toggleLatch;

        private void OnEnable()
        {
            // Built-in Recorder (mit .Valid-Schutz – Namen können je Unity-Version abweichen).
            Add("main_thread_ms", ProfilerCategory.Internal, "Main Thread", 1e-6);
            Add("gc_alloc_kb", ProfilerCategory.Memory, "GC Allocated In Frame", 1.0 / 1024);
            Add("used_mem_mb", ProfilerCategory.Memory, "System Used Memory", 1.0 / (1024 * 1024));
            Add("draw_calls", ProfilerCategory.Render, "Draw Calls Count", 1);
            Add("setpass_calls", ProfilerCategory.Render, "SetPass Calls Count", 1);
            Add("batches", ProfilerCategory.Render, "Batches Count", 1);
            Add("tris", ProfilerCategory.Render, "Triangles Count", 1);
            Add("verts", ProfilerCategory.Render, "Vertices Count", 1);

            // Eigene Hot-Path-Marker (im Code mit ProfilerMarker gesetzt). Weitere hier ergänzen.
            Add("SettlingBody.FixedUpdate_ms", ProfilerCategory.Scripts, "SettlingBody.FixedUpdate", 1e-6);

            OpenFile();
        }

        private void Add(string label, ProfilerCategory category, string statName, double scale)
        {
            var rec = ProfilerRecorder.StartNew(category, statName);
            _stats.Add(new Stat { Label = label, Recorder = rec, Scale = scale });
        }

        private void OpenFile()
        {
            _path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProfilerLogs"));
            Directory.CreateDirectory(_path);
            var file = Path.Combine(_path, $"profile_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

            _writer = new StreamWriter(file, append: false) { AutoFlush = true };

            var header = new StringBuilder("t_s,fps,ms_avg,ms_max");
            foreach (var s in _stats)
            {
                header.Append(',').Append(s.Label).Append("_avg");
                header.Append(',').Append(s.Label).Append("_max");
            }

            header.Append(",note");
            _writer.WriteLine(header.ToString());

            var invalid = _stats.FindAll(s => !s.Recorder.Valid).ConvertAll(s => s.Label);
            Debug.Log($"[ProfilerSampler] Schreibe -> {file}\n" +
                      $"Gültige Recorder: {_stats.Count - invalid.Count}/{_stats.Count}." +
                      (invalid.Count > 0 ? $" Nicht verfügbar (Spalten bleiben 0): {string.Join(", ", invalid)}." : "") +
                      " F8 = Pause/Weiter.");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame && !_toggleLatch)
            {
                logging = !logging;
                Debug.Log($"[ProfilerSampler] Logging {(logging ? "AN" : "PAUSE")}.");
            }
            _toggleLatch = Keyboard.current != null && Keyboard.current.f8Key.isPressed;

            if (!logging || _writer == null) return;

            var dt = UnityEngine.Time.unscaledDeltaTime;
            _frames++;
            _dtSum += dt;
            if (dt > _dtMax) _dtMax = dt;

            foreach (var s in _stats)
            {
                if (!s.Recorder.Valid) continue;
                double v = s.Recorder.LastValue;
                s.Sum += v;
                if (v > s.Max) s.Max = v;
            }

            _timer += dt;
            if (_timer >= intervalSeconds && _frames > 0)
            {
                WriteRow();
                ResetWindow();
            }
        }

        private void WriteRow()
        {
            var ci = CultureInfo.InvariantCulture;
            _elapsed += _dtSum;

            var fps = _dtSum > 0 ? _frames / _dtSum : 0;
            var msAvg = _frames > 0 ? (_dtSum / _frames) * 1000.0 : 0;
            var msMax = _dtMax * 1000.0;

            var row = new StringBuilder();
            row.Append(_elapsed.ToString("0.00", ci)).Append(',');
            row.Append(fps.ToString("0.0", ci)).Append(',');
            row.Append(msAvg.ToString("0.00", ci)).Append(',');
            row.Append(msMax.ToString("0.00", ci));

            foreach (var s in _stats)
            {
                var avg = (s.Sum / _frames) * s.Scale;
                var max = s.Max * s.Scale;
                row.Append(',').Append(avg.ToString("0.###", ci));
                row.Append(',').Append(max.ToString("0.###", ci));
            }

            row.Append(',').Append((Note ?? string.Empty).Replace(',', ';'));
            _writer.WriteLine(row.ToString());
        }

        private void ResetWindow()
        {
            _timer = 0;
            _frames = 0;
            _dtSum = 0;
            _dtMax = 0;
            foreach (var s in _stats) { s.Sum = 0; s.Max = 0; }
        }

        private void OnDisable()
        {
            foreach (var s in _stats)
            {
                if (s.Recorder.Valid) s.Recorder.Dispose();
            }
            _stats.Clear();

            if (_writer != null)
            {
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
                Debug.Log($"[ProfilerSampler] CSV geschlossen ({_path}).");
            }
        }
    }
}
