using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Contrato para objetos con los que el jugador puede interactuar.
    /// Implementa esta interfaz en un <see cref="MonoBehaviour"/> (normalmente en el mismo objeto que el collider).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texto opcional para UI (puede ser vacío).</summary>
        string InteractionPrompt { get; }

        bool CanInteract(PlayerController interactor);

        void Interact(PlayerController interactor);
    }
}
