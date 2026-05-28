using UnityEngine.Events;

namespace ThreeDUnity.AI
{
    /// <summary>
    /// Fuente de eventos idle / caminar para sincronizar el <see cref="Animator"/> del NPC.
    /// </summary>
    public interface INpcWalkAnimationSource
    {
        void AddOnStartIdleListener(UnityAction listener);
        void RemoveOnStartIdleListener(UnityAction listener);
        void AddOnStartWalkingListener(UnityAction listener);
        void RemoveOnStartWalkingListener(UnityAction listener);
    }
}
