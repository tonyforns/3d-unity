using System.Collections;
using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Puerta que abre y cierra rotando en su pivote (rotación local).
    /// Coloca el script en el transform que debe girar (hijo de un pivote en la bisagra) o asigna <see cref="doorTransform"/>.
    /// </summary>
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableRotatingDoor : MonoBehaviour, IInteractable
    {
        [Header("Referencia")]
        [Tooltip("Objeto que rota. Si está vacío, usa este transform.")]
        [SerializeField] private Transform doorTransform;

        [Header("Rotación")]
        [Tooltip("Eje local respecto al cual gira la puerta (típicamente Y si el pivote está en la bisagra).")]
        [SerializeField] private Vector3 localRotationAxis = Vector3.up;

        [Tooltip("Ángulo en grados desde la posición cerrada hasta la abierta (regla de la mano derecha sobre el eje local).")]
        [SerializeField] private float openAngleDegrees = 90f;

        [Tooltip("Si está activo, la rotación actual en escena se considera la posición abierta al iniciar.")]
        [SerializeField] private bool startOpen;

        [Header("Animación")]
        [SerializeField] private float moveDurationSeconds = 0.6f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Texto UI")]
        [SerializeField] private string promptWhenClosed = "Abrir";
        [SerializeField] private string promptWhenOpen = "Cerrar";

        private Quaternion closedLocalRotation;
        private Quaternion openLocalRotation;
        private bool isOpen;
        private bool isMoving;

        private void Awake()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }
        }

        private void Start()
        {
            Vector3 axis = localRotationAxis.sqrMagnitude > 0.0001f
                ? localRotationAxis.normalized
                : Vector3.up;

            Quaternion current = doorTransform.localRotation;

            if (startOpen)
            {
                openLocalRotation = current;
                closedLocalRotation = current * Quaternion.AngleAxis(-openAngleDegrees, axis);
                isOpen = true;
            }
            else
            {
                closedLocalRotation = current;
                openLocalRotation = closedLocalRotation * Quaternion.AngleAxis(openAngleDegrees, axis);
            }
        }

        public string InteractionPrompt => isOpen ? promptWhenOpen : promptWhenClosed;

        public bool CanInteract(PlayerController interactor) => !isMoving && interactor != null;

        public void Interact(PlayerController interactor)
        {
            if (isMoving || doorTransform == null)
            {
                return;
            }

            Quaternion from = doorTransform.localRotation;
            Quaternion to = isOpen ? closedLocalRotation : openLocalRotation;

            if (moveDurationSeconds <= 0f)
            {
                doorTransform.localRotation = to;
                isOpen = !isOpen;
                return;
            }

            StartCoroutine(RotateRoutine(from, to));
        }

        private IEnumerator RotateRoutine(Quaternion from, Quaternion to)
        {
            isMoving = true;

            float duration = Mathf.Max(0.0001f, moveDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += UnityEngine.Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = easing != null && easing.length > 0 ? easing.Evaluate(t) : t;
                doorTransform.localRotation = Quaternion.SlerpUnclamped(from, to, eased);
                yield return null;
            }

            doorTransform.localRotation = to;
            isOpen = !isOpen;
            isMoving = false;
        }

        private void OnValidate()
        {
            if (moveDurationSeconds < 0f)
            {
                moveDurationSeconds = 0f;
            }

            if (easing == null || easing.length == 0)
            {
                easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
}
