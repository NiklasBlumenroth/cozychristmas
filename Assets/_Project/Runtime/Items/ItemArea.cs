using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Benannter Bereich für die Item-Persistenz (z. B. „Bibliothek"). Definiert räumlich über einen
    /// <see cref="Collider"/> (i. d. R. BoxCollider, Trigger), welche aufnehmbaren Items zu diesem
    /// Bereich gehören. Kann auf demselben GameObject wie eine <c>AreaZone</c> liegen, ist aber
    /// bewusst von HUD/Fortschritt entkoppelt – hier geht es nur um Speichern/Laden von Items.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ItemArea : MonoBehaviour
    {
        [Tooltip("Eindeutiger Bereichsname; bestimmt den Dateinamen der gespeicherten Pose.")]
        [SerializeField] private string areaName = "Bereich";

        private Collider[] _colliders;

        public string AreaName => areaName;

        private Collider[] Colliders => _colliders != null && _colliders.Length > 0
            ? _colliders
            : (_colliders = GetComponentsInChildren<Collider>());

        /// <summary>True, wenn der Weltpunkt innerhalb (oder am Rand) eines der Bereichs-Collider liegt.</summary>
        public bool Contains(Vector3 worldPoint)
        {
            foreach (var col in Colliders)
            {
                if (col == null) continue;
                // ClosestPoint liefert bei „innen" exakt den Punkt selbst -> Distanz 0.
                if ((col.ClosestPoint(worldPoint) - worldPoint).sqrMagnitude <= 1e-6f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
