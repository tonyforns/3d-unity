using UnityEngine;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Actualiza el <see cref="Animator"/> del NPC según camina o está quieto durante la compra.
    /// Requiere un Animator Controller con el parámetro bool <c>IsWalking</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopCustomerAgentAnimator : MonoBehaviour
    {
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

        [SerializeField] private ShopCustomerAgent customerAgent;
        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (customerAgent == null)
            {
                customerAgent = GetComponent<ShopCustomerAgent>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            if (customerAgent == null)
            {
                Debug.LogError($"{nameof(ShopCustomerAgentAnimator)} requiere un {nameof(ShopCustomerAgent)}.", this);
                return;
            }

            customerAgent.AddOnStartIdleListener(HandleStartIdle);
            customerAgent.AddOnStartWalkingListener(HandleStartWalking);
        }

        private void OnDisable()
        {
            if (customerAgent == null)
            {
                return;
            }

            customerAgent.RemoveOnStartIdleListener(HandleStartIdle);
            customerAgent.RemoveOnStartWalkingListener(HandleStartWalking);
        }

        private void HandleStartIdle()
        {
            SetWalking(false);
        }

        private void HandleStartWalking()
        {
            SetWalking(true);
        }

        private void SetWalking(bool isWalking)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsWalkingHash, isWalking);
        }
    }
}
