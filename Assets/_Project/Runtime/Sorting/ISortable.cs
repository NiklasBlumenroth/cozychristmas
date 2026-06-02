using CozySanta.Core.Sorting;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Sortierbares Weltobjekt: liefert seinen <see cref="SortKey"/>. Implementierende Komponente ist
    /// ein <c>UnityEngine.Component</c> (für Reparenting/Identität). Ergänzt das F3-Aufnehmen
    /// (<c>IPickup</c>), ohne es zu ersetzen.
    /// </summary>
    public interface ISortable
    {
        SortKey Key { get; }
    }
}
