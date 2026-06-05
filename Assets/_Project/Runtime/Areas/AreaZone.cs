using CozySanta.Runtime.Keys;
using CozySanta.Runtime.Progression;
using UnityEngine;

namespace CozySanta.Runtime.Areas
{
    /// <summary>
    /// BoxCollider-Trigger der eine Area räumlich definiert. Meldet Betreten/Verlassen an den
    /// <see cref="AreaManager"/>; dieser zeigt/versteckt das zugeordnete <see cref="hudSection"/>.
    /// Player-Erkennung über <see cref="KeyInventoryManager"/> (kein Tag-Check).
    /// Erfordert einen BoxCollider mit isTrigger = true am selben oder Kind-GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class AreaZone : MonoBehaviour
    {
        [SerializeField] private AreaTracker areaTracker;
        [SerializeField] private AreaManager areaManager;
        [SerializeField] private GameObject  hudSection;

        public AreaTracker  Tracker    => areaTracker;
        public GameObject   HudSection => hudSection;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            areaManager?.RegisterZone(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            areaManager?.UnregisterZone(this);
        }

        private static bool IsPlayer(Collider col) =>
            col.GetComponentInParent<CozySanta.Runtime.Player.FirstPersonController>() != null;
    }
}
