using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Rendering
{
    /// <summary>
    /// Zeichnet ruhende Items eines Gebäudes per GPU-Instancing statt einzeln (Apply zu
    /// <see cref="CozySanta.Core.Rendering.InstanceSlots"/>). Hintergrund: Tausende Item-Renderer =
    /// tausende Draw Calls (URP-SRP-Batcher fasst die Anzahl nicht zusammen). Da die Items massenhaft
    /// Duplikate sind (gleiches Mesh+Material), kollabiert <c>Graphics.RenderMeshInstanced</c> jede
    /// Variante auf einen Draw – egal ob 1 oder 1.000 Kopien.
    ///
    /// Liegt je Gebäude auf dem <c>ItemArea.ItemParent</c> (Gebäude-Root). Der <c>AreaActivator</c>
    /// deaktiviert diesen Root beim Verlassen → <see cref="Update"/> (und damit alle Draws) laufen nur
    /// im betretenen Gebäude. Items melden sich selbst an/ab: <see cref="Register"/> beim Ruhen
    /// (<c>SettlingBody.EnterRest</c>), <see cref="Unregister"/> beim Aufnehmen (<c>PlayerCarry.TryPickup</c>).
    ///
    /// Registrierte Items: eigener <see cref="MeshRenderer"/> aus (durch den Instanz-Draw ersetzt),
    /// Collider bleibt (weiter aufhebbar). Ruhende Items bewegen sich nicht → Weltmatrix wird einmal
    /// gecacht, kein Per-Frame-Neuberechnen. Der Zeichen-/Aufräum-Teil liegt in
    /// <c>InstancedItemRenderer.Draw.cs</c>.
    /// </summary>
    public sealed partial class InstancedItemRenderer : MonoBehaviour
    {
        // Grenze von Graphics.RenderMeshInstanced.
        private const int MaxPerBatch = 1023;

        private readonly Dictionary<GroupKey, Group> _groups = new Dictionary<GroupKey, Group>();
        private readonly HashSet<int> _registered = new HashSet<int>();

        /// <summary>Registriert alle Mesh-Renderer eines ruhenden Items für den Instanz-Draw und schaltet
        /// deren Einzel-Renderer ab. Doppel-Registrierung desselben Items ist ein No-Op.</summary>
        public void Register(Transform itemRoot)
        {
            if (itemRoot == null)
            {
                return;
            }

            var id = itemRoot.GetInstanceID();
            if (!_registered.Add(id))
            {
                return;
            }

            foreach (var r in itemRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                if (!r.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null)
                {
                    continue;
                }

                var mesh = mf.sharedMesh;
                var mats = r.sharedMaterials;
                var subCount = Mathf.Min(mesh.subMeshCount, mats.Length);
                var matrix = r.localToWorldMatrix;

                for (var s = 0; s < subCount; s++)
                {
                    var mat = mats[s];
                    if (mat == null)
                    {
                        continue;
                    }

                    var key = new GroupKey(mesh, mat, s, r.shadowCastingMode, r.receiveShadows, r.gameObject.layer);
                    GetOrCreateGroup(key).Add(id, itemRoot, matrix);
                }

                // Einzel-Renderer aus – der Instanz-Draw übernimmt. Collider bleibt unangetastet (aufhebbar).
                r.enabled = false;
            }
        }

        /// <summary>Meldet ein Item wieder ab (z. B. beim Aufnehmen): entfernt seine Instanz-Slots und
        /// schaltet die Einzel-Renderer wieder an, damit es getragen sichtbar ist. Unbekannt = No-Op.</summary>
        public void Unregister(Transform itemRoot)
        {
            if (itemRoot == null)
            {
                return;
            }

            var id = itemRoot.GetInstanceID();
            if (!_registered.Remove(id))
            {
                return;
            }

            RemoveOwner(id);

            foreach (var r in itemRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                r.enabled = true;
            }
        }

        // Entfernt alle Slots eines Besitzers aus allen Gruppen (von hinten, swap-sicher).
        private void RemoveOwner(int ownerId)
        {
            foreach (var g in _groups.Values)
            {
                for (var i = g.Slots.Count - 1; i >= 0; i--)
                {
                    if (g.Slots.OwnerAt(i) == ownerId)
                    {
                        g.RemoveAt(i);
                    }
                }
            }
        }

        private Group GetOrCreateGroup(GroupKey key)
        {
            if (!_groups.TryGetValue(key, out var g))
            {
                g = new Group(key);
                _groups.Add(key, g);
            }

            return g;
        }

        // Items werden nur selten zerstört (ClearArea/Reset). Die Owner-null-Prüfung läuft daher gedrosselt
        // (nicht jeden Frame über tausende Items) – die Szene ist CPU-/Main-Thread-gebunden, jeder gesparte
        // Per-Frame-Scan zählt.
        private const int CompactInterval = 120;
        private int _compactCountdown;

        private void Update()
        {
            if (--_compactCountdown <= 0)
            {
                CompactDestroyed();
                _compactCountdown = CompactInterval;
            }

            DrawAll();
        }
    }
}
