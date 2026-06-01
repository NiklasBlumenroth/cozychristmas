namespace CozySanta.Core.Interaction
{
    /// <summary>
    /// Reines Gate für die Interaktions-Auslösung. Trennt die Entscheidung (Decide) vom
    /// Seiteneffekt (Apply: <c>Interact()</c> in der Runtime).
    /// </summary>
    public static class InteractionTrigger
    {
        /// <summary>Nur auslösen, wenn ein Ziel fokussiert ist UND die Interaktionstaste gedrückt wurde.</summary>
        public static bool ShouldInteract(bool hasFocus, bool interactPressed)
        {
            return hasFocus && interactPressed;
        }
    }
}
