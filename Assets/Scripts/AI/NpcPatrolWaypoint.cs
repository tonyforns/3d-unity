using UnityEngine;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Marca un punto de una ruta de patrulla. Colócalo en hijos de <see cref="NpcPatrolRoute"/>.
    /// </summary>
    public class NpcPatrolWaypoint : MonoBehaviour
    {
        [Tooltip("Espera en este punto. Si es negativo, usa el valor del agente de patrulla.")]
        [SerializeField, Min(-1f)] private float waitSeconds = -1f;

        public float WaitSeconds => waitSeconds;

        public bool HasCustomWait => waitSeconds >= 0f;
    }
}
