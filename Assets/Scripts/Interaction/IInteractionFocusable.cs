namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Recibe aviso cuando el jugador apunta a este interactuable y puede pulsar la tecla de interacción.
    /// </summary>
    public interface IInteractionFocusable
    {
        void SetInteractionFocused(bool focused);
    }
}
