using UnityEngine;

namespace ThreeDUnity.Audio
{
    /// <summary>
    /// Loop de <see cref="AudioClipId.PlayerWalking"/> mientras hay input de movimiento en <see cref="MovePlayerController"/>.
    /// </summary>
    [RequireComponent(typeof(MovePlayerController))]
    public class PlayerWalkingAudio : MonoBehaviour
    {
        [SerializeField] private AudioClipId clipId = AudioClipId.PlayerWalking;

        private MovePlayerController movement;
        private AudioSource walkingSource;

        private void Awake()
        {
            movement = GetComponent<MovePlayerController>();
            var go = new GameObject("WalkingLoopAudio");
            go.transform.SetParent(transform, false);
            walkingSource = go.AddComponent<AudioSource>();
            walkingSource.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (walkingSource != null)
            {
                walkingSource.Stop();
            }
        }

        private void LateUpdate()
        {
            if (AudioManager.Instance == null || movement == null)
            {
                StopLoop();
                return;
            }

            bool shouldPlay = movement.HasMoveInput && movement.IsGrounded;

            if (!shouldPlay)
            {
                StopLoop();
                return;
            }

            if (walkingSource.isPlaying)
            {
                return;
            }

            if (!AudioManager.Instance.TryConfigureSourceFromCatalog(clipId, walkingSource, loop: true, randomizePitch: false))
            {
                return;
            }

            walkingSource.Play();
        }

        private void StopLoop()
        {
            if (walkingSource == null || !walkingSource.isPlaying)
            {
                return;
            }

            walkingSource.Stop();
            walkingSource.clip = null;
            walkingSource.loop = false;
        }
    }
}
