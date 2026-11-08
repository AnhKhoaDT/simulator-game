using System;
using Startup.Core.Localization;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // Shop overlay: one row per upgrade with a localized name, price info and a buy button.
    public sealed class ShopPanel : IDisposable
    {
        private readonly LocalizedBindings _bindings = new LocalizedBindings();
        private readonly EconomyManager _economy;
        private readonly BarUpgrades _upgrades;
        private readonly VisualElement _overlay;
        private readonly Button _closeButton;
        private readonly UpgradeRow _tableRow;
        private readonly UpgradeRow _waiterRow;

        public ShopPanel(VisualElement root, EconomyManager economy, BarUpgrades upgrades)
        {
            _economy = economy;
            _upgrades = upgrades;
            _overlay = root.Q<VisualElement>("shop-overlay");

            _bindings.Bind(root.Q<Label>("shop-title"), LocKeys.ShopTitle);
            _closeButton = root.Q<Button>("btn-shop-close");
            _bindings.Bind(_closeButton, LocKeys.CommonClose);
            _closeButton.clicked += Close;

            _tableRow = new UpgradeRow(root, "table", LocKeys.ShopUpgradeTable, LocKeys.ShopInfoTable, _bindings);
            _waiterRow = new UpgradeRow(root, "waiter", LocKeys.ShopUpgradeWaiter, LocKeys.ShopInfoWaiter, _bindings);
            _tableRow.BuyClicked += BuyTable;
            _waiterRow.BuyClicked += HireWaiter;

            _economy.Changed += Refresh;
            _upgrades.Changed += Refresh;
            Refresh();
        }

        public event Action Closed;

        public void Open()
        {
            _overlay.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Close()
        {
            _overlay.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }

        public void Dispose()
        {
            _economy.Changed -= Refresh;
            _upgrades.Changed -= Refresh;
            _closeButton.clicked -= Close;
            _tableRow.BuyClicked -= BuyTable;
            _waiterRow.BuyClicked -= HireWaiter;
            _tableRow.Dispose();
            _waiterRow.Dispose();
            _bindings.Dispose();
        }

        private void BuyTable()
        {
            _upgrades.TryBuyTable(_economy);
        }

        private void HireWaiter()
        {
            _upgrades.TryHireWaiter(_economy);
        }

        private void Refresh()
        {
            _tableRow.Show(_upgrades.CanBuyTable, _upgrades.NextTablePrice, _economy.Money,
                new object[] { _upgrades.TablesOwned, _upgrades.MaxTables, _upgrades.NextTablePrice });
            _waiterRow.Show(_upgrades.CanHireWaiter, _upgrades.NextWaiterPrice, _economy.Money,
                new object[] { _upgrades.WaiterLevel, _upgrades.MaxWaiterLevel, _upgrades.NextWaiterPrice });
        }

        // One row of the shop UI (name, info, maxed label, buy button).
        private sealed class UpgradeRow : IDisposable
        {
            private readonly VisualElement _info;
            private readonly VisualElement _maxed;
            private readonly Button _buy;
            private readonly Action<object[]> _setInfo;

            public UpgradeRow(VisualElement root, string id, string nameKey, string infoKey, LocalizedBindings bindings)
            {
                bindings.Bind(root.Q<Label>(id + "-name"), nameKey);
                _info = root.Q<Label>(id + "-info");
                _setInfo = bindings.BindWithArgs(_info as Label, infoKey, new object[] { 0, 0, 0 });
                _maxed = root.Q<Label>(id + "-maxed");
                bindings.Bind(_maxed as Label, LocKeys.ShopMaxed);
                _buy = root.Q<Button>("btn-buy-" + id);
                bindings.Bind(_buy, LocKeys.ShopBtnBuy);
                _buy.clicked += OnBuyClicked;
            }

            public event Action BuyClicked;

            public void Show(bool canBuy, int price, int money, object[] infoArgs)
            {
                _info.style.display = canBuy ? DisplayStyle.Flex : DisplayStyle.None;
                _buy.style.display = canBuy ? DisplayStyle.Flex : DisplayStyle.None;
                _maxed.style.display = canBuy ? DisplayStyle.None : DisplayStyle.Flex;

                if (canBuy)
                {
                    _setInfo(infoArgs);
                    _buy.SetEnabled(money >= price);
                }
            }

            public void Dispose()
            {
                _buy.clicked -= OnBuyClicked;
            }

            private void OnBuyClicked()
            {
                BuyClicked?.Invoke();
            }
        }
    }
}
