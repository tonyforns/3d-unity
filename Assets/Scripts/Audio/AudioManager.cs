using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUnity.Audio
{
    /// <summary>
    /// Singleton de audio: ambiente dedicado, pool de <see cref="AudioSource"/> para SFX y reproducción por <see cref="AudioClipId"/>.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Catálogo")]
        [SerializeField] private AudioClipDef[] clipDefinitions;

        [Header("Pool SFX")]
        [SerializeField, Min(1)] private int poolInitialSize = 8;
        [SerializeField] private Transform poolRoot;

        private readonly Dictionary<AudioClipId, AudioClipDef> clipById = new Dictionary<AudioClipId, AudioClipDef>();
        private readonly List<AudioSource> sfxPool = new List<AudioSource>();

        private AudioSource ambientSource;
        private Transform ambientRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildDictionary();
            EnsureAmbientSource();
            EnsurePoolRoot();
            PrewarmPool();
            PlayAmbient(AudioClipId.Enviroment);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Crea o devuelve el <see cref="AudioSource"/> usado exclusivamente para ambiente (un solo canal continuo).
        /// </summary>
        public AudioSource GetOrCreateAmbientAudioSource()
        {
            EnsureAmbientSource();
            return ambientSource;
        }

        /// <summary>
        /// Asigna y reproduce el clip de ambiente indicado en el AudioSource dedicado.
        /// </summary>
        public void PlayAmbient(AudioClipId id, bool loop = true)
        {
            if (!TryGetDef(id, out AudioClipDef def))
            {
                return;
            }

            AudioSource src = GetOrCreateAmbientAudioSource();
            src.loop = loop;
            src.pitch = 1f;
            src.clip = def.Clip;
            src.volume = def.Volume;
            src.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
                ambientSource.clip = null;
            }
        }

        /// <summary>
        /// Asigna clip, volumen y loop desde el catálogo a un <see cref="AudioSource"/> que controles tú (p. ej. pasos en loop del jugador).
        /// </summary>
        public bool TryConfigureSourceFromCatalog(AudioClipId id, AudioSource target, bool loop, bool randomizePitch)
        {
            if (target == null || !TryGetDef(id, out AudioClipDef def) || def.Clip == null)
            {
                return false;
            }

            target.clip = def.Clip;
            target.volume = def.Volume;
            target.loop = loop;
            target.pitch = randomizePitch ? Random.Range(def.PitchMin, def.PitchMax) : 1f;
            target.spatialBlend = 1f;
            target.playOnAwake = false;
            return true;
        }

        /// <summary>
        /// Reproduce un clip del catálogo usando el pool de SFX en la posición mundial indicada (audio 3D).
        /// </summary>
        public void PlayClip(AudioClipId id, Vector3 position, bool loop = false, bool usePitch = false)
        {
            if (!TryBeginPooledClip(id, position, loop, usePitch, out AudioSource src))
            {
                return;
            }

            src.Play();
        }

        /// <summary>
        /// Reproduce el clip en loop durante <paramref name="durationSeconds"/> y luego lo detiene y libera el emisor del pool.
        /// </summary>
        public void PlayClipLoopForDuration(AudioClipId id, Vector3 position, float durationSeconds, bool usePitch = false)
        {
            if (durationSeconds <= 0f)
            {
                Debug.LogWarning("[AudioManager] PlayClipLoopForDuration: durationSeconds debe ser mayor que 0.");
                return;
            }

            if (!TryBeginPooledClip(id, position, loop: true, usePitch, out AudioSource src))
            {
                return;
            }

            src.Play();
            StartCoroutine(StopPooledSourceAfter(src, durationSeconds));
        }

        private IEnumerator StopPooledSourceAfter(AudioSource src, float durationSeconds)
        {
            yield return new WaitForSeconds(durationSeconds);
            if (src == null)
            {
                yield break;
            }

            src.Stop();
            src.clip = null;
            src.loop = false;
        }

        private bool TryBeginPooledClip(AudioClipId id, Vector3 position, bool loop, bool usePitch, out AudioSource src)
        {
            src = null;
            if (!TryGetDef(id, out AudioClipDef def))
            {
                return false;
            }

            src = AcquirePooledSource();
            if (src == null)
            {
                return false;
            }

            src.transform.position = position;
            src.spatialBlend = 1f;
            src.loop = loop;
            src.clip = def.Clip;
            src.volume = def.Volume;
            src.pitch = usePitch ? Random.Range(def.PitchMin, def.PitchMax) : 1f;
            return true;
        }

        private void BuildDictionary()
        {
            clipById.Clear();

            if (clipDefinitions == null)
            {
                return;
            }

            for (int i = 0; i < clipDefinitions.Length; i++)
            {
                AudioClipDef def = clipDefinitions[i];
                if (def == null)
                {
                    continue;
                }

                if (def.Id == AudioClipId.None)
                {
                    Debug.LogWarning($"[AudioManager] {def.name} tiene AudioClipId.None; se omite del diccionario.", def);
                    continue;
                }

                if (clipById.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"[AudioManager] Id duplicado {def.Id} ({def.name} sobrescribe la entrada anterior).", def);
                }

                clipById[def.Id] = def;
            }
        }

        private bool TryGetDef(AudioClipId id, out AudioClipDef def)
        {
            if (id == AudioClipId.None)
            {
                Debug.LogWarning("[AudioManager] PlayClip/PlayAmbient con AudioClipId.None.");
                def = null;
                return false;
            }

            if (clipById.TryGetValue(id, out def))
            {
                if (def.Clip != null)
                {
                    return true;
                }

                Debug.LogWarning($"[AudioManager] AudioClipDef '{def.name}' no tiene AudioClip asignado.", def);
                return false;
            }

            Debug.LogWarning($"[AudioManager] No hay AudioClipDef registrado para {id}.");
            return false;
        }

        private void EnsureAmbientSource()
        {
            if (ambientSource != null)
            {
                return;
            }

            if (ambientRoot == null)
            {
                var go = new GameObject("AmbientAudio");
                go.transform.SetParent(transform, false);
                ambientRoot = go.transform;
            }

            ambientSource = ambientRoot.gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            var go = new GameObject("SFXPool");
            go.transform.SetParent(transform, false);
            poolRoot = go.transform;
        }

        private void PrewarmPool()
        {
            while (sfxPool.Count < poolInitialSize)
            {
                sfxPool.Add(CreatePooledSource());
            }
        }

        private AudioSource CreatePooledSource()
        {
            var go = new GameObject($"SFX_{sfxPool.Count}");
            go.transform.SetParent(poolRoot, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }

        private AudioSource AcquirePooledSource()
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                AudioSource candidate = sfxPool[i];
                if (candidate != null && !candidate.isPlaying)
                {
                    return candidate;
                }
            }

            AudioSource extra = CreatePooledSource();
            sfxPool.Add(extra);
            return extra;
        }
    }
}
