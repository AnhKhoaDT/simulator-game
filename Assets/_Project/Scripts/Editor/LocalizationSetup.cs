using System.Collections.Generic;
using System.Reflection;
using Startup.Core.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;

namespace Startup.EditorTools
{
    // One-click, idempotent setup of Localization Settings, locales and the UI string table.
    public static class LocalizationSetup
    {
        private const string RootFolder = "Assets/_Project/Settings/Localization";
        private const string LocalesFolder = RootFolder + "/Locales";
        private const string TablesFolder = RootFolder + "/Tables";
        private const string SettingsPath = RootFolder + "/Localization Settings.asset";

        [MenuItem("Startup/Setup Localization")]
        public static void Run()
        {
            EnsureFolder("Assets/_Project/Settings", "Localization");
            EnsureFolder(RootFolder, "Locales");
            EnsureFolder(RootFolder, "Tables");

            EnsureSettings();
            var vi = EnsureLocale(LanguageCodes.Vietnamese, "Vietnamese (vi)");
            var en = EnsureLocale(LanguageCodes.English, "English (en)");
            var collection = EnsureCollection(new List<Locale> { vi, en });
            var changed = FillEntries(collection);

            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WarnAboutMissingKeys();
            Debug.Log($"Startup localization ready. {changed} entries added or updated.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void EnsureSettings()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
            {
                return;
            }

            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        private static Locale EnsureLocale(string code, string displayName)
        {
            var identifier = new LocaleIdentifier(code);
            var locale = LocalizationEditorSettings.GetLocale(identifier);
            if (locale != null)
            {
                return locale;
            }

            locale = Locale.CreateLocale(identifier);
            locale.name = displayName;
            AssetDatabase.CreateAsset(locale, $"{LocalesFolder}/{displayName}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static StringTableCollection EnsureCollection(List<Locale> locales)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocKeys.Table);
            return collection != null
                ? collection
                : LocalizationEditorSettings.CreateStringTableCollection(LocKeys.Table, TablesFolder, locales);
        }

        private static int FillEntries(StringTableCollection collection)
        {
            var changed = 0;
            foreach (var row in LocalizationData.Rows)
            {
                if (!collection.SharedData.Contains(row.Key))
                {
                    collection.SharedData.AddKey(row.Key);
                }

                changed += SetValue(collection, LanguageCodes.Vietnamese, row.Key, row.Vietnamese);
                changed += SetValue(collection, LanguageCodes.English, row.Key, row.English);
            }

            return changed;
        }

        private static int SetValue(StringTableCollection collection, string code, string key, string value)
        {
            var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
            if (table == null)
            {
                Debug.LogError($"String table for locale '{code}' is missing in '{collection.TableCollectionName}'.");
                return 0;
            }

            // Placeholders like {0} need Smart String turned on for the entry.
            var smart = value.Contains("{");
            var entry = table.GetEntry(key);
            if (entry != null && entry.Value == value && entry.IsSmart == smart)
            {
                return 0;
            }

            // Script data is authoritative, so existing differing values are overwritten.
            if (entry == null)
            {
                entry = table.AddEntry(key, value);
            }
            else
            {
                entry.Value = value;
            }

            entry.IsSmart = smart;

            LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            EditorUtility.SetDirty(table);
            return 1;
        }

        // Catches a LocKeys constant that has no row in LocalizationData.
        private static void WarnAboutMissingKeys()
        {
            var known = new HashSet<string>();
            foreach (var row in LocalizationData.Rows)
            {
                known.Add(row.Key);
            }

            foreach (var field in typeof(LocKeys).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                var value = field.GetRawConstantValue() as string;
                if (field.Name != nameof(LocKeys.Table) && !known.Contains(value))
                {
                    Debug.LogWarning($"LocKeys.{field.Name} ('{value}') has no row in LocalizationData.");
                }
            }
        }
    }
}
