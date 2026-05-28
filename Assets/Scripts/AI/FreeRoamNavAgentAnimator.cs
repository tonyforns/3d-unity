using UnityEngine;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Escucha eventos idle / caminar de agentes en el mismo GameObject y actualiza el <see cref="Animator"/>.
    /// Requiere un Animator Controller con el parámetro bool <c>IsWalking</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeRoamNavAgentAnimator : MonoBehaviour
    {
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

        [SerializeField] private Animator animator;

        private INpcWalkAnimationSource[] walkSources;

        private void Awake()
        {
            walkSources = GetComponents<INpcWalkAnimationSource>();

            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
        }

        private void OnEnable()
        {
            if (walkSources == null || walkSources.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(FreeRoamNavAgentAnimator)} requiere un componente que implemente {nameof(INpcWalkAnimationSource)}.",
                    this);
                return;
            }

            foreach (INpcWalkAnimationSource source in walkSources)
            {
                source.AddOnStartIdleListener(HandleStartIdle);
                source.AddOnStartWalkingListener(HandleStartWalking);
            }
        }

        private void OnDisable()
        {
            if (walkSources == null)
            {
                return;
            }

            foreach (INpcWalkAnimationSource source in walkSources)
            {
                source.RemoveOnStartIdleListener(HandleStartIdle);
                source.RemoveOnStartWalkingListener(HandleStartWalking);
            }
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
                Debug.LogWarning($"{nameof(FreeRoamNavAgentAnimator)} no encontró un {nameof(Animator)}.", this);
                return;
            }

            animator.SetBool(IsWalkingHash, isWalking);
        }
    }
}
