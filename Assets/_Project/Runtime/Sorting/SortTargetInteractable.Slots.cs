using CozySanta.Core.Sorting;
using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Raster-Teil des Slot-Containers (Apply-Schicht): Aufbau des 3D-Rasters + Spalten-Collider,
    /// Belegungsabfragen (hinterster frei / vorderster belegt), Zell-Posen und die Ghost-Pose.
    /// Ausgelagert, um die Kern-Interaktion in <see cref="SortTargetInteractable"/> schlank zu halten.
    /// </summary>
    public sealed partial class SortTargetInteractable
    {
        private void BuildContainer()
        {
            var sx = Mathf.Max(1, gridSize.x);
            var sy = Mathf.Max(1, gridSize.y);
            var sz = Mathf.Max(1, gridSize.z);

            _target = new SortTarget(new SortKey(acceptedFacets), requiredCount);
            _grid = new Component[sx, sy, sz];

            // Vorhandene Spalten (z. B. nach Configure) entfernen, dann neu erzeugen.
            if (_columns != null)
            {
                foreach (var column in _columns)
                {
                    if (column != null) Destroy(column.gameObject);
                }
            }

            // Fach-eigene Collider deaktivieren – die Spalten-Collider sind die Ziele.
            foreach (var bodyCollider in GetComponents<Collider>())
            {
                bodyCollider.enabled = false;
            }

            _columns = new SlotColumn[sx, sy];
            var reference = slotAnchor != null ? slotAnchor : transform;
            var inverse = InverseScale(transform.lossyScale);

            for (var ix = 0; ix < sx; ix++)
            {
                for (var iy = 0; iy < sy; iy++)
                {
                    var go = new GameObject($"Column_{ix}_{iy}");
                    go.transform.SetParent(transform, worldPositionStays: false);
                    go.transform.localScale = inverse; // Welt-Scale 1 (Collidergröße in Metern)
                    go.transform.position = CellWorldPos(ix, iy, (sz - 1) * 0.5f);
                    go.transform.rotation = reference.rotation;

                    var box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(cellSpacing.x, cellSpacing.y, Mathf.Max(cellSpacing.z, cellSpacing.z * sz));

                    var column = go.AddComponent<SlotColumn>();
                    column.Bind(this, ix, iy);
                    _columns[ix, iy] = column;
                }
            }
        }

        private bool InRange(int x, int y)
            => _grid != null && x >= 0 && y >= 0 && x < _grid.GetLength(0) && y < _grid.GetLength(1);

        // Hinterster (höchstes z) freier Slot der Spalte – Ziel fürs Einlegen.
        private bool TryGetRearEmpty(int x, int y, out int z)
        {
            for (z = _grid.GetLength(2) - 1; z >= 0; z--)
            {
                if (_grid[x, y, z] == null) return true;
            }

            z = -1;
            return false;
        }

        // Vorderster (kleinstes z) belegter Slot der Spalte – Ziel fürs Entnehmen.
        private bool TryGetFrontOccupied(int x, int y, out int z)
        {
            var depth = _grid.GetLength(2);
            for (z = 0; z < depth; z++)
            {
                if (_grid[x, y, z] != null) return true;
            }

            z = -1;
            return false;
        }

        // Rastergröße aus dem Laufzeit-Raster (falls vorhanden) oder direkt aus gridSize (Edit-Time-Gizmos).
        private Vector3Int GridDims()
            => _grid != null
                ? new Vector3Int(_grid.GetLength(0), _grid.GetLength(1), _grid.GetLength(2))
                : new Vector3Int(Mathf.Max(1, gridSize.x), Mathf.Max(1, gridSize.y), Mathf.Max(1, gridSize.z));

        private Vector3 CellOffset(int ix, int iy, float iz)
        {
            var dims = GridDims();
            return new Vector3(
                (ix - (dims.x - 1) * 0.5f) * cellSpacing.x,
                (iy - (dims.y - 1) * 0.5f) * cellSpacing.y,
                iz * cellSpacing.z);
        }

        private Vector3 CellWorldPos(int ix, int iy, float iz)
        {
            var reference = slotAnchor != null ? slotAnchor : transform;
            return reference.position + (reference.rotation * CellOffset(ix, iy, iz));
        }

        // Reihenrichtung folgt der Anker-Rotation; die Einlage-Orientierung wird zusätzlich um
        // placedEuler gedreht (entkoppelt von der Rasterrichtung).
        private Quaternion CellRotation()
            => (slotAnchor != null ? slotAnchor : transform).rotation * Quaternion.Euler(placedEuler);

        private void PlaceVisual(Component component, int x, int y, int z)
        {
            component.transform.SetParent(transform, worldPositionStays: false);
            component.transform.SetPositionAndRotation(CellWorldPos(x, y, z), CellRotation());
            ApplyPlacedScale(component);

            // Eingelegte Objekte sind reine Visuals: Collider aus (Ziel ist der Spalten-Collider), kinematisch.
            foreach (var collider in component.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                collider.enabled = false;
            }

            if (component.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private void RestoreVisual(Component component, int id)
        {
            if (_originalScale.TryGetValue(id, out var original))
            {
                component.transform.localScale = original;
                _originalScale.Remove(id);
            }
            // Collider/Physik werden von PlayerCarry.TryPickup (carried) wieder gesetzt.
        }

        private void ApplyPlacedScale(Component component)
        {
            if (!(placedScale > 0f) || Mathf.Approximately(placedScale, 1f))
            {
                return;
            }

            var id = component.GetInstanceID();
            if (!_originalScale.ContainsKey(id))
            {
                _originalScale[id] = component.transform.localScale;
            }

            component.transform.localScale = _originalScale[id] * placedScale;
        }

        /// <summary>Ghost-Pose für Spalte (x,y): hinterster freier Slot. False, wenn der getragene
        /// <paramref name="key"/> nicht passt, die Spalte voll oder das Fach gesperrt ist.</summary>
        public bool TryGetGhostCellPose(int x, int y, SortKey key,
            out Vector3 position, out Quaternion rotation, out float scaleMultiplier)
        {
            position = default;
            rotation = default;
            scaleMultiplier = (placedScale > 0f && !Mathf.Approximately(placedScale, 1f)) ? placedScale : 1f;
            if (_target == null || _target.IsClosed || !_target.Classify(key) || !InRange(x, y))
            {
                return false;
            }

            if (!TryGetRearEmpty(x, y, out var z))
            {
                return false;
            }

            position = CellWorldPos(x, y, z);
            rotation = CellRotation();
            return true;
        }

#if UNITY_EDITOR
        // Edit-Time-Vorschau des Slot-Rasters: Drahtgitter-Boxen je Slot, Orientierungs-Tick (Vorderseite
        // des eingelegten Objekts) und Hervorhebung des hintersten Slots (Füllstart) je Spalte.
        private void OnDrawGizmos()
        {
            if (cellSpacing.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var dims = GridDims();
            var rot = CellRotation();
            var cellSize = new Vector3(
                Mathf.Max(0.01f, Mathf.Abs(cellSpacing.x) * 0.9f),
                Mathf.Max(0.01f, Mathf.Abs(cellSpacing.y) * 0.9f),
                Mathf.Max(0.01f, Mathf.Abs(cellSpacing.z) * 0.9f));

            for (var x = 0; x < dims.x; x++)
            {
                for (var y = 0; y < dims.y; y++)
                {
                    for (var z = 0; z < dims.z; z++)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(CellWorldPos(x, y, z), rot, Vector3.one);
                        Gizmos.color = z == dims.z - 1
                            ? new Color(0.30f, 1f, 0.40f, 0.90f)   // hinterster Slot = Füllstart
                            : new Color(0.30f, 1f, 0.40f, 0.35f);
                        Gizmos.DrawWireCube(Vector3.zero, cellSize);

                        Gizmos.color = new Color(0.20f, 0.60f, 1f, 0.90f);
                        Gizmos.DrawLine(Vector3.zero, Vector3.forward * (cellSize.z * 0.5f + 0.02f));
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
#endif

        private static Vector3 InverseScale(Vector3 s) => new Vector3(
            Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x,
            Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y,
            Mathf.Approximately(s.z, 0f) ? 1f : 1f / s.z);
    }
}
