namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Löst die von der Core-Entscheidung gewählte <c>TargetId</c> auf das konkrete
    /// <see cref="IInteractable"/> auf. Wird vom Probe implementiert (Frame-Zuordnung), damit die
    /// Core-Schicht frei von Runtime-Typen bleibt.
    /// </summary>
    public interface IInteractableResolver
    {
        bool TryResolve(int targetId, out IInteractable interactable);
    }
}
