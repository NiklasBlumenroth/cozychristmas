using System;
using CozySanta.Core.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Runtime.Rendering
{
    public sealed partial class InstancedItemRenderer
    {
        // Sicherheits-Marge um die Gruppen-AABB (worldBounds), damit Unitys grobes Gruppen-Frustum-Culling
        // am Rand nicht zu früh greift.
        private const float BoundsMargin = 1f;

        // Zeichnet jede Gruppe in 1023er-Chunks. Kein Per-Instanz-Culling: die Szene ist CPU-/Main-Thread-
        // gebunden (GPU hat reichlich Luft), darum spart das Zeichnen ALLER Instanzen die teure Per-Frame-
        // CPU-Arbeit. Grobes Culling ganzer Gruppen übernimmt Unity günstig über `worldBounds` (Gruppe
        // komplett außerhalb des Sichtkegels → der ganze Call entfällt). Ruhende Items bewegen sich nicht
        // → Matrizen sind gecacht.
        private void DrawAll()
        {
            foreach (var g in _groups.Values)
            {
                var count = g.Slots.Count;
                if (g.Broken || count == 0 || g.Mesh == null)
                {
                    continue;
                }

                var rp = g.Params;
                rp.worldBounds = g.WorldBounds(BoundsMargin, transform.position);

                try
                {
                    foreach (var (start, c) in InstanceSlots.ChunkRanges(count, MaxPerBatch))
                    {
                        Graphics.RenderMeshInstanced(rp, g.Mesh, g.Submesh, g.Matrices, c, start);
                    }
                }
                catch (System.Exception e)
                {
                    // Material/Shader kann nicht instanziert werden: Gruppe einmalig stilllegen und ihre
                    // Items wieder einzeln zeichnen lassen (kein Per-Frame-Fehlerflut, korrekt sichtbar).
                    g.Broken = true;
                    g.ReenableOwners();
                    Debug.LogWarning($"[InstancedItemRenderer] Gruppe '{g.Mesh?.name}' nicht instanzierbar – " +
                                     $"Fallback auf Einzel-Renderer. {e.Message}", this);
                }
            }
        }

        // Räumt Slots zerstörter Items (z. B. ItemPersistence.ClearArea/Reset) auf, damit keine
        // Geister-Matrix weitergezeichnet wird. Billiger Null-Scan; pro Gruppe von hinten swap-entfernt.
        private void CompactDestroyed()
        {
            foreach (var g in _groups.Values)
            {
                for (var i = g.Slots.Count - 1; i >= 0; i--)
                {
                    if (g.Owners[i] == null)
                    {
                        _registered.Remove(g.Slots.OwnerAt(i));
                        g.RemoveAt(i);
                    }
                }
            }
        }

        // Gruppenschlüssel: gleiche (Mesh, Submesh, Material, Schatten, Schattenempfang, Layer) → ein Draw.
        private readonly struct GroupKey : IEquatable<GroupKey>
        {
            public readonly Mesh Mesh;
            public readonly Material Material;
            public readonly int Submesh;
            public readonly ShadowCastingMode Shadow;
            public readonly bool Receive;
            public readonly int Layer;

            public GroupKey(Mesh mesh, Material material, int submesh, ShadowCastingMode shadow, bool receive, int layer)
            {
                Mesh = mesh;
                Material = material;
                Submesh = submesh;
                Shadow = shadow;
                Receive = receive;
                Layer = layer;
            }

            public bool Equals(GroupKey o) =>
                Mesh == o.Mesh && Material == o.Material && Submesh == o.Submesh &&
                Shadow == o.Shadow && Receive == o.Receive && Layer == o.Layer;

            public override bool Equals(object o) => o is GroupKey k && Equals(k);

            public override int GetHashCode()
            {
                unchecked
                {
                    var h = Mesh != null ? Mesh.GetHashCode() : 0;
                    h = (h * 397) ^ (Material != null ? Material.GetHashCode() : 0);
                    h = (h * 397) ^ Submesh;
                    h = (h * 397) ^ (int)Shadow;
                    h = (h * 397) ^ (Receive ? 1 : 0);
                    h = (h * 397) ^ Layer;
                    return h;
                }
            }
        }

        // Eine instanzierte Gruppe: dichte Parallel-Arrays (Matrix/Owner), Index-Buchhaltung über InstanceSlots.
        private sealed class Group
        {
            public readonly Mesh Mesh;
            public readonly int Submesh;
            public RenderParams Params;
            public readonly InstanceSlots Slots = new InstanceSlots();
            public Matrix4x4[] Matrices = new Matrix4x4[16];
            public Transform[] Owners = new Transform[16];

            private Bounds _bounds;
            private bool _hasBounds;

            // Material/Shader nicht instanzierbar → Gruppe stillgelegt, Items rendern einzeln (Fallback).
            public bool Broken;

            public Group(GroupKey key)
            {
                Mesh = key.Mesh;
                Submesh = key.Submesh;

                // RenderMeshInstanced verlangt das Instancing-Flag am Material. Zur Laufzeit erzwingen
                // (in-memory, kein Asset-Schreiben) → der Renderer funktioniert unabhängig davon, ob das
                // Editor-Tool das Material erwischt hat (z. B. in FBX eingebettete Materialien).
                if (key.Material != null && !key.Material.enableInstancing)
                {
                    key.Material.enableInstancing = true;
                }

                Params = new RenderParams(key.Material)
                {
                    shadowCastingMode = key.Shadow,
                    receiveShadows = key.Receive,
                    layer = key.Layer,
                };
            }

            public void Add(int ownerId, Transform owner, in Matrix4x4 matrix)
            {
                var i = Slots.Add(ownerId);
                EnsureCapacity(i + 1);
                Matrices[i] = matrix;
                Owners[i] = owner;

                var p = (Vector3)matrix.GetColumn(3);
                if (!_hasBounds)
                {
                    _bounds = new Bounds(p, Vector3.zero);
                    _hasBounds = true;
                }
                else
                {
                    _bounds.Encapsulate(p);
                }
            }

            // Swap-with-last in der Core-Slot-Liste; die Parallel-Arrays werden im Gleichtakt nachgezogen.
            public void RemoveAt(int index)
            {
                var movedFrom = Slots.RemoveAt(index);
                if (movedFrom >= 0)
                {
                    Matrices[index] = Matrices[movedFrom];
                    Owners[index] = Owners[movedFrom];
                }

                Owners[Slots.Count] = null; // freigewordenen Slot-Verweis lösen (kein Leak/Geister-Transform)
            }

            // Schaltet die Einzel-Renderer aller Items dieser Gruppe wieder an (Fallback bei nicht
            // instanzierbarem Material). Doppelte Owner (Multi-Submesh) sind unschädlich.
            public void ReenableOwners()
            {
                for (var i = 0; i < Slots.Count; i++)
                {
                    var owner = Owners[i];
                    if (owner == null)
                    {
                        continue;
                    }

                    foreach (var r in owner.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                    {
                        r.enabled = true;
                    }
                }
            }

            public Bounds WorldBounds(float margin, Vector3 fallbackCenter)
            {
                if (!_hasBounds || Slots.Count == 0)
                {
                    return new Bounds(fallbackCenter, Vector3.one);
                }

                var b = _bounds;
                b.Expand(margin * 2f); // Bounds.Expand erweitert die Größe um den Betrag (je Seite die Hälfte)
                return b;
            }

            private void EnsureCapacity(int needed)
            {
                if (Matrices.Length >= needed)
                {
                    return;
                }

                var cap = Matrices.Length;
                while (cap < needed)
                {
                    cap *= 2;
                }

                Array.Resize(ref Matrices, cap);
                Array.Resize(ref Owners, cap);
            }
        }
    }
}
