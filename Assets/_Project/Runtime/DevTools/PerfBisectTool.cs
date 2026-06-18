using System.Collections.Generic;
using CozySanta.Runtime.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Bisektions-Helfer für die Performance-Untersuchung: schaltet zur Laufzeit einzelne Render-Verdächtige
    /// an/aus, damit der <see cref="ProfilerSampler"/> den FPS-/Tris-/Draw-Delta misst. Der aktuelle
    /// Schalter-Zustand wird in <see cref="ProfilerSampler.Note"/> geschrieben → erscheint als CSV-Spalte
    /// <c>note</c>, sodass jede Messzeile selbst-dokumentiert ist.
    ///
    /// Tasten (jeweils umschalten):
    ///   T = alle Terrains · H = Haupt-Schatten (Cascades) · G = Terrain-Schattenwurf ·
    ///   L = Extra-Lichter (alle außer Haupt-Directional) · B = Bücher/Items-Renderer.
    /// Kein OnGUI (würde die GUI-/GC-Messung verfälschen) – Zustand steht in der Console + CSV.
    /// </summary>
    public sealed class PerfBisectTool : MonoBehaviour
    {
        private Terrain[] _terrains;
        private Light _mainLight;
        private readonly List<Light> _extraLights = new List<Light>();
        private readonly Dictionary<Light, LightShadows> _origLightShadow = new Dictionary<Light, LightShadows>();
        private readonly Dictionary<Terrain, ShadowCastingMode> _origTerrainShadow = new Dictionary<Terrain, ShadowCastingMode>();

        private readonly List<Behaviour> _devTools = new List<Behaviour>();

        private bool _terrainsOn = true;
        private bool _mainShadowsOn = true;
        private bool _terrainShadowsOn = true;
        private bool _extraLightsOn = true;
        private bool _booksOn = true;
        private bool _hudsOn = true;

        private void Start()
        {
            _terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in _terrains) _origTerrainShadow[t] = t.shadowCastingMode;

            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _origLightShadow[l] = l.shadows;
                if (l.type == LightType.Directional && _mainLight == null) _mainLight = l;
                else _extraLights.Add(l);
            }

            // Alle aktiven IMGUI-Dev-Tools einsammeln (Namespace CozySanta.Runtime.DevTools), außer den
            // Mess-Tools selbst – damit U deren OnGUI-/GC-Last komplett abschalten kann.
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is PerfBisectTool || mb is ProfilerSampler || !mb.enabled) continue;
                if (mb.GetType().Namespace == "CozySanta.Runtime.DevTools") _devTools.Add(mb);
            }

            ApplyRender();
            Debug.Log($"[Bisect] bereit. {_terrains.Length} Terrains, {_extraLights.Count} Extra-Lichter, " +
                      $"{_devTools.Count} Dev-Tools, Haupt-Licht {(_mainLight != null ? "gefunden" : "FEHLT")}. " +
                      $"Tasten: T/H/G/L/B/U. {StateString()}");
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;

            if (k.tKey.wasPressedThisFrame) { _terrainsOn = !_terrainsOn; ApplyRender(); Logged("T"); }
            if (k.hKey.wasPressedThisFrame) { _mainShadowsOn = !_mainShadowsOn; ApplyRender(); Logged("H"); }
            if (k.gKey.wasPressedThisFrame) { _terrainShadowsOn = !_terrainShadowsOn; ApplyRender(); Logged("G"); }
            if (k.lKey.wasPressedThisFrame) { _extraLightsOn = !_extraLightsOn; ApplyRender(); Logged("L"); }
            if (k.bKey.wasPressedThisFrame) { _booksOn = !_booksOn; ToggleBooks(); Logged("B"); }
            if (k.uKey.wasPressedThisFrame) { _hudsOn = !_hudsOn; ToggleHuds(); Logged("U"); }
        }

        private void ToggleHuds()
        {
            foreach (var mb in _devTools) if (mb != null) mb.enabled = _hudsOn;
            UpdateNote();
        }

        private void ApplyRender()
        {
            foreach (var t in _terrains)
            {
                if (t == null) continue;
                t.drawHeightmap = _terrainsOn;
                t.drawTreesAndFoliage = _terrainsOn;
                // Terrain wirft nur, wenn es überhaupt gezeichnet wird UND Terrain-Schatten an sind.
                t.shadowCastingMode = (_terrainsOn && _terrainShadowsOn) ? _origTerrainShadow[t] : ShadowCastingMode.Off;
            }

            if (_mainLight != null)
            {
                _mainLight.shadows = _mainShadowsOn ? _origLightShadow[_mainLight] : LightShadows.None;
            }

            foreach (var l in _extraLights)
            {
                if (l != null) l.enabled = _extraLightsOn;
            }

            UpdateNote();
        }

        private void ToggleBooks()
        {
            // Renderer aller Items (PrefabId) umschalten – frisch suchen, da Items zur Laufzeit geladen werden.
            foreach (var id in FindObjectsByType<PrefabId>(FindObjectsSortMode.None))
            {
                foreach (var r in id.GetComponentsInChildren<Renderer>(true)) r.enabled = _booksOn;
            }

            UpdateNote();
        }

        private void UpdateNote() => ProfilerSampler.Note = StateString();

        private string StateString()
            => $"T{B(_terrainsOn)} H{B(_mainShadowsOn)} G{B(_terrainShadowsOn)} L{B(_extraLightsOn)} B{B(_booksOn)} U{B(_hudsOn)}";

        private static int B(bool on) => on ? 1 : 0;

        private void Logged(string key) => Debug.Log($"[Bisect] {key} -> {StateString()}");
    }
}
