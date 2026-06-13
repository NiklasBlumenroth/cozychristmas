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

        [Tooltip("Welche Items zu diesem Gebäude gehören (z. B. der Bücher-Katalog für die Bibliothek).")]
        [SerializeField] private ItemCatalog catalog;

        [Tooltip("Höchstzahl je Variante in diesem Bereich (z. B. 20 pro Buch).")]
        [SerializeField] private int maxPerVariant = 20;

        private Collider[] _colliders;

        public string AreaName => areaName;

        /// <summary>Katalog der zu diesem Bereich gehörenden Items (kann null sein).</summary>
        public ItemCatalog Catalog => catalog;

        /// <summary>Höchstzahl je Variante in diesem Bereich.</summary>
        public int MaxPerVariant => maxPerVariant;

        /// <summary>Konfiguriert Katalog + Höchstzahl (für Editor-/Setup-Code).</summary>
        public void Configure(ItemCatalog itemCatalog, int max)
        {
            catalog = itemCatalog;
            maxPerVariant = max;
        }

        private Collider[] Colliders => _colliders != null && _colliders.Length > 0
            ? _colliders
            : (_colliders = GetComponentsInChildren<Collider>());

        /// <summary>True, wenn der Weltpunkt innerhalb eines der Bereichs-Collider liegt. Nutzt die
        /// Welt-AABB (<c>bounds</c>) – robust für Trigger/Prefab-Collider und ohne Exceptions bei
        /// nicht-konvexen MeshCollidern (ClosestPoint würde dort werfen).</summary>
        public bool Contains(Vector3 worldPoint)
        {
            foreach (var col in Colliders)
            {
                if (col != null && col.bounds.Contains(worldPoint))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Diagnose: Anzahl gefundener Collider (0 = Bereich hat kein Volumen → erkennt nie jemanden).</summary>
        public int ColliderCount => Colliders.Length;
    }
}
