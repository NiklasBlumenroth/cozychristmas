using CozySanta.Core.Items;
using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Lässt ein aufnehmbares Item ruhen, sobald es sich nicht mehr bewegt (Apply-Schicht zu
    /// <see cref="SettleTimer"/>): solange der Rigidbody dynamisch ist, wird pro Physik-Schritt die
    /// Geschwindigkeit ausgewertet; bei Ruhe wird das Item eingefroren (<c>isKinematic = true</c>,
    /// schlafend) und dieser Controller deaktiviert – das ist entscheidend bei tausenden Büchern,
    /// weil ruhende Items dann keine FixedUpdate-Last mehr erzeugen. Der Collider bleibt aktiv, das
    /// Item bleibt also über den Fadenkreuz-Raycast aufhebbar.
    ///
    /// Aufwecken passiert von außen: <see cref="PlayerCarry"/> macht das Item beim Aufnehmen ohnehin
    /// kinematisch (Controller pausiert) und ruft beim Ablegen <see cref="BeginSettling"/> auf, womit
    /// es erneut dynamisch wird und nach dem Liegenbleiben wieder einschläft.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SettlingBody : MonoBehaviour
    {
        [Tooltip("Lineare Geschwindigkeit (m/s), unter der das Item als ruhig gilt.")]
        [SerializeField] private float linearThreshold = 0.05f;
        [Tooltip("Winkelgeschwindigkeit (rad/s), unter der das Item als ruhig gilt.")]
        [SerializeField] private float angularThreshold = 0.2f;
        [Tooltip("Dauer (s), die es ununterbrochen ruhig sein muss, bevor es einfriert. Nur relevant " +
                 "in der Authoring-Phase (geladene Bücher starten ohnehin eingefroren) – ruhig großzügig.")]
        [SerializeField] private float settleDuration = 3f;

        private Rigidbody _body;
        private SettleTimer _timer;

        private Rigidbody Body => _body != null ? _body : (_body = GetComponent<Rigidbody>());

        private void OnEnable() => _timer.Reset();

        private void FixedUpdate()
        {
            var body = Body;
            // Kinematisch = getragen oder bereits ruhend -> nichts zu tun.
            if (body.isKinematic) return;

            var lin = body.linearVelocity.magnitude;
            var ang = body.angularVelocity.magnitude;
            if (_timer.Tick(lin, ang, UnityEngine.Time.fixedDeltaTime, linearThreshold, angularThreshold, settleDuration))
            {
                EnterRest();
            }
        }

        /// <summary>Friert das Item in der aktuellen Pose ein (kinematisch, schlafend) und stoppt die
        /// Auswertung. Bleibt aufhebbar. Auch direkt nutzbar, um geladene Items sofort ruhen zu lassen.</summary>
        public void EnterRest()
        {
            var body = Body;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            enabled = false;
        }

        /// <summary>Weckt das Item wieder in die dynamische Simulation (z. B. nach dem Ablegen),
        /// sodass es erneut fallen und danach einschlafen kann.</summary>
        public void BeginSettling()
        {
            Body.isKinematic = false;
            _timer.Reset();
            enabled = true;
        }
    }
}
