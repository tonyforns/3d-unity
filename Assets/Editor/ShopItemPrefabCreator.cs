#if UNITY_EDITOR
using ThreeDUnity.Interaction;
using ThreeDUnity.Items;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDUnity.EditorTools
{
    public static class ShopItemPrefabCreator
    {
        private const string DefaultPrefabPath = "Assets/Prefabs/ShopItem.prefab";

        [MenuItem("GameObject/ThreeD Unity/Shop Item", false, 10)]
        private static void CreateShopItemInScene(MenuCommand menuCommand)
        {
            GameObject root = BuildShopItemHierarchy();
            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create Shop Item");
            Selection.activeGameObject = root;
        }

        [MenuItem("Assets/Create/ThreeD Unity/Shop Item Prefab")]
        private static void CreateShopItemPrefabAsset()
        {
            GameObject root = BuildShopItemHierarchy();

            string directory = System.IO.Path.GetDirectoryName(DefaultPrefabPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(DefaultPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        private static GameObject BuildShopItemHierarchy()
        {
            GameObject root = new GameObject("ShopItem");
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.6f, 0.6f, 0.6f);
            collider.center = new Vector3(0f, 0.3f, 0f);

            InteractableShopItem interactable = root.AddComponent<InteractableShopItem>();

            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(root.transform, false);

            GameObject priceSign = new GameObject("PriceSign");
            priceSign.transform.SetParent(root.transform, false);
            priceSign.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            FaceTarget billboard = priceSign.AddComponent<FaceTarget>();

            GameObject canvasGo = new GameObject("PriceCanvas");
            canvasGo.transform.SetParent(priceSign.transform, false);
            canvasGo.transform.localPosition = Vector3.zero;
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(220f, 90f);

            GameObject labelGo = new GameObject("PriceLabel");
            labelGo.transform.SetParent(canvasGo.transform, false);

            TextMeshProUGUI priceLabel = labelGo.AddComponent<TextMeshProUGUI>();
            priceLabel.text = "$0";
            priceLabel.fontSize = 36f;
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.color = Color.white;

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            priceSign.SetActive(false);

            SerializedObject serializedInteractable = new SerializedObject(interactable);
            serializedInteractable.FindProperty("visualAnchor").objectReferenceValue = visualRoot.transform;
            serializedInteractable.FindProperty("priceSignRoot").objectReferenceValue = priceSign;
            serializedInteractable.FindProperty("priceLabel").objectReferenceValue = priceLabel;
            serializedInteractable.FindProperty("priceBillboard").objectReferenceValue = billboard;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

            InteractionLayers.ApplyToInteractableHierarchy(root.transform);

            return root;
        }
    }
}
#endif
