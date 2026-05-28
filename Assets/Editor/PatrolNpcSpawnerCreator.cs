#if UNITY_EDITOR
using ThreeDUnity.AI;
using UnityEditor;
using UnityEngine;

namespace ThreeDUnity.EditorTools
{
    public static class PatrolNpcSpawnerCreator
    {
        [MenuItem("GameObject/ThreeD Unity/Patrol NPC Spawner", false, 10)]
        private static void CreateSpawner(MenuCommand menuCommand)
        {
            GameObject root = new GameObject("PatrolNpcSpawner");
            PatrolNpcSpawner spawner = root.AddComponent<PatrolNpcSpawner>();

            GameObject spawnPointA = new GameObject("SpawnPoint_1");
            spawnPointA.transform.SetParent(root.transform, false);
            spawnPointA.transform.localPosition = new Vector3(-2f, 0f, 0f);

            GameObject spawnPointB = new GameObject("SpawnPoint_2");
            spawnPointB.transform.SetParent(root.transform, false);
            spawnPointB.transform.localPosition = new Vector3(2f, 0f, 0f);

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            SerializedProperty spawnPointsProperty = serializedSpawner.FindProperty("spawnPoints");
            spawnPointsProperty.arraySize = 2;
            spawnPointsProperty.GetArrayElementAtIndex(0).objectReferenceValue = spawnPointA.transform;
            spawnPointsProperty.GetArrayElementAtIndex(1).objectReferenceValue = spawnPointB.transform;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create Patrol NPC Spawner");
            Selection.activeGameObject = root;
        }
    }
}
#endif
