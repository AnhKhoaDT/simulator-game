using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Startup.Features.BarGame
{
    // Sun, sky and ambient follow the clock; a warm lamp inside the bar fades in at night.
    public class DayLighting : MonoBehaviour
    {
        private const float DawnStart = 5f;
        private const float DawnEnd = 7f;
        private const float DuskStart = 18f;
        private const float DuskEnd = 20f;
        private const float HoursPerDay = 24f;
        private const float SunDayIntensity = 1.1f;
        private const float SunNightIntensity = 0.12f;
        private const float LampIntensity = 2.2f;
        private const float LampRange = 12f;
        private const float LampHeight = 2.6f;

        private static readonly Color DaySky = new Color(0.52f, 0.7f, 0.86f);
        private static readonly Color NightSky = new Color(0.03f, 0.04f, 0.08f);
        private static readonly Color DayAmbient = new Color(0.55f, 0.55f, 0.58f);
        private static readonly Color NightAmbient = new Color(0.2f, 0.22f, 0.32f);
        private static readonly Color DaySun = new Color(1f, 0.96f, 0.88f);
        private static readonly Color DuskSun = new Color(1f, 0.6f, 0.35f);
        private static readonly Color LampColor = new Color(1f, 0.78f, 0.5f);

        // Below this daylight, windows and street lamps switch to their lit color.
        private const float NightThreshold = 0.35f;

        private readonly List<NightLight> _nightLights = new List<NightLight>();

        private ShiftClock _clock;
        private Light _sun;
        private Light _lamp;
        private Camera _camera;
        private bool _isNight;
        private bool _applied;

        private readonly struct NightLight
        {
            public readonly Renderer Renderer;
            public readonly Material Day;
            public readonly Material Night;

            public NightLight(Renderer renderer, Material day, Material night)
            {
                Renderer = renderer;
                Day = day;
                Night = night;
            }
        }

        // A renderer that shows 'nightColor' after dark (lit windows, lamp bulbs).
        public void AddNightLight(Renderer renderer, Color dayColor, Color nightColor)
        {
            _nightLights.Add(new NightLight(renderer,
                PrimitiveFactory.GetMaterial(dayColor), PrimitiveFactory.GetMaterial(nightColor)));
            _applied = false;
        }

        public void Setup(ShiftClock clock, Light sun, Camera cam)
        {
            _clock = clock;
            _sun = sun;
            _camera = cam;

            var lamp = new GameObject("InteriorLamp");
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = Vector3.up * LampHeight;
            _lamp = lamp.AddComponent<Light>();
            _lamp.type = LightType.Point;
            _lamp.color = LampColor;
            _lamp.range = LampRange;
            _lamp.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (_clock == null)
            {
                return;
            }

            var hour = _clock.DayFraction * HoursPerDay;
            var daylight = Daylight(hour);
            var dusk = 1f - Mathf.Abs(daylight * 2f - 1f);

            _sun.intensity = Mathf.Lerp(SunNightIntensity, SunDayIntensity, daylight);
            _sun.color = Color.Lerp(DaySun, DuskSun, dusk);
            RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, daylight);
            _lamp.intensity = LampIntensity * (1f - daylight);

            if (_camera != null)
            {
                _camera.backgroundColor = Color.Lerp(NightSky, DaySky, daylight);
            }

            // Swap materials only when day turns to night or back, not every frame.
            var night = daylight < NightThreshold;
            if (night == _isNight && _applied)
            {
                return;
            }

            _isNight = night;
            _applied = true;
            foreach (var light in _nightLights)
            {
                light.Renderer.sharedMaterial = night ? light.Night : light.Day;
            }
        }

        // 0 at night, 1 in full day, with smooth dawn and dusk ramps.
        private static float Daylight(float hour)
        {
            if (hour < DawnStart || hour >= DuskEnd)
            {
                return 0f;
            }

            if (hour < DawnEnd)
            {
                return Mathf.SmoothStep(0f, 1f, (hour - DawnStart) / (DawnEnd - DawnStart));
            }

            if (hour >= DuskStart)
            {
                return Mathf.SmoothStep(1f, 0f, (hour - DuskStart) / (DuskEnd - DuskStart));
            }

            return 1f;
        }
    }
}
