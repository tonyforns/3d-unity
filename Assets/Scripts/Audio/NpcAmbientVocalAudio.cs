using System.Collections;
using UnityEngine;

namespace ThreeDUnity.Audio
{
    /// <summary>
    /// Reproduce de forma aleatoria toses o eructo en la posición del NPC.
    /// </summary>
    public class NpcAmbientVocalAudio : MonoBehaviour
    {
        private static readonly AudioClipId[] VocalClips =
        {
            AudioClipId.CoughShort,
            AudioClipId.CoughDouble,
            AudioClipId.Belch,
        };

        [SerializeField, Min(1f)] private float minIntervalSeconds = 12f;
        [SerializeField, Min(1f)] private float maxIntervalSeconds = 35f;
        [SerializeField] private bool randomizePitch = true;

        private Coroutine vocalRoutine;

        private void OnEnable()
        {
            vocalRoutine = StartCoroutine(VocalLoopRoutine());
        }

        private void OnDisable()
        {
            if (vocalRoutine != null)
            {
                StopCoroutine(vocalRoutine);
                vocalRoutine = null;
            }
        }

        private IEnumerator VocalLoopRoutine()
        {
            float maxInterval = Mathf.Max(minIntervalSeconds, maxIntervalSeconds);
            yield return new WaitForSeconds(Random.Range(0f, maxInterval));

            while (isActiveAndEnabled)
            {
                float wait = Random.Range(minIntervalSeconds, maxIntervalSeconds);
                yield return new WaitForSeconds(wait);

                if (AudioManager.Instance == null)
                {
                    continue;
                }

                AudioClipId clip = VocalClips[Random.Range(0, VocalClips.Length)];
                AudioManager.Instance.PlayClip(clip, transform.position, usePitch: randomizePitch);
            }
        }

        private void OnValidate()
        {
            if (maxIntervalSeconds < minIntervalSeconds)
            {
                maxIntervalSeconds = minIntervalSeconds;
            }
        }
    }
}
