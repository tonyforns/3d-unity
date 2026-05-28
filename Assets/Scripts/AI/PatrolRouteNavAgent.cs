using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Recorre una <see cref="NpcPatrolRoute"/> con <see cref="NavMeshAgent"/> (ideal para NPCs en la calle).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class PatrolRouteNavAgent : MonoBehaviour, INpcWalkAnimationSource
    {
        [Header("Ruta")]
        [SerializeField] private NpcPatrolRoute patrolRoute;
        [SerializeField] private int startWaypointIndex;
        [SerializeField] private bool loopRoute = true;
        [SerializeField] private bool pingPong;
        [Tooltip("A veces va al waypoint anterior en lugar del siguiente. No aplica si Ping Pong está activo.")]
        [SerializeField] private bool varyDirection = true;
        [SerializeField, Range(0f, 1f)] private float reverseDirectionChance = 0.35f;

        [Header("Espera en cada punto")]
        [SerializeField] private float minWaitTime = 0.5f;
        [SerializeField] private float maxWaitTime = 2f;
        [SerializeField] private bool useWaypointWaitOverrides = true;

        [Header("Navegación")]
        [Tooltip("Radio al waypoint: el destino de navegación es aleatorio dentro y con estar a esta distancia del centro cuenta como llegada.")]
        [SerializeField, Min(0f)] private float waypointApproachRadius = 1.5f;
        [SerializeField, Min(1)] private int maxApproachSampleAttempts = 10;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;
        [SerializeField] private float reachTimeoutSeconds = 8f;

        [Header("Inicio")]
        [SerializeField] private bool startPatrolOnPlay = true;
        [SerializeField] private bool autoFindRouteInScene;

        [Header("Eventos")]
        [SerializeField] private UnityEvent onStartIdle;
        [SerializeField] private UnityEvent onStartWalking;

        private NavMeshAgent agent;
        private int currentWaypointIndex;
        private int stepDirection = 1;
        private float waitTimer;
        private float destinationElapsedTime;
        private bool isPatrolPaused;
        private Vector3 currentWaypointCenter;
        private PatrolState currentState = PatrolState.None;

        private enum PatrolState
        {
            None,
            Idle,
            Walking
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            FreeRoamNavAgent roam = GetComponent<FreeRoamNavAgent>();
            if (roam != null)
            {
                roam.enabled = false;
            }

        }

        private void Start()
        {
            if (startPatrolOnPlay)
            {
                BeginPatrol();
            }
        }

        private void Update()
        {
            if (isPatrolPaused
                || currentState == PatrolState.None
                || patrolRoute == null
                || !patrolRoute.IsValid)
            {
                return;
            }

            if (!EnsureAgentOnNavMesh())
            {
                return;
            }

            if (agent.pathPending)
            {
                return;
            }

            if (currentState == PatrolState.Idle)
            {
                WaitThenAdvance();
                return;
            }

            if (HasReachedDestination())
            {
                EnterIdle();
                return;
            }

            if (IsTravelingToDestination())
            {
                destinationElapsedTime += Time.deltaTime;
                if (destinationElapsedTime >= reachTimeoutSeconds)
                {
                    TryMoveToCurrentWaypoint();
                }

                return;
            }

            destinationElapsedTime += Time.deltaTime;
            if (destinationElapsedTime >= reachTimeoutSeconds)
            {
                TryMoveToCurrentWaypoint();
            }
        }

        /// <summary>
        /// Asigna la ruta antes de <see cref="BeginPatrol"/> (p. ej. desde un spawner).
        /// Desactiva la búsqueda automática y el inicio en Play del prefab.
        /// </summary>
        public void InitializeForSpawn(NpcPatrolRoute route, int waypointIndex)
        {
            patrolRoute = route;
            startWaypointIndex = waypointIndex;
            autoFindRouteInScene = false;
            startPatrolOnPlay = false;
        }

        public void BeginPatrol()
        {
            if (!TryResolveRoute())
            {
                Debug.LogWarning($"{nameof(PatrolRouteNavAgent)}: no hay ruta válida asignada.", this);
                return;
            }

            patrolRoute.RebuildCache();
            if (!patrolRoute.IsValid)
            {
                Debug.LogWarning(
                    $"{nameof(PatrolRouteNavAgent)}: la ruta necesita al menos 2 waypoints.",
                    patrolRoute);
                return;
            }

            currentWaypointIndex = Mathf.Clamp(startWaypointIndex, 0, patrolRoute.WaypointCount - 1);
            stepDirection = varyDirection && !pingPong ? PickRandomStepDirection() : 1;
            TryMoveToCurrentWaypoint();
        }

        public void PausePatrol()
        {
            isPatrolPaused = true;
            currentState = PatrolState.None;
            destinationElapsedTime = 0f;
            waitTimer = 0f;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
        }

        public void ResumePatrol()
        {
            isPatrolPaused = false;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            BeginPatrol();
        }

        public void StopPatrol()
        {
            isPatrolPaused = true;
            currentState = PatrolState.None;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }

        public void AddOnStartIdleListener(UnityAction listener)
        {
            if (listener != null)
            {
                onStartIdle.AddListener(listener);
            }
        }

        public void RemoveOnStartIdleListener(UnityAction listener)
        {
            if (listener != null)
            {
                onStartIdle.RemoveListener(listener);
            }
        }

        public void AddOnStartWalkingListener(UnityAction listener)
        {
            if (listener != null)
            {
                onStartWalking.AddListener(listener);
            }
        }

        public void RemoveOnStartWalkingListener(UnityAction listener)
        {
            if (listener != null)
            {
                onStartWalking.RemoveListener(listener);
            }
        }

        private bool TryResolveRoute()
        {
            if (patrolRoute != null)
            {
                return true;
            }

            if (!autoFindRouteInScene)
            {
                return false;
            }

            patrolRoute = FindFirstObjectByType<NpcPatrolRoute>();
            return patrolRoute != null;
        }

        private void WaitThenAdvance()
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer > 0f)
            {
                return;
            }

            AdvanceWaypointIndex();
            TryMoveToCurrentWaypoint();
        }

        private void AdvanceWaypointIndex()
        {
            int count = patrolRoute.WaypointCount;
            if (count <= 1)
            {
                return;
            }

            if (pingPong)
            {
                AdvancePingPong(count);
                return;
            }

            if (varyDirection)
            {
                AdvanceWithVariableDirection(count);
                return;
            }

            currentWaypointIndex++;
            if (currentWaypointIndex >= count)
            {
                currentWaypointIndex = loopRoute ? 0 : count - 1;
            }
        }

        private void AdvancePingPong(int count)
        {
            int next = currentWaypointIndex + stepDirection;
            if (next >= count)
            {
                if (loopRoute)
                {
                    stepDirection = -1;
                    currentWaypointIndex = count - 2;
                }
                else
                {
                    currentWaypointIndex = count - 1;
                }
            }
            else if (next < 0)
            {
                if (loopRoute)
                {
                    stepDirection = 1;
                    currentWaypointIndex = 1;
                }
                else
                {
                    currentWaypointIndex = 0;
                }
            }
            else
            {
                currentWaypointIndex = next;
            }
        }

        private void AdvanceWithVariableDirection(int count)
        {
            stepDirection = PickRandomStepDirection();
            int nextIndex = currentWaypointIndex + stepDirection;

            if (loopRoute)
            {
                currentWaypointIndex = WrapWaypointIndex(nextIndex, count);
                return;
            }

            if (nextIndex < 0 || nextIndex >= count)
            {
                stepDirection = -stepDirection;
                nextIndex = currentWaypointIndex + stepDirection;
            }

            currentWaypointIndex = Mathf.Clamp(nextIndex, 0, count - 1);
        }

        private int PickRandomStepDirection()
        {
            int count = patrolRoute.WaypointCount;
            bool canGoForward = loopRoute || currentWaypointIndex < count - 1;
            bool canGoBackward = loopRoute || currentWaypointIndex > 0;

            if (canGoForward && canGoBackward)
            {
                return Random.value < reverseDirectionChance ? -1 : 1;
            }

            if (canGoForward)
            {
                return 1;
            }

            return -1;
        }

        private static int WrapWaypointIndex(int index, int count)
        {
            index %= count;
            if (index < 0)
            {
                index += count;
            }

            return index;
        }

        private void TryMoveToCurrentWaypoint()
        {
            destinationElapsedTime = 0f;
            currentWaypointCenter = patrolRoute.GetWaypointPosition(currentWaypointIndex);

            if (!TryGetWaypointApproachPosition(currentWaypointCenter, out Vector3 destination))
            {
                destination = currentWaypointCenter;
            }

            if (!TryNavigateTo(destination))
            {
                return;
            }

            EnterWalking();
        }

        private bool TryGetWaypointApproachPosition(Vector3 waypointCenter, out Vector3 position)
        {
            position = waypointCenter;

            if (waypointApproachRadius <= 0f)
            {
                return TrySampleNavMeshPosition(waypointCenter, out position);
            }

            float radiusSqr = waypointApproachRadius * waypointApproachRadius;

            for (int i = 0; i < maxApproachSampleAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * waypointApproachRadius;
                Vector3 candidate = waypointCenter + new Vector3(offset.x, 0f, offset.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas)
                    && (hit.position - waypointCenter).sqrMagnitude <= radiusSqr)
                {
                    position = hit.position;
                    return true;
                }
            }

            Vector3 toAgent = transform.position - waypointCenter;
            toAgent.y = 0f;

            if (toAgent.sqrMagnitude < 0.01f)
            {
                Transform waypoint = patrolRoute.GetWaypoint(currentWaypointIndex);
                if (waypoint != null)
                {
                    toAgent = -waypoint.forward;
                    toAgent.y = 0f;
                }
            }

            if (toAgent.sqrMagnitude < 0.01f)
            {
                toAgent = Vector3.forward;
            }

            Vector3 standOffCandidate = waypointCenter + toAgent.normalized * waypointApproachRadius;
            if (NavMesh.SamplePosition(standOffCandidate, out NavMeshHit standOffHit, navMeshSampleRadius, NavMesh.AllAreas)
                && (standOffHit.position - waypointCenter).sqrMagnitude <= radiusSqr)
            {
                position = standOffHit.position;
                return true;
            }

            return TrySampleNavMeshPosition(waypointCenter, out position);
        }

        private bool TrySampleNavMeshPosition(Vector3 worldPosition, out Vector3 position)
        {
            position = worldPosition;

            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            return false;
        }

        private bool TryNavigateTo(Vector3 worldPosition)
        {
            if (!EnsureAgentOnNavMesh())
            {
                return false;
            }

            if (!TrySampleNavMeshPosition(worldPosition, out worldPosition))
            {
                return false;
            }

            agent.isStopped = false;
            agent.ResetPath();
            return agent.SetDestination(worldPosition);
        }

        private bool EnsureAgentOnNavMesh()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                return true;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            if (agent == null || !agent.isOnNavMesh)
            {
                Debug.LogWarning(
                    $"{nameof(PatrolRouteNavAgent)}: el NPC no está sobre el NavMesh.",
                    this);
                return false;
            }

            return true;
        }

        private bool HasReachedDestination()
        {
            if (waypointApproachRadius > 0f)
            {
                return IsNearCurrentWaypoint();
            }

            return HasReachedNavMeshDestination();
        }

        private bool IsNearCurrentWaypoint()
        {
            float radiusSqr = waypointApproachRadius * waypointApproachRadius;
            Vector3 position = transform.position;
            Vector3 center = currentWaypointCenter;
            position.y = 0f;
            center.y = 0f;
            return (position - center).sqrMagnitude <= radiusSqr;
        }

        private bool HasReachedNavMeshDestination()
        {
            if (agent.pathPending)
            {
                return false;
            }

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                return false;
            }

            if (!agent.hasPath)
            {
                return true;
            }

            return agent.velocity.sqrMagnitude < 0.05f;
        }

        private bool IsTravelingToDestination()
        {
            if (waypointApproachRadius > 0f && IsNearCurrentWaypoint())
            {
                return false;
            }

            return agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
        }

        private void EnterIdle()
        {
            if (currentState == PatrolState.Idle)
            {
                return;
            }

            currentState = PatrolState.Idle;
            destinationElapsedTime = 0f;
            waitTimer = ResolveWaitTime();

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            onStartIdle?.Invoke();
        }

        private float ResolveWaitTime()
        {
            if (useWaypointWaitOverrides
                && patrolRoute.TryGetWaypointWait(currentWaypointIndex, out float customWait))
            {
                return customWait;
            }

            return Random.Range(minWaitTime, maxWaitTime);
        }

        private void EnterWalking()
        {
            if (currentState == PatrolState.Walking)
            {
                return;
            }

            currentState = PatrolState.Walking;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            onStartWalking?.Invoke();
        }
    }
}
