using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Define una ruta por la calle usando transforms (hijos o lista manual).
    /// </summary>
    public class NpcPatrolRoute : MonoBehaviour
    {
        [Header("Waypoints")]
        [Tooltip("Si está activo y la lista manual está vacía, usa los hijos en orden jerárquico.")]
        [SerializeField] private bool collectChildWaypoints = true;
        [SerializeField] private Transform[] waypoints;

        [Header("Visualización")]
        [SerializeField] private Color pathColor = new(0.2f, 0.85f, 1f, 0.9f);
        [SerializeField] private float waypointGizmoRadius = 0.25f;

        private Transform[] cachedWaypoints = System.Array.Empty<Transform>();

        public int WaypointCount => cachedWaypoints.Length;
        public bool IsValid => cachedWaypoints.Length >= 2;

        private void Awake()
        {
            RebuildCache();
        }

        private void OnTransformChildrenChanged()
        {
            if (collectChildWaypoints)
            {
                RebuildCache();
            }
        }

        public void RebuildCache()
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                cachedWaypoints = FilterValid(waypoints);
                return;
            }

            if (!collectChildWaypoints)
            {
                cachedWaypoints = System.Array.Empty<Transform>();
                return;
            }

            var collected = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    collected.Add(child);
                }
            }

            cachedWaypoints = collected.ToArray();
        }

        public Transform GetWaypoint(int index)
        {
            if (index < 0 || index >= cachedWaypoints.Length)
            {
                return null;
            }

            return cachedWaypoints[index];
        }

        public Vector3 GetWaypointPosition(int index)
        {
            Transform waypoint = GetWaypoint(index);
            return waypoint != null ? waypoint.position : transform.position;
        }

        public bool TryGetWaypointWait(int index, out float waitSeconds)
        {
            waitSeconds = 0f;
            Transform waypoint = GetWaypoint(index);
            if (waypoint == null)
            {
                return false;
            }

            NpcPatrolWaypoint marker = waypoint.GetComponent<NpcPatrolWaypoint>();
            if (marker != null && marker.HasCustomWait)
            {
                waitSeconds = marker.WaitSeconds;
                return true;
            }

            return false;
        }

        private static Transform[] FilterValid(Transform[] source)
        {
            var valid = new List<Transform>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                {
                    valid.Add(source[i]);
                }
            }

            return valid.ToArray();
        }

        private void OnDrawGizmos()
        {
            DrawPathGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawPathGizmos(true);
        }

        private void DrawPathGizmos(bool selected)
        {
            Transform[] points = Application.isPlaying ? cachedWaypoints : GetEditorWaypoints();
            if (points == null || points.Length == 0)
            {
                return;
            }

            Gizmos.color = selected ? pathColor : new Color(pathColor.r, pathColor.g, pathColor.b, pathColor.a * 0.45f);

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(points[i].position, waypointGizmoRadius);

                if (i + 1 < points.Length && points[i + 1] != null)
                {
                    Gizmos.DrawLine(points[i].position, points[i + 1].position);
                }
            }

            if (points.Length >= 2 && points[0] != null && points[^1] != null)
            {
                Gizmos.DrawLine(points[^1].position, points[0].position);
            }
        }

        private Transform[] GetEditorWaypoints()
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                return FilterValid(waypoints);
            }

            if (!collectChildWaypoints)
            {
                return System.Array.Empty<Transform>();
            }

            var collected = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                collected.Add(transform.GetChild(i));
            }

            return collected.ToArray();
        }
    }
}
