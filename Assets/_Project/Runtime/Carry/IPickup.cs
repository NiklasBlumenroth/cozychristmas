namespace CozySanta.Runtime.Carry
{
    /// <summary>
    /// Aufnehmbares Weltobjekt: liefert sein Gewicht. Implementierende Komponente ist ein
    /// <c>UnityEngine.Component</c> (für Reparenting/Identität beim Tragen).
    /// </summary>
    public interface IPickup
    {
        float Weight { get; }
    }
}
