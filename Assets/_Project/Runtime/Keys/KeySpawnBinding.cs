using CozySanta.Runtime.Progression;
using UnityEngine;

namespace CozySanta.Runtime.Keys
{
    /// <summary>
    /// Spawnt ein Schlüssel-Prefab einmalig wenn die verbundene <see cref="AreaTracker"/>-Area
    /// abgeschlossen wird. Liegt als Geschwister-Komponente neben dem AreaTracker oder an einem
    /// eigenen GameObject im Level.
    /// </summary>
    public sealed class KeySpawnBinding : MonoBehaviour
    {
        [SerializeField] private AreaTracker   targetArea;
        [SerializeField] private GameObject    keyPrefab;
        [SerializeField] private Transform     spawnTransform;

        private bool _spawned;

        private void OnEnable()
        {
            if (targetArea != null) targetArea.OnAreaCompleted += HandleAreaCompleted;
        }

        private void OnDisable()
        {
            if (targetArea != null) targetArea.OnAreaCompleted -= HandleAreaCompleted;
        }

        private void HandleAreaCompleted()
        {
            if (_spawned || keyPrefab == null) return;
            _spawned = true;

            var pos = spawnTransform != null ? spawnTransform.position : transform.position;
            var rot = spawnTransform != null ? spawnTransform.rotation : Quaternion.identity;
            Instantiate(keyPrefab, pos, rot);
        }
    }
}
