namespace CozySanta.Core.Props
{
    /// <summary>Bewegungsphase einer Tür.</summary>
    public enum DoorPhase { Closed, Opening, Open, Closing }

    /// <summary>
    /// Reine, testbare Tür-Toggle-Logik (Decide): hält Phase und Fortschritt (0 = zu, 1 = offen),
    /// kehrt bei erneutem Toggle die Richtung um (ohne Sprung) und rastet an den Enden ein. Keine
    /// Unity-Abhängigkeit; das Setzen der tatsächlichen Rotation ist Apply-Aufgabe der Runtime.
    /// </summary>
    public sealed class DoorMotion
    {
        private readonly float _duration;

        /// <param name="openDuration">Dauer (Sekunden) für eine volle Auf- bzw. Zu-Bewegung.</param>
        public DoorMotion(float openDuration)
        {
            _duration = openDuration <= 0f ? 0.0001f : openDuration;
        }

        /// <summary>Aktuelle Bewegungsphase.</summary>
        public DoorPhase Phase { get; private set; } = DoorPhase.Closed;

        /// <summary>Fortschritt 0 (geschlossen) … 1 (offen).</summary>
        public float Progress01 { get; private set; }

        /// <summary>True, während sich die Tür bewegt.</summary>
        public bool IsMoving => Phase == DoorPhase.Opening || Phase == DoorPhase.Closing;

        /// <summary>True, wenn die Tür vollständig offen steht.</summary>
        public bool IsOpen => Phase == DoorPhase.Open;

        /// <summary>True, wenn aktuell „offen" angesteuert wird (für den Hinweistext).</summary>
        public bool TargetsOpen => Phase == DoorPhase.Open || Phase == DoorPhase.Opening;

        /// <summary>Schaltet um: aus Ruhe startet die Bewegung, in Bewegung kehrt die Richtung um.</summary>
        public void Toggle()
        {
            Phase = TargetsOpen ? DoorPhase.Closing : DoorPhase.Opening;
        }

        /// <summary>Treibt den Fortschritt um <paramref name="deltaSeconds"/> Richtung Ziel und rastet bei 0/1 ein.</summary>
        public void Step(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || !IsMoving)
            {
                return;
            }

            var delta = deltaSeconds / _duration;
            if (Phase == DoorPhase.Opening)
            {
                Progress01 += delta;
                if (Progress01 >= 1f)
                {
                    Progress01 = 1f;
                    Phase = DoorPhase.Open;
                }
            }
            else // Closing
            {
                Progress01 -= delta;
                if (Progress01 <= 0f)
                {
                    Progress01 = 0f;
                    Phase = DoorPhase.Closed;
                }
            }
        }
    }
}
