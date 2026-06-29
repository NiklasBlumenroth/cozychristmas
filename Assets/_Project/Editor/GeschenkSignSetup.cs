using UnityEditor;

namespace CozySanta.Editor
{
    /// <summary>
    /// Setzt auf jedes <c>BoardRegal</c>-Schild der Geschenkehalle (unter <c>GeschenkehalleInnen</c>) ein
    /// Abbild-Mesh des Items, das dort einsortiert werden soll. Reiner Editor-/Asset-Schritt; die gesamte
    /// Logik liegt in <see cref="BoardSignSetup"/> (geteilt mit der Dekohalle).
    /// </summary>
    public static class GeschenkSignSetup
    {
        private const string HallRoot = "GeschenkehalleInnen";
        private const string CatalogPath = GeschenkItemSetup.CatalogPath;

        [MenuItem("CozySanta/Geschenkehalle/Item-Abbild auf Schilder setzen")]
        public static void Setup() => BoardSignSetup.Run(HallRoot, CatalogPath, "Schild");
    }
}
