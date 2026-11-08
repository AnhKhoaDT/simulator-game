using Startup.Shared;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Startup.Core.Localization
{
    // Owns the active locale. Preference storage is injected so Core stays independent of Data.
    public static class LocalizationService
    {
        private static ILanguagePreference _preference;

        public static string CurrentCode
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : LanguageCodes.Default;
            }
        }

        // Safe to call from every scene; only the first call applies the saved language.
        public static void Initialize(ILanguagePreference preference)
        {
            if (_preference != null)
            {
                return;
            }

            if (!LocalizationSettings.HasSettings)
            {
                Debug.LogError("No Localization Settings asset found. Run the menu 'Startup/Setup Localization'.");
                return;
            }

            _preference = preference;
            var init = LocalizationSettings.InitializationOperation;
            if (init.IsDone)
            {
                ApplyLocale(_preference.Load(LanguageCodes.Default), false);
            }
            else
            {
                // Selecting a locale before initialization finishes is unreliable, so wait.
                init.Completed += _ => ApplyLocale(_preference.Load(LanguageCodes.Default), false);
            }
        }

        // One-off lookup for text that is not bound to a UI element; returns the key if unavailable.
        public static string GetText(string key)
        {
            if (!LocalizationSettings.HasSettings)
            {
                return key;
            }

            return LocalizationSettings.StringDatabase.GetLocalizedString(LocKeys.Table, key);
        }

        public static void SetLanguage(string code)
        {
            ApplyLocale(code, true);
        }

        public static void ToggleLanguage()
        {
            var next = CurrentCode == LanguageCodes.Vietnamese ? LanguageCodes.English : LanguageCodes.Vietnamese;
            SetLanguage(next);
        }

        private static void ApplyLocale(string code, bool save)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null)
            {
                Debug.LogError($"Locale '{code}' not found in Localization Settings.");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
            if (save && _preference != null)
            {
                _preference.Save(code);
            }
        }
    }
}
