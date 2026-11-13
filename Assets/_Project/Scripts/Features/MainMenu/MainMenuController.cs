using System.Collections.Generic;
using System.Linq;
using Startup.Core.Localization;
using Startup.Core.SceneFlow;
using Startup.Core.UI;
using Startup.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Features.MainMenu
{
    // Self-contained main menu: loads UI + venues from Resources, builds buttons, wires navigation.
    public class MainMenuController : MonoBehaviour
    {
        private const string UxmlPath = "UI/MainMenu";
        private const string VenuesPath = "Venues";

        private readonly LocalizedBindings _bindings = new LocalizedBindings();
        private Button _languageButton;
        private Button _settingsButton;
        private TutorialPanel _tutorial;
        private SettingsPanel _settings;

        private void Start()
        {
            var root = UiDocuments.Create(gameObject, UxmlPath, out _);
            if (root == null)
            {
                return;
            }

            LocalizationService.Initialize(new LanguagePreferenceStore(new LocalDataStore()));
            BindTexts(root);
            BuildVenueButtons(root);
            WireFooter(root);

            _tutorial = new TutorialPanel(root);
            _settings = new SettingsPanel(root, new PlayerProfileStore(new LocalDataStore()), _tutorial);
        }

        private void OnDestroy()
        {
            _bindings.Dispose();
            _settings?.Dispose();
            _tutorial?.Dispose();

            if (_languageButton != null)
            {
                _languageButton.clicked -= LocalizationService.ToggleLanguage;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked -= OpenSettings;
            }
        }

        private void OpenSettings()
        {
            _settings.Open();
        }

        private void BindTexts(VisualElement root)
        {
            _bindings.Bind(root.Q<Label>("title"), LocKeys.MenuTitle);
            _bindings.Bind(root.Q<Label>("subtitle"), LocKeys.MenuSubtitle);
            _bindings.Bind(root.Q<Button>("btn-language"), LocKeys.MenuBtnLanguage);
            _bindings.Bind(root.Q<Button>("btn-settings"), LocKeys.MenuBtnSettings);
            _bindings.Bind(root.Q<Button>("btn-quit"), LocKeys.MenuBtnQuit);
        }

        private void BuildVenueButtons(VisualElement root)
        {
            var list = root.Q<VisualElement>("venue-list");
            if (list == null)
            {
                return;
            }

            // Load all venue definitions; order by name for a stable layout.
            var venues = Resources.LoadAll<VenueDefinition>(VenuesPath)
                .OrderBy(v => v.name)
                .ToList();

            foreach (var venue in venues)
            {
                list.Add(CreateVenueButton(venue));
            }
        }

        private Button CreateVenueButton(VenueDefinition venue)
        {
            var button = new Button();
            _bindings.Bind(button, venue.NameKey);
            button.AddToClassList("venue-btn");

            if (venue.Unlocked)
            {
                var target = venue.SceneName;
                button.clicked += () => SceneLoader.LoadSceneAsync(target);
            }
            else
            {
                var badge = new Label();
                badge.AddToClassList("venue-badge");
                _bindings.Bind(badge, LocKeys.MenuVenueLocked);
                button.Add(badge);
                button.AddToClassList("venue-btn--locked");
                button.SetEnabled(false);
            }

            return button;
        }

        private void WireFooter(VisualElement root)
        {
            _languageButton = root.Q<Button>("btn-language");
            if (_languageButton != null)
            {
                _languageButton.clicked += LocalizationService.ToggleLanguage;
            }

            _settingsButton = root.Q<Button>("btn-settings");
            if (_settingsButton != null)
            {
                _settingsButton.clicked += OpenSettings;
            }

            var quit = root.Q<Button>("btn-quit");
            if (quit != null)
            {
                quit.clicked += QuitGame;
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
