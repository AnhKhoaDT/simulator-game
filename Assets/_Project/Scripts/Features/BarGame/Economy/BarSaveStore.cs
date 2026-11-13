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

        // Restores economy, upgrades and inventory; the caller reads the returned data for the clock and layout.
        public BarSaveData LoadInto(EconomyManager economy, BarUpgrades upgrades, BarInventory inventory)
        {
            var data = _store.Load(Key, new BarSaveData());
            economy.Restore(data);
            upgrades.Restore(data);
            inventory.Restore(data.InventoryItems);
            return data;
        }

        public void Save(EconomyManager economy, BarUpgrades upgrades, ShiftClock clock, BarFurniture furniture,
            BarInventory inventory)
        {
            var data = new BarSaveData
            {
                LayoutSaved = furniture != null,
                DecorPlacements = furniture != null ? furniture.SnapshotDecor() : new System.Collections.Generic.List<DecorSave>(),
                InventoryItems = inventory.Snapshot(),
                TablePlacements = furniture != null ? furniture.SnapshotTables() : new System.Collections.Generic.List<UnityEngine.Vector3>(),
                ChairPlacements = furniture != null ? furniture.SnapshotChairs() : new System.Collections.Generic.List<UnityEngine.Vector3>(),
                Money = economy.Money,
                TotalRevenue = economy.TotalRevenue,
                CustomersServed = economy.CustomersServed,
                WaiterLevel = 0,
                Waiters = new System.Collections.Generic.List<WaiterRecord>(upgrades.Waiters),
                DecorSaved = furniture != null,
                ClockMinutes = clock != null ? clock.TotalMinutes : -1
            };
            _store.Save(Key, data);
        }
    }
}
