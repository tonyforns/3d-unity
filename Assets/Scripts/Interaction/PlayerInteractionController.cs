using ThreeDUnity.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Detecta un <see cref="IInteractable"/> frente al jugador y dispara <see cref="IInteractable.Interact"/> al pulsar la tecla.
    /// Añádelo al mismo GameObject que el controlador en primera persona y asigna la cámara del jugador.
    /// El raycast solo impacta la capa <see cref="InteractionLayers.InteractableName"/>.
    /// </summary>
    public class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform viewTransform;
        [SerializeField] private float maxDistance = 3f;
        [SerializeField] private LayerMask raycastMask;
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        public IInteractable CurrentTarget { get; private set; }

        public PlayerController Interactor => controller;

        public Key InteractKey => interactKey;

        public Transform ViewTransform => viewTransform;

        private IInteractionFocusable focusedInteractable;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            EnsureRaycastMask();
        }

        private void Reset()
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                viewTransform = cam.transform;
            }

            raycastMask = InteractionLayers.InteractableMask;
        }

        private void EnsureRaycastMask()
        {
            if (raycastMask.value != 0)
            {
                return;
            }

            raycastMask = InteractionLayers.InteractableMask;
        }

        private void Update()
        {
            if (CashRegisterPaymentUI.IsOpen)
            {
                return;
            }

            RefreshTarget();
            UpdateInteractionFocus();

            if (CurrentTarget == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (!keyboard[interactKey].wasPressedThisFrame)
            {
                return;
            }

            if (!CurrentTarget.CanInteract(controller))
            {
                return;
            }

            CurrentTarget.Interact(controller);
        }

        private void RefreshTarget()
        {
            if (viewTransform == null)
            {
                CurrentTarget = null;
                return;
            }

            Ray ray = new Ray(viewTransform.position, viewTransform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask, triggerInteraction))
            {
                CurrentTarget = null;
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            CurrentTarget = interactable;
        }

        private void UpdateInteractionFocus()
        {
            IInteractionFocusable desired = ResolveFocusable(CurrentTarget);

            if (ReferenceEquals(focusedInteractable, desired))
            {
                return;
            }

            focusedInteractable?.SetInteractionFocused(false);
            focusedInteractable = desired;
            focusedInteractable?.SetInteractionFocused(true);
        }

        private IInteractionFocusable ResolveFocusable(IInteractable target)
        {
            if (target == null || !target.CanInteract(controller))
            {
                return null;
            }

            return target as IInteractionFocusable;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (viewTransform == null)
            {
                return;
            }

            Gizmos.color = CurrentTarget != null ? Color.green : Color.cyan;
            Vector3 end = viewTransform.position + viewTransform.forward * maxDistance;
            Gizmos.DrawLine(viewTransform.position, end);
        }
#endif
    }
}
