#if UNITY_EDITOR
using ThreeDUnity.Interaction;
using UnityEditor;
using UnityEngine;

namespace ThreeDUnity.EditorTools
{
    public static class InteractableLayerSceneUtility
    {
        [MenuItem("Tools/ThreeD Unity/Apply Interactable Layer In Scene")]
        private static void ApplyInOpenScenes()
        {
            int layer = InteractionLayers.Interactable;
            if (layer < 0)
            {
                EditorUtility.DisplayDialog(
                    "Capa Interactable",
                    "Define la capa \"Interactable\" en Edit > Project Settings > Tags and Layers.",
                    "OK");
                return;
            }

            InteractablePhysicsLayer[] components = Object.FindObjectsByType<InteractablePhysicsLayer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int count = 0;
            foreach (InteractablePhysicsLayer component in components)
            {
                if (component == null)
                {
                    continue;
                }

                InteractionLayers.ApplyToInteractableHierarchy(component.transform);
                EditorUtility.SetDirty(component.gameObject);
                count++;
            }

            Debug.Log($"Capa Interactable aplicada en {count} objeto(s).");
        }
    }
}
#endif
