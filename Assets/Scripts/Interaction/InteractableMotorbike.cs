using ThreeDUnity.Interaction;
using UnityEngine;

[RequireComponent(typeof(InteractablePhysicsLayer))]
public class InteractableMotorbike : MonoBehaviour, IInteractable
{
    private const string DefaultAttachPointName = "BikeTransform";

    [Header("Rider Settings")]
    [SerializeField] private float mountedSpeedMultiplier = 2f;

    [Header("Mount alignment (local space del Character)")]
    [Tooltip("Opcional: hijo del Character. Si está asignado, usa su posición/rotación local.")]
    [SerializeField] private Transform riderAttachPoint;
    [Tooltip("Usado si riderAttachPoint está vacío. Ajusta en el Inspector hasta que la moto quede bien bajo los pies del jugador.")]
    [SerializeField] private Vector3 mountedLocalPosition = new(0f, -0.9f, 0.35f);
    [SerializeField] private Vector3 mountedLocalEulerAngles = Vector3.zero;

    private bool isMounted;
    private PlayerController mountedInteractor;
    private Transform originalParent;
    private float previousSpeedMultiplier = 1f;

    public string InteractionPrompt => isMounted ? "Bajarse" : "Usar";

    public bool CanInteract(PlayerController interactor)
    {
        if (interactor == null)
        {
            return false;
        }

        if (isMounted)
        {
            return interactor == mountedInteractor;
        }

        return true;
    }

    public void Interact(PlayerController interactor)
    {
        if (!isMounted)
        {
            Mount(interactor);
            return;
        }

        if (interactor == mountedInteractor)
        {
            Dismount(interactor);
        }
    }

    private void Mount(PlayerController interactor)
    {
        if (interactor == null)
        {
            return;
        }

        Transform playerRoot = interactor.transform;
        Transform attachPoint = ResolveAttachPoint(playerRoot);

        isMounted = true;
        mountedInteractor = interactor;
        originalParent = transform.parent;

        MovePlayerController mover = interactor.GetComponent<MovePlayerController>();
        if (mover != null)
        {
            previousSpeedMultiplier = mover.MoveSpeedMultiplier;
            mover.SetMoveSpeedMultiplier(mountedSpeedMultiplier);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(InteractableMotorbike)} no encontró {nameof(MovePlayerController)} en el jugador.",
                this);
        }

        transform.SetParent(playerRoot, false);
        ApplyMountAlignment(attachPoint);
    }

    private void Dismount(PlayerController interactor)
    {
        isMounted = false;

        transform.SetParent(originalParent, true);
        originalParent = null;

        if (interactor != null)
        {
            MovePlayerController mover = interactor.GetComponent<MovePlayerController>();
            if (mover != null)
            {
                mover.SetMoveSpeedMultiplier(previousSpeedMultiplier);
            }
        }

        mountedInteractor = null;
    }

    private Transform ResolveAttachPoint(Transform playerRoot)
    {
        if (riderAttachPoint != null)
        {
            if (IsOnMotorbikeHierarchy(riderAttachPoint))
            {
                Debug.LogError(
                    $"{nameof(InteractableMotorbike)}: riderAttachPoint no puede estar en la moto. " +
                    $"Usa un hijo de '{playerRoot.name}' o deja el campo vacío.",
                    this);
            }
            else if (riderAttachPoint.IsChildOf(playerRoot))
            {
                return riderAttachPoint;
            }
        }

        return playerRoot.Find(DefaultAttachPointName);
    }

    private void ApplyMountAlignment(Transform attachPoint)
    {
        if (attachPoint != null)
        {
            transform.localPosition = attachPoint.localPosition;
            transform.localRotation = attachPoint.localRotation;
            return;
        }

        transform.localPosition = mountedLocalPosition;
        transform.localRotation = Quaternion.Euler(mountedLocalEulerAngles);
    }

    private bool IsOnMotorbikeHierarchy(Transform point)
    {
        return point == transform || point.IsChildOf(transform);
    }
}
