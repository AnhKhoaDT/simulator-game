using System.Collections;
using Startup.Core.Localization;
using Startup.Core.SceneFlow;
using Startup.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Features.Splash
{
    // Self-contained splash: loads its UI from Resources, shows it briefly, then routes to Main Menu.
    public class SplashController : MonoBehaviour
    {
        private const string UxmlPath = "UI/Splash";
        private const string PanelSettingsPath = "UI/MainPanelSettings";

        [SerializeField, Range(0.5f, 10f)] private float _displaySeconds = 2.5f;

        private readonly LocalizedBindings _bindings = new LocalizedBindings();

        private void Start()
        {
            var uxml = Resources.Load<VisualTreeAsset>(UxmlPath);
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);

            if (uxml == null || panelSettings == null)
            {
                Debug.LogError($"SplashController could not load UI from Resources ('{UxmlPath}', '{PanelSettingsPath}').");
                return;
            }

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;

            LocalizationService.Initialize(new LanguagePreferenceStore(new LocalDataStore()));
            BindTexts(document.rootVisualElement);
            StartCoroutine(ShowThenContinue());
        }

        private void OnDestroy()
        {
            _bindings.Dispose();
        }

        private void BindTexts(VisualElement root)
        {
            _bindings.Bind(root.Q<Label>("title"), LocKeys.SplashTitle);
            _bindings.Bind(root.Q<Label>("tagline"), LocKeys.SplashTagline);
            _bindings.Bind(root.Q<Label>("footer"), LocKeys.SplashLoading);
        }

        private IEnumerator ShowThenContinue()
        {
            yield return new WaitForSeconds(_displaySeconds);
            SceneLoader.LoadSceneAsync(SceneNames.MainMenu);
        }
    }
}
