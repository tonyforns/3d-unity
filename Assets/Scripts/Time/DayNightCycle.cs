using UnityEngine;

namespace ThreeDUnity.TimeSystem
{
    /// <summary>
    /// Rota la luz direccional (sol) y ajusta intensidad / color según la hora del <see cref="GameTimeManager"/>.
    /// </summary>
    [RequireComponent(typeof(GameTimeManager))]
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Light sunLight;
        [SerializeField] private GameTimeManager timeManager;

        [Header("Rotación del sol")]
        [SerializeField] private float sunAzimuthY = 170f;

        [Header("Luz del sol")]
        [SerializeField] private Gradient sunColorOverDay;
        [SerializeField] private AnimationCurve sunIntensityOverDay =
            AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1.2f);

        [Header("Luz ambiental")]
        [SerializeField] private bool driveAmbientLight = true;
        [SerializeField] private Gradient ambientSkyOverDay;
        [SerializeField] private Gradient ambientEquatorOverDay;
        [SerializeField] private Gradient ambientGroundOverDay;

        private void Reset()
        {
            sunLight = FindSunInScene();
            timeManager = GetComponent<GameTimeManager>();
            EnsureDefaultGradients();
        }

        private void Awake()
        {
            if (timeManager == null)
            {
                timeManager = GetComponent<GameTimeManager>();
            }

            if (timeManager == null)
            {
                timeManager = GameTimeManager.Instance;
            }

            if (sunLight == null)
            {
                sunLight = FindSunInScene();
            }

            EnsureDefaultGradients();

            if (sunLight != null)
            {
                RenderSettings.sun = sunLight;
            }
        }

        private void OnEnable()
        {
            if (timeManager != null)
            {
                timeManager.AddTimeOfDayChangedListener(HandleTimeOfDayChanged);
                HandleTimeOfDayChanged(timeManager.TimeOfDayHours);
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.RemoveTimeOfDayChangedListener(HandleTimeOfDayChanged);
            }
        }

        private void HandleTimeOfDayChanged(float timeOfDayHours)
        {
            float t = Mathf.Repeat(timeOfDayHours / 24f, 1f);

            if (sunLight != null)
            {
                float sunAngle = t * 360f - 90f;
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, sunAzimuthY, 0f);

                if (sunColorOverDay != null && sunColorOverDay.colorKeys.Length > 0)
                {
                    sunLight.color = sunColorOverDay.Evaluate(t);
                }

                sunLight.intensity = sunIntensityOverDay.Evaluate(t);
            }

            if (!driveAmbientLight)
            {
                return;
            }

            if (ambientSkyOverDay != null && ambientSkyOverDay.colorKeys.Length > 0)
            {
                RenderSettings.ambientSkyColor = ambientSkyOverDay.Evaluate(t);
            }

            if (ambientEquatorOverDay != null && ambientEquatorOverDay.colorKeys.Length > 0)
            {
                RenderSettings.ambientEquatorColor = ambientEquatorOverDay.Evaluate(t);
            }

            if (ambientGroundOverDay != null && ambientGroundOverDay.colorKeys.Length > 0)
            {
                RenderSettings.ambientGroundColor = ambientGroundOverDay.Evaluate(t);
            }
        }

        private static Light FindSunInScene()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    return light;
                }
            }

            return null;
        }

        private void EnsureDefaultGradients()
        {
            if (sunColorOverDay == null || sunColorOverDay.colorKeys.Length == 0)
            {
                sunColorOverDay = CreateDefaultSunColorGradient();
            }

            if (ambientSkyOverDay == null || ambientSkyOverDay.colorKeys.Length == 0)
            {
                ambientSkyOverDay = CreateDefaultAmbientSkyGradient();
            }

            if (ambientEquatorOverDay == null || ambientEquatorOverDay.colorKeys.Length == 0)
            {
                ambientEquatorOverDay = CreateDefaultAmbientEquatorGradient();
            }

            if (ambientGroundOverDay == null || ambientGroundOverDay.colorKeys.Length == 0)
            {
                ambientGroundOverDay = CreateDefaultAmbientGroundGradient();
            }

            if (sunIntensityOverDay == null || sunIntensityOverDay.length == 0)
            {
                sunIntensityOverDay = AnimationCurve.Linear(0f, 0.05f, 0.5f, 1.2f);
                sunIntensityOverDay.AddKey(1f, 0.05f);
            }
        }

        private static Gradient CreateDefaultSunColorGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.08f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0.25f),
                    new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.15f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.08f, 0.2f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateDefaultAmbientSkyGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.02f, 0.03f, 0.08f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.45f, 0.65f), 0.5f),
                    new GradientColorKey(new Color(0.02f, 0.03f, 0.08f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Gradient CreateDefaultAmbientEquatorGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.06f, 0.1f), 0f),
                    new GradientColorKey(new Color(0.25f, 0.28f, 0.32f), 0.5f),
                    new GradientColorKey(new Color(0.05f, 0.06f, 0.1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Gradient CreateDefaultAmbientGroundGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.03f), 0f),
                    new GradientColorKey(new Color(0.12f, 0.11f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.03f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
