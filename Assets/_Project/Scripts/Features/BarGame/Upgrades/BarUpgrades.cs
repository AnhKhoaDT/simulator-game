using System;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plain C# upgrade state: how many tables are owned and the waiter level, with purchase rules.
    public class BarUpgrades
    {
        private readonly UpgradeSettings _settings;

        public BarUpgrades(UpgradeSettings settings)
        {
            _settings = settings;
            TablesOwned = settings.StartTables;
        }

        public int TablesOwned { get; private set; }

        public int WaiterLevel { get; private set; }

        public int MaxTables => _settings.StartTables + _settings.TablePrices.Length;

        public int MaxWaiterLevel => _settings.WaiterPrices.Length;

        public bool CanBuyTable => TablesOwned < MaxTables;

        public bool CanHireWaiter => WaiterLevel < MaxWaiterLevel;

        public int NextTablePrice => CanBuyTable ? _settings.TablePrices[TablesOwned - _settings.StartTables] : 0;

        public int NextWaiterPrice => CanHireWaiter ? _settings.WaiterPrices[WaiterLevel] : 0;

        // Seconds between auto-serves; 0 means no waiter hired yet.
        public float WaiterInterval
        {
            get
            {
                var intervals = _settings.WaiterIntervals;
                if (WaiterLevel == 0 || intervals.Length == 0)
                {
                    return 0f;
                }

                return intervals[Mathf.Min(WaiterLevel, intervals.Length) - 1];
            }
        }

        public event Action Changed;

        public bool TryBuyTable(EconomyManager economy)
        {
            if (!CanBuyTable || !economy.TrySpend(NextTablePrice))
            {
                return false;
            }

            TablesOwned++;
            Changed?.Invoke();
            return true;
        }

        public bool TryHireWaiter(EconomyManager economy)
        {
            if (!CanHireWaiter || !economy.TrySpend(NextWaiterPrice))
            {
                return false;
            }

            WaiterLevel++;
            Changed?.Invoke();
            return true;
        }

        public void Restore(BarSaveData data)
        {
            TablesOwned = Mathf.Clamp(data.TablesOwned, _settings.StartTables, MaxTables);
            WaiterLevel = Mathf.Clamp(data.WaiterLevel, 0, MaxWaiterLevel);
            Changed?.Invoke();
        }
    }
}
