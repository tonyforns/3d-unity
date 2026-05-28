using UnityEngine;

public class PlayerController : MonoBehaviour
{
    MovePlayerController moveController;

    private void Awake()
    {
        moveController = GetComponent<MovePlayerController>();
        if (moveController == null)
        {
            Debug.LogError($"{nameof(PlayerController)} requires a {nameof(MovePlayerController)} component.", this);
            enabled = false;
            return;
        }
    }
}
