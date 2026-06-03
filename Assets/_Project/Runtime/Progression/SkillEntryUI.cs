using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// View-Komponente für eine einzelne Skill-Zeile im editor-authored Skillmenü-Prefab.
    /// Laufzeitcode (PlayerProgression) ruft die Set*-Methoden auf; kein UI wird erzeugt.
    /// Reihenfolge im SkillMenuView-Array entspricht dem SkillId-Enum (0=LampPower … 6=ObjectPull).
    /// </summary>
    public sealed class SkillEntryUI : MonoBehaviour
    {
        [Header("Texte")]
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private TMP_Text skillLevelText;
        [SerializeField] private TMP_Text skillValueText;

        [Header("Freischalt-Badge (nur SortVision & ObjectPull)")]
        [Tooltip("GameObject mit 'Freigeschaltet'-Label; standardmäßig inaktiv.")]
        [SerializeField] private GameObject unlockBadge;

        [Header("Investitions-Button")]
        [SerializeField] private Button investButton;

        public void SetName(string displayName)
        {
            if (skillNameText != null) skillNameText.text = displayName;
        }

        public void SetLevel(int current, int max)
        {
            if (skillLevelText != null) skillLevelText.text = $"{current} / {max}";
        }

        public void SetValue(string valueDisplay)
        {
            if (skillValueText != null) skillValueText.text = valueDisplay;
        }

        /// <param name="unlocked">true = Badge einblenden (bei Freischalt-Skills).</param>
        public void SetUnlocked(bool unlocked)
        {
            if (unlockBadge != null) unlockBadge.SetActive(unlocked);
        }

        /// <param name="canInvest">false = Button ausgegraut (keine Punkte oder Max-Stufe).</param>
        public void SetInteractable(bool canInvest)
        {
            if (investButton != null) investButton.interactable = canInvest;
        }

        /// <summary>Registriert den Invest-Callback; ersetzt vorherige Listener.</summary>
        public void SetOnInvest(UnityAction callback)
        {
            if (investButton == null) return;
            investButton.onClick.RemoveAllListeners();
            investButton.onClick.AddListener(callback);
        }
    }
}
