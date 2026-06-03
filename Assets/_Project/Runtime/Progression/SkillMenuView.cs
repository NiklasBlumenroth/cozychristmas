using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// View-Komponente des editor-authored Skillmenü-Panels.
    /// PlayerProgression hält eine Referenz und ruft Set*-Methoden auf; kein UI wird erzeugt.
    /// skillEntries[i] entspricht SkillId-Enum-Wert i (0=LampPower … 6=ObjectPull).
    /// </summary>
    public sealed class SkillMenuView : MonoBehaviour
    {
        [Header("Status-Header")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider xpBar;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text availablePointsText;

        [Header("Schließen")]
        [SerializeField] private Button closeButton;

        [Header("Skill-Einträge (Reihenfolge = SkillId-Enum: LampPower…ObjectPull)")]
        [SerializeField] private SkillEntryUI[] skillEntries = new SkillEntryUI[7];

        public Button CloseButton => closeButton;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        public void SetLevel(int level)
        {
            if (levelText != null) levelText.text = $"Level {level}";
        }

        public void SetXp(int currentXp, int xpForNext)
        {
            if (xpText != null) xpText.text = $"{currentXp} / {xpForNext} XP";
            if (xpBar != null) xpBar.value = xpForNext > 0 ? (float)currentXp / xpForNext : 0f;
        }

        public void SetAvailablePoints(int points)
        {
            if (availablePointsText != null)
                availablePointsText.text = $"Skillpunkte: {points}";
        }

        /// <summary>Gibt den Eintrag für SkillId-Index i zurück (0-basiert, entspricht SkillId-Enum).</summary>
        public SkillEntryUI GetEntry(int skillIdIndex)
        {
            if (skillIdIndex < 0 || skillIdIndex >= skillEntries.Length) return null;
            return skillEntries[skillIdIndex];
        }
    }
}
