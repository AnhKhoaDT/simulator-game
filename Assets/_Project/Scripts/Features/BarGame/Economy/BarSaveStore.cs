using Startup.Data;

namespace Startup.Features.BarGame
{
    // Talks only to IDataStore, so swapping local files for a server later changes nothing here.
    public class BarSaveStore
    {
        private const string Key = "bar_progress";

        private readonly IDataStore _store;

        public BarSaveStore(IDataStore store)
        {
            _store = store;
        }

        public void LoadInto(EconomyManager economy, BarUpgrades upgrades)
        {
            var data = _store.Load(Key, new BarSaveData());
            economy.Restore(data);
            upgrades.Restore(data);
        }

        public void Save(EconomyManager economy, BarUpgrades upgrades)
        {
            var data = new BarSaveData
            {
                Money = economy.Money,
                TotalRevenue = economy.TotalRevenue,
                CustomersServed = economy.CustomersServed,
                TablesOwned = upgrades.TablesOwned,
                WaiterLevel = upgrades.WaiterLevel
            };
            _store.Save(Key, data);
        }
    }
}
