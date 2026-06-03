using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// View-Komponente des editor-authored Area-HUD-Panels (oben rechts).
    /// Laufzeitcode bindet/aktualisiert nur; kein UI wird erzeugt.
    /// </summary>
    public sealed class AreaHudView : MonoBehaviour
    {
        [Header("Area")]
        [SerializeField] private TMP_Text areaNameText;
        [SerializeField] private TaskEntryUI[] taskEntries = new TaskEntryUI[0];

        [Header("Akku")]
        [SerializeField] private Slider batteryBar;

        [Header("Ladestation")]
        [SerializeField] private GameObject chargeSection;
        [SerializeField] private Slider chargeBar;

        [Header("XP / Level")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider xpBar;

        private void Awake()
        {
            if (chargeSection != null) chargeSection.SetActive(false);
        }

        public void SetAreaName(string name)
        {
            if (areaNameText != null) areaNameText.text = name;
        }

        public void SetBattery(float fraction)
        {
            if (batteryBar != null) batteryBar.value = fraction;
        }

        public void SetChargeBarVisible(bool visible)
        {
            if (chargeSection != null) chargeSection.SetActive(visible);
        }

        public void SetChargeProgress(float fraction)
        {
            if (chargeBar != null) chargeBar.value = fraction;
        }

        public void SetLevel(int level)
        {
            if (levelText != null) levelText.text = $"Level {level}";
        }

        public void SetXp(int currentXp, int xpForNext)
        {
            if (xpBar != null) xpBar.value = xpForNext > 0 ? (float)currentXp / xpForNext : 0f;
        }

        public TaskEntryUI GetEntry(int index)
            => index >= 0 && index < taskEntries.Length ? taskEntries[index] : null;

        public int EntryCount => taskEntries.Length;
    }
}
