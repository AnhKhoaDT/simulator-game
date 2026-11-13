using System;
using System.Collections.Generic;
using Startup.Core.Localization;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // A full-screen item browser shared by the shop and the inventory: category tabs on the left, a grid of picture
    // cards in the middle, and a detail pane with a big picture and one action button on the right.
    public abstract class CatalogScreen : IDisposable
    {
        private readonly VisualElement _overlay;
        private readonly List<Card> _cards = new List<Card>();
        private readonly List<Button> _tabs = new List<Button>();
        private readonly List<Action> _tabHandlers = new List<Action>();
        private readonly List<int> _tabCategories = new List<int>();

        private Button _closeButton;
        private Button _actionButton;
        private Label _emptyLabel;
        private Label _detailName;
        private VisualElement _detailIcon;
        private Label _ownedInfo;
        private Label _countInfo;
        private Label _staffInfo;
        private Label _maxedInfo;
        private Action<object[]> _setWallet;
        private Action<object[]> _setOwned;
        private Action<object[]> _setCount;
        private Action<object[]> _setStaff;
        private Action<object[]> _setAction;
        private int _category = AllCategories;
        private bool _iconsReady;
        private bool _isOpen;

        protected CatalogScreen(VisualElement root, string overlayName)
        {
            _overlay = root.Q<VisualElement>(overlayName);
            LocalizationService.LanguageChanged += OnLanguageChanged;
        }

        // Tab value meaning "every category".
        protected const int AllCategories = -1;

        protected LocalizedBindings Bindings { get; } = new LocalizedBindings();

        protected Entry Selected { get; private set; }

        public event Action Closed;

        public void Open()
        {
            _overlay.style.display = DisplayStyle.Flex;
            _isOpen = true;
            if (!_iconsReady)
            {
                LoadIcons();
            }

            Refresh();
        }

        public void Close()
        {
            _overlay.style.display = DisplayStyle.None;
            _isOpen = false;
            Closed?.Invoke();
        }

        public virtual void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            _closeButton.clicked -= Close;
            _actionButton.clicked -= OnActionClicked;
            for (var i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].clicked -= _tabHandlers[i];
            }

            foreach (var card in _cards)
            {
                card.Dispose();
            }

            Bindings.Dispose();
        }

        protected abstract bool IsListed(Entry entry);

        // Short text on the card corner: the price in the shop, the quantity in storage.
        protected abstract string Badge(Entry entry);

        // Fill the detail pane's info lines and action button for the selected entry.
        protected abstract void UpdateDetail(Entry entry);

        protected abstract void OnAction(Entry entry);

        // Re-reads everything shown; call whenever money, stock or levels change.
        protected void Refresh()
        {
            if (!_isOpen)
            {
                return;
            }

            var any = false;
            Card first = null;
            foreach (var card in _cards)
            {
                var visible = IsListed(card.Entry) && (_category == AllCategories || (int)card.Entry.Category == _category);
                card.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                card.Badge.text = Badge(card.Entry);
                if (visible)
                {
                    any = true;
                    first = first ?? card;
                }
            }

            _emptyLabel.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;
            if (Selected == null || !IsShown(Selected))
            {
                Select(first?.Entry);
            }
            else
            {
                ShowDetail();
            }
        }

        // Builds the whole screen. Subclasses call this once their own fields are ready.
        protected void Build(string titleKey, string emptyKey, bool allTab, string actionKey, bool showWallet,
            IEnumerable<Entry> entries, IEnumerable<ShopCategory> categories)
        {
            var top = Add(_overlay, "screen-top");
            var title = AddLabel(top, "screen-title");
            Bindings.Bind(title, titleKey);
            var wallet = AddLabel(top, "screen-wallet");
            _setWallet = Bindings.BindWithArgs(wallet, LocKeys.HudMoney, new object[] { string.Empty });
            wallet.style.display = showWallet ? DisplayStyle.Flex : DisplayStyle.None;
            _closeButton = new Button();
            _closeButton.AddToClassList("hud-btn");
            Bindings.Bind(_closeButton, LocKeys.CommonClose);
            _closeButton.clicked += Close;
            top.Add(_closeButton);

            var body = Add(_overlay, "screen-body");
            var tabs = Add(body, "screen-tabs");
            if (allTab)
            {
                AddTab(tabs, LocKeys.InvCatAll, AllCategories);
            }

            foreach (var category in categories)
            {
                AddTab(tabs, FurnitureCatalog.CategoryKey(category), (int)category);
            }

            var center = Add(body, "screen-center");
            var grid = new ScrollView(ScrollViewMode.Vertical);
            grid.AddToClassList("screen-grid");
            center.Add(grid);
            _emptyLabel = AddLabel(center, "screen-empty");
            Bindings.Bind(_emptyLabel, emptyKey);
            _emptyLabel.style.display = DisplayStyle.None;

            foreach (var entry in entries)
            {
                var card = new Card(entry, Bindings, Select);
                grid.Add(card.Root);
                _cards.Add(card);
            }

            BuildDetail(Add(body, "screen-detail"), actionKey);
            _category = allTab ? AllCategories : (int)ShopCategory.Seating;
            UpdateTabHighlight();
        }

        protected void SetWallet(int money)
        {
            _setWallet(new object[] { Money.Format(money) });
        }

        protected void ShowOwned(int count)
        {
            ShowOnly(_ownedInfo);
            _setOwned(new object[] { count });
        }

        protected void ShowCount(int count)
        {
            ShowOnly(_countInfo);
            _setCount(new object[] { count });
        }

        protected void ShowStaff(int hired)
        {
            ShowOnly(_staffInfo);
            _setStaff(new object[] { hired });
        }

        protected void ShowMaxed()
        {
            ShowOnly(_maxedInfo);
        }

        protected void SetAction(bool visible, bool enabled, int price)
        {
            _actionButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _actionButton.SetEnabled(enabled);
            _setAction(new object[] { price > 0 ? Money.Format(price) : string.Empty });
        }

        private void BuildDetail(VisualElement detail, string actionKey)
        {
            _detailIcon = Add(detail, "detail-icon");
            _detailName = AddLabel(detail, "detail-name");
            _ownedInfo = AddLabel(detail, "detail-info");
            _setOwned = Bindings.BindWithArgs(_ownedInfo, LocKeys.ShopOwned, new object[] { 0 });
            _countInfo = AddLabel(detail, "detail-info");
            _setCount = Bindings.BindWithArgs(_countInfo, LocKeys.InvCount, new object[] { 0 });
            _staffInfo = AddLabel(detail, "detail-info");
            _setStaff = Bindings.BindWithArgs(_staffInfo, LocKeys.ShopInfoWaiter, new object[] { 0 });
            _maxedInfo = AddLabel(detail, "detail-info");
            Bindings.Bind(_maxedInfo, LocKeys.ShopMaxed);

            _actionButton = new Button();
            _actionButton.AddToClassList("hud-btn");
            _actionButton.AddToClassList("detail-action");
            _setAction = Bindings.BindWithArgs(_actionButton, actionKey, new object[] { 0 });
            _actionButton.clicked += OnActionClicked;
            detail.Add(_actionButton);
        }

        private void AddTab(VisualElement tabs, string key, int category)
        {
            var tab = new Button();
            tab.AddToClassList("screen-tab");
            Bindings.Bind(tab, key);
            Action handler = () => SelectCategory(category);
            tab.clicked += handler;
            tabs.Add(tab);
            _tabs.Add(tab);
            _tabHandlers.Add(handler);
            _tabCategories.Add(category);
        }

        private void SelectCategory(int category)
        {
            _category = category;
            UpdateTabHighlight();
            Selected = null;
            Refresh();
        }

        private void UpdateTabHighlight()
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].EnableInClassList("screen-tab--active", _tabCategories[i] == _category);
            }
        }

        private void Select(Entry entry)
        {
            Selected = entry;
            foreach (var card in _cards)
            {
                card.Root.EnableInClassList("item-card--selected", card.Entry == entry);
            }

            ShowDetail();
        }

        private void ShowDetail()
        {
            var hasSelection = Selected != null;
            _detailIcon.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
            _detailName.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
            _actionButton.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
            ShowOnly(null);
            if (!hasSelection)
            {
                return;
            }

            _detailName.text = LocalizationService.GetText(Selected.NameKey);
            foreach (var card in _cards)
            {
                if (card.Entry == Selected)
                {
                    _detailIcon.style.backgroundImage = card.Icon.style.backgroundImage;
                }
            }

            UpdateDetail(Selected);
        }

        private bool IsShown(Entry entry)
        {
            return IsListed(entry) && (_category == AllCategories || (int)entry.Category == _category);
        }

        private void ShowOnly(Label line)
        {
            foreach (var info in new[] { _ownedInfo, _countInfo, _staffInfo, _maxedInfo })
            {
                info.style.display = info == line ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void LoadIcons()
        {
            _iconsReady = true;
            foreach (var card in _cards)
            {
                var icon = ItemIcons.Get(card.Entry.ModelPath);
                if (icon != null)
                {
                    card.Icon.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(icon));
                }
            }
        }

        // Names and prices are text built from the language, so rebuild them.
        private void OnLanguageChanged()
        {
            Refresh();
        }

        private void OnActionClicked()
        {
            if (Selected != null)
            {
                OnAction(Selected);
            }
        }

        private static VisualElement Add(VisualElement parent, string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        private static Label AddLabel(VisualElement parent, string className)
        {
            var label = new Label();
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        // One thing on sale or in storage. Furniture carries its catalog entry; the waiter does not.
        protected sealed class Entry
        {
            public Entry(string id, string nameKey, ShopCategory category, string modelPath, FurnitureDef def = null)
            {
                Id = id;
                NameKey = nameKey;
                Category = category;
                ModelPath = modelPath;
                Def = def;
            }

            public string Id { get; }

            public string NameKey { get; }

            public ShopCategory Category { get; }

            public string ModelPath { get; }

            public FurnitureDef Def { get; }
        }

        private sealed class Card : IDisposable
        {
            private readonly Action<Entry> _onClick;
            private readonly EventCallback<ClickEvent> _handler;

            public Card(Entry entry, LocalizedBindings bindings, Action<Entry> onClick)
            {
                Entry = entry;
                _onClick = onClick;

                Root = new VisualElement();
                Root.AddToClassList("item-card");

                Icon = new VisualElement();
                Icon.AddToClassList("item-icon");
                Root.Add(Icon);

                var name = new Label();
                name.AddToClassList("item-name");
                bindings.Bind(name, entry.NameKey);
                Root.Add(name);

                Badge = new Label();
                Badge.AddToClassList("item-badge");
                Root.Add(Badge);

                _handler = OnClick;
                Root.RegisterCallback(_handler);
            }

            public Entry Entry { get; }

            public VisualElement Root { get; }

            public VisualElement Icon { get; }

            public Label Badge { get; }

            public void Dispose()
            {
                Root.UnregisterCallback(_handler);
            }

            private void OnClick(ClickEvent evt)
            {
                _onClick(Entry);
            }
        }
    }
}
