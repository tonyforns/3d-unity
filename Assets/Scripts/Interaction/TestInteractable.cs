using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Objeto interactuable de prueba: registra en consola, cuenta interacciones y rota un poco al pulsar E.
    /// </summary>
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class TestInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Probar objeto";
        [SerializeField] private float rotateStepDegrees = 15f;
        [SerializeField] private bool allowInteract = true;

        public string InteractionPrompt => interactionPrompt;

        public int TimesInteracted { get; private set; }

        public bool CanInteract(PlayerController interactor) => allowInteract && interactor != null;

        public void Interact(PlayerController interactor)
        {
            TimesInteracted++;
            transform.Rotate(0f, rotateStepDegrees, 0f, Space.World);
            Debug.Log(
                $"[TestInteractable] '{name}' interactuado ({TimesInteracted}x) por '{interactor.name}'.",
                this);
        }
    }
}
