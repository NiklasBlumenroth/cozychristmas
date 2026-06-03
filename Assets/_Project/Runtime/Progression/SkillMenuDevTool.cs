using CozySanta.Core.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// IMGUI-Dev-Tool (analog DevSpawnMenu): zeigt Level/XP/Punkte, ermöglicht XP-Gutschrift
    /// und Skill-Investitionen ohne das editor-authored Menü. Taste F2 zum Ein-/Ausblenden.
    /// Kein authored Gameplay-UI – nur Testwerkzeug.
    /// </summary>
    public sealed class SkillMenuDevTool : MonoBehaviour
    {
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private int xpAmount = 100;

        private bool _visible;

        private static readonly string[] SkillLabels =
            { "LampPower", "LampCone", "LampBattery", "CarryCapacity", "MoveSpeed", "SortVision", "ObjectPull" };

        private void Update()
        {
            if (Keyboard.current?.f2Key.wasPressedThisFrame == true)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || progression == null) return;

            var state = progression.State;
            if (state == null) return;

            var rect  = new Rect(10, 10, 280, 360);
            GUI.Box(rect, "Skill-DevTool (F2)");

            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 24, rect.width - 16, rect.height - 32));

            var l = state.Ledger;
            GUILayout.Label($"Level {l.Level}   XP: {l.XpIntoLevel}/{l.XpForNextLevel}");
            GUILayout.Label($"Punkte verfügbar: {state.AvailablePoints}");

            GUILayout.Space(4);
            if (GUILayout.Button($"+{xpAmount} XP"))
            {
                state.AwardXp(xpAmount);
            }

            GUILayout.Space(6);
            for (var i = 0; i < SkillLabels.Length; i++)
            {
                var skill = state.Skills.Get((SkillId)i);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{SkillLabels[i]} [{skill.Level}/{skill.MaxLevel}]  {skill.Value:F2}", GUILayout.Width(210));
                if (GUILayout.Button("+", GUILayout.Width(30)))
                    progression.Invest((SkillId)i);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }
    }
}
