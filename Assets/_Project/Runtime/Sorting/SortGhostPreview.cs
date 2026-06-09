using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Apply-only: verwaltet einen durchscheinenden Laufzeit-Klon des aktuell getragenen Objekts und
    /// positioniert ihn am nächsten freien Slot des anvisierten Fachs. Enthält keine Fachlogik – die
    /// Gültigkeit (offenes, nicht volles Fach) liefert <see cref="SortTargetInteractable.HasFreeSlot"/>
    /// bzw. <see cref="SortTargetInteractable.TryGetSlotPose"/>. Wird vom Interaktions-Controller je
    /// Frame über <see cref="Set"/> gespeist.
    /// </summary>
    public sealed class SortGhostPreview
    {
        // Optionaler Override; ist er null, wird je Renderer das Originalmaterial geklont und grün
        // eingefärbt (siehe MakeGhostMaterial).
        private readonly Material _ghostMaterial;
        // Halbtransparentes Grün für die Vorschau.
        private static readonly Color GhostTint = new Color(0.30f, 1f, 0.40f, 0.45f);

        private GameObject _ghost;
        private int _sourceId;
        private SortTargetInteractable _fach;
        // Zur Laufzeit erzeugte Materialinstanzen, damit sie beim Verstecken sauber zerstört werden.
        private readonly List<Material> _runtimeMaterials = new List<Material>();

        public SortGhostPreview(Material ghostMaterial)
        {
            _ghostMaterial = ghostMaterial;
        }

        /// <summary>
        /// Zeigt/aktualisiert den Ghost für (<paramref name="fach"/>, <paramref name="source"/>) oder
        /// versteckt ihn, wenn etwas fehlt oder kein freier Slot vorhanden ist. Idempotent pro Frame.
        /// </summary>
        public void Set(SortTargetInteractable fach, Component source)
        {
            if (fach == null || source == null
                || !fach.TryGetSlotPose(out var pos, out var rot, out var scaleMul))
            {
                Hide();
                return;
            }

            var srcId = source.GetInstanceID();
            if (_ghost == null || _sourceId != srcId || _fach != fach)
            {
                Rebuild(source);
                _sourceId = srcId;
                _fach = fach;
            }

            if (_ghost == null)
            {
                return;
            }

            _ghost.transform.SetParent(fach.transform, worldPositionStays: false);
            _ghost.transform.SetPositionAndRotation(pos, rot);
            _ghost.transform.localScale = source.transform.localScale * scaleMul;
            if (!_ghost.activeSelf)
            {
                _ghost.SetActive(true);
            }
        }

        /// <summary>Entfernt den aktuellen Ghost (z. B. wenn kein Fach mehr anvisiert wird).</summary>
        public void Hide()
        {
            if (_ghost != null)
            {
                Object.Destroy(_ghost);
            }

            for (var i = 0; i < _runtimeMaterials.Count; i++)
            {
                if (_runtimeMaterials[i] != null)
                {
                    Object.Destroy(_runtimeMaterials[i]);
                }
            }

            _runtimeMaterials.Clear();
            _ghost = null;
            _sourceId = 0;
            _fach = null;
        }

        private void Rebuild(Component source)
        {
            Hide();
            _ghost = Object.Instantiate(source.gameObject);
            _ghost.name = "[Ghost] " + source.gameObject.name;
            StripToVisual(_ghost);
            ApplyGhostLook(_ghost);
        }

        // Lässt nur die Darstellung übrig: Collider/Rigidbody/Skripte (Pickup/Sortable etc.) entfernen,
        // damit der Ghost nicht aufnehmbar/sortierbar ist und die Interaktions-Probe ihn ignoriert.
        private static void StripToVisual(GameObject go)
        {
            foreach (var collider in go.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.Destroy(collider);
            }

            foreach (var body in go.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.Destroy(body);
            }

            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                Object.Destroy(behaviour);
            }
        }

        private void ApplyGhostLook(GameObject go)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;

                // Override-Material gewinnt, falls gesetzt; sonst je Slot das Original grün einfärben.
                if (_ghostMaterial != null)
                {
                    var fixedMats = renderer.sharedMaterials;
                    for (var i = 0; i < fixedMats.Length; i++)
                    {
                        fixedMats[i] = _ghostMaterial;
                    }

                    renderer.sharedMaterials = fixedMats;
                    continue;
                }

                var source = renderer.sharedMaterials;
                var tinted = new Material[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i] == null)
                    {
                        continue;
                    }

                    tinted[i] = MakeGhostMaterial(source[i]);
                    _runtimeMaterials.Add(tinted[i]);
                }

                renderer.sharedMaterials = tinted;
            }
        }

        // Klont das Originalmaterial, färbt es grün und stellt es (best effort, URP Lit/SimpleLit) auf
        // Transparent. Bei anderen Shadern bleibt zumindest die grüne Einfärbung erhalten.
        private static Material MakeGhostMaterial(Material source)
        {
            var m = new Material(source);

            if (m.HasProperty("_BaseColor"))
            {
                m.SetColor("_BaseColor", GhostTint);
            }

            if (m.HasProperty("_Color"))
            {
                m.SetColor("_Color", GhostTint);
            }

            // URP-Surface auf Transparent + Alpha-Blending (No-op bei Shadern ohne diese Properties).
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
            }

            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }
    }
}
