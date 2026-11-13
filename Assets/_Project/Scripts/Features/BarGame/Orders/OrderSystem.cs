using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plain C# order logic: what a seated customer asks for, which items are unlocked,
    // and a signal when an order is paid.
    public class OrderSystem
    {
        private readonly OrderSettings _settings;
        private readonly bool[] _unlocked;

        public OrderSystem(OrderSettings settings, int totalRevenue)
        {
            _settings = settings;
            _unlocked = new bool[settings.Items != null ? settings.Items.Length : 0];
            UpdateUnlocks(totalRevenue, false);
        }

        public float ServiceWaitSeconds => _settings.ServiceWaitSeconds;

        public event Action Served;

        public event Action Abandoned;

        // Raised once per paid order with the total price and the items on the bill.
        public event Action<int, IReadOnlyList<MenuItem>> Paid;

        public event Action<MenuItem> ItemUnlocked;

        public MenuItem PickItem()
        {
            var available = CountUnlocked();
            if (available == 0)
            {
                Debug.LogError("OrderSystem has no unlocked menu items. Give at least one item UnlockRevenue 0.");
                return null;
            }

            var choice = UnityEngine.Random.Range(0, available);
            for (var i = 0; i < _unlocked.Length; i++)
            {
                if (_unlocked[i] && choice-- == 0)
                {
                    return _settings.Items[i];
                }
            }

            return null;
        }

        public void CompleteOrder(IReadOnlyList<MenuItem> items)
        {
            var total = 0;
            foreach (var item in items)
            {
                total += item.Price;
            }

            Paid?.Invoke(total, items);
        }

        // A customer started being served (before they eat and pay).
        public void NotifyServed()
        {
            Served?.Invoke();
        }

        // A customer left without paying: no seat, or ran out of patience.
        public void NotifyAbandoned()
        {
            Abandoned?.Invoke();
        }

        public void RefreshUnlocks(int totalRevenue)
        {
            UpdateUnlocks(totalRevenue, true);
        }

        private void UpdateUnlocks(int totalRevenue, bool announce)
        {
            for (var i = 0; i < _unlocked.Length; i++)
            {
                var item = _settings.Items[i];
                if (_unlocked[i] || totalRevenue < item.UnlockRevenue)
                {
                    continue;
                }

                _unlocked[i] = true;
                if (announce)
                {
                    ItemUnlocked?.Invoke(item);
                }
            }
        }

        private int CountUnlocked()
        {
            var count = 0;
            for (var i = 0; i < _unlocked.Length; i++)
            {
                if (_unlocked[i])
                {
                    count++;
                }
            }

            return count;
        }
    }
}
