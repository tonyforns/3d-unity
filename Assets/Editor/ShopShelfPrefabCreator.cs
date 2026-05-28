#if UNITY_EDITOR
using ThreeDUnity.Interaction;
using UnityEditor;
using UnityEngine;

namespace ThreeDUnity.EditorTools
{
    public static class ShopShelfPrefabCreator
    {
        private const int DefaultSlotCount = 3;
        private const float SlotSpacing = 0.45f;

        [MenuItem("GameObject/ThreeD Unity/Shop Shelf", false, 11)]
        private static void CreateShopShelf(MenuCommand menuCommand)
        {
            GameObject root = BuildShopShelfHierarchy(DefaultSlotCount);
            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create Shop Shelf");
            Selection.activeGameObject = root;
        }

        private static GameObject BuildShopShelfHierarchy(int slotCount)
        {
            GameObject root = new GameObject("ShopShelf");
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3((slotCount - 1) * SlotSpacing * 0.5f, 0.5f, 0f);
            collider.size = new Vector3(Mathf.Max(1f, slotCount * SlotSpacing), 1f, 0.6f);

            ShopShelf shelf = root.AddComponent<ShopShelf>();
            root.AddComponent<InteractableShopShelf>();

            ShopShelfSlot[] slots = new ShopShelfSlot[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObject = new GameObject($"Slot_{i + 1}");
                slotObject.transform.SetParent(root.transform, false);
                slotObject.transform.localPosition = new Vector3(i * SlotSpacing, 0.5f, 0f);
                slots[i] = slotObject.AddComponent<ShopShelfSlot>();
            }

            SerializedObject serializedShelf = new SerializedObject(shelf);
            SerializedProperty slotsProperty = serializedShelf.FindProperty("slots");
            slotsProperty.arraySize = slotCount;
            for (int i = 0; i < slotCount; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            serializedShelf.ApplyModifiedPropertiesWithoutUndo();

            InteractionLayers.ApplyToInteractableHierarchy(root.transform);

            return root;
        }
    }
}
#endif
