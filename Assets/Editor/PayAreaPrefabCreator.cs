#if UNITY_EDITOR
using ThreeDUnity.Interaction;
using UnityEditor;
using UnityEngine;

namespace ThreeDUnity.EditorTools
{
    public static class PayAreaPrefabCreator
    {
        private const int DefaultSlotCount = 4;
        private const float SlotSpacing = 0.35f;

        [MenuItem("GameObject/ThreeD Unity/Pay Area", false, 12)]
        private static void CreatePayArea(MenuCommand menuCommand)
        {
            GameObject root = BuildPayAreaHierarchy(DefaultSlotCount);
            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create Pay Area");
            Selection.activeGameObject = root;
        }

        private static GameObject BuildPayAreaHierarchy(int slotCount)
        {
            GameObject root = new GameObject("PayArea");
            PayArea payArea = root.AddComponent<PayArea>();

            PayAreaSlot[] slots = new PayAreaSlot[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObject = new GameObject($"PaySlot_{i + 1}");
                slotObject.transform.SetParent(root.transform, false);
                slotObject.transform.localPosition = new Vector3(i * SlotSpacing, 0.1f, 0f);
                slots[i] = slotObject.AddComponent<PayAreaSlot>();
            }

            SerializedObject serializedPayArea = new SerializedObject(payArea);
            SerializedProperty slotsProperty = serializedPayArea.FindProperty("slots");
            slotsProperty.arraySize = slotCount;
            for (int i = 0; i < slotCount; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            serializedPayArea.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }
    }
}
#endif
