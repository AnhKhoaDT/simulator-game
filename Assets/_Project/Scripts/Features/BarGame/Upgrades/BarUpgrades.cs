using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plain C# staff state: the hired waiters, with purchase rules. Furniture lives in the inventory.
    public class BarUpgrades
    {
        private const int FallbackWaiterPrice = 300;
        private const float PriceGrowth = 1.4f;

        private readonly UpgradeSettings _settings;
        private readonly List<WaiterRecord> _waiters = new List<WaiterRecord>();

        public BarUpgrades(UpgradeSettings settings)
        {
            _settings = settings;
        }

        public IReadOnlyList<WaiterRecord> Waiters => _waiters;

        // The configured prices come first; after that each hire costs 40% more than the last.
        public int NextWaiterPrice
        {
            get
            {
                var prices = _settings.WaiterPrices;
                var hired = _waiters.Count;
                if (prices == null || prices.Length == 0)
                {
                    return FallbackWaiterPrice;
                }

                if (hired < prices.Length)
                {
                    return prices[hired];
                }

                return Mathf.RoundToInt(prices[prices.Length - 1] * Mathf.Pow(PriceGrowth, hired - prices.Length + 1));
            }
        }

        public event Action Changed;

        public bool TryHireWaiter(EconomyManager economy)
        {
            if (!economy.TrySpend(NextWaiterPrice))
            {
                return false;
            }

            _waiters.Add(new WaiterRecord());
            Changed?.Invoke();
            return true;
        }

        public void Dismiss(WaiterRecord waiter)
        {
            if (_waiters.Remove(waiter))
            {
                Changed?.Invoke();
            }
        }

        // After a waiter's name or resting flag was edited.
        public void NotifyWaiterEdited()
        {
            Changed?.Invoke();
        }

        public void Restore(BarSaveData data)
        {
            _waiters.Clear();
            if (data.Waiters != null)
            {
                _waiters.AddRange(data.Waiters);
            }

            // Saves from before waiters had names only stored a level.
            for (var i = _waiters.Count; i < data.WaiterLevel; i++)
            {
                _waiters.Add(new WaiterRecord());
            }

            Changed?.Invoke();
        }
    }
}
