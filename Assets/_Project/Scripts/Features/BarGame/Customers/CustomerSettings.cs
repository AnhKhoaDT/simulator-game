using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Tunable customer flow values, editable on BarSceneBuilder in the Inspector.
    [Serializable]
    public class CustomerSettings
    {
        [Min(0.5f)] public float SpawnIntervalSeconds = 4f;
        [Range(1, 30)] public int MaxActive = 10;
        [Min(0.5f)] public float WalkSpeed = 2.5f;
        [Min(1f)] public float WaitForSeatSeconds = 10f;
    }
}
