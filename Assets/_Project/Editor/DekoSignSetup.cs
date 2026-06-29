using UnityEditor;

namespace CozySanta.Editor
{
    /// <summary>
    /// Setzt auf jedes <c>BoardRegal</c>-Schild der Dekohalle (Regale unter <c>DekoInnen</c>) ein Abbild-Mesh
    /// des Items, das in das jeweilige Regal einsortiert werden soll (Quelle: akzeptierter SortKey der Fächer
    /// → Prefab aus dem <c>DekohalleCatalog</c>). Pendant zu <see cref="GeschenkSignSetup"/>; die gesamte
    /// Logik liegt in <see cref="BoardSignSetup"/>. Voraussetzung: die Regale sind bereits belegt
    /// (<see cref="DekohalleSortAssignmentSetup"/>). Reiner Editor-/Asset-Schritt.
    /// </summary>
    public static class DekoSignSetup
    {
        private const string HallRoot = "DekoInnen";
        private const string CatalogPath = "Assets/_Project/Data/DekohalleCatalog.asset";

        [MenuItem("CozySanta/Dekohalle/Item-Abbild auf Schilder setzen")]
        public static void Setup() => BoardSignSetup.Run(HallRoot, CatalogPath, "Deko-Schild");
    }
}
