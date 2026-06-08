using UnityEngine;

namespace CozySanta.Runtime.Teleport
{
    /// <summary>
    /// Winziger Weiterleiter, den der <see cref="TeleportRouter"/> beim Start automatisch an jeden
    /// Trigger-Collider hängt. Unity feuert <c>OnTriggerEnter/Exit</c> nur auf der Komponente am
    /// Collider-Objekt selbst – diese leitet die Events mit dem Paar-Index an den Router zurück.
    /// Nicht manuell hinzufügen (wird zur Laufzeit erzeugt).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TeleportTriggerForwarder : MonoBehaviour
    {
        private TeleportRouter _router;
        private int _index;

        public void Bind(TeleportRouter router, int index)
        {
            _router = router;
            _index = index;
        }

        private void OnTriggerEnter(Collider other) => _router?.HandleEnter(_index, other);

        private void OnTriggerExit(Collider other) => _router?.HandleExit(_index, other);
    }
}
