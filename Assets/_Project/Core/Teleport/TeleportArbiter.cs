using System.Collections.Generic;

namespace CozySanta.Core.Teleport
{
    /// <summary>
    /// Entscheidungslogik (Decide) der Teleport-Trigger ohne Unity-Abhängigkeiten: verhindert das
    /// „Bounce"-Problem, bei dem der Spieler nach einem Teleport im Ziel-Trigger landet und sofort
    /// wieder weggeschickt wird. Trigger werden über einen stabilen Index identifiziert.
    ///
    /// Modell: Ein Trigger gilt als „belegt", solange der Spieler darin steht. Ein belegter Trigger
    /// feuert nicht. Beim Teleport meldet die Apply-Schicht alle Trigger, die das Ziel überlappen, als
    /// belegt (<see cref="MarkOccupied"/>) – deren unmittelbar folgendes Betreten wird so verschluckt.
    /// Erst wenn der Spieler den Trigger wieder verlässt (<see cref="NotifyExit"/>), kann er erneut auslösen.
    /// </summary>
    public sealed class TeleportArbiter
    {
        private readonly HashSet<int> _occupied = new HashSet<int>();

        /// <summary>Meldet das Betreten von Trigger <paramref name="triggerId"/>. Liefert true, wenn
        /// daraufhin teleportiert werden soll (Trigger war frei); false, wenn der Trigger bereits belegt
        /// ist (Spieler steht schon drin bzw. ist gerade hier gelandet → kein erneuter Sprung).</summary>
        public bool ShouldTeleport(int triggerId)
        {
            // Add gibt false zurück, wenn die ID bereits enthalten war → Trigger belegt → nicht auslösen.
            return _occupied.Add(triggerId);
        }

        /// <summary>Markiert einen Trigger als belegt, ohne auszulösen. Von der Apply-Schicht für alle
        /// Trigger genutzt, die das Teleport-Ziel überlappen, damit deren Betreten nicht zurückwirft.</summary>
        public void MarkOccupied(int triggerId) => _occupied.Add(triggerId);

        /// <summary>Meldet das Verlassen eines Triggers – danach kann er wieder auslösen.</summary>
        public void NotifyExit(int triggerId) => _occupied.Remove(triggerId);

        /// <summary>Setzt die Belegung zurück (alle Trigger frei). Von der Apply-Schicht beim Teleport
        /// genutzt, um die Belegung anschließend exakt auf die am Ziel überlappenden Trigger zu setzen –
        /// so wird der Quell-Trigger sofort frei, auch wenn (nach dem Versetzen) kein OnTriggerExit feuert.</summary>
        public void Reset() => _occupied.Clear();

        /// <summary>True, solange der Trigger als belegt gilt (Diagnose/Test).</summary>
        public bool IsOccupied(int triggerId) => _occupied.Contains(triggerId);
    }
}
