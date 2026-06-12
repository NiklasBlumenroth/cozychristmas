using CozySanta.Core.Areas;
using UnityEngine;

namespace CozySanta.Runtime.Areas
{
    /// <summary>
    /// Schaltet Szenen-Bereiche exklusiv (Apply): genau ein Bereichs-Root ist aktiv, alle anderen sind
    /// per <c>SetActive(false)</c> aus (kein Rendern/Physik/Update). Liegt auf dem GameManager. Die
    /// Auswahl löst der <see cref="Teleport.TeleportRouter"/> beim Teleport aus.
    ///
    /// Wichtig: Player, GameManager, HUD und die Teleport-Objekte (Trigger/Ziele) liegen AUSSERHALB
    /// der Bereichs-Roots, damit sie nicht mitdeaktiviert werden.
    /// </summary>
    public sealed class AreaActivator : MonoBehaviour
    {
        [Tooltip("Alle Bereichs-Roots (z. B. Außenwelt, BibliothekInne). Immer genau einer ist aktiv.")]
        [SerializeField] private GameObject[] areaRoots = new GameObject[0];
        [Tooltip("Beim Spielstart aktiver Bereich (z. B. Außenwelt). Leer = erster Eintrag.")]
        [SerializeField] private GameObject startActiveArea;

        private void Start()
        {
            var index = startActiveArea != null ? IndexOf(startActiveArea) : (areaRoots.Length > 0 ? 0 : -1);
            if (index < 0 && startActiveArea != null)
            {
                Debug.LogWarning($"[Area] Start-Bereich '{startActiveArea.name}' ist nicht in areaRoots gelistet.", this);
                index = areaRoots.Length > 0 ? 0 : -1;
            }

            ApplyActive(index);
        }

        /// <summary>Aktiviert den Bereich <paramref name="root"/> und deaktiviert alle anderen.
        /// Unbekannter/null-Root → keine Änderung (verhindert versehentliches Komplett-Abschalten).</summary>
        public void Activate(GameObject root)
        {
            var index = IndexOf(root);
            if (index < 0)
            {
                Debug.LogWarning($"[Area] Unbekannter Bereich '{(root != null ? root.name : "null")}' – ignoriert.", this);
                return;
            }

            ApplyActive(index);
        }

        private void ApplyActive(int activeIndex)
        {
            var flags = AreaActivation.Resolve(areaRoots.Length, activeIndex);
            for (var i = 0; i < areaRoots.Length; i++)
            {
                if (areaRoots[i] != null)
                {
                    areaRoots[i].SetActive(flags[i]);
                }
            }
        }

        private int IndexOf(GameObject root)
        {
            if (root == null)
            {
                return -1;
            }

            for (var i = 0; i < areaRoots.Length; i++)
            {
                if (areaRoots[i] == root)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
