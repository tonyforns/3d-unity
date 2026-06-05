using ThreeDUnity.AI;
using UnityEngine;

namespace ThreeDUnity.Audio
{
    /// <summary>
    /// Añade <see cref="NpcAmbientVocalAudio"/> a los agentes NPC presentes en escena al iniciar.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class NpcAmbientVocalAudioBootstrap : MonoBehaviour
    {
        [SerializeField] private bool attachOnStart = true;

        private void Start()
        {
            if (attachOnStart)
            {
                AttachToAllAgents();
            }
        }

        public void AttachToAllAgents()
        {
            AttachToAgents<ShopCustomerAgent>();
            AttachToAgents<FreeRoamNavAgent>();
            AttachToAgents<PatrolRouteNavAgent>();
        }

        private static void AttachToAgents<T>() where T : MonoBehaviour
        {
            T[] agents = FindObjectsByType<T>(FindObjectsSortMode.None);
            for (int i = 0; i < agents.Length; i++)
            {
                T agent = agents[i];
                if (agent == null)
                {
                    continue;
                }

                if (agent.GetComponent<NpcAmbientVocalAudio>() == null)
                {
                    agent.gameObject.AddComponent<NpcAmbientVocalAudio>();
                }
            }
        }
    }
}
