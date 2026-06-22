using System;
using CozySanta.Core.Carry;
using UnityEngine;

namespace CozySanta.Runtime.Carry
{
    /// <summary>
    /// Treibt ein Item per kurzer Flugbewegung an seine Zielpose (Apply zur Core-<see cref="FlightProgress"/>).
    /// Der Logikzustand (Tragstapel / Fach) wird vom Aufrufer SOFORT geändert; nur die Optik holt hier auf,
    /// daher dürfen beliebig viele Items gleichzeitig fliegen (unterbrechbar/Queue). Zwei Modi:
    /// <list type="bullet">
    /// <item>Anker (Aufnehmen/Restack): parentet sofort an den Ziel-Anker (abbruchsicher) und blendet den
    ///   lokalen Offset auf die Zielpose ein – folgt der laufenden Hand automatisch, collider-los.</item>
    /// <item>Welt (Sortieren/Ablegen): feste Weltpose; beim Ablegen mit <see cref="Rigidbody.SweepTest"/>,
    ///   damit das Item nicht durch Wände fliegt, sondern am Hindernis fallen gelassen wird.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarriedItemFlight : MonoBehaviour
    {
        private enum Mode { None, Anchor, World }

        private Mode _mode = Mode.None;
        private FlightProgress _progress;

        // Anker-Modus (lokal, folgt dem Anker).
        private Vector3 _startLocalPos, _targetLocalPos;
        private Quaternion _startLocalRot, _targetLocalRot;

        // Welt-Modus (feste Zielpose).
        private Vector3 _startWorldPos, _targetWorldPos;
        private Quaternion _startWorldRot, _targetWorldRot;
        private bool _sweep;
        private Rigidbody _body;
        private Action _onLanded;

        /// <summary>Holt die Flug-Komponente am Item oder hängt sie an.</summary>
        public static CarriedItemFlight For(Component item)
        {
            return item.TryGetComponent<CarriedItemFlight>(out var flight)
                ? flight
                : item.gameObject.AddComponent<CarriedItemFlight>();
        }

        /// <summary>Collider-loser Flug an einen (ggf. bewegten) Anker: parentet sofort und blendet den
        /// lokalen Offset auf <paramref name="localPos"/>/<paramref name="localRot"/> ein.</summary>
        public void BeginToAnchor(Transform anchor, Vector3 localPos, Quaternion localRot, float duration)
        {
            transform.SetParent(anchor, worldPositionStays: true);
            _startLocalPos = transform.localPosition;
            _startLocalRot = transform.localRotation;
            _targetLocalPos = localPos;
            _targetLocalRot = localRot;
            _progress = new FlightProgress(duration);
            _mode = Mode.Anchor;
        }

        /// <summary>Flug an eine feste Weltpose. <paramref name="sweep"/>=true (Ablegen) prüft die Bahn per
        /// <see cref="Rigidbody.SweepTest"/> und ruft <paramref name="onLanded"/> bei Hindernis ODER Ankunft;
        /// sonst (Sortieren) nur bei Ankunft.</summary>
        public void BeginToWorld(Vector3 pos, Quaternion rot, float duration, bool sweep, Action onLanded)
        {
            _startWorldPos = transform.position;
            _startWorldRot = transform.rotation;
            _targetWorldPos = pos;
            _targetWorldRot = rot;
            _sweep = sweep;
            _onLanded = onLanded;
            _body = sweep ? GetComponent<Rigidbody>() : null;
            _progress = new FlightProgress(duration);
            _mode = Mode.World;
        }

        private void Update()
        {
            if (_mode == Mode.Anchor)
            {
                StepAnchor();
            }
            else if (_mode == Mode.World)
            {
                StepWorld();
            }
        }

        private void StepAnchor()
        {
            _progress.Step(UnityEngine.Time.deltaTime);
            var t = _progress.Eased;
            transform.localPosition = Vector3.Lerp(_startLocalPos, _targetLocalPos, t);
            transform.localRotation = Quaternion.Slerp(_startLocalRot, _targetLocalRot, t);
            if (_progress.IsDone)
            {
                transform.localPosition = _targetLocalPos;
                transform.localRotation = _targetLocalRot;
                _mode = Mode.None;
            }
        }

        private void StepWorld()
        {
            _progress.Step(UnityEngine.Time.deltaTime);
            var t = _progress.Eased;
            var nextPos = Vector3.Lerp(_startWorldPos, _targetWorldPos, t);
            var nextRot = Quaternion.Slerp(_startWorldRot, _targetWorldRot, t);

            if (_sweep && _body != null)
            {
                var delta = nextPos - transform.position;
                var dist = delta.magnitude;
                if (dist > 1e-5f && _body.SweepTest(delta / dist, out var hit, dist))
                {
                    // Hindernis auf der Bahn → kurz davor stoppen und dort fallen lassen.
                    transform.position += (delta / dist) * Mathf.Max(0f, hit.distance - 0.01f);
                    transform.rotation = nextRot;
                    Land();
                    return;
                }
            }

            transform.SetPositionAndRotation(nextPos, nextRot);
            if (_progress.IsDone)
            {
                transform.SetPositionAndRotation(_targetWorldPos, _targetWorldRot);
                Land();
            }
        }

        private void Land()
        {
            _mode = Mode.None;
            var callback = _onLanded;
            _onLanded = null;
            callback?.Invoke();
        }

        // Abbruchsicher: wird das Item mitten im Flug deaktiviert (z. B. mit dem Gebäude über AreaActivator)
        // oder zerstört, sofort in den Endzustand springen – kein Limbo zwischen Hand und Ziel.
        private void OnDisable()
        {
            if (_mode == Mode.Anchor)
            {
                transform.localPosition = _targetLocalPos;
                transform.localRotation = _targetLocalRot;
                _mode = Mode.None;
            }
            else if (_mode == Mode.World)
            {
                transform.SetPositionAndRotation(_targetWorldPos, _targetWorldRot);
                Land();
            }
        }
    }
}
