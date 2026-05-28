using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Coloca este objeto y sus hijos en la capa <see cref="InteractionLayers.InteractableName"/>.
    /// Se añade automáticamente a los componentes <see cref="IInteractable"/> del proyecto.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractablePhysicsLayer : MonoBehaviour
    {
        private void Awake()
        {
            InteractionLayers.ApplyToInteractableHierarchy(transform);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InteractionLayers.ApplyToInteractableHierarchy(transform);
        }

        private void Reset()
        {
            InteractionLayers.ApplyToInteractableHierarchy(transform);
        }
#endif
    }
}
