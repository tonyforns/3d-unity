using ThreeDUnity.TimeSystem;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Cama interactuable: de noche (después de las 18:00) el jugador duerme hasta el amanecer del día siguiente.
    /// </summary>
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableBed : MonoBehaviour, IInteractable
    {
        [Header("Referencias")]
        [SerializeField] private GameTimeManager timeManager;

        [Header("Horario")]
        [Tooltip("Hora mínima para dormir (inclusive). Con 18, se puede dormir desde las 18:00.")]
        [SerializeField, Range(0f, 24f)] private float sleepAvailableFromHour = 18f;
        [Tooltip("Hora a la que despiertas. -1 usa la hora de amanecer del GameTimeManager.")]
        [SerializeField] private float wakeHour = 6f;

        [Header("Interacción")]
        [SerializeField] private string promptCanSleep = "Dormir hasta el amanecer";
        [SerializeField] private string promptTooEarly = "Aún es pronto para dormir";

        [Header("Eventos")]
        [SerializeField] private UnityEvent onSlept;

        public string InteractionPrompt =>
            CanSleepNow() ? promptCanSleep : promptTooEarly;

        private void Awake()
        {
            if (timeManager == null)
            {
                timeManager = GameTimeManager.Instance;
            }

            if (timeManager == null)
            {
                timeManager = FindFirstObjectByType<GameTimeManager>();
            }
        }

        public bool CanInteract(PlayerController interactor)
        {
            return interactor != null && timeManager != null;
        }

        public void Interact(PlayerController interactor)
        {
            if (interactor == null || timeManager == null || !CanSleepNow())
            {
                return;
            }

            float targetWakeHour = wakeHour >= 0f ? wakeHour : timeManager.SunriseHour;
            timeManager.SleepUntilNextMorning(targetWakeHour);
            onSlept?.Invoke();
        }

        private bool CanSleepNow()
        {
            return timeManager.TimeOfDayHours >= sleepAvailableFromHour;
        }
    }
}
