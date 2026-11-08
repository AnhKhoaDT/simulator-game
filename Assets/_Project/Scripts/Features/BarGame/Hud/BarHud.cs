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
        private TutorialPanel _tutorial;
        private SettingsPanel _settings;
        private Button _settingsButton;
        private EconomyManager _economy;
        private OrderSystem _orders;
        private ShiftClock _clock;
        private bool _paused;
        private bool _shopOpen;
        private ShopPanel _shop;
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
            IPlayerProfile profile)
        {
            var root = UiDocuments.Create(gameObject, UxmlPath, out _document);
            if (root == null)
            {
                return;
            }

            _economy = economy;
            _orders = orders;
            _clock = clock;
            _profile = profile;

            NameTags = new NameTagLayer(root);
            BindStats(root);
            BindShift(root);
            BindPauseMenu(root);
            BindShop(root, upgrades);
            BindHint(root);
            BindMenus(root);
            Touch = new TouchControls(root);

            _economy.Changed += RefreshStats;
            _orders.Completed += OnOrderCompleted;
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

            // While a popup is open, letters belong to it (typing a name) and Esc closes it.
            if (_tutorial.IsOpen || _settings.IsOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    (_tutorial.IsOpen ? (OverlayPanel)_tutorial : _settings).Close();
                }

                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame && !_shopOpen)
            {
                SetPaused(!_paused);
                return;
            }

            if (_paused)
            {
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame)
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

            if (!_shopOpen && keyboard.oKey.wasPressedThisFrame)
            {
                _clock.SetOpen(!_clock.IsOpen);
            }
        }

        // Show the keyboard hint on PC and the touch hint on phones.
        private void BindHint(VisualElement root)
        {
            var pc = root.Q<Label>("hint-pc");
            var touch = root.Q<Label>("hint-touch");
            _bindings.Bind(pc, LocKeys.HudHintPc);
            _bindings.Bind(touch, LocKeys.HudHintTouch);
            SetShown(pc, !TouchControls.IsTouchPlatform);
            SetShown(touch, TouchControls.IsTouchPlatform);
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
            _orders.Completed -= OnOrderCompleted;
            _orders.ItemUnlocked -= OnItemUnlocked;
            _clock.MinuteChanged -= RefreshTime;
            _clock.OpenChanged -= RefreshShift;

            _openButton.clicked -= OnOpenClicked;
            _closeButton.clicked -= OnCloseClicked;
            _shopButton.clicked -= OnShopClicked;
            _shop.Closed -= OnShopClosed;
            _shop.Dispose();
            _pauseButton.clicked -= OnPauseClicked;
            _resumeButton.clicked -= OnResumeClicked;
            _homeButton.clicked -= OnHomeClicked;

            _settingsButton.clicked -= OnSettingsClicked;
            _settings.OwnerNameChanged -= OnOwnerNameChanged;
            _settings.Closed -= OnMenuClosed;
            _tutorial.Closed -= OnMenuClosed;
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

            _settingsButton = root.Q<Button>("btn-settings");
            _bindings.Bind(_settingsButton, LocKeys.MenuBtnSettings);
            _settingsButton.clicked += OnSettingsClicked;
        }

        private void OnSettingsClicked()
        {
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
            if (!_paused && !_shopOpen && !_settings.IsOpen && !_tutorial.IsOpen)
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
            _setMoney = _bindings.BindWithArgs(root.Q<Label>("money"), LocKeys.HudMoney, new object[] { _economy.Money });
            _setCustomers = _bindings.BindWithArgs(root.Q<Label>("customers"), LocKeys.HudCustomers,
                new object[] { _economy.CustomersServed });
            _setTime = _bindings.BindWithArgs(root.Q<Label>("time"), LocKeys.HudTime, new object[] { _clock.TimeText });

            var served = root.Q<Label>("toast");
            _servedToast = new ToastLabel(served,
                _bindings.BindWithArgs(served, LocKeys.BarServed, new object[] { string.Empty, 0 }));

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

        private void BindShop(VisualElement root, BarUpgrades upgrades)
        {
            _shopButton = root.Q<Button>("btn-shop");
            _bindings.Bind(_shopButton, LocKeys.HudBtnShop);
            _shopButton.clicked += OnShopClicked;

            _shop = new ShopPanel(root, _economy, upgrades);
            _shop.Closed += OnShopClosed;
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
            _setMoney(new object[] { _economy.Money });
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

        private void OnOrderCompleted(MenuItem item)
        {
            _servedToast.Show(new object[] { LocalizationService.GetText(item.NameKey), item.Price }, ToastMilliseconds);
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
