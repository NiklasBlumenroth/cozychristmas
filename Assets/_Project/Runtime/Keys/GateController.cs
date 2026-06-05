using System.Collections;
using CozySanta.Core.Keys;
using UnityEngine;

namespace CozySanta.Runtime.Keys
{
    /// <summary>
    /// Tor-Objekt das sich automatisch öffnet, wenn der Spieler die Proximity-Zone betritt und
    /// alle benötigten Schlüssel besitzt. Rotiert um 90° um die Y-Achse von <see cref="doorPivot"/>.
    /// Player-Erkennung über <see cref="KeyInventoryManager"/>-Komponente (kein Tag-Check).
    /// </summary>
    public sealed class GateController : MonoBehaviour
    {
        [SerializeField] private string[] requiredKeyIds = new string[0];
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openDuration = 0.8f;
        [SerializeField] private float openAngle = 90f;

        private GateLockData          _lock;
        private GateState             _state = GateState.Closed;
        private KeyInventoryManager   _inventoryManager;

        private void Awake()
        {
            _lock = new GateLockData(requiredKeyIds);
        }

        private void Start()
        {
            _inventoryManager = Object.FindFirstObjectByType<KeyInventoryManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Gate] OnTriggerEnter: {other.gameObject.name}");

            if (_state != GateState.Closed)
            { Debug.Log($"[Gate] Abbruch: State = {_state}"); return; }

            if (_inventoryManager == null)
            { Debug.LogWarning("[Gate] Kein KeyInventoryManager gefunden."); return; }

            if (other.GetComponentInParent<CozySanta.Runtime.Player.FirstPersonController>() == null)
            { Debug.Log($"[Gate] '{other.gameObject.name}' ist kein Spieler – ignoriert."); return; }

            var keys = string.Join(", ", _inventoryManager.Inventory.GetAllKeys());
            var needed = string.Join(", ", requiredKeyIds);
            Debug.Log($"[Gate] Spieler erkannt. Inventar: [{keys}]  Benötigt: [{needed}]");

            if (requiredKeyIds.Length == 0)
            { Debug.LogWarning("[Gate] requiredKeyIds ist leer – bitte ID im Inspector eintragen. Tor öffnet nicht.", this); return; }

            if (!_lock.CanOpen(_inventoryManager.Inventory))
            { Debug.Log("[Gate] Schlüssel fehlen – Tor bleibt zu."); return; }

            _state = GateState.Opening;
            _inventoryManager.ConsumeKeys(requiredKeyIds);
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            var pivot   = doorPivot != null ? doorPivot : transform;
            var start   = pivot.localRotation;
            var target  = start * Quaternion.Euler(0f, openAngle, 0f);
            var elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += UnityEngine.Time.deltaTime;
                pivot.localRotation = Quaternion.Slerp(start, target, elapsed / openDuration);
                yield return null;
            }

            pivot.localRotation = target;
            _state = GateState.Open;
        }
    }
}
