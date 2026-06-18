using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Authoring-/Debug-Helfer zum Einstellen des Einlage-Dreh-Offsets (<see cref="SortPlacementRotation"/>)
    /// des aktuell GETRAGENEN Items – live im Fach-Ghost sichtbar, ohne das bewegte Objekt in der Hierarchy
    /// selektieren zu müssen. Tasten verstellen den Euler-Offset in Schritten; der aktuelle Wert wird oben
    /// eingeblendet, sodass man ihn ablesen und ins Prefab/Setup übernehmen kann.
    ///
    /// Tasten (Großschreibung-Taste = +, gepaart = −), Schrittweite per Inspector, Shift = Feinschritt:
    ///   I / K = Pitch (X),  J / L = Yaw (Y),  U / O = Roll (Z),  P = Wert in die Console loggen.
    /// </summary>
    public sealed class SortPlacementRotationDevTool : MonoBehaviour
    {
        [Tooltip("Trag-System; leer = automatische Suche beim Start.")]
        [SerializeField] private PlayerCarry carry;
        [Tooltip("Schrittweite je Tastendruck (Grad).")]
        [SerializeField] private float stepDegrees = 15f;
        [Tooltip("Feinschrittweite, solange Shift gehalten wird (Grad).")]
        [SerializeField] private float fineStepDegrees = 5f;
        [Tooltip("Schriftgröße der Einblendung.")]
        [SerializeField] private int fontSize = 16;

        private GUIStyle _style;

        private void Awake()
        {
            if (carry == null) carry = FindAnyObjectByType<PlayerCarry>();
        }

        private void Update()
        {
            if (!TryGetCarriedOffset(out var rot)) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var step = (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                ? fineStepDegrees
                : stepDegrees;

            var delta = Vector3.zero;
            if (keyboard.iKey.wasPressedThisFrame) delta.x += step;
            if (keyboard.kKey.wasPressedThisFrame) delta.x -= step;
            if (keyboard.jKey.wasPressedThisFrame) delta.y += step;
            if (keyboard.lKey.wasPressedThisFrame) delta.y -= step;
            if (keyboard.uKey.wasPressedThisFrame) delta.z += step;
            if (keyboard.oKey.wasPressedThisFrame) delta.z -= step;

            if (delta != Vector3.zero)
            {
                rot.PlacedEuler = Wrap(rot.PlacedEuler + delta);
                Debug.Log($"[PlacedRotation] {rot.gameObject.name}: PlacedEuler = {rot.PlacedEuler}");
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                Debug.Log($"[PlacedRotation] {rot.gameObject.name}: PlacedEuler = {rot.PlacedEuler}");
            }
        }

        private bool TryGetCarriedOffset(out SortPlacementRotation rot)
        {
            rot = null;
            if (carry == null || carry.CarriedCount == 0) return false;
            if (!carry.TryPeekTopComponent(out var top) || top == null) return false;
            return top.TryGetComponent(out rot);
        }

        private void OnGUI()
        {
            if (!TryGetCarriedOffset(out var rot)) return;

            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter
                };
                _style.normal.textColor = Color.white;
            }

            var e = rot.PlacedEuler;
            var text = $"Einlage-Drehung „{rot.gameObject.name}\":  X {e.x:0}   Y {e.y:0}   Z {e.z:0}\n" +
                       "I/K = X   J/L = Y   U/O = Z   (Shift = fein, P = loggen)";
            GUI.Label(new Rect(0f, 8f, Screen.width, 60f), text, _style);
        }

        private static Vector3 Wrap(Vector3 e) => new Vector3(WrapAngle(e.x), WrapAngle(e.y), WrapAngle(e.z));

        private static float WrapAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }
    }
}
