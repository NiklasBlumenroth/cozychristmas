using CozySanta.Core.Progression;
using CozySanta.Runtime.Abilities;
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
    /// Die Stufen-Werte kommen aus der editierbaren <see cref="SkillTableConfig"/>. Bindet das
    /// editor-authored Skillmenü; erzeugt keine UI zur Laufzeit.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        // Lichtkegel ist kein Skill mehr (entfernt); fester Radius, der Kegel bleibt als Mechanik.
        private const float ConeRadius = 1.2f;

        [Header("Apply-Ziele")]
        [SerializeField] private PlayerCarry          carry;
        [SerializeField] private MeltController        melt;
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private MagicSortAbility      autoSort;   // Fähigkeit A
        [SerializeField] private MagicGatherAbility    gather;     // Fähigkeit B

        [Header("XP-Quellen (Beträge)")]
        [SerializeField] private int sortXp           = 50;
        [SerializeField] private int meltXpPerPercent = 5;

        [Header("Stufen-Tabelle (Balancing)")]
        [SerializeField] private SkillTableConfig skillTable;

        [Header("Menü")]
        [SerializeField] private SkillMenuView skillMenuView;

        private static readonly int SkillCount = System.Enum.GetValues(typeof(SkillId)).Length;

        private ProgressionState _state;
        private float _lastCoverage;

        public ProgressionState State => _state;

        private void Awake()
        {
            _state = new ProgressionState(BuildConfigs());

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

        /// <summary>Dev-/Test-Hilfe: bucht genau so viel XP, dass GENAU ein Level aufsteigt (= 1 Skillpunkt).</summary>
        public void LevelUp()
        {
            var l = _state.Ledger;
            var needed = l.XpForNextLevel - l.XpIntoLevel;
            AwardXp(needed > 0 ? needed : l.XpForNextLevel);
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

        private SkillConfig[] BuildConfigs()
            => skillTable != null ? skillTable.BuildConfigs() : FallbackConfigs();

        // Fallback, falls kein Asset zugewiesen ist (Startentwurf identisch zur SkillTableConfig).
        private static SkillConfig[] FallbackConfigs() => new[]
        {
            new SkillConfig(1.2f, 3.0f, 20),                      // LampPower
            new SkillConfig(12f,  32f,  20),                      // LampBattery
            new SkillConfig(5f,   25f,  20),                      // CarryCapacity
            new SkillConfig(3f,   6f,   20),                      // MoveSpeed
            new SkillConfig(10f,  4f,   20, true, true, 1f, 5f),  // ObjectPull (Heranholen)
            new SkillConfig(8f,   3f,   20, true, true, 1f, 5f),  // AutoSort (Auto-Einsortieren)
        };

        private void ApplySkills()
        {
            if (carry    != null) carry.Capacity   = _state.Skills.ValueOf(SkillId.CarryCapacity);
            if (movement != null) movement.MoveSpeed = _state.Skills.ValueOf(SkillId.MoveSpeed);
            if (melt != null)
            {
                melt.MeltStrength    = _state.Skills.ValueOf(SkillId.LampPower);
                melt.MeltRadius      = ConeRadius; // fester Lichtkegel
                melt.BatteryCapacity = _state.Skills.ValueOf(SkillId.LampBattery);
            }

            ApplyAbility(autoSort, SkillId.AutoSort);
            ApplyAbility(gather,   SkillId.ObjectPull);
        }

        private void ApplyAbility(MagicAbility ability, SkillId id)
        {
            if (ability == null) return;
            var skill = _state.Skills.Get(id);
            ability.Configure(skill.Charges, skill.Value, skill.IsUnlocked);
        }

        private void InitMenuView()
        {
            if (skillMenuView == null) return;
            for (var i = 0; i < SkillCount; i++)
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

            for (var i = 0; i < SkillCount; i++)
            {
                var entry = skillMenuView.GetEntry(i);
                if (entry == null) continue;
                var skill = _state.Skills.Get((SkillId)i);
                entry.SetLevel(skill.Level, skill.MaxLevel);
                entry.SetValue(FormatValue(skill));
                entry.SetUnlocked(skill.IsUnlocked);
                entry.SetInteractable(_state.AvailablePoints > 0 && skill.CanRaise);
            }
        }

        private static string FormatValue(Skill skill)
        {
            var v = skill.Value;
            if (skill.HasCharges) return $"{v:F1}s · {skill.Charges} Lad."; // Fähigkeit: Cooldown + Ladungen
            return skill.Id switch
            {
                SkillId.CarryCapacity => $"{v:F1} kg",
                SkillId.LampBattery   => $"{v:F0} s",
                SkillId.MoveSpeed     => $"{v:F1} m/s",
                _                     => $"{v:F2}x",
            };
        }
    }
}
