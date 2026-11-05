using System.Collections;
using Startup.Core.SceneFlow;
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

            StartCoroutine(ShowThenContinue());
        }

        private IEnumerator ShowThenContinue()
        {
            yield return new WaitForSeconds(_displaySeconds);
            SceneLoader.LoadSceneAsync(SceneNames.MainMenu);
        }
    }
}
