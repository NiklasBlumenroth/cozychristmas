using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Optionaler item-eigener Dreh-Offset fürs Einsortieren. Wird ein Objekt in ein Fach gelegt – oder
    /// als durchscheinender Ghost vorgeschaut –, dreht das Fach es zusätzlich um diesen Euler-Winkel,
    /// relativ zur Fach-/Slot-Ausrichtung. Damit lässt sich ein Mesh mit „schiefer" Grund-Ausrichtung
    /// (z. B. die Zuckerstangen, deren FBX anders liegt als die Lebkuchen) in JEDEM Fach korrekt hinlegen,
    /// ohne die Fächer item-spezifisch zu machen – jedes Item bleibt in jedes Fach legbar. Ghost und
    /// tatsächliche Pose nutzen denselben Offset. Ohne diese Komponente = kein Offset.
    /// </summary>
    public sealed class SortPlacementRotation : MonoBehaviour
    {
        [Tooltip("Zusätzliche Drehung (Euler-Winkel) beim Einlegen/Ghost, relativ zur Fach-Ausrichtung.")]
        [SerializeField] private Vector3 placedEuler;

        /// <summary>Der Dreh-Offset als Quaternion (Identität bei 0).</summary>
        public Quaternion Offset => Quaternion.Euler(placedEuler);

        /// <summary>Euler-Winkel des Offsets (für Editor-/Setup-Code).</summary>
        public Vector3 PlacedEuler
        {
            get => placedEuler;
            set => placedEuler = value;
        }

#if UNITY_EDITOR
        // Edit-Mode-Vorschau (ohne Play-Mode): graue Box = Grund-Ausrichtung, grüne Box = mit Offset.
        // So lässt sich der richtige Winkel direkt am Prefab/der Instanz eintippen und sofort sehen.
        private void OnDrawGizmosSelected()
        {
            if (!TryGetLocalBounds(out var center, out var size))
            {
                return;
            }

            var pivot = transform.TransformPoint(center);

            Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(pivot, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * size.magnitude * 0.5f);

            Gizmos.color = new Color(0.30f, 1f, 0.40f, 0.95f);
            Gizmos.matrix = Matrix4x4.TRS(pivot, transform.rotation * Offset, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * size.magnitude * 0.5f);

            Gizmos.matrix = Matrix4x4.identity;
        }

        // Kombinierte Mesh-Bounds im lokalen Raum dieses Transforms (robust bei gedrehtem/skaliertem Root).
        private bool TryGetLocalBounds(out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero;
            size = Vector3.zero;
            var bounds = new Bounds();
            var has = false;

            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;

                var toLocal = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var c = mesh.bounds.center;
                var e = mesh.bounds.extents;
                for (var i = 0; i < 8; i++)
                {
                    var corner = c + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y,
                        (i & 4) == 0 ? -e.z : e.z);
                    var p = toLocal.MultiplyPoint3x4(corner);
                    if (!has) { bounds = new Bounds(p, Vector3.zero); has = true; }
                    else bounds.Encapsulate(p);
                }
            }

            if (!has) return false;
            center = bounds.center;
            size = bounds.size;
            return true;
        }
#endif
    }
}
