using System;
using System.Collections.Generic;
using Startup.Core.Localization;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // The owner's storage screen: everything held but not placed, as picture cards. Place closes the screen
    // and starts carrying the item.
    public sealed class InventoryPanel : CatalogScreen
    {
        private readonly BarInventory _inventory;

        public InventoryPanel(VisualElement root, BarInventory inventory) : base(root, "inventory-overlay")
        {
            _inventory = inventory;

            var entries = new List<Entry>();
            foreach (var def in FurnitureCatalog.All)
            {
                entries.Add(new Entry(def.Id, def.NameKey, def.Category, def.ModelPath, def));
            }

            var categories = new List<ShopCategory>();
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
            {
                if (category != ShopCategory.Staff)
                {
                    categories.Add(category);
                }
            }

            Build(LocKeys.InvTitle, LocKeys.InvEmpty, true, LocKeys.InvBtnPlace, false, entries, categories);
            _inventory.Changed += Refresh;
        }

        // Raised when the owner picks an item to put down; the screen is already closed by then.
        public event Action<FurnitureDef> PlaceRequested;

        public override void Dispose()
        {
            _inventory.Changed -= Refresh;
            base.Dispose();
        }

        protected override bool IsListed(Entry entry)
        {
            return _inventory.Count(entry.Id) > 0;
        }

        protected override string Badge(Entry entry)
        {
            return "x" + _inventory.Count(entry.Id);
        }

        protected override void UpdateDetail(Entry entry)
        {
            ShowCount(_inventory.Count(entry.Id));
            SetAction(true, true, 0);
        }

        protected override void OnAction(Entry entry)
        {
            Close();
            PlaceRequested?.Invoke(entry.Def);
        }
    }
}
