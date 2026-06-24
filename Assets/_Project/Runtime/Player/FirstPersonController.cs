using CozySanta.Core.Player;
using UnityEngine;
using SN = System.Numerics;

namespace CozySanta.Runtime.Player
{
    /// <summary>
    /// First-Person-Bewegung und -Blick. Liest Eingaben (über das Input System gesetzt), berechnet
    /// Bewegung/Blick über die reine Core-Mathematik und wendet die Seiteneffekte auf
    /// <see cref="CharacterController"/> (Bewegung + Schwerkraft), Körper (Yaw) und Kamera (Pitch) an.
    /// Eingaben werden per Setter injiziert – so bleibt die Komponente testbar und vom konkreten
    /// Input-Binding entkoppelt (z. B. PlayerInput-UnityEvents → SetMoveInput/SetLookInput).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 1.1f;

        [Header("Hocke (Shift halten)")]
        [Tooltip("Körperhöhe in der Hocke (Meter).")]
        [SerializeField] private float crouchHeight = 1.1f;
        [Tooltip("Übergangsgeschwindigkeit Stehen↔Hocke (Höhe pro Sekunde).")]
        [SerializeField] private float crouchSpeed = 8f;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;
        private bool _jumpRequested;
        private bool _crouchHeld;
        private float _standHeight;
        private float _standCenterY;
        private float _standEyeHeight;
        private Vector2 _moveInput;
        private Vector2 _lookInput;

        public void SetMoveInput(Vector2 value) => _moveInput = value;
        public void SetLookInput(Vector2 value) => _lookInput = value;

        /// <summary>Hocke ein/aus (pro Frame): true, solange die Hocktaste gehalten wird.</summary>
        public void SetCrouch(bool held) => _crouchHeld = held;

        /// <summary>Fordert einen Sprung an; wird im nächsten <see cref="ApplyMove"/> verbraucht.</summary>
        public void RequestJump() => _jumpRequested = true;

        /// <summary>Setzt die vertikale Geschwindigkeit zurück (z. B. nach einem Teleport, damit der
        /// Spieler nicht mit aufgesammeltem Fall-Impuls am Ziel ankommt).</summary>
        public void ResetVerticalVelocity() => _verticalVelocity = 0f;

        /// <summary>Laufgeschwindigkeit (m/s). Andockpunkt für MoveSpeed-Upgrade (F6).</summary>
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // Steh-Referenzwerte merken (Hocke fährt relativ dazu, Füße bleiben am Boden).
            _standHeight = _controller.height;
            _standCenterY = _controller.center.y;
            _standEyeHeight = cameraPivot != null ? cameraPivot.localPosition.y : _standHeight;
        }

        private void OnEnable()
        {
            // Mauszeiger in die Bildmitte sperren und ausblenden – die Sicht folgt 1:1 der Maus.
            // (Im Editor löst Esc die Sperre; zum Wiedersperren Play kurz aus/an oder ins Game-View klicken.)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            ApplyLook();
            ApplyCrouch(UnityEngine.Time.deltaTime);
            ApplyMove(UnityEngine.Time.deltaTime);

            // Maus-Delta ist ein Pro-Frame-Wert: nach dem Anwenden zurücksetzen, damit die Sicht
            // nicht weiterdreht, wenn die Maus stillsteht. Bewegung (gehaltene Achse) bleibt erhalten.
            _lookInput = Vector2.zero;
        }

        private void ApplyLook()
        {
            // Nur umschauen, wenn der Cursor gesperrt ist. Gibt ein Menü/Overlay den Cursor frei
            // (z. B. ESC im Item-Inventar), pausiert der Maus-Blick, statt hinter der UI weiterzudrehen.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            // Yaw dreht den Körper, Pitch (begrenzt) die Kamera.
            transform.Rotate(Vector3.up, _lookInput.x * lookSensitivity, Space.Self);
            _pitch = LookMath.ClampPitch(_pitch, -_lookInput.y * lookSensitivity, minPitch, maxPitch);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void ApplyCrouch(float deltaTime)
        {
            var height = CrouchMotion.StepHeight(
                _controller.height, _standHeight, crouchHeight, _crouchHeld, crouchSpeed, deltaTime);
            if (Mathf.Approximately(height, _controller.height))
            {
                return; // keine Änderung -> Controller/Kamera nicht unnötig schreiben
            }

            _controller.height = height;
            var center = _controller.center;
            center.y = CrouchMotion.CenterY(_standCenterY, _standHeight, height);
            _controller.center = center;

            if (cameraPivot != null)
            {
                var eye = cameraPivot.localPosition;
                eye.y = CrouchMotion.EyeHeight(height, _standHeight, _standEyeHeight);
                cameraPivot.localPosition = eye;
            }
        }

        private void ApplyMove(float deltaTime)
        {
            var local = MovementCalculator.ComputeLocalVelocity(
                new SN.Vector2(_moveInput.x, _moveInput.y), moveSpeed);
            var world = (transform.right * local.X) + (transform.forward * local.Y);

            var jumpVelocity = JumpCalculator.ComputeJumpVelocity(jumpHeight, gravity);
            _verticalVelocity = JumpCalculator.StepVerticalVelocity(
                _verticalVelocity, _controller.isGrounded, _jumpRequested,
                jumpVelocity, gravity, deltaTime);
            _jumpRequested = false;

            var displacement = (world * deltaTime) + (Vector3.up * (_verticalVelocity * deltaTime));
            _controller.Move(displacement);
        }
    }
}
