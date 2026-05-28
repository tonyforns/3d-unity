#if UNITY_EDITOR
using ThreeDUnity.AI;
using UnityEditor;
using UnityEngine;

namespace ThreeDUnity.EditorTools
{
    public static class NpcPatrolRouteCreator
    {
        private const int DefaultWaypointCount = 6;
        private const float WaypointSpacing = 4f;

        [MenuItem("GameObject/ThreeD Unity/NPC Patrol Route", false, 11)]
        private static void CreatePatrolRoute(MenuCommand menuCommand)
        {
            GameObject root = BuildPatrolRouteHierarchy(DefaultWaypointCount);
            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create NPC Patrol Route");
            Selection.activeGameObject = root;
        }

        private static GameObject BuildPatrolRouteHierarchy(int waypointCount)
        {
            GameObject root = new GameObject("StreetPatrolRoute");
            NpcPatrolRoute route = root.AddComponent<NpcPatrolRoute>();

            for (int i = 0; i < waypointCount; i++)
            {
                GameObject waypointObject = new GameObject($"Waypoint_{i + 1}");
                waypointObject.transform.SetParent(root.transform, false);
                waypointObject.transform.localPosition = new Vector3(0f, 0f, i * WaypointSpacing);
                waypointObject.AddComponent<NpcPatrolWaypoint>();
            }

            route.RebuildCache();
            return root;
        }
    }
}
#endif
