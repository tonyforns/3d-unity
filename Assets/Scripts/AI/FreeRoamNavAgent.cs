using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Patrulla el NavMesh eligiendo destinos aleatorios dentro de un radio.
    /// Requiere NavMesh horneado y un <see cref="NavMeshAgent"/> en el mismo GameObject.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class FreeRoamNavAgent : MonoBehaviour, INpcWalkAnimationSource
    {
        [Header("Roam")]
        [SerializeField] private float roamRadius = 15f;
        [SerializeField] private int maxSampleAttempts = 30;

        [Header("Wait at destination")]
        [SerializeField] private float minWaitTime = 1f;
        [SerializeField] private float maxWaitTime = 4f;

        [Header("Unreachable destination")]
        [SerializeField] private float reachTimeoutSeconds = 5f;

        [Header("Startup")]
        [SerializeField] private bool pickDestinationOnStart = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onStartIdle;
        [SerializeField] private UnityEvent onStartWalking;

        private NavMeshAgent agent;
        private float waitTimer;
        private float destinationElapsedTime;
        private RoamState currentState = RoamState.None;

        private enum RoamState
        {
            None,
            Idle,
            Walking
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            if (pickDestinationOnStart)
            {
                waitTimer = 0f;
                PickNewDestination();
            }
            else
            {
                EnterIdle();
            }
        }

        private void Update()
        {
            if (agent.pathPending)
            {
                return;
            }

            if (currentState == RoamState.Idle)
            {
                WaitThenPickNewDestination();
                return;
            }

            if (HasReachedDestination())
            {
                EnterIdle();
                return;
            }

            if (IsTravelingToDestination())
            {
                destinationElapsedTime += UnityEngine.Time.deltaTime;
                if (destinationElapsedTime >= reachTimeoutSeconds)
                {
                    PickNewDestination();
                }

                return;
            }

            destinationElapsedTime += UnityEngine.Time.deltaTime;
            if (destinationElapsedTime >= reachTimeoutSeconds)
            {
                PickNewDestination();
            }
        }

        private bool HasReachedDestination()
        {
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
            return agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
        }

        private void WaitThenPickNewDestination()
        {
            waitTimer -= UnityEngine.Time.deltaTime;
            if (waitTimer > 0f)
            {
                return;
            }

            PickNewDestination();
        }

        private void PickNewDestination()
        {
            destinationElapsedTime = 0f;

            if (TryGetRandomPoint(transform.position, roamRadius, out Vector3 destination))
            {
                agent.SetDestination(destination);
                EnterWalking();
            }
        }

        private void EnterIdle()
        {
            if (currentState == RoamState.Idle)
            {
                return;
            }

            currentState = RoamState.Idle;
            destinationElapsedTime = 0f;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
            onStartIdle?.Invoke();
        }

        private void EnterWalking()
        {
            if (currentState == RoamState.Walking)
            {
                return;
            }

            currentState = RoamState.Walking;
            onStartWalking?.Invoke();
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

        private bool TryGetRandomPoint(Vector3 origin, float radius, out Vector3 result)
        {
            for (int i = 0; i < maxSampleAttempts; i++)
            {
                Vector3 random = origin + Random.insideUnitSphere * radius;
                random.y = origin.y;

                if (NavMesh.SamplePosition(random, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = origin;
            return false;
        }
    }
}
