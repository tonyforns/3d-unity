using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Genera NPC de patrulla desde un array de prefabs (Animator en la raíz) y los asigna a una <see cref="NpcPatrolRoute"/>.
    /// </summary>
    public class PatrolNpcSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Variantes de NPC con NavMeshAgent y PatrolRouteNavAgent. El Animator debe estar en la raíz.")]
        [SerializeField] private GameObject[] npcPrefabs;

        [Header("Ruta")]
        [SerializeField] private NpcPatrolRoute patrolRoute;

        [Header("Spawn")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(0)] private int spawnCount = 3;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool pickRandomPrefab = true;
        [SerializeField] private bool distributeStartWaypoints = true;
        [SerializeField] private Transform spawnedNpcParent;

        [Header("Navegación")]
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

        private readonly List<GameObject> spawnedInstances = new();

        public IReadOnlyList<GameObject> SpawnedInstances => spawnedInstances;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAll();
            }
        }

        [ContextMenu("Spawn All")]
        public void SpawnAll()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            patrolRoute.RebuildCache();

            int count = ResolveSpawnCount();
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = PickPrefab(i);
                if (prefab == null)
                {
                    continue;
                }

                if (!TryGetSpawnPosition(i, out Vector3 spawnPosition))
                {
                    Debug.LogWarning(
                        $"{nameof(PatrolNpcSpawner)}: no se pudo colocar el NPC {i + 1} sobre el NavMesh.",
                        this);
                    continue;
                }

                GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, GetSpawnParent());
                instance.name = $"{prefab.name}_{spawnedInstances.Count + 1}";

                if (!TryConfigureInstance(instance, i, count))
                {
                    Destroy(instance);
                    continue;
                }

                spawnedInstances.Add(instance);
            }
        }

        [ContextMenu("Clear Spawned")]
        public void ClearSpawned()
        {
            for (int i = spawnedInstances.Count - 1; i >= 0; i--)
            {
                GameObject instance = spawnedInstances[i];
                if (instance != null)
                {
                    Destroy(instance);
                }
            }

            spawnedInstances.Clear();
        }

        private bool ValidateConfiguration()
        {
            if (patrolRoute == null)
            {
                Debug.LogWarning($"{nameof(PatrolNpcSpawner)}: asigna una {nameof(NpcPatrolRoute)}.", this);
                return false;
            }

            if (!patrolRoute.IsValid)
            {
                patrolRoute.RebuildCache();
            }

            if (!patrolRoute.IsValid)
            {
                Debug.LogWarning(
                    $"{nameof(PatrolNpcSpawner)}: la ruta necesita al menos 2 waypoints.",
                    patrolRoute);
                return false;
            }

            if (npcPrefabs == null || npcPrefabs.Length == 0)
            {
                Debug.LogWarning($"{nameof(PatrolNpcSpawner)}: asigna al menos un prefab de NPC.", this);
                return false;
            }

            return true;
        }

        private int ResolveSpawnCount()
        {
            if (spawnCount > 0)
            {
                return spawnCount;
            }

            return spawnPoints != null ? spawnPoints.Length : 0;
        }

        private GameObject PickPrefab(int spawnIndex)
        {
            if (npcPrefabs.Length == 1)
            {
                return npcPrefabs[0];
            }

            if (pickRandomPrefab)
            {
                return npcPrefabs[Random.Range(0, npcPrefabs.Length)];
            }

            return npcPrefabs[spawnIndex % npcPrefabs.Length];
        }

        private bool TryGetSpawnPosition(int spawnIndex, out Vector3 position)
        {
            Vector3 candidate = transform.position;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform point = spawnPoints[spawnIndex % spawnPoints.Length];
                if (point != null)
                {
                    candidate = point.position;
                }
            }
            else if (distributeStartWaypoints)
            {
                int waypointIndex = ResolveStartWaypointIndex(spawnIndex, ResolveSpawnCount());
                candidate = patrolRoute.GetWaypointPosition(waypointIndex);
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            position = candidate;
            return false;
        }

        private bool TryConfigureInstance(GameObject instance, int spawnIndex, int totalSpawns)
        {
            if (!ValidatePrefab(instance, isPrefabAsset: false, out string error))
            {
                Debug.LogWarning($"{nameof(PatrolNpcSpawner)}: {instance.name} — {error}", instance);
                return false;
            }

            PatrolRouteNavAgent patrolAgent = instance.GetComponent<PatrolRouteNavAgent>();
            int waypointIndex = distributeStartWaypoints
                ? ResolveStartWaypointIndex(spawnIndex, totalSpawns)
                : 0;

            patrolAgent.InitializeForSpawn(patrolRoute, waypointIndex);
            patrolAgent.BeginPatrol();
            return true;
        }

        private int ResolveStartWaypointIndex(int spawnIndex, int totalSpawns)
        {
            int waypointCount = patrolRoute.WaypointCount;
            if (waypointCount <= 1 || totalSpawns <= 1)
            {
                return 0;
            }

            return Mathf.Clamp(
                Mathf.RoundToInt((float)spawnIndex / (totalSpawns - 1) * (waypointCount - 1)),
                0,
                waypointCount - 1);
        }

        private Transform GetSpawnParent()
        {
            return spawnedNpcParent != null ? spawnedNpcParent : transform;
        }

        private static bool ValidatePrefab(GameObject gameObject, bool isPrefabAsset, out string error)
        {
            error = null;

            if (gameObject.GetComponent<NavMeshAgent>() == null)
            {
                error = "falta NavMeshAgent";
                return false;
            }

            if (gameObject.GetComponent<PatrolRouteNavAgent>() == null)
            {
                error = "falta PatrolRouteNavAgent";
                return false;
            }

            Animator animator = gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                error = "falta Animator en la raíz";
                return false;
            }

            if (!isPrefabAsset && animator.transform != gameObject.transform)
            {
                error = "el Animator debe estar en la raíz del prefab";
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (npcPrefabs == null)
            {
                return;
            }

            for (int i = 0; i < npcPrefabs.Length; i++)
            {
                GameObject prefab = npcPrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                if (!ValidatePrefab(prefab, isPrefabAsset: true, out string error))
                {
                    Debug.LogWarning(
                        $"{nameof(PatrolNpcSpawner)}: prefab '{prefab.name}' — {error}",
                        this);
                }
            }
        }
#endif
    }
}
