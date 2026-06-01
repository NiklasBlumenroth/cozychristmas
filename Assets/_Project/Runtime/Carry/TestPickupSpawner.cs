using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.Carry
{
    /// <summary>
    /// Test-/Debug-Hilfe: spawnt bei Taste „O" ein aufnehmbares Prefab vor dem Spieler. Das Prefab hat
    /// einen Rigidbody mit Schwerkraft und fällt nach dem Spawnen zu Boden.
    /// </summary>
    public sealed class TestPickupSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private float distance = 1.5f;
        [SerializeField] private float heightOffset = 0.5f;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.oKey.wasPressedThisFrame || prefab == null)
            {
                return;
            }

            var origin = spawnOrigin != null ? spawnOrigin : transform;
            var position = origin.position + (origin.forward * distance) + (Vector3.up * heightOffset);
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
