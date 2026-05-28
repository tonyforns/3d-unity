using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.TimeSystem
{
    /// <summary>
    /// Reloj del juego: 24 horas de juego = 24 minutos reales (1 minuto real = 1 hora de juego).
    /// </summary>
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        /// <summary>Segundos reales que equivalen a una hora de juego (60 = 1 minuto real por hora).</summary>
        public const float DefaultRealSecondsPerGameHour = 60f;

        [Header("Hora inicial")]
        [SerializeField, Range(0f, 24f)] private float startingHour = 8f;
        [SerializeField, Min(1)] private int startingDay = 1;

        [Header("Velocidad")]
        [Tooltip("Segundos reales por cada hora de juego. 60 = un minuto real es una hora en el juego.")]
        [SerializeField, Min(0.01f)] private float realSecondsPerGameHour = DefaultRealSecondsPerGameHour;

        [Header("Día / noche (lógica)")]
        [SerializeField, Range(0f, 24f)] private float sunriseHour = 6f;
        [SerializeField, Range(0f, 24f)] private float sunsetHour = 20f;

        [Header("Eventos")]
        [SerializeField] private UnityEvent<float> onTimeOfDayChanged;
        [SerializeField] private UnityEvent<int> onHourChanged;
        [SerializeField] private UnityEvent<int> onDayChanged;

        /// <summary>Hora actual en [0, 24).</summary>
        public float TimeOfDayHours { get; private set; }

        /// <summary>Día del calendario del juego (1, 2, 3…).</summary>
        public int CurrentDay { get; private set; }

        /// <summary>Hora entera actual (0–23).</summary>
        public int CurrentHour => Mathf.FloorToInt(TimeOfDayHours) % 24;

        /// <summary>Minuto actual (0–59).</summary>
        public int CurrentMinute =>
            Mathf.FloorToInt((TimeOfDayHours - Mathf.Floor(TimeOfDayHours)) * 60f) % 60;

        /// <summary>Progreso del día en [0, 1).</summary>
        public float NormalizedTimeOfDay => TimeOfDayHours / 24f;

        public bool IsPaused { get; private set; }

        public bool IsDaytime
        {
            get
            {
                float hour = TimeOfDayHours;
                if (sunriseHour < sunsetHour)
                {
                    return hour >= sunriseHour && hour < sunsetHour;
                }

                return hour >= sunriseHour || hour < sunsetHour;
            }
        }

        public float SunriseHour => sunriseHour;

        private int _lastNotifiedHour = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(GameTimeManager)}] Ya existe una instancia en escena; se destruye el duplicado.",
                    this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            TimeOfDayHours = Mathf.Repeat(startingHour, 24f);
            CurrentDay = Mathf.Max(1, startingDay);
            _lastNotifiedHour = CurrentHour;
            NotifyTimeChanged();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (IsPaused)
            {
                return;
            }

            AdvanceTime(UnityEngine.Time.deltaTime);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        public void SetTimeOfDay(float hour, bool notifyListeners = true)
        {
            TimeOfDayHours = Mathf.Repeat(hour, 24f);
            _lastNotifiedHour = CurrentHour;

            if (notifyListeners)
            {
                NotifyTimeChanged();
            }
        }

        /// <summary>
        /// Salta al amanecer del día siguiente (p. ej. de las 22:00 a las 06:00) y notifica a los listeners.
        /// </summary>
        public void SleepUntilNextMorning(float wakeHour = -1f)
        {
            if (wakeHour < 0f)
            {
                wakeHour = sunriseHour;
            }

            wakeHour = Mathf.Repeat(wakeHour, 24f);
            CurrentDay++;
            TimeOfDayHours = wakeHour;
            _lastNotifiedHour = CurrentHour;

            onDayChanged?.Invoke(CurrentDay);
            NotifyTimeChanged();
            onHourChanged?.Invoke(CurrentHour);
        }

        public void AddTimeOfDayChangedListener(UnityAction<float> listener)
        {
            if (listener != null)
            {
                onTimeOfDayChanged.AddListener(listener);
            }
        }

        public void RemoveTimeOfDayChangedListener(UnityAction<float> listener)
        {
            if (listener != null)
            {
                onTimeOfDayChanged.RemoveListener(listener);
            }
        }

        public void AddHourChangedListener(UnityAction<int> listener)
        {
            if (listener != null)
            {
                onHourChanged.AddListener(listener);
            }
        }

        public void RemoveHourChangedListener(UnityAction<int> listener)
        {
            if (listener != null)
            {
                onHourChanged.RemoveListener(listener);
            }
        }

        public void AddDayChangedListener(UnityAction<int> listener)
        {
            if (listener != null)
            {
                onDayChanged.AddListener(listener);
            }
        }

        public void RemoveDayChangedListener(UnityAction<int> listener)
        {
            if (listener != null)
            {
                onDayChanged.RemoveListener(listener);
            }
        }

        private void AdvanceTime(float realDeltaSeconds)
        {
            float gameHoursDelta = realDeltaSeconds / realSecondsPerGameHour;
            TimeOfDayHours += gameHoursDelta;

            while (TimeOfDayHours >= 24f)
            {
                TimeOfDayHours -= 24f;
                CurrentDay++;
                onDayChanged?.Invoke(CurrentDay);
            }

            NotifyTimeChanged();

            int hourNow = CurrentHour;
            if (hourNow != _lastNotifiedHour)
            {
                _lastNotifiedHour = hourNow;
                onHourChanged?.Invoke(hourNow);
            }
        }

        private void NotifyTimeChanged()
        {
            onTimeOfDayChanged?.Invoke(TimeOfDayHours);
        }
    }
}
