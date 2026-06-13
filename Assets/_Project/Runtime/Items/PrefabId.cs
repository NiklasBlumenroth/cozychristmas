using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Stabiler Wiedererkennungs-Schlüssel eines aufnehmbaren Items für die Persistenz. Beim Speichern
    /// wird <see cref="Key"/> + Pose abgelegt, beim Laden über den <see cref="ItemCatalog"/> wieder
    /// das passende Prefab instanziiert. Standardmäßig der Prefab-Name (vom Setup-Tool gesetzt).
    /// </summary>
    public sealed class PrefabId : MonoBehaviour
    {
        [Tooltip("Eindeutiger Schlüssel (i. d. R. der Prefab-Name); muss im ItemCatalog vorkommen.")]
        [SerializeField] private string key;

        public string Key => key;

        /// <summary>Setzt den Schlüssel (für Editor-/Setup-Code).</summary>
        public void SetKey(string value) => key = value;
    }
}
