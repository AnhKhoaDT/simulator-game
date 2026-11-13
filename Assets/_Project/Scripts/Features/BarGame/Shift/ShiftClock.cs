using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // In-game time of day and whether the bar is open. The player opens and closes the shop whenever they want.
    public class ShiftClock : MonoBehaviour
    {
        public const int MinutesPerDay = 24 * 60;
        private const int MinutesPerHour = 60;

        private ShiftSettings _settings;
        private float _minutes;
        private int _lastWholeMinute = -1;

        public bool IsOpen { get; private set; }

        public int TotalMinutes => (int)_minutes;

        public int Hour => TotalMinutes / MinutesPerHour;

        public int Minute => TotalMinutes % MinutesPerHour;

        // 0..1 through the day; 0.5 is noon.
        public float DayFraction => _minutes / MinutesPerDay;

        public float SpawnRate => _settings.SpawnRate(Hour);

        public string TimeText => $"{Hour:00}:{Minute:00}";

        public event Action MinuteChanged;

        public event Action OpenChanged;

        // startMinutes < 0 means "no saved time": begin at the configured start hour.
        public void Setup(ShiftSettings settings, int startMinutes)
        {
            _settings = settings;
            _minutes = startMinutes >= 0 ? startMinutes % MinutesPerDay : settings.StartHour * MinutesPerHour;
        }

        public void SetOpen(bool open)
        {
            if (open == IsOpen)
            {
                return;
            }

            IsOpen = open;
            OpenChanged?.Invoke();
        }

        private void Update()
        {
            if (_settings == null)
            {
                return;
            }

            _minutes = (_minutes + Time.deltaTime * _settings.MinutesPerSecond) % MinutesPerDay;
            if (TotalMinutes != _lastWholeMinute)
            {
                _lastWholeMinute = TotalMinutes;
                MinuteChanged?.Invoke();
            }
        }
    }
}
