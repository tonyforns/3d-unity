using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Capa de física usada por <see cref="PlayerInteractionController"/> y colliders de objetos interactuables.
    /// </summary>
    public static class InteractionLayers
    {
        public const string InteractableName = "Interactable";

        public static int Interactable => LayerMask.NameToLayer(InteractableName);

        public static LayerMask InteractableMask
        {
            get
            {
                int layer = Interactable;
                return layer >= 0 ? 1 << layer : 0;
            }
        }

        /// <summary>
        /// Asigna la capa Interactable a este transform y a todos sus hijos.
        /// </summary>
        public static void ApplyToInteractableHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            int layer = Interactable;
            if (layer < 0)
            {
                return;
            }

            ApplyRecursive(root, layer);
        }

        private static void ApplyRecursive(Transform current, int layer)
        {
            current.gameObject.layer = layer;

            for (int i = 0; i < current.childCount; i++)
            {
                ApplyRecursive(current.GetChild(i), layer);
            }
        }
    }
}
