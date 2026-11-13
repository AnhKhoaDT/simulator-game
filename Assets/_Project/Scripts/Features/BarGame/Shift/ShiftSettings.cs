using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Game-clock speed and how busy each part of the day is. A bar is busiest in the evening.
    [Serializable]
    public class ShiftSettings
    {
        [Tooltip("Game minutes that pass per real second. 2 = a full day in 12 minutes.")]
        [Min(0.1f)] public float MinutesPerSecond = 2f;
        [Range(0, 23)] public int StartHour = 16;

        [Header("Arrival rate by time of day (1 = normal)")]
        [Min(0f)] public float LunchRate = 1.3f;
        [Min(0f)] public float EveningRate = 2f;
        [Min(0f)] public float LateNightRate = 0.3f;

        public float SpawnRate(int hour)
        {
            if (hour >= 17 && hour < 23)
            {
                return EveningRate;
            }

            if (hour >= 11 && hour < 14)
            {
                return LunchRate;
            }

            return hour < 6 ? LateNightRate : 1f;
        }
    }
}
