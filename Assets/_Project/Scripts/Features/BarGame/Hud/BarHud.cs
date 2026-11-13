using System;
using Startup.Core.Localization;
using Startup.Core.SceneFlow;
using Startup.Core.UI;
using Startup.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // In-game HUD: money, customers served, clock and open/closed state, open/close, shop and pause buttons,
    // toasts, and the on-screen joystick.
    public class BarHud : MonoBehaviour
    {
        private const string UxmlPath = "UI/BarHud";
        private const long ToastMilliseconds = 2000;
        private const long UnlockToastMilliseconds = 4000;

        private readonly LocalizedBindings _bindings = new LocalizedBindings();

        private UIDocument _document;
        private IPlayerProfile _profile;
        private VisualElement _hintBar;
        private Label[] _decorLabels;
        private Action<object[]> _setBlockedReason;
        private TutorialPanel _tutorial;
        private SettingsPanel _settings;
        private Button _settingsButton;
        private EconomyManager _economy;
        private BarUpgrades _upgrades;
        private OrderSystem _orders;
        private ShiftClock _clock;
        private bool _paused;
        private bool _shopOpen;
        private bool _inventoryOpen;
        private ShopPanel _shop;
        private InventoryPanel _inventoryPanel;
        private PaymentPanel _payment;
        private Label _cashierPrompt;
        private StaffPanel _staff;
        private VisualElement _hintRows;
        private Button _inventoryButton;
        private Action<object[]> _setMoney;
        private Action<object[]> _setCustomers;
        private Action<object[]> _setTime;
        private ToastLabel _servedToast;
        private ToastLabel _unlockToast;
        private VisualElement _pauseOverlay;
        private Label _statusOpen;
        private Label _statusClosed;
        private Button _openButton;
        private Button _closeButton;
        private Button _shopButton;
        private Button _pauseButton;
        private Button _resumeButton;
        private Button _homeButton;

        public TouchControls Touch { get; private set; }

        public NameTagLayer NameTags { get; private set; }

        // Raised when the owner renames themselves in Settings.
        public event Action<string> OwnerNameChanged;

        // Raised when the owner picks an inventory item to put down.
        public event Action<FurnitureDef> PlaceRequested;

        // Set by the scene: true while a piece is being carried, when the inventory must stay closed.
        public Func<bool> IsCarrying { get; set; }

        public void ShowCashierPrompt(bool shown)
        {
            SetShown(_cashierPrompt, shown);
        }

        // Freezes the game while the owner counts out change; 'done' gets the total change given.
        public void OpenPayment(System.Collections.Generic.IReadOnlyList<MenuItem> items, int bill, int cash,
            Action<int> done)
        {
            Time.timeScale = 0f;
            _payment.Open(items, bill, cash, given =>
            {
                Time.timeScale = 1f;
                done(given);
            });
        }

        // Shows "How to play" once, the first time the player enters the bar.
        public void ShowTutorialOnce()
        {
            if (_profile.LoadTutorialSeen())
            {
                return;
            }

            _profile.SaveTutorialSeen();
            OpenMenu(_tutorial);
        }

        // True when this screen position (bottom-left origin) is over a HUD button or an open menu.
        public bool IsOverUi(Vector2 screenPosition)
        {
            var panel = _document != null ? _document.rootVisualElement.panel : null;
            return panel != null && TouchControls.IsOverUi(panel, TouchControls.ToPanel(panel, screenPosition));
        }

        public void Setup(EconomyManager economy, OrderSystem orders, BarUpgrades upgrades, ShiftClock clock,
            IPlayerProfile profile, BarInventory inventory)
        {
            var root = UiDocuments.Create(gameObject, UxmlPath, out _document);
            if (root == null)
            {
                return;
            }

            _economy = economy;
            _upgrades = upgrades;
            _orders = orders;
            _clock = clock;
            _profile = profile;

            NameTags = new NameTagLayer(root);
            BindStats(root);
            BindShift(root);
            BindPauseMenu(root);
            BindShop(root, upgrades, inventory);
            BindPayment(root);
            BindHint(root);
            BindDecorPrompt(root);
            BindMenus(root);
            Touch = new TouchControls(root);

            _economy.Changed += RefreshStats;
            LocalizationService.LanguageChanged += RefreshStats;
            _orders.Paid += OnOrderPaid;
            _orders.ItemUnlocked += OnItemUnlocked;
            _clock.MinuteChanged += RefreshTime;
            _clock.OpenChanged += RefreshShift;
            RefreshTime();
            RefreshShift();
        }

        private void Update()
        {
            Touch?.Tick();
            ReadHotkeys();
        }

        // With the cursor locked for mouse look, buttons cannot be clicked, so every action has a key too.
        private void ReadHotkeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _clock == null)
            {
                return;
            }

            // Counting change: the panel owns the keyboard until the customer is paid.
            if (_payment.IsOpen)
            {
                _payment.HandleKeys(keyboard);
                return;
            }

            // While a popup is open, letters belong to it (typing a name) and Esc closes it.
            var popup = OpenPopup();
            if (popup != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    popup.Close();
                }

                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_shopOpen)
                {
                    _shop.Close();
                }
                else if (_inventoryOpen)
                {
                    _inventoryPanel.Close();
                }
                else
                {
                    SetPaused(!_paused);
                }

                return;
            }

            if (_paused)
            {
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame && !_inventoryOpen)
            {
                if (_shopOpen)
                {
                    _shop.Close();
                }
                else
                {
                    OnShopClicked();
                }

                return;
            }

            if (keyboard.iKey.wasPressedThisFrame && !_shopOpen)
            {
                if (_inventoryOpen)
                {
                    _inventoryPanel.Close();
                }
                else
                {
                    OnInventoryClicked();
                }

                return;
            }

            if (keyboard.hKey.wasPressedThisFrame && _hintRows != null)
            {
                ToggleHint();
            }

            if (!_shopOpen && !_inventoryOpen && keyboard.oKey.wasPressedThisFrame)
            {
                _clock.SetOpen(!_clock.IsOpen);
            }
        }

        private OverlayPanel OpenPopup()
        {
            if (_tutorial.IsOpen)
            {
                return _tutorial;
            }

            if (_settings.IsOpen)
            {
                return _settings;
            }

            return _staff.IsOpen ? _staff : null;
        }

        // The staff card for one waiter; the game freezes while it is open like any other menu.
        public void OpenStaff(WaiterRecord record)
        {
            if (_payment.IsOpen || _shopOpen || _inventoryOpen)
            {
                return;
            }

            Time.timeScale = 0f;
            _staff.Open(record);
        }

        // Fixed labels, one shown at a time, so each keeps its own localization key. Order matches DecorPrompt.
        private void BindDecorPrompt(VisualElement root)
        {
            _decorLabels = new[]
            {
                root.Q<Label>("decor-pick-table"), root.Q<Label>("decor-pick-chair"), root.Q<Label>("decor-pick-item"),
                root.Q<Label>("decor-pick-seat"), root.Q<Label>("decor-pick-waiter"), root.Q<Label>("decor-busy"),
                root.Q<Label>("decor-carry-table"), root.Q<Label>("decor-carry-chair"), root.Q<Label>("decor-carry-item"),
                root.Q<Label>("decor-blocked"), root.Q<Label>("decor-sitting")
            };
            _bindings.Bind(_decorLabels[0], LocKeys.DecorPickTable);
            _bindings.Bind(_decorLabels[1], LocKeys.DecorPickChair);
            _bindings.Bind(_decorLabels[2], LocKeys.DecorPickItem);
            _bindings.Bind(_decorLabels[3], LocKeys.DecorPickSeat);
            _bindings.Bind(_decorLabels[4], LocKeys.DecorPickWaiter);
            _bindings.Bind(_decorLabels[5], LocKeys.DecorBusy);
            _bindings.Bind(_decorLabels[6], LocKeys.DecorCarryTable);
            _bindings.Bind(_decorLabels[7], LocKeys.DecorCarryChair);
            _bindings.Bind(_decorLabels[8], LocKeys.DecorCarryItem);
            _setBlockedReason = _bindings.BindWithArgs(_decorLabels[9], LocKeys.DecorBlocked, new object[] { string.Empty });
            _bindings.Bind(_decorLabels[10], LocKeys.DecorSitting);

            _cashierPrompt = root.Q<Label>("cashier-give-bill");
            _bindings.Bind(_cashierPrompt, LocKeys.CashierGiveBill);
        }

        public void ShowDecorPrompt(DecorPrompt prompt, PlaceCheck reason)
        {
            if (_decorLabels == null)
            {
                return;
            }

            if (prompt == DecorPrompt.Blocked)
            {
                _setBlockedReason(new object[] { LocalizationService.GetText(ReasonKey(reason)) });
            }

            for (var i = 0; i < _decorLabels.Length; i++)
            {
                SetShown(_decorLabels[i], (int)prompt == i + 1);
            }
        }

        private static string ReasonKey(PlaceCheck reason)
        {
            switch (reason)
            {
                case PlaceCheck.Wall: return LocKeys.DecorReasonWall;
                case PlaceCheck.Table: return LocKeys.DecorReasonTable;
                case PlaceCheck.Door: return LocKeys.DecorReasonDoor;
                case PlaceCheck.Queue: return LocKeys.DecorReasonQueue;
                case PlaceCheck.Staff: return LocKeys.DecorReasonStaff;
                case PlaceCheck.Decor: return LocKeys.DecorReasonDecor;
                case PlaceCheck.Counter: return LocKeys.DecorReasonCounter;
                case PlaceCheck.Owner: return LocKeys.DecorReasonOwner;
                case PlaceCheck.Chair: return LocKeys.DecorReasonChair;
                case PlaceCheck.Item: return LocKeys.DecorReasonItem;
                default: return LocKeys.DecorReasonUnderTable;
            }
        }

        // A small "H  Controls" chip is always on screen; H opens or closes the full list of keys above it, and the
        // choice is remembered. On phones the chip is replaced by the touch hint.
        private void BindHint(VisualElement root)
        {
            _hintBar = root.Q<VisualElement>("hint-bar");
            _hintBar.Clear();

            if (TouchControls.IsTouchPlatform)
            {
                var touch = new Label { pickingMode = PickingMode.Ignore };
                touch.AddToClassList("hint");
                _bindings.Bind(touch, LocKeys.HudHintTouch);
                _hintBar.Add(touch);
                return;
            }

            _hintRows = new VisualElement { pickingMode = PickingMode.Ignore };
            _hintRows.AddToClassList("hint-panel");
            foreach (var row in HintRows.All)
            {
                AddHintRow(_hintRows, row.Keys, row.LabelKey);
            }

            _hintBar.Add(_hintRows);

            var toggle = new VisualElement { pickingMode = PickingMode.Ignore };
            toggle.AddToClassList("hint-chip");
            AddHintRow(toggle, "H", LocKeys.HintToggle);
            _hintBar.Add(toggle);
            SetShown(_hintRows, !_profile.LoadHintHidden());
        }

        private void AddHintRow(VisualElement parent, string keys, string labelKey)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("hint-row");
            var cap = new Label(keys) { pickingMode = PickingMode.Ignore };
            cap.AddToClassList("keycap");
            var text = new Label { pickingMode = PickingMode.Ignore };
            text.AddToClassList("hint-text");
            _bindings.Bind(text, labelKey);
            row.Add(cap);
            row.Add(text);
            parent.Add(row);
        }

        private void ToggleHint()
        {
            var hide = _hintRows.style.display != DisplayStyle.None;
            SetShown(_hintRows, !hide);
            _profile.SaveHintHidden(hide);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            _bindings.Dispose();

            if (_economy == null)
            {
                return;
            }

            _economy.Changed -= RefreshStats;
            _orders.Paid -= OnOrderPaid;
            _orders.ItemUnlocked -= OnItemUnlocked;
            _clock.MinuteChanged -= RefreshTime;
            _clock.OpenChanged -= RefreshShift;

            _openButton.clicked -= OnOpenClicked;
            _closeButton.clicked -= OnCloseClicked;
            _shopButton.clicked -= OnShopClicked;
            _shop.Closed -= OnShopClosed;
            _shop.Dispose();
            _inventoryButton.clicked -= OnInventoryClicked;
            _inventoryPanel.Closed -= OnInventoryClosed;
            _inventoryPanel.PlaceRequested -= OnPlaceRequested;
            _inventoryPanel.Dispose();
            _payment.Dispose();
            _pauseButton.clicked -= OnPauseClicked;
            _resumeButton.clicked -= OnResumeClicked;
            _homeButton.clicked -= OnHomeClicked;

            _settingsButton.clicked -= OnSettingsClicked;
            _settings.OwnerNameChanged -= OnOwnerNameChanged;
            _settings.Closed -= OnMenuClosed;
            _tutorial.Closed -= OnMenuClosed;
            _staff.Closed -= OnMenuClosed;
            _staff.Dispose();
            LocalizationService.LanguageChanged -= RefreshStats;
            _settings.Dispose();
            _tutorial.Dispose();
        }

        // Settings and How to play are shared with the main menu. Opening either freezes the game.
        private void BindMenus(VisualElement root)
        {
            _tutorial = new TutorialPanel(root);
            _settings = new SettingsPanel(root, _profile, _tutorial);
            _settings.OwnerNameChanged += OnOwnerNameChanged;
            _tutorial.Closed += OnMenuClosed;
            _settings.Closed += OnMenuClosed;
            _staff = new StaffPanel(root, _upgrades);
            _staff.Closed += OnMenuClosed;

            _settingsButton = root.Q<Button>("btn-settings");
            _bindings.Bind(_settingsButton, LocKeys.MenuBtnSettings);
            _settingsButton.clicked += OnSettingsClicked;
        }

        private void OnSettingsClicked()
        {
            if (_payment.IsOpen)
            {
                return;
            }

            OpenMenu(_settings);
        }

        private void OpenMenu(OverlayPanel panel)
        {
            Time.timeScale = 0f;
            panel.Open();
        }

        // Back to the pause menu if it is open, otherwise back to the game.
        private void OnMenuClosed()
        {
            if (!_paused && !_shopOpen && !_inventoryOpen && !_settings.IsOpen && !_tutorial.IsOpen && !_staff.IsOpen)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnOwnerNameChanged(string name)
        {
            OwnerNameChanged?.Invoke(name);
        }

        private void LateUpdate()
        {
            NameTags?.Tick(Camera.main);
        }

        private void BindStats(VisualElement root)
        {
            _setMoney = _bindings.BindWithArgs(root.Q<Label>("money"), LocKeys.HudMoney,
                new object[] { Money.Format(_economy.Money) });
            _setCustomers = _bindings.BindWithArgs(root.Q<Label>("customers"), LocKeys.HudCustomers,
                new object[] { _economy.CustomersServed });
            _setTime = _bindings.BindWithArgs(root.Q<Label>("time"), LocKeys.HudTime, new object[] { _clock.TimeText });

            var served = root.Q<Label>("toast");
            _servedToast = new ToastLabel(served,
                _bindings.BindWithArgs(served, LocKeys.BarServed, new object[] { string.Empty }));

            var unlocked = root.Q<Label>("toast-unlock");
            _unlockToast = new ToastLabel(unlocked,
                _bindings.BindWithArgs(unlocked, LocKeys.BarUnlocked, new object[] { string.Empty }));
        }

        // Two labels and two buttons, one of each shown, so each text keeps a fixed localization key.
        private void BindShift(VisualElement root)
        {
            _statusOpen = root.Q<Label>("status-open");
            _statusClosed = root.Q<Label>("status-closed");
            _openButton = root.Q<Button>("btn-open");
            _closeButton = root.Q<Button>("btn-close");

            _bindings.Bind(_statusOpen, LocKeys.HudStatusOpen);
            _bindings.Bind(_statusClosed, LocKeys.HudStatusClosed);
            _bindings.Bind(_openButton, LocKeys.HudBtnOpen);
            _bindings.Bind(_closeButton, LocKeys.HudBtnClose);

            _openButton.clicked += OnOpenClicked;
            _closeButton.clicked += OnCloseClicked;
        }

        private void BindPauseMenu(VisualElement root)
        {
            _pauseOverlay = root.Q<VisualElement>("pause-overlay");
            _pauseButton = root.Q<Button>("btn-pause");
            _resumeButton = root.Q<Button>("btn-resume");
            _homeButton = root.Q<Button>("btn-home");

            _bindings.Bind(_pauseButton, LocKeys.HudBtnPause);
            _bindings.Bind(root.Q<Label>("pause-title"), LocKeys.HudPaused);
            _bindings.Bind(_resumeButton, LocKeys.CommonResume);
            _bindings.Bind(_homeButton, LocKeys.CommonHome);

            _pauseButton.clicked += OnPauseClicked;
            _resumeButton.clicked += OnResumeClicked;
            _homeButton.clicked += OnHomeClicked;
        }

        private void BindShop(VisualElement root, BarUpgrades upgrades, BarInventory inventory)
        {
            _shopButton = root.Q<Button>("btn-shop");
            _bindings.Bind(_shopButton, LocKeys.HudBtnShop);
            _shopButton.clicked += OnShopClicked;

            _shop = new ShopPanel(root, _economy, upgrades, inventory);
            _shop.Closed += OnShopClosed;

            _inventoryButton = root.Q<Button>("btn-inventory");
            _bindings.Bind(_inventoryButton, LocKeys.HudBtnInventory);
            _inventoryButton.clicked += OnInventoryClicked;

            _inventoryPanel = new InventoryPanel(root, inventory);
            _inventoryPanel.Closed += OnInventoryClosed;
            _inventoryPanel.PlaceRequested += OnPlaceRequested;
        }

        private void BindPayment(VisualElement root)
        {
            _payment = new PaymentPanel(root);
        }

        // Like the shop, the inventory freezes the game. It stays shut while a piece is in the owner's hands.
        private void OnInventoryClicked()
        {
            if (_payment.IsOpen || _paused || _shopOpen || _inventoryOpen || (IsCarrying != null && IsCarrying()))
            {
                return;
            }

            Time.timeScale = 0f;
            _inventoryOpen = true;
            _inventoryPanel.Open();
        }

        private void OnInventoryClosed()
        {
            _inventoryOpen = false;
            Time.timeScale = 1f;
        }

        private void OnPlaceRequested(FurnitureDef def)
        {
            PlaceRequested?.Invoke(def);
        }

        private void OnOpenClicked()
        {
            _clock.SetOpen(true);
        }

        private void OnCloseClicked()
        {
            _clock.SetOpen(false);
        }

        // The shop freezes the game like the pause menu, so the owner never walks behind it.
        private void OnShopClicked()
        {
            if (_payment.IsOpen || _inventoryOpen)
            {
                return;
            }

            Time.timeScale = 0f;
            _shopOpen = true;
            _shop.Open();
        }

        private void OnShopClosed()
        {
            _shopOpen = false;
            Time.timeScale = 1f;
        }

        private void RefreshStats()
        {
            _setMoney(new object[] { Money.Format(_economy.Money) });
            _setCustomers(new object[] { _economy.CustomersServed });
        }

        private void RefreshTime()
        {
            _setTime(new object[] { _clock.TimeText });
        }

        private void RefreshShift()
        {
            var open = _clock.IsOpen;
            SetShown(_statusOpen, open);
            SetShown(_closeButton, open);
            SetShown(_statusClosed, !open);
            SetShown(_openButton, !open);
        }

        private void OnPauseClicked()
        {
            if (_payment.IsOpen)
            {
                return;
            }

            SetPaused(true);
        }

        private void OnResumeClicked()
        {
            SetPaused(false);
        }

        private void OnHomeClicked()
        {
            SetPaused(false);
            SceneLoader.LoadSceneAsync(SceneNames.MainMenu);
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            _pauseOverlay.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnOrderPaid(int total, System.Collections.Generic.IReadOnlyList<MenuItem> items)
        {
            _servedToast.Show(new object[] { Money.Format(total) }, ToastMilliseconds);
        }

        private void OnItemUnlocked(MenuItem item)
        {
            _unlockToast.Show(new object[] { LocalizationService.GetText(item.NameKey) }, UnlockToastMilliseconds);
        }

        private static void SetShown(VisualElement element, bool shown)
        {
            if (element != null)
            {
                element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
