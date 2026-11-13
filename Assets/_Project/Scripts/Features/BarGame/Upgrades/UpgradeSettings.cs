using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Upgrade prices and effects, editable on BarSceneBuilder. Arrays are indexed by upgrade step.
    [Serializable]
    public class UpgradeSettings
    {
        [Tooltip("Tables already standing in a brand-new bar.")]
        [Range(0, 8)] public int StartTables = 2;
        [Tooltip("Price of the first hires, in order; later hires cost 40% more each time.")]
        public int[] WaiterPrices = { 300, 800, 1500 };
        [Min(0.5f)] public float WaiterWalkSpeed = 3.5f;
    }
}
