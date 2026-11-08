using System;
using Startup.Core.Localization;
using Startup.Core.SceneFlow;
using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // In-game HUD: money, customers served, play time, pause menu and a short "served" toast.
    public class BarHud : MonoBehaviour
    {
        private const string UxmlPath = "UI/BarHud";
        private const string PanelSettingsPath = "UI/MainPanelSettings";
        private const long ToastMilliseconds = 2000;
        private const long UnlockToastMilliseconds = 4000;
        private const int SecondsPerMinute = 60;

        private readonly LocalizedBindings _bindings = new LocalizedBindings();

        private EconomyManager _economy;
        private OrderSystem _orders;
        private ShopPanel _shop;
        private Button _shopButton;
        private Action<object[]> _setMoney;
        private Action<object[]> _setCustomers;
        private Action<object[]> _setTime;
        private ToastLabel _servedToast;
        private ToastLabel _unlockToast;
        private VisualElement _pauseOverlay;
        private Button _pauseButton;
        private Button _resumeButton;
        private Button _homeButton;
        private float _elapsed;
        private int _shownSeconds = -1;

        public void Setup(EconomyManager economy, OrderSystem orders, BarUpgrades upgrades)
        {
            var uxml = Resources.Load<VisualTreeAsset>(UxmlPath);
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (uxml == null || panelSettings == null)
            {
                Debug.LogError($"BarHud could not load UI from Resources ('{UxmlPath}', '{PanelSettingsPath}').");
                return;
            }

            _economy = economy;
            _orders = orders;

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;
            var root = document.rootVisualElement;
            StretchToScreen(root);

            BindStats(root);
            BindPauseMenu(root);
            BindShop(root, upgrades);

            _economy.Changed += RefreshStats;
            _orders.Completed += OnOrderCompleted;
            _orders.ItemUnlocked += OnItemUnlocked;
        }

        private void Update()
        {
            if (_setTime == null)
            {
                return;
            }

            // Scaled time, so the clock stops while paused.
            _elapsed += Time.deltaTime;
            var seconds = (int)_elapsed;
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _setTime(new object[] { FormatTime(seconds) });
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            _bindings.Dispose();

            if (_economy != null)
            {
                _economy.Changed -= RefreshStats;
            }

            if (_orders != null)
            {
                _orders.Completed -= OnOrderCompleted;
                _orders.ItemUnlocked -= OnItemUnlocked;
            }

            if (_shop != null)
            {
                _shopButton.clicked -= OnShopClicked;
                _shop.Closed -= OnShopClosed;
                _shop.Dispose();
            }

            if (_pauseButton != null)
            {
                _pauseButton.clicked -= OnPauseClicked;
                _resumeButton.clicked -= OnResumeClicked;
                _homeButton.clicked -= OnHomeClicked;
            }
        }

        // The wrapper UI Toolkit puts around the UXML does not fill the screen by itself, so
        // flex-grow on our root would collapse to content height and overlays would cover only a strip.
        private static void StretchToScreen(VisualElement root)
        {
            root.style.flexGrow = 1f;
            foreach (var child in root.Children())
            {
                child.style.flexGrow = 1f;
            }
        }

        private void BindStats(VisualElement root)
        {
            _setMoney = _bindings.BindWithArgs(root.Q<Label>("money"), LocKeys.HudMoney, new object[] { _economy.Money });
            _setCustomers = _bindings.BindWithArgs(root.Q<Label>("customers"), LocKeys.HudCustomers,
                new object[] { _economy.CustomersServed });
            _setTime = _bindings.BindWithArgs(root.Q<Label>("time"), LocKeys.HudTime, new object[] { FormatTime(0) });

            var served = root.Q<Label>("toast");
            _servedToast = new ToastLabel(served,
                _bindings.BindWithArgs(served, LocKeys.BarServed, new object[] { string.Empty, 0 }));

            var unlocked = root.Q<Label>("toast-unlock");
            _unlockToast = new ToastLabel(unlocked,
                _bindings.BindWithArgs(unlocked, LocKeys.BarUnlocked, new object[] { string.Empty }));
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

        // The shop freezes the game like the pause menu, so clicks never reach customers behind it.
        private void OnShopClicked()
        {
            Time.timeScale = 0f;
            _shop.Open();
        }

        private void OnShopClosed()
        {
            Time.timeScale = 1f;
        }

        private void RefreshStats()
        {
            _setMoney(new object[] { _economy.Money });
            _setCustomers(new object[] { _economy.CustomersServed });
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

        private static string FormatTime(int totalSeconds)
        {
            return $"{totalSeconds / SecondsPerMinute:00}:{totalSeconds % SecondsPerMinute:00}";
        }
    }
}
