namespace CozySanta.Core.Keys
{
    /// <summary>
    /// Unveränderliche Beschreibung der Schlüsselanforderung eines Tors.
    /// Entscheidungslogik ohne Unity-Abhängigkeiten.
    /// </summary>
    public sealed class GateLockData
    {
        public readonly string[] RequiredKeyIds;

        public GateLockData(string[] requiredKeyIds)
        {
            RequiredKeyIds = requiredKeyIds ?? System.Array.Empty<string>();
        }

        public bool CanOpen(KeyInventory inventory) =>
            inventory != null && inventory.HasKeys(RequiredKeyIds);
    }
}
