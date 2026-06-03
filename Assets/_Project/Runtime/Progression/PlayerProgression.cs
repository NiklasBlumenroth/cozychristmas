using CozySanta.Core.Progression;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Player;
using CozySanta.Runtime.Snow;
using CozySanta.Runtime.Sorting;
using UnityEngine;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// Apply-Schicht des XP-/Skillsystems. Hält den <see cref="ProgressionState"/>, verdrahtet
    /// XP-Quellen (F4-onCompleted, F5-Coverage-Delta) und überträgt Skillwerte auf die Ziel-Komponenten.
    /// Bindet das editor-authored Skillmenü; erzeugt keine UI zur Laufzeit.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        [Header("Apply-Ziele")]
        [SerializeField] private PlayerCarry        carry;
        [SerializeField] private MeltController     melt;
        [SerializeField] private FirstPersonController movement;

        [Header("XP-Quellen (Beträge)")]
        [SerializeField] private int sortXp          = 50;
        [SerializeField] private int meltXpPerPercent = 5;

        [Header("Menü")]
        [SerializeField] private SkillMenuView skillMenuView;

        // Platzhalterwerte für alle 7 Skills – Balancing folgt in F14 (Konzept 08)
        private static readonly SkillConfig[] DefaultConfigs =
        {
            new SkillConfig(1.2f,  0.09f, 3.0f,  20),        // LampPower
            new SkillConfig(0.8f,  0.05f, 1.8f,  20),        // LampCone
            new SkillConfig(12f,   1.0f,  32f,   20),        // LampBattery
            new SkillConfig(5f,    1.0f,  25f,   20),        // CarryCapacity
            new SkillConfig(3f,    0.15f, 6f,    20),        // MoveSpeed
            new SkillConfig(0f,    1f,    20f,   20, true),  // SortVision (F9)
            new SkillConfig(0f,    1f,    20f,   20, true),  // ObjectPull (F10)
        };

        private ProgressionState _state;
        private float _lastCoverage;

        public ProgressionState State => _state;

        private void Awake()
        {
            _state = new ProgressionState(DefaultConfigs);

            var sortTargets = Object.FindObjectsByType<SortTargetInteractable>(FindObjectsSortMode.None);
            foreach (var st in sortTargets)
                st.AddCompletionListener(AwardSortXp);
        }

        private void Start()
        {
            if (melt != null) _lastCoverage = melt.Coverage;
            ApplySkills();
            InitMenuView();
            RefreshView();
        }

        private void Update()
        {
            if (melt == null) return;
            var current = melt.Coverage;
            var delta   = current - _lastCoverage;
            if (delta > 0.001f)
            {
                _state.AwardXp(Mathf.RoundToInt(delta * 100f * meltXpPerPercent));
                RefreshView();
            }
            _lastCoverage = current;
        }

        /// <summary>Bucht einen beliebigen XP-Betrag (Andockpunkt für Area-Abschluss, F7).</summary>
        public void AwardXp(int amount)
        {
            _state.AwardXp(amount);
            RefreshView();
        }

        public void AwardSortXp()
        {
            _state.AwardXp(sortXp);
            RefreshView();
        }

        public void Invest(SkillId id)
        {
            if (!_state.Invest(id)) return;
            ApplySkills();
            RefreshView();
        }

        private void ApplySkills()
        {
            if (carry    != null) carry.Capacity          = _state.Skills.ValueOf(SkillId.CarryCapacity);
            if (movement != null) movement.MoveSpeed       = _state.Skills.ValueOf(SkillId.MoveSpeed);
            if (melt     == null) return;
            melt.MeltStrength    = _state.Skills.ValueOf(SkillId.LampPower);
            melt.MeltRadius      = _state.Skills.ValueOf(SkillId.LampCone);
            melt.BatteryCapacity = _state.Skills.ValueOf(SkillId.LampBattery);
        }

        private void InitMenuView()
        {
            if (skillMenuView == null) return;
            // Close-Button: SkillMenuView.Awake() hat bereits Hide() verdrahtet
            for (var i = 0; i < 7; i++)
            {
                var entry = skillMenuView.GetEntry(i);
                if (entry == null) continue;
                var id = (SkillId)i;
                entry.SetOnInvest(() => Invest(id));
            }
        }

        private void RefreshView()
        {
            if (skillMenuView == null) return;
            var l = _state.Ledger;
            skillMenuView.SetLevel(l.Level);
            skillMenuView.SetXp(l.XpIntoLevel, l.XpForNextLevel);
            skillMenuView.SetAvailablePoints(_state.AvailablePoints);

            for (var i = 0; i < 7; i++)
            {
                var entry = skillMenuView.GetEntry(i);
                if (entry == null) continue;
                var skill = _state.Skills.Get((SkillId)i);
                entry.SetLevel(skill.Level, skill.MaxLevel);
                entry.SetValue(FormatValue((SkillId)i, skill.Value));
                entry.SetUnlocked(skill.IsUnlocked);
                entry.SetInteractable(_state.AvailablePoints > 0 && skill.CanRaise);
            }
        }

        private static string FormatValue(SkillId id, float v) => id switch
        {
            SkillId.CarryCapacity => $"{v:F1} kg",
            SkillId.LampBattery   => $"{v:F0} s",
            SkillId.MoveSpeed     => $"{v:F1} m/s",
            _                     => $"{v:F2}x",
        };
    }
}
