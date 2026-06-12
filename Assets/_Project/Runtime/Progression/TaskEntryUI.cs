using CozySanta.Core.Progression;
using TMPro;
using UnityEngine;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// View-Komponente für eine einzelne Aufgabenzeile im editor-authored Area-HUD-Prefab.
    /// Laufzeitcode ruft <see cref="SetTask"/> auf; kein UI wird erzeugt.
    /// </summary>
    public sealed class TaskEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text taskNameText;
        [SerializeField] private TMP_Text progressText;

        public void SetTask(string description, float current, float required, TaskType type)
        {
            if (taskNameText != null) taskNameText.text = description;
            if (progressText != null) progressText.text = TaskProgressFormatter.Format(current, required, type);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
