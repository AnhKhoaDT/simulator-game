using System;
using System.Collections.Generic;
using Startup.Core.Localization;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // The shop screen: furniture by category with a picture and price, plus the waiter under Staff.
    // Bought furniture goes into the inventory.
    public sealed class ShopPanel : CatalogScreen
    {
        private const string WaiterId = "waiter";
        private const string WaiterModel = CustomerFactory.ModelFolder + "q";

        private readonly EconomyManager _economy;
        private readonly BarUpgrades _upgrades;
        private readonly BarInventory _inventory;

        public ShopPanel(VisualElement root, EconomyManager economy, BarUpgrades upgrades, BarInventory inventory)
            : base(root, "shop-overlay")
        {
            _economy = economy;
            _upgrades = upgrades;
            _inventory = inventory;

            var entries = new List<Entry>();
            foreach (var def in FurnitureCatalog.All)
            {
                entries.Add(new Entry(def.Id, def.NameKey, def.Category, def.ModelPath, def));
            }

            entries.Add(new Entry(WaiterId, LocKeys.ShopUpgradeWaiter, ShopCategory.Staff, WaiterModel));
            Build(LocKeys.ShopTitle, LocKeys.InvEmpty, false, LocKeys.ShopBtnBuyPrice, true, entries,
                (ShopCategory[])Enum.GetValues(typeof(ShopCategory)));

            _economy.Changed += Refresh;
            _upgrades.Changed += Refresh;
            _inventory.Changed += Refresh;
        }

        public override void Dispose()
        {
            _economy.Changed -= Refresh;
            _upgrades.Changed -= Refresh;
            _inventory.Changed -= Refresh;
            base.Dispose();
        }

        protected override bool IsListed(Entry entry)
        {
            return true;
        }

        protected override string Badge(Entry entry)
        {
            return Money.Format(entry.Def != null ? entry.Def.Price : _upgrades.NextWaiterPrice);
        }

        protected override void UpdateDetail(Entry entry)
        {
            SetWallet(_economy.Money);
            if (entry.Def != null)
            {
                ShowOwned(_inventory.Count(entry.Id));
                SetAction(true, _economy.Money >= entry.Def.Price, entry.Def.Price);
                return;
            }

            ShowStaff(_upgrades.Waiters.Count);
            SetAction(true, _economy.Money >= _upgrades.NextWaiterPrice, _upgrades.NextWaiterPrice);
        }

        protected override void OnAction(Entry entry)
        {
            if (entry.Def == null)
            {
                _upgrades.TryHireWaiter(_economy);
            }
            else if (_economy.TrySpend(entry.Def.Price))
            {
                _inventory.Add(entry.Id);
            }
        }
    }
}
