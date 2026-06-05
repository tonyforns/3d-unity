using UnityEngine;

namespace ThreeDUnity.Audio
{
    /// <summary>
    /// Identificador de clips de audio definidos en el proyecto. Añade entradas aquí y crea un <see cref="AudioClipDef"/> por cada una.
    /// </summary>
    public enum AudioClipId
    {
        None = 0,
        Enviroment = 1,
        DoorOpen = 2,
        DoorClose = 3,
        CoinsGather = 4,
        CardboardHit = 5,
        CoughShort = 6,
        CoughDouble = 7,
        Belch = 8,
        Fail = 9,
        PlayerWalking = 12,
    }

    /// <summary>
    /// Asocia un <see cref="AudioClip"/> con un <see cref="AudioClipId"/> para uso desde <see cref="AudioManager"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioClip",
        menuName = "3D Unity/Audio/Clip",
        order = 0)]
    public class AudioClipDef : ScriptableObject
    {
        [SerializeField] private AudioClipId id = AudioClipId.None;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float pitchMin = 0.95f;
        [SerializeField] private float pitchMax = 1.05f;

        public AudioClipId Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
    }
}
