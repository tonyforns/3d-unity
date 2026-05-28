using System.Globalization;
using ThreeDUnity.TimeSystem;
using TMPro;
using UnityEngine;

namespace ThreeDUnity.UI
{
    /// <summary>
    /// Muestra la hora (y opcionalmente el día) del <see cref="GameTimeManager"/> en un <see cref="TMP_Text"/>.
    /// </summary>
    public class GameClockUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TMP_Text clockLabel;
        [SerializeField] private GameTimeManager timeManager;

        [Header("Formato")]
        [SerializeField] private bool use24HourClock = true;
        [SerializeField] private bool showDayNumber;
        [SerializeField] private string dayPrefix = "Día ";

        private void Awake()
        {
            if (clockLabel == null)
            {
                clockLabel = GetComponentInChildren<TMP_Text>(true);
            }

            if (timeManager == null)
            {
                timeManager = GameTimeManager.Instance;
            }

            if (timeManager == null)
            {
                timeManager = FindFirstObjectByType<GameTimeManager>();
            }
        }

        private void OnEnable()
        {
            if (timeManager == null)
            {
                return;
            }

            timeManager.AddTimeOfDayChangedListener(HandleTimeChanged);
            HandleTimeChanged(timeManager.TimeOfDayHours);
        }

        private void OnDisable()
        {
            if (timeManager == null)
            {
                return;
            }

            timeManager.RemoveTimeOfDayChangedListener(HandleTimeChanged);
        }

        private void HandleTimeChanged(float _)
        {
            if (clockLabel == null || timeManager == null)
            {
                return;
            }

            clockLabel.text = FormatClock(timeManager);
        }

        private string FormatClock(GameTimeManager manager)
        {
            int hour = manager.CurrentHour;
            int minute = manager.CurrentMinute;
            string timeText;

            if (use24HourClock)
            {
                timeText = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:00}:{1:00}",
                    hour,
                    minute);
            }
            else
            {
                int displayHour = hour % 12;
                if (displayHour == 0)
                {
                    displayHour = 12;
                }

                string period = hour < 12 ? "AM" : "PM";
                timeText = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1:00} {2}",
                    displayHour,
                    minute,
                    period);
            }

            if (!showDayNumber)
            {
                return timeText;
            }

            return $"{dayPrefix}{manager.CurrentDay}  {timeText}";
        }
    }
}
