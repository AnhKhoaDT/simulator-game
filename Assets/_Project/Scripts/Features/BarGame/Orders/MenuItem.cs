using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // One orderable item. NameKey is a String Table key, never display text.
    [Serializable]
    public class MenuItem
    {
        public string NameKey;
        [Min(1)] public int Price = 50;
        [Min(0.5f)] public float EatSeconds = 4f;
        public Color Color = Color.white;
        [Tooltip("Total revenue needed before customers start ordering this item. 0 = available from the start.")]
        [Min(0)] public int UnlockRevenue;

        public MenuItem()
        {
        }

        public MenuItem(string nameKey, int price, float eatSeconds, Color color, int unlockRevenue = 0)
        {
            NameKey = nameKey;
            Price = price;
            EatSeconds = eatSeconds;
            Color = color;
            UnlockRevenue = unlockRevenue;
        }
    }
}
