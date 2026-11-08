using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Upgrade prices and effects, editable on BarSceneBuilder. Arrays are indexed by upgrade step.
    [Serializable]
    public class UpgradeSettings
    {
        [Range(1, 8)] public int StartTables = 2;
        [Tooltip("Price of the next table, in order. Length = how many tables can still be bought.")]
        public int[] TablePrices = { 200, 500 };
        [Tooltip("Price of each waiter level.")]
        public int[] WaiterPrices = { 300, 800, 1500 };
        [Tooltip("Seconds the waiter rests at the station before the next trip, per waiter level.")]
        public float[] WaiterIntervals = { 8f, 5f, 3f };
        [Min(0.5f)] public float WaiterWalkSpeed = 3.5f;
    }
}
