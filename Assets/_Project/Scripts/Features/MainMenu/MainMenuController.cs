using System.Collections.Generic;
using System.Linq;
using Startup.Core.SceneFlow;
using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Features.MainMenu
{
    // Self-contained main menu: loads UI + venues from Resources, builds buttons, wires navigation.
    public class MainMenuController : MonoBehaviour
    {
        private const string UxmlPath = "UI/MainMenu";
        private const string PanelSettingsPath = "UI/MainPanelSettings";
        private const string VenuesPath = "Venues";

        private void Start()
        {
            var uxml = Resources.Load<VisualTreeAsset>(UxmlPath);
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);

            if (uxml == null || panelSettings == null)
            {
                Debug.LogError($"MainMenuController could not load UI from Resources ('{UxmlPath}', '{PanelSettingsPath}').");
                return;
            }

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;

            var root = document.rootVisualElement;
            BuildVenueButtons(root);
            WireFooter(root);
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

        private static Button CreateVenueButton(VenueDefinition venue)
        {
            var button = new Button { text = venue.DisplayName };
            button.AddToClassList("venue-btn");

            if (venue.Unlocked)
            {
                var target = venue.SceneName;
                button.clicked += () => SceneLoader.LoadSceneAsync(target);
            }
            else
            {
                button.text = venue.DisplayName + "  [Khoa]";
                button.AddToClassList("venue-btn--locked");
                button.SetEnabled(false);
            }

            return button;
        }

        private void WireFooter(VisualElement root)
        {
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
