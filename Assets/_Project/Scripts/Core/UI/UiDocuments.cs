using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Core.UI
{
    // One way to put a UXML screen on a GameObject: shared panel settings, the default theme, full-screen root.
    public static class UiDocuments
    {
        private const string PanelSettingsPath = "UI/MainPanelSettings";
        private const string ThemePath = "UI/RuntimeTheme";

        // Returns the document root, or null (with an error) if the UXML or panel settings are missing.
        public static VisualElement Create(GameObject host, string uxmlPath, out UIDocument document)
        {
            document = null;
            var uxml = Resources.Load<VisualTreeAsset>(uxmlPath);
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (uxml == null || panelSettings == null)
            {
                Debug.LogError($"UiDocuments could not load UI from Resources ('{uxmlPath}', '{PanelSettingsPath}').");
                return null;
            }

            EnsureTheme(panelSettings);
            document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;

            var root = document.rootVisualElement;
            StretchToScreen(root);
            return root;
        }

        // Without a theme, buttons, text fields and scroll views lose their built-in look and text alignment.
        private static void EnsureTheme(PanelSettings panelSettings)
        {
            if (panelSettings.themeStyleSheet != null)
            {
                return;
            }

            var theme = Resources.Load<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                Debug.LogWarning($"UiDocuments found no runtime theme at Resources/{ThemePath}.");
                return;
            }

            panelSettings.themeStyleSheet = theme;
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
    }
}
