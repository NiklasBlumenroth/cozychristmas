using System.Collections;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Geschenk-Truhe der Geschenkehalle (Apply-Schicht zu <see cref="GiftChestValidation"/>). Per
    /// Interaktion (Rechtsklick) öffnet/schließt der Spieler den Deckel; geworfene Items fallen physisch
    /// hinein. Beim Schließen wird der Inhalt validiert (siehe <c>GiftChest.Validate.cs</c>): ist alles
    /// die akzeptierte Sorte, verschwinden die Items und werden gezählt; ist eines falsch, fliegt der
    /// gesamte Inhalt durch die Röhre zurück. Erreicht der Zählstand die Soll-Menge, verriegelt die Truhe
    /// dauerhaft und der Spieler weiß: diese Geschenkart ist abgeschlossen.
    /// </summary>
    public sealed partial class GiftChest : MonoBehaviour, IInteractable
    {
        // Konkrete Subklasse, damit Unity das generische UnityEvent serialisieren kann.
        [System.Serializable] private sealed class IntEvent : UnityEvent<int> { }

        [Header("Akzeptierte Sorte (genau ein SortKey)")]
        [Tooltip("Facettenwerte der akzeptierten Geschenkart, identisch zum Sortable des Item-Prefabs " +
                 "(i. d. R. ein Wert = Prefab-Name). Vom Setup-Tool gesetzt.")]
        [SerializeField] private string[] acceptedFacets = new string[0];

        [Tooltip("Gesamtzahl dieser Geschenkart im Raum. Erreicht der Zählstand diesen Wert, verriegelt " +
                 "die Truhe.")]
        [SerializeField] private int required = 100;

        [Header("Deckel")]
        [Tooltip("Der bewegliche Deckel (chest_top). Leer = dieses Transform.")]
        [SerializeField] private Transform lid;
        [Tooltip("Lokale Euler-Drehung des Deckels im geöffneten Zustand (relativ zur geschlossenen Pose). " +
                 "Größerer Winkel als 90°, damit oben Platz zum Einwerfen ist.")]
        [SerializeField] private Vector3 openEuler = new Vector3(-110f, 0f, 0f);
        [SerializeField] private float openDuration = 0.5f;

        [Header("Innenraum & Auswurf")]
        [Tooltip("Trigger-Box, die den Innenraum markiert. Beim Schließen werden alle Sortable-Items in " +
                 "diesem Volumen geprüft.")]
        [SerializeField] private BoxCollider insideVolume;
        [Tooltip("Ziel, an dem abgewiesene Items wieder erscheinen (z. B. 'RöhrenPosition').")]
        [SerializeField] private Transform ejectTarget;
        [Tooltip("Zufällige Streuung (m) am Auswurfpunkt, damit Items nicht exakt überlappen.")]
        [SerializeField] private float ejectScatter = 0.35f;
        [Tooltip("Layer, auf denen nach Items gesucht wird (Standard: alle).")]
        [SerializeField] private LayerMask itemMask = ~0;

        [Header("Ereignisse")]
        [Tooltip("Feuert nach jeder erfolgreichen Annahme mit der Anzahl angenommener Items (XP/Andockpunkt).")]
        [SerializeField] private IntEvent onItemsAccepted = new IntEvent();
        [Tooltip("Feuert einmalig, wenn die Truhe die Soll-Menge erreicht und verriegelt (Area-Andockpunkt).")]
        [SerializeField] private UnityEvent onLocked = new UnityEvent();

        private SortKey _accepted;
        private bool    _builtKey;
        private int     _acceptedCount;
        private bool    _locked;
        private bool    _isOpen;
        private bool    _animating;
        private Quaternion _closedLocalRot;

        /// <summary>Bisher angenommene (eingelagerte) Items.</summary>
        public int AcceptedCount => _acceptedCount;

        /// <summary>True, sobald die Soll-Menge erreicht ist (dauerhaft verschlossen).</summary>
        public bool IsLocked => _locked;

        /// <summary>True, wenn der Deckel offen steht.</summary>
        public bool IsOpen => _isOpen;

        private Transform Lid => lid != null ? lid : transform;

        private SortKey Accepted
        {
            get
            {
                if (!_builtKey)
                {
                    _accepted = new SortKey(acceptedFacets);
                    _builtKey = true;
                }

                return _accepted;
            }
        }

        /// <summary>Setzt die akzeptierte Sorte (für Editor-/Setup-Code).</summary>
        public void SetAccepted(string[] facets, int requiredCount)
        {
            acceptedFacets = facets ?? new string[0];
            _accepted = new SortKey(acceptedFacets);
            _builtKey = true;
            required = requiredCount;
        }

        /// <summary>Registriert einen Hörer für das Verriegeln (z. B. AreaTracker bucht +1).</summary>
        public void AddLockListener(UnityAction listener) => onLocked.AddListener(listener);

        /// <summary>Registriert einen Hörer für jede Annahme (Anzahl angenommener Items).</summary>
        public void AddAcceptListener(UnityAction<int> listener) => onItemsAccepted.AddListener(listener);

        private void Awake()
        {
            _closedLocalRot = Lid.localRotation;
        }

        // IInteractable ----------------------------------------------------------------------------

        public string PromptText
        {
            get
            {
                if (_locked) return "Abgeschlossen";
                return _isOpen ? "Truhe schließen" : "Truhe öffnen";
            }
        }

        /// <summary>Rechtsklick-Aktion: verriegelte Truhe bleibt zu; sonst Deckel auf/zu. Beim Schließen
        /// wird der Inhalt validiert (siehe partielle <c>Validate</c>-Datei).</summary>
        public void Interact()
        {
            if (_locked || _animating) return;

            if (_isOpen)
            {
                ValidateAndClose();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            _isOpen = true;
            AnimateLid(_closedLocalRot * Quaternion.Euler(openEuler));
        }

        // Schließt den Deckel optisch (von ValidateAndClose nach der Inhaltsprüfung aufgerufen).
        private void CloseLid(bool thenLock)
        {
            _isOpen = false;
            AnimateLid(_closedLocalRot, thenLock);
        }

        private void AnimateLid(Quaternion target, bool thenLock = false)
        {
            StopAllCoroutines();
            StartCoroutine(LidRoutine(target, thenLock));
        }

        private IEnumerator LidRoutine(Quaternion target, bool thenLock)
        {
            _animating = true;
            var start = Lid.localRotation;
            var elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += UnityEngine.Time.deltaTime;
                Lid.localRotation = Quaternion.Slerp(start, target, elapsed / openDuration);
                yield return null;
            }

            Lid.localRotation = target;
            _animating = false;

            if (thenLock)
            {
                _locked = true;
                onLocked?.Invoke();
            }
        }
    }
}
