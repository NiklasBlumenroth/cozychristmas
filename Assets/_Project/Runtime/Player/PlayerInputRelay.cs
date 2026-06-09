using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Interaction;
using CozySanta.Runtime.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.Player
{
    /// <summary>
    /// Input-Adapter: bindet die Input-System-Actions der Map „Player" an die entkoppelten Setter
    /// des Controllers. Auf dasselbe GameObject wie eine <c>PlayerInput</c>-Komponente (Behavior
    /// „Send Messages") setzen; die Action-Namen Move/Look/Interact werden per Konvention
    /// (OnMove/OnLook/OnInteract) aufgerufen. Ablegen wird im Prototyp direkt über Taste „Q" gelesen
    /// (formale Drop-Action folgt mit der finalen Tastenbelegung).
    /// </summary>
    public sealed class PlayerInputRelay : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;
        [SerializeField] private PlayerInteractionController interaction;
        [SerializeField] private PlayerCarry carry;

        [Header("Skill-Menü (optional)")]
        [SerializeField] private SkillMenuView skillMenu;

        [Header("Area-HUD (optional)")]
        [SerializeField] private AreaHudView areaHud;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<FirstPersonController>();
            if (interaction == null) interaction = GetComponent<PlayerInteractionController>();
            if (carry == null) carry = GetComponent<PlayerCarry>();
        }

        private void Update()
        {
            // Interaktion über die Maus: links = aufnehmen / aus Fach entnehmen, rechts = NUR
            // einsortieren (in ein fokussiertes Fach). Ablegen läuft über Taste „Q", damit Briefe
            // nicht versehentlich neben dem Fach fallen gelassen werden.
            var mouse = Mouse.current;
            if (mouse != null && interaction != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    interaction.TryTake();

                if (mouse.rightButton.wasPressedThisFrame)
                    interaction.TryPlace();
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame && interaction != null)
                    interaction.TryDrop();

                if (keyboard.spaceKey.wasPressedThisFrame && controller != null)
                    controller.RequestJump();

                if (keyboard.xKey.wasPressedThisFrame && skillMenu != null)
                    ToggleSkillMenu();
            }

            HandleCharging();
        }

        private void HandleCharging()
        {
            var station  = interaction?.FocusedInteractable as LadeStation;
            var charging = station != null && Mouse.current?.rightButton.isPressed == true;
            if (charging) station.ChargeTick(UnityEngine.Time.deltaTime);
        }

        private void ToggleSkillMenu()
        {
            if (skillMenu.IsVisible) skillMenu.Hide();
            else                     skillMenu.Show();
        }

        public void OnMove(InputValue value)
        {
            if (controller != null)
                controller.SetMoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            // Kein Look wenn Menü offen – sonst dreht sich die Kamera hinter dem Menü weiter.
            if (skillMenu != null && skillMenu.IsVisible) return;
            if (controller != null)
                controller.SetLookInput(value.Get<Vector2>());
        }

        // Frühere „Interact"-Action (E): bewusst wirkungslos – Aufnehmen/Ablegen läuft jetzt über die
        // Maus (siehe Update). Methode bleibt, damit PlayerInput (Send Messages) nichts vermisst.
        public void OnInteract(InputValue value)
        {
        }
    }
}
