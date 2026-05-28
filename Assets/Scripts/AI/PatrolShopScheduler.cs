using System.Collections;
using UnityEngine;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// NPC de patrulla que, cada 45–120 s (por defecto), pausa la ruta y ejecuta una compra con <see cref="ShopCustomerAgent"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PatrolShopScheduler : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private PatrolRouteNavAgent patrolAgent;
        [SerializeField] private ShopCustomerAgent shopCustomer;

        [Header("Programación")]
        [SerializeField] private bool enableShoppingTrips = true;
        [SerializeField, Min(0f)] private float minSecondsUntilShopping = 45f;
        [SerializeField, Min(0f)] private float maxSecondsUntilShopping = 120f;
        [SerializeField] private bool scheduleOnStart = true;

        private Coroutine scheduleRoutine;
        private bool isOnShoppingTrip;

        private void Awake()
        {
            if (patrolAgent == null)
            {
                patrolAgent = GetComponent<PatrolRouteNavAgent>();
            }

            if (shopCustomer == null)
            {
                shopCustomer = GetComponent<ShopCustomerAgent>();
            }

            if (shopCustomer != null)
            {
                shopCustomer.ConfigureForPatrolShopping();
            }
        }

        private void OnEnable()
        {
            if (shopCustomer != null)
            {
                shopCustomer.AddOnShoppingTripEndedListener(HandleShoppingTripEnded);
            }

            if (scheduleOnStart && enableShoppingTrips)
            {
                StartSchedule();
            }
        }

        private void OnDisable()
        {
            StopSchedule();

            if (shopCustomer != null)
            {
                shopCustomer.RemoveOnShoppingTripEndedListener(HandleShoppingTripEnded);
            }

            if (isOnShoppingTrip && patrolAgent != null)
            {
                patrolAgent.ResumePatrol();
                isOnShoppingTrip = false;
            }
        }

        public void StartSchedule()
        {
            StopSchedule();
            scheduleRoutine = StartCoroutine(ShoppingScheduleRoutine());
        }

        public void StopSchedule()
        {
            if (scheduleRoutine != null)
            {
                StopCoroutine(scheduleRoutine);
                scheduleRoutine = null;
            }
        }

        private IEnumerator ShoppingScheduleRoutine()
        {
            while (enableShoppingTrips)
            {
                float delay = Random.Range(
                    Mathf.Min(minSecondsUntilShopping, maxSecondsUntilShopping),
                    Mathf.Max(minSecondsUntilShopping, maxSecondsUntilShopping));

                yield return new WaitForSeconds(delay);

                if (!isActiveAndEnabled || shopCustomer == null || patrolAgent == null)
                {
                    continue;
                }

                if (shopCustomer.IsShopping || isOnShoppingTrip)
                {
                    continue;
                }

                BeginShoppingTrip();
            }
        }

        private void BeginShoppingTrip()
        {
            isOnShoppingTrip = true;
            patrolAgent.PausePatrol();
            shopCustomer.BeginShopping();
        }

        private void HandleShoppingTripEnded()
        {
            if (!isOnShoppingTrip)
            {
                return;
            }

            isOnShoppingTrip = false;

            if (patrolAgent != null && patrolAgent.isActiveAndEnabled)
            {
                patrolAgent.ResumePatrol();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxSecondsUntilShopping < minSecondsUntilShopping)
            {
                maxSecondsUntilShopping = minSecondsUntilShopping;
            }
        }
#endif
    }
}
