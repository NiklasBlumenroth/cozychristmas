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

        [Header("Diagnose")]
        [Tooltip("Verbose Per-Trigger-Logs (standardmäßig aus). Warnungen bei Fehlkonfiguration erscheinen immer.")]
        [SerializeField] private bool verboseLog;

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
            if (_state != GateState.Closed) { Log($"Abbruch: State = {_state}"); return; }

            if (_inventoryManager == null)
            { Debug.LogWarning("[Gate] Kein KeyInventoryManager gefunden.", this); return; }

            if (other.GetComponentInParent<CozySanta.Runtime.Player.FirstPersonController>() == null)
            { Log($"'{other.gameObject.name}' ist kein Spieler – ignoriert."); return; }

            if (requiredKeyIds.Length == 0)
            { Debug.LogWarning("[Gate] requiredKeyIds ist leer – bitte ID im Inspector eintragen. Tor öffnet nicht.", this); return; }

            if (!_lock.CanOpen(_inventoryManager.Inventory))
            {
                if (verboseLog)
                {
                    var keys = string.Join(", ", _inventoryManager.Inventory.GetAllKeys());
                    Log($"Schlüssel fehlen – Inventar [{keys}], benötigt [{string.Join(", ", requiredKeyIds)}].");
                }
                return;
            }

            _state = GateState.Opening;
            _inventoryManager.ConsumeKeys(requiredKeyIds);
            Log($"Tor geöffnet (verbraucht: {string.Join(", ", requiredKeyIds)}).");
            StartCoroutine(OpenRoutine());
        }

        // Verbose-Diagnose, standardmäßig aus; pro Tor im Inspector aktivierbar.
        private void Log(string msg)
        {
            if (verboseLog) Debug.Log($"[Gate] {msg}", this);
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
