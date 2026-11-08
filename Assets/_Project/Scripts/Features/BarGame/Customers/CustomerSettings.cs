using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Tunable customer flow values, editable on BarSceneBuilder in the Inspector.
    [Serializable]
    public class CustomerSettings
    {
        [Tooltip("Base seconds between arrivals while open; the hour of day speeds it up or slows it down.")]
        [Min(0.5f)] public float SpawnIntervalSeconds = 5f;
        [Range(1, 30)] public int MaxActive = 10;
        [Min(0.5f)] public float WalkSpeed = 2.5f;
        [Min(1f)] public float WaitForSeatSeconds = 10f;
        [Tooltip("How long a customer waits at the cashier before leaving without paying.")]
        [Min(1f)] public float WaitToPaySeconds = 25f;
    }
}
