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
    /// Zwei Modi (Taste T schaltet um): „Fach" justiert die Einlage (Dreh/Größe/Höhe, live im Fach-Ghost),
    /// „Hand" justiert NUR die Hand-Drehung (<see cref="SortPlacementRotation.CarryEuler"/>, live am
    /// getragenen Item, kein Ghost nötig).
    ///
    /// Tasten (Schrittweite per Inspector, Shift = Feinschritt):
    ///   I / K = Pitch (X),  J / L = Yaw (Y),  U / O = Roll (Z)
    ///   , / . = kleiner / größer (Scale, nur Fach),  Bild↑ / Bild↓ = Höhe (Y-Offset, nur Fach)
    ///   T = Modus Fach/Hand,  P = Wert loggen.
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
        private bool _handMode;   // false = Fach (Einlage), true = Hand (CarryEuler)

        private void Awake()
        {
            if (carry == null) carry = FindAnyObjectByType<PlayerCarry>();
        }

        private void Update()
        {
            if (!TryGetCarriedOffset(out var rot)) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Modus umschalten (Fach <-> Hand).
            if (keyboard.tKey.wasPressedThisFrame)
            {
                _handMode = !_handMode;
                LogState(rot);
            }

            var shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            var changed = false;

            // Rotation: I/K = X, J/L = Y, U/O = Z (in beiden Modi).
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
                if (_handMode) rot.CarryEuler = Wrap(rot.CarryEuler + delta);
                else           rot.PlacedEuler = Wrap(rot.PlacedEuler + delta);
                changed = true;
            }

            // Größe + Höhe nur im Fach-Modus (Hand justiert ausschließlich die Drehung).
            if (!_handMode)
            {
                var sStep = shift ? fineScaleStep : scaleStep;
                if (keyboard.commaKey.wasPressedThisFrame) { rot.PlacedScale -= sStep; changed = true; }
                if (keyboard.periodKey.wasPressedThisFrame) { rot.PlacedScale += sStep; changed = true; }

                var oStep = shift ? fineOffsetStep : offsetStep;
                if (keyboard.pageUpKey.wasPressedThisFrame) { var o = rot.PlacedOffset; o.y += oStep; rot.PlacedOffset = o; changed = true; }
                if (keyboard.pageDownKey.wasPressedThisFrame) { var o = rot.PlacedOffset; o.y -= oStep; rot.PlacedOffset = o; changed = true; }
            }

            // Hand-Modus: Drehung sofort am getragenen Item sichtbar machen (RelayoutHands läuft sonst erst
            // bei der nächsten Stapeländerung). Das oberste Item liegt am Hand-Anker mit localRotation.
            if (_handMode && changed && carry.TryPeekTopComponent(out var topComp) && topComp != null)
            {
                topComp.transform.localRotation = rot.CarryOffset;
            }

            if (changed || keyboard.pKey.wasPressedThisFrame) LogState(rot);
        }

        private bool TryGetCarriedOffset(out SortPlacementRotation rot)
        {
            rot = null;
            if (carry == null || carry.CarriedCount == 0) return false;
            if (!carry.TryPeekTopComponent(out var top) || top == null) return false;
            return top.TryGetComponent(out rot);
        }

        private void LogState(SortPlacementRotation rot)
        {
            if (_handMode)
                Debug.Log($"[CarryAdjust] {rot.gameObject.name}: CarryEuler = {rot.CarryEuler}");
            else
                Debug.Log($"[PlacedAdjust] {rot.gameObject.name}: Euler = {rot.PlacedEuler}, " +
                          $"Scale = {rot.PlacedScale:0.###}, Offset = {rot.PlacedOffset}");
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

            string text;
            if (_handMode)
            {
                var c = rot.CarryEuler;
                text = $"[HAND] „{rot.gameObject.name}\":  Dreh X {c.x:0} Y {c.y:0} Z {c.z:0}\n" +
                       "I/K J/L U/O = drehen   T = Modus Fach   (Shift = fein, P = loggen)";
            }
            else
            {
                var e = rot.PlacedEuler;
                text = $"[FACH] „{rot.gameObject.name}\":  Dreh X {e.x:0} Y {e.y:0} Z {e.z:0}   " +
                       $"Größe {rot.PlacedScale:0.##}   Höhe {rot.PlacedOffset.y:0.###}\n" +
                       "I/K J/L U/O = drehen   ,/. = Größe   Bild↑/Bild↓ = Höhe   T = Modus Hand   (Shift = fein, P = loggen)";
            }
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
