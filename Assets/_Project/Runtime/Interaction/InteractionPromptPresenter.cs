using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Bindet den Fokuszustand an ein vorab im Editor erstelltes Hinweis-Objekt. Der Code blendet
    /// es nur ein/aus – er erzeugt KEIN UI zur Laufzeit (Constitution V / FR-008).
    /// </summary>
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject promptRoot;

        /// <summary>Sichtbarkeitszustand – auch ohne zugewiesenes UI testbar.</summary>
        public bool IsShown { get; private set; }

        /// <summary>Zuletzt angeforderter Hinweistext (für optionale spätere Textbindung).</summary>
        public string CurrentText { get; private set; }

        public void Show(string text)
        {
            CurrentText = text;
            IsShown = true;
            if (promptRoot != null && !promptRoot.activeSelf)
            {
                promptRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            IsShown = false;
            if (promptRoot != null && promptRoot.activeSelf)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}
