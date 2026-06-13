namespace CozySanta.Core.Items
{
    /// <summary>
    /// Reine Entscheidung „Objekt zur Ruhe gekommen?" für aufnehmbare Items. Akkumuliert die Zeit,
    /// in der lineare und Winkel-Geschwindigkeit unter ihren Schwellen liegen; meldet Ruhe, sobald
    /// diese ruhige Phase <paramref name="settleDuration"/> erreicht. Jede Bewegung über der Schwelle
    /// setzt den Zähler zurück. Ohne UnityEngine-Abhängigkeit – die Runtime füttert die echten
    /// Geschwindigkeiten und friert das Objekt beim Ruhesignal ein (Decide/Apply).
    /// </summary>
    public struct SettleTimer
    {
        private float _calm;

        /// <summary>
        /// Decide: <c>true</c> genau in dem Frame, in dem das Objekt als ruhend gilt. Liegen
        /// <paramref name="linearSpeed"/> und <paramref name="angularSpeed"/> unter ihren Schwellen,
        /// wächst die ruhige Zeit um <paramref name="deltaTime"/>; erreicht sie
        /// <paramref name="settleDuration"/>, wird Ruhe gemeldet. Andernfalls Reset.
        /// </summary>
        public bool Tick(
            float linearSpeed, float angularSpeed, float deltaTime,
            float linearThreshold, float angularThreshold, float settleDuration)
        {
            var calm = linearSpeed <= linearThreshold && angularSpeed <= angularThreshold;
            if (!calm)
            {
                _calm = 0f;
                return false;
            }

            _calm += deltaTime;
            return _calm >= settleDuration;
        }

        /// <summary>Setzt die ruhige Phase zurück (z. B. beim erneuten Aufwecken nach dem Ablegen).</summary>
        public void Reset()
        {
            _calm = 0f;
        }
    }
}
