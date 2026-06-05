using System.Collections;
using System.Collections.Generic;
using ThreeDUnity.Interaction;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// NPC que recorre puntos con <see cref="NavMeshAgent"/>: estantería → recoge ítems → pay area → deposita.
    /// Implementa <see cref="IPayAreaItemSource"/> para vaciar su inventario en el mostrador.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class ShopCustomerAgent : MonoBehaviour, IPayAreaItemSource, INpcWalkAnimationSource
    {
        [Header("Destinos (opcional)")]
        [Tooltip("Si están vacíos y Auto Find In Scene está activo, se buscan en la escena al iniciar la compra.")]
        [SerializeField] private ShopShelf targetShelf;
        [SerializeField] private Transform shelfApproachPoint;
        [SerializeField] private PayArea targetPayArea;
        [SerializeField] private Transform payAreaApproachPoint;

        [Header("Búsqueda en escena")]
        [SerializeField] private bool autoFindInScene = true;
        [SerializeField] private bool waitForShelfWithStock = true;
        [SerializeField, Min(0.1f)] private float shelfSearchRetryInterval = 1f;
        [SerializeField, Min(0.5f)] private float approachStandOffDistance = 1.5f;
        [SerializeField] private string[] approachChildNames = { "CustomerApproach", "ApproachPoint" };

        [Header("Compra")]
        [SerializeField, Min(1)] private int itemsToTake = 2;
        [SerializeField] private Transform carryAnchor;

        [Header("Tiempos")]
        [SerializeField, Min(0f)] private float shelfActionDelay = 0.35f;
        [SerializeField, Min(0f)] private float pickItemDelay = 0.25f;
        [SerializeField, Min(0f)] private float depositActionDelay = 0.35f;

        [Header("Navegación")]
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;
        [SerializeField, Min(0f)] private float reachTimeoutSeconds = 12f;
        [SerializeField, Min(0f)] private float stuckCheckIntervalSeconds = 2f;
        [SerializeField, Min(1)] private int maxRecoverAttemptsPerTrip = 3;
        [Tooltip("Radio a la zona de estantería: con estar cerca basta para recoger (no hace falta el punto exacto del NavMesh).")]
        [SerializeField, Min(0f)] private float maxShelfArrivalDistance = 2.5f;
        [Tooltip("Radio a la zona de pay area: con estar cerca basta para depositar.")]
        [SerializeField, Min(0f)] private float maxPayAreaArrivalDistance = 2.5f;

        [Header("Salida")]
        [SerializeField] private bool leaveAfterPurchase = true;
        [Tooltip("Si está desactivado, no navega al exit point (útil para NPC que siguen patrullando).")]
        [SerializeField] private bool useExitPoint = true;
        [Tooltip("Tras depositar en el mostrador, espera a que la caja confirme el pago correcto antes de irse.")]
        [SerializeField] private bool waitForPaymentBeforeLeaving = true;
        [SerializeField] private InteractableCashRegister cashRegister;
        [SerializeField] private bool autoFindCashRegister = true;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private bool autoFindExitInScene = true;

        [Header("Inicio")]
        [SerializeField] private bool startShoppingOnPlay = true;

        [Header("Eventos")]
        [SerializeField] private UnityEvent onStartWalking;
        [SerializeField] private UnityEvent onStartIdle;
        [SerializeField] private UnityEvent onShoppingFinished;
        [SerializeField] private UnityEvent onLeftShop;
        [SerializeField] private UnityEvent onShoppingTripEnded;

        private readonly List<InteractableShopItem> inventory = new List<InteractableShopItem>();
        private readonly List<InteractableShopItem> depositedPayAreaItems = new List<InteractableShopItem>();

        private NavMeshAgent agent;
        private CustomerState currentState = CustomerState.Idle;
        private bool isWalking;
        private bool isSubscribedToPayment;
        private float destinationElapsedTime;
        private float stuckElapsedTime;
        private int recoverAttempts;
        private Coroutine actionRoutine;

        private enum CustomerState
        {
            Idle,
            WaitingForShelf,
            MovingToShelf,
            PickingFromShelf,
            MovingToPayArea,
            Depositing,
            WaitingForPayment,
            Leaving,
            Finished
        }

        public IReadOnlyList<InteractableShopItem> Inventory => inventory;
        public bool IsWaitingForShelf => currentState == CustomerState.WaitingForShelf;
        public bool IsWaitingForPayment => currentState == CustomerState.WaitingForPayment;
        public bool IsShopping =>
            currentState != CustomerState.Idle && currentState != CustomerState.Finished;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (carryAnchor == null)
            {
                carryAnchor = transform;
            }

            FreeRoamNavAgent roam = GetComponent<FreeRoamNavAgent>();
            if (roam != null)
            {
                roam.enabled = false;
            }
        }

        private void Start()
        {
            if (startShoppingOnPlay)
            {
                BeginShopping();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromCashRegister();
            ReleaseTargetShelf();
        }

        private void Update()
        {
            if (currentState != CustomerState.MovingToShelf
                && currentState != CustomerState.MovingToPayArea
                && currentState != CustomerState.Leaving)
            {
                return;
            }

            if (!IsAgentOnNavMesh())
            {
                return;
            }

            if (TryCompleteShoppingApproach())
            {
                return;
            }

            if (agent.pathPending)
            {
                return;
            }

            if (HasReachedDestination())
            {
                OnReachedNavigationTarget();
                return;
            }

            if (IsTravelingToDestination())
            {
                destinationElapsedTime += UnityEngine.Time.deltaTime;

                if (agent.velocity.sqrMagnitude < 0.01f)
                {
                    stuckElapsedTime += UnityEngine.Time.deltaTime;
                    if (stuckElapsedTime >= stuckCheckIntervalSeconds)
                    {
                        if (TryRecoverCurrentTrip())
                        {
                            return;
                        }
                    }
                }
                else
                {
                    stuckElapsedTime = 0f;
                }

                if (destinationElapsedTime >= reachTimeoutSeconds)
                {
                    if (TryRecoverCurrentTrip())
                    {
                        return;
                    }

                    FailShopping($"No alcanzó el destino a tiempo ({currentState}).");
                }
            }
        }

        /// <summary>
        /// Cliente de patrulla: espera el pago si aplica, pero no usa exit point; al terminar sigue la ruta.
        /// </summary>
        public void ConfigureForPatrolShopping()
        {
            startShoppingOnPlay = false;
            leaveAfterPurchase = true;
            useExitPoint = false;
            autoFindExitInScene = false;
            exitPoint = null;
        }

        /// <summary>Inicia el recorrido estantería → pay area.</summary>
        public void BeginShopping()
        {
            StopActionRoutine();
            inventory.Clear();
            depositedPayAreaItems.Clear();

            if (waitForShelfWithStock && !TryGetShelfWithStock(out _))
            {
                StartWaitingForShelf();
                return;
            }

            ContinueBeginShopping();
        }

        private void StartWaitingForShelf()
        {
            StopActionRoutine();
            currentState = CustomerState.WaitingForShelf;
            destinationElapsedTime = 0f;
            SetAgentStopped(true);
            SetWalking(false);
            actionRoutine = StartCoroutine(WaitForShelfThenShopRoutine());
        }

        private IEnumerator WaitForShelfThenShopRoutine()
        {
            while (!TryPrepareShelfAccess())
            {
                yield return new WaitForSeconds(shelfSearchRetryInterval);
            }

            actionRoutine = null;
            ContinueBeginShopping();
        }

        private bool TryPrepareShelfAccess()
        {
            if (!TryGetShelfWithStock(out _))
            {
                ReleaseTargetShelf();
                return false;
            }

            return TryReserveTargetShelf();
        }

        private void ContinueBeginShopping()
        {
            if (!ResolveShoppingTargets())
            {
                return;
            }

            if (!TryReserveTargetShelf())
            {
                StartWaitingForShelf();
                return;
            }

            destinationElapsedTime = 0f;
            ResetNavigationRecovery();
            currentState = CustomerState.MovingToShelf;

            if (!TryNavigateToShelf())
            {
                FailShopping("No se pudo calcular ruta hacia la estantería.");
                return;
            }

            SetWalking(true);
        }

        /// <summary>
        /// Asigna estantería y pay area: referencias del inspector o búsqueda en escena.
        /// </summary>
        public bool ResolveShoppingTargets()
        {
            if (autoFindInScene)
            {
                if (targetShelf == null || CountOccupiedSlots(targetShelf) <= 0)
                {
                    targetShelf = FindBestShelf();
                }

                if (targetPayArea == null)
                {
                    targetPayArea = FindBestPayArea();
                }
            }

            if (targetShelf == null || CountOccupiedSlots(targetShelf) <= 0)
            {
                ReleaseTargetShelf();

                if (waitForShelfWithStock)
                {
                    StartWaitingForShelf();
                }
                else
                {
                    Debug.LogError(
                        $"{nameof(ShopCustomerAgent)}: no hay {nameof(ShopShelf)} con stock en la escena.",
                        this);
                }

                return false;
            }

            if (!targetShelf.IsAvailableFor(this))
            {
                ReleaseTargetShelf();

                if (autoFindInScene)
                {
                    targetShelf = FindBestShelf();
                    shelfApproachPoint = null;
                }

                if (targetShelf == null || !targetShelf.IsAvailableFor(this))
                {
                    if (waitForShelfWithStock)
                    {
                        StartWaitingForShelf();
                    }

                    return false;
                }
            }

            if (targetPayArea == null)
            {
                Debug.LogError(
                    $"{nameof(ShopCustomerAgent)}: no hay {nameof(PayArea)} con espacio libre en la escena.",
                    this);
                return false;
            }

            if (shelfApproachPoint == null)
            {
                shelfApproachPoint = targetShelf.CustomerActionPoint != null
                    ? targetShelf.CustomerActionPoint
                    : FindApproachPoint(targetShelf.transform);
            }

            if (payAreaApproachPoint == null)
            {
                payAreaApproachPoint = targetPayArea.CustomerActionPoint != null
                    ? targetPayArea.CustomerActionPoint
                    : FindApproachPoint(targetPayArea.transform);
            }

            return true;
        }

        public bool TryDeliverItemsTo(PayArea payArea)
        {
            if (payArea == null || inventory.Count == 0)
            {
                return false;
            }

            bool deliveredAny = false;

            for (int i = inventory.Count - 1; i >= 0; i--)
            {
                InteractableShopItem item = inventory[i];
                if (item == null)
                {
                    inventory.RemoveAt(i);
                    continue;
                }

                if (!payArea.TryAddItem(item))
                {
                    break;
                }

                depositedPayAreaItems.Add(item);
                inventory.RemoveAt(i);
                deliveredAny = true;
            }

            return deliveredAny;
        }

        private void OnReachedNavigationTarget()
        {
            destinationElapsedTime = 0f;
            SetWalking(false);

            switch (currentState)
            {
                case CustomerState.MovingToShelf:
                    if (!IsNearShelf())
                    {
                        RetryNavigationToCurrentShoppingTarget();
                        return;
                    }

                    StopAgentForShoppingAction();
                    currentState = CustomerState.PickingFromShelf;
                    actionRoutine = StartCoroutine(PickItemsFromShelfRoutine());
                    break;
                case CustomerState.MovingToPayArea:
                    if (!IsNearPayArea())
                    {
                        RetryNavigationToCurrentShoppingTarget();
                        return;
                    }

                    StopAgentForShoppingAction();
                    currentState = CustomerState.Depositing;
                    actionRoutine = StartCoroutine(DepositAtPayAreaRoutine());
                    break;
                case CustomerState.Leaving:
                    FinishLeaving();
                    break;
            }
        }

        private bool TryCompleteShoppingApproach()
        {
            switch (currentState)
            {
                case CustomerState.MovingToShelf:
                    if (maxShelfArrivalDistance > 0f && IsNearShelf())
                    {
                        OnReachedNavigationTarget();
                        return true;
                    }

                    break;
                case CustomerState.MovingToPayArea:
                    if (maxPayAreaArrivalDistance > 0f && IsNearPayArea())
                    {
                        OnReachedNavigationTarget();
                        return true;
                    }

                    break;
            }

            return false;
        }

        private void StopAgentForShoppingAction()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
        }

        private IEnumerator PickItemsFromShelfRoutine()
        {
            yield return WaitUntilNearShelfOrFail();
            if (currentState != CustomerState.PickingFromShelf)
            {
                yield break;
            }

            yield return new WaitForSeconds(shelfActionDelay);

            int picked = 0;
            for (int i = 0; i < itemsToTake; i++)
            {
                if (!targetShelf.TryTakeItem(out InteractableShopItem item))
                {
                    break;
                }

                item.PrepareForCarry(carryAnchor);
                inventory.Add(item);
                picked++;

                if (i < itemsToTake - 1)
                {
                    yield return new WaitForSeconds(pickItemDelay);
                }
            }

            if (picked == 0)
            {
                ReleaseTargetShelf();

                if (waitForShelfWithStock)
                {
                    shelfApproachPoint = null;
                    if (autoFindInScene)
                    {
                        targetShelf = null;
                    }

                    StartWaitingForShelf();
                }
                else
                {
                    FailShopping("La estantería no tenía productos para recoger.");
                }

                yield break;
            }

            ReleaseTargetShelf();
            currentState = CustomerState.MovingToPayArea;
            destinationElapsedTime = 0f;
            ResetNavigationRecovery();

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            if (!TryNavigateToPayArea())
            {
                FailShopping("No se pudo calcular ruta hacia la pay area.");
                yield break;
            }

            SetWalking(true);
        }

        private IEnumerator DepositAtPayAreaRoutine()
        {
            yield return WaitUntilNearPayAreaOrFail();
            if (currentState != CustomerState.Depositing)
            {
                yield break;
            }

            yield return new WaitForSeconds(depositActionDelay);

            if (!TryDeliverItemsTo(targetPayArea))
            {
                FailShopping("No se pudieron depositar los ítems en la pay area.");
                yield break;
            }

            onShoppingFinished?.Invoke();

            if (!leaveAfterPurchase)
            {
                CompleteShopping();
                yield break;
            }

            if (waitForPaymentBeforeLeaving)
            {
                StartWaitingForPayment();
            }
            else if (ShouldNavigateToExit())
            {
                BeginLeaving();
            }
            else
            {
                CompleteShopping();
            }
        }

        private void StartWaitingForPayment()
        {
            if (!TryResolveCashRegister())
            {
                Debug.LogWarning(
                    $"{nameof(ShopCustomerAgent)}: no hay caja registradora para la pay area; se marcha sin esperar el cobro.",
                    this);

                if (ShouldNavigateToExit())
                {
                    BeginLeaving();
                }
                else
                {
                    CompleteShopping();
                }

                return;
            }

            cashRegister.AddPaymentSuccessListener(HandlePaymentSuccess);
            cashRegister.AddPaymentFailureListener(HandlePaymentFailure);
            isSubscribedToPayment = true;
            currentState = CustomerState.WaitingForPayment;
            SetAgentStopped(true);
            SetWalking(false);
        }

        private void HandlePaymentSuccess()
        {
            if (currentState != CustomerState.WaitingForPayment)
            {
                return;
            }

            UnsubscribeFromCashRegister();
            depositedPayAreaItems.Clear();

            if (ShouldNavigateToExit())
            {
                BeginLeaving();
            }
            else
            {
                CompleteShopping();
            }
        }

        private void HandlePaymentFailure()
        {
            if (currentState != CustomerState.WaitingForPayment)
            {
                return;
            }

            UnsubscribeFromCashRegister();
            DestroyDepositedPayAreaItems();

            if (ShouldNavigateToExit())
            {
                BeginLeaving();
            }
            else
            {
                CompleteShopping();
            }
        }

        private void DestroyDepositedPayAreaItems()
        {
            for (int i = depositedPayAreaItems.Count - 1; i >= 0; i--)
            {
                InteractableShopItem item = depositedPayAreaItems[i];
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            depositedPayAreaItems.Clear();
        }

        private bool ShouldNavigateToExit()
        {
            return useExitPoint && leaveAfterPurchase && TryResolveExitPoint();
        }

        private bool TryResolveCashRegister()
        {
            if (cashRegister != null)
            {
                return true;
            }

            if (!autoFindCashRegister || targetPayArea == null)
            {
                return false;
            }

            InteractableCashRegister[] registers =
                FindObjectsByType<InteractableCashRegister>(FindObjectsSortMode.None);

            foreach (InteractableCashRegister register in registers)
            {
                if (register != null && register.PayArea == targetPayArea)
                {
                    cashRegister = register;
                    return true;
                }
            }

            return false;
        }

        private void UnsubscribeFromCashRegister()
        {
            if (!isSubscribedToPayment || cashRegister == null)
            {
                return;
            }

            cashRegister.RemovePaymentSuccessListener(HandlePaymentSuccess);
            cashRegister.RemovePaymentFailureListener(HandlePaymentFailure);
            isSubscribedToPayment = false;
        }

        private void BeginLeaving()
        {
            UnsubscribeFromCashRegister();
            destinationElapsedTime = 0f;
            ResetNavigationRecovery();
            currentState = CustomerState.Leaving;

            if (!TryNavigateTo(exitPoint.position))
            {
                Debug.LogWarning(
                    $"{nameof(ShopCustomerAgent)}: no se pudo ir al punto de salida.",
                    this);
                CompleteShopping();
                return;
            }

            SetWalking(true);
        }

        private bool TryRecoverCurrentTrip()
        {
            if (recoverAttempts >= maxRecoverAttemptsPerTrip)
            {
                return false;
            }

            bool recovered = currentState switch
            {
                CustomerState.MovingToShelf => TryNavigateToShelf(),
                CustomerState.MovingToPayArea => TryNavigateToPayArea(),
                CustomerState.Leaving => exitPoint != null && TryNavigateTo(exitPoint.position),
                _ => false
            };

            if (!recovered)
            {
                return false;
            }

            recoverAttempts++;
            destinationElapsedTime = 0f;
            stuckElapsedTime = 0f;
            SetWalking(true);
            Debug.LogWarning(
                $"{nameof(ShopCustomerAgent)}: recuperando navegación ({recoverAttempts}/{maxRecoverAttemptsPerTrip}) en estado {currentState}.",
                this);
            return true;
        }

        private void ResetNavigationRecovery()
        {
            recoverAttempts = 0;
            stuckElapsedTime = 0f;
        }

        private void FinishLeaving()
        {
            SetWalking(false);
            SetAgentStopped(true);
            onLeftShop?.Invoke();
            CompleteShopping();
        }

        private bool TryResolveExitPoint()
        {
            if (exitPoint != null)
            {
                return true;
            }

            if (!autoFindExitInScene)
            {
                return false;
            }

            exitPoint = FindExitPoint();
            return exitPoint != null;
        }

        private Transform FindExitPoint()
        {
            CustomerExitPoint[] exits = FindObjectsByType<CustomerExitPoint>(FindObjectsSortMode.None);
            Transform best = null;
            float bestDistance = float.MinValue;

            foreach (CustomerExitPoint exit in exits)
            {
                if (exit == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, exit.transform.position);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = exit.transform;
                }
            }

            return best;
        }

        private bool TryNavigateToShelf()
        {
            if (targetShelf == null)
            {
                return false;
            }

            return TryNavigateTo(GetShelfArrivalPoint());
        }

        private bool TryNavigateToPayArea()
        {
            if (targetPayArea == null)
            {
                return false;
            }

            return TryNavigateTo(GetPayAreaArrivalPoint());
        }

        private bool TryNavigateTo(Vector3 worldPosition)
        {
            if (!EnsureAgentOnNavMesh())
            {
                return false;
            }

            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                worldPosition = hit.position;
            }

            agent.isStopped = false;
            agent.ResetPath();
            return agent.SetDestination(worldPosition);
        }

        private bool IsAgentOnNavMesh()
        {
            return agent != null && agent.isOnNavMesh;
        }

        private bool EnsureAgentOnNavMesh()
        {
            if (IsAgentOnNavMesh())
            {
                return true;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            if (!IsAgentOnNavMesh())
            {
                Debug.LogWarning(
                    $"{nameof(ShopCustomerAgent)}: el NPC no está sobre el NavMesh. Colócalo sobre el suelo horneado.",
                    this);
                return false;
            }

            return true;
        }

        private void SetAgentStopped(bool stopped)
        {
            if (!IsAgentOnNavMesh())
            {
                return;
            }

            agent.isStopped = stopped;
        }

        private bool TryGetShelfWithStock(out ShopShelf shelf)
        {
            if (autoFindInScene)
            {
                targetShelf = null;
                shelfApproachPoint = null;
                shelf = FindBestShelf();
            }
            else if (targetShelf != null && CountOccupiedSlots(targetShelf) > 0)
            {
                shelf = targetShelf;
            }
            else
            {
                shelf = null;
            }

            if (shelf != null)
            {
                targetShelf = shelf;
            }

            return shelf != null;
        }

        private ShopShelf FindBestShelf()
        {
            ShopShelf[] shelves = FindObjectsByType<ShopShelf>(FindObjectsSortMode.None);
            ShopShelf best = null;
            float bestScore = float.MinValue;

            foreach (ShopShelf shelf in shelves)
            {
                if (shelf == null)
                {
                    continue;
                }

                if (!shelf.IsAvailableFor(this))
                {
                    continue;
                }

                int available = CountOccupiedSlots(shelf);
                if (available <= 0)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, shelf.transform.position);
                float score = available >= itemsToTake ? 1000f : available * 100f;
                score -= distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = shelf;
                }
            }

            return best;
        }

        private PayArea FindBestPayArea()
        {
            PayArea[] payAreas = FindObjectsByType<PayArea>(FindObjectsSortMode.None);
            PayArea best = null;
            float bestScore = float.MinValue;
            int requiredSlots = Mathf.Max(1, itemsToTake);

            foreach (PayArea payArea in payAreas)
            {
                if (payArea == null)
                {
                    continue;
                }

                int freeSlots = CountFreePayAreaSlots(payArea);
                if (freeSlots <= 0)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, payArea.transform.position);
                float score = freeSlots >= requiredSlots ? 1000f : freeSlots * 100f;
                score -= distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = payArea;
                }
            }

            return best;
        }

        private Transform FindApproachPoint(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            CustomerApproachPoint marker = root.GetComponentInChildren<CustomerApproachPoint>(true);
            if (marker != null)
            {
                return marker.transform;
            }

            if (approachChildNames != null)
            {
                foreach (string childName in approachChildNames)
                {
                    if (string.IsNullOrWhiteSpace(childName))
                    {
                        continue;
                    }

                    Transform child = root.Find(childName);
                    if (child != null)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private bool TryGetStandOffPosition(Transform target, out Vector3 position)
        {
            position = target.position;

            Vector3 toAgent = transform.position - target.position;
            toAgent.y = 0f;

            if (toAgent.sqrMagnitude < 0.01f)
            {
                toAgent = -target.forward;
                toAgent.y = 0f;
            }

            if (toAgent.sqrMagnitude < 0.01f)
            {
                toAgent = Vector3.forward;
            }

            Vector3 candidate = target.position + toAgent.normalized * approachStandOffDistance;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            if (NavMesh.SamplePosition(target.position, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            return false;
        }

        private static int CountOccupiedSlots(ShopShelf shelf)
        {
            int count = 0;
            foreach (ShopShelfSlot slot in shelf.GetComponentsInChildren<ShopShelfSlot>())
            {
                if (slot != null && slot.IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountFreePayAreaSlots(PayArea payArea)
        {
            int count = 0;
            foreach (PayAreaSlot slot in payArea.GetComponentsInChildren<PayAreaSlot>())
            {
                if (slot != null && !slot.IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasReachedDestination()
        {
            if (!IsAgentOnNavMesh())
            {
                return false;
            }

            return currentState switch
            {
                CustomerState.MovingToShelf => maxShelfArrivalDistance > 0f
                    ? IsNearShelf()
                    : HasReachedNavMeshDestination(),
                CustomerState.MovingToPayArea => maxPayAreaArrivalDistance > 0f
                    ? IsNearPayArea()
                    : HasReachedNavMeshDestination(),
                CustomerState.Leaving => HasReachedNavMeshDestination(),
                _ => HasReachedNavMeshDestination()
            };
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

        private bool IsNearShelf()
        {
            if (targetShelf == null)
            {
                return false;
            }

            return IsWithinHorizontalDistance(
                transform.position,
                GetShelfInteractionCenter(),
                maxShelfArrivalDistance);
        }

        private bool IsNearPayArea()
        {
            if (targetPayArea == null)
            {
                return false;
            }

            return IsWithinHorizontalDistance(
                transform.position,
                GetPayAreaInteractionCenter(),
                maxPayAreaArrivalDistance);
        }

        private Vector3 GetShelfInteractionCenter()
        {
            if (targetShelf == null)
            {
                return transform.position;
            }

            if (targetShelf.CustomerActionPoint != null)
            {
                return targetShelf.CustomerActionPoint.position;
            }

            if (shelfApproachPoint != null)
            {
                return shelfApproachPoint.position;
            }

            return targetShelf.transform.position;
        }

        private Vector3 GetPayAreaInteractionCenter()
        {
            if (targetPayArea == null)
            {
                return transform.position;
            }

            if (targetPayArea.CustomerActionPoint != null)
            {
                return targetPayArea.CustomerActionPoint.position;
            }

            if (payAreaApproachPoint != null)
            {
                return payAreaApproachPoint.position;
            }

            return targetPayArea.transform.position;
        }

        private Vector3 GetShelfArrivalPoint()
        {
            if (shelfApproachPoint != null)
            {
                return shelfApproachPoint.position;
            }

            if (targetShelf != null && targetShelf.CustomerActionPoint != null)
            {
                return targetShelf.CustomerActionPoint.position;
            }

            if (targetShelf != null && TryGetStandOffPosition(targetShelf.transform, out Vector3 standOff))
            {
                return standOff;
            }

            return targetShelf != null ? targetShelf.transform.position : transform.position;
        }

        private Vector3 GetPayAreaArrivalPoint()
        {
            if (payAreaApproachPoint != null)
            {
                return payAreaApproachPoint.position;
            }

            if (targetPayArea != null && targetPayArea.CustomerActionPoint != null)
            {
                return targetPayArea.CustomerActionPoint.position;
            }

            if (targetPayArea != null && TryGetStandOffPosition(targetPayArea.transform, out Vector3 standOff))
            {
                return standOff;
            }

            return targetPayArea != null ? targetPayArea.transform.position : transform.position;
        }

        private static bool IsWithinHorizontalDistance(Vector3 from, Vector3 to, float maxDistance)
        {
            from.y = 0f;
            to.y = 0f;
            return (from - to).sqrMagnitude <= maxDistance * maxDistance;
        }

        private void RetryNavigationToCurrentShoppingTarget()
        {
            if (TryRecoverCurrentTrip())
            {
                SetWalking(true);
                return;
            }

            switch (currentState)
            {
                case CustomerState.MovingToShelf:
                    FailShopping(
                        $"No llegó lo bastante cerca de la estantería (máx. {maxShelfArrivalDistance:F1} m del punto de acción).");
                    break;
                case CustomerState.MovingToPayArea:
                    FailShopping(
                        $"No llegó lo bastante cerca de la pay area (máx. {maxPayAreaArrivalDistance:F1} m del punto de acción).");
                    break;
            }
        }

        private IEnumerator WaitUntilNearShelfOrFail()
        {
            float elapsed = 0f;

            while (!IsNearShelf())
            {
                if (!IsTravelingToDestination())
                {
                    TryNavigateToShelf();
                    SetWalking(true);
                }

                elapsed += Time.deltaTime;
                if (elapsed >= reachTimeoutSeconds)
                {
                    FailShopping(
                        $"No llegó lo bastante cerca de la estantería para recoger (máx. {maxShelfArrivalDistance:F1} m).");
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator WaitUntilNearPayAreaOrFail()
        {
            float elapsed = 0f;

            while (!IsNearPayArea())
            {
                if (!IsTravelingToDestination())
                {
                    TryNavigateToPayArea();
                    SetWalking(true);
                }

                elapsed += Time.deltaTime;
                if (elapsed >= reachTimeoutSeconds)
                {
                    FailShopping(
                        $"No llegó lo bastante cerca de la pay area para depositar (máx. {maxPayAreaArrivalDistance:F1} m).");
                    yield break;
                }

                yield return null;
            }
        }

        private bool IsTravelingToDestination()
        {
            if (currentState == CustomerState.MovingToShelf
                && maxShelfArrivalDistance > 0f
                && IsNearShelf())
            {
                return false;
            }

            if (currentState == CustomerState.MovingToPayArea
                && maxPayAreaArrivalDistance > 0f
                && IsNearPayArea())
            {
                return false;
            }

            return agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
        }

        private void SetWalking(bool walking)
        {
            if (isWalking == walking)
            {
                return;
            }

            isWalking = walking;

            if (walking)
            {
                onStartWalking?.Invoke();
            }
            else
            {
                onStartIdle?.Invoke();
            }
        }

        private void CompleteShopping()
        {
            ReleaseTargetShelf();
            currentState = CustomerState.Finished;
            SetAgentStopped(true);
            SetWalking(false);
            onShoppingTripEnded?.Invoke();
        }

        private void FailShopping(string reason)
        {
            Debug.LogWarning($"{nameof(ShopCustomerAgent)}: {reason}", this);
            StopActionRoutine();
            UnsubscribeFromCashRegister();
            DestroyDepositedPayAreaItems();
            ReleaseTargetShelf();
            currentState = CustomerState.Finished;
            SetAgentStopped(true);
            SetWalking(false);
            onShoppingTripEnded?.Invoke();
        }

        private bool TryReserveTargetShelf()
        {
            if (targetShelf == null)
            {
                return false;
            }

            return targetShelf.TryReserve(this);
        }

        private void ReleaseTargetShelf()
        {
            if (targetShelf == null)
            {
                return;
            }

            targetShelf.Release(this);
        }

        private void StopActionRoutine()
        {
            if (actionRoutine == null)
            {
                return;
            }

            StopCoroutine(actionRoutine);
            actionRoutine = null;
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

        public void AddOnShoppingTripEndedListener(UnityAction listener)
        {
            if (listener != null)
            {
                onShoppingTripEnded.AddListener(listener);
            }
        }

        public void RemoveOnShoppingTripEndedListener(UnityAction listener)
        {
            if (listener != null)
            {
                onShoppingTripEnded.RemoveListener(listener);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (shelfApproachPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(shelfApproachPoint.position, 0.25f);
                if (targetShelf != null)
                {
                    Gizmos.DrawLine(shelfApproachPoint.position, targetShelf.transform.position);
                }
            }

            if (payAreaApproachPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(payAreaApproachPoint.position, 0.25f);
                if (targetPayArea != null)
                {
                    Gizmos.DrawLine(payAreaApproachPoint.position, targetPayArea.transform.position);
                }
            }

            if (exitPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(exitPoint.position, 0.3f);
                Gizmos.DrawLine(transform.position, exitPoint.position);
            }
        }
#endif
    }
}
