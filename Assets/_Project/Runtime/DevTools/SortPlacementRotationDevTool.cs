using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Authoring-/Debug-Helfer zum Einstellen der Einlage-Justage (<see cref="SortPlacementRotation"/>) des
    /// aktuell GETRAGENEN Items – live im Fach-Ghost sichtbar, ohne das bewegte Objekt in der Hierarchy
    /// selektieren zu müssen. Verstellt Dreh-Offset, Größenfaktor und Höhen-/Positions-Offset; der aktuelle
    /// Stand wird oben eingeblendet, sodass man ihn ablesen und ins Prefab übernehmen kann.
    ///
    /// Tasten (Schrittweite per Inspector, Shift = Feinschritt):
    ///   I / K = Pitch (X),  J / L = Yaw (Y),  U / O = Roll (Z)
    ///   , / . = kleiner / größer (Scale),  Bild↑ / Bild↓ = Höhe (Y-Offset),  P = Wert loggen.
    /// </summary>
    public sealed class SortPlacementRotationDevTool : MonoBehaviour
    {
        [Tooltip("Trag-System; leer = automatische Suche beim Start.")]
        [SerializeField] private PlayerCarry carry;

        [Header("Rotation (Grad)")]
        [Tooltip("Schrittweite je Tastendruck.")]
        [SerializeField] private float stepDegrees = 15f;
        [Tooltip("Feinschritt, solange Shift gehalten wird.")]
        [SerializeField] private float fineStepDegrees = 5f;

        [Header("Größe & Höhe")]
        [Tooltip("Schrittweite des Größenfaktors je Tastendruck.")]
        [SerializeField] private float scaleStep = 0.05f;
        [Tooltip("Feinschritt des Größenfaktors (Shift).")]
        [SerializeField] private float fineScaleStep = 0.01f;
        [Tooltip("Schrittweite des Höhen-/Positions-Offsets je Tastendruck (Meter).")]
        [SerializeField] private float offsetStep = 0.01f;
        [Tooltip("Feinschritt des Offsets (Shift, Meter).")]
        [SerializeField] private float fineOffsetStep = 0.002f;

        [Header("Anzeige")]
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

            var shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            var changed = false;

            // Rotation: I/K = X, J/L = Y, U/O = Z.
            var rStep = shift ? fineStepDegrees : stepDegrees;
            var delta = Vector3.zero;
            if (keyboard.iKey.wasPressedThisFrame) delta.x += rStep;
            if (keyboard.kKey.wasPressedThisFrame) delta.x -= rStep;
            if (keyboard.jKey.wasPressedThisFrame) delta.y += rStep;
            if (keyboard.lKey.wasPressedThisFrame) delta.y -= rStep;
            if (keyboard.uKey.wasPressedThisFrame) delta.z += rStep;
            if (keyboard.oKey.wasPressedThisFrame) delta.z -= rStep;
            if (delta != Vector3.zero)
            {
                rot.PlacedEuler = Wrap(rot.PlacedEuler + delta);
                changed = true;
            }

            // Größe: , = kleiner, . = größer.
            var sStep = shift ? fineScaleStep : scaleStep;
            if (keyboard.commaKey.wasPressedThisFrame) { rot.PlacedScale -= sStep; changed = true; }
            if (keyboard.periodKey.wasPressedThisFrame) { rot.PlacedScale += sStep; changed = true; }

            // Höhe (Y-Offset): Bild↑ = hoch, Bild↓ = runter.
            var oStep = shift ? fineOffsetStep : offsetStep;
            if (keyboard.pageUpKey.wasPressedThisFrame) { var o = rot.PlacedOffset; o.y += oStep; rot.PlacedOffset = o; changed = true; }
            if (keyboard.pageDownKey.wasPressedThisFrame) { var o = rot.PlacedOffset; o.y -= oStep; rot.PlacedOffset = o; changed = true; }

            if (changed || keyboard.pKey.wasPressedThisFrame) LogState(rot);
        }

        private bool TryGetCarriedOffset(out SortPlacementRotation rot)
        {
            rot = null;
            if (carry == null || carry.CarriedCount == 0) return false;
            if (!carry.TryPeekTopComponent(out var top) || top == null) return false;
            return top.TryGetComponent(out rot);
        }

        private static void LogState(SortPlacementRotation rot)
            => Debug.Log($"[PlacedAdjust] {rot.gameObject.name}: Euler = {rot.PlacedEuler}, " +
                         $"Scale = {rot.PlacedScale:0.###}, Offset = {rot.PlacedOffset}");

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
            var text = $"Einlage „{rot.gameObject.name}\":  Dreh X {e.x:0} Y {e.y:0} Z {e.z:0}   " +
                       $"Größe {rot.PlacedScale:0.##}   Höhe {rot.PlacedOffset.y:0.###}\n" +
                       "I/K J/L U/O = drehen   ,/. = Größe   Bild↑/Bild↓ = Höhe   (Shift = fein, P = loggen)";
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
