using System;
using Startup.Core.Localization;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Menu and patience values, editable on BarSceneBuilder in the Inspector.
    [Serializable]
    public class OrderSettings
    {
        [Min(1f)] public float ServiceWaitSeconds = 20f;
        public MenuItem[] Items = CreateDefaultItems();

        private static MenuItem[] CreateDefaultItems()
        {
            return new[]
            {
                new MenuItem(LocKeys.ItemSoda, 30, 3f, new Color(0.3f, 0.65f, 0.95f), ItemShape.Can),
                new MenuItem(LocKeys.ItemBeer, 50, 4f, new Color(0.95f, 0.7f, 0.15f), ItemShape.Bottle),
                new MenuItem(LocKeys.ItemSnack, 80, 5f, new Color(0.85f, 0.35f, 0.25f), ItemShape.Plate, 300),
                new MenuItem(LocKeys.ItemGrill, 150, 6f, new Color(0.55f, 0.3f, 0.15f), ItemShape.Skewers, 1000)
            };
        }
    }
}
