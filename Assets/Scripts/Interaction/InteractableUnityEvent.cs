using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Implementación mínima de <see cref="IInteractable"/> configurable desde el inspector (<see cref="UnityEvent"/>).
    /// </summary>
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableUnityEvent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Interactuar";
        [SerializeField] private UnityEvent<GameObject> onInteract;

        public string InteractionPrompt => interactionPrompt;

        public bool CanInteract(PlayerController interactor) => true;

        public void Interact(PlayerController interactor)
        {
            onInteract?.Invoke(interactor.gameObject);
        }
    }
}
