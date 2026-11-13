using Startup.Shared;

namespace Startup.Data
{
    // Persists the selected language code through any IDataStore.
    public class LanguagePreferenceStore : ILanguagePreference
    {
        public const string Key = "settings_language";

        private readonly IDataStore _store;

        public LanguagePreferenceStore(IDataStore store)
        {
            _store = store;
        }

        public string Load(string defaultCode)
        {
            return _store.Load(Key, defaultCode);
        }

        public void Save(string code)
        {
            _store.Save(Key, code);
        }
    }
}
